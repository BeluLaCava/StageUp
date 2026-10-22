using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{

    public class AlianzaEstrategica
    {
        public const string EstadoEnEvaluacion = "EnEvaluacion";
        public const string EstadoActiva = "Activa";
        public const string EstadoPausada = "Pausada";
        public const string EstadoFinalizada = "Finalizada";
        public const string EstadoDadaDeBaja = "DadaDeBaja";

        public int IdAlianzaEstrategica { get; set; }
        public string NombreEntidad { get; set; }
        public string TipoInstitucion { get; set; }
        public string DatosContacto { get; set; }
        public int IdUsuarioInternoResponsable { get; set; }
        public string NombreResponsable { get; set; }
        public string ObjetivoComercial { get; set; }
        public string Alcance { get; set; }
        public string DescripcionAcuerdo { get; set; }
        public string BeneficiosAsociados { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string EstadoAlianza { get; set; }
        public string Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public bool Activa { get; set; }

        public int CantidadContactos { get; set; }
        public int CantidadCampanias { get; set; }
        public int CantidadContenidos { get; set; }
        public int CantidadOrigenes { get; set; }


        public string ObservacionCambioEstado { get; set; }


        public List<int> IdsContactos { get; set; } = new List<int>();
        public List<int> IdsCampanias { get; set; } = new List<int>();
        public List<int> IdsContenidos { get; set; } = new List<int>();
        public List<int> IdsOrigenes { get; set; } = new List<int>();

        public bool EnGestion
        {
            get { return EstadoAlianza == EstadoEnEvaluacion || EstadoAlianza == EstadoActiva || EstadoAlianza == EstadoPausada; }
        }


        public bool Vencida
        {
            get
            {
                return (EstadoAlianza == EstadoActiva || EstadoAlianza == EstadoPausada) &&
                    FechaFin.HasValue && FechaFin.Value.Date < DateTime.Today;
            }
        }

        public int CantidadVinculos
        {
            get { return CantidadContactos + CantidadCampanias + CantidadContenidos + CantidadOrigenes; }
        }
    }



    public class SeguimientoAlianza
    {
        public const string TipoSeguimiento = "Seguimiento";
        public const string TipoCambioEstado = "CambioEstado";
        public const string TipoRenovacion = "Renovacion";
        public const string TipoFinalizacion = "Finalizacion";
        public const string TipoBaja = "Baja";
        public const string TipoReactivacion = "Reactivacion";

        public int IdSeguimientoAlianza { get; set; }
        public int IdAlianzaEstrategica { get; set; }
        public string TipoRegistro { get; set; }
        public DateTime FechaInteraccion { get; set; }
        public string TipoInteraccion { get; set; }
        public string Observacion { get; set; }
        public string Avance { get; set; }
        public string AccionPendiente { get; set; }
        public int? IdUsuarioInternoAsignado { get; set; }
        public string NombreAsignado { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoResultante { get; set; }
        public int IdUsuarioInternoRegistro { get; set; }
        public string NombreRegistro { get; set; }
        public DateTime FechaRegistro { get; set; }
    }


    public class VigenciaAlianza
    {
        public int IdAlianzaVigencia { get; set; }
        public DateTime FechaInicioAnterior { get; set; }
        public DateTime? FechaFinAnterior { get; set; }
        public DateTime FechaInicioNueva { get; set; }
        public DateTime? FechaFinNueva { get; set; }
        public string Observacion { get; set; }
        public string NombreUsuario { get; set; }
        public DateTime FechaRegistro { get; set; }
    }


    public class RenovacionAlianza
    {
        public int IdAlianzaEstrategica { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Alcance { get; set; }
        public string BeneficiosAsociados { get; set; }
        public int? IdUsuarioInternoResponsable { get; set; }
        public string Observacion { get; set; }
        public bool Reactivar { get; set; }
    }

    public class FiltroAlianzaEstrategica
    {
        public const string VistaEnGestion = "EnGestion";
        public const string VistaHistoricas = "Historicas";
        public const string VistaTodas = "Todas";

        public string Vista { get; set; } = VistaEnGestion;
        public string Estado { get; set; }
        public string TipoInstitucion { get; set; }
        public int? IdUsuarioInternoResponsable { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Texto { get; set; }

        public bool TieneCriterios
        {
            get
            {
                return Vista != VistaEnGestion || Estado != null || TipoInstitucion != null || IdUsuarioInternoResponsable.HasValue ||
                    Desde.HasValue || Hasta.HasValue || Texto != null;
            }
        }
    }

    public class ResumenAlianzas
    {
        public int Total { get; set; }
        public int EnGestion { get; set; }
        public int Activas { get; set; }
        public int EnEvaluacion { get; set; }
        public int Vencidas { get; set; }
        public int Historicas { get; set; }
    }
}
