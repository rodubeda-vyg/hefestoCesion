using HefModCesiones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Security.Cryptography.Xml;
using System.Reflection.PortableExecutable;

namespace HefApiCesionElectronica.Dal
{
    public class HefPublicacion
    {

        /// <summary>
        /// Publica el documento aec en el SII
        /// </summary>
        /// <returns></returns>
        public static HefRespuesta PublicarDocumentoProduccion(string xAec, string credencial, string pEmail, string pRutCompany, string pFileName)
        {
            ////
            //// Constantes 
            const string URL_PRODUCCION = "https://palena.sii.cl/cgi_rtc/RTC/RTCAnotEnvio.cgi";

            ////
            //// Iniciar la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.CodigoSII = null;
            resp.Trackid = null;
            resp.Mensaje = "PublicarDocumento en SII (Producción)";

            ////
            //// Iniciar el proceso
            try
            {


                ////
                //// Cree el archivo rtf para transmitir el documento AEC
                //// 
                
                string pUrl = URL_PRODUCCION;
                string pTokenCertificacion = credencial;
                string pDvCompany = pRutCompany.Split('-')[1];
                pRutCompany = pRutCompany.Split('-')[0];

                ////
                //// Cree el contenido del envio
                string RFC = string.Empty;
                RFC += "--7d23e2a11301c4\r\n";
                RFC += "Content-Disposition: form-data; name=\"emailNotif\"\r\n";
                RFC += "\r\n";
                RFC += pEmail + "\r\n";
                RFC += "--7d23e2a11301c4\r\n";
                RFC += "Content-Disposition: form-data; name=\"rutCompany\"\r\n";
                RFC += "\r\n";
                RFC += pRutCompany + "\r\n";
                RFC += "--7d23e2a11301c4\r\n";
                RFC += "Content-Disposition: form-data; name=\"dvCompany\"\r\n";
                RFC += "\r\n";
                RFC += pDvCompany + "\r\n";
                RFC += "--7d23e2a11301c4\r\n";
                RFC += "Content-Disposition: form-data; name=\"archivo\"; filename=\"" + pFileName + "\"\r\n";
                RFC += "Content-Type: text/xml\r\n";
                RFC += "\r\n";

                ////
                //// Escriba el final de la secuencia
                RFC += xAec;
                RFC += "\r\n";
                RFC += "--7d23e2a11301c4--\r\n";

                ////
                //// Cree los parametros del header.
                string pMethod = "POST";
                string pAccept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/vnd.ms-powerpoint, application/ms-excel, application/msword, */*";
                string pReferer = "www.hefestosDte.cl";
                string pToken = "TOKEN={0}";

                ////
                //// Cree un nuevo request para iniciar el proceso
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pUrl);
                request.Method = pMethod;
                request.Accept = pAccept;
                request.Referer = pReferer;

                ////
                //// Agregar el content-type
                request.ContentType = "multipart/form-data: boundary=7d23e2a11301c4";
                request.ContentLength = RFC.Length;

                ////
                //// Defina manualmente los headers del request
                request.Headers.Add("Accept-Language", "es-cl");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                request.Headers.Add("Cache-Control", "no-cache");
                request.Headers.Add("Cookie", string.Format(pToken, pTokenCertificacion));

                ////
                //// Defina el user agent
                request.UserAgent = "Mozilla/4.0 (compatible; PROG 1.0; Windows NT 5.0; YComp 5.0.2.4)";
                request.KeepAlive = true;

                ////
                //// Recupere el streamwriter para escribir la secuencia
                using (StreamWriter sw = new StreamWriter(request.GetRequestStream(), Encoding.GetEncoding("ISO-8859-1")))
                {
                    sw.Write(RFC);
                }

                ////
                //// Defina donde depositar el resultado
                string respuestaSii = string.Empty;

                ////
                //// Recupere la respuesta del sii
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        respuestaSii = sr.ReadToEnd().Trim();
                    }

                }

                ////
                //// Hay respuesta?
                if (string.IsNullOrEmpty(respuestaSii))
                    throw new ArgumentNullException("Respuesta del SII es null");

                ////
                //// Recupere el status del proceso
                Match m_status = Regex.Match(respuestaSii, "<STATUS>(.*?)</STATUS>", RegexOptions.Singleline);
                if (!m_status.Success)
                    throw new Exception("El SII no respondío al envio del documento AEC.\r\n" + respuestaSii);

                ////
                //// Interprete la respuesta del SII
                switch (m_status.Groups[1].Value)
                {
                    case "0":
                        ////
                        //// Recuoere el trackid de la operación
                        resp.EsCorrecto = true;
                        resp.Detalle = "Documento AEC Publicado en el SII. Envío recibido OK (SII).";
                        resp.CodigoSII = "0";
                        resp.Trackid = Regex.Match(respuestaSii, "<TRACKID>(.*?)</TRACKID>", RegexOptions.Singleline).Groups[1].Value;
                        resp.Resultado = xAec;
                        break;
                    case "1":
                        resp.EsCorrecto = false;
                        resp.Detalle = "Rut usuario autenticado no tiene permiso para enviar en empresa Cedente.";
                        resp.CodigoSII = "1";
                        resp.Trackid = null;
                        resp.Resultado = null;
                        break;
                    case "2":
                        resp.EsCorrecto = false;
                        resp.Detalle = "Error en tamaño del archivo enviado.";
                        resp.CodigoSII = "2";
                        resp.Trackid = null;
                        resp.Resultado = null;
                        break;
                    case "4":
                        resp.EsCorrecto = false;
                        resp.Detalle = "Faltan parámetros de entrada.";
                        resp.CodigoSII = "4";
                        resp.Trackid = null;
                        resp.Resultado = null;
                        break;
                    case "5":
                        resp.EsCorrecto = false;
                        resp.Detalle = "Error de autenticación, TOKEN inválido, no existe o está expirado.";
                        resp.CodigoSII = "5";
                        resp.Trackid = null;
                        resp.Resultado = null;
                        break;
                    case "6":
                        resp.EsCorrecto = false;
                        resp.Detalle = "Empresa no es DTE.";
                        resp.CodigoSII = "6";
                        resp.Trackid = null;
                        resp.Resultado = null;
                        break;
                    case "9":
                        resp.EsCorrecto = false;
                        resp.Detalle = "Error Interno.";
                        resp.CodigoSII = "9";
                        resp.Trackid = null;
                        resp.Resultado = null;
                        break;
                    default:
                        resp.EsCorrecto = false;
                        resp.Detalle = "Error Interno.";
                        resp.CodigoSII = "9";
                        resp.Trackid = null;
                        resp.Resultado = null;
                        break;

                }

                ////
                //// Normalice la respuesta
                if (resp.CodigoSII != "0")
                {
                    resp.Detalle = "No fue posible publicar documento AEC. Descripción SII: " + resp.Detalle;
                    resp.Resultado = respuestaSii;

                }



            }
            catch (Exception det)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = det.Message;
                resp.Resultado = null;

            }

            ////
            //// Regrese el valor de retorno
            return resp;

        }



        /// <summary>
        /// Firma la semilla
        /// </summary>
        /// <returns></returns>
        public static HefRespuesta firmarSemilla(string semilla , X509Certificate2 certificado)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.EsCorrecto = false;
            resp.Mensaje = "firmarSemilla";

            ////
            //// Inicie el proceso
            try
            {


                XmlDocument xSemilla = new XmlDocument();
                xSemilla.LoadXml(semilla);

                // Create a SignedXml object.
                SignedXml signedXml = new SignedXml(xSemilla);
                signedXml.Signature.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";

                // Add the key to the SignedXml document.  'key'
                signedXml.SigningKey = certificado.PrivateKey;

                // Get the signature object from the SignedXml object.
                Signature XMLSignature = signedXml.Signature;

                // Create a reference to be signed.  Pass "" 
                // to specify that all of the current XML
                // document should be signed.
                Reference reference = new Reference();
                ///reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1"; // Algoritmo SHA-1
                reference.Uri = "";

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

                // Compute the signature.
                signedXml.ComputeSignature();

                // Get the XML representation of the signature and save
                // it to an XmlElement object.
                XmlElement xmlDigitalSignature = signedXml.GetXml();


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

    }

}
