using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // CU-004-004 Filtros del tablero de métricas comerciales.
    public class FiltroMetricasComerciales
    {
        public const string CanalNoInformado = "NoInformado";
        public const string TipoSolicitante = "Solicitante";
        public const string TipoGestor = "Gestor";

        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? IdCampaniaComercial { get; set; }
        public string CanalOrigen { get; set; }
        public string TipoUsuario { get; set; }

        public bool TieneCriterios
        {
            get { return Desde.HasValue || Hasta.HasValue || IdCampaniaComercial.HasValue || CanalOrigen != null || TipoUsuario != null; }
        }
    }

    public class ResumenMetricasComerciales
    {
        public int NuevosUsuarios { get; set; }
        public int UsuariosConOrigen { get; set; }
        public int EspaciosIncorporados { get; set; }
        public int CampaniasActivas { get; set; }
        public int ContactosConvertidos { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalCampanias { get; set; }
        public int TotalContactos { get; set; }

        public int UsuariosSinOrigen
        {
            get { return NuevosUsuarios - UsuariosConOrigen; }
        }

        // A2: no hay nada registrado todavía.
        public bool SinInformacion
        {
            get { return TotalUsuarios == 0 && TotalCampanias == 0 && TotalContactos == 0; }
        }

        public bool SinResultados
        {
            get { return NuevosUsuarios == 0 && EspaciosIncorporados == 0 && CampaniasActivas == 0 && ContactosConvertidos == 0; }
        }
    }

    // Usuarios registrados por un origen (IdOrigenUsuario null = origen no informado).
    public class MetricaOrigenUsuario
    {
        public int? IdOrigenUsuario { get; set; }
        public string CanalOrigen { get; set; }
        public string NombreOrigen { get; set; }
        public int? IdCampaniaComercial { get; set; }
        public string NombreCampania { get; set; }
        public int? IdAlianzaEstrategica { get; set; }
        public string NombreAlianza { get; set; }
        public int CantidadUsuarios { get; set; }
        public int CantidadGestores { get; set; }
    }

    // Total por canal de origen (para el detalle de A10 y el gráfico).
    public class MetricaCanalOrigen
    {
        public string CanalOrigen { get; set; }
        public string NombreCanal { get; set; }
        public int CantidadUsuarios { get; set; }
        public decimal Porcentaje { get; set; }
        public List<MetricaOrigenUsuario> Origenes { get; set; } = new List<MetricaOrigenUsuario>();
    }
}
