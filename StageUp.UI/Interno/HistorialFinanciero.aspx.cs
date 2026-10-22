using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{




    public partial class HistorialFinanciero : Page
    {
        public const string PermisoRequerido = "CONSULTAR_HISTORIAL_FINANCIERO";

        private readonly BLL_HistorialFinanciero _bll = new BLL_HistorialFinanciero();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }


            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            if (!IsPostBack)
            {
                CargarOpciones();
                LeerFiltrosDeLaDireccion();


                if (Request.QueryString["detalle"] == "no_disponible")
                {
                    MostrarMensaje(BLL_HistorialFinanciero.MensajeDetalleNoDisponible, true);
                }

                CargarOperaciones();
            }
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarOperaciones();
        }

        protected void lnkLimpiar_Click(object sender, EventArgs e)
        {
            txtDesde.Text = string.Empty;
            txtHasta.Text = string.Empty;
            ddlEstadoPago.ClearSelection();
            ddlTipo.ClearSelection();
            ddlGestor.ClearSelection();
            txtTexto.Text = string.Empty;
            pnlMensaje.Visible = false;
            CargarOperaciones();
        }

        protected static string Importe(object importe, object moneda)
        {
            return importe == null ? "—" : BLL_CuentaCorriente.FormatearImporte(Convert.ToDecimal(importe), Convert.ToString(moneda));
        }

        protected static string Texto(object valor)
        {
            string texto = Convert.ToString(valor);
            return string.IsNullOrWhiteSpace(texto) ? "—" : texto;
        }

        protected static string Factura(object dato)
        {
            OperacionFinanciera o = (OperacionFinanciera)dato;
            if (!string.IsNullOrEmpty(o.NumeroComprobante))
            {
                return o.NumeroComprobante;
            }

            bool correspondeFactura = o.TipoOperacion == "ReservaAbonada" || o.TipoOperacion == "PagoNoConfirmado" || o.TipoOperacion == "ReservaSinPago";
            return correspondeFactura && o.EstadoPago != "Fallido" && o.EstadoPago != "Vencido" && o.EstadoPago != "Anulado" ? BLL_HistorialFinanciero.TextoFacturaPendiente() : "—";
        }

        protected string UrlDetalle(object codigo)
        {
            string consulta = ArmarConsulta();
            return ResolveUrl("~/Interno/DetalleOperacionFinanciera.aspx") + "?op=" + HttpUtility.UrlEncode(Convert.ToString(codigo)) +
                   (consulta.Length == 0 ? string.Empty : "&volver=" + HttpUtility.UrlEncode(consulta));
        }

        private void CargarOpciones()
        {
            ddlEstadoPago.Items.Clear();
            ddlEstadoPago.Items.Add(new ListItem("Todos", string.Empty));
            foreach (KeyValuePair<string, string> estado in BLL_HistorialFinanciero.ObtenerEstados())
            {
                ddlEstadoPago.Items.Add(new ListItem(estado.Value, estado.Key));
            }

            ddlTipo.Items.Clear();
            ddlTipo.Items.Add(new ListItem("Todas", string.Empty));
            foreach (KeyValuePair<string, string> tipo in BLL_HistorialFinanciero.ObtenerTipos())
            {
                ddlTipo.Items.Add(new ListItem(tipo.Value, tipo.Key));
            }

            ddlGestor.Items.Clear();
            ddlGestor.Items.Add(new ListItem("Todos", string.Empty));
            foreach (UsuarioExterno gestor in _bll.ListarGestores())
            {
                string nombre = string.IsNullOrWhiteSpace(gestor.Nombre) ? gestor.CorreoElectronico : gestor.Nombre;
                ddlGestor.Items.Add(new ListItem(nombre, gestor.IdUsuarioExterno.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void LeerFiltrosDeLaDireccion()
        {
            NameValueCollection qs = Request.QueryString;
            DateTime fecha;
            if (DateTime.TryParseExact(qs["desde"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            {
                txtDesde.Text = qs["desde"];
            }

            if (DateTime.TryParseExact(qs["hasta"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            {
                txtHasta.Text = qs["hasta"];
            }

            Seleccionar(ddlEstadoPago, qs["estado"]);
            Seleccionar(ddlTipo, qs["tipo"]);
            Seleccionar(ddlGestor, qs["gestor"]);
            string texto = qs["q"];
            txtTexto.Text = string.IsNullOrWhiteSpace(texto) ? string.Empty : (texto.Length > 100 ? texto.Substring(0, 100) : texto);
        }

        private static void Seleccionar(DropDownList lista, string valor)
        {
            if (!string.IsNullOrEmpty(valor) && lista.Items.FindByValue(valor) != null)
            {
                lista.ClearSelection();
                lista.Items.FindByValue(valor).Selected = true;
            }
        }


        private string ArmarConsulta()
        {
            List<string> partes = new List<string>();
            Agregar(partes, "desde", txtDesde.Text);
            Agregar(partes, "hasta", txtHasta.Text);
            Agregar(partes, "estado", ddlEstadoPago.SelectedValue);
            Agregar(partes, "tipo", ddlTipo.SelectedValue);
            Agregar(partes, "gestor", ddlGestor.SelectedValue);
            Agregar(partes, "q", txtTexto.Text);
            return string.Join("&", partes);
        }

        private static void Agregar(List<string> partes, string clave, string valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                partes.Add(clave + "=" + HttpUtility.UrlEncode(valor.Trim()));
            }
        }

        private void CargarOperaciones()
        {
            DateTime desde;
            DateTime hasta;
            int idGestor;
            FiltroHistorialFinanciero filtro = new FiltroHistorialFinanciero
            {
                Desde = DateTime.TryParse(txtDesde.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out desde) ? desde : (DateTime?)null,
                Hasta = DateTime.TryParse(txtHasta.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta) ? hasta : (DateTime?)null,
                EstadoPago = ddlEstadoPago.SelectedValue,
                TipoOperacion = ddlTipo.SelectedValue,
                IdUsuarioGestor = int.TryParse(ddlGestor.SelectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out idGestor) ? idGestor : (int?)null,
                Texto = txtTexto.Text
            };

            bool hayFiltros = filtro.Desde.HasValue || filtro.Hasta.HasValue || filtro.EstadoPago.Length > 0 ||
                              filtro.TipoOperacion.Length > 0 || filtro.IdUsuarioGestor.HasValue || !string.IsNullOrWhiteSpace(filtro.Texto);

            ResultadoOperacion<List<OperacionFinanciera>> resultado = _bll.Listar(filtro);
            List<OperacionFinanciera> lista = resultado.Exitoso ? resultado.Valor : new List<OperacionFinanciera>();
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
            }


            bool noHayNinguna = lista.Count == 0 && resultado.Exitoso && (!hayFiltros || _bll.NoHayOperaciones());
            pnlSinOperaciones.Visible = noHayNinguna;
            pnlSinResultados.Visible = lista.Count == 0 && resultado.Exitoso && !noHayNinguna;
            litCantidad.Text = lista.Count == 0 ? string.Empty
                : (lista.Count == 1 ? "1 operación" : lista.Count.ToString(CultureInfo.InvariantCulture) + " operaciones") +
                  (lista.Count >= 500 ? " (se muestran las 500 más recientes; usá los filtros para acotar)" : string.Empty);

            List<ResumenHistorialFinanciero> resumen = BLL_HistorialFinanciero.Resumir(lista);
            rptResumen.DataSource = resumen;
            rptResumen.DataBind();
            CargarSaldoGestor(filtro.IdUsuarioGestor);
            pnlResumen.Visible = resumen.Count > 0 || pnlSaldoGestor.Visible;

            rptOperaciones.DataSource = lista;
            rptOperaciones.DataBind();
            rptOperaciones.Visible = lista.Count > 0;
        }

        private void CargarSaldoGestor(int? idGestor)
        {
            pnlSaldoGestor.Visible = idGestor.HasValue;
            if (!idGestor.HasValue)
            {
                return;
            }

            litNombreGestor.Text = ddlGestor.SelectedItem == null ? "el gestor" : ddlGestor.SelectedItem.Text;
            List<SaldoCuentaCorriente> saldos = _bll.ObtenerSaldosGestor(idGestor.Value);
            rptSaldoGestor.DataSource = saldos;
            rptSaldoGestor.DataBind();
            litSinSaldoGestor.Visible = !saldos.Any();
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
