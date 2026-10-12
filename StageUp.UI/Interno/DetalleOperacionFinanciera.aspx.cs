using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // CU-002-003 pasos 11 a 14: pantalla "Detalle de operación financiera".
    // Si el detalle no está disponible vuelve al historial con el aviso (A7).
    public partial class DetalleOperacionFinanciera : Page
    {
        private readonly BLL_HistorialFinanciero _bll = new BLL_HistorialFinanciero();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno(HistorialFinanciero.PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            string volver = ConsultaParaVolver();
            lnkVolver.NavigateUrl = "~/Interno/HistorialFinanciero.aspx" + (volver.Length == 0 ? string.Empty : "?" + volver);

            if (IsPostBack)
            {
                return;
            }

            ResultadoOperacion<OperacionFinanciera> resultado = _bll.ObtenerDetalle(Request.QueryString["op"]);
            if (!resultado.Exitoso)
            {
                // A7: vuelve al historial con el aviso.
                Response.Redirect("~/Interno/HistorialFinanciero.aspx?detalle=no_disponible" + (volver.Length == 0 ? string.Empty : "&" + volver), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            Mostrar(resultado.Valor);
        }

        protected static string Importe(object importe, object moneda)
        {
            return importe == null ? "—" : BLL_CuentaCorriente.FormatearImporte(Convert.ToDecimal(importe), Convert.ToString(moneda));
        }

        protected static string ImporteConSigno(object importe, object moneda)
        {
            decimal valor = Convert.ToDecimal(importe);
            return (valor < 0 ? "− " : "+ ") + BLL_CuentaCorriente.FormatearImporte(Math.Abs(valor), Convert.ToString(moneda));
        }

        protected static string TipoComprobante(string tipo)
        {
            return tipo == "FC" ? "Factura" : tipo == "NC" ? "Nota de crédito" : tipo == "ND" ? "Nota de débito" : tipo;
        }

        // Solo se aceptan los filtros conocidos del historial.
        private string ConsultaParaVolver()
        {
            string volver = Request.QueryString["volver"];
            if (string.IsNullOrWhiteSpace(volver) || volver.Length > 400)
            {
                return string.Empty;
            }

            string[] claves = { "desde", "hasta", "estado", "tipo", "gestor", "q" };
            List<string> partes = new List<string>();
            foreach (string parte in volver.Split('&'))
            {
                int igual = parte.IndexOf('=');
                if (igual > 0 && claves.Contains(parte.Substring(0, igual)))
                {
                    partes.Add(parte);
                }
            }

            return string.Join("&", partes);
        }

        private void Mostrar(OperacionFinanciera o)
        {
            string moneda = o.Moneda ?? "ARS";
            litCodigo.Text = o.Codigo;
            litTipo.Text = BLL_HistorialFinanciero.DescribirTipo(o.TipoOperacion);
            lblEstado.Text = Server.HtmlEncode(BLL_HistorialFinanciero.DescribirEstado(o.EstadoPago));
            lblEstado.CssClass = "pago-estado pago-estado-" + o.EstadoPago;

            List<string> avisos = BLL_HistorialFinanciero.EstadosParticulares(o);
            rptAvisos.DataSource = avisos;
            rptAvisos.DataBind();
            rptAvisos.Visible = avisos.Count > 0;

            litMonto.Text = Importe(o.MontoTotal, moneda);
            litComision.Text = o.ImporteComision.HasValue
                ? Importe(o.ImporteComision, moneda) + (o.PorcentajeComision.HasValue ? " (" + Porcentaje(o.PorcentajeComision.Value) + ")" : string.Empty)
                : (o.TipoOperacion == "PagoNoConfirmado" || o.TipoOperacion == "ReservaSinPago" ? "Se calcula al confirmarse el pago" : "No corresponde");
            litImporteGestor.Text = o.ImporteGestor.HasValue ? Importe(o.ImporteGestor, moneda) : "No corresponde";
            litCargo.Text = o.TipoOperacion == "CargoCancelacion"
                ? Importe(o.ImporteCargo ?? o.MontoTotal, moneda) + (o.PorcentajeCargo.HasValue ? " (" + Porcentaje(o.PorcentajeCargo.Value) + " del valor de la reserva)" : string.Empty)
                : "No corresponde";
            litEstadoPago.Text = BLL_HistorialFinanciero.DescribirEstado(o.EstadoPago);
            litFecha.Text = o.FechaOperacion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

            litReserva.Text = o.IdReserva.HasValue
                ? "N° " + o.IdReserva.Value.ToString(CultureInfo.InvariantCulture) +
                  (o.FechaSolicitada.HasValue ? " · " + o.FechaSolicitada.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : string.Empty) +
                  (o.MinutoDesde.HasValue && o.MinutoHasta.HasValue ? " de " + Hora(o.MinutoDesde.Value) + " a " + Hora(o.MinutoHasta.Value) + " hs" : string.Empty)
                : "No corresponde";
            litEspacio.Text = string.IsNullOrWhiteSpace(o.NombreEspacio) ? "No corresponde" : o.NombreEspacio;
            litEstadoReserva.Text = string.IsNullOrEmpty(o.EstadoReserva) ? "—"
                : o.EstadoReserva + (o.FechaCancelacion.HasValue ? " (cancelada el " + o.FechaCancelacion.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ")" : string.Empty);
            litSolicitante.Text = Persona(o.NombreSolicitante, o.CorreoSolicitante);
            litGestor.Text = Persona(o.NombreGestor, o.CorreoGestor);
            litValorReserva.Text = o.ImporteReserva.HasValue ? Importe(o.ImporteReserva, moneda) : "—";

            litReferencia.Text = string.IsNullOrEmpty(o.ReferenciaPago) ? "No informada" : o.ReferenciaPago;
            litMedio.Text = o.MedioPago ?? (o.IdPago.HasValue ? "—" : "No corresponde");
            litFormaPago.Text = FormaPago(o);
            Comprobante factura = o.Comprobantes.FirstOrDefault(c => c.Tipo == "FC");
            litFactura.Text = factura != null
                ? factura.Numero + " (" + BLL_ParametroPlataforma.TextoEstado(factura.Estado == "Anulado"
                    ? BLL_ParametroPlataforma.EstadoFacturaAnulada : BLL_ParametroPlataforma.EstadoFacturaEmitida).ToLowerInvariant() + ")"
                : !string.IsNullOrEmpty(o.NumeroComprobante) ? o.NumeroComprobante
                : (o.TipoOperacion == "ReservaAbonada" || o.TipoOperacion == "PagoNoConfirmado" || o.TipoOperacion == "ReservaSinPago" ? BLL_HistorialFinanciero.TextoFacturaPendiente() : "No corresponde");
            litObservacion.Text = string.IsNullOrWhiteSpace(o.Observacion) ? "—" : o.Observacion;

            rptComprobantes.DataSource = o.Comprobantes;
            rptComprobantes.DataBind();
            rptComprobantes.Visible = o.Comprobantes.Count > 0;
            pnlSinComprobantes.Visible = o.Comprobantes.Count == 0;

            rptMovimientos.DataSource = o.Movimientos;
            rptMovimientos.DataBind();
            rptMovimientos.Visible = o.Movimientos.Count > 0;
            pnlSinMovimientos.Visible = o.Movimientos.Count == 0;

            pnlSaldoGestor.Visible = o.IdUsuarioGestor.HasValue;
            litSaldoGestor.Text = o.SaldosGestor.Count == 0
                ? "sin movimientos en su cuenta de gestor."
                : string.Join(" · ", o.SaldosGestor.Select(s => BLL_CuentaCorriente.FormatearImporte(s.Saldo, s.Moneda) + " (" + s.Moneda + ")"));
        }

        private static string FormaPago(OperacionFinanciera o)
        {
            if (!o.IdPago.HasValue || o.TipoOperacion == "CargoCancelacion")
            {
                return o.TipoOperacion == "CargoCancelacion" ? "Descontado de la devolución al solicitante" : "No corresponde";
            }

            List<string> partes = new List<string>();
            if ((o.ImporteTarjeta ?? 0m) > 0m) partes.Add(Importe(o.ImporteTarjeta, o.Moneda) + " con tarjeta");
            if ((o.ImporteSaldo ?? 0m) > 0m) partes.Add(Importe(o.ImporteSaldo, o.Moneda) + " con saldo a favor");
            string texto = partes.Count == 0 ? "—" : string.Join(" + ", partes);
            return string.IsNullOrEmpty(o.CodigoAutorizacion) ? texto : texto + " · autorización " + o.CodigoAutorizacion;
        }

        private static string Persona(string nombre, string correo)
        {
            return string.IsNullOrWhiteSpace(nombre) ? "No corresponde" : nombre + (string.IsNullOrEmpty(correo) ? string.Empty : " (" + correo + ")");
        }

        private static string Porcentaje(decimal valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture) + " %";
        }

        private static string Hora(int minutos)
        {
            return minutos == 1440 ? "24:00"
                : (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
