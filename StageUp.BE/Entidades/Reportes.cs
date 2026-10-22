using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{




    public class FiltroReporte
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string Agrupacion { get; set; }
        public string Moneda { get; set; }
        public string Provincia { get; set; }
        public string TipoEspacio { get; set; }
    }


    public class IndicadoresTablero
    {
        public int UsuariosActivos { get; set; }
        public int UsuariosNuevos { get; set; }
        public int EspaciosPublicados { get; set; }
        public int ReservasSolicitadas { get; set; }
        public int ReservasConfirmadas { get; set; }
        public decimal ImporteReservas { get; set; }
        public decimal ImporteComisiones { get; set; }
        public decimal? PromedioCalificacion { get; set; }
        public int TicketsPendientes { get; set; }
    }


    public class FilaReportePeriodo
    {
        public DateTime InicioPeriodo { get; set; }
        public int CantidadReservas { get; set; }
        public decimal ImporteReservas { get; set; }
        public decimal ImporteComisiones { get; set; }


        public string Etiqueta { get; set; }
        public decimal PorcentajeBarra { get; set; }
    }

    public class FilaReporteZona
    {
        public string Provincia { get; set; }
        public string Ciudad { get; set; }
        public int CantidadReservas { get; set; }
        public decimal ImporteReservas { get; set; }

        public decimal PorcentajeBarra { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
    }

    public class FilaReporteEstado
    {
        public string Estado { get; set; }
        public int Cantidad { get; set; }

        public decimal PorcentajeBarra { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
    }

    public class FilaParticipacionEncuesta
    {
        public int IdEncuesta { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string PublicoObjetivo { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int CantidadRespuestas { get; set; }
        public int CantidadDestinatarios { get; set; }


        public decimal? TasaParticipacion { get; set; }
        public decimal PorcentajeBarra { get; set; }
    }


    public class ReporteCompleto
    {
        public FiltroReporte Filtro { get; set; }
        public IndicadoresTablero Indicadores { get; set; }
        public List<FilaReportePeriodo> Ingresos { get; set; } = new List<FilaReportePeriodo>();


        public List<FilaReporteZona> Zonas { get; set; } = new List<FilaReporteZona>();


        public List<FilaReporteZona> ZonasDetalle { get; set; } = new List<FilaReporteZona>();
        public List<FilaReporteEstado> Estados { get; set; } = new List<FilaReporteEstado>();
        public List<FilaParticipacionEncuesta> Encuestas { get; set; } = new List<FilaParticipacionEncuesta>();

        public decimal TotalImporteReservas { get; set; }
        public decimal TotalImporteComisiones { get; set; }
        public int TotalReservasPeriodo { get; set; }


        public string Aviso { get; set; }
    }
}
