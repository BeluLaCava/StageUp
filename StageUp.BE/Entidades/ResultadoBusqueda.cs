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


        public int? CapacidadMinima { get; set; }
        public decimal? PrecioMaximo { get; set; }


        public string CategoriaNovedad { get; set; }



        public string Estado { get; set; }

        public string Orden { get; set; } = OrdenRelevancia;


        public int Maximo { get; set; } = 25;

        public bool TieneCriteriosAvanzados
        {
            get
            {
                return FechaDesde.HasValue || FechaHasta.HasValue || !string.IsNullOrEmpty(Ubicacion) ||
                    CapacidadMinima.HasValue || PrecioMaximo.HasValue || !string.IsNullOrEmpty(CategoriaNovedad) ||
                    !string.IsNullOrEmpty(Estado);
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
