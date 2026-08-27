using System;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Usuario de staff interno de StageUp, con un rol (RolInterno) y
    /// un área (AreaInterna) asociados. La gestión completa de usuarios
    /// internos y permisos corresponde a CU-001-012, que queda fuera del
    /// alcance de este primer avance: por ahora solo existe la entidad y
    /// la tabla, preparando la arquitectura.
    /// </summary>
    public class UsuarioInterno
    {
        public int IdUsuarioInterno { get; set; }
        public int IdAreaInterna { get; set; }
        public int IdRolInterno { get; set; }
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
