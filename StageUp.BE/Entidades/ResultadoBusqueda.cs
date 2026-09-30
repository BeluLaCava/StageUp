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

        // La arma BLL_Busqueda según el tipo (a dónde lleva el resultado).
        public string Url { get; set; }
        public string EtiquetaTipo { get; set; }
    }

    // Qué buscar: el texto y los tipos de contenido pedidos.
    public class FiltroBusquedaGlobal
    {
        public string Texto { get; set; }
        public List<string> Tipos { get; set; } = new List<string>();

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
