using System;
using System.Globalization;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using StageUp.BLL;

namespace StageUp.UI.Interno
{
    public partial class IniciarSesionInterno : Page
    {
        private readonly BLL_UsuarioInterno _bllUsuarioInterno = new BLL_UsuarioInterno();

        protected string ClaveSitioRecaptcha
        {
            get
            {
                string claveEntorno = Environment.GetEnvironmentVariable("STAGEUP_RECAPTCHA_SITE_KEY");
                string clave = string.IsNullOrWhiteSpace(claveEntorno)
                    ? WebConfigurationManager.AppSettings["RecaptchaSiteKey"]
                    : claveEntorno;
                return HttpUtility.HtmlAttributeEncode(clave ?? string.Empty);
            }
        }

        protected string CodigoIdiomaRecaptcha
        {
            get { return HttpUtility.UrlEncode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Ítems 28/29 del checklist de correcciones: se retira esta
            // pantalla como punto de ingreso propio. Todo el login (externo
            // e interno) pasa ahora por la pantalla única IniciarSesion.aspx;
            // esta URL se mantiene solo porque muchas páginas internas
            // todavía redirigen acá cuando no hay sesión iniciada
            // (GestorDeSesion.EstaAutenticadoComoInterno() == false), así que
            // simplemente rebota a la pantalla unificada sin mostrar ningún
            // formulario propio.
            Response.Redirect("~/IniciarSesion.aspx");
        }

        protected void btnIniciarSesionInterno_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            ResultadoOperacion<StageUp.BE.Entidades.UsuarioInterno> resultado =
                _bllUsuarioInterno.IniciarSesion(
                    txtCorreoInterno.Text,
                    txtPasswordInterno.Text,
                    Request.Form["g-recaptcha-response"]);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje);
                return;
            }

            Response.Redirect("~/Interno/PanelAdministrador.aspx");
        }

        private void MostrarMensaje(string mensaje)
        {
            litMensajeLoginInterno.Text = mensaje;
            pnlMensajeLoginInterno.CssClass = "form-message form-message-error";
            pnlMensajeLoginInterno.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
