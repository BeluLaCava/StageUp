using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{
    public partial class Registrarse : Page
    {
        private readonly BLL_UsuarioExterno _bllUsuarioExterno = new BLL_UsuarioExterno();
        private readonly BLL_OrigenUsuario _bllOrigen = new BLL_OrigenUsuario();

        private int? IdUsuarioExternoPendiente
        {
            get { return ViewState["IdUsuarioExternoPendiente"] as int?; }
            set { ViewState["IdUsuarioExternoPendiente"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarOrigenes();
            }
        }



        private void CargarOrigenes()
        {
            if (_bllOrigen.ObtenerPorCodigo(OrigenDeDifusion.Leer(Context)) != null)
            {
                pnlOrigen.Visible = false;
                return;
            }

            ddlOrigen.Items.Clear();
            ddlOrigen.Items.Add(new ListItem("Prefiero no decirlo", string.Empty));
            foreach (OrigenUsuario origen in _bllOrigen.ListarParaRegistro())
            {
                ddlOrigen.Items.Add(new ListItem(origen.NombreOrigen, origen.IdOrigenUsuario.ToString(CultureInfo.InvariantCulture)));
            }

            pnlOrigen.Visible = ddlOrigen.Items.Count > 1;
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
                chkAceptaPoliticaPrivacidad.Checked,
                txtTelefono.Text);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensajeRegistro, litMensajeRegistro, resultado.Mensaje, esError: true);
                return;
            }

            IdUsuarioExternoPendiente = resultado.Valor;

            int idOrigen;
            _bllOrigen.AsignarAlRegistrarse(resultado.Valor, OrigenDeDifusion.Leer(Context),
                pnlOrigen.Visible && int.TryParse(ddlOrigen.SelectedValue, out idOrigen) ? idOrigen : (int?)null);
            OrigenDeDifusion.Olvidar(Context);

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

        protected void cvAceptaTerminos_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = chkAceptaTerminos.Checked;
        }

        protected void cvAceptaPoliticaPrivacidad_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = chkAceptaPoliticaPrivacidad.Checked;
        }

        private static void MostrarMensaje(System.Web.UI.WebControls.Panel panel, Literal literal, string mensaje, bool esError)
        {
            literal.Text = mensaje;
            panel.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            panel.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
