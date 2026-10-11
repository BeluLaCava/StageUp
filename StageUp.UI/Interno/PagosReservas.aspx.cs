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
    // CU-002-001 Registrar y controlar pagos de reservas: pantalla "Pagos de
    // reservas" (A6 a A10) y registro de avisos del medio de pago externo
    // simulado (pasos 1 a 9 y A1 a A5).
    public partial class PagosReservas : Page
    {
        private const string PermisoRequerido = "CONTROLAR_PAGOS_RESERVAS";

        private readonly BLL_Pago _bllPago = new BLL_Pago();

        protected bool AvisoAbierto { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            // A6 paso 2 y A7.
            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            if (!IsPostBack)
            {
                CargarPagos();
            }
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarPagos();
        }

        protected void lnkLimpiar_Click(object sender, EventArgs e)
        {
            ddlEstado.ClearSelection();
            txtDesde.Text = string.Empty;
            txtHasta.Text = string.Empty;
            txtTexto.Text = string.Empty;
            pnlMensaje.Visible = false;
            CargarPagos();
        }

        protected void rptPagos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Ver")
            {
                return;
            }

            string argumento = Convert.ToString(e.CommandArgument);
            int id;
            if (argumento.Length < 3 || !int.TryParse(argumento.Substring(2), out id))
            {
                return;
            }

            ResultadoOperacion<DetallePagoReserva> resultado = argumento.StartsWith("p:", StringComparison.Ordinal)
                ? _bllPago.ObtenerDetalleAdministracion(id, null)
                : _bllPago.ObtenerDetalleAdministracion(null, id);

            if (!resultado.Exitoso)
            {
                // A10: se vuelve al listado con el aviso.
                MostrarMensaje(resultado.Mensaje, true);
                CargarPagos();
                return;
            }

            MostrarDetalle(resultado.Valor);
        }

        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            pnlDetalle.Visible = false;
        }

        protected void btnRegistrarAviso_Click(object sender, EventArgs e)
        {
            AvisoAbierto = true;
            int idReserva;
            decimal importe;
            DateTime fecha;

            AvisoMedioPago aviso = new AvisoMedioPago
            {
                IdReserva = int.TryParse(txtAvisoReserva.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out idReserva) ? idReserva : (int?)null,
                IdentificadorExterno = txtAvisoIdentificador.Text,
                Importe = BLL_CuentaCorriente.IntentarLeerImporte(txtAvisoImporte.Text, out importe) ? importe : (decimal?)null,
                Moneda = ddlAvisoMoneda.SelectedValue,
                FechaOperacion = DateTime.TryParse(txtAvisoFecha.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha) ? fecha : (DateTime?)null,
                Medio = ddlAvisoMedio.SelectedValue,
                EstadoInformado = ddlAvisoEstado.SelectedValue,
                Motivo = txtAvisoMotivo.Text
            };

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion<int> resultado = _bllPago.RegistrarAvisoMedioExterno(aviso, idResponsable);

            string mensaje = resultado.Mensaje;
            if (resultado.Exitoso && resultado.Valor > 0)
            {
                mensaje += " (Pago N° " + resultado.Valor.ToString(CultureInfo.InvariantCulture) + ")";
            }

            MostrarMensaje(mensaje, !resultado.Exitoso || resultado.CodigoAlternativo == "A1" || resultado.CodigoAlternativo == "A2");
            if (resultado.Exitoso)
            {
                txtAvisoReserva.Text = string.Empty;
                txtAvisoIdentificador.Text = string.Empty;
                txtAvisoImporte.Text = string.Empty;
                txtAvisoFecha.Text = string.Empty;
                txtAvisoMotivo.Text = string.Empty;
                ddlAvisoMedio.ClearSelection();
                ddlAvisoEstado.ClearSelection();
            }

            CargarPagos();
        }

        // ------------------------------------------------------------------
        // Ayudas para la grilla
        // ------------------------------------------------------------------
        protected string ArgumentoDetalle(object dato)
        {
            ItemPagoReserva item = (ItemPagoReserva)dato;
            return item.IdPago.HasValue
                ? "p:" + item.IdPago.Value.ToString(CultureInfo.InvariantCulture)
                : "r:" + (item.IdReserva ?? 0).ToString(CultureInfo.InvariantCulture);
        }

        protected string DescribirReserva(object dato)
        {
            ItemPagoReserva item = (ItemPagoReserva)dato;
            string texto = item.IdReserva.HasValue
                ? (item.Concepto == "Deuda" ? "Cargo / deuda" : "Reserva N° " + item.IdReserva.Value.ToString(CultureInfo.InvariantCulture))
                : (item.Concepto == "Deuda" ? "Cargo por cancelación / deuda" : "Sin reserva asociada");
            if (!string.IsNullOrEmpty(item.NombreEspacio))
            {
                texto += "<br /><small>" + HttpUtility.HtmlEncode(item.NombreEspacio) + "</small>";
            }

            if (item.Estado == "EsperandoPago" && item.FechaLimitePago.HasValue)
            {
                texto += "<br /><small>Vence el " + item.FechaLimitePago.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + "</small>";
            }

            return texto;
        }

        private void CargarPagos()
        {
            DateTime desde;
            DateTime hasta;
            FiltroPagosReserva filtro = new FiltroPagosReserva
            {
                Grupo = ddlEstado.SelectedValue,
                Desde = DateTime.TryParse(txtDesde.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out desde) ? desde : (DateTime?)null,
                Hasta = DateTime.TryParse(txtHasta.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta) ? hasta : (DateTime?)null,
                Texto = txtTexto.Text
            };

            bool hayFiltros = filtro.Grupo != BLL_Pago.GrupoTodos || filtro.Desde.HasValue || filtro.Hasta.HasValue ||
                              !string.IsNullOrWhiteSpace(filtro.Texto);

            ResultadoOperacion<List<ItemPagoReserva>> resultado = _bllPago.ListarAdministracion(filtro);
            List<ItemPagoReserva> pagos = resultado.Exitoso ? resultado.Valor : new List<ItemPagoReserva>();
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
            }

            bool noHayNinguno = pagos.Count == 0 && (!hayFiltros || _bllPago.NoHayPagos());
            pnlSinPagos.Visible = pagos.Count == 0 && noHayNinguno;
            pnlSinResultados.Visible = pagos.Count == 0 && !noHayNinguno;
            litCantidad.Text = pagos.Count == 0 ? string.Empty
                : pagos.Count == 1 ? "1 pago" : pagos.Count.ToString(CultureInfo.InvariantCulture) + " pagos" +
                  (pagos.Count >= 500 ? " (se muestran los 500 más recientes: usá los filtros para acotar)" : string.Empty);

            rptPagos.DataSource = pagos;
            rptPagos.DataBind();
        }

        private void MostrarDetalle(DetallePagoReserva d)
        {
            string moneda = d.Moneda ?? "ARS";
            litDetalleTitulo.Text = d.IdPago > 0
                ? "Pago N° " + d.IdPago.ToString(CultureInfo.InvariantCulture)
                : "Reserva N° " + d.IdReserva + " (sin pago registrado)";
            lblDetalleEstado.Text = HttpUtility.HtmlEncode(BLL_Pago.DescribirEstado(d.Estado));
            lblDetalleEstado.CssClass = "pago-estado pago-estado-" + GrupoDe(d.Estado);

            litDetalleImporte.Text = BLL_CuentaCorriente.FormatearImporte(d.ImporteTotal, moneda) +
                (d.ImporteSaldo > 0 ? " (" + BLL_CuentaCorriente.FormatearImporte(d.ImporteSaldo, moneda) + " con saldo a favor)" : string.Empty);
            litDetalleFecha.Text = (d.FechaOperacion ?? d.FechaPago).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            litDetalleIdentificador.Text = d.IdentificadorExterno ?? "—";
            litDetalleMedio.Text = d.IdPago > 0 ? BLL_Pago.DescribirMedioPago(d) : "—";
            litDetalleEstadoInformado.Text = d.EstadoInformado ?? "—";
            litDetalleOrigen.Text = d.Origen == "MedioExterno"
                ? "Aviso del medio de pago" + (string.IsNullOrEmpty(d.NombreInternoRegistro) ? string.Empty : " (cargado por " + d.NombreInternoRegistro + ")")
                : "Pago en StageUp";
            litDetalleReserva.Text = d.IdReserva.HasValue
                ? (d.Concepto == "Deuda" ? "Cargo / deuda de la cuenta corriente" : "Reserva N° " + d.IdReserva.Value.ToString(CultureInfo.InvariantCulture))
                : (d.Concepto == "Deuda" ? "Cargo por cancelación / deuda de la cuenta corriente" : "Sin reserva asociada");
            litDetalleEspacio.Text = string.IsNullOrEmpty(d.NombreEspacio) ? "—" : d.NombreEspacio + " · " + BLL_Pago.DescribirFechaReserva(d);
            litDetalleEstadoReserva.Text = string.IsNullOrEmpty(d.EstadoReserva) ? "—"
                : d.EstadoReserva + " · pago " + (d.EstadoPagoReserva ?? "-") +
                  (d.FechaLimitePago.HasValue && d.EstadoPagoReserva == BLL_Pago.EstadoPendiente
                      ? " (vence el " + d.FechaLimitePago.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + ")"
                      : string.Empty);
            litDetalleSolicitante.Text = string.IsNullOrWhiteSpace(d.NombreUsuario) ? "—" : d.NombreUsuario + " (" + d.CorreoUsuario + ")";
            litDetalleGestor.Text = string.IsNullOrWhiteSpace(d.NombreGestor) ? "—" : d.NombreGestor + " (" + d.CorreoGestor + ")";
            litDetalleTransaccion.Text = d.Estado == "Aprobado" && d.PorcentajeComisionPlataforma.HasValue
                ? "Comisión StageUp " + d.PorcentajeComisionPlataforma.Value.ToString("0.##", CultureInfo.InvariantCulture) + " %: " +
                  BLL_CuentaCorriente.FormatearImporte(d.ImporteComisionPlataforma ?? 0m, moneda) + " · al gestor: " +
                  BLL_CuentaCorriente.FormatearImporte(d.ImporteTotal - (d.ImporteComisionPlataforma ?? 0m), moneda)
                : "Sin importes acreditados todavía";
            litDetalleFactura.Text = string.IsNullOrEmpty(d.NumeroFactura) ? "Sin factura (el pago no está confirmado)"
                : d.NumeroFactura + (d.FechaFactura.HasValue ? " del " + d.FechaFactura.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : string.Empty);
            litDetalleObservacion.Text = !string.IsNullOrEmpty(d.Observacion) ? d.Observacion
                : !string.IsNullOrEmpty(d.MotivoRechazo) ? d.MotivoRechazo : "—";

            pnlDetalle.Visible = true;
        }

        private static string GrupoDe(string estado)
        {
            switch (estado)
            {
                case "Aprobado": return BLL_Pago.GrupoConfirmado;
                case "Rechazado": return BLL_Pago.GrupoFallido;
                case "Anulado":
                case "Vencido": return BLL_Pago.GrupoCancelado;
                case "Pendiente":
                case "EsperandoPago": return BLL_Pago.GrupoPendiente;
                default: return BLL_Pago.GrupoEnRevision;
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
