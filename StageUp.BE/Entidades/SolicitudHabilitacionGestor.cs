using System;

namespace StageUp.BE.Entidades
{


    public class SolicitudHabilitacionGestor
    {
        public const string EstadoPendienteRevision = "PendienteRevision";
        public const string EstadoAprobada = "Aprobada";
        public const string EstadoRechazada = "Rechazada";

        public int IdSolicitud { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string Estado { get; set; }


        public string NombreResponsable { get; set; }
        public string DocumentoResponsable { get; set; }


        public string TelefonoContacto { get; set; }
        public string CorreoContacto { get; set; }


        public string CondicionFiscal { get; set; }
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }


        public string NombreEspacio { get; set; }
        public string TipoEspacio { get; set; }
        public string Provincia { get; set; }
        public string Ciudad { get; set; }
        public string DescripcionPropuesta { get; set; }


        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRevision { get; set; }
        public int? IdUsuarioInternoRevisor { get; set; }
        public string MotivoRechazo { get; set; }


        public string NombreUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string NombreRevisor { get; set; }
    }
}
