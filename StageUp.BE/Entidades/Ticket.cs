using System;
using System.Globalization;

namespace StageUp.BE.Entidades
{
    // Solicitud de soporte (CU-003-002 / CU-003-003). En la base es la tabla
    // Ticket: "motivo" es la columna asunto y "funcionalidad vinculada" la
    // columna categoria (ver Database/68_SolicitudesSoporte.sql).
    public class Ticket
    {
        public int IdTicket { get; set; }
        public int IdUsuarioExterno { get; set; }
        public int? IdReservaAsociada { get; set; }

        // Reserva, Pago, Espacio, Cuenta, Gestor u Otra.
        public string FuncionalidadVinculada { get; set; }
        public string Motivo { get; set; }
        public string Descripcion { get; set; }
        public string InformacionAdicional { get; set; }

        // Desde dónde se registró: Soporte, CentroAyuda, Asistente o Reserva.
        public string Origen { get; set; }

        // Ver StageUp.BE.Enumerados.EstadoTicket.
        public string Estado { get; set; }
        public int? IdUsuarioInternoAsignado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaActividad { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime? FechaCierre { get; set; }

        // Ítem 12 (leído / no leído): última vez que cada lado abrió la
        // solicitud.
        public DateTime? FechaUltimaLecturaUsuario { get; set; }
        public DateTime? FechaUltimaLecturaSoporte { get; set; }

        // Completados por la vista vw_TicketSoporte.
        public string NombreUsuarioExterno { get; set; }
        public string CorreoUsuarioExterno { get; set; }
        public string NombreUsuarioInternoAsignado { get; set; }
        public int CantidadRespuestas { get; set; }

        // Respuestas y cambios de estado que el usuario todavía no vio.
        public int NovedadesUsuario { get; set; }

        // Mensajes del usuario que el equipo todavía no leyó (+1 si la
        // solicitud nunca se abrió).
        public int NovedadesSoporte { get; set; }

        // Contexto de la reserva asociada (null si no tiene).
        public string NombreEspacioReserva { get; set; }
        public DateTime? FechaReserva { get; set; }
        public int? MinutoDesdeReserva { get; set; }
        public int? MinutoHastaReserva { get; set; }
        public string EstadoReserva { get; set; }

        public bool TieneReservaAsociada
        {
            get { return IdReservaAsociada.HasValue; }
        }

        // Identificador del caso que ve el usuario (CU-003-002 paso 11).
        public string Codigo
        {
            get { return FormatearCodigo(IdTicket); }
        }

        public static string FormatearCodigo(int idTicket)
        {
            return "SOP-" + idTicket.ToString("D6", CultureInfo.InvariantCulture);
        }
    }
}
