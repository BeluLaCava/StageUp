using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    public class ResultadoPreguntaEncuesta
    {
        public int IdPreguntaEncuesta { get; set; }
        public string TextoPregunta { get; set; }
        public int Orden { get; set; }
        public int TotalRespuestas { get; set; }
        public List<ResultadoOpcionEncuesta> Opciones { get; set; }

        public ResultadoPreguntaEncuesta()
        {
            Opciones = new List<ResultadoOpcionEncuesta>();
        }
    }
}
