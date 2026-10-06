using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // CU-001-008 Gestionar disponibilidad de espacios artísticos: lo que se
    // muestra en la pantalla "Disponibilidad del espacio".

    // Un día del calendario con todo lo que pasa en el espacio ese día.
    public class DiaCalendarioDisponibilidad
    {
        public DateTime Fecha { get; set; }
        public bool EsHoy { get; set; }

        // true cuando ese día rige una franja de fecha concreta (excepción)
        // en lugar del horario semanal habitual.
        public bool TieneExcepcion { get; set; }

        public List<ItemCalendarioDisponibilidad> Items { get; set; } = new List<ItemCalendarioDisponibilidad>();
    }

    public class ItemCalendarioDisponibilidad
    {
        public const string TipoDisponible = "Disponible";
        public const string TipoBloqueo = "Bloqueo";
        public const string TipoActividad = "Actividad";
        public const string TipoReservaAceptada = "ReservaAceptada";
        public const string TipoSolicitudPendiente = "SolicitudPendiente";

        public string Tipo { get; set; }
        public int MinutoDesde { get; set; }
        public int MinutoHasta { get; set; }
        public string Detalle { get; set; }
        public int? IdReserva { get; set; }
    }

    // Operaciones (reservas aceptadas, solicitudes pendientes y actividades
    // internas) asociadas a una franja: se muestran en su detalle (A6/A10)
    // y deciden si se puede modificar o eliminar (A7/A11).
    public class OperacionesAsociadasFranja
    {
        public int ReservasAceptadas { get; set; }
        public int SolicitudesPendientes { get; set; }
        public int Actividades { get; set; }

        public bool HayReservasOSolicitudes
        {
            get { return ReservasAceptadas + SolicitudesPendientes > 0; }
        }

        public bool HayAlguna
        {
            get { return ReservasAceptadas + SolicitudesPendientes + Actividades > 0; }
        }
    }
}
