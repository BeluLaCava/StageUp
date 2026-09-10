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
    public partial class ConfigurarIdioma : Page
    {
        private readonly BLL_Idioma _bllIdioma = new BLL_Idioma();
        private readonly BLL_Traduccion _bllTraduccion = new BLL_Traduccion();

        private int? IdIdiomaConfigurado
        {
            get { return ViewState["IdIdiomaConfigurado"] as int?; }
            set { ViewState["IdIdiomaConfigurado"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (IsPostBack)
            {
                return;
            }

            int idIdioma;
            if (!int.TryParse(Request.QueryString["id"], out idIdioma))
            {
                MostrarIdiomaNoEncontrado();
                return;
            }

            Idioma idioma = _bllIdioma.ObtenerPorId(idIdioma);
            if (idioma == null || !idioma.Activo)
            {
                MostrarIdiomaNoEncontrado();
                return;
            }

            IdIdiomaConfigurado = idioma.IdIdioma;
            litNombreIdioma.Text = Server.HtmlEncode(idioma.NombreIdioma);
            litCodigoIdioma.Text = Server.HtmlEncode(idioma.CodigoIdioma);
            CargarModulos();
            CargarTraducciones();
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            CargarTraducciones();
        }

        protected void rptTraducciones_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || !IdIdiomaConfigurado.HasValue)
            {
                return;
            }

            int idEtiqueta = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            ResultadoOperacion resultado;

            if (e.CommandName == "Guardar")
            {
                var texto = (TextBox)e.Item.FindControl("txtTextoTraducido");
                resultado = _bllTraduccion.Guardar(
                    IdIdiomaConfigurado.Value,
                    idEtiqueta,
                    texto.Text,
                    GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
            }
            else if (e.CommandName == "Eliminar")
            {
                resultado = _bllTraduccion.Eliminar(
                    IdIdiomaConfigurado.Value,
                    idEtiqueta,
                    GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
            }
            else
            {
                return;
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarTraducciones();
        }

        protected string ObtenerClaseFila(Traduccion traduccion)
        {
            return traduccion != null && traduccion.TieneTraduccion
                ? "translation-row translation-row-complete"
                : "translation-row translation-row-pending";
        }

        protected string ObtenerClaseEstado(Traduccion traduccion)
        {
            return traduccion != null && traduccion.TieneTraduccion
                ? "translation-status translation-status-complete"
                : "translation-status translation-status-pending";
        }

        protected string ObtenerTextoEstado(Traduccion traduccion)
        {
            return traduccion != null && traduccion.TieneTraduccion ? "Completada" : "Pendiente";
        }

        private void CargarModulos()
        {
            List<Traduccion> traducciones = _bllTraduccion.ListarConfiguracion(IdIdiomaConfigurado.Value);
            var modulos = new List<string>();
            foreach (Traduccion traduccion in traducciones)
            {
                if (!modulos.Contains(traduccion.Modulo))
                {
                    modulos.Add(traduccion.Modulo);
                }
            }

            modulos.Sort(StringComparer.CurrentCultureIgnoreCase);
            ddlModulo.Items.Clear();
            ddlModulo.Items.Add(new ListItem("Todos los módulos", string.Empty));
            foreach (string modulo in modulos)
            {
                ddlModulo.Items.Add(new ListItem(modulo, modulo));
            }
        }

        private void CargarTraducciones()
        {
            if (!IdIdiomaConfigurado.HasValue)
            {
                return;
            }

            List<Traduccion> todas = _bllTraduccion.ListarConfiguracion(IdIdiomaConfigurado.Value);
            int completadas = 0;
            foreach (Traduccion traduccion in todas)
            {
                if (traduccion.TieneTraduccion)
                {
                    completadas++;
                }
            }

            int porcentaje = todas.Count == 0
                ? 0
                : (int)Math.Round(completadas * 100m / todas.Count);

            litTotalEtiquetas.Text = todas.Count.ToString(CultureInfo.InvariantCulture);
            litCompletadas.Text = completadas.ToString(CultureInfo.InvariantCulture);
            litPendientes.Text = (todas.Count - completadas).ToString(CultureInfo.InvariantCulture);
            litPorcentaje.Text = porcentaje.ToString(CultureInfo.InvariantCulture);
            pnlBarraProgreso.Style["width"] = porcentaje.ToString(CultureInfo.InvariantCulture) + "%";

            string busqueda = string.IsNullOrWhiteSpace(txtBuscar.Text) ? null : txtBuscar.Text.Trim();
            string modulo = ddlModulo.SelectedValue;
            var filtradas = new List<Traduccion>();

            foreach (Traduccion traduccion in todas)
            {
                if (chkSoloPendientes.Checked && traduccion.TieneTraduccion)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(modulo) && traduccion.Modulo != modulo)
                {
                    continue;
                }

                if (busqueda != null &&
                    !Contiene(traduccion.ClaveEtiqueta, busqueda) &&
                    !Contiene(traduccion.TextoPredeterminado, busqueda) &&
                    !Contiene(traduccion.TextoTraducido, busqueda))
                {
                    continue;
                }

                filtradas.Add(traduccion);
            }

            pnlSinEtiquetas.Visible = filtradas.Count == 0;
            rptTraducciones.DataSource = filtradas;
            rptTraducciones.DataBind();
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return !string.IsNullOrEmpty(valor) &&
                valor.IndexOf(busqueda, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void MostrarIdiomaNoEncontrado()
        {
            pnlIdiomaNoEncontrado.Visible = true;
            pnlConfiguracion.Visible = false;
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
