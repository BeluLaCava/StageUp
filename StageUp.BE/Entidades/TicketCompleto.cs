using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // Solicitud + conversación + historial, para las pantallas de detalle
    // (la del usuario y la del panel interno).
    public class TicketCompleto
    {
        public Ticket Ticket { get; set; }
        public List<TicketMensaje> Mensajes { get; set; } = new List<TicketMensaje>();
        public List<TicketActualizacion> Actualizaciones { get; set; } = new List<TicketActualizacion>();

        // Cantidad de novedades que había antes de abrir el detalle (A8).
        public int Novedades { get; set; }

        // Estados a los que se puede pasar desde el actual (panel interno).
        public List<string> EstadosPermitidos { get; set; } = new List<string>();
    }
}
