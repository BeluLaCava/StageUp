using System;
using System.Globalization;

namespace StageUp.BE.Entidades
{



    public class Ticket
    {
        public int IdTicket { get; set; }
        public int IdUsuarioExterno { get; set; }
        public int? IdReservaAsociada { get; set; }


        public string FuncionalidadVinculada { get; set; }
        public string Motivo { get; set; }
        public string Descripcion { get; set; }
        public string InformacionAdicional { get; set; }


        public string Origen { get; set; }


        public string Estado { get; set; }
        public int? IdUsuarioInternoAsignado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaActividad { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime? FechaCierre { get; set; }



        public DateTime? FechaUltimaLecturaUsuario { get; set; }
        public DateTime? FechaUltimaLecturaSoporte { get; set; }


        public string NombreUsuarioExterno { get; set; }
        public string CorreoUsuarioExterno { get; set; }
        public string NombreUsuarioInternoAsignado { get; set; }
        public int CantidadRespuestas { get; set; }


        public int NovedadesUsuario { get; set; }



        public int NovedadesSoporte { get; set; }


        public string NombreEspacioReserva { get; set; }
        public DateTime? FechaReserva { get; set; }
        public int? MinutoDesdeReserva { get; set; }
        public int? MinutoHastaReserva { get; set; }
        public string EstadoReserva { get; set; }

        public bool TieneReservaAsociada
        {
            get { return IdReservaAsociada.HasValue; }
        }


        public string Codigo
        {
            get { return FormatearCodigo(IdTicket); }
        }

        public static string FormatearCodigo(int idTicket)
        {
            return "SOP-" + idTicket.ToString("D6", CultureInfo.InvariantCulture);
        }
    }
}
