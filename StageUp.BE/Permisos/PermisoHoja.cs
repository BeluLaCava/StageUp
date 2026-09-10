using System;
using System.Collections.Generic;

namespace StageUp.BE.Permisos
{
    public class PermisoHoja : PermisoComponente
    {
        public string CodigoPermiso { get; private set; }
        public string Descripcion { get; private set; }
        public string UrlAsociada { get; private set; }
        public string NombreGrupo { get; set; }

        public PermisoHoja(int idComponentePermiso, string codigoPermiso, string nombre, string descripcion, string urlAsociada)
        {
            IdComponentePermiso = idComponentePermiso;
            CodigoPermiso = codigoPermiso;
            Nombre = nombre;
            Descripcion = descripcion;
            UrlAsociada = urlAsociada;
        }

        public override void AgregarHijo(PermisoComponente componente)
        {
            throw new NotSupportedException("Un permiso es una hoja del árbol y no puede tener hijos.");
        }

        public override void EliminarHijo(PermisoComponente componente)
        {
            throw new NotSupportedException("Un permiso es una hoja del árbol y no puede tener hijos.");
        }

        public override List<PermisoHoja> Listar()
        {
            return new List<PermisoHoja> { this };
        }
    }
}
