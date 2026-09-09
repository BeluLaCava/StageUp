using System.Collections.Generic;

namespace StageUp.BE.Menu
{
    public abstract class ComponenteMenu
    {
        public string Nombre { get; protected set; }

        public abstract void Agregar(ComponenteMenu componente);

        public abstract List<ItemMenu> ObtenerItems();
    }
}
