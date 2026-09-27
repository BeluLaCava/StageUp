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
    }
}
