using System;

namespace StageUp.BE.Entidades
{
    public class Faq
    {
        public int IdFaq { get; set; }
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
    }
}
