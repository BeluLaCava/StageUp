using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // Parámetros de la plataforma (script 49, permiso CONFIGURAR_PARAMETROS).
    public partial class ParametrosPlataforma : Page
    {
        private readonly BLL_ParametroPlataforma _bllParametros = new BLL_ParametroPlataforma();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarParametros();
            }
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            Dictionary<string, string> valores = new Dictionary<string, string>();

            foreach (RepeaterItem item in rptParametros.Items)
            {
                HiddenField hfClave = (HiddenField)item.FindControl("hfClave");
                TextBox txtValor = (TextBox)item.FindControl("txtValor");
                valores[hfClave.Value] = txtValor.Text;
            }

            // Se validan y guardan todos juntos: los plazos y porcentajes de
            // cancelación dependen unos de otros.
            ResultadoOperacion resultado = _bllParametros.ActualizarVarios(valores, idResponsable);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            bool hayCambios = !string.IsNullOrEmpty(resultado.Mensaje);
            MostrarMensaje(hayCambios ? "Se guardaron los cambios." : "No había cambios para guardar.", false);
            CargarParametros();
        }

        private void CargarParametros()
        {
            rptParametros.DataSource = _bllParametros.Listar();
            rptParametros.DataBind();
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

            if (!GestorDeSesion.TienePermisoInterno("CONFIGURAR_PARAMETROS"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return false;
            }

            return true;
        }
    }
}
