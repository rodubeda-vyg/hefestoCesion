using HefModCesiones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;

namespace HefApiCesionElectronica.Neg.Extensiones
{
    internal class HefSignature
    {
        /// <summary>
        /// Permite firmar el xmlelement actual
        /// </summary>
        /// <param name="elemento"></param>
        /// <param name="certificado"></param>
        public static HefRespuesta HefFirmar(string fragmento, X509Certificate2 certificado)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "Firmar documento";

            ////
            //// Inicie el proceso de firma
            try
            {
                #region CREAR EL ELEMENTO XML

                ////
                //// recuperar el ID
                string referenciaUri = "#" + Regex.Match(
                    fragmento, 
                        "ID=\"(.*?)\"", 
                            RegexOptions.Singleline)
                                .Groups[1].Value;


                XmlDocument xfragmento = new XmlDocument();
                xfragmento.PreserveWhitespace = true;
                xfragmento.LoadXml(fragmento);

                #endregion

                #region GENERAR LA FIRMA

                // Create a SignedXml object.
                SignedXml signedXml = new SignedXml(xfragmento);
                signedXml.Signature.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                signedXml.SigningKey = certificado.PrivateKey;
                Signature XMLSignature = signedXml.Signature;

                ////
                //// referencia
                Reference reference = new Reference();
                reference.Uri = referenciaUri; //referenciaUri; 
                reference.AddTransform(new XmlDsigC14NTransform(false));
                reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";
                signedXml.AddReference(reference);

                ////
                //// keyInfo
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new RSAKeyValue((RSA)certificado.PrivateKey));
                keyInfo.AddClause(new KeyInfoX509Data(certificado));
                XMLSignature.KeyInfo = keyInfo;

                ////
                //// Calcule la firma
                signedXml.ComputeSignature();

                // Get the XML representation of the signature and save
                // it to an XmlElement object.
                XmlElement xmlDigitalSignature = signedXml.GetXml();

                #endregion

                #region AGREGAR LA FIRMA

                xfragmento.DocumentElement.AppendChild(xmlDigitalSignature);

                #endregion

                ////
                //// Regrese el resultado
                resp.EsCorrecto = true;
                resp.Detalle = $"firmado correctamente.";
                resp.Resultado = xfragmento.OuterXml;

            }
            catch (Exception error)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = error.Message;
                resp.Resultado = null;
            }

            ////
            //// Return
            return resp;

        }


    }
}
