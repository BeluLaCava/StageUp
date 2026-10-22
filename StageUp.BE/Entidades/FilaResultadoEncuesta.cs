namespace StageUp.BE.Entidades
{




    public class FilaResultadoEncuesta
    {
        public int IdPreguntaEncuesta { get; set; }
        public string TextoPregunta { get; set; }
        public int OrdenPregunta { get; set; }
        public int IdOpcionPregunta { get; set; }
        public string TextoOpcion { get; set; }
        public int OrdenOpcion { get; set; }
        public int CantidadRespuestas { get; set; }
        public int TotalRespuestasPregunta { get; set; }
    }
}
