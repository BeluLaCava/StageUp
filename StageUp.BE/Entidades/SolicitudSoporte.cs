using System;

namespace StageUp.BE.Entidades
{
    // CU-003-002 pasos 3 a 8: datos del formulario "Nueva solicitud de soporte".
    public class SolicitudSoporteNueva
    {
        public int IdUsuarioExterno { get; set; }
        public string Motivo { get; set; }
        public string FuncionalidadVinculada { get; set; }
        public string Descripcion { get; set; }
        public string InformacionAdicional { get; set; }
        public int? IdReservaAsociada { get; set; }
        public string Origen { get; set; }
    }

    // Historial del caso: alta, cambios de estado (con estado anterior,
    // estado nuevo, responsable, fecha y hora) y asignaciones.
    public class TicketActualizacion
    {
        public int IdTicketActualizacion { get; set; }
        public int IdTicket { get; set; }

        // Alta, CambioEstado o Asignacion.
        public string TipoActualizacion { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; }
        public int? IdUsuarioInterno { get; set; }
        public int? IdUsuarioExterno { get; set; }
        public string NombreResponsable { get; set; }
        public bool EsInterno { get; set; }
        public string Detalle { get; set; }
        public DateTime Fecha { get; set; }
        public bool EsNuevo { get; set; }
    }

    // CU-003-003 paso 6 y A3: filtros del listado interno. Null = sin filtrar.
    public class FiltroTicket
    {
        public const string AsignacionSinAsignar = "SinAsignar";
        public const string AsignacionUsuario = "Usuario";

        public string Estado { get; set; }
        public string FuncionalidadVinculada { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Usuario { get; set; }
        public int? IdTicket { get; set; }
        public string Asignacion { get; set; }
        public int? IdUsuarioInternoAsignado { get; set; }
        public bool SoloNoLeidas { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanioPagina { get; set; } = 20;

        public bool TieneCriterios
        {
            get
            {
                return Estado != null || FuncionalidadVinculada != null || Desde.HasValue || Hasta.HasValue ||
                    Usuario != null || IdTicket.HasValue || Asignacion != null || SoloNoLeidas;
            }
        }
    }

    // Resumen del panel interno de solicitudes de soporte.
    public class ResumenSoporte
    {
        public int Total { get; set; }
        public int Pendientes { get; set; }
        public int EnProceso { get; set; }
        public int Resueltas { get; set; }
        public int Cerradas { get; set; }
        public int SinLeer { get; set; }
        public int SinAsignar { get; set; }
    }

    // Página del listado interno.
    public class PaginaTickets
    {
        public System.Collections.Generic.List<Ticket> Tickets { get; set; } = new System.Collections.Generic.List<Ticket>();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanioPagina { get; set; }

        public int CantidadPaginas
        {
            get { return TamanioPagina <= 0 ? 1 : Math.Max(1, (Total + TamanioPagina - 1) / TamanioPagina); }
        }
    }
}
