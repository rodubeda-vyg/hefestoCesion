﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Este código fue generado por una herramienta.
//     Versión de runtime:4.0.30319.42000
//
//     Los cambios en este archivo podrían causar un comportamiento incorrecto y se perderán si
//     se vuelve a generar el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HefApiCesionElectronica.Neg.Properties {
    using System;
    
    
    /// <summary>
    ///   Clase de recurso fuertemente tipado, para buscar cadenas traducidas, etc.
    /// </summary>
    // StronglyTypedResourceBuilder generó automáticamente esta clase
    // a través de una herramienta como ResGen o Visual Studio.
    // Para agregar o quitar un miembro, edite el archivo .ResX y, a continuación, vuelva a ejecutar ResGen
    // con la opción /str o recompile su proyecto de VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Devuelve la instancia de ResourceManager almacenada en caché utilizada por esta clase.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("HefApiCesionElectronica.Neg.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Reemplaza la propiedad CurrentUICulture del subproceso actual para todas las
        ///   búsquedas de recursos mediante esta clase de recurso fuertemente tipado.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Busca una cadena traducida similar a &lt;?xml version=&quot;1.0&quot; encoding=&quot;ISO-8859-1&quot;?&gt;
        ///&lt;AEC version=&quot;1.0&quot; xmlns=&quot;http://www.sii.cl/SiiDte&quot; xmlns:xsi=&quot;http://www.w3.org/2001/XMLSchema-instance&quot; xsi:schemaLocation=&quot;http://www.sii.cl/SiiDte AEC_v10.xsd&quot;&gt;
        ///&lt;!-- HEFESTO API CESION ELECTRÓNICA V1.1 &apos;https://lenguajedemaquinas.blogspot.com/&apos;--&gt;
        ///&lt;DocumentoAEC ID=&quot;AEC&quot;&gt;
        ///&lt;Caratula version=&quot;1.0&quot;&gt;
        ///&lt;RutCedente&gt;AAAA&lt;/RutCedente&gt;
        ///&lt;RutCesionario&gt;AAAA&lt;/RutCesionario&gt;
        ///&lt;NmbContacto&gt;AAAA&lt;/NmbContacto&gt;
        ///&lt;FonoContacto&gt;AAAA&lt;/FonoContacto&gt;
        ///&lt;MailContacto&gt;AAAA&lt;/MailCo [resto de la cadena truncado]&quot;;.
        /// </summary>
        internal static string AEC_Template {
            get {
                return ResourceManager.GetString("AEC_Template", resourceCulture);
            }
        }
    }
}
