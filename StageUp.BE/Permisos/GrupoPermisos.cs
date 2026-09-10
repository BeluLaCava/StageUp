using System.Collections.Generic;

namespace StageUp.BE.Permisos
{
    public class GrupoPermisos : PermisoComponente
    {
        private readonly List<PermisoComponente> _hijos = new List<PermisoComponente>();

        public GrupoPermisos(int idComponentePermiso, string nombre)
        {
            IdComponentePermiso = idComponentePermiso;
            Nombre = nombre;
        }

        public IReadOnlyList<PermisoComponente> Hijos
        {
            get { return _hijos; }
        }

        public override void AgregarHijo(PermisoComponente componente)
        {
            _hijos.Add(componente);
        }

        public override void EliminarHijo(PermisoComponente componente)
        {
            _hijos.Remove(componente);
        }

        public override List<PermisoHoja> Listar()
        {
            var hojas = new List<PermisoHoja>();
            foreach (PermisoComponente hijo in _hijos)
            {
                hojas.AddRange(hijo.Listar());
            }
            return hojas;
        }
    }
}
