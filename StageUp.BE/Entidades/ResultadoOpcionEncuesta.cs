namespace StageUp.BE.Entidades
{
    public class ResultadoOpcionEncuesta
    {
        public int IdOpcionPregunta { get; set; }
        public string TextoOpcion { get; set; }
        public int Orden { get; set; }
        public int CantidadRespuestas { get; set; }

        // Calculado en el momento por la BLL a partir de las respuestas que
        // haya en ese instante (gráfico de encuestas al instante, nada
        // queda cacheado).
        public double Porcentaje { get; set; }
    }
}
