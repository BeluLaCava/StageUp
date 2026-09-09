using System;

namespace StageUp.BE.Entidades
{
    public class UsuarioExterno
    {
        public int IdUsuarioExterno { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string CorreoElectronico { get; set; }
        public string PasswordHash { get; set; }
        public string Telefono { get; set; }
        public string EstadoCuenta { get; set; }
        public string PerfilUsuario { get; set; }
        public string FotoPerfilRuta { get; set; }
        public string DescripcionPerfil { get; set; }
        public bool AceptaTerminos { get; set; }
        public bool AceptaPoliticaPrivacidad { get; set; }
        public DateTime? FechaAceptacionTerminos { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaActivacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
    }
}
