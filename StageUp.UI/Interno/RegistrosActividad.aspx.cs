using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class RegistrosActividad : Page
    {
        private readonly BLL_Bitacora _bllBitacora = new BLL_Bitacora();
        private readonly BLL_UsuarioExterno _bllUsuario = new BLL_UsuarioExterno();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            if (!IsPostBack)
            {
                PoblarFiltros();
                CargarTodos();
            }
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;

            string correo = txtCorreoUsuario.Text.Trim();
            int? idUsuario = null;

            if (!string.IsNullOrWhiteSpace(correo))
            {
                idUsuario = _bllUsuario.ObtenerIdPorCorreo(correo);
                if (idUsuario == null)
                {
                    MostrarMensaje("No encontramos ningún usuario con ese correo.", esError: true);
                    rptRegistros.DataSource = null;
                    rptRegistros.DataBind();
                    pnlSinResultados.Visible = false;
                    return;
                }
            }

            DateTime? fechaDesde = ParsearFecha(txtFechaDesde.Text);
            DateTime? fechaHasta = ParsearFecha(txtFechaHasta.Text);
            string tipoOperacion = string.IsNullOrEmpty(ddlTipoOperacion.SelectedValue) ? null : ddlTipoOperacion.SelectedValue;
            string tipoEntidadAfectada = string.IsNullOrEmpty(ddlTipoEntidad.SelectedValue) ? null : ddlTipoEntidad.SelectedValue;

            bool hayFiltrosAplicados = idUsuario != null || fechaDesde != null || fechaHasta != null
                || tipoOperacion != null || tipoEntidadAfectada != null;

            List<RegistroActividad> registros = _bllBitacora.Buscar(idUsuario, fechaDesde, fechaHasta, tipoOperacion, tipoEntidadAfectada);
            MostrarResultados(registros, hayFiltrosAplicados);
        }

        protected void lnkLimpiarFiltros_Click(object sender, EventArgs e)
        {
            txtCorreoUsuario.Text = string.Empty;
            txtFechaDesde.Text = string.Empty;
            txtFechaHasta.Text = string.Empty;
            ddlTipoOperacion.SelectedIndex = 0;
            ddlTipoEntidad.SelectedIndex = 0;
            pnlMensaje.Visible = false;

            CargarTodos();
        }

        protected string ObtenerNombreMostrado(string nombreResponsable)
        {
            return string.IsNullOrWhiteSpace(nombreResponsable) ? "Sistema" : nombreResponsable;
        }

        private void PoblarFiltros()
        {
            ddlTipoOperacion.Items.Clear();
            ddlTipoOperacion.Items.Add(new ListItem("(Todos)", ""));
            foreach (string tipo in BLL_Bitacora.TiposDeOperacion)
            {
                ddlTipoOperacion.Items.Add(new ListItem(tipo, tipo));
            }

            ddlTipoEntidad.Items.Clear();
            ddlTipoEntidad.Items.Add(new ListItem("(Todas)", ""));
            foreach (string tipo in BLL_Bitacora.TiposDeEntidadAfectada)
            {
                ddlTipoEntidad.Items.Add(new ListItem(tipo, tipo));
            }
        }

        private void CargarTodos()
        {
            List<RegistroActividad> registros = _bllBitacora.Buscar();
            MostrarResultados(registros, hayFiltrosAplicados: false);
        }

        private void MostrarResultados(List<RegistroActividad> registros, bool hayFiltrosAplicados)
        {
            rptRegistros.DataSource = registros;
            rptRegistros.DataBind();

            bool sinResultados = registros.Count == 0;
            pnlSinResultados.Visible = sinResultados;

            if (!sinResultados)
            {
                return;
            }

            if (hayFiltrosAplicados)
            {
                litTituloSinResultados.Text = "No se encontraron registros para los filtros seleccionados.";
                litDescripcionSinResultados.Text = "Probá modificar los filtros o hacé clic en \"Limpiar filtros\" para ver el listado completo.";
            }
            else
            {
                litTituloSinResultados.Text = "Todavía no hay registros de actividad.";
                litDescripcionSinResultados.Text = "A medida que se usen las funcionalidades del sistema (registro, login, alta de espacios, etc.), van a aparecer acá.";
            }
        }

        private static DateTime? ParsearFecha(string texto)
        {
            DateTime valor;
            return DateTime.TryParse(texto, out valor) ? valor : (DateTime?)null;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
