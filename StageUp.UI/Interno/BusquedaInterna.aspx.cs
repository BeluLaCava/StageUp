using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno
{
    // Búsqueda interna de la plataforma, simple y avanzada. Qué tipos de
    // información se pueden buscar (y qué estados se pueden filtrar) depende
    // de los permisos del rol: lo resuelve BLL_Busqueda. Los filtros viajan
    // en la URL (Infraestructura/ConsultaBusqueda) y los aplica el SP
    // sp_Busqueda_Interna (script 76).
    public partial class BusquedaInterna : Page
    {
        private readonly BLL_Busqueda _bllBusqueda = new BLL_Busqueda();

        private int IdRol
        {
            get { return GestorDeSesion.ObtenerIdRolInternoActual().Value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso() || IsPostBack)
            {
                return;
            }

            CargarOpciones();

            FiltroBusquedaGlobal filtro = ConsultaBusqueda.Leer(Request.QueryString);
            filtro.CapacidadMinima = null;
            filtro.PrecioMaximo = null;
            MostrarFiltroEnFormulario(filtro);

            if (ConsultaBusqueda.UsaOpcionesAvanzadas(filtro))
            {
                busquedaAvanzada.Attributes["open"] = "open";
            }

            if (ConsultaBusqueda.HayBusqueda(filtro))
            {
                EjecutarBusqueda(filtro);
            }
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            Response.Redirect(ConsultaBusqueda.Armar("~/Interno/BusquedaInterna.aspx", LeerFormulario()));
        }

        private void CargarOpciones()
        {
            cblTipos.Items.Clear();
            foreach (KeyValuePair<string, string> tipo in _bllBusqueda.ObtenerTiposInternosPermitidos(IdRol))
            {
                cblTipos.Items.Add(new ListItem(tipo.Value, tipo.Key));
            }

            ddlEstado.Items.Clear();
            ddlEstado.Items.Add(new ListItem("Cualquiera", string.Empty));
            foreach (KeyValuePair<string, string> estado in _bllBusqueda.ObtenerEstadosInternosPermitidos(IdRol))
            {
                ddlEstado.Items.Add(new ListItem(estado.Value, estado.Key));
            }

            ddlCategoria.Items.Clear();
            ddlCategoria.Items.Add(new ListItem("Todas", string.Empty));
            foreach (KeyValuePair<string, string> categoria in BLL_Busqueda.CategoriasNovedad)
            {
                ddlCategoria.Items.Add(new ListItem(categoria.Value, categoria.Key));
            }

            ddlOrden.Items.Clear();
            foreach (KeyValuePair<string, string> orden in BLL_Busqueda.Ordenes)
            {
                ddlOrden.Items.Add(new ListItem(orden.Value, orden.Key));
            }

            ddlMaximo.Items.Clear();
            foreach (int maximo in BLL_Busqueda.MaximosPermitidos)
            {
                ddlMaximo.Items.Add(new ListItem(maximo.ToString(CultureInfo.InvariantCulture), maximo.ToString(CultureInfo.InvariantCulture)));
            }
            ddlMaximo.SelectedValue = "25";
        }

        private void MostrarFiltroEnFormulario(FiltroBusquedaGlobal filtro)
        {
            txtBusqueda.Text = filtro.Texto;
            foreach (ListItem item in cblTipos.Items)
            {
                item.Selected = filtro.Tipos.Contains(item.Value);
            }

            txtDesde.Text = filtro.FechaDesde.HasValue ? filtro.FechaDesde.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
            txtHasta.Text = filtro.FechaHasta.HasValue ? filtro.FechaHasta.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
            txtUbicacion.Text = filtro.Ubicacion;
            Seleccionar(ddlEstado, filtro.Estado);
            Seleccionar(ddlCategoria, filtro.CategoriaNovedad);
            Seleccionar(ddlOrden, filtro.Orden);
            Seleccionar(ddlMaximo, filtro.Maximo.ToString(CultureInfo.InvariantCulture));
        }

        private FiltroBusquedaGlobal LeerFormulario()
        {
            FiltroBusquedaGlobal filtro = new FiltroBusquedaGlobal
            {
                Texto = string.IsNullOrWhiteSpace(txtBusqueda.Text) ? null : txtBusqueda.Text.Trim(),
                Tipos = cblTipos.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList(),
                FechaDesde = LeerFecha(txtDesde.Text),
                FechaHasta = LeerFecha(txtHasta.Text),
                Ubicacion = string.IsNullOrWhiteSpace(txtUbicacion.Text) ? null : txtUbicacion.Text.Trim(),
                Estado = string.IsNullOrEmpty(ddlEstado.SelectedValue) ? null : ddlEstado.SelectedValue,
                CategoriaNovedad = string.IsNullOrEmpty(ddlCategoria.SelectedValue) ? null : ddlCategoria.SelectedValue,
                Orden = ddlOrden.SelectedValue
            };

            int maximo;
            if (int.TryParse(ddlMaximo.SelectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out maximo))
            {
                filtro.Maximo = maximo;
            }

            return filtro;
        }

        private void EjecutarBusqueda(FiltroBusquedaGlobal filtro)
        {
            ResultadoOperacion<List<GrupoResultadosBusqueda>> resultado = _bllBusqueda.BuscarInterno(filtro, IdRol);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            int total = resultado.Valor.Sum(g => g.Resultados.Count);
            string queBusco = string.IsNullOrWhiteSpace(filtro.Texto) ? "con los filtros elegidos" : "para \"" + Server.HtmlEncode(filtro.Texto) + "\"";
            string filtros = ConsultaBusqueda.Describir(filtro, _bllBusqueda.ObtenerTiposInternosPermitidos(IdRol), _bllBusqueda.ObtenerEstadosInternosPermitidos(IdRol));

            pnlResumen.Visible = true;
            litResumen.Text = (total == 0
                    ? "No encontramos resultados " + queBusco + "."
                    : total + (total == 1 ? " resultado " : " resultados ") + queBusco + ".")
                + (filtros.Length == 0 ? string.Empty : " Filtros: " + Server.HtmlEncode(filtros) + ".");

            rptGrupos.DataSource = resultado.Valor;
            rptGrupos.DataBind();
        }

        private static void Seleccionar(DropDownList lista, string valor)
        {
            if (valor != null && lista.Items.FindByValue(valor) != null)
            {
                lista.SelectedValue = valor;
            }
        }

        private static DateTime? LeerFecha(string texto)
        {
            DateTime fecha;
            return DateTime.TryParseExact((texto ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                ? fecha
                : (DateTime?)null;
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

            if (!GestorDeSesion.TienePermisoInterno("BUSCAR_EN_PLATAFORMA"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
