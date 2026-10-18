using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno
{
    // Backup y restore de la base (ítem 17 de la segunda entrega).
    public partial class BackupRestore : Page
    {
        private readonly BLL_Backup _bllBackup = new BLL_Backup();

        private int? IdBackupARestaurar
        {
            get { return ViewState["IdBackupARestaurar"] as int?; }
            set { ViewState["IdBackupARestaurar"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarHistorial();
            }
        }

        protected void btnGenerar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion<BackupBaseDatos> resultado = _bllBackup.GenerarBackup(txtDescripcion.Text, idResponsable);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                txtDescripcion.Text = string.Empty;
            }

            CargarHistorial();
        }

        protected void rptBackups_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || e.CommandName != "Restaurar")
            {
                return;
            }

            int idBackup = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            BackupBaseDatos backup = _bllBackup.ListarHistorial().FirstOrDefault(b => b.IdBackup == idBackup);
            if (backup == null)
            {
                MostrarMensaje("El backup seleccionado ya no figura en el historial.", true);
                CargarHistorial();
                return;
            }

            IdBackupARestaurar = idBackup;
            litBackupElegido.Text = Server.HtmlEncode(backup.NombreArchivo +
                (backup.Fecha.HasValue ? " (" + backup.Fecha.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + ")" : string.Empty));
            txtConfirmacion.Text = string.Empty;
            pnlConfirmarRestauracion.Visible = true;
            CargarHistorial();
        }

        protected void btnConfirmarRestauracion_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdBackupARestaurar.HasValue)
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllBackup.Restaurar(IdBackupARestaurar.Value, txtConfirmacion.Text, idResponsable);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarHistorial();
                return;
            }

            // La base cambió por completo: la sesión se vuelve a validar
            // contra la base restaurada (script 79). Sigue abierta si el
            // usuario existe, es el mismo y está activo en el backup, con los
            // permisos que tiene ahí.
            IdBackupARestaurar = null;
            txtConfirmacion.Text = string.Empty;
            ResultadoRevalidacionSesion sesion = ControlSesionBaseDatos.VerificarSinExcepciones();
            GestorDeSesion.TomarAvisoRestauracion();

            // No se pudo comprobar la cuenta contra la base restaurada: la
            // sesión queda suspendida y no se usan los permisos anteriores.
            if (sesion == ResultadoRevalidacionSesion.NoVerificada)
            {
                MostrarRestaurado(resultado.Mensaje,
                    "La base de datos se restauró correctamente, pero no se pudo comprobar tu usuario contra la base restaurada. " +
                    "Hasta comprobarlo, el panel queda bloqueado y no se usan tus permisos anteriores. Volvé a intentar en unos segundos.",
                    "~/Interno/PanelAdministrador.aspx", "Volver a intentar");
                return;
            }

            if (sesion == ResultadoRevalidacionSesion.Invalida)
            {
                MostrarRestaurado(resultado.Mensaje,
                    "La base de datos se restauró correctamente. Tu usuario no figura activo en el backup restaurado, por eso se cerró tu sesión. " +
                    "Iniciá sesión con un usuario interno que exista en esa copia.",
                    "~/IniciarSesion.aspx", "Iniciar sesión");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_BACKUP"))
            {
                MostrarRestaurado(resultado.Mensaje,
                    "La base de datos se restauró correctamente y tu sesión sigue abierta, pero en el backup restaurado tu rol no tiene permiso para gestionar backups.",
                    "~/Interno/PanelAdministrador.aspx", "Ir al panel");
                return;
            }

            MostrarMensaje(resultado.Mensaje + " Tu sesión sigue abierta: tu usuario existe y está activo en el backup restaurado, y se actualizaron tus permisos.", false);
            pnlConfirmarRestauracion.Visible = false;
            CargarHistorial();
        }

        private void MostrarRestaurado(string mensaje, string detalle, string urlContinuar, string textoContinuar)
        {
            MostrarMensaje(mensaje, false);
            litRestaurado.Text = Server.HtmlEncode(detalle);
            lnkContinuarRestaurado.NavigateUrl = urlContinuar;
            lnkContinuarRestaurado.Text = textoContinuar;
            pnlContenido.Visible = false;
            pnlRestaurado.Visible = true;
        }

        protected void lnkCancelarRestauracion_Click(object sender, EventArgs e)
        {
            IdBackupARestaurar = null;
            pnlConfirmarRestauracion.Visible = false;
            CargarHistorial();
        }

        protected static string FormatearTamanio(object bytes)
        {
            double tamanio = Convert.ToDouble(bytes, CultureInfo.InvariantCulture);
            if (tamanio >= 1024 * 1024)
            {
                return (tamanio / (1024 * 1024)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            }

            return (tamanio / 1024).ToString("0", CultureInfo.InvariantCulture) + " KB";
        }

        private void CargarHistorial()
        {
            List<BackupBaseDatos> backups = _bllBackup.ListarHistorial();
            pnlSinBackups.Visible = backups.Count == 0;
            rptBackups.DataSource = backups;
            rptBackups.DataBind();
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_BACKUP"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
