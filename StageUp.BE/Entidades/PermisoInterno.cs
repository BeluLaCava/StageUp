using System;

namespace StageUp.BE.Entidades
{
    public class PermisoInterno
    {
        public int IdPermisoInterno { get; set; }
        public string CodigoPermiso { get; set; }
        public string NombrePermiso { get; set; }
        public string Descripcion { get; set; }
        public string Modulo { get; set; }
        public string Accion { get; set; }
        public string EstadoPermiso { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
    }
}
