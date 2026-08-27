using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BLL;

namespace StageUp.UI
{
    /// <summary>
    /// Code-behind de Registrarse.aspx. Implementa CU-001-001 Registrar y
    /// activar usuario externo como un flujo de 3 pasos dentro de la misma
    /// página (paneles), tal como lo describe el caso de uso: datos de
    /// registro -> código de activación -> confirmación.
    /// Esta página no accede a SQL Server ni contiene reglas de negocio:
    /// solo llama a StageUp.BLL.BLL_UsuarioExterno.
    /// </summary>
    public partial class Registrarse : Page
    {
        private readonly BLL_UsuarioExterno _bllUsuarioExterno = new BLL_UsuarioExterno();

        /// <summary>
        /// Id del usuario externo recién registrado, pendiente de activación.
        /// Se guarda en ViewState para sobrevivir los postbacks del panel
        /// de activación (reenviar código / activar cuenta).
        /// </summary>
        private int? IdUsuarioExternoPendiente
        {
            get { return ViewState["IdUsuarioExternoPendiente"] as int?; }
            set { ViewState["IdUsuarioExternoPendiente"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnRegistrarse_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            ResultadoOperacion<int> resultado = _bllUsuarioExterno.Registrar(
                txtNombre.Text,
                txtApellido.Text,
                txtEmail.Text,
                txtPassword.Text,
                txtConfirmarPassword.Text,
                chkAceptaTerminos.Checked,
                chkAceptaPoliticaPrivacidad.Checked);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensajeRegistro, litMensajeRegistro, resultado.Mensaje, esError: true);
                return;
            }

            IdUsuarioExternoPendiente = resultado.Valor;

            pnlDatosRegistro.Visible = false;
            pnlActivacion.Visible = true;
            MostrarMensaje(pnlMensajeActivacion, litMensajeActivacion, resultado.Mensaje, esError: false);
        }

        protected void btnActivarCuenta_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || IdUsuarioExternoPendiente == null)
            {
                return;
            }

            ResultadoOperacion resultado = _bllUsuarioExterno.ValidarActivacion(
                IdUsuarioExternoPendiente.Value, txtCodigoActivacion.Text);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensajeActivacion, litMensajeActivacion, resultado.Mensaje, esError: true);
                return;
            }

            pnlActivacion.Visible = false;
            pnlExitoRegistro.Visible = true;
        }

        protected void lnkReenviarCodigoActivacion_Click(object sender, EventArgs e)
        {
            if (IdUsuarioExternoPendiente == null)
            {
                return;
            }

            ResultadoOperacion resultado = _bllUsuarioExterno.ReenviarCodigoActivacion(IdUsuarioExternoPendiente.Value);
            MostrarMensaje(pnlMensajeActivacion, litMensajeActivacion, resultado.Mensaje, !resultado.Exitoso);
        }

        private static void MostrarMensaje(System.Web.UI.WebControls.Panel panel, Literal literal, string mensaje, bool esError)
        {
            literal.Text = mensaje;
            panel.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            panel.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
