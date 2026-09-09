using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    public class EspacioArtistico
    {
        public FichaEspacio Ficha { get; set; } = new FichaEspacio();
        public int IdEspacioArtistico { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string NombreEspacio { get; set; }
        public string Descripcion { get; set; }
        public string TipoEspacio { get; set; }
        public string EstadoEspacio { get; set; }
        public bool Publicado { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
    }

    public class FichaEspacio
    {
        public string FotoRuta { get; set; }
        public string Provincia { get; set; }
        public string Ciudad { get; set; }
        public string Direccion { get; set; }
        public int? CapacidadMaxima { get; set; }
        public decimal? PrecioHora { get; set; }
        public string Moneda { get; set; } = "ARS";
        public string TipoPiso { get; set; }
        public string DetalleEquipamiento { get; set; }
        public List<string> Equipamiento { get; set; } = new List<string>();
        public List<FranjaEspacio> Disponibilidad { get; set; } = new List<FranjaEspacio>();
    }

    public class FranjaEspacio
    {
        public int? DiaSemana { get; set; }
        public string Fecha { get; set; }
        public int MinutoDesde { get; set; }
        public int MinutoHasta { get; set; }
        public bool Bloqueado { get; set; }
    }
}
