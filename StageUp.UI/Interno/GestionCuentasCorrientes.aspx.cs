using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // Pagos y cuentas corrientes desde el panel interno (ítems 5C, 5D y 6B,
    // permiso GESTIONAR_PAGOS): consulta de cuentas, NC/ND manuales,
    // anulación, liquidaciones a gestores y listado de pagos.
    public partial class GestionCuentasCorrientes : Page
    {
        private const string PermisoRequerido = "GESTIONAR_PAGOS";

        private readonly BLL_CuentaCorriente _bllCuenta = new BLL_CuentaCorriente();
        private readonly BLL_Pago _bllPago = new BLL_Pago();

        private int? IdUsuarioCuenta
        {
            get { return ViewState["IdUsuarioCuenta"] as int?; }
            set { ViewState["IdUsuarioCuenta"] = value; }
        }

        private string RolCuenta
        {
            get { return ViewState["RolCuenta"] as string ?? BLL_CuentaCorriente.RolCliente; }
            set { ViewState["RolCuenta"] = value; }
        }

        private int? IdComprobanteAAnular
        {
            get { return ViewState["IdComprobanteAAnular"] as int?; }
            set { ViewState["IdComprobanteAAnular"] = value; }
        }

        // Nombre y correo de los usuarios listados, para el encabezado de la cuenta.
        private Dictionary<int, string> NombresUsuarios
        {
            get { return ViewState["NombresUsuarios"] as Dictionary<int, string> ?? new Dictionary<int, string>(); }
            set { ViewState["NombresUsuarios"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                txtPagosDesde.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                txtPagosHasta.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                CargarCuentas();
                CargarPagos();
            }
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            if (TieneAcceso())
            {
                pnlMensaje.Visible = false;
                CargarCuentas();
            }
        }

        protected void rptCuentas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || e.CommandName != "Ver")
            {
                return;
            }

            string[] partes = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture).Split('|');
            int idUsuario;
            if (partes.Length != 2 || !int.TryParse(partes[0], out idUsuario))
            {
                return;
            }

            IdUsuarioCuenta = idUsuario;
            RolCuenta = partes[1] == BLL_CuentaCorriente.RolGestor ? BLL_CuentaCorriente.RolGestor : BLL_CuentaCorriente.RolCliente;
            IdComprobanteAAnular = null;
            pnlMensaje.Visible = false;
            LimpiarFormularios();
            CargarCuenta();
        }

        protected void btnEmitir_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdUsuarioCuenta.HasValue)
            {
                return;
            }

            decimal importe;
            if (!BLL_CuentaCorriente.IntentarLeerImporte(txtImporteComprobante.Text, out importe))
            {
                MostrarMensaje("Ingresá un importe válido (por ejemplo 1500,50).", true);
                return;
            }

            int? idReserva = null;
            if (!string.IsNullOrWhiteSpace(txtReservaComprobante.Text))
            {
                int numero;
                if (!int.TryParse(txtReservaComprobante.Text.Trim(), out numero))
                {
                    MostrarMensaje("El N° de reserva tiene que ser un número.", true);
                    return;
                }

                idReserva = numero;
            }

            ResultadoOperacion<int> resultado = _bllCuenta.EmitirComprobanteManual(
                ddlTipoComprobante.SelectedValue, IdUsuarioCuenta.Value, RolCuenta, importe,
                ddlMonedaComprobante.SelectedValue, txtMotivoComprobante.Text, idReserva,
                GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                LimpiarFormularios();
                CargarCuenta();
                CargarCuentas();
            }
        }

        protected void btnLiquidar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdUsuarioCuenta.HasValue || RolCuenta != BLL_CuentaCorriente.RolGestor)
            {
                return;
            }

            decimal importe;
            if (!BLL_CuentaCorriente.IntentarLeerImporte(txtImporteLiquidacion.Text, out importe))
            {
                MostrarMensaje("Ingresá un importe válido (por ejemplo 1500,50).", true);
                return;
            }

            ResultadoOperacion resultado = _bllCuenta.RegistrarLiquidacion(
                IdUsuarioCuenta.Value, ddlMonedaLiquidacion.SelectedValue, importe, txtDetalleLiquidacion.Text,
                GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                LimpiarFormularios();
                CargarCuenta();
                CargarCuentas();
            }
        }

        protected void rptMovimientosCuenta_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || e.CommandName != "Anular")
            {
                return;
            }

            string[] partes = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture).Split('|');
            int idComprobante;
            if (partes.Length < 1 || !int.TryParse(partes[0], out idComprobante))
            {
                return;
            }

            IdComprobanteAAnular = idComprobante;
            litComprobanteAAnular.Text = Server.HtmlEncode(partes.Length > 1 ? partes[1] : idComprobante.ToString(CultureInfo.InvariantCulture));
            txtMotivoAnulacion.Text = string.Empty;
            pnlAnular.Visible = true;
            CargarCuenta();
        }

        protected void btnConfirmarAnulacion_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdComprobanteAAnular.HasValue)
            {
                return;
            }

            ResultadoOperacion resultado = _bllCuenta.AnularComprobante(
                IdComprobanteAAnular.Value, txtMotivoAnulacion.Text, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                IdComprobanteAAnular = null;
                pnlAnular.Visible = false;
                CargarCuentas();
            }

            CargarCuenta();
        }

        protected void lnkCancelarAnulacion_Click(object sender, EventArgs e)
        {
            IdComprobanteAAnular = null;
            pnlAnular.Visible = false;
            CargarCuenta();
        }

        protected void btnFiltrarPagos_Click(object sender, EventArgs e)
        {
            if (TieneAcceso())
            {
                pnlMensaje.Visible = false;
                CargarPagos();
            }
        }

        // Solo se anulan las NC/ND manuales vigentes (no las de una cancelación
        // ni los movimientos de anulación).
        protected static bool EsAnulable(object item)
        {
            MovimientoCuentaCorriente movimiento = (MovimientoCuentaCorriente)item;
            return movimiento.IdComprobante.HasValue
                && movimiento.OrigenComprobante == "AjusteManual"
                && movimiento.EstadoComprobante == "Emitido"
                && (movimiento.TipoMovimiento == "NotaCredito" || movimiento.TipoMovimiento == "NotaDebito");
        }

        protected static string DescribirConcepto(object item)
        {
            Pago pago = (Pago)item;
            return pago.Concepto == "Deuda"
                ? "Pago de saldo deudor"
                : "Reserva N° " + pago.IdReserva + (string.IsNullOrEmpty(pago.NombreEspacio) ? string.Empty : " · " + pago.NombreEspacio);
        }

        protected static string DescribirMedio(object item)
        {
            Pago pago = (Pago)item;
            string medio = BLL_Pago.DescribirMedios(pago).Trim();
            if (medio.StartsWith("con ", StringComparison.Ordinal))
            {
                medio = medio.Substring(4);
            }

            if (pago.Estado == "Rechazado" && !string.IsNullOrEmpty(pago.MarcaTarjeta))
            {
                medio = "tarjeta " + pago.MarcaTarjeta + " terminada en " + pago.UltimosDigitos;
            }

            return string.IsNullOrEmpty(medio) ? "-" : medio;
        }

        private void CargarCuentas()
        {
            List<SaldoCuentaCorriente> cuentas = _bllCuenta.ListarCuentas(txtBuscar.Text);
            Dictionary<int, string> nombres = new Dictionary<int, string>();
            foreach (SaldoCuentaCorriente cuenta in cuentas)
            {
                nombres[cuenta.IdUsuarioExterno] = cuenta.NombreUsuario + " (" + cuenta.CorreoUsuario + ")";
            }

            Dictionary<int, string> anteriores = NombresUsuarios;
            if (IdUsuarioCuenta.HasValue && !nombres.ContainsKey(IdUsuarioCuenta.Value) && anteriores.ContainsKey(IdUsuarioCuenta.Value))
            {
                nombres[IdUsuarioCuenta.Value] = anteriores[IdUsuarioCuenta.Value];
            }

            NombresUsuarios = nombres;
            pnlSinCuentas.Visible = cuentas.Count == 0;
            rptCuentas.DataSource = cuentas;
            rptCuentas.DataBind();
        }

        private void CargarCuenta()
        {
            if (!IdUsuarioCuenta.HasValue)
            {
                pnlCuenta.Visible = false;
                return;
            }

            int idUsuario = IdUsuarioCuenta.Value;
            bool esGestor = RolCuenta == BLL_CuentaCorriente.RolGestor;
            string nombre;
            pnlCuenta.Visible = true;
            litRolCuenta.Text = esGestor ? "Cuenta de gestor" : "Cuenta de cliente";
            litUsuarioCuenta.Text = Server.HtmlEncode(NombresUsuarios.TryGetValue(idUsuario, out nombre) ? nombre : "Usuario N° " + idUsuario);
            pnlLiquidar.Visible = esGestor;

            List<SaldoCuentaCorriente> saldos = _bllCuenta.ObtenerSaldos(idUsuario, RolCuenta);
            litSinSaldosCuenta.Visible = saldos.Count == 0;
            rptSaldosCuenta.DataSource = saldos;
            rptSaldosCuenta.DataBind();

            ResultadoOperacion<List<MovimientoCuentaCorriente>> movimientos = _bllCuenta.ListarMovimientos(new FiltroMovimientos
            {
                IdUsuarioExterno = idUsuario,
                RolCuenta = RolCuenta
            });

            List<MovimientoCuentaCorriente> lista = movimientos.Exitoso ? movimientos.Valor : new List<MovimientoCuentaCorriente>();
            pnlSinMovimientosCuenta.Visible = lista.Count == 0;
            rptMovimientosCuenta.DataSource = lista;
            rptMovimientosCuenta.DataBind();
        }

        private void CargarPagos()
        {
            DateTime desde;
            DateTime hasta;
            if (!DateTime.TryParseExact(txtPagosDesde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out desde))
            {
                desde = DateTime.Today.AddDays(-30);
            }

            if (!DateTime.TryParseExact(txtPagosHasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta))
            {
                hasta = DateTime.Today;
            }

            ResultadoOperacion<List<Pago>> resultado = _bllPago.Listar(new FiltroPagos
            {
                Desde = desde,
                Hasta = hasta,
                Estado = ddlEstadoPago.SelectedValue
            });

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            pnlSinPagos.Visible = resultado.Valor.Count == 0;
            rptPagos.DataSource = resultado.Valor;
            rptPagos.DataBind();
        }

        private void LimpiarFormularios()
        {
            txtImporteComprobante.Text = string.Empty;
            txtReservaComprobante.Text = string.Empty;
            txtMotivoComprobante.Text = string.Empty;
            txtImporteLiquidacion.Text = string.Empty;
            txtDetalleLiquidacion.Text = string.Empty;
            pnlAnular.Visible = false;
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
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return false;
            }

            return true;
        }
    }
}
