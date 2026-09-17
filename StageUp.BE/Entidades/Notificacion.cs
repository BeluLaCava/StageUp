using System;

namespace StageUp.BE.Entidades
{
    public class Notificacion
    {
        public int IdNotificacion { get; set; }
        public int IdUsuarioExterno { get; set; }

        // Ver StageUp.BE.Enumerados.TipoNotificacion.
        public string Tipo { get; set; }
        public string Mensaje { get; set; }
        public string UrlDestino { get; set; }
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
