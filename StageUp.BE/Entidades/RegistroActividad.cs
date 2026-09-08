using System;

namespace StageUp.BE.Entidades
{
    public class RegistroActividad
    {
        public int IdRegistroActividad { get; set; }
        public int? IdUsuarioExternoResponsable { get; set; }
        public int? IdUsuarioInternoResponsable { get; set; }
        public string TipoOperacion { get; set; }
        public string TipoEntidadAfectada { get; set; }
        public int? IdEntidadAfectada { get; set; }
        public string DescripcionOperacion { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string OrigenOperacion { get; set; }
        public string NombreResponsable { get; set; }
        public string CorreoResponsable { get; set; }
    }
}
