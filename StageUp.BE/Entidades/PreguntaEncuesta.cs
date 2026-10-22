using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    public class PreguntaEncuesta
    {
        public int IdPreguntaEncuesta { get; set; }
        public int IdEncuesta { get; set; }
        public string Texto { get; set; }
        public int Orden { get; set; }



        public List<OpcionPregunta> Opciones { get; set; }

        public PreguntaEncuesta()
        {
            Opciones = new List<OpcionPregunta>();
        }
    }
}
