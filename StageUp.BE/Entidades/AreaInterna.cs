using System;

namespace StageUp.BE.Entidades
{
    public class AreaInterna
    {
        public int IdAreaInterna { get; set; }
        public string NombreArea { get; set; }
        public string Descripcion { get; set; }
        public string EstadoArea { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
    }
}
