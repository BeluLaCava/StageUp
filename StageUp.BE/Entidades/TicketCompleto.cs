using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // Ticket + su hilo de mensajes completo, para las pantallas de detalle
    // (tanto la del usuario externo como la del panel interno). Mismo
    // patrón que EncuestaCompleta.
    public class TicketCompleto
    {
        public Ticket Ticket { get; set; }
        public List<TicketMensaje> Mensajes { get; set; } = new List<TicketMensaje>();
    }
}
