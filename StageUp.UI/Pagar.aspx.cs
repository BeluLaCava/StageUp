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

        protected void chkUsarSaldo_CheckedChanged(object sender, EventArgs e)
        {
            CargarContexto(false);
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

            ResultadoOperacion<Pago> resultado = _bllPago.PagarReserva(IdReserva, idUsuario, tarjeta, pnlSaldo.Visible && chkUsarSaldo.Checked);

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
            if (primeraVez)
            {
                chkUsarSaldo.Checked = tieneSaldo;
            }

            chkUsarSaldo.Text = " Usar mi saldo a favor (" + Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(contexto.SaldoDisponible, moneda)) + " disponible)";

            decimal conSaldo = tieneSaldo && chkUsarSaldo.Checked ? contexto.ImporteConSaldo : 0m;
            decimal conTarjeta = contexto.Total - conSaldo;
            litConSaldo.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(conSaldo, moneda));
            litConTarjeta.Text = Server.HtmlEncode(BLL_CuentaCorriente.FormatearImporte(conTarjeta, moneda));

            pnlTarjeta.Visible = conTarjeta > 0;
            btnPagar.Text = conTarjeta > 0
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
