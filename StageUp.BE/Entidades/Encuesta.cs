using System;

namespace StageUp.BE.Entidades
{
    public class Encuesta
    {
        public int IdEncuesta { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string PublicoObjetivo { get; set; }
        public string Estado { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }

        // No hay un estado "Vencida" persistido (no hay SQL Server Agent en
        // la edición Express para recalcularlo con un job). Se calcula acá,
        // comparando la fecha de vencimiento contra la hora actual, igual
        // criterio que ya se usa para los recordatorios de reserva.
        public bool EstaVencida
        {
            get { return FechaVencimiento < DateTime.Now; }
        }

        public bool EstaVigente
        {
            get { return Estado == "Activa" && FechaInicio <= DateTime.Now && !EstaVencida; }
        }
    }
}
