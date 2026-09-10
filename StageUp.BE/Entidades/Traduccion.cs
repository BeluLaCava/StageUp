using System;

namespace StageUp.BE.Entidades
{
    public class Traduccion
    {
        public int? IdTraduccion { get; set; }
        public int IdIdioma { get; set; }
        public int IdEtiquetaTraduccion { get; set; }
        public string ClaveEtiqueta { get; set; }
        public string TextoPredeterminado { get; set; }
        public string TextoTraducido { get; set; }
        public string Modulo { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }

        public bool TieneTraduccion
        {
            get { return !string.IsNullOrWhiteSpace(TextoTraducido); }
        }
    }
}
