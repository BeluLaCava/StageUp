using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // Cuenta corriente del usuario (ítem 6B de la segunda entrega): como
    // cliente (pagos, notas de crédito y débito, saldo a favor) y, si es
    // gestor de espacios, como gestor (ingresos, comisiones y liquidaciones).
    public partial class MiCuentaCorriente : Page
    {
        private readonly BLL_CuentaCorriente _bllCuenta = new BLL_CuentaCorriente();
        private readonly BLL_Pago _bllPago = new BLL_Pago();
        private readonly BLL_ParametroPlataforma _bllParametros = new BLL_ParametroPlataforma();

        private string RolCuenta
        {
            get { return ViewState["RolCuenta"] as string ?? BLL_CuentaCorriente.RolCliente; }
            set { ViewState["RolCuenta"] = value; }
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
                int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
                bool esGestor = GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString()
                    || _bllCuenta.ObtenerSaldos(idUsuario, BLL_CuentaCorriente.RolGestor).Count > 0;

                RolCuenta = esGestor && Request.QueryString["cuenta"] == BLL_CuentaCorriente.RolGestor
                    ? BLL_CuentaCorriente.RolGestor
                    : BLL_CuentaCorriente.RolCliente;

                pnlTabs.Visible = esGestor;
                lnkCuentaCliente.CssClass = RolCuenta == BLL_CuentaCorriente.RolCliente ? "cc-tab is-active" : "cc-tab";
                lnkCuentaGestor.CssClass = RolCuenta == BLL_CuentaCorriente.RolGestor ? "cc-tab is-active" : "cc-tab";

                CargarFiltros();
                CargarVencimientos();
                CargarSaldos();
                CargarMovimientos();
            }
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarMovimientos();
        }

        protected void lnkLimpiar_Click(object sender, EventArgs e)
        {
            txtDesde.Text = string.Empty;
            txtHasta.Text = string.Empty;
            ddlMoneda.SelectedIndex = 0;
            ddlTipo.SelectedIndex = 0;
            CargarMovimientos();
        }

        protected void btnPagarDeuda_Click(object sender, EventArgs e)
        {
            int mes;
            int anio;
            int.TryParse(ddlMes.SelectedValue, out mes);
            int.TryParse(ddlAnio.SelectedValue, out anio);
            DatosTarjeta tarjeta = new DatosTarjeta
            {
                Titular = txtTitular.Text,
                Numero = txtNumero.Text,
                MesVencimiento = mes,
                AnioVencimiento = anio,
                CodigoSeguridad = txtCodigo.Text
            };

            ResultadoOperacion<Pago> resultado = _bllPago.PagarDeuda(
                GestorDeSesion.ObtenerIdUsuarioActual().Value, hfMonedaDeuda.Value, tarjeta);

            txtNumero.Text = string.Empty;
            txtCodigo.Text = string.Empty;
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarSaldos();
            CargarMovimientos();
        }

        private void CargarFiltros()
        {
            ddlMoneda.Items.Clear();
            ddlMoneda.Items.Add(new ListItem("Todas", string.Empty));
            ddlMoneda.Items.Add(new ListItem("Pesos (ARS)", "ARS"));
            ddlMoneda.Items.Add(new ListItem("Dólares (USD)", "USD"));

            ddlTipo.Items.Clear();
            ddlTipo.Items.Add(new ListItem("Todos", string.Empty));
            foreach (KeyValuePair<string, string> tipo in BLL_CuentaCorriente.ObtenerTiposMovimiento(RolCuenta))
            {
                ddlTipo.Items.Add(new ListItem(tipo.Value, tipo.Key));
            }
        }

        private void CargarVencimientos()
        {
            ddlMes.Items.Clear();
            ddlMes.Items.Add(new ListItem("Mes", string.Empty));
            for (int mes = 1; mes <= 12; mes++)
            {
                ddlMes.Items.Add(new ListItem(mes.ToString("00", CultureInfo.InvariantCulture), mes.ToString(CultureInfo.InvariantCulture)));
            }

            ddlAnio.Items.Clear();
            ddlAnio.Items.Add(new ListItem("Año", string.Empty));
            for (int anio = DateTime.Today.Year; anio <= DateTime.Today.Year + 15; anio++)
            {
                ddlAnio.Items.Add(new ListItem(anio.ToString(CultureInfo.InvariantCulture), anio.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void CargarSaldos()
        {
            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<SaldoCuentaCorriente> saldos = _bllCuenta.ObtenerSaldos(idUsuario, RolCuenta);
            bool esGestor = RolCuenta == BLL_CuentaCorriente.RolGestor;

            litTituloSaldo.Text = esGestor ? "Saldo como gestor" : "Saldo como cliente";
            pnlSinSaldos.Visible = saldos.Count == 0;
            rptSaldos.DataSource = saldos;
            rptSaldos.DataBind();

            litNotaCuenta.Text = Server.HtmlEncode(esGestor
                ? "Cada reserva pagada de tus espacios suma su importe y descuenta la comisión de StageUp (" +
                  _bllParametros.ObtenerDecimal(BLL_ParametroPlataforma.ComisionPlataformaPorcentaje).ToString("0.##", CultureInfo.InvariantCulture) +
                  " %). StageUp te transfiere el saldo con liquidaciones periódicas."
                : "El saldo a favor (por ejemplo, de una reserva cancelada) se usa automáticamente cuando pagás tu próxima reserva, si así lo elegís.");

            SaldoCuentaCorriente deuda = esGestor ? null : saldos.FirstOrDefault(s => s.Saldo < 0);
            pnlDeuda.Visible = deuda != null;
            if (deuda != null)
            {
                hfMonedaDeuda.Value = deuda.Moneda;
                litDeuda.Text = Server.HtmlEncode(Importe(-deuda.Saldo, deuda.Moneda));
                btnPagarDeuda.Text = "Pagar " + Importe(-deuda.Saldo, deuda.Moneda);
            }
        }

        private void CargarMovimientos()
        {
            DateTime desde;
            DateTime hasta;
            FiltroMovimientos filtro = new FiltroMovimientos
            {
                IdUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value,
                RolCuenta = RolCuenta,
                Moneda = ddlMoneda.SelectedValue,
                TipoMovimiento = ddlTipo.SelectedValue,
                Desde = DateTime.TryParseExact(txtDesde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out desde) ? desde : (DateTime?)null,
                Hasta = DateTime.TryParseExact(txtHasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta) ? hasta : (DateTime?)null
            };

            ResultadoOperacion<List<MovimientoCuentaCorriente>> resultado = _bllCuenta.ListarMovimientos(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                rptMovimientos.DataSource = null;
                rptMovimientos.DataBind();
                return;
            }

            pnlSinMovimientos.Visible = resultado.Valor.Count == 0;
            rptMovimientos.DataSource = resultado.Valor;
            rptMovimientos.DataBind();
        }

        protected string DescribirSaldo(object item)
        {
            SaldoCuentaCorriente saldo = (SaldoCuentaCorriente)item;
            if (RolCuenta == BLL_CuentaCorriente.RolGestor)
            {
                return saldo.Saldo >= 0 ? "Saldo a liquidar (" + saldo.Moneda + ")" : "Saldo deudor (" + saldo.Moneda + ")";
            }

            return saldo.Saldo > 0 ? "Saldo a favor (" + saldo.Moneda + ")"
                : saldo.Saldo < 0 ? "Saldo deudor (" + saldo.Moneda + ")"
                : "Saldo (" + saldo.Moneda + ")";
        }

        protected static string Importe(decimal importe, string moneda)
        {
            return BLL_CuentaCorriente.FormatearImporte(importe, moneda);
        }

        protected static string ImporteConSigno(decimal importe, string moneda)
        {
            return (importe < 0 ? "− " : importe > 0 ? "+ " : string.Empty) + Importe(Math.Abs(importe), moneda);
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
