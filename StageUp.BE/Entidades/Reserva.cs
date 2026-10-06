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
        public bool RecordatorioEnviado { get; set; }

        // Módulo de pagos (script 49): NoRequerido | Pendiente | Pagado | Devuelto | Vencido
        public string EstadoPago { get; set; }
        public DateTime? FechaLimitePago { get; set; }
        public DateTime? FechaPago { get; set; }

        public string NombreEspacio { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string NombreGestor { get; set; }
        public string NombreSolicitante { get; set; }
        public string CorreoSolicitante { get; set; }
        public DateTime? SolicitanteDesde { get; set; }
        public int CantidadReservasAceptadasSolicitante { get; set; }
        public decimal PromedioCalificacionSolicitante { get; set; }
        public int CantidadCalificacionesSolicitante { get; set; }
        public bool CalificacionEspacioRealizada { get; set; }
        public bool CalificacionSolicitanteRealizada { get; set; }
        public List<Calificacion> CalificacionesSolicitante { get; set; } = new List<Calificacion>();

        // Detalle de reserva (CU-001-005 A8): datos del espacio que muestra
        // la pantalla DetalleReserva.aspx (sp_Reserva_ObtenerDetalle).
        public string TipoEspacio { get; set; }
        public string ProvinciaEspacio { get; set; }
        public string CiudadEspacio { get; set; }
        public string DireccionEspacio { get; set; }
        public int? CapacidadMaxima { get; set; }
        public string TipoPiso { get; set; }
        public string DetalleEquipamiento { get; set; }
    }

    // Política de cancelación vigente (CU-001-005 A14 a A16), tomada de los
    // parámetros de la plataforma.
    public class PoliticaCancelacion
    {
        public int DiasSinCargo { get; set; }
        public int DiasCargoParcial { get; set; }
        public decimal PorcentajeCargoParcial { get; set; }
        public decimal PorcentajeCargoTotal { get; set; }
    }
}
