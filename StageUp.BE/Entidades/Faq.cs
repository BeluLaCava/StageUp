using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{


    public class Faq
    {
        public int IdFaq { get; set; }
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public string Estado { get; set; }
        public int? IdCategoriaAyuda { get; set; }
        public string NombreCategoria { get; set; }
        public int OrdenCategoria { get; set; }
        public bool CategoriaActiva { get; set; }


        public bool EsBasica { get; set; }
        public string PalabrasClave { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
    }

    public class FiltroFaq
    {
        public string Estado { get; set; }
        public int? IdCategoriaAyuda { get; set; }
        public string Texto { get; set; }
    }

    public class CategoriaAyuda
    {
        public int IdCategoriaAyuda { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Orden { get; set; }
        public bool Activa { get; set; }
        public int CantidadActivas { get; set; }
        public int CantidadBasicas { get; set; }
    }




    public class NodoAsistente
    {
        public const string TipoPregunta = "Pregunta";
        public const string TipoRespuesta = "Respuesta";

        public int IdNodoAsistente { get; set; }
        public int? IdNodoPadre { get; set; }
        public string Codigo { get; set; }
        public string Tipo { get; set; }
        public string TextoOpcion { get; set; }
        public string Mensaje { get; set; }
        public string PalabrasClave { get; set; }
        public int Orden { get; set; }





        public string DescripcionOpcion { get; set; }
        public string Icono { get; set; }
        public string TextoBotonLista { get; set; }

        public List<NodoAsistente> Opciones { get; set; } = new List<NodoAsistente>();
        public List<AccionNodoAsistente> Acciones { get; set; } = new List<AccionNodoAsistente>();



        public string Recorrido { get; set; }

        public bool EsRespuesta
        {
            get { return Tipo == TipoRespuesta; }
        }
    }




    public class AccionNodoAsistente
    {
        public const string TipoEnlace = "Enlace";
        public const string TipoNodo = "Nodo";
        public const string TipoSoporte = "Soporte";

        public int IdAccionNodoAsistente { get; set; }
        public int IdNodoAsistente { get; set; }
        public string Texto { get; set; }
        public string TipoAccion { get; set; }
        public string Destino { get; set; }
        public int Orden { get; set; }
    }



    public class RespuestaAsistente
    {
        public bool Reconocida { get; set; }
        public NodoAsistente Nodo { get; set; }
        public List<NodoAsistente> Sugerencias { get; set; } = new List<NodoAsistente>();




        public string RespuestaAutomatica { get; set; }
        public bool AsistenteAutomaticoNoDisponible { get; set; }




        public bool ConDatosPrivados { get; set; }
    }


    public class ConsultaAsistenteSinRespuesta
    {
        public string Texto { get; set; }
        public string TextoNormalizado { get; set; }
        public int Cantidad { get; set; }
        public DateTime UltimaFecha { get; set; }
    }
}
