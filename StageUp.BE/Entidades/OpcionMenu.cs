using System;

namespace StageUp.BE.Entidades
{
    // Opción del menú del panel interno (ítem 19 de la segunda entrega): el
    // menú deja de salir directo de los permisos y pasa a ser una entidad
    // administrable. Cada opción exige un permiso (IdComponentePermiso) para
    // mostrarse.
    public class OpcionMenu
    {
        public int IdOpcionMenu { get; set; }
        public string Texto { get; set; }
        public string Descripcion { get; set; }
        public string Url { get; set; }
        public string Modulo { get; set; }
        public int Orden { get; set; }
        public int IdComponentePermiso { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }

        // Completados por el JOIN con ComponentePermiso, para mostrar en el ABMC.
        public string CodigoPermiso { get; set; }
        public string NombrePermiso { get; set; }
    }
}
