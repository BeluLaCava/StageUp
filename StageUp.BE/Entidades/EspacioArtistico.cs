using System;

namespace StageUp.BE.Entidades
{
    public class EspacioArtistico
    {
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
}
