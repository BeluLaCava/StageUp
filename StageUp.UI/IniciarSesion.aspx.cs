using System;
using System.Web.UI;
using StageUp.BLL;

namespace StageUp.UI
{
    /// <summary>
    /// Code-behind de IniciarSesion.aspx. Resuelve el login (necesario
    /// para poder usar una cuenta ya activada) y, dentro de la misma
    /// página, el flujo completo de CU-001-002 Recuperar acceso a la
    /// cuenta (solicitar código -> validar código y definir nueva
    /// contraseña -> confirmación).
    /// </summary>
    public partial class IniciarSesion : Page
    {
        private readonly BLL_UsuarioExterno _bllUsuarioExterno = new BLL_UsuarioExterno();

        private string CorreoRecuperacionPendiente
        {
            get { return ViewState["CorreoRecuperacionPendiente"] as string; }
            set { ViewState["CorreoRecuperacionPendiente"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnIniciarSesion_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            ResultadoOperacion<StageUp.BE.Entidades.UsuarioExterno> resultado =
                _bllUsuarioExterno.IniciarSesion(txtLoginEmail.Text, txtLoginPassword.Text);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensajeLogin, litMensajeLogin, resultado.Mensaje, esError: true);
                return;
            }

            Response.Redirect("~/Default.aspx");
        }

        protected void lnkOlvideContrasena_Click(object sender, EventArgs e)
        {
            txtRecuperarEmail.Text = txtLoginEmail.Text;
            MostrarPanel(pnlRecuperarSolicitar);
        }

        protected void btnEnviarCodigoRecuperacion_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            ResultadoOperacion resultado = _bllUsuarioExterno.SolicitarRecuperacion(txtRecuperarEmail.Text);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensajeRecuperarSolicitar, litMensajeRecuperarSolicitar, resultado.Mensaje, esError: true);
                return;
            }

            CorreoRecuperacionPendiente = txtRecuperarEmail.Text.Trim();
            MostrarPanel(pnlRecuperarActualizar);
            MostrarMensaje(pnlMensajeRecuperarActualizar, litMensajeRecuperarActualizar, resultado.Mensaje, esError: false);
        }

        protected void btnActualizarPassword_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || string.IsNullOrEmpty(CorreoRecuperacionPendiente))
            {
                return;
            }

            ResultadoOperacion resultado = _bllUsuarioExterno.ValidarCodigoYActualizarPassword(
                CorreoRecuperacionPendiente,
                txtCodigoRecuperacion.Text,
                txtNuevaPassword.Text,
                txtConfirmarNuevaPassword.Text);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensajeRecuperarActualizar, litMensajeRecuperarActualizar, resultado.Mensaje, esError: true);
                return;
            }

            MostrarPanel(pnlRecuperarExito);
        }

        protected void lnkReenviarCodigoRecuperacion_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CorreoRecuperacionPendiente))
            {
                return;
            }

            ResultadoOperacion resultado = _bllUsuarioExterno.SolicitarRecuperacion(CorreoRecuperacionPendiente);
            MostrarMensaje(pnlMensajeRecuperarActualizar, litMensajeRecuperarActualizar, resultado.Mensaje, !resultado.Exitoso);
        }

        protected void lnkVolverALogin_Click(object sender, EventArgs e)
        {
            MostrarPanel(pnlLogin);
        }

        /// <summary>
        /// Muestra únicamente el panel indicado y oculta el resto de los
        /// pasos del flujo de login / recuperación.
        /// </summary>
        private void MostrarPanel(System.Web.UI.WebControls.Panel panelAMostrar)
        {
            pnlLogin.Visible = ReferenceEquals(panelAMostrar, pnlLogin);
            pnlRecuperarSolicitar.Visible = ReferenceEquals(panelAMostrar, pnlRecuperarSolicitar);
            pnlRecuperarActualizar.Visible = ReferenceEquals(panelAMostrar, pnlRecuperarActualizar);
            pnlRecuperarExito.Visible = ReferenceEquals(panelAMostrar, pnlRecuperarExito);
        }

        private static void MostrarMensaje(System.Web.UI.WebControls.Panel panel, Literal literal, string mensaje, bool esError)
        {
            literal.Text = mensaje;
            panel.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            panel.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
