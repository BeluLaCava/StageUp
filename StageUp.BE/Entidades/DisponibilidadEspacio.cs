using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{




    public class DiaCalendarioDisponibilidad
    {
        public DateTime Fecha { get; set; }
        public bool EsHoy { get; set; }



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
