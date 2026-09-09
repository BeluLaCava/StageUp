using System;
using System.Web.UI;
using StageUp.BLL;

namespace StageUp.UI.Interno
{
    public partial class IniciarSesionInterno : Page
    {
        private readonly BLL_UsuarioInterno _bllUsuarioInterno = new BLL_UsuarioInterno();

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnIniciarSesionInterno_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            ResultadoOperacion<StageUp.BE.Entidades.UsuarioInterno> resultado =
                _bllUsuarioInterno.IniciarSesion(txtCorreoInterno.Text, txtPasswordInterno.Text);

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
