using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // Agrupa la encuesta con sus preguntas (y las opciones de cada una), tal
    // como la necesita tanto el panel de administración (para editar/publicar)
    // como la pantalla pública (para armar el formulario de respuesta).
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
