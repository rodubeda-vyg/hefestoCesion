using Microsoft.AspNetCore.Mvc;
using HefModCesiones;
using System.Text;
using HefApiCesionElectronica.Neg;
using System.Security.Cryptography.X509Certificates;

namespace HefApiCesionElectronica.Controllers
{
    [ApiController]
    [Route("/")]
    public class CesionController : ControllerBase
    {

        /// <summary>
        /// Objetos a inyectar
        /// </summary>
        private readonly IHefCeder _HefCeder;

        /// <summary>
        /// Constructor de la clase
        /// </summary>
        /// <param name="HefCeder"></param>
        public CesionController(IHefCeder HefCeder)
        {
            _HefCeder = HefCeder;
        }

        /// <summary>
        /// Permite ceder documento
        /// </summary>
        [HttpPost]
        [Route("Hefesto/Api/CederDocumento")]
        public ActionResult Ceder(HefConsultaCesionDTO consulta)
        {

            ////
            //// Inciie la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "Ceder";

            ////
            //// Inicie el proceso
            try
            {

                ////
                //// Inicie el proceso de cesion del documento
                resp = _HefCeder.GenerarCesionElectronica(consulta);

            }
            catch (Exception err)
            {
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.Resultado = null;
                
            }

            ////
            //// Regrese el valor de retorno
            return Ok(resp);

        }

        /// <summary>
        /// Permite consultar el trackid de la cesion electrónica
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Hefesto/Api/ConsultarTrackid")]
        public ActionResult ConsultarTrackid(HefConsultaTrackidDTO consulta)
        {

            ////
            //// Inciie la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "ConsultarTrackid";

            ////
            //// Inicie el proceso
            try
            {

                ////
                //// Inicie el proceso de cesion del documento
                resp = _HefCeder.ConsultarTrackid(consulta);

            }
            catch (Exception err)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.Trackid = consulta.Trackid.ToString();
                resp.Resultado = null;

                ////
                //// regrese el error
                return BadRequest(resp);

            }

            ////
            //// Regrese el valor de retorno
            return Ok(resp);

        }

        /// <summary>
        /// Permite consultar el trackid de la cesion electrónica
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Hefesto/Api/ConsultarEstadoCesion")]
        public ActionResult ConsultarEstadoCesion(HefConsultaEstadoCesionDTO consulta)
        {

            ////
            //// Inciie la respuesta
            HefRespuesta resp = new HefRespuesta();
            resp.Mensaje = "ConsultarEstadoCesion";

            ////
            //// Inicie el proceso
            try
            {

                ////
                //// Inicie el proceso de cesion del documento
                resp = _HefCeder.ConsultarEstadoCesion(consulta);

            }
            catch (Exception err)
            {
                ////
                //// Notifique el error
                resp.EsCorrecto = false;
                resp.Detalle = err.Message;
                resp.Resultado = null;

                ////
                //// regrese el error
                return BadRequest(resp);

            }

            ////
            //// Regrese el valor de retorno
            return Ok(resp);

        }


    }

}