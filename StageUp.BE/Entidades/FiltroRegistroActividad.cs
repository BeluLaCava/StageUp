using System;

namespace StageUp.BE.Entidades
{
    public class FiltroRegistroActividad
    {
        public int? IdUsuarioExternoResponsable { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string TipoOperacion { get; set; }
        public string TipoEntidadAfectada { get; set; }




        public int? IdUsuarioInternoResponsable { get; set; }
        public string TextoResponsable { get; set; }
        public string TipoResponsable { get; set; }
        public int Maximo { get; set; } = 500;
    }
}
