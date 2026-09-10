using System;

namespace StageUp.BE.Entidades
{
    public class Idioma
    {
        public int IdIdioma { get; set; }
        public string CodigoIdioma { get; set; }
        public string NombreIdioma { get; set; }
        public bool EsPredeterminado { get; set; }
        public string EstadoIdioma { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
        public int CantidadEtiquetas { get; set; }
        public int CantidadTraducciones { get; set; }

        public int PorcentajeTraducido
        {
            get
            {
                return CantidadEtiquetas == 0
                    ? 0
                    : (int)Math.Round(CantidadTraducciones * 100m / CantidadEtiquetas);
            }
        }
    }
}
