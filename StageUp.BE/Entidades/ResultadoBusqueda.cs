using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{


    public class ResultadoBusqueda
    {
        public string Tipo { get; set; }
        public int IdReferencia { get; set; }
        public string Titulo { get; set; }
        public string Detalle { get; set; }
        public DateTime? Fecha { get; set; }


        public int Posicion { get; set; }


        public string Url { get; set; }
        public string EtiquetaTipo { get; set; }
    }




    public class FiltroBusquedaGlobal
    {
        public const string OrdenRelevancia = "Relevancia";
        public const string OrdenRecientes = "Recientes";
        public const string OrdenAntiguos = "Antiguos";
        public const string OrdenAlfabetico = "Alfabetico";

        public string Texto { get; set; }
        public List<string> Tipos { get; set; } = new List<string>();

        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }


        public string Ubicacion { get; set; }

        public string TipoEspacio { get; set; }
        public string Provincia { get; set; }
        public string Ciudad { get; set; }
        public string Zona { get; set; }
        public decimal? PrecioMinimo { get; set; }


        public int? CapacidadMinima { get; set; }
        public decimal? PrecioMaximo { get; set; }
        public DateTime? FechaDisponibilidad { get; set; }
        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }
        public List<string> Equipamiento { get; set; } = new List<string>();
        public string Caracteristicas { get; set; }
        public decimal? CalificacionMinima { get; set; }


        public string CategoriaNovedad { get; set; }



        public string Estado { get; set; }

        public string Perfil { get; set; }
        public string Rol { get; set; }
        public string Responsable { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Operacion { get; set; }
        public string EntidadAfectada { get; set; }
        public int? IdRegistro { get; set; }

        public string Orden { get; set; } = OrdenRelevancia;


        public int Maximo { get; set; } = 25;

        public bool TieneCriteriosAvanzados
        {
            get
            {
                return FechaDesde.HasValue || FechaHasta.HasValue || !string.IsNullOrEmpty(Ubicacion) ||
                    !string.IsNullOrEmpty(TipoEspacio) || !string.IsNullOrEmpty(Provincia) || !string.IsNullOrEmpty(Ciudad) ||
                    !string.IsNullOrEmpty(Zona) || PrecioMinimo.HasValue || CapacidadMinima.HasValue || PrecioMaximo.HasValue ||
                    FechaDisponibilidad.HasValue || MinutoDesde.HasValue || MinutoHasta.HasValue || (Equipamiento != null && Equipamiento.Count > 0) ||
                    !string.IsNullOrEmpty(Caracteristicas) || CalificacionMinima.HasValue || !string.IsNullOrEmpty(CategoriaNovedad) ||
                    !string.IsNullOrEmpty(Estado) || !string.IsNullOrEmpty(Perfil) || !string.IsNullOrEmpty(Rol) ||
                    !string.IsNullOrEmpty(Responsable) || !string.IsNullOrEmpty(Nombre) || !string.IsNullOrEmpty(Correo) ||
                    !string.IsNullOrEmpty(Operacion) || !string.IsNullOrEmpty(EntidadAfectada) || IdRegistro.HasValue;
            }
        }

        public bool Incluye(string tipo)
        {
            return Tipos != null && Tipos.Contains(tipo);
        }
    }


    public class GrupoResultadosBusqueda
    {
        public string Tipo { get; set; }
        public string EtiquetaTipo { get; set; }
        public List<ResultadoBusqueda> Resultados { get; set; } = new List<ResultadoBusqueda>();
    }
}
