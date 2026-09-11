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

        public string NombreGestor { get; set; }
        public string ApellidoGestor { get; set; }
        public DateTime? GestorDesde { get; set; }
        public int CantidadEspaciosPublicadosGestor { get; set; }
        public decimal PromedioCalificacion { get; set; }
        public int CantidadCalificaciones { get; set; }

        public string NombreCompletoGestor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NombreGestor) && string.IsNullOrWhiteSpace(ApellidoGestor))
                    return null;
                return (NombreGestor + " " + ApellidoGestor).Trim();
            }
        }
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

        public List<string> Fotos { get; set; } = new List<string>();
    }

    public class FranjaEspacio
    {
        public int? DiaSemana { get; set; }
        public string Fecha { get; set; }
        public int MinutoDesde { get; set; }
        public int MinutoHasta { get; set; }
        public bool Bloqueado { get; set; }
    }

    public class FiltroBusquedaEspacios
    {
        public string TextoBusqueda { get; set; }
        public string TipoEspacio { get; set; }

        public string Ubicacion { get; set; }
        public decimal? PrecioMaximo { get; set; }
        public int? CapacidadMinima { get; set; }
        public string TipoPiso { get; set; }

        public string FechaDisponibilidad { get; set; }
        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }

        public List<string> Equipamiento { get; set; } = new List<string>();
    }
}
