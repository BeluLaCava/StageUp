using System;

namespace StageUp.BE.Entidades
{
    public class Ticket
    {
        public int IdTicket { get; set; }
        public int IdUsuarioExterno { get; set; }
        public int? IdReservaAsociada { get; set; }
        public string Categoria { get; set; }
        public string Asunto { get; set; }

        // Ver StageUp.BE.Enumerados.EstadoTicket.
        public string Estado { get; set; }
        public int? IdUsuarioInternoAsignado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaActividad { get; set; }
        public DateTime? FechaCierre { get; set; }

        // Completados por el JOIN de los stored procedures de listado /
        // detalle, para no tener que hacer una segunda consulta desde la UI.
        public string NombreUsuarioExterno { get; set; }
        public string CorreoUsuarioExterno { get; set; }
        public string NombreUsuarioInternoAsignado { get; set; }

        // Contexto de la reserva asociada (null si el ticket no está
        // asociado a ninguna reserva). Lo completa el LEFT JOIN de los
        // stored procedures de consulta, para que soporte responda sabiendo
        // de qué servicio contratado se trata.
        public string NombreEspacioReserva { get; set; }
        public DateTime? FechaReserva { get; set; }
        public int? MinutoDesdeReserva { get; set; }
        public int? MinutoHastaReserva { get; set; }
        public string EstadoReserva { get; set; }

        public bool TieneReservaAsociada
        {
            get { return IdReservaAsociada.HasValue; }
        }
    }
}
