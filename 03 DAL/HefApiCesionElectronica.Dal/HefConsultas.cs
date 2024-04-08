using HefModCesiones;
using Microsoft.VisualBasic;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace HefApiCesionElectronica.Dal
{
    /// <summary>
    /// Consultas al SII
    /// </summary>
    public class HefConsultas
    {

        /// <summary>
        /// Recupera los datos del contribuyente usando las credenciales de la empresa
        /// </summary>
        /// <param name="credenciales">Credenciales para la consulta</param>
        /// <remarks>
        /// semilla    : https://misiir.sii.cl/cgi_misii/siihome.cgi
        /// </remarks>
        /// <returns>
        /// Documento json con los datos del contribuyente en formato string
        /// </returns>
        public static HefRespuesta SII_Datos_Generales(string credenciales)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "Dal/SII_Datos_Generales";

            ////
            //// Variables
            string data = string.Empty;

            ////
            //// Identifique la url donde recuperar la información
            string uriSIITarget = "https://misiir.sii.cl/cgi_misii/siihome.cgi";

            ////
            //// Inicie el proceso
            try
            {

                ////
                //// Genera la consulta al SII
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uriSIITarget);
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
                req.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                req.Headers.Add("Accept-Language", "es-419,es;q=0.9,es-ES;q=0.8,en;q=0.7,en-GB;q=0.6,en-US;q=0.5");
                req.Headers.Add("Cookie", credenciales);
                req.Host = "misiir.sii.cl";
                req.Referer = "https://herculesr.sii.cl/";
                req.Headers.Add("Sec-Ch-Ua", "\"Microsoft Edge\";v=\"123\", \"Not:A-Brand\";v=\"8\", \"Chromium\";v=\"123\"");
                req.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
                req.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                req.Headers.Add("Sec-Fetch-Dest", "document");
                req.Headers.Add("Sec-Fetch-Mode", "navigate");
                req.Headers.Add("Sec-Fetch-Site", "same-site");
                req.Headers.Add("Sec-Fetch-User", "?1");
                req.Headers.Add("Upgrade-Insecure-Requests", "1");
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36 Edg/123.0.0.0";
                req.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
                {
                    ////
                    //// Si no hay respuesta del servidor
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(
                            $"El SII no ha contestado nuestra solicitud. status code : {response.StatusCode.ToString()}");

                    ////
                    //// recupere la respuesta del SII
                    using (Stream receiveStream = response.GetResponseStream())
                    {
                        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.GetEncoding("ISO-8859-1")))
                        {
                            data = readStream.ReadToEnd();
                        }

                    }

                }

                ////
                //// Complete la respuesta
                resp.EsCorrecto = true;
                resp.Detalle = "Proceso ejecutado OK";
                resp.Resultado = data;

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
        /// Recupera los datos del certificado
        /// </summary>
        /// <returns></returns>
        public static HefRespuesta RecuperarDatosCertificado(string credenciales)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "MyMethod";

            ////
            //// Inicie el proceso
            try
            {





                ////
                //// Complete la respuesta
                resp.EsCorrecto = true;
                resp.Detalle = "Proceso ejecutado OK";
                resp.Resultado = null;

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
        /// Consulta al SII por el esatdo de un envio al SII (trackid)
        /// </summary>
        /// <param name="certificado"></param>
        /// <returns></returns>
        public static HefRespuesta RecuperarEstadoTrackid(string token, string rutEmpresa, long trackid)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "RecuperarTokenSiiDte";

            ////
            //// Protocolos se transmision
            System.Net.ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11 |
                        SecurityProtocolType.Tls;

            ////
            //// Variables
            string respuesta = string.Empty;

            ////
            //// Inicie el proceso
            try
            {

               

                #region RECUPERAR TOKEN DESDE EL SII

                ////
                //// Recupere la semilla desde el SII
                //// string urlSemilla = "https://maullin.sii.cl/DTEWS/services/wsRPETCConsulta";
                string urlSemilla = "https://palena.sii.cl/DTEWS/services/wsRPETCConsulta";

                ////
                //// Consulte al SII
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(urlSemilla);
                req.Method = "POST";
                req.Headers.Add("Accept-Encoding", "gzip,deflate");
                req.ContentType = "text/xml;charset=UTF-8";
                req.Headers.Add("SOAPAction", "");
                req.Host = "palena.sii.cl";
                req.UserAgent = "Apache-HttpClient/4.5.5(Java/16.0.1)";

                ////
                //// Escriba la consulta ( POST ) y su largo en bytes
                string postData = $@"
                <soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:def=""http://DefaultNamespace"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <def:getEstEnvio soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                         <Token xsi:type=""xsd:string"">{token}</Token>
                         <TrackId xsi:type=""xsd:string"">{trackid}</TrackId>
                      </def:getEstEnvio>
                   </soapenv:Body>
                </soapenv:Envelope>";


                byte[] postBytes = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = postBytes.Length;

                ////
                //// Escriba los bytes en el request stream
                using (Stream postStream = req.GetRequestStream())
                {
                    postStream.Write(postBytes, 0, postBytes.Length);
                    postStream.Flush();
                    postStream.Close();
                }

                ////
                //// Recupere la respuesta de la consulta ( response )
                using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
                {
                    ////
                    //// Recuperar el token
                    if (response.StatusCode == HttpStatusCode.OK)
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            respuesta = reader.ReadToEnd();
                        }

                }

                ////
                //// Compruebe la semilla
                Match mRespuesta = Regex.Match(
                    respuesta, 
                        "getEstEnvioReturn.*?>(.*?)<", 
                            RegexOptions.Singleline);

                ////
                //// Valide la semilla
                if (!mRespuesta.Success)
                    throw new Exception($"El servidor del SII no regreso ningun estado. Respuesta = '{respuesta}'");

                ////
                //// Recupere la respuesta xml de la semilla
                respuesta = WebUtility.HtmlDecode(mRespuesta.Groups[1].Value);

                #endregion

                ////
                //// Complete la respuesta
                resp.EsCorrecto = true;
                resp.Detalle = "Recuperción respuesta OK";
                resp.Trackid = trackid.ToString();
                resp.Resultado = respuesta;

            }
            catch (Exception err)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.CodigoSII = null;
                resp.Trackid = trackid.ToString();
                resp.Resultado = null;

            }

            ////
            //// Regrese el valor de retorno
            return resp;

        }


        /// <summary>
        /// Consulta al SII por el estado de una cesión
        /// </summary>
        /// <param name="certificado"></param>
        /// <returns></returns>
        public static HefRespuesta RecuperarEstadoCesion(string token, string rutEmpresa, string tipoDoc, string folioDoc, string idCesion="")
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "RecuperarEstadoCesion";

            ////
            //// Protocolos se transmision
            System.Net.ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11 |
                        SecurityProtocolType.Tls;

            ////
            //// Variables
            string respuesta = string.Empty;

            ////
            //// Inicie el proceso
            try
            {

                #region RECUPERAR TOKEN DESDE EL SII

                ////
                //// Recupere la semilla desde el SII
                string urlSemilla = "https://palena.sii.cl/DTEWS/services/wsRPETCConsulta";

                ////
                //// Consulte al SII
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(urlSemilla);
                req.Method = "POST";
                req.Headers.Add("Accept-Encoding", "gzip,deflate");
                req.ContentType = "text/xml;charset=UTF-8";
                req.Headers.Add("SOAPAction", "");
                req.Host = "palena.sii.cl";
                req.UserAgent = "Apache-HttpClient/4.5.5(Java/16.0.1)";

                ////
                //// Escriba la consulta ( POST ) y su largo en bytes
                string postData = $@"
                <soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:def=""http://DefaultNamespace"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <def:getEstCesion soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                         <Token xsi:type=""xsd:string"">{token}</Token>
                         <RutEmisor xsi:type=""xsd:string"">{rutEmpresa.Split('-')[0]}</RutEmisor>
                         <DVEmisor xsi:type=""xsd:string"">{rutEmpresa.Split('-')[1]}</DVEmisor>
                         <TipoDoc xsi:type=""xsd:string"">{tipoDoc}</TipoDoc>
                         <FolioDoc xsi:type=""xsd:string"">{folioDoc}</FolioDoc>
                         <IdCesion xsi:type=""xsd:string""></IdCesion>
                      </def:getEstCesion>
                   </soapenv:Body>
                </soapenv:Envelope>";


                byte[] postBytes = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = postBytes.Length;

                ////
                //// Escriba los bytes en el request stream
                using (Stream postStream = req.GetRequestStream())
                {
                    postStream.Write(postBytes, 0, postBytes.Length);
                    postStream.Flush();
                    postStream.Close();
                }

                ////
                //// Recupere la respuesta de la consulta ( response )
                using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
                {
                    ////
                    //// Recuperar el token
                    if (response.StatusCode == HttpStatusCode.OK)
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            respuesta = reader.ReadToEnd();
                        }

                }

                ////
                //// Compruebe la semilla
                Match mRespuesta = Regex.Match(
                    respuesta,
                        "getEstCesionReturn.*?>(.*?)<",
                            RegexOptions.Singleline);

                ////
                //// Valide la semilla
                if (!mRespuesta.Success)
                    throw new Exception($"El servidor del SII no regreso ningun estado. Respuesta = '{respuesta}'");

                ////
                //// Recupere la respuesta xml de la semilla
                respuesta = WebUtility.HtmlDecode(mRespuesta.Groups[1].Value);

                #endregion

                ////
                //// Complete la respuesta
                resp.EsCorrecto = true;
                resp.Detalle = "Recuperción respuesta OK";
                resp.Resultado = respuesta;

            }
            catch (Exception err)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.CodigoSII = "-1";
                resp.Resultado = null;

            }

            ////
            //// Regrese el valor de retorno
            return resp;

        }


    }

}