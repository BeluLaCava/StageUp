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

    // CU-003-001 A4: respuesta del asistente a una consulta escrita.
    public class RespuestaAsistente
    {
        public bool Reconocida { get; set; }
        public Faq Respuesta { get; set; }
        public List<Faq> Sugerencias { get; set; } = new List<Faq>();
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
