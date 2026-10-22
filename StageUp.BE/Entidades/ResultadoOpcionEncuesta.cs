namespace StageUp.BE.Entidades
{
    public class ResultadoOpcionEncuesta
    {
        public int IdOpcionPregunta { get; set; }
        public string TextoOpcion { get; set; }
        public int Orden { get; set; }
        public int CantidadRespuestas { get; set; }




        public double Porcentaje { get; set; }
    }
}
