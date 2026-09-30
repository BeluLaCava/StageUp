namespace StageUp.BE.Entidades
{
    // Opción elegida por el usuario para una pregunta puntual de la encuesta.
    public class RespuestaEncuestaDetalle
    {
        public int IdRespuestaEncuesta { get; set; }
        public int IdPreguntaEncuesta { get; set; }
        public int IdOpcionPregunta { get; set; }
    }
}
