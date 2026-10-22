using System.Collections.Generic;

namespace StageUp.BE.Entidades
{


    public class TicketCompleto
    {
        public Ticket Ticket { get; set; }
        public List<TicketMensaje> Mensajes { get; set; } = new List<TicketMensaje>();
        public List<TicketActualizacion> Actualizaciones { get; set; } = new List<TicketActualizacion>();


        public int Novedades { get; set; }


        public List<string> EstadosPermitidos { get; set; } = new List<string>();
    }
}
