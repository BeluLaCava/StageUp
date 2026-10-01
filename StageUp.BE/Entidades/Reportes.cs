using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // Entidades de los reportes con gráficos (ítem 15 de la segunda entrega,
    // script 48, Interno/Reportes.aspx).

    // Qué pidió ver el usuario. Provincia y TipoEspacio en null = todas/todos.
    public class FiltroReporte
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string Agrupacion { get; set; }   // Dia | Semana | Mes | Anio
        public string Moneda { get; set; }       // ARS | USD
        public string Provincia { get; set; }
        public string TipoEspacio { get; set; }
    }

    // Tablero: totales del período (sp_Reporte_Indicadores).
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

    // Una barra del gráfico de ingresos (un día, semana, mes o año).
    public class FilaReportePeriodo
    {
        public DateTime InicioPeriodo { get; set; }
        public int CantidadReservas { get; set; }
        public decimal ImporteReservas { get; set; }
        public decimal ImporteComisiones { get; set; }

        // Los completa BLL_Reporte.
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

        // Respuestas sobre destinatarios, en %. Null si no hay destinatarios.
        public decimal? TasaParticipacion { get; set; }
        public decimal PorcentajeBarra { get; set; }
    }

    // Todo lo que muestra la pantalla de reportes para un filtro.
    public class ReporteCompleto
    {
        public FiltroReporte Filtro { get; set; }
        public IndicadoresTablero Indicadores { get; set; }
        public List<FilaReportePeriodo> Ingresos { get; set; } = new List<FilaReportePeriodo>();
        // Para el gráfico: las 9 zonas con más ingresos y el resto sumado en
        // "Otras zonas".
        public List<FilaReporteZona> Zonas { get; set; } = new List<FilaReporteZona>();

        // Para la vista de tabla y el CSV: todas las zonas, sin agrupar.
        public List<FilaReporteZona> ZonasDetalle { get; set; } = new List<FilaReporteZona>();
        public List<FilaReporteEstado> Estados { get; set; } = new List<FilaReporteEstado>();
        public List<FilaParticipacionEncuesta> Encuestas { get; set; } = new List<FilaParticipacionEncuesta>();

        public decimal TotalImporteReservas { get; set; }
        public decimal TotalImporteComisiones { get; set; }
        public int TotalReservasPeriodo { get; set; }

        // Aviso cuando la BLL tuvo que ajustar la agrupación pedida.
        public string Aviso { get; set; }
    }
}
