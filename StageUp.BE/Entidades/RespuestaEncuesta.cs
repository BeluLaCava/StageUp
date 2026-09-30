using System;

namespace StageUp.BE.Entidades
{
    // Cabecera de la respuesta de un usuario externo a una encuesta (una
    // sola por usuario y encuesta). Se usa para pasarle al mapper un objeto
    // en vez de ids sueltos.
    public class RespuestaEncuesta
    {
        public int IdRespuestaEncuesta { get; set; }
        public int IdEncuesta { get; set; }
        public int IdUsuarioExterno { get; set; }
        public DateTime FechaRespuesta { get; set; }
    }
}
