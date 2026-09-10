using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Solicitud de reserva de un espacio artístico hecha por un usuario externo.
    /// Incluye horario y precio pactado (columnas normalizadas desde la tanda del
    /// 10/09 — antes viajaban embebidos como texto dentro de ComentarioSolicitante).
    /// Sigue sin pago real ni facturación (eso queda para el Avance 2).
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
        public DateTime? FechaFinalizacion { get; set; }

        // Horario y precio pactado (columnas normalizadas, tanda 10/09). Nulos para
        // reservas viejas hechas antes de que existiera el planificador de horarios.
        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }
        public decimal? PrecioHoraPactado { get; set; }
        public string Moneda { get; set; }
        public decimal? ImporteEstimado { get; set; }

        // Comisión de cancelación (tanda 5): se completa recién al cancelar una
        // reserva ya Aceptada con menos de 24hs de anticipación.
        public bool ComisionAplicada { get; set; }
        public decimal? ImporteComision { get; set; }
        public DateTime? FechaCancelacion { get; set; }

        // Datos adicionales que traen los SPs de listado (join), para no tener que
        // hacer una consulta extra por cada fila al mostrarlas en la UI.
        public string NombreEspacio { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string NombreSolicitante { get; set; }
        public string CorreoSolicitante { get; set; }
        public DateTime? SolicitanteDesde { get; set; }
        public int CantidadReservasAceptadasSolicitante { get; set; }
        public decimal PromedioCalificacionSolicitante { get; set; }
        public int CantidadCalificacionesSolicitante { get; set; }
        public bool CalificacionEspacioRealizada { get; set; }
        public bool CalificacionSolicitanteRealizada { get; set; }
        public List<Calificacion> CalificacionesSolicitante { get; set; } = new List<Calificacion>();
    }
}
