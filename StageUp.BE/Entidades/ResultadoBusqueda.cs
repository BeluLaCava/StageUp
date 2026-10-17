using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // Un resultado de la búsqueda global (ítems 4 y 13 de la segunda
    // entrega). El mismo formato sirve para cualquier tipo de contenido.
    public class ResultadoBusqueda
    {
        public string Tipo { get; set; }
        public int IdReferencia { get; set; }
        public string Titulo { get; set; }
        public string Detalle { get; set; }
        public DateTime? Fecha { get; set; }

        // Lugar dentro de su tipo según el orden pedido (lo calcula el SP).
        public int Posicion { get; set; }

        // La arma BLL_Busqueda según el tipo (a dónde lleva el resultado).
        public string Url { get; set; }
        public string EtiquetaTipo { get; set; }
    }

    // Qué buscar: el texto, los tipos de contenido pedidos y los criterios de
    // la búsqueda avanzada (todos opcionales; null = sin ese filtro). La MPP
    // lo pasa entero como parámetros del SP (script 76).
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

        // Espacios (y reservas, en la búsqueda interna): ciudad, provincia o dirección.
        public string Ubicacion { get; set; }

        // Solo búsqueda pública (espacios).
        public int? CapacidadMinima { get; set; }
        public decimal? PrecioMaximo { get; set; }

        // Novedades: Institucional, Catalogo, Consejos o Comunidad.
        public string CategoriaNovedad { get; set; }

        // Solo búsqueda interna: estado de un tipo de contenido (por ejemplo
        // "Pendiente" de Reserva). Va junto con un único tipo en Tipos.
        public string Estado { get; set; }

        public string Orden { get; set; } = OrdenRelevancia;

        // Resultados por tipo de contenido.
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

    // Resultados agrupados por tipo, para mostrarlos por secciones.
    public class GrupoResultadosBusqueda
    {
        public string Tipo { get; set; }
        public string EtiquetaTipo { get; set; }
        public List<ResultadoBusqueda> Resultados { get; set; } = new List<ResultadoBusqueda>();
    }
}
