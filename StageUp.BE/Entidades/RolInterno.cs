using System;
using System.Collections.Generic;
using StageUp.BE.Permisos;

namespace StageUp.BE.Entidades
{
    public class RolInterno
    {
        public int IdRolInterno { get; set; }
        public int? IdAreaInterna { get; set; }
        public string NombreArea { get; set; }
        public string NombreRol { get; set; }
        public string Descripcion { get; set; }
        public string EstadoRol { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }

        // CU-001-012 A11 paso 4: datos resumidos del listado de roles.
        public int CantidadUsuariosActivos { get; set; }
        public int CantidadComponentes { get; set; }
    }

    // CU-001-012 A12 paso 2: detalle del rol con sus permisos y los usuarios
    // internos que lo tienen asignado.
    public class DetalleRolInterno
    {
        public RolInterno Rol { get; set; }

        // Lo que está tildado en el rol (grupos enteros o permisos sueltos).
        public List<PermisoComponente> ComponentesAsignados { get; set; }

        // Los permisos que resultan de esa asignación (los de cada grupo
        // más los sueltos, sin repetir).
        public List<PermisoHoja> PermisosEfectivos { get; set; }

        public List<UsuarioInterno> Usuarios { get; set; }
    }
}
