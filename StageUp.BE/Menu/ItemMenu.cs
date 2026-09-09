using System;
using System.Collections.Generic;

namespace StageUp.BE.Menu
{
    public class ItemMenu : ComponenteMenu
    {
        public string Url { get; private set; }
        public string Descripcion { get; private set; }

        public ItemMenu(string nombre, string url, string descripcion)
        {
            Nombre = nombre;
            Url = url;
            Descripcion = descripcion;
        }

        public override void Agregar(ComponenteMenu componente)
        {
            throw new NotSupportedException();
        }

        public override List<ItemMenu> ObtenerItems()
        {
            return new List<ItemMenu> { this };
        }
    }
}
