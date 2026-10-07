using System;
using System.Collections.Generic;
using System.Text;
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
        private readonly BLL_ExportacionSegura _bllExportacion = new BLL_ExportacionSegura();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("VER_BITACORA"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
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
            pnlDetalle.Visible = false;
            FiltroRegistroActividad filtro = ArmarFiltro();
            MostrarResultados(_bllBitacora.Buscar(filtro), HayFiltros(filtro));
        }

        protected void lnkLimpiarFiltros_Click(object sender, EventArgs e)
        {
            txtResponsable.Text = string.Empty;
            ddlTipoResponsable.SelectedIndex = 0;
            pnlDetalle.Visible = false;
            txtFechaDesde.Text = string.Empty;
            txtFechaHasta.Text = string.Empty;
            ddlTipoOperacion.SelectedIndex = 0;
            ddlTipoEntidad.SelectedIndex = 0;
            pnlMensaje.Visible = false;

            CargarTodos();
        }

        protected void btnExportar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;

            ResultadoOperacion<string> resultado = _bllExportacion.ExportarBitacoraCifrada(ArmarFiltro());
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                CargarTodos();
                return;
            }

            byte[] contenido = Encoding.UTF8.GetBytes(resultado.Valor);
            Response.Clear();
            Response.ContentType = "application/xml";
            Response.AddHeader("Content-Disposition",
                "attachment; filename=bitacora_export_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xml");
            Response.BinaryWrite(contenido);
            Response.End();
        }

        protected string ObtenerNombreMostrado(string nombreResponsable)
        {
            return string.IsNullOrWhiteSpace(nombreResponsable) ? "Sistema" : nombreResponsable;
        }

        protected void rptRegistros_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idRegistro;
            if (e.CommandName != "Ver" || !int.TryParse(Convert.ToString(e.CommandArgument), out idRegistro))
            {
                return;
            }

            RegistroActividad registro = _bllBitacora.ObtenerPorId(idRegistro);
            if (registro == null)
            {
                MostrarMensaje("No se encontró el registro.", esError: true);
                return;
            }

            litDetalleId.Text = registro.IdRegistroActividad.ToString();
            litDetalleOperacion.Text = Server.HtmlEncode(registro.TipoOperacion);
            litDetalleEntidad.Text = Server.HtmlEncode(registro.TipoEntidadAfectada);
            litDetalleIdAfectado.Text = registro.IdEntidadAfectada.HasValue ? registro.IdEntidadAfectada.Value.ToString() : "-";
            litDetalleResponsable.Text = Server.HtmlEncode(
                registro.TipoResponsable == "Sistema"
                    ? "Sistema (proceso automático)"
                    : ObtenerNombreMostrado(registro.NombreResponsable) +
                      (string.IsNullOrEmpty(registro.CorreoResponsable) ? string.Empty : " (" + registro.CorreoResponsable + ")") +
                      " · usuario " + registro.TipoResponsable.ToLowerInvariant());
            litDetalleFecha.Text = registro.FechaOperacion.ToString("dd/MM/yyyy HH:mm:ss");
            litDetalleOrigen.Text = Server.HtmlEncode(string.IsNullOrEmpty(registro.OrigenOperacion) ? "-" : registro.OrigenOperacion);
            litDetalleDescripcion.Text = Server.HtmlEncode(string.IsNullOrEmpty(registro.DescripcionOperacion) ? "-" : registro.DescripcionOperacion);
            pnlDetalle.Visible = true;
        }

        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            pnlDetalle.Visible = false;
        }

        private FiltroRegistroActividad ArmarFiltro()
        {
            return new FiltroRegistroActividad
            {
                TextoResponsable = string.IsNullOrWhiteSpace(txtResponsable.Text) ? null : txtResponsable.Text.Trim(),
                TipoResponsable = string.IsNullOrEmpty(ddlTipoResponsable.SelectedValue) ? null : ddlTipoResponsable.SelectedValue,
                FechaDesde = ParsearFecha(txtFechaDesde.Text),
                FechaHasta = ParsearFecha(txtFechaHasta.Text),
                TipoOperacion = string.IsNullOrEmpty(ddlTipoOperacion.SelectedValue) ? null : ddlTipoOperacion.SelectedValue,
                TipoEntidadAfectada = string.IsNullOrEmpty(ddlTipoEntidad.SelectedValue) ? null : ddlTipoEntidad.SelectedValue
            };
        }

        private static bool HayFiltros(FiltroRegistroActividad filtro)
        {
            return filtro.TextoResponsable != null || filtro.TipoResponsable != null || filtro.FechaDesde != null ||
                filtro.FechaHasta != null || filtro.TipoOperacion != null || filtro.TipoEntidadAfectada != null;
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
            List<RegistroActividad> registros = _bllBitacora.Buscar(new FiltroRegistroActividad());
            MostrarResultados(registros, hayFiltrosAplicados: false);
        }

        private void MostrarResultados(List<RegistroActividad> registros, bool hayFiltrosAplicados)
        {
            rptRegistros.DataSource = registros;
            rptRegistros.DataBind();

            litCantidad.Text = registros.Count >= BLL_Bitacora.MaximoRegistrosPorBusqueda
                ? "Se muestran los " + BLL_Bitacora.MaximoRegistrosPorBusqueda + " registros más recientes. Usá los filtros para acotar la búsqueda."
                : registros.Count + (registros.Count == 1 ? " registro." : " registros.");

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
