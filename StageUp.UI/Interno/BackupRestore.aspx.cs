using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

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

            // La base cambió por completo (el usuario puede no existir en el
            // backup restaurado): se cierra la sesión y se pide volver a entrar.
            IdBackupARestaurar = null;
            GestorDeSesion.CerrarSesionInterna();
            MostrarMensaje(resultado.Mensaje, false);
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
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return false;
            }

            return true;
        }
    }
}
