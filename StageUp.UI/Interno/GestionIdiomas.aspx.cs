using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class GestionIdiomas : Page
    {
        private readonly BLL_Idioma _bllIdioma = new BLL_Idioma();

        private int? IdIdiomaEnEdicion
        {
            get { return ViewState["IdIdiomaEnEdicion"] as int?; }
            set { ViewState["IdIdiomaEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarIdiomas();
            }
        }

        protected void btnGuardarIdioma_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || !TieneAcceso())
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;

            if (IdIdiomaEnEdicion.HasValue)
            {
                resultado = _bllIdioma.Modificar(
                    IdIdiomaEnEdicion.Value,
                    txtNombreIdioma.Text,
                    txtCodigoIdioma.Text,
                    chkPredeterminado.Checked,
                    idResponsable);
            }
            else
            {
                resultado = _bllIdioma.Registrar(
                    txtNombreIdioma.Text,
                    txtCodigoIdioma.Text,
                    chkPredeterminado.Checked,
                    idResponsable);
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (!resultado.Exitoso)
            {
                return;
            }

            LimpiarFormulario();
            CargarIdiomas();
        }

        protected void lnkCancelarEdicion_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void rptIdiomas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idIdioma = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            switch (e.CommandName)
            {
                case "Configurar":
                    Response.Redirect("~/Interno/ConfigurarIdioma.aspx?id=" + idIdioma);
                    return;

                case "Editar":
                    CargarIdiomaEnFormulario(idIdioma);
                    return;

                case "Baja":
                    ResultadoOperacion resultado = _bllIdioma.DarDeBaja(
                        idIdioma,
                        GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    if (resultado.Exitoso && IdIdiomaEnEdicion == idIdioma)
                    {
                        LimpiarFormulario();
                    }
                    CargarIdiomas();
                    return;
            }
        }

        protected string ObtenerInicialIdioma(string nombreIdioma)
        {
            return string.IsNullOrWhiteSpace(nombreIdioma)
                ? "I"
                : nombreIdioma.Trim().Substring(0, 1).ToUpper(CultureInfo.CurrentCulture);
        }

        private void CargarIdiomas()
        {
            List<Idioma> idiomas = _bllIdioma.Listar();
            litCantidadIdiomas.Text = idiomas.Count.ToString(CultureInfo.InvariantCulture);
            pnlSinIdiomas.Visible = idiomas.Count == 0;
            rptIdiomas.DataSource = idiomas;
            rptIdiomas.DataBind();
        }

        private void CargarIdiomaEnFormulario(int idIdioma)
        {
            Idioma idioma = _bllIdioma.ObtenerPorId(idIdioma);
            if (idioma == null || !idioma.Activo)
            {
                MostrarMensaje("No se encontró el idioma seleccionado.", true);
                return;
            }

            IdIdiomaEnEdicion = idioma.IdIdioma;
            txtNombreIdioma.Text = idioma.NombreIdioma;
            txtCodigoIdioma.Text = idioma.CodigoIdioma;
            chkPredeterminado.Checked = idioma.EsPredeterminado;
            litTituloFormulario.Text = "Editar idioma";
            btnGuardarIdioma.Text = "Guardar cambios";
            lnkCancelarEdicion.Visible = true;
        }

        private void LimpiarFormulario()
        {
            IdIdiomaEnEdicion = null;
            txtNombreIdioma.Text = string.Empty;
            txtCodigoIdioma.Text = string.Empty;
            chkPredeterminado.Checked = false;
            litTituloFormulario.Text = "Nuevo idioma";
            btnGuardarIdioma.Text = "Crear idioma";
            lnkCancelarEdicion.Visible = false;
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_IDIOMAS"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return false;
            }

            return true;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError
                ? "form-message form-message-error language-admin-message"
                : "form-message form-message-success language-admin-message";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
