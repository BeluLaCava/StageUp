using System;

namespace StageUp.BE.Entidades
{
    // Mensaje privado entre el solicitante y el gestor de una reserva.
    // Ver Database/73_MensajesReserva.sql.
    public class MensajeReserva
    {
        public int IdMensajeReserva { get; set; }
        public int IdReserva { get; set; }
        public int IdUsuarioExternoEmisor { get; set; }
        public string NombreEmisor { get; set; }
        public string ApellidoEmisor { get; set; }
        public string Texto { get; set; }
        public DateTime FechaEnvio { get; set; }

        // Cuándo lo leyó el otro participante (null = todavía no lo leyó).
        public DateTime? FechaLectura { get; set; }

        public bool Leido
        {
            get { return FechaLectura.HasValue; }
        }

        public string NombreCompletoEmisor
        {
            get { return ((NombreEmisor ?? string.Empty) + " " + (ApellidoEmisor ?? string.Empty)).Trim(); }
        }
    }

    // Mensajes sin leer de un usuario en una reserva, según su papel en ella.
    public class MensajesNoLeidosReserva
    {
        public const string RolSolicitante = "Solicitante";
        public const string RolGestor = "Gestor";

        public int IdReserva { get; set; }
        public string Rol { get; set; }
        public int Cantidad { get; set; }
    }
}
