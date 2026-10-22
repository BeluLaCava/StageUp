using System;

namespace StageUp.BE.Entidades
{




    public class OpcionMenu
    {
        public int IdOpcionMenu { get; set; }
        public string Texto { get; set; }
        public string Descripcion { get; set; }
        public string Url { get; set; }
        public string Modulo { get; set; }
        public int Orden { get; set; }
        public int IdComponentePermiso { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }


        public string CodigoPermiso { get; set; }
        public string NombrePermiso { get; set; }
    }
}
