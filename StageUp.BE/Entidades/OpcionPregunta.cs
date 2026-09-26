namespace StageUp.BE.Entidades
{
    public class OpcionPregunta
    {
        public int IdOpcionPregunta { get; set; }
        public int IdPreguntaEncuesta { get; set; }
        public string Texto { get; set; }
        public int Orden { get; set; }
    }
}
