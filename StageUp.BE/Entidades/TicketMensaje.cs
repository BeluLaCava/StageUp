using System;

namespace StageUp.BE.Entidades
{
    // Mensaje de la conversación de una solicitud de soporte: del usuario
    // solicitante o una respuesta del equipo interno.
    public class TicketMensaje
    {
        public int IdTicketMensaje { get; set; }
        public int IdTicket { get; set; }

        // Exactamente uno de los dos está cargado.
        public int? IdUsuarioExterno { get; set; }
        public int? IdUsuarioInterno { get; set; }

        public string Mensaje { get; set; }
        public DateTime FechaEnvio { get; set; }

        public string NombreAutor { get; set; }
        public bool EsInterno { get; set; }

        // Ítem 12, los completa BLL_Ticket según quién mira: el mensaje es
        // nuevo para quien lo está viendo, o ya lo leyó la otra parte.
        public bool EsNuevo { get; set; }
        public bool LeidoPorLaOtraParte { get; set; }
    }
}
