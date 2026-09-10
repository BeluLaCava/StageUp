using System;

namespace StageUp.BE.Entidades
{
    public class UsuarioInterno
    {
        public int IdUsuarioInterno { get; set; }
        public int IdAreaInterna { get; set; }
        public int IdRolInterno { get; set; }
        public string NombreArea { get; set; }
        public string NombreRol { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string CorreoElectronico { get; set; }
        public string PasswordHash { get; set; }
        public string EstadoCuenta { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
    }
}
