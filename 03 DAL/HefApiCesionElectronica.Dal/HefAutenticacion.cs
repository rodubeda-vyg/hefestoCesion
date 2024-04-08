using HefModCesiones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace HefApiCesionElectronica.Dal
{
    /// <summary>
    /// Metodos relacionados con la autenticación del SII
    /// </summary>
    public class HefAutenticacion
    {

        /// <summary>
        /// Recupera las cookies del proceso de autenticacion
        /// </summary>
        public static HefRespuesta GetCredenciales(X509Certificate2 certificado)
        {
            ////
            //// Cree la entidad para recuperar la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "GetToken";
            resp.Detalle = "Recupera el token desde el SII";

            ////
            //// Protocolos se transmision
            System.Net.ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11 |
                        SecurityProtocolType.Tls;

            ////
            //// Target donde apunta la autenticación del SII
            string uriSIITarget = "https://herculesr.sii.cl/cgi_AUT2000/CAutInicio.cgi?";
            uriSIITarget += "https://misiir.sii.cl/cgi_misii/siihome.cgi";

            ////
            //// Inicie el proceso
            try
            {

                ////
                //// Consulta al SII para autenticar al cliente actual ( certificado )
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uriSIITarget);
                req.PreAuthenticate = true;
                req.AllowAutoRedirect = true;
                req.ClientCertificates.Add(certificado);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";

                ////
                //// Escriba la consulta ( POST ) y su largo en bytes
                string postData = "referencia=https%3A%2F%2Fmisiir.sii.cl%2Fcgi_misii%2Fsiihome.cgi";
                byte[] postBytes = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = postBytes.Length;

                ////
                //// Escriba los bytes en el request stream
                Stream postStream = req.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Flush();
                postStream.Close();

                ////
                //// Recupere la respuesta de la consulta ( response )
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();

                ////
                //// Recupere la respuesta del SII
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception("No fue posible comunicarse con el servidor remoto.");

                ////
                //// Recupere las cookies generadas por el SII
                bool HasCookies = response.Headers.AllKeys.Contains("Set-Cookie");
                if (!HasCookies)
                    throw new Exception("La consulta actual no regresó cookies.");

                ////
                //// Recupere las cookies del proceso
                string cookies = response.Headers["Set-Cookie"];
                if (string.IsNullOrEmpty(cookies))
                    throw new Exception("La consulta actual no regresó ninguna cookies. ( cookies= null )");

                ////
                //// Cree el arreglo de cookies
                string sCookies = string.Join("; ", cookies.Split(',')
                    .Cast<string>().Select(p=> p.Split(';')[0].Trim() ).ToArray());

                ////
                //// Complete la respuesta del proceso
                resp.EsCorrecto = true;
                resp.Resultado = sCookies;

            }
            catch (Exception Ex)
            {
                resp.EsCorrecto = false;
                resp.Mensaje = "No fue posible realizar conección al SII.\r\n" + Ex.Message;
            }

            ////
            //// Regrese el valor de retorno
            return resp;

        }


        /// <summary>
        /// Recupera el token de producción para Dtes
        /// </summary>
        public static HefRespuesta GetToken(X509Certificate2 certificado)
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
            string semilla = string.Empty;

            ////
            //// Inicie el proceso
            try
            {

                #region RECUPERAR SEMILLA DESDE EL SII

                ////
                //// Recupere la semilla desde el SII
                string urlSemilla = "https://palena.sii.cl/DTEWS/CrSeed.jws";

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
                string postData = @$"<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:def=""http://DefaultNamespace"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <def:getSeed soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/""/>
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
                            semilla = reader.ReadToEnd();
                        }

                }

                ////
                //// recupere la información de la respuesta
                Match mSemillaRespuesta = Regex.Match(
                    semilla,
                        "getSeedReturn.*?>(.*?)<",
                            RegexOptions.Singleline);

                ////
                //// Valide la semilla
                if (!mSemillaRespuesta.Success)
                    throw new Exception($"El servidor del SII no regreso ninguna semilla. Respuesta = '{semilla}'");

                ////
                //// Recupere la respuesta xml de la semilla
                semilla = WebUtility.HtmlDecode(mSemillaRespuesta.Groups[1].Value);

                ////
                //// Recupere la semilla
                Match mSemilla = Regex.Match(semilla, "<SEMILLA>(.*?)<\\/SEMILLA>", RegexOptions.Singleline);
                if (!mSemilla.Success)
                    throw new Exception($"El servidor del SII no regreso ninguna semilla. Respuesta = '{semilla}'");

                ////
                //// recupere la semilla
                semilla = mSemilla.Groups[1].Value;


                #endregion

                #region CREAR SOBRE CON SEMILLA FIRMADA

                ////
                //// Firme la semilla
                semilla = FirmarDocumentoSemilla(semilla, certificado);

                #endregion

                #region RECUPERAR TOKEN DESDE EL SII

                ////
                //// Recupere la semilla desde el SII
                urlSemilla = "https://palena.sii.cl/DTEWS/GetTokenFromSeed.jws";

                ////
                //// Consulte al SII
                req = (HttpWebRequest)WebRequest.Create(urlSemilla);
                req.Method = "POST";
                req.Headers.Add("Accept-Encoding", "gzip,deflate");
                req.ContentType = "text/xml;charset=UTF-8";
                req.Headers.Add("SOAPAction", "");
                req.Host = "palena.sii.cl";
                req.UserAgent = "Apache-HttpClient/4.5.5(Java/16.0.1)";

                ////
                //// Escriba la consulta ( POST ) y su largo en bytes
                postData = @$"<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:def=""http://DefaultNamespace"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <def:getToken soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                         <pszXml xsi:type=""xsd:string""><![CDATA[{semilla}]]></pszXml>
                      </def:getToken>
                   </soapenv:Body>
                </soapenv:Envelope>";

                postBytes = Encoding.UTF8.GetBytes(postData);
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
                            semilla = reader.ReadToEnd();
                        }

                }

                ////
                //// Compruebe la semilla
                Match mTokenRespuesta = Regex.Match(semilla, "getTokenReturn.*?>(.*?)<", RegexOptions.Singleline);

                ////
                //// Valide la semilla
                if (!mTokenRespuesta.Success)
                    throw new Exception($"El servidor del SII no regreso ninguna token. Respuesta = '{semilla}'");

                ////
                //// Recupere la respuesta xml de la semilla
                semilla = WebUtility.HtmlDecode(mTokenRespuesta.Groups[1].Value);

                ////
                //// Recupere la semilla
                Match mToken = Regex.Match(semilla, "<TOKEN>(.*?)<\\/TOKEN>", RegexOptions.Singleline);
                if (!mToken.Success)
                    throw new Exception($"El servidor del SII no regreso ninguna token. Respuesta = '{semilla}'");

                #endregion

                ////
                //// Complete la respuesta
                resp.EsCorrecto = true;
                resp.Detalle = "Proceso ejecutado OK";
                resp.Resultado = mToken.Groups[1].Value;

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
        /// Firma la semilla para ser enviada al SII
        /// </summary>
        /// <param name="documento"></param>system.
        /// <param name="certificado"></param>
        /// <returns></returns>
        internal static string FirmarDocumentoSemilla(string semilla, X509Certificate2 certificado)
        {
            ////
            //// Construya el sobre para colocar la semilla
            string body = $"<getToken><item><Semilla>{semilla}</Semilla></item></getToken>";

            ////
            //// Cree un nuevo documento xml y defina sus caracteristicas
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;
            doc.LoadXml(body);

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(doc);

            // Add the key to the SignedXml document.  'key'
            signedXml.SigningKey = certificado.PrivateKey;

            // Get the signature object from the SignedXml object.
            Signature XMLSignature = signedXml.Signature;

            // Create a reference to be signed.  Pass "" 
            // to specify that all of the current XML
            // document should be signed.
            Reference reference = new Reference("");

            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";

            // Add the Reference object to the Signature object.
            XMLSignature.SignedInfo.AddReference(reference);

            // Add an RSAKeyValue KeyInfo (optional; helps recipient find key to validate).
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new RSAKeyValue((RSA)certificado.PrivateKey));


            ////
            //// Agregar información del certificado x509
            //// X509Certificate MSCert = X509Certificate.CreateFromCertFile(Certificate);
            keyInfo.AddClause(new KeyInfoX509Data(certificado));


            // Add the KeyInfo object to the Reference object.
            XMLSignature.KeyInfo = keyInfo;


            ////
            //// ASignar el metodo de firma
            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";


            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));


            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }

            // Save the signed XML document to a file specified
            // using the passed string.
            //XmlTextWriter xmltw = new XmlTextWriter(@"d:\ResultadoFirma.xml", new UTF8Encoding(false));
            //doc.WriteTo(xmltw);
            //xmltw.Close();
            return doc.InnerXml;


        }

    }

}
