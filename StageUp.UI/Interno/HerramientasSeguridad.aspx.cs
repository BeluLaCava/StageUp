using System;
using System.IO;
using System.Text;
using System.Web.UI;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class HerramientasSeguridad : Page
    {
        private readonly BLL_ExportacionSegura _bllExportacion = new BLL_ExportacionSegura();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_ENCRIPTACION"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return;
            }

            if (!IsPostBack)
            {
                ActualizarEstadoClaves();
            }
        }

        protected void btnGenerarClaves_Click(object sender, EventArgs e)
        {
            string claveBase64Publica;
            string claveBase64Privada;
            _bllExportacion.GenerarNuevoParDeClaves(out claveBase64Publica, out claveBase64Privada);

            txtClavePublicaGenerada.Text = claveBase64Publica;
            txtClavePrivadaGenerada.Text = claveBase64Privada;
            pnlClavesGeneradas.Visible = true;

            ActualizarEstadoClaves();
        }

        protected void btnDescifrar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            pnlResultadoDescifrado.Visible = false;

            if (!fuArchivoCifrado.HasFile)
            {
                MostrarMensaje("Elegí un archivo exportado antes de descifrar.", esError: true);
                return;
            }

            string envoltorioXml;
            using (StreamReader lector = new StreamReader(fuArchivoCifrado.FileContent, Encoding.UTF8))
            {
                envoltorioXml = lector.ReadToEnd();
            }

            ResultadoOperacion<string> resultado = _bllExportacion.ImportarYDescifrar(envoltorioXml);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                return;
            }

            txtXmlDescifrado.Text = resultado.Valor;
            pnlResultadoDescifrado.Visible = true;
        }

        private void ActualizarEstadoClaves()
        {
            litEstadoClaves.Text = _bllExportacion.HayClavesConfiguradas()
                ? "configurado."
                : "todavía no se generó ningún par de claves.";
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
