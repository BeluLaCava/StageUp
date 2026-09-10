using System;

namespace StageUp.BE.Entidades
{
    public class Calificacion
    {
        public int IdCalificacion { get; set; }
        public int IdReserva { get; set; }
        public int IdUsuarioAutor { get; set; }
        public string TipoCalificacion { get; set; }
        public int Puntaje { get; set; }
        public string Comentario { get; set; }
        public DateTime FechaAlta { get; set; }
        public bool Activo { get; set; }
        public int IdEspacioArtistico { get; set; }
        public int? IdUsuarioEvaluado { get; set; }
        public string NombreEspacio { get; set; }
        public string NombreAutor { get; set; }
        public string NombreEvaluado { get; set; }
        public DateTime FechaReserva { get; set; }
    }

    public class ResumenReputacion
    {
        public int IdReferencia { get; set; }
        public decimal Promedio { get; set; }
        public int CantidadCalificaciones { get; set; }
    }
}
