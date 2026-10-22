using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{




    public partial class Buscar : Page
    {
        private readonly BLL_Busqueda _bllBusqueda = new BLL_Busqueda();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            CargarOpciones();

            FiltroBusquedaGlobal filtro = ConsultaBusqueda.Leer(Request.QueryString);
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
            Response.Redirect(ConsultaBusqueda.Armar("~/Buscar.aspx", LeerFormulario()));
        }

        private void CargarOpciones()
        {
            cblTipos.Items.Clear();
            foreach (KeyValuePair<string, string> tipo in BLL_Busqueda.ObtenerTiposPublicos())
            {
                cblTipos.Items.Add(new ListItem(tipo.Value, tipo.Key));
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

            cblEquipamiento.Items.Clear();
            foreach (KeyValuePair<string, string> equipo in BLL_Busqueda.EquipamientosEspacio)
            {
                cblEquipamiento.Items.Add(new ListItem(equipo.Value, equipo.Key));
            }
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
            txtTipoEspacio.Text = filtro.TipoEspacio;
            txtProvincia.Text = filtro.Provincia;
            txtCiudad.Text = filtro.Ciudad;
            txtZona.Text = filtro.Zona;
            txtPrecioMinimo.Text = filtro.PrecioMinimo.HasValue ? filtro.PrecioMinimo.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
            txtCapacidad.Text = filtro.CapacidadMinima.HasValue ? filtro.CapacidadMinima.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
            txtPrecio.Text = filtro.PrecioMaximo.HasValue ? filtro.PrecioMaximo.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
            txtFechaDisponible.Text = filtro.FechaDisponibilidad.HasValue ? filtro.FechaDisponibilidad.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
            txtHoraDesde.Text = Hora(filtro.MinutoDesde);
            txtHoraHasta.Text = Hora(filtro.MinutoHasta);
            txtCaracteristicas.Text = filtro.Caracteristicas;
            Seleccionar(ddlCalificacion, filtro.CalificacionMinima.HasValue ? filtro.CalificacionMinima.Value.ToString("0.##", CultureInfo.InvariantCulture) : null);
            foreach (ListItem item in cblEquipamiento.Items) item.Selected = filtro.Equipamiento.Contains(item.Value);
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
                TipoEspacio = Texto(txtTipoEspacio.Text),
                Provincia = Texto(txtProvincia.Text),
                Ciudad = Texto(txtCiudad.Text),
                Zona = Texto(txtZona.Text),
                FechaDisponibilidad = LeerFecha(txtFechaDisponible.Text),
                MinutoDesde = LeerMinutos(txtHoraDesde.Text),
                MinutoHasta = LeerMinutos(txtHoraHasta.Text),
                Equipamiento = cblEquipamiento.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList(),
                Caracteristicas = Texto(txtCaracteristicas.Text),
                CategoriaNovedad = string.IsNullOrEmpty(ddlCategoria.SelectedValue) ? null : ddlCategoria.SelectedValue,
                Orden = ddlOrden.SelectedValue
            };

            int entero;
            if (int.TryParse(txtCapacidad.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out entero))
            {
                filtro.CapacidadMinima = entero;
            }

            decimal precio;
            if (decimal.TryParse((txtPrecioMinimo.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                filtro.PrecioMinimo = precio;
            }

            if (decimal.TryParse((txtPrecio.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                filtro.PrecioMaximo = precio;
            }

            if (decimal.TryParse(ddlCalificacion.SelectedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                filtro.CalificacionMinima = precio;
            }

            if (int.TryParse(ddlMaximo.SelectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out entero))
            {
                filtro.Maximo = entero;
            }

            return filtro;
        }

        private void EjecutarBusqueda(FiltroBusquedaGlobal filtro)
        {
            ResultadoOperacion<List<GrupoResultadosBusqueda>> resultado = _bllBusqueda.BuscarPublico(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }


            if (!string.IsNullOrWhiteSpace(resultado.Mensaje))
            {
                MostrarMensaje(resultado.Mensaje, false);
            }

            int total = resultado.Valor.Sum(g => g.Resultados.Count);
            string queBusco = string.IsNullOrWhiteSpace(filtro.Texto) ? "con los filtros elegidos" : "para \"" + Server.HtmlEncode(filtro.Texto) + "\"";
            string filtros = ConsultaBusqueda.Describir(filtro, BLL_Busqueda.ObtenerTiposPublicos(), null);

            pnlResumen.Visible = true;
            litResumen.Text = (total == 0
                    ? "No encontramos resultados " + queBusco + ". Probá con otra palabra o con menos filtros."
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

        private static int? LeerMinutos(string texto)
        {
            TimeSpan hora;
            return TimeSpan.TryParseExact(texto ?? string.Empty, @"hh\:mm", CultureInfo.InvariantCulture, out hora) ? (int?)hora.TotalMinutes : null;
        }

        private static string Hora(int? minutos)
        {
            return minutos.HasValue ? TimeSpan.FromMinutes(minutos.Value).ToString(@"hh\:mm", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string Texto(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
