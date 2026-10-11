using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // Pago de una reserva aceptada (ítems 5B y 5D de la segunda entrega).
    public partial class Pagar : Page
    {
        private readonly BLL_Pago _bllPago = new BLL_Pago();

        private int IdReserva
        {
            get { return ViewState["IdReserva"] as int? ?? 0; }
            set { ViewState["IdReserva"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!IsPostBack)
            {
                int idReserva;
                if (!int.TryParse(Request.QueryString["reserva"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idReserva))
                {
                    MostrarNoDisponible("No se indicó qué reserva pagar.");
                    return;
                }

                IdReserva = idReserva;
                CargarVencimientos();
                CargarContexto(true);
            }
        }

        protected void rblModoPago_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarContexto(false);
        }

        protected void btnActualizarResumen_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarContexto(false);
        }

        // Lo que eligió el cliente. Sin saldo a favor, se paga con tarjeta.
        private EleccionPago LeerEleccion(out bool eligio)
        {
            eligio = !pnlSaldo.Visible || rblModoPago.SelectedItem != null;
            ModoPago modo = ModoPago.Tarjeta;
            if (pnlSaldo.Visible && rblModoPago.SelectedItem != null)
            {
                Enum.TryParse(rblModoPago.SelectedValue, out modo);
            }

            decimal importeSaldo;
            BLL_CuentaCorriente.IntentarLeerImporte(txtImporteSaldo.Text, out importeSaldo);
            return new EleccionPago { Modo = modo, ImporteSaldo = importeSaldo };
        }

        protected void btnPagar_Click(object sender, EventArgs e)
        {
            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            DatosTarjeta tarjeta = null;
            if (pnlTarjeta.Visible)
            {
                int mes;
                int anio;
                int.TryParse(ddlMes.SelectedValue, out mes);
                int.TryParse(ddlAnio.SelectedValue, out anio);
                tarjeta = new DatosTarjeta
                {
                    Titular = txtTitular.Text,
                    Numero = txtNumero.Text,
                    MesVencimiento = mes,
                    AnioVencimiento = anio,
                    CodigoSeguridad = txtCodigo.Text
                };
            }

            bool eligio;
            EleccionPago eleccion = LeerEleccion(out eligio);
            if (!eligio)
            {
                MostrarMensaje("Elegí cómo querés pagar: con tarjeta, con tu saldo a favor o combinando los dos.", true);
                CargarContexto(false);
                return;
            }

            ResultadoOperacion<Pago> resultado = _bllPago.PagarReserva(IdReserva, idUsuario, tarjeta, eleccion);

            // El número y el código no se vuelven a mostrar en la página.
            txtNumero.Text = string.Empty;
            txtCodigo.Text = string.Empty;

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarContexto(false);
                return;
            }

            Pago pago = resultado.Valor;
            pnlPago.Visible = false;
            pnlExito.Visible = true;
            litExitoNumero.Text = pago.IdPago.ToString(CultureInfo.InvariantCulture);
            litExitoReserva.Text = "N° " + IdReserva.ToString(CultureInfo.InvariantCulture) + " · " + litEspacio.Text;
            litExitoTotal.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(pago.ImporteTotal, pago.Moneda));
            litExitoMedios.Text = Server.HtmlEncode(DescribirMediosParaComprobante(pago));
            lnkVerFactura.NavigateUrl = "~/DetallePago.aspx?id=" + pago.IdPago.ToString(CultureInfo.InvariantCulture);
            MostrarMensaje(resultado.Mensaje, false);
        }

        private void CargarContexto(bool primeraVez)
        {
            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<ContextoPagoReserva> resultado = _bllPago.ObtenerContextoPago(IdReserva, idUsuario);
            if (!resultado.Exitoso)
            {
                MostrarNoDisponible(resultado.Mensaje);
                return;
            }

            ContextoPagoReserva contexto = resultado.Valor;
            Reserva reserva = contexto.Reserva;
            string moneda = reserva.Moneda ?? "ARS";

            litEspacio.Text = Server.HtmlEncode(reserva.NombreEspacio);
            litNumeroReserva.Text = reserva.IdReserva.ToString(CultureInfo.InvariantCulture);
            litFecha.Text = reserva.FechaSolicitada.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            litHorario.Text = reserva.MinutoDesde.HasValue && reserva.MinutoHasta.HasValue
                ? FormatearHora(reserva.MinutoDesde.Value) + " a " + FormatearHora(reserva.MinutoHasta.Value) + " hs"
                : "-";
            litTotal.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(contexto.Total, moneda));
            litVence.Text = reserva.FechaLimitePago.HasValue
                ? "Tenés tiempo hasta el " + reserva.FechaLimitePago.Value.ToString("dd/MM/yyyy 'a las' HH:mm", CultureInfo.InvariantCulture) +
                  " hs. Si no se paga, la reserva se cancela sola, sin cargo."
                : "Pagala para confirmar la reserva.";

            bool tieneSaldo = contexto.SaldoDisponible > 0;
            pnlSaldo.Visible = tieneSaldo;
            if (tieneSaldo)
            {
                litSaldoDisponible.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(contexto.SaldoDisponible, moneda));
                string seleccionado = rblModoPago.SelectedItem == null ? null : rblModoPago.SelectedValue;
                rblModoPago.Items.Clear();
                rblModoPago.Items.Add(new ListItem("Todo con tarjeta de crédito", ModoPago.Tarjeta.ToString()));
                ListItem soloSaldo = new ListItem("Todo con mi saldo a favor", ModoPago.Saldo.ToString());
                if (contexto.SaldoDisponible < contexto.Total)
                {
                    soloSaldo.Text += " (no alcanza para toda la reserva)";
                    soloSaldo.Enabled = false;
                }

                rblModoPago.Items.Add(soloSaldo);
                rblModoPago.Items.Add(new ListItem("Una parte con saldo y el resto con tarjeta", ModoPago.Combinado.ToString()));
                ListItem item = seleccionado == null ? null : rblModoPago.Items.FindByValue(seleccionado);
                if (item != null && item.Enabled)
                {
                    item.Selected = true;
                }

                if (primeraVez || txtImporteSaldo.Text.Trim().Length == 0)
                {
                    txtImporteSaldo.Text = Math.Min(contexto.SaldoDisponible, contexto.Total - 0.01m).ToString("0.00", CultureInfo.InvariantCulture);
                }
            }

            bool eligio;
            EleccionPago eleccion = LeerEleccion(out eligio);
            pnlImporteSaldo.Visible = tieneSaldo && eleccion.Modo == ModoPago.Combinado && eligio;

            litResumenTotal.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(contexto.Total, moneda));
            litResumenDisponible.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(contexto.SaldoDisponible, moneda));

            if (!eligio)
            {
                litConSaldo.Text = "-";
                litConTarjeta.Text = "-";
                litResumenRestante.Text = "-";
                litResumenMedio.Text = "Elegí cómo querés pagar";
                pnlTarjeta.Visible = false;
                btnPagar.Text = "Pagar " + BLL_CuentaCorriente.FormatearImporte(contexto.Total, moneda);
                return;
            }

            ResultadoOperacion<ResumenPago> resumen = _bllPago.CalcularResumen(contexto, eleccion);
            if (!resumen.Exitoso)
            {
                MostrarMensaje(resumen.Mensaje, true);
                litConSaldo.Text = "-";
                litConTarjeta.Text = "-";
                litResumenRestante.Text = "-";
                litResumenMedio.Text = "-";
                pnlTarjeta.Visible = false;
                return;
            }

            ResumenPago r = resumen.Valor;
            litConSaldo.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(r.SaldoAUsar, moneda));
            litConTarjeta.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(r.ImporteTarjeta, moneda));
            litResumenRestante.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(r.SaldoRestante, moneda));
            litResumenMedio.Text = Server.HtmlEncode(r.MedioDescripcion);

            pnlTarjeta.Visible = r.ImporteTarjeta > 0;
            btnPagar.Text = r.ImporteTarjeta > 0
                ? "Pagar " + BLL_CuentaCorriente.FormatearImporte(contexto.Total, moneda)
                : "Pagar con mi saldo a favor";
        }

        private void CargarVencimientos()
        {
            ddlMes.Items.Clear();
            ddlMes.Items.Add(new ListItem("Mes", string.Empty));
            for (int mes = 1; mes <= 12; mes++)
            {
                string texto = mes.ToString("00", CultureInfo.InvariantCulture);
                ddlMes.Items.Add(new ListItem(texto, mes.ToString(CultureInfo.InvariantCulture)));
            }

            ddlAnio.Items.Clear();
            ddlAnio.Items.Add(new ListItem("Año", string.Empty));
            for (int anio = DateTime.Today.Year; anio <= DateTime.Today.Year + 15; anio++)
            {
                string texto = anio.ToString(CultureInfo.InvariantCulture);
                ddlAnio.Items.Add(new ListItem(texto, texto));
            }
        }

        private static string DescribirMediosParaComprobante(Pago pago)
        {
            string medios = BLL_Pago.DescribirMedios(pago).Trim();
            if (medios.StartsWith("con ", StringComparison.Ordinal))
            {
                medios = medios.Substring(4);
            }

            medios = medios.Length == 0 ? medios : char.ToUpperInvariant(medios[0]) + medios.Substring(1);
            return pago.ImporteTarjeta > 0 && !string.IsNullOrEmpty(pago.CodigoAutorizacion)
                ? medios + " (autorización " + pago.CodigoAutorizacion + ")"
                : medios;
        }

        private static string FormatearHora(int minutos)
        {
            return minutos == 1440
                ? "24:00"
                : (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private void MostrarNoDisponible(string mensaje)
        {
            pnlPago.Visible = false;
            pnlExito.Visible = false;
            pnlNoDisponible.Visible = true;
            litNoDisponible.Text = Server.HtmlEncode(mensaje);
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
