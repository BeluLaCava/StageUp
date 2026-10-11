using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // CU-002-002 A5 a A9: pantalla "Transacciones económicas".
    public partial class TransaccionesEconomicas : Page
    {
        private const string PermisoRequerido = "CONSULTAR_TRANSACCIONES";

        private readonly BLL_TransaccionEconomica _bll = new BLL_TransaccionEconomica();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            // A5 paso 2 y A6.
            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            if (!IsPostBack)
            {
                CargarTransacciones();
            }
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarTransacciones();
        }

        protected void lnkLimpiar_Click(object sender, EventArgs e)
        {
            ddlTipo.ClearSelection();
            ddlEstadoPago.ClearSelection();
            txtDesde.Text = string.Empty;
            txtHasta.Text = string.Empty;
            txtTexto.Text = string.Empty;
            pnlMensaje.Visible = false;
            CargarTransacciones();
        }

        protected void rptTransacciones_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            if (e.CommandName != "Ver" || !int.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                return;
            }

            ResultadoOperacion<TransaccionEconomica> resultado = _bll.ObtenerDetalle(id);
            if (!resultado.Exitoso)
            {
                // A9: se vuelve al listado con el aviso.
                MostrarMensaje(resultado.Mensaje, true);
                CargarTransacciones();
                return;
            }

            MostrarDetalle(resultado.Valor);
        }

        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            pnlDetalle.Visible = false;
        }

        protected string DescribirTipo(object dato)
        {
            TransaccionEconomica t = (TransaccionEconomica)dato;
            string texto = BLL_TransaccionEconomica.DescribirTipo(t.TipoOperacion);
            if (t.TipoOperacion == BLL_TransaccionEconomica.TipoCargoCancelacion && t.PorcentajeCargo.HasValue)
            {
                texto += " (" + t.PorcentajeCargo.Value.ToString("0.##", CultureInfo.InvariantCulture) + " %)";
            }

            return texto;
        }

        protected static string Importe(object importe, object moneda)
        {
            return importe == null ? "—" : BLL_CuentaCorriente.FormatearImporte(Convert.ToDecimal(importe), Convert.ToString(moneda));
        }

        protected static string Porcentaje(object porcentaje)
        {
            return porcentaje == null ? string.Empty : Convert.ToDecimal(porcentaje).ToString("0.##", CultureInfo.InvariantCulture) + " %";
        }

        private void CargarTransacciones()
        {
            DateTime desde;
            DateTime hasta;
            FiltroTransacciones filtro = new FiltroTransacciones
            {
                TipoOperacion = ddlTipo.SelectedValue,
                EstadoPago = ddlEstadoPago.SelectedValue,
                Desde = DateTime.TryParse(txtDesde.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out desde) ? desde : (DateTime?)null,
                Hasta = DateTime.TryParse(txtHasta.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta) ? hasta : (DateTime?)null,
                Texto = txtTexto.Text
            };

            bool hayFiltros = filtro.TipoOperacion.Length > 0 || filtro.EstadoPago.Length > 0 || filtro.Desde.HasValue ||
                              filtro.Hasta.HasValue || !string.IsNullOrWhiteSpace(filtro.Texto);

            ResultadoOperacion<List<TransaccionEconomica>> resultado = _bll.Listar(filtro);
            List<TransaccionEconomica> lista = resultado.Exitoso ? resultado.Valor : new List<TransaccionEconomica>();
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
            }

            bool noHayNinguna = lista.Count == 0 && (!hayFiltros || _bll.NoHayTransacciones());
            pnlSinTransacciones.Visible = lista.Count == 0 && noHayNinguna;
            pnlSinResultados.Visible = lista.Count == 0 && !noHayNinguna;
            litCantidad.Text = lista.Count == 0 ? string.Empty
                : lista.Count == 1 ? "1 transacción" : lista.Count.ToString(CultureInfo.InvariantCulture) + " transacciones";

            rptTransacciones.DataSource = lista;
            rptTransacciones.DataBind();
        }

        private void MostrarDetalle(TransaccionEconomica t)
        {
            string moneda = t.Moneda ?? "ARS";
            litDetalleTitulo.Text = "Transacción " + t.Codigo;
            lblDetalleEstado.Text = HttpUtility.HtmlEncode(BLL_TransaccionEconomica.DescribirEstadoPago(t.EstadoPago));
            lblDetalleEstado.CssClass = "pago-estado pago-estado-" + t.EstadoPago;

            litDetalleTipo.Text = BLL_TransaccionEconomica.DescribirTipo(t.TipoOperacion);
            litDetalleFecha.Text = t.FechaGeneracion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            litDetalleMonto.Text = Importe(t.MontoTotal, moneda);
            litDetalleComision.Text = Importe(t.ImporteComision, moneda) + (t.PorcentajeComision.HasValue ? " (" + Porcentaje(t.PorcentajeComision) + ")" : string.Empty);
            litDetalleGestorImporte.Text = Importe(t.ImporteGestor, moneda);
            litDetalleCargo.Text = t.TipoOperacion == BLL_TransaccionEconomica.TipoCargoCancelacion && t.PorcentajeCargo.HasValue
                ? Porcentaje(t.PorcentajeCargo) + " del valor de la reserva" + (t.ImporteReserva.HasValue ? " (" + Importe(t.ImporteReserva, moneda) + ")" : string.Empty)
                : "No corresponde";
            litDetalleReserva.Text = t.IdReserva.HasValue
                ? "N° " + t.IdReserva.Value.ToString(CultureInfo.InvariantCulture) +
                  (t.FechaSolicitada.HasValue ? " · " + t.FechaSolicitada.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : string.Empty) +
                  (t.MinutoDesde.HasValue && t.MinutoHasta.HasValue ? " de " + Hora(t.MinutoDesde.Value) + " a " + Hora(t.MinutoHasta.Value) + " hs" : string.Empty)
                : "No informada";
            litDetalleEspacio.Text = t.NombreEspacio ?? "No informado";
            litDetalleEstadoReserva.Text = (t.EstadoReserva ?? "—") +
                (t.FechaCancelacion.HasValue ? " (cancelada el " + t.FechaCancelacion.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ")" : string.Empty);
            litDetalleSolicitante.Text = string.IsNullOrWhiteSpace(t.NombreSolicitante) ? "No informado" : t.NombreSolicitante + (t.CorreoSolicitante != null ? " (" + t.CorreoSolicitante + ")" : string.Empty);
            litDetalleGestor.Text = string.IsNullOrWhiteSpace(t.NombreGestor) ? "No informado" : t.NombreGestor + (t.CorreoGestor != null ? " (" + t.CorreoGestor + ")" : string.Empty);
            litDetalleReferencia.Text = string.IsNullOrEmpty(t.ReferenciaPago) ? "No informada" : t.ReferenciaPago;
            litDetalleMedio.Text = t.MedioPago ?? "—";

            List<string> comprobantes = new List<string>();
            if (!string.IsNullOrEmpty(t.NumeroFactura) && t.TipoOperacion == BLL_TransaccionEconomica.TipoReservaAbonada) comprobantes.Add("Factura " + t.NumeroFactura);
            if (!string.IsNullOrEmpty(t.NumeroComprobante)) comprobantes.Add("Nota de débito " + t.NumeroComprobante);
            litDetalleComprobantes.Text = comprobantes.Count == 0 ? "—" : string.Join(" · ", comprobantes);
            litDetalleRegistro.Text = t.EstadoRegistro == BLL_TransaccionEconomica.RegistroPendienteControl ? "Pendiente de control administrativo" : "Registrada";
            litDetalleObservacion.Text = string.IsNullOrEmpty(t.Observacion) ? "—" : t.Observacion;

            pnlDetalle.Visible = true;
        }

        private static string Hora(int minutos)
        {
            return minutos == 1440 ? "24:00"
                : (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
