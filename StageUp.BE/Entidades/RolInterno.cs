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


        public int CantidadUsuariosActivos { get; set; }
        public int CantidadComponentes { get; set; }
    }



    public class DetalleRolInterno
    {
        public RolInterno Rol { get; set; }


        public List<PermisoComponente> ComponentesAsignados { get; set; }



        public List<PermisoHoja> PermisosEfectivos { get; set; }

        public List<UsuarioInterno> Usuarios { get; set; }
    }
}
