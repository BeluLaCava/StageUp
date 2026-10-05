using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    public class UsuarioExterno
    {
        public int IdUsuarioExterno { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string CorreoElectronico { get; set; }
        public string PasswordHash { get; set; }
        public string Telefono { get; set; }
        public string EstadoCuenta { get; set; }
        public string PerfilUsuario { get; set; }
        public string FotoPerfilRuta { get; set; }
        public string DescripcionPerfil { get; set; }
        public bool AceptaTerminos { get; set; }
        public bool AceptaPoliticaPrivacidad { get; set; }
        public DateTime? FechaAceptacionTerminos { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaActivacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public bool Activo { get; set; }
    }

    // CU-001-003 (A8/A9): resultado de evaluar o aplicar la baja lógica de
    // una cuenta externa (sp_UsuarioExterno_BajaLogica). Condiciones y Avisos
    // los arma la BLL con textos para mostrar al usuario.
    public class EvaluacionBajaCuenta
    {
        public string Resultado { get; set; }
        public int ReservasPendientes { get; set; }
        public int ReservasAceptadas { get; set; }
        public int PagosPendientes { get; set; }
        public int SolicitudesRecibidas { get; set; }
        public int ReservasRecibidasAceptadas { get; set; }
        public int CuentasConSaldo { get; set; }
        public int EspaciosPublicados { get; set; }
        public int TicketsAbiertos { get; set; }
        public int EspaciosPausados { get; set; }

        public List<string> Condiciones { get; set; } = new List<string>();
        public List<string> Avisos { get; set; } = new List<string>();

        public bool PuedeDarseDeBaja
        {
            get { return Resultado == "OK"; }
        }
    }
}
