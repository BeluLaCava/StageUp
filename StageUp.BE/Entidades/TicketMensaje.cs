using System;

namespace StageUp.BE.Entidades
{


    public class TicketMensaje
    {
        public int IdTicketMensaje { get; set; }
        public int IdTicket { get; set; }


        public int? IdUsuarioExterno { get; set; }
        public int? IdUsuarioInterno { get; set; }

        public string Mensaje { get; set; }
        public DateTime FechaEnvio { get; set; }

        public string NombreAutor { get; set; }
        public bool EsInterno { get; set; }



        public bool EsNuevo { get; set; }
        public bool LeidoPorLaOtraParte { get; set; }
    }
}
