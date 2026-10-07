using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // Reportes con gráficos y tablero de indicadores (ítem 15 de la segunda
    // entrega). Los gráficos son HTML/CSS (sin librerías externas): una sola
    // serie por gráfico, un solo color, con tooltip y vista de tabla.
    public partial class Reportes : Page
    {
        private const string PermisoRequerido = "VER_REPORTES";
        private const int MaximoEtiquetasEje = 12;

        private static readonly CultureInfo Cultura = new CultureInfo("es-AR");

        private readonly BLL_Reporte _bllReporte = new BLL_Reporte();

        private FiltroReporte _filtroActual;
        private int _cantidadPeriodos;

        protected string DescripcionGraficoIngresos { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarCombos();
                AplicarFiltroEnFormulario(BLL_Reporte.CrearFiltroPorDefecto());
                CargarReporte();
            }
        }

        protected void btnAplicar_Click(object sender, EventArgs e)
        {
            if (TieneAcceso())
            {
                CargarReporte();
            }
        }

        protected void lnkUltimos12Meses_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            AplicarFiltroEnFormulario(BLL_Reporte.CrearFiltroPorDefecto());
            ddlProvincia.SelectedIndex = 0;
            ddlTipoEspacio.SelectedIndex = 0;
            CargarReporte();
        }

        protected void lnkExportarCsv_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            FiltroReporte filtro;
            if (!LeerFiltroDelFormulario(out filtro))
            {
                return;
            }

            ResultadoOperacion<ReporteCompleto> resultado = _bllReporte.ObtenerReporte(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            string nombreArchivo = "StageUp_reporte_" + filtro.Desde.ToString("yyyyMMdd", CultureInfo.InvariantCulture) +
                                   "_" + filtro.Hasta.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".csv";

            // Con BOM, para que Excel reconozca los acentos.
            UTF8Encoding codificacion = new UTF8Encoding(true);
            byte[] contenido = codificacion.GetPreamble()
                .Concat(codificacion.GetBytes(BLL_Reporte.GenerarCsvReporteCompleto(resultado.Valor)))
                .ToArray();

            Response.Clear();
            Response.ContentType = "text/csv; charset=utf-8";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + nombreArchivo);
            Response.BinaryWrite(contenido);
            Response.End();
        }

        // ---- Carga ----------------------------------------------------------

        private void CargarCombos()
        {
            ddlAgrupacion.Items.Clear();
            foreach (KeyValuePair<string, string> agrupacion in BLL_Reporte.ObtenerAgrupaciones())
            {
                ddlAgrupacion.Items.Add(new ListItem(agrupacion.Value, agrupacion.Key));
            }

            ddlMoneda.Items.Clear();
            ddlMoneda.Items.Add(new ListItem("Pesos (ARS)", "ARS"));
            ddlMoneda.Items.Add(new ListItem("Dólares (USD)", "USD"));

            Dictionary<string, List<string>> valores = _bllReporte.ListarValoresFiltros();

            ddlProvincia.Items.Clear();
            ddlProvincia.Items.Add(new ListItem("Todas", string.Empty));
            foreach (string provincia in valores["Provincia"])
            {
                ddlProvincia.Items.Add(new ListItem(provincia, provincia));
            }

            ddlTipoEspacio.Items.Clear();
            ddlTipoEspacio.Items.Add(new ListItem("Todos", string.Empty));
            foreach (string tipo in valores["TipoEspacio"])
            {
                ddlTipoEspacio.Items.Add(new ListItem(tipo, tipo));
            }
        }

        private void AplicarFiltroEnFormulario(FiltroReporte filtro)
        {
            txtDesde.Text = filtro.Desde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            txtHasta.Text = filtro.Hasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            SeleccionarValor(ddlAgrupacion, filtro.Agrupacion);
            SeleccionarValor(ddlMoneda, filtro.Moneda);
        }

        private bool LeerFiltroDelFormulario(out FiltroReporte filtro)
        {
            filtro = null;
            DateTime desde;
            DateTime hasta;
            if (!DateTime.TryParseExact(txtDesde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out desde) ||
                !DateTime.TryParseExact(txtHasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta))
            {
                MostrarMensaje("Completá las fechas desde y hasta.", true);
                return false;
            }

            filtro = new FiltroReporte
            {
                Desde = desde,
                Hasta = hasta,
                Agrupacion = ddlAgrupacion.SelectedValue,
                Moneda = ddlMoneda.SelectedValue,
                Provincia = ddlProvincia.SelectedValue,
                TipoEspacio = ddlTipoEspacio.SelectedValue
            };
            return true;
        }

        private void CargarReporte()
        {
            pnlMensaje.Visible = false;

            FiltroReporte filtro;
            if (!LeerFiltroDelFormulario(out filtro))
            {
                pnlReporte.Visible = false;
                return;
            }

            ResultadoOperacion<ReporteCompleto> resultado = _bllReporte.ObtenerReporte(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                pnlReporte.Visible = false;
                return;
            }

            ReporteCompleto reporte = resultado.Valor;
            _filtroActual = reporte.Filtro;

            // La BLL puede haber cambiado la agrupación (períodos muy largos).
            SeleccionarValor(ddlAgrupacion, reporte.Filtro.Agrupacion);
            pnlAviso.Visible = !string.IsNullOrEmpty(reporte.Aviso);
            litAviso.Text = Server.HtmlEncode(reporte.Aviso ?? string.Empty);

            MostrarIndicadores(reporte.Indicadores);
            MostrarIngresos(reporte);
            MostrarCategorias(reporte);

            pnlReporte.Visible = true;
        }

        private void MostrarIndicadores(IndicadoresTablero indicadores)
        {
            litKpiIngresos.Text = Server.HtmlEncode(Importe(indicadores.ImporteReservas));
            litKpiConfirmadas.Text = Server.HtmlEncode(indicadores.ReservasConfirmadas.ToString("N0", Cultura) + " reservas confirmadas");
            litKpiComisiones.Text = Server.HtmlEncode(Importe(indicadores.ImporteComisiones));
            litKpiSolicitadas.Text = indicadores.ReservasSolicitadas.ToString("N0", Cultura);
            litKpiUsuariosNuevos.Text = indicadores.UsuariosNuevos.ToString("N0", Cultura);
            litKpiUsuariosActivos.Text = Server.HtmlEncode(indicadores.UsuariosActivos.ToString("N0", Cultura) + " usuarios activos en total");
            litKpiEspacios.Text = indicadores.EspaciosPublicados.ToString("N0", Cultura);
            litKpiCalificacion.Text = indicadores.PromedioCalificacion.HasValue
                ? Server.HtmlEncode(indicadores.PromedioCalificacion.Value.ToString("0.0", Cultura) + " / 5")
                : "Sin reseñas";
            litKpiTickets.Text = indicadores.TicketsPendientes.ToString("N0", Cultura);
        }

        private void MostrarIngresos(ReporteCompleto reporte)
        {
            string agrupacion = BLL_Reporte.ObtenerAgrupaciones()[reporte.Filtro.Agrupacion].ToLowerInvariant();
            litTituloIngresos.Text = Server.HtmlEncode("Ingresos por reservas confirmadas, por " + agrupacion);
            litTotalIngresos.Text = Server.HtmlEncode(Importe(reporte.TotalImporteReservas));
            litTotalComisiones.Text = Server.HtmlEncode(Importe(reporte.TotalImporteComisiones));
            litTotalReservas.Text = reporte.TotalReservasPeriodo.ToString("N0", Cultura);
            litTablaTotalReservas.Text = litTotalReservas.Text;
            litTablaTotalIngresos.Text = litTotalIngresos.Text;
            litTablaTotalComisiones.Text = litTotalComisiones.Text;

            bool hayMovimientos = reporte.Ingresos.Any(f => f.CantidadReservas > 0 || f.ImporteComisiones > 0);
            pnlIngresosVacio.Visible = !hayMovimientos;
            pnlIngresosGrafico.Visible = hayMovimientos;

            decimal maximo = reporte.Ingresos.Count == 0 ? 0m : reporte.Ingresos.Max(f => f.ImporteReservas);
            litMaximoIngresos.Text = Server.HtmlEncode(Importe(maximo));

            FilaReportePeriodo mejor = reporte.Ingresos.OrderByDescending(f => f.ImporteReservas).FirstOrDefault();
            DescripcionGraficoIngresos = "Gráfico de columnas de ingresos por " + agrupacion + ". Total " +
                Importe(reporte.TotalImporteReservas) +
                (mejor != null && mejor.ImporteReservas > 0
                    ? ". Período más alto: " + DescribirPeriodo(mejor.InicioPeriodo) + " con " + Importe(mejor.ImporteReservas) + "."
                    : ".");

            _cantidadPeriodos = reporte.Ingresos.Count;
            rptIngresosColumnas.DataSource = reporte.Ingresos;
            rptIngresosColumnas.DataBind();
            rptIngresosEjes.DataSource = reporte.Ingresos;
            rptIngresosEjes.DataBind();
            rptIngresosTabla.DataSource = reporte.Ingresos;
            rptIngresosTabla.DataBind();
        }

        private void MostrarCategorias(ReporteCompleto reporte)
        {
            pnlZonasVacio.Visible = reporte.Zonas.Count == 0;
            rptZonas.DataSource = reporte.Zonas;
            rptZonas.DataBind();
            rptZonasTabla.DataSource = reporte.ZonasDetalle;
            rptZonasTabla.DataBind();

            pnlEstadosVacio.Visible = reporte.Estados.Count == 0;
            rptEstados.DataSource = reporte.Estados;
            rptEstados.DataBind();
            rptEstadosTabla.DataSource = reporte.Estados;
            rptEstadosTabla.DataBind();

            pnlEncuestasVacio.Visible = reporte.Encuestas.Count == 0;
            rptEncuestas.DataSource = reporte.Encuestas;
            rptEncuestas.DataBind();
            rptEncuestasTabla.DataSource = reporte.Encuestas;
            rptEncuestasTabla.DataBind();
        }

        // ---- Ayudas para el marcado --------------------------------------

        protected string Importe(object valor)
        {
            decimal importe = Convert.ToDecimal(valor, CultureInfo.InvariantCulture);
            return BLL_Reporte.FormatearImporte(importe, _filtroActual != null ? _filtroActual.Moneda : "ARS");
        }

        protected static string Porcentaje(object valor)
        {
            return Convert.ToDecimal(valor, CultureInfo.InvariantCulture).ToString("0.#", Cultura) + " %";
        }

        // Alto/ancho de la barra. Un valor mayor a cero nunca queda invisible.
        protected static string EstiloAlto(object porcentaje)
        {
            return "height:" + Medida(porcentaje) + "%;";
        }

        protected static string EstiloAncho(object porcentaje)
        {
            return "width:" + Medida(porcentaje) + "%;";
        }

        private static string Medida(object porcentaje)
        {
            decimal valor = Convert.ToDecimal(porcentaje, CultureInfo.InvariantCulture);
            if (valor > 0m && valor < 1m)
            {
                valor = 1m;
            }

            return Math.Min(100m, Math.Max(0m, valor)).ToString("0.##", CultureInfo.InvariantCulture);
        }

        // Con muchos períodos se muestra una etiqueta cada tantos, para que
        // no se pisen. El valor exacto de cada barra está en el tooltip.
        protected string MostrarEtiquetaEje(int indice, string etiqueta)
        {
            int paso = Math.Max(1, (int)Math.Ceiling(_cantidadPeriodos / (double)MaximoEtiquetasEje));
            return indice % paso == 0 ? etiqueta : string.Empty;
        }

        protected string DescribirPeriodo(DateTime inicio)
        {
            string agrupacion = _filtroActual != null ? _filtroActual.Agrupacion : BLL_Reporte.AgrupacionMes;
            switch (agrupacion)
            {
                case BLL_Reporte.AgrupacionDia:
                    return inicio.ToString("dd/MM/yyyy", Cultura);
                case BLL_Reporte.AgrupacionSemana:
                    return "Semana del " + inicio.ToString("dd/MM/yyyy", Cultura);
                case BLL_Reporte.AgrupacionAnio:
                    return inicio.ToString("yyyy", Cultura);
                default:
                    return Cultura.TextInfo.ToTitleCase(inicio.ToString("MMMM yyyy", Cultura));
            }
        }

        protected string TooltipPeriodo(object item)
        {
            FilaReportePeriodo fila = (FilaReportePeriodo)item;
            return Atributo(DescribirPeriodo(fila.InicioPeriodo) + "\n" +
                            Importe(fila.ImporteReservas) + " · " + fila.CantidadReservas + " reservas\n" +
                            "Comisiones: " + Importe(fila.ImporteComisiones));
        }

        protected string TooltipZona(object item)
        {
            FilaReporteZona zona = (FilaReporteZona)item;
            return Atributo(zona.Provincia + " · " + zona.Ciudad + "\n" +
                            Importe(zona.ImporteReservas) + " · " + zona.CantidadReservas + " reservas");
        }

        protected string TooltipEstado(object item)
        {
            FilaReporteEstado estado = (FilaReporteEstado)item;
            return Atributo(NombreEstado(estado.Estado) + ": " + estado.Cantidad + " reservas (" +
                            Porcentaje(estado.PorcentajeDelTotal) + ")");
        }

        protected string TooltipEncuesta(object item)
        {
            FilaParticipacionEncuesta encuesta = (FilaParticipacionEncuesta)item;
            return Atributo(encuesta.Titulo + "\n" + DescribirParticipacion(item) + "\n" +
                            "Público: " + NombrePublico(encuesta.PublicoObjetivo) +
                            " · Vence: " + encuesta.FechaVencimiento.ToString("dd/MM/yyyy", Cultura));
        }

        protected string DescribirParticipacion(object item)
        {
            FilaParticipacionEncuesta encuesta = (FilaParticipacionEncuesta)item;
            string respuestas = encuesta.CantidadRespuestas + " de " + encuesta.CantidadDestinatarios;
            return encuesta.TasaParticipacion.HasValue
                ? respuestas + " · " + Porcentaje(encuesta.TasaParticipacion.Value)
                : encuesta.CantidadRespuestas + " respuestas";
        }

        protected static string NombreEstado(string estado)
        {
            return BLL_Reporte.NombreEstadoReserva(estado);
        }

        protected static string NombrePublico(string publico)
        {
            return BLL_Reporte.NombrePublicoEncuesta(publico);
        }

        protected string TasaParticipacion(object tasa)
        {
            return tasa == null ? "-" : Porcentaje(tasa);
        }

        private static string Atributo(string texto)
        {
            return HttpUtility.HtmlAttributeEncode(texto ?? string.Empty);
        }

        // ---- Varios ---------------------------------------------------------

        private static void SeleccionarValor(DropDownList lista, string valor)
        {
            ListItem item = lista.Items.FindByValue(valor ?? string.Empty);
            if (item != null)
            {
                lista.ClearSelection();
                item.Selected = true;
            }
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

            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
