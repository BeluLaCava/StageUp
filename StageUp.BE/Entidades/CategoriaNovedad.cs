using System;

namespace StageUp.BE.Entidades
{


    public class CategoriaNovedad
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int Orden { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }


        public int CantidadNovedades { get; set; }
        public int CantidadSuscriptores { get; set; }
    }
}
