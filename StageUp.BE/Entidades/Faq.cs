using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // CU-003-004: contenido de ayuda (pregunta frecuente) con categoría,
    // estado (Borrador, Activo, Inactivo) y uso en el asistente (CU-003-001).
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
        public bool UsarEnAsistente { get; set; }
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
        public int CantidadAsistente { get; set; }
    }

    // CU-003-001 A4 a A6 (corrección de María): paso del asistente guiado.
    // Tipo "Pregunta": el asistente pregunta y las opciones son sus nodos
    // hijos. Tipo "Respuesta": el asistente responde y ofrece acciones.
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
        public List<NodoAsistente> Opciones { get; set; } = new List<NodoAsistente>();
        public List<AccionNodoAsistente> Acciones { get; set; } = new List<AccionNodoAsistente>();

        // Recorrido desde el inicio (por ejemplo "Reservas > Quiero cancelar
        // una reserva"), para precargar el motivo de la solicitud de soporte.
        public string Recorrido { get; set; }

        public bool EsRespuesta
        {
            get { return Tipo == TipoRespuesta; }
        }
    }

    // Acción que ofrece una respuesta del asistente: "Enlace" (destino =
    // página ~/...), "Nodo" (destino = código de otro paso) o "Soporte"
    // (registrar una solicitud de soporte, CU-003-002).
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

    // CU-003-001 A4 y A5: resultado de una consulta escrita. Primero se busca
    // en los pasos del asistente; si no hay coincidencia, en las preguntas
    // frecuentes que se marcaron para el asistente (CU-003-004).
    public class RespuestaAsistente
    {
        public bool Reconocida { get; set; }
        public NodoAsistente Nodo { get; set; }
        public Faq PreguntaFrecuente { get; set; }
        public List<NodoAsistente> Sugerencias { get; set; } = new List<NodoAsistente>();
    }

    // CU-003-004 A15: consulta del asistente sin respuesta, agrupada.
    public class ConsultaAsistenteSinRespuesta
    {
        public string Texto { get; set; }
        public string TextoNormalizado { get; set; }
        public int Cantidad { get; set; }
        public DateTime UltimaFecha { get; set; }
    }
}
