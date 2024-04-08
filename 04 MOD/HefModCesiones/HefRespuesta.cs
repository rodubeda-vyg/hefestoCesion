namespace HefModCesiones
{

    /// <summary>
    /// Representa el resultado de un proceso
    /// </summary>
    public class HefRespuesta
    {
        public string? Fecha { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public bool EsCorrecto { get; set; }
        public string? Mensaje { get; set; }
        public string? Detalle { get; set; }

        public string? CodigoSII { get; set; } = null;

        public string? Trackid { get; set; } = null;


        public object? Resultado { get; set; }

    }
}