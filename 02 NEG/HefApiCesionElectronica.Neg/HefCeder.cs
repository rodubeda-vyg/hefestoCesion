
using HefApiCesionElectronica.Neg.Properties;
using HefModCesiones;
using Newtonsoft.Json;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace HefApiCesionElectronica.Neg
{

    /// <summary>
    /// Tipos de archivos
    /// </summary>
    public enum hefTipoArchivo
    { 
        Default = 0,
        EnvioDte = 1,
        Dte = 2,
        Aec = 3
    }

    /// <summary>
    /// Cree la interface para el rpoceso de inyeccion de dependencias
    /// </summary>
    public interface IHefCeder
    {

        /// <summary>
        /// Genera la cesion electrónica de un documento DTE
        /// </summary>
        /// <param name="consulta"></param>
        /// <returns></returns>
        HefRespuesta GenerarCesionElectronica(HefConsultaCesionDTO consulta);

        /// <summary>
        /// Consulta e trackid de un envio al SII (AEC)
        /// </summary>
        /// <param name="consulta"></param>
        /// <returns></returns>
        HefRespuesta ConsultarTrackid(HefConsultaTrackidDTO consulta);

        /// <summary>
        /// Consulta estado de un documento aec 
        /// </summary>
        /// <param name="consulta"></param>
        /// <returns></returns>
        HefRespuesta ConsultarEstadoCesion(HefConsultaEstadoCesionDTO consulta);

    }

    /// <summary>
    /// Permite ceder un documento DTE
    /// </summary>
    public class HefCeder: IHefCeder
    {   
        /// <summary>
        /// Indica que tipo de documento se esta cargando
        /// </summary>
        public hefTipoArchivo TipoDocumento { get; set; }

        #region VARIABLES PRIVADAS DE LA CLASE

        /// <summary>
        /// Almacena la consulta original del cliente
        /// </summary>
        private HefConsultaCesionDTO? _consulta { get; set; }

        /// <summary>
        /// Representa el documento original
        /// </summary>
        public string? _xmlOriginal { get; set; }

        /// <summary>
        /// Representa el docuemnto xml a procesar
        /// </summary>
        private string? _xmlDte { get; set; }

        /// <summary>
        /// Representa el certificado actual
        /// </summary>
        private X509Certificate2? _certificado { get; set; }

        /// <summary>
        /// Representa el TMST actual del documento
        /// </summary>
        private string? _tmstActual { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        /// <summary>
        /// Credenciales de autenticación
        /// </summary>
        public string? _credenciales { get; set; }

        /// <summary>
        /// Represena el token necesario para la publicación del AEC
        /// </summary>
        public string? _token { get; set; }

        #endregion

        /// <summary>
        /// Genera la cesión electrónica
        /// </summary>
        public HefRespuesta GenerarCesionElectronica(HefConsultaCesionDTO consulta)
        {
            ////
            //// Cree la respuesta del proceso
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "generarCesionElectronica()";

            ////
            //// Cual es el encoding a utilizar
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");

            ////
            //// Guarde la consulta original
            this._consulta = consulta;

            ////
            //// Variables privadas del metodo
            string _xml = string.Empty;

            ////
            //// Inicie el proceso
            try
            {
                #region DETERMINAR SI ES UN DOCUMENTO DTE O AEC?

                ////
                //// Recupere el documento xml
                _xml = encoding.GetString(Convert.FromBase64String(_consulta.DteBase64));

                ////
                //// Determine que documento es utilizando el schema apropiado
                //// EnvioDTE_v10.xsd, DTE_v10.xsd, AEC_v10.xsd
                this.TipoDocumento = hefTipoArchivo.Default;

                ////
                //// Es un documento EnvioDTE?
                if (Regex.IsMatch(_xml, ".*?<EnvioDTE.*?>.*?<DTE", RegexOptions.Singleline))
                    this.TipoDocumento = hefTipoArchivo.EnvioDte;
                
                //if (Regex.IsMatch(_xml, "\\?>[\n|\r\n]<EnvioDTE.*?>|[\n|\r\n]<EnvioDTE.*?>", RegexOptions.Singleline))
                //    this.TipoDocumento = hefTipoArchivo.EnvioDte;
                //////
                //// Es un documento DTE?
                //if (Regex.IsMatch(_xml, "<DTE.*?>", RegexOptions.Singleline))
                //    this.TipoDocumento = hefTipoArchivo.Dte;
                
                ////
                //// Es un documento AEC?
                if (Regex.IsMatch(_xml, "<AEC.*?>.*?<DTECedido.*?<DTE", RegexOptions.Singleline))
                    this.TipoDocumento = hefTipoArchivo.Aec;
                ////
                //// Compruebe la validación
                if (this.TipoDocumento == hefTipoArchivo.Default)
                    throw new Exception("No fue posible identificar el documento xml actual.");

                ////
                //// Recupera el documento original para procesos posteriores
                this._xmlOriginal = _xml;

                #endregion

                #region VALIDE ELEMENTOS PARA REALIZAR LA CESIÓN DEL DOCUMENTO

                ////
                //// Recupere el primer dte del documento actual
                //// En el caso que exista más de un dte emita un error indicando al 
                //// usuario que el documento no es valido para la operación.
                List<string> _dtes = Regex.Matches(
                    _xml,
                        "<DTE\\s.*?>",
                            RegexOptions.Singleline)
                                .Cast<Match>().Select(p => p.Value)
                                    .ToList();

                ////
                //// Cuantos archivos dtes hay?
                if (_dtes.Count > 1)
                    throw new Exception($"No es posible procesar el archivo xml actual, este contiene " +
                        $"más de un archivo DTE. Para realizar el proceso de cesión debe especificar " +
                            $"cual es el documento dte a ceder.");

                ////
                //// Quizas no existen los dtes
                if (_dtes.Count == 0)
                    throw new Exception($"No fue posible encontrar un docucumento " +
                        $"dte en el archivo xml actual.");

                ////
                //// Extraiga el documento DTE
                this._xmlDte = Regex.Match(_xml, "<DTE.*?<\\/DTE>", RegexOptions.Singleline).Value;

                #endregion

                #region RECUPERAR EL CERTIFICADO 

                ////
                //// Transforme el base64 a certificado
                byte[] buffer = Convert.FromBase64String(_consulta.CertificadoBase64);
                X509Certificate2 certificado = new X509Certificate2(buffer, consulta.Heslo);
                if (certificado == null)
                    throw new Exception($"No fue posible construír el certificado con los datos de " +
                        $"la consulta. Favor verificar el pass y los bytes del certifiado.");

                ////
                //// El certificado tiene private key?
                if ( !certificado.HasPrivateKey )
                    throw new Exception($"Certificado " +
                        $"'{certificado.Subject.HefGetCertificadoCn()}' " +
                            $"no tiene private key");

                ////
                //// El certificado está expirado?
                if (DateTime.TryParse(certificado.GetExpirationDateString(), out DateTime ExpirationDate))
                    if (DateTime.Now > ExpirationDate)
                        throw new Exception($"El certificado " +
                            $"'{certificado.Subject.HefGetCertificadoCn()}' " +
                                $"se encuentra expirado. '{certificado.GetExpirationDateString()}'");

                ////
                //// Almacene el certificado
                this._certificado = certificado;

                #endregion

                #region RECUPERAR LAS CREDENCIALES DEL USUARIO Y TOKEN 

                ////
                //// Recuperar el token del certificado
                HefRespuesta respCreden = Dal.HefAutenticacion.GetCredenciales(this._certificado);
                if (!respCreden.EsCorrecto)
                    throw new Exception($"No fue posible recuperar el token del certificado. " +
                        $"'{respCreden.Detalle}'");

                this._credenciales = respCreden.Resultado as string;

                HefRespuesta respToken = Dal.HefAutenticacion.GetToken(this._certificado);
                if (!respToken.EsCorrecto)
                        throw new Exception($"No fue posible recuperar el token del certificado. " +
                            $"'{respToken.Detalle}'");

                this._token = respToken.Resultado as string;

                #endregion

                #region IDENTIFIQUE EL PROCESO A EJECUTAR

                HefRespuesta respDocumentoAec = new HefRespuesta();
                switch (this.TipoDocumento)
                {
                    
                    case hefTipoArchivo.EnvioDte:
                        respDocumentoAec = GenerarDocumentoAEC();
                        break;
                    case hefTipoArchivo.Dte:
                        respDocumentoAec = GenerarDocumentoAEC();
                        break;
                    case hefTipoArchivo.Aec:
                        respDocumentoAec = GenerarDocumentoReAEC();
                        break;
                    default:
                        throw new Exception("No fue posible identificar el tipo de proceso a ejecutar.");
                }

                #endregion

                ////
                //// Recupere el objeto respuesta de la cesion
                resp = respDocumentoAec;

            }
            catch (Exception ex)
            {
                ////
                //// Cree la respuesta del proceso
                resp.EsCorrecto = false;
                resp.Detalle = ex.Message;
                resp.Resultado = null;
            }

            ////
            //// Regrese el valor de retorno
            return resp;
        
        }

        /// <summary>
        /// Permite consultar al SII por el estado de un envío al SII
        /// </summary>
        /// <param name="consulta"></param>
        /// <returns></returns>
        public HefRespuesta ConsultarTrackid(HefConsultaTrackidDTO consulta)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "ConsultarTrackid";

            ////
            //// Inicie el proceso
            try
            {

                #region RECUPERAR LAS CREDENCIALES DEL USUARIO Y TOKEN 

                ////
                //// Recupere el certificado
                this._certificado = consulta.CertificadoBase64.HefGetCertificate(consulta.Heslo);
                
                ////
                //// Recuperar el token del certificado
                HefRespuesta respCreden = Dal.HefAutenticacion.GetCredenciales(this._certificado);
                if (!respCreden.EsCorrecto)
                    throw new Exception($"No fue posible recuperar el token del certificado. " +
                        $"'{respCreden.Detalle}'");

                this._credenciales = respCreden.Resultado as string;

                HefRespuesta respToken = Dal.HefAutenticacion.GetToken(this._certificado);
                if (!respToken.EsCorrecto)
                    throw new Exception($"No fue posible recuperar el token del certificado. " +
                        $"'{respToken.Detalle}'");

                this._token = respToken.Resultado as string;

                #endregion

                #region CONSULTAR AL SII POR EL ESTADO DEL TRACKID

                ////
                //// Consulte el estado del trackid
                HefRespuesta respConsulta = Dal.HefConsultas.RecuperarEstadoTrackid(this._token, consulta.RutEmpresa, consulta.Trackid);
                if (!respConsulta.EsCorrecto)
                    return respConsulta;

                ////
                //// Analice la respuesta del SII
                resp =  (respConsulta.Resultado as string).HefAnalizarRespuestaEstadoCesion();

                ////
                //// Agregar eltrackid de la operación para vincular la consulta 
                resp.Trackid = consulta.Trackid.ToString();

                #endregion

                ////
                //// Regrese el valor de retorno
                //// El valor de retorno esta en resp


            }
            catch (Exception err)
            {
                ////
                //// notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.Resultado = null;
            }

            return resp;
        
        }

        /// <summary>
        /// Permite consultar al SII por el estado de un documento cedido
        /// </summary>
        /// <param name="consulta"></param>
        /// <returns></returns>
        public HefRespuesta ConsultarEstadoCesion(HefConsultaEstadoCesionDTO consulta)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "ConsultarEstadoCesion";

            ////
            //// Inicie el proceso
            try
            {

                #region RECUPERAR LAS CREDENCIALES DEL USUARIO Y TOKEN 

                ////
                //// Recupere el certificado
                this._certificado = consulta.CertificadoBase64.HefGetCertificate(consulta.Heslo);

                ////
                //// Recuperar el token del certificado
                HefRespuesta respCreden = Dal.HefAutenticacion.GetCredenciales(this._certificado);
                if (!respCreden.EsCorrecto)
                    throw new Exception($"No fue posible recuperar el token del certificado. " +
                        $"'{respCreden.Detalle}'");

                this._credenciales = respCreden.Resultado as string;

                HefRespuesta respToken = Dal.HefAutenticacion.GetToken(this._certificado);
                if (!respToken.EsCorrecto)
                    throw new Exception($"No fue posible recuperar el token del certificado. " +
                        $"'{respToken.Detalle}'");

                this._token = respToken.Resultado as string;

                #endregion

                #region CONSULTAR AL SII POR EL ESTADO DEL TRACKID

                ////
                //// Consulte el estado del trackid
                HefRespuesta respConsulta = Dal.HefConsultas.RecuperarEstadoCesion(
                    this._token, 
                        consulta.RutEmisor,
                            consulta.TipoDoc.ToString(),
                                consulta.FolioDoc.ToString());
                
                ////
                //// El proceso fue correcto?
                if (!respConsulta.EsCorrecto)
                    return respConsulta;

                ////
                //// Analice la respuesta del SII
                resp = (respConsulta.Resultado as string).HefAnalizarRespuestaEstadoCesion2();

                #endregion

                ////
                //// Regrese el valor de retorno
                //// El valor de retorno esta en resp


            }
            catch (Exception err)
            {
                ////
                //// notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.Resultado = null;
            }

            return resp;

        }

        #region METODOS PRIVADOS 03-04-2024

        /// <summary>
        /// Inicia la generación de una cesion
        /// </summary>
        private HefRespuesta GenerarDocumentoAEC()
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "GenerarDocumentoAEC";

            ////
            //// Inicie el proceso
            try
            {

                #region PREPARE EL DOCUMENTO

                ////
                //// Recuperar el template del AEC
                string template = Resources.AEC_Template;
                string xmlDte = this._xmlOriginal.HefGetDTE();

                ////
                //// Recupere todas las cesiones del documento original del cliente
                //// y luego asignelas al documento template
                string cesiones = this._xmlOriginal.HefGetCesiones();
                if ( !string.IsNullOrEmpty(cesiones) )
                    template = Regex.Replace(
                        template,
                            "</DTECedido>",
                                $"</DTECedido>\r\n{cesiones}",
                                    RegexOptions.Singleline);

                ////
                //// Recupere la Cesión del template para actualizar sus datos
                int SeqCesion = template.HefGetCountCesiones();
                string lastCesion = template.HefGetLastCesion();
                lastCesion = Regex.Replace(
                    lastCesion,
                        "<SeqCesion>(.*?)</SeqCesion>",
                            $"<SeqCesion>{SeqCesion}</SeqCesion>",
                                RegexOptions.Singleline);

                ////
                //// Actualice el ID de la cesion
                lastCesion = Regex.Replace(
                    lastCesion,
                        "ID=\".*?\"",
                            $"ID=\"DocumentoCesion{SeqCesion}\"",
                                RegexOptions.Singleline);

                #endregion

                #region AGREGAR LA CARATULA DEL DOCUMENTO (AEC)

                ////
                //// Cree la caratula del documento AEC
                template = Regex.Replace(template,
                    "<RutCedente>.*?</RutCedente>",
                        $"<RutCedente>{_consulta.RutCedente}</RutCedente>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<RutCesionario>.*?</RutCesionario>",
                        $"<RutCesionario>{_consulta.RutCesionario}</RutCesionario>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<NmbContacto>.*?</NmbContacto>",
                        $"<NmbContacto>{_consulta.NmbContacto}</NmbContacto>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<FonoContacto>.*?</FonoContacto>",
                        $"<FonoContacto>{_consulta.FonoContacto}</FonoContacto>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<MailContacto>.*?</MailContacto>",
                        $"<MailContacto>{_consulta.MailContacto}</MailContacto>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<TmstFirmaEnvio>.*?</TmstFirmaEnvio>",
                        $"<TmstFirmaEnvio>{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}</TmstFirmaEnvio>",
                            RegexOptions.Singleline);


                #endregion

                #region AGREGAR DATOS DEL DOCUMENTO (DTECedido)

                ////
                //// Agregar el DTE al AEC
                template = Regex.Replace(
                    template,
                        "<DTE/>",
                            xmlDte,
                                RegexOptions.Singleline);

                ////
                //// Actualice 'TmstFirma'
                template = Regex.Replace(
                    template,
                        "<TmstFirma>.*?<\\/TmstFirma>",
                            $"<TmstFirma>{this._tmstActual}</TmstFirma>",
                                RegexOptions.Singleline);

                #endregion

                #region  AGREGAR LOS DATOS DE LA CESION (DTE)

                ////
                //// Complete los datos del documento DTE
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("TipoDTE"),
                        xmlDte.HefGetXmlNodeValue("TipoDTE"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("RUTEmisor"),
                        xmlDte.HefGetXmlNodeValue("RUTEmisor"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("RUTReceptor"),
                        $"<RUTReceptor>{xmlDte.HefGetXmlNodeValue2("RUTRecep")}</RUTReceptor>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("Folio"),
                        xmlDte.HefGetXmlNodeValue("Folio"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("FchEmis"),
                        xmlDte.HefGetXmlNodeValue("FchEmis"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("MntTotal"),
                        xmlDte.HefGetXmlNodeValue("MntTotal"),
                            RegexOptions.Singleline);

                #endregion

                #region  AGREGAR LOS DATOS DE LA CESION (CEDENTE)

                ////
                //// Complete los datos del cedente
                lastCesion = Regex.Replace(lastCesion,
                    "<RUT>CCCC</RUT>",
                        $"<RUT>{_consulta.RutCedente}</RUT>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<RazonSocial>CCCC</RazonSocial>",
                        $"<RazonSocial>{_consulta.RznSocCedente}</RazonSocial>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<Direccion>CCCC</Direccion>",
                        $"<Direccion>{_consulta.DireccionCedente}</Direccion>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<eMail>CCCC</eMail>",
                        $"<eMail>{_consulta.EmailCedente}</eMail>",
                            RegexOptions.Singleline);


                #endregion

                #region AGREGAR LOS DATOS DE DECLARACION JURADA ( CEDENTE )

                ////
                //// Recuperar los datos del certificado
                string datosCertificado = this._credenciales.HefGetDatosCertificado();

                ////
                //// recupere la información
                lastCesion = Regex.Replace(lastCesion,
                   "<RUT>GGGG<\\/RUT>",
                       $"<RUT>{datosCertificado.Split('|')[0]}</RUT>",
                           RegexOptions.Singleline);

                ////
                //// recupere la información
                lastCesion = Regex.Replace(lastCesion,
                   "<Nombre>GGGG<\\/Nombre>",
                       $"<Nombre>{datosCertificado.Split('|')[1]}</Nombre>",
                           RegexOptions.Singleline);

                ////
                //// Recupere el rut del receptor y rznsoc
                string rutReceptor = Regex.Match(xmlDte,
                    "<RUTRecep>(.*?)<\\/RUTRecep>",
                        RegexOptions.Singleline).Groups[1].Value;

                string RznSocRecep = Regex.Match(xmlDte,
                    "<RznSocRecep>(.*?)<\\/RznSocRecep>",
                        RegexOptions.Singleline).Groups[1].Value;

                ////
                //// Complete los datos de la 'DeclaracionJurada'
                string declaracion = string.Empty;
                declaracion += "Se declara bajo juramento que " + _consulta.RznSocCedente + ", RUT " + _consulta.RutCedente + " ha puesto a ";
                declaracion += "disposición del cesionario " + _consulta.RznSocCesionario + ", RUT " + _consulta.RutCesionario + ", el ";
                declaracion += "o los documentos donde constan los recibos de las mercaderías entregadas o ";
                declaracion += "servicios prestados, entregados por parte del deudor de la factura ";
                declaracion += $"{RznSocRecep}, RUT {rutReceptor}, de acuerdo a lo establecido en la Ley N° 19.983";

                ////
                //// Asigne los datos
                lastCesion = Regex.Replace(lastCesion,
                    "<DeclaracionJurada/>",
                        $"<DeclaracionJurada>{declaracion}</DeclaracionJurada>",
                            RegexOptions.Singleline);

                #endregion

                #region AGREGAR LOS DATOS DE LA CESION ( CESIONARIO )

                ////
                //// Complete los datos del cesionario
                lastCesion = Regex.Replace(lastCesion,
                    "<RUT>DDDD</RUT>",
                        $"<RUT>{_consulta.RutCesionario}</RUT>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                   "<RazonSocial>DDDD</RazonSocial>",
                       $"<RazonSocial>{_consulta.RznSocCesionario}</RazonSocial>",
                           RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<Direccion>DDDD</Direccion>",
                        $"<Direccion>{_consulta.DireccionCesionario}</Direccion>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<eMail>DDDD</eMail>",
                        $"<eMail>{_consulta.EmailCesionario}</eMail>",
                            RegexOptions.Singleline);

                #endregion

                #region AGREGAR LOS DATOS DE LA CESION ( DATOS FINALES )

                ////
                //// agregar lso datos 
                //// - MontoCesion
                //// - UltimoVencimiento
                //// - TmstCesion

                ////
                //// Complete los datos de el monto de la cesión.
                lastCesion = Regex.Replace(lastCesion,
                    "<MontoCesion>EEEE</MontoCesion>",
                        $"<MontoCesion>{xmlDte.HefGetXmlNodeValue2("MntTotal")}</MontoCesion>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<UltimoVencimiento>EEEE</UltimoVencimiento>",
                        $"<UltimoVencimiento>{xmlDte.HefGetUltimoVencimientoDTE()}</UltimoVencimiento>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<TmstCesion>EEEE</TmstCesion>",
                        $"<TmstCesion>{this._tmstActual}</TmstCesion>",
                            RegexOptions.Singleline);

                #endregion

                #region ACTUALICE LA ULTIMA CESION

                ////
                //// Recuperar la ultima cesion (template)
                string currentlastCesion = template.HefGetLastCesion();
                template = Regex.Replace(
                    template,
                        currentlastCesion,
                            lastCesion,
                                RegexOptions.Singleline);


                #endregion

                #region FIRMAR EL DOCUMENTO COMPLETO

                ////
                //// Firme el documento completo
                HefRespuesta respSignature = FirmarDocumentoAECCompleto(template);
                if (!respSignature.EsCorrecto)
                    throw new Exception($"No fue posible firmar el documento completo AEC. " +
                        $"'{respSignature.Detalle}'");

                ////
                //// Recupere el documento firmado
                XmlDocument xAEC = new XmlDocument();
                xAEC.PreserveWhitespace = true;
                xAEC.LoadXml(respSignature.Resultado as string);

                #endregion

                #region ENVIAR AEC AL SII

                ////
                //// Recupere al ultima cesion del documento
                lastCesion = xAEC.OuterXml.HefGetLastCesion();
                string eMail = Regex.Match(
                    lastCesion,
                        "Cedente.*?eMail>(.*?)<",
                            RegexOptions.Singleline).Groups[1].Value;
                string rutCedente = Regex.Match(
                    lastCesion,
                        "Cedente.*?RUT>(.*?)<",
                            RegexOptions.Singleline).Groups[1].Value;
                string nombreArchivo = $"HefestoCesion_R" +
                    $"{rutCedente.Replace("-", "")}" +
                        $"_F{DateTime.Now.ToString("yyyyMMddHHmmss")}.xml";

                ////
                //// Envíe al SII el documento AEC
                resp = Dal
                    .HefPublicacion
                        .PublicarDocumentoProduccion(
                            xAEC.OuterXml,
                                this._token,
                                    eMail,
                                        rutCedente,
                                            nombreArchivo);


                ////
                //// Actualice el valor de la respuesta
                if (resp.EsCorrecto)
                    resp.Resultado = ((string)resp.Resultado).HefGetBase64();


                ////
                //// Test
                //// Guardar el documento en disco
                if (resp.EsCorrecto)
                    File.WriteAllText($"Repositorio/HefCesion{resp.Trackid}.xml", xAEC.OuterXml, Encoding.GetEncoding("ISO-8859-1"));


                #endregion

            }
            catch (Exception err)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.Resultado = null;

            }

            ////
            //// Regrese el valor de retorno
            return resp;

        }

        /// <summary>
        /// Inicia la generación de una cesion
        /// </summary>
        private HefRespuesta GenerarDocumentoReAEC()
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "GenerarDocumentoReAEC";

            ////
            //// Inicie el proceso
            try
            {

                #region PREPARE EL DOCUMENTO

                ////
                //// Recuperar el template del AEC
                string template = Resources.AEC_Template;
                string xmlDte = this._xmlOriginal.HefGetDTE();

                ////
                //// Recupere todas las cesiones del documento original del cliente
                //// y luego asignelas al documento template
                string cesiones = this._xmlOriginal.HefGetCesiones();
                template = Regex.Replace(
                    template,
                        "</DTECedido>",
                            $"</DTECedido>\r\n{cesiones}",
                                RegexOptions.Singleline);

                ////
                //// Recupere la Cesión del template para actualizar sus datos
                int SeqCesion = template.HefGetCountCesiones();
                string lastCesion = template.HefGetLastCesion();
                lastCesion = Regex.Replace(
                    lastCesion,
                        "<SeqCesion>(.*?)</SeqCesion>",
                            $"<SeqCesion>{SeqCesion}</SeqCesion>",
                                RegexOptions.Singleline);

                ////
                //// Actualice el ID de la cesion
                lastCesion = Regex.Replace(
                    lastCesion,
                        "ID=\".*?\"",
                            $"ID=\"DocumentoCesion{SeqCesion}\"",
                                RegexOptions.Singleline);

                #endregion

                #region AGREGAR LA CARATULA DEL DOCUMENTO (AEC)

                ////
                //// Cree la caratula del documento AEC
                template = Regex.Replace(template,
                    "<RutCedente>.*?</RutCedente>",
                        $"<RutCedente>{_consulta.RutCedente}</RutCedente>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<RutCesionario>.*?</RutCesionario>",
                        $"<RutCesionario>{_consulta.RutCesionario}</RutCesionario>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<NmbContacto>.*?</NmbContacto>",
                        $"<NmbContacto>{_consulta.NmbContacto}</NmbContacto>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<FonoContacto>.*?</FonoContacto>",
                        $"<FonoContacto>{_consulta.FonoContacto}</FonoContacto>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<MailContacto>.*?</MailContacto>",
                        $"<MailContacto>{_consulta.MailContacto}</MailContacto>", RegexOptions.Singleline);
                template = Regex.Replace(template,
                    "<TmstFirmaEnvio>.*?</TmstFirmaEnvio>",
                        $"<TmstFirmaEnvio>{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}</TmstFirmaEnvio>",
                            RegexOptions.Singleline);


                #endregion

                #region AGREGAR DATOS DEL DOCUMENTO (DTECedido)

                ////
                //// copie el dtecedido original solo cuando esta sea una recesión.
                template = Regex.Replace(
                    template,
                        "<DTECedido.*?<\\/DTECedido>",
                            Regex.Match(this._xmlOriginal, "<DTECedido.*?<\\/DTECedido>",RegexOptions.Singleline).Value,
                                RegexOptions.Singleline);

                #endregion

                #region  AGREGAR LOS DATOS DE LA CESION (DTE)

                ////
                //// Complete los datos del documento DTE
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("TipoDTE"),
                        xmlDte.HefGetXmlNodeValue("TipoDTE"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("RUTEmisor"),
                        xmlDte.HefGetXmlNodeValue("RUTEmisor"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("RUTReceptor"),
                        $"<RUTReceptor>{xmlDte.HefGetXmlNodeValue2("RUTRecep")}</RUTReceptor>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("Folio"),
                        xmlDte.HefGetXmlNodeValue("Folio"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("FchEmis"),
                        xmlDte.HefGetXmlNodeValue("FchEmis"),
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    lastCesion.HefGetXmlNodeValue("MntTotal"),
                        xmlDte.HefGetXmlNodeValue("MntTotal"),
                            RegexOptions.Singleline);

                #endregion

                #region  AGREGAR LOS DATOS DE LA CESION (CEDENTE)

                ////
                //// Complete los datos del cedente
                lastCesion = Regex.Replace(lastCesion,
                    "<RUT>CCCC</RUT>",
                        $"<RUT>{_consulta.RutCedente}</RUT>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<RazonSocial>CCCC</RazonSocial>",
                        $"<RazonSocial>{_consulta.RznSocCedente}</RazonSocial>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<Direccion>CCCC</Direccion>",
                        $"<Direccion>{_consulta.DireccionCedente}</Direccion>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<eMail>CCCC</eMail>",
                        $"<eMail>{_consulta.EmailCedente}</eMail>",
                            RegexOptions.Singleline);


                #endregion

                #region AGREGAR LOS DATOS DE DECLARACION JURADA ( CEDENTE )

                ////
                //// Recupere los datos del certificado
                string DatosCertificado = this._credenciales.HefGetDatosCertificado();


                ////
                //// recupere la información
                lastCesion = Regex.Replace(lastCesion,
                   "<RUT>GGGG<\\/RUT>",
                       $"<RUT>{DatosCertificado.Split('|')[0]}</RUT>",
                           RegexOptions.Singleline);

                ////
                //// recupere la información
                lastCesion = Regex.Replace(lastCesion,
                   "<Nombre>GGGG<\\/Nombre>",
                       $"<Nombre>{DatosCertificado.Split('|')[1]}</Nombre>",
                           RegexOptions.Singleline);

                ////
                //// Recupere el rut del receptor y rznsoc
                string rutReceptor = Regex.Match(xmlDte,
                    "<RUTRecep>(.*?)<\\/RUTRecep>",
                        RegexOptions.Singleline).Groups[1].Value;

                string RznSocRecep = Regex.Match(xmlDte,
                    "<RznSocRecep>(.*?)<\\/RznSocRecep>",
                        RegexOptions.Singleline).Groups[1].Value;

                ////
                //// Complete los datos de la 'DeclaracionJurada'
                string declaracion = string.Empty;
                declaracion += "Se declara bajo juramento que " + _consulta.RznSocCedente + ", RUT " + _consulta.RutCedente + " ha puesto a ";
                declaracion += "disposición del cesionario " + _consulta.RznSocCesionario + ", RUT " + _consulta.RutCesionario + ", el ";
                declaracion += "o los documentos donde constan los recibos de las mercaderías entregadas o ";
                declaracion += "servicios prestados, entregados por parte del deudor de la factura ";
                declaracion += $"{RznSocRecep}, RUT {rutReceptor}, de acuerdo a lo establecido en la Ley N° 19.983";

                ////
                //// Asigne los datos
                lastCesion = Regex.Replace(lastCesion,
                    "<DeclaracionJurada/>",
                        $"<DeclaracionJurada>{declaracion}</DeclaracionJurada>",
                            RegexOptions.Singleline);

                #endregion

                #region AGREGAR LOS DATOS DE LA CESION ( CESIONARIO )

                ////
                //// Complete los datos del cesionario
                lastCesion = Regex.Replace(lastCesion,
                    "<RUT>DDDD</RUT>",
                        $"<RUT>{_consulta.RutCesionario}</RUT>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                   "<RazonSocial>DDDD</RazonSocial>",
                       $"<RazonSocial>{_consulta.RznSocCesionario}</RazonSocial>",
                           RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<Direccion>DDDD</Direccion>",
                        $"<Direccion>{_consulta.DireccionCesionario}</Direccion>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<eMail>DDDD</eMail>",
                        $"<eMail>{_consulta.EmailCesionario}</eMail>",
                            RegexOptions.Singleline);

                #endregion

                #region AGREGAR LOS DATOS DE LA CESION ( DATOS FINALES )

                ////
                //// agregar lso datos 
                //// - MontoCesion
                //// - UltimoVencimiento
                //// - TmstCesion

                ////
                //// Complete los datos de el monto de la cesión.
                lastCesion = Regex.Replace(lastCesion,
                    "<MontoCesion>EEEE</MontoCesion>",
                        $"<MontoCesion>{xmlDte.HefGetXmlNodeValue2("MntTotal")}</MontoCesion>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<UltimoVencimiento>EEEE</UltimoVencimiento>",
                        $"<UltimoVencimiento>{xmlDte.HefGetUltimoVencimientoDTE()}</UltimoVencimiento>",
                            RegexOptions.Singleline);
                lastCesion = Regex.Replace(lastCesion,
                    "<TmstCesion>EEEE</TmstCesion>",
                        $"<TmstCesion>{this._tmstActual}</TmstCesion>",
                            RegexOptions.Singleline);

                #endregion

                #region ACTUALICE LA ULTIMA CESION

                ////
                //// Recuperar la ultima cesion (template)
                string currentlastCesion = template.HefGetLastCesion();
                template = Regex.Replace(
                    template,
                        currentlastCesion,
                            lastCesion,
                                RegexOptions.Singleline);


                #endregion

                #region FIRMAR EL DOCUMENTO COMPLETO

                ////
                //// Firme el documento completo
                HefRespuesta respSignature = FirmarDocumentoAECCompleto(template);
                if (!respSignature.EsCorrecto)
                    throw new Exception($"No fue posible firmar el documento completo AEC. " +
                        $"'{respSignature.Detalle}'");

                ////
                //// Recupere el documento firmado
                XmlDocument xAEC = new XmlDocument();
                xAEC.PreserveWhitespace = true;
                xAEC.LoadXml(respSignature.Resultado as string);

                #endregion

                #region ENVIAR AEC AL SII

                ////
                //// Recupere al ultima cesion del documento
                lastCesion = xAEC.OuterXml.HefGetLastCesion();
                string eMail = Regex.Match(
                    lastCesion,
                        "Cedente.*?eMail>(.*?)<",
                            RegexOptions.Singleline).Groups[1].Value;
                string rutCedente = Regex.Match(
                    lastCesion,
                        "Cedente.*?RUT>(.*?)<",
                            RegexOptions.Singleline).Groups[1].Value;
                string nombreArchivo = $"HefestoCesion_R" +
                    $"{rutCedente.Replace("-", "")}" +
                        $"_F{DateTime.Now.ToString("yyyyMMddHHmmss")}.xml";


                ////
                //// Envíe al SII el documento AEC
                resp = Dal
                    .HefPublicacion
                        .PublicarDocumentoProduccion(
                            xAEC.OuterXml,
                                this._token,
                                    eMail,
                                        rutCedente,
                                            nombreArchivo);

                ////
                //// Actualice el valor de la respuesta
                if (resp.EsCorrecto)
                    resp.Resultado = ((string)resp.Resultado).HefGetBase64();

                ////
                //// Test
                //// Guardar el documento en disco
                if (resp.EsCorrecto)
                    File.WriteAllText($"Repositorio/HefReCesion{resp.Trackid}.xml", xAEC.OuterXml, Encoding.GetEncoding("ISO-8859-1"));


                #endregion

            }
            catch (Exception err)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.Resultado = null;

            }

            ////
            //// Regrese el valor de retorno
            return resp;

        }

        /// <summary>
        /// Permite firmar el documento aec completo
        /// </summary>
        /// <remarks>
        /// Metodo poco ortodoxo pero efectivo, es necesario investigar para realizar esta proceso 
        /// más fluido.
        /// </remarks>
        private HefRespuesta FirmarDocumentoAECCompleto(string template)
        {
            ////
            //// Inicie la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "Firmar documento completo (AEC)";

            ////
            //// Inicie el proceso
            try
            {
                ////
                //// Cargar documento 
                XmlDocument xAEC = new XmlDocument();
                xAEC.PreserveWhitespace = true;
                xAEC.LoadXml(template);

                ////
                //// Recuperar el nodo a firmar
                //// con este metodo heradamos el namespace del documento
                XmlElement xDTECedido = (XmlElement)xAEC.DocumentElement.GetElementsByTagName("DTECedido")[0];
                HefRespuesta firma1 = Extensiones.HefSignature.HefFirmar(xDTECedido.OuterXml, this._certificado);

                ////
                //// Recuperar el nodo a firmar
                //// Siempre es el nodo cesion sin firmar ( generalmente el ultimo )
                XmlElement xLastCesion = xAEC.DocumentElement.GetElementsByTagName("Cesion")
                    .Cast<XmlElement>()
                        .LastOrDefault();

                ////
                //// Firme el nodo
                HefRespuesta firma2 = Extensiones.HefSignature.HefFirmar(xLastCesion.OuterXml, this._certificado);

                ////
                //// Actualice el nodo DTECedido solo sí el documento actual representa la primera cesión
                //// de lo contrario no es necesario actualizar el nodo pues debe ir el nodo original de la 
                //// primer cesión.
                if (template.HefGetCountCesiones() == 1)
                    template = Regex.Replace(
                        template,
                            "<DTECedido.*?<\\/DTECedido>",
                                firma1.Resultado as string,
                                    RegexOptions.Singleline);

                ////
                //// Actualice la ultima cesion del documento actual
                template = Regex.Replace(
                    template,
                        template.HefGetLastCesion(),
                            firma2.Resultado as string,
                                RegexOptions.Singleline);

                ////
                //// Cargue nuevamente el documento para asignar correctamente las firmas anteriores.
                xAEC = new XmlDocument();
                xAEC.PreserveWhitespace = true;
                xAEC.LoadXml(template);

                ////
                //// Firme el documento completo
                HefRespuesta firma3 = Extensiones.HefSignature.HefFirmar(xAEC.OuterXml, this._certificado);

                ////
                //// firme el documento completo
                xAEC = new XmlDocument();
                xAEC.PreserveWhitespace = true;
                xAEC.LoadXml(firma3.Resultado as string);

                ////
                //// regrese la respuesta
                resp.EsCorrecto = true;
                resp.Detalle = "Documento firmado correctamente";
                resp.Resultado = xAEC.OuterXml;


            }
            catch (Exception err)
            {
                ////
                //// Notficar el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;

                
            }

            ////
            //// regrese el valor de retorno
            return resp;
        
        }

        #endregion

    }

}