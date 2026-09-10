using System.Collections.Generic;

namespace StageUp.BE.Permisos
{
    public abstract class PermisoComponente
    {
        public int IdComponentePermiso { get; protected set; }
        public string Nombre { get; protected set; }
        public bool Asignado { get; set; }

        public abstract void AgregarHijo(PermisoComponente componente);

        public abstract void EliminarHijo(PermisoComponente componente);

        public abstract List<PermisoHoja> Listar();
    }
}
