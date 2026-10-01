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

        // CU-001-013: búsqueda por responsable (nombre o correo, usuario
        // externo o interno) y tope de filas (ítem 36: la bitácora no se
        // carga entera).
        public int? IdUsuarioInternoResponsable { get; set; }
        public string TextoResponsable { get; set; }
        public string TipoResponsable { get; set; }   // Externo | Interno | null = ambos
        public int Maximo { get; set; } = 500;
    }
}
