using System.Collections.Generic;

namespace StageUp.BE.Entidades
{



    public class EncuestaCompleta
    {
        public Encuesta Encuesta { get; set; }
        public List<PreguntaEncuesta> Preguntas { get; set; }

        public EncuestaCompleta()
        {
            Preguntas = new List<PreguntaEncuesta>();
        }
    }
}
