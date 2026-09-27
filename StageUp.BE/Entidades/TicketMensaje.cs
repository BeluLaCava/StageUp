using System;

namespace StageUp.BE.Entidades
{
    public class TicketMensaje
    {
        public int IdTicketMensaje { get; set; }
        public int IdTicket { get; set; }

        // Exactamente uno de los dos está cargado: el mensaje es del usuario
        // externo que abrió el ticket o de un interno que responde, nunca de
        // ambos a la vez.
        public int? IdUsuarioExterno { get; set; }
        public int? IdUsuarioInterno { get; set; }

        public string Mensaje { get; set; }
        public DateTime FechaEnvio { get; set; }

        // Completados por el JOIN de sp_TicketMensaje_ListarPorTicket, para
        // que la UI pueda mostrar el hilo sin resolver el autor por su cuenta.
        public string NombreAutor { get; set; }
        public bool EsInterno { get; set; }
    }
}
