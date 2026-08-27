using System;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Área interna de StageUp (ej. Tecnología, Administración). Forma
    /// parte de la arquitectura Usuario Interno -> Rol -> Permiso que se
    /// prepara desde este avance, aunque todavía no tiene pantallas de
    /// administración propias.
    /// </summary>
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
