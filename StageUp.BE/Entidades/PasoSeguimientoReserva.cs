using System;

namespace StageUp.BE.Entidades
{
    // Un paso de la línea de tiempo de una reserva (ítem 6A de la segunda
    // entrega): solicitud enviada, revisión del gestor, reserva confirmada,
    // día de la reserva, finalizada, calificación (o rechazada / cancelada).
    public class PasoSeguimientoReserva
    {
        public string Titulo { get; set; }
        public string Detalle { get; set; }
        public DateTime? Fecha { get; set; }

        // "Completado", "Actual", "Pendiente" o "Interrumpido" (rechazo o
        // cancelación). La UI lo usa para el color del punto de la línea.
        public string Estado { get; set; }
    }
}
