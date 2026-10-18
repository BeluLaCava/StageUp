using System;

namespace StageUp.BE.Entidades
{
    // Categoría de novedades y del newsletter (script 85). Administrable:
    // reemplaza a los valores fijos que había en el código.
    public class CategoriaNovedad
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int Orden { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }

        // Solo informativos (listado de administración).
        public int CantidadNovedades { get; set; }
        public int CantidadSuscriptores { get; set; }
    }
}
