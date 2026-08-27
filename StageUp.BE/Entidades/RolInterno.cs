using System;

namespace StageUp.BE.Entidades
{
    public class RolInterno
    {
        public int IdRolInterno { get; set; }
        public int? IdAreaInterna { get; set; }
        public string NombreRol { get; set; }
        public string Descripcion { get; set; }
        public string EstadoRol { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
    }
}
