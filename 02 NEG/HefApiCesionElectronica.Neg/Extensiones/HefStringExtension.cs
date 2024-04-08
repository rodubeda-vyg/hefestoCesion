using HefModCesiones;
using Newtonsoft.Json;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace HefApiCesionElectronica.Neg
{
    public static class HefStringExtension
    {

        /// <summary>
        /// Recupera el cn de un certificado
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetCertificadoCn(this string str)
        {
            return Regex.Match(str, "CN=(.*?),", RegexOptions.Singleline).Value.Trim();

        }

        /// <summary>
        /// Recupere el nodo indicado
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetXmlNodeValue(this string str, string nodeName)
        {
            return Regex.Match(str, $"<{nodeName}>.*?<\\/{nodeName}>", RegexOptions.Singleline).Value;
        }

        /// <summary>
        /// Recupera el valor de de un nodo xml
        /// </summary>
        /// <param name="str"></param>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public static string HefGetXmlNodeValue2(this string str, string nodeName)
        {
            return Regex.Match(str, $"<{nodeName}>(.*?)<\\/{nodeName}>", RegexOptions.Singleline).Groups[1].Value;
        }

        /// <summary>
        /// Recupere un nodo especifico
        /// </summary>
        /// <param name="str"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string HefGetXmlNode(this string str, string node)
        {
            return Regex.Match(str, node, RegexOptions.Singleline).Value;
        }


        /// <summary>
        /// Recupere todas las cesiones disponibles del documento aec
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetCesiones(this string str)
        {
            return string.Join("\r\n",
                Regex.Matches(
                    str,
                        "<Cesion\\s.*?>.*?\\/Cesion>",
                            RegexOptions.Singleline).Cast<Match>().Select(p=>p.Value).ToArray());
        }


        /// <summary>
        /// Cuenta cuantas cesiones hay actualmente
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int HefGetCountCesiones(this string str)
        {
            return Regex.Matches(str, "<Cesion\\s.*?<\\/Cesion>", RegexOptions.Singleline).Count;
        }

        /// <summary>
        /// Recupere la ultima cesion
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetLastCesion(this string str)
        {
            return Regex.Matches(
                    str,
                        "<Cesion\\s.*?>.*?\\/Cesion>",
                            RegexOptions.Singleline).Cast<Match>().Select(p => p.Value).LastOrDefault();


        }

        /// <summary>
        /// Recupere el documento DTE
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetDTE(this string str)
        {
            return Regex.Match(str, "<DTE\\s.*?<\\/DTE>", RegexOptions.Singleline).Value;
        }

        /// <summary>
        /// Recupera el monto total del documento DTE
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetMntTotalDTE(this string str)
        {
            return Regex.Match(str, "<MntTotal>(.*?)<\\/MntTotal>", RegexOptions.Singleline).Groups[1].Value;
        }

        /// <summary>
        /// Recupera el monto total del documento DTE
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetUltimoVencimientoDTE(this string str)
        {
            string fchEmis = Regex.Match(str, "<FchEmis>(.*?)<\\/FchEmis>", RegexOptions.Singleline).Groups[1].Value;
            DateTime dt;
            if (DateTime.TryParse(fchEmis, out dt))
                return dt.AddMonths(1).ToString("yyyy-MM-dd");
            else
                return DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");

        }

        /// <summary>
        /// Recupera los datos del certificado 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetDatosCertificado(this string credenciales)
        {

            ////
            //// inicie el resultado
            string resultado = string.Empty;

            ////
            //// inicie el proceso
            try
            {
                ////
                //// Recuperar los datos del certificado
                HefRespuesta respDatos = Dal.HefConsultas.SII_Datos_Generales(credenciales);
                if (!respDatos.EsCorrecto)
                    throw new Exception($"No fue posible recuperar el datos básicos del certificado. " +
                        $"'{respDatos.Detalle}'");

                ////
                //// Recupere los datos del contribuyente autenticado en el sii.
                string jsonResult = Regex.Match(
                    respDatos.Resultado as string,
                        "DatosCntrNow = (\\{.*?\\});",
                            RegexOptions.Singleline
                                ).Groups[1].Value;

                ////
                //// Recupere el objeto json
                dynamic jsonDatos = JsonConvert.DeserializeObject(jsonResult);
                if (jsonDatos == null)
                    throw new Exception("No fue posible recuperar los datos del certificado. No se encontró el objeto 'DatosCntrNow'");

                ////
                //// existe el contribuyente
                dynamic jsonContribuyente = jsonDatos.contribuyente;
                if (jsonContribuyente == null)
                    throw new Exception("No fue posible recuperar los datos del certificado. No se encontró el objeto 'jsonDatos.Contribuyente'");

                ////
                //// Constrya la respuesta
                resultado = $"{jsonContribuyente.rut}-{jsonContribuyente.dv}|" +
                    $"{jsonContribuyente.nombres} " +
                        $"{jsonContribuyente.apellidoPaterno} " +
                            $"{jsonContribuyente.apellidoMaterno}";

            }
            catch (Exception err)
            {
                ////
                //// notifique el error
                throw new Exception(err.Message);
            }

            ////
            //// regrese el valor de retorno
            return resultado;

        }

        /// <summary>
        /// Transforma el documento xml a base 64
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetBase64(this string str)
        {

            byte[] bytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(str);
            return Convert.ToBase64String(bytes);

        }

        /// <summary>
        /// Recupere el xml de forma lineal
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HefGetXmllineal(this string str)
        {
            return Regex.Replace(str, ">[\r\n\t\\s]<", "", RegexOptions.Singleline);
        }

        /// <summary>
        /// Recupera el certficado
        /// </summary>
        /// <param name="str"></param>
        /// <param name="heslo"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static X509Certificate2 HefGetCertificate(this string str, string heslo)
        {

            ////
            //// Encoding
            Encoding _encoding = Encoding.GetEncoding("ISO-8859-1");

            ////
            ////Recupere los bytes del certificado
            byte[] bytes = Convert.FromBase64String(str);

            ////
            //// Reconstruya el certificado
            X509Certificate2 certificado = new X509Certificate2(bytes, heslo);

            ////
            //// El certificado tiene private key?
            if (certificado == null)
                throw new Exception($"No fue posible construír el certificado.");

            ////
            //// El certificado tiene private key?
            if (!certificado.HasPrivateKey)
                throw new Exception($"El certficado no tiene private key.");

            ////
            //// El certificado esta expirado?
            DateTime expiracion;
            if (DateTime.TryParse(certificado.GetExpirationDateString(), out expiracion))
                if (DateTime.Now > expiracion)
                    throw new Exception($"El certficado se encuentra expirado.");

            ////
            //// Regrese el certificado
            return certificado;

        }

        /// <summary>
        /// Analiza la respuesta del sii a la consulta de estado de envio al SII
        /// </summary>
        /// <returns></returns>
        public static HefRespuesta HefAnalizarRespuestaEstadoCesion(this string str)
        {

            ////
            //// Inicie la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "Hefesto Respuesta Estado Envío Cesión Trackid";

            ////
            //// Iniciar
            try
            {

                ////
                //// Estado del procesamiento del SII
                string sii_estado = string.Empty;
                string sii_glosa = string.Empty;

                ////
                //// Estado del estado del envio AEC
                string sii_Envio = string.Empty;
                string sii_Trackid = string.Empty;
                string sii_Descr_Estado = string.Empty;

                ////
                //// Recupere los valores
                sii_estado = Regex.Match(
                    str,
                        "<SII:ESTADO>(.*?)</SII:ESTADO>",
                            RegexOptions.Singleline).Groups[1].Value;
                ////
                //// Recupere los valores
                sii_glosa = Regex.Match(
                    str,
                        "<SII:GLOSA>(.*?)</SII:GLOSA>",
                            RegexOptions.Singleline).Groups[1].Value;

                ////
                //// Sino hay valor emita un error
                if (string.IsNullOrEmpty(sii_estado))
                    throw new Exception("La respuesta del Sii a la consulta del estado del envío, no tiene estado.");

                ////
                //// si el estado es diferente de '0' quiere decir que es un error
                if (sii_estado != "0")
                {
                    return new HefRespuesta {

                        EsCorrecto = false,
                        Mensaje = "HefAnalizarRespuestaEstadoCesion",
                        Detalle = sii_glosa,
                        Trackid = "-1",
                        CodigoSII = sii_estado,
                        Resultado = str.HefGetXmllineal().HefGetBase64()

                };
                
                }

                ////
                //// En el caso que sii_estado sea igual a '0'
                //// quiere decir que existe un estado del documento aec enviado.
                sii_Trackid = Regex.Match(
                    str, 
                        "<TRACKID>(.*?)<\\/TRACKID>", 
                            RegexOptions.Singleline).Groups[1].Value;
                sii_Envio = Regex.Match(
                    str,
                        "<ESTADO_ENVIO>(.*?)<\\/ESTADO_ENVIO>",
                            RegexOptions.Singleline).Groups[1].Value;
                sii_Descr_Estado = Regex.Match(
                    str,
                        "<DESC_ESTADO>(.*?)<\\/DESC_ESTADO>",
                            RegexOptions.Singleline).Groups[1].Value;

                ////
                //// Regrese la respuesta
                resp.EsCorrecto = (sii_Envio== "EOK")?true:false;
                resp.Detalle = sii_Descr_Estado;
                resp.Trackid = sii_Trackid;
                resp.CodigoSII = sii_estado;
                resp.Resultado = str.HefGetXmllineal().HefGetBase64();

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
        /// Analiza la respuesta del sii a la consulta de estado de envio al SII
        /// </summary>
        /// <returns></returns>
        public static HefRespuesta HefAnalizarRespuestaEstadoCesion2(this string str)
        {

            ////
            //// Inicie la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "Hefesto Respuesta Estado Cesión";

            ////
            //// Iniciar
            try
            {

                ////
                //// Estado del procesamiento del SII
                string sii_estado = string.Empty;
                string sii_glosa = string.Empty;

                ////
                //// Estado del estado del envio AEC
                string sii_Envio = string.Empty;
                string sii_Trackid = string.Empty;
                string sii_Descr_Estado = string.Empty;

                ////
                //// Recupere los valores
                sii_estado = Regex.Match(
                    str,
                        "<SII:ESTADO>(.*?)</SII:ESTADO>",
                            RegexOptions.Singleline).Groups[1].Value;
                ////
                //// Recupere los valores
                sii_glosa = Regex.Match(
                    str,
                        "<SII:GLOSA>(.*?)</SII:GLOSA>",
                            RegexOptions.Singleline).Groups[1].Value;

                ////
                //// Sino hay valor emita un error
                if (string.IsNullOrEmpty(sii_estado))
                    throw new Exception("La respuesta del Sii a la consulta del estado del envío, no tiene estado.");

                ////
                //// si el estado es diferente de '0' quiere decir que es un error
                if (sii_estado != "0")
                {
                    return new HefRespuesta
                    {

                        EsCorrecto = false,
                        Mensaje = "HefAnalizarRespuestaEstadoCesion",
                        Detalle = sii_glosa,
                        Trackid = "-1",
                        CodigoSII = sii_estado,
                        Resultado = str.HefGetXmllineal().HefGetBase64()

                    };

                }

                ////
                //// Regrese la respuesta
                resp.EsCorrecto = (sii_estado == "0") ? true : false;
                resp.Detalle = sii_glosa;
                resp.Trackid = null;
                resp.CodigoSII = sii_estado;
                resp.Resultado = str.HefGetXmllineal().HefGetBase64();

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





    }

}
