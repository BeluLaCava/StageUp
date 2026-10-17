using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI
{
    // Newsletter (script 78), sin necesidad de cuenta:
    //   Newsletter.aspx                     -> formulario de suscripción
    //   Newsletter.aspx?confirmar={token}   -> doble confirmación
    //   Newsletter.aspx?token={token}       -> preferencias (categorías, baja)
    //   Newsletter.aspx?token={token}&baja=1 -> preferencias con la baja a
    //       la vista (enlace "Darme de baja" de cada correo). La baja nunca
    //       se hace solo por abrir el enlace: hay que confirmarla.
    public partial class Newsletter : Page
    {
        private readonly BLL_Newsletter _bllNewsletter = new BLL_Newsletter();

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

        private string TokenPreferencias
        {
            get { return ViewState["TokenPreferencias"] as string; }
            set { ViewState["TokenPreferencias"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los enlaces llevan un token: que no viaje como referencia a otros sitios.
            Response.AppendHeader("Referrer-Policy", "no-referrer");

            if (IsPostBack)
            {
                return;
            }

            CargarCategorias(cblCategorias);
            CargarCategorias(cblPreferencias);
            foreach (ListItem item in cblCategorias.Items)
            {
                item.Selected = true;
            }

            string confirmar = Request.QueryString["confirmar"];
            string token = Request.QueryString["token"];
            if (!string.IsNullOrEmpty(confirmar))
            {
                Confirmar(confirmar);
            }
            else if (!string.IsNullOrEmpty(token))
            {
                TokenPreferencias = token;
                MostrarPreferencias(Request.QueryString["baja"] == "1");
            }
            else
            {
                MostrarSolo(pnlSuscribir);
            }
        }

        protected void btnSuscribir_Click(object sender, EventArgs e)
        {
            ResultadoOperacion resultado = _bllNewsletter.Suscribir(
                txtCorreo.Text, txtNombre.Text, Elegidas(cblCategorias), chkAcepto.Checked,
                Request.Form["g-recaptcha-response"], UrlBase());

            if (!resultado.Exitoso)
            {
                MostrarSolo(pnlSuscribir);
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            MostrarSolo(null);
            MostrarMensaje(resultado.Mensaje, false);
        }

        protected void btnGuardarPreferencias_Click(object sender, EventArgs e)
        {
            ResultadoOperacion resultado = _bllNewsletter.GuardarPreferencias(TokenPreferencias, Elegidas(cblPreferencias));
            MostrarPreferencias(false);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }

        protected void btnBaja_Click(object sender, EventArgs e)
        {
            ResultadoOperacion resultado = _bllNewsletter.DarDeBaja(TokenPreferencias);
            MostrarPreferencias(false);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }

        protected void btnReactivar_Click(object sender, EventArgs e)
        {
            ResultadoOperacion resultado = _bllNewsletter.Reactivar(TokenPreferencias);
            MostrarPreferencias(false);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }

        private void Confirmar(string token)
        {
            ResultadoOperacion<SuscriptorNewsletter> resultado = _bllNewsletter.Confirmar(token);
            if (!resultado.Exitoso)
            {
                MostrarSolo(pnlSuscribir);
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            MostrarSolo(pnlConfirmada);
            lnkPreferenciasConfirmada.NavigateUrl = "~/Newsletter.aspx?token=" + HttpUtility.UrlEncode(resultado.Valor.TokenPreferencias);
            MostrarMensaje(resultado.Mensaje, false);
        }

        private void MostrarPreferencias(bool destacarBaja)
        {
            ResultadoOperacion<SuscriptorNewsletter> resultado = _bllNewsletter.ObtenerPorToken(TokenPreferencias);
            if (!resultado.Exitoso)
            {
                MostrarSolo(pnlSuscribir);
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            SuscriptorNewsletter suscriptor = resultado.Valor;
            MostrarSolo(pnlPreferencias);
            litCorreo.Text = suscriptor.CorreoEnmascarado;
            pnlActiva.Visible = suscriptor.Estado == SuscriptorNewsletter.EstadoActivo;
            pnlInactiva.Visible = suscriptor.Estado == SuscriptorNewsletter.EstadoBaja;
            pnlPendiente.Visible = suscriptor.Estado == SuscriptorNewsletter.EstadoPendiente;
            pnlBajaDestacada.Visible = destacarBaja && pnlActiva.Visible;

            foreach (ListItem item in cblPreferencias.Items)
            {
                item.Selected = suscriptor.Categorias.Count == 0 || suscriptor.Categorias.Contains(item.Value);
            }
        }

        private static void CargarCategorias(CheckBoxList lista)
        {
            lista.Items.Clear();
            foreach (KeyValuePair<string, string> categoria in BLL_Busqueda.CategoriasNovedad)
            {
                lista.Items.Add(new ListItem(categoria.Value, categoria.Key));
            }
        }

        private static List<string> Elegidas(CheckBoxList lista)
        {
            return lista.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList();
        }

        private string UrlBase()
        {
            return new Uri(Request.Url, ResolveUrl("~/")).ToString();
        }

        // Solo se muestra uno de los paneles (o ninguno, tras suscribirse).
        private void MostrarSolo(Panel visible)
        {
            pnlSuscribir.Visible = ReferenceEquals(visible, pnlSuscribir);
            pnlConfirmada.Visible = ReferenceEquals(visible, pnlConfirmada);
            pnlPreferencias.Visible = ReferenceEquals(visible, pnlPreferencias);
            phScriptRecaptcha.Visible = pnlSuscribir.Visible;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            if (string.IsNullOrEmpty(mensaje))
            {
                return;
            }

            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlMensaje.Visible = true;
        }
    }
}
