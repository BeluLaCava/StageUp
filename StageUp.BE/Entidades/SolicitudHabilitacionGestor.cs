using System;

namespace StageUp.BE.Entidades
{
    // CU-001-007 (A1 a A6): solicitud de un usuario externo para habilitarse
    // como gestor de espacios artísticos (script 54).
    public class SolicitudHabilitacionGestor
    {
        public const string EstadoPendienteRevision = "PendienteRevision";
        public const string EstadoAprobada = "Aprobada";
        public const string EstadoRechazada = "Rechazada";

        public int IdSolicitud { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string Estado { get; set; }

        // Datos identificatorios del responsable
        public string NombreResponsable { get; set; }
        public string DocumentoResponsable { get; set; }

        // Datos de contacto
        public string TelefonoContacto { get; set; }
        public string CorreoContacto { get; set; }

        // Datos administrativos
        public string CondicionFiscal { get; set; }
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }

        // Datos iniciales del espacio o entidad
        public string NombreEspacio { get; set; }
        public string TipoEspacio { get; set; }
        public string Provincia { get; set; }
        public string Ciudad { get; set; }
        public string DescripcionPropuesta { get; set; }

        // Revisión
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRevision { get; set; }
        public int? IdUsuarioInternoRevisor { get; set; }
        public string MotivoRechazo { get; set; }

        // Datos de lectura (joins)
        public string NombreUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string NombreRevisor { get; set; }
    }
}
