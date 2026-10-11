using System;
using System.Globalization;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // CU-002-001: detalle del pago aprobado de una reserva y su factura
    // simulada, para el solicitante que pagó y para el gestor del espacio.
    public partial class DetallePago : Page
    {
        private readonly BLL_Pago _bllPago = new BLL_Pago();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (IsPostBack)
            {
                return;
            }

            int idPago;
            int idReserva;
            int? pago = int.TryParse(Request.QueryString["id"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idPago) ? idPago : (int?)null;
            int? reserva = int.TryParse(Request.QueryString["reserva"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idReserva) ? idReserva : (int?)null;
            if (!pago.HasValue && !reserva.HasValue)
            {
                MostrarNoDisponible("No se indicó qué pago mostrar.");
                return;
            }

            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<DetallePagoReserva> resultado = _bllPago.ObtenerDetalleParaUsuario(pago, reserva, idUsuario);
            if (!resultado.Exitoso)
            {
                MostrarNoDisponible(resultado.Mensaje);
                return;
            }

            Mostrar(resultado.Valor, idUsuario);
        }

        private void Mostrar(DetallePagoReserva d, int idUsuario)
        {
            string moneda = d.Moneda ?? "ARS";
            bool esGestor = d.IdUsuarioGestor == idUsuario && d.IdUsuarioExterno != idUsuario;

            litNumeroPago.Text = d.IdPago.ToString(CultureInfo.InvariantCulture);
            litEstado.Text = "Pago " + BLL_Pago.DescribirEstado(d.Estado).ToLowerInvariant();
            litIdentificador.Text = d.IdentificadorExterno ?? "-";
            litFecha.Text = (d.FechaOperacion ?? d.FechaPago).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            litMedio.Text = BLL_Pago.DescribirMedioPago(d);
            litImporte.Text = BLL_CuentaCorriente.FormatearImporte(d.ImporteTotal, moneda);
            lnkReserva.Text = "N° " + (d.IdReserva.HasValue ? d.IdReserva.Value.ToString(CultureInfo.InvariantCulture) : "-");
            lnkReserva.NavigateUrl = d.IdReserva.HasValue ? "~/DetalleReserva.aspx?id=" + d.IdReserva.Value.ToString(CultureInfo.InvariantCulture) : null;
            litEspacio.Text = d.NombreEspacio ?? "-";
            litFechaReserva.Text = BLL_Pago.DescribirFechaReserva(d);
            litSolicitante.Text = d.NombreUsuario ?? "-";
            litGestor.Text = d.NombreGestor ?? "-";

            pnlLiquidacionGestor.Visible = esGestor;
            if (esGestor)
            {
                decimal comision = d.ImporteComisionPlataforma ?? 0m;
                litComision.Text = BLL_CuentaCorriente.FormatearImporte(comision, moneda) +
                    (d.PorcentajeComisionPlataforma.HasValue
                        ? " (" + d.PorcentajeComisionPlataforma.Value.ToString("0.##", CultureInfo.InvariantCulture) + " %)"
                        : string.Empty);
                litNeto.Text = BLL_CuentaCorriente.FormatearImporte(d.ImporteTotal - comision, moneda);
                lnkVolver.NavigateUrl = "~/SolicitudesRecibidas.aspx";
                lnkVolver.Text = "Volver a Solicitudes recibidas";
            }

            pnlFactura.Visible = d.IdComprobanteFactura.HasValue;
            if (d.IdComprobanteFactura.HasValue)
            {
                DatosFacturacion emisor = BLL_Pago.ObtenerDatosFacturacion();
                litEmisor.Text = emisor.RazonSocial;
                litEmisorDatos.Text = emisor.DatosFiscales;
                litNumeroFactura.Text = d.NumeroFactura;
                litFechaFactura.Text = d.FechaFactura.HasValue ? d.FechaFactura.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : string.Empty;
                litFacturaCliente.Text = d.NombreUsuario + (string.IsNullOrEmpty(d.CorreoUsuario) ? string.Empty : " (" + d.CorreoUsuario + ")");
                litFacturaConcepto.Text = "Reserva N° " + d.IdReserva + " · " + d.NombreEspacio + " · " + BLL_Pago.DescribirFechaReserva(d);
                litFacturaFormaPago.Text = BLL_Pago.DescribirMedioPago(d);
                litFacturaTotal.Text = BLL_CuentaCorriente.FormatearImporte(d.ImporteTotal, moneda);
                litFacturaLeyenda.Text = emisor.Leyenda;
            }
        }

        private void MostrarNoDisponible(string mensaje)
        {
            pnlDetalle.Visible = false;
            pnlNoDisponible.Visible = true;
            litNoDisponible.Text = mensaje;
        }
    }
}
