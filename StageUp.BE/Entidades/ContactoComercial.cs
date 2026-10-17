using System;

namespace StageUp.BE.Entidades
{
    // CU-004-002 Contacto comercial. No es un usuario de StageUp: cuando se
    // incorpora a la plataforma se lo puede asociar con su cuenta.
    public class ContactoComercial
    {
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoContactado = "Contactado";
        public const string EstadoInteresado = "Interesado";
        public const string EstadoRechazado = "Rechazado";
        public const string EstadoIncorporado = "Incorporado";

        public int IdContactoComercial { get; set; }
        public string NombreContacto { get; set; }
        public string ApellidoContacto { get; set; }
        public string EntidadRepresentada { get; set; }
        public string TipoEntidad { get; set; }
        public string MedioContacto { get; set; }
        public string CorreoElectronico { get; set; }
        public string Telefono { get; set; }
        public string TelefonoNormalizado { get; set; }
        public string OtroDatoContacto { get; set; }
        public int IdOrigenUsuario { get; set; }
        public string NombreOrigen { get; set; }
        public string Observaciones { get; set; }
        public string EstadoContacto { get; set; }
        public int? IdCampaniaComercial { get; set; }
        public string NombreCampania { get; set; }
        public int? IdAlianzaEstrategica { get; set; }
        public string NombreAlianza { get; set; }
        public int? IdUsuarioExterno { get; set; }
        public string NombreUsuarioExterno { get; set; }
        public string CorreoUsuarioExterno { get; set; }
        public DateTime? FechaIncorporacion { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
        public int CantidadSeguimientos { get; set; }

        // Observación del cambio de estado hecho desde la edición (A8).
        public string ObservacionCambioEstado { get; set; }

        public string NombreCompleto
        {
            get { return string.IsNullOrWhiteSpace(ApellidoContacto) ? NombreContacto : NombreContacto + " " + ApellidoContacto; }
        }
    }

    // Observación de seguimiento de un contacto (A7) o cambio de estado (A8).
    public class SeguimientoContacto
    {
        public const string TipoCambioEstado = "CambioEstado";

        public int IdSeguimientoContacto { get; set; }
        public int IdContactoComercial { get; set; }
        public DateTime FechaSeguimiento { get; set; }
        public string TipoSeguimiento { get; set; }
        public string Descripcion { get; set; }
        public string Resultado { get; set; }
        public string InteresManifestado { get; set; }
        public string ProximaAccion { get; set; }
        public DateTime? FechaProximaAccion { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoResultante { get; set; }
        public int IdUsuarioInternoResponsable { get; set; }
        public string NombreResponsable { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class FiltroContactoComercial
    {
        public string Estado { get; set; }
        public string TipoEntidad { get; set; }
        public int? IdOrigenUsuario { get; set; }
        public int? IdCampaniaComercial { get; set; }
        public string Texto { get; set; }

        public bool TieneCriterios
        {
            get { return Estado != null || TipoEntidad != null || IdOrigenUsuario.HasValue || IdCampaniaComercial.HasValue || Texto != null; }
        }
    }

    public class ResumenContactos
    {
        public int Total { get; set; }
        public int Pendientes { get; set; }
        public int Contactados { get; set; }
        public int Interesados { get; set; }
        public int Rechazados { get; set; }
        public int Incorporados { get; set; }
    }

    // A13: contacto ya registrado con datos equivalentes.
    public class PosibleContactoDuplicado
    {
        public int IdContactoComercial { get; set; }
        public string Nombre { get; set; }
        public string Coincidencia { get; set; }
    }

    // A9: cuenta de StageUp con la que se asocia un contacto incorporado.
    public class CuentaParaVincular
    {
        public int IdUsuarioExterno { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string CorreoElectronico { get; set; }
        public string PerfilUsuario { get; set; }
        public string EstadoCuenta { get; set; }
    }
}
