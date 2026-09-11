using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
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

        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }
        public decimal? PrecioHoraPactado { get; set; }
        public string Moneda { get; set; }
        public decimal? ImporteEstimado { get; set; }

        public bool ComisionAplicada { get; set; }
        public decimal? ImporteComision { get; set; }
        public DateTime? FechaCancelacion { get; set; }

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
