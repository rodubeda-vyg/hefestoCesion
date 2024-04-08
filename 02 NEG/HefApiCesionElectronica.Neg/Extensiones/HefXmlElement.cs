using HefModCesiones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HefApiCesionElectronica.Neg
{
    /// <summary>
    /// Permite firmar un elemento del documento xml
    /// </summary>
    public static class HefXmlElementExtension
    {

        /// <summary>
        /// Permite firmar el xmlelement actual
        /// </summary>
        /// <param name="elemento"></param>
        /// <param name="certificado"></param>
        public static HefRespuesta HefFirmarFirst(this XmlElement elemento, X509Certificate2 certificado)
        {
            ////
            //// Cree la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "firmar documento";

            ////
            //// Inicie el proceso de firma
            try
            {

                #region RECUPERE EL NODO A FIRMAR

                ////
                //// Recupere el elemento a firmar
                string referenciaUri = $"#" + elemento.
                    ChildNodes.Cast<XmlNode>()
                        .ToList().FirstOrDefault
                            (p => p.NodeType == XmlNodeType.Element)
                                .Attributes["ID"].Value;

                #endregion

                #region GENERAR LA FIRMA

                // Create a SignedXml object.
                SignedXml signedXml = new SignedXml(elemento);
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

                ////
                //// Encuentre el signature vacio que debe ser completado
                XmlElement SignatureOld = elemento
                    .GetElementsByTagName("Signature")
                        .Cast<XmlElement>()
                            .FirstOrDefault(p => p.HasChildNodes == false);
                ////
                //// Reemplace el nodo
                SignatureOld.ParentNode.ReplaceChild(
                    elemento.OwnerDocument.ImportNode(xmlDigitalSignature,true)
                        , SignatureOld);

                #endregion

                ////
                //// Regrese el resultado
                resp.EsCorrecto = true;
                resp.Detalle = $"Nodo '{elemento.Name}' firmado correctamente.";
                resp.Resultado = elemento;

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

        /// <summary>
        /// Recupera el primer elemento hijo de un xmlelement
        /// </summary>
        /// <param name="elemento"></param>
        /// <returns></returns>
        public static XmlElement HefGetFirstElementChild(this XmlElement elemento)
        {
            ////
            //// Recupere el elemento a firmar
            return (XmlElement)elemento.
                ChildNodes.Cast<XmlNode>()
                    .ToList().FirstOrDefault
                        (p => p.NodeType == XmlNodeType.Element);

        }

    }

}
