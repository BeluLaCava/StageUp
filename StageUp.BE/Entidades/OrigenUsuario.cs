using System;

namespace StageUp.BE.Entidades
{
    // Origen de usuario: canal o acción por el que una persona llegó a
    // StageUp (CU-004-004). El código se usa en los enlaces de difusión
    // (por ejemplo Registrarse.aspx?origen=primavera-2026).
    public class OrigenUsuario
    {
        public const string CanalCampania = "Campania";
        public const string CanalAlianza = "Alianza";
        public const string CanalContactoDirecto = "ContactoDirecto";

        public int IdOrigenUsuario { get; set; }
        public string NombreOrigen { get; set; }
        public string Codigo { get; set; }
        public string CanalOrigen { get; set; }
        public string Descripcion { get; set; }
        public int? IdCampaniaComercial { get; set; }
        public string NombreCampania { get; set; }
        public int? IdAlianzaEstrategica { get; set; }
        public string NombreAlianza { get; set; }
        public bool VisibleEnRegistro { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
        public int CantidadUsuarios { get; set; }
        public int CantidadContactos { get; set; }
    }
}
