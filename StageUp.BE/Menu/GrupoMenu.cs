using System.Collections.Generic;

namespace StageUp.BE.Menu
{
    public class GrupoMenu : ComponenteMenu
    {
        private readonly List<ComponenteMenu> _hijos = new List<ComponenteMenu>();

        public GrupoMenu(string nombre)
        {
            Nombre = nombre;
        }

        public IReadOnlyList<ComponenteMenu> Hijos
        {
            get { return _hijos; }
        }

        public override void Agregar(ComponenteMenu componente)
        {
            _hijos.Add(componente);
        }

        public override List<ItemMenu> ObtenerItems()
        {
            List<ItemMenu> items = new List<ItemMenu>();
            foreach (ComponenteMenu hijo in _hijos)
            {
                items.AddRange(hijo.ObtenerItems());
            }
            return items;
        }
    }
}
