using Microsoft.AspNetCore.Mvc;

namespace HefApiCesionElectronica.Controllers
{
    [ApiController]
    [Route("/")]
    public class DefaultCotroller : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="env"></param>
        public DefaultCotroller(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public ActionResult Get()
        {
            
            ////
            //// Donde esta el html default?
            string pathHtmlDefault = Path.Combine(_env.WebRootPath, "Pages\\HefDefault.html");

            ////
            //// Recupere el contenido del archivo
            string htmlContent = System.IO.File.ReadAllText(pathHtmlDefault);

            ////
            //// regrese el archivo 
            return Content(htmlContent, "text/html");

        }
    }
}