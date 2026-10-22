using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{

    public class CampaniaComercial
    {
        public const string EstadoPlanificada = "Planificada";
        public const string EstadoActiva = "Activa";
        public const string EstadoFinalizada = "Finalizada";
        public const string EstadoCancelada = "Cancelada";

        public int IdCampaniaComercial { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string CanalDifusion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string EstadoCampania { get; set; }
        public List<string> PublicoObjetivo { get; set; } = new List<string>();
        public int? IdUsuarioInternoResponsable { get; set; }
        public string NombreResponsable { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public bool Activa { get; set; }

        public int CantidadContactos { get; set; }
        public int CantidadAlianzas { get; set; }
        public int CantidadContenidos { get; set; }
        public int CantidadOrigenes { get; set; }


        public List<int> IdsContactos { get; set; } = new List<int>();
        public List<int> IdsAlianzas { get; set; } = new List<int>();
        public List<int> IdsContenidos { get; set; } = new List<int>();
        public List<int> IdsOrigenes { get; set; } = new List<int>();

        public int CantidadVinculos
        {
            get { return CantidadContactos + CantidadAlianzas + CantidadContenidos + CantidadOrigenes; }
        }
    }

    public class FiltroCampaniaComercial
    {
        public const string VistaActivas = "Activas";
        public const string VistaBajas = "Bajas";
        public const string VistaTodas = "Todas";

        public string Vista { get; set; } = VistaActivas;
        public string Estado { get; set; }
        public string CanalDifusion { get; set; }
        public string Segmento { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Texto { get; set; }

        public bool TieneCriterios
        {
            get
            {
                return Vista != VistaActivas || Estado != null || CanalDifusion != null || Segmento != null ||
                    Desde.HasValue || Hasta.HasValue || Texto != null;
            }
        }
    }

    public class ResumenCampanias
    {
        public int Total { get; set; }
        public int Activas { get; set; }
        public int Bajas { get; set; }
    }
}
