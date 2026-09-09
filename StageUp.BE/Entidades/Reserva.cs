using System;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Solicitud de reserva de un espacio artístico hecha por un usuario externo.
    /// Versión simplificada del core del negocio (CU-001-005): solo fecha solicitada
    /// y estado, sin franjas horarias, disponibilidad ni pago (queda para el Avance 2).
    /// </summary>
    public class Reserva
    {
        public int IdReserva { get; set; }
        public int IdEspacioArtistico { get; set; }
        public int IdUsuarioExternoSolicitante { get; set; }
        public DateTime FechaSolicitada { get; set; }
        public string ComentarioSolicitante { get; set; }
        public string EstadoReserva { get; set; }
        public string ComentarioResolucion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }

        // Datos adicionales que traen los SPs de listado (join), para no tener que
        // hacer una consulta extra por cada fila al mostrarlas en la UI.
        public string NombreEspacio { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string NombreSolicitante { get; set; }
        public string CorreoSolicitante { get; set; }
    }
}
