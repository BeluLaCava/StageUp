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
    // Newsletter (script 85): administración de suscriptores, de las
    // categorías de novedades y consulta de los envíos con el estado de cada
    // destinatario. Mismo permiso que Novedades y newsletter.
    public partial class GestionNewsletter : Page
    {
        private const string PermisoRequerido = "GESTIONAR_NOVEDADES";

        private readonly BLL_Newsletter _bllNewsletter = new BLL_Newsletter();
        private readonly BLL_CategoriaNovedad _bllCategoria = new BLL_CategoriaNovedad();
        private readonly BLL_Novedad _bllNovedad = new BLL_Novedad();

        private Dictionary<string, string> _etiquetasCategorias;

        private string CodigoCategoriaEnEdicion
        {
            get { return ViewState["CodigoCategoriaEnEdicion"] as string; }
            set { ViewState["CodigoCategoriaEnEdicion"] = value; }
        }

        private int? IdEnvioSeleccionado
        {
            get { return ViewState["IdEnvioSeleccionado"] as int?; }
            set { ViewState["IdEnvioSeleccionado"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarTodo();
            }
        }

        // ------------------------------------------------------------------
        // Suscriptores
        // ------------------------------------------------------------------
        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            if (TieneAcceso())
            {
                CargarSuscriptores();
            }
        }

        protected void lnkLimpiar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            txtBuscar.Text = string.Empty;
            ddlEstado.SelectedIndex = 0;
            ddlCuenta.SelectedIndex = 0;
            CargarSuscriptores();
        }

        protected void rptSuscriptores_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idSuscriptor = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = e.CommandName == "Reenviar"
                ? _bllNewsletter.ReenviarConfirmacion(idSuscriptor, idResponsable, UrlBase())
                : e.CommandName == "Baja"
                    ? _bllNewsletter.DarDeBajaAdministrativa(idSuscriptor, idResponsable)
                    : null;

            if (resultado != null)
            {
                MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            }

            CargarResumen();
            CargarSuscriptores();
        }

        // ------------------------------------------------------------------
        // Categorías
        // ------------------------------------------------------------------
        protected void rptCategorias_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            string codigo = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture);
            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;

            switch (e.CommandName)
            {
                case "Editar":
                    CategoriaNovedad categoria = _bllCategoria.ListarParaAdministrar().FirstOrDefault(c => c.Codigo == codigo);
                    if (categoria == null)
                    {
                        MostrarMensaje("La categoría no existe.", true);
                        return;
                    }

                    CodigoCategoriaEnEdicion = categoria.Codigo;
                    txtCategoriaNombre.Text = categoria.Nombre;
                    txtCategoriaOrden.Text = categoria.Orden.ToString(CultureInfo.InvariantCulture);
                    litTituloCategoria.Text = "Editar categoría (código " + Server.HtmlEncode(categoria.Codigo) + ")";
                    btnGuardarCategoria.Text = "Guardar cambios";
                    lnkCancelarCategoria.Visible = true;
                    return;

                case "Desactivar":
                case "Activar":
                    ResultadoOperacion resultado = _bllCategoria.CambiarEstado(codigo, e.CommandName == "Activar", idResponsable);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    CargarCategorias();
                    return;
            }
        }

        protected void btnGuardarCategoria_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int orden;
            string textoOrden = (txtCategoriaOrden.Text ?? string.Empty).Trim();
            if (textoOrden.Length == 0)
            {
                orden = SiguienteOrden();
            }
            else if (!int.TryParse(textoOrden, NumberStyles.Integer, CultureInfo.InvariantCulture, out orden))
            {
                MostrarMensaje("El orden tiene que ser un número entre 0 y 9999.", true);
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = CodigoCategoriaEnEdicion == null
                ? _bllCategoria.Registrar(txtCategoriaNombre.Text, orden, idResponsable)
                : _bllCategoria.Modificar(CodigoCategoriaEnEdicion, txtCategoriaNombre.Text, orden, idResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                LimpiarFormularioCategoria();
                CargarCategorias();
            }
        }

        protected void lnkCancelarCategoria_Click(object sender, EventArgs e)
        {
            LimpiarFormularioCategoria();
        }

        // ------------------------------------------------------------------
        // Envíos
        // ------------------------------------------------------------------
        protected void rptEnvios_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || e.CommandName != "Ver")
            {
                return;
            }

            IdEnvioSeleccionado = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            CargarEnvios();
        }

        protected void btnReintentarEnvio_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdEnvioSeleccionado.HasValue)
            {
                return;
            }

            EnvioNewsletter envio = _bllNovedad.ListarEnvios(null, 500).FirstOrDefault(x => x.IdEnvioNewsletter == IdEnvioSeleccionado.Value);
            if (envio == null)
            {
                MostrarMensaje("No se encontró el envío.", true);
                CargarEnvios();
                return;
            }

            ResultadoOperacion<int> resultado = _bllNovedad.ReintentarNewsletter(
                envio.IdNovedad,
                new Uri(Request.Url, ResolveUrl("~/Novedades.aspx")).ToString(),
                GestorDeSesion.ObtenerIdUsuarioInternoActual().Value,
                UrlBase(),
                Server.MapPath("~/"));
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarResumen();
            CargarEnvios();
        }

        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            IdEnvioSeleccionado = null;
            CargarEnvios();
        }

        // ------------------------------------------------------------------
        // Carga
        // ------------------------------------------------------------------
        private void CargarTodo()
        {
            CargarResumen();
            CargarSuscriptores();
            CargarCategorias();
            LimpiarFormularioCategoria();
            CargarEnvios();
        }

        private void CargarResumen()
        {
            ResumenSuscriptoresNewsletter resumen = _bllNewsletter.ObtenerResumen();
            litActivos.Text = resumen.Activos.ToString(CultureInfo.InvariantCulture);
            litSinCuenta.Text = resumen.ActivosSinCuenta.ToString(CultureInfo.InvariantCulture);
            litPendientes.Text = resumen.Pendientes.ToString(CultureInfo.InvariantCulture);
            litBajas.Text = resumen.Bajas.ToString(CultureInfo.InvariantCulture);
        }

        private void CargarSuscriptores()
        {
            FiltroSuscriptoresNewsletter filtro = new FiltroSuscriptoresNewsletter
            {
                Texto = txtBuscar.Text,
                Estado = string.IsNullOrEmpty(ddlEstado.SelectedValue) ? null : ddlEstado.SelectedValue,
                ConCuenta = ddlCuenta.SelectedValue == "1" ? true : ddlCuenta.SelectedValue == "0" ? (bool?)false : null,
                Maximo = 200
            };

            int total;
            List<SuscriptorNewsletter> suscriptores = _bllNewsletter.Listar(filtro, out total);
            litTotalSuscriptores.Text = total == 0
                ? string.Empty
                : suscriptores.Count < total
                    ? "Se muestran los " + suscriptores.Count + " más recientes de " + total + ". Usá los filtros para encontrar uno en particular."
                    : total + (total == 1 ? " suscriptor." : " suscriptores.");
            pnlSinSuscriptores.Visible = suscriptores.Count == 0;
            rptSuscriptores.Visible = suscriptores.Count > 0;
            rptSuscriptores.DataSource = suscriptores;
            rptSuscriptores.DataBind();
        }

        private void CargarCategorias()
        {
            rptCategorias.DataSource = _bllCategoria.ListarParaAdministrar();
            rptCategorias.DataBind();
            _etiquetasCategorias = null;
        }

        private void CargarEnvios()
        {
            List<EnvioNewsletter> envios = _bllNovedad.ListarEnvios(null, 100);
            pnlSinEnvios.Visible = envios.Count == 0;
            rptEnvios.Visible = envios.Count > 0;
            rptEnvios.DataSource = envios;
            rptEnvios.DataBind();

            EnvioNewsletter seleccionado = IdEnvioSeleccionado.HasValue
                ? envios.FirstOrDefault(x => x.IdEnvioNewsletter == IdEnvioSeleccionado.Value)
                : null;
            pnlDetalleEnvio.Visible = seleccionado != null;
            if (seleccionado == null)
            {
                IdEnvioSeleccionado = null;
                return;
            }

            litDetalleEnvio.Text = "Destinatarios de \"" + seleccionado.TituloNovedad + "\" · " +
                BLL_Novedad.ObtenerEtiquetaEstadoEnvio(seleccionado.Estado) + ", " + seleccionado.CantidadEnviados + " de " +
                seleccionado.CantidadDestinatarios + " recibieron";
            btnReintentarEnvio.Visible = seleccionado.PuedeReintentar;
            rptDestinatarios.DataSource = _bllNovedad.ListarDestinatariosDeEnvio(seleccionado.IdEnvioNewsletter);
            rptDestinatarios.DataBind();
        }

        private void LimpiarFormularioCategoria()
        {
            CodigoCategoriaEnEdicion = null;
            txtCategoriaNombre.Text = string.Empty;
            txtCategoriaOrden.Text = string.Empty;
            litTituloCategoria.Text = "Nueva categoría";
            btnGuardarCategoria.Text = "Agregar categoría";
            lnkCancelarCategoria.Visible = false;
        }

        private int SiguienteOrden()
        {
            List<CategoriaNovedad> categorias = _bllCategoria.ListarParaAdministrar();
            return categorias.Count == 0 ? 10 : Math.Min(9999, categorias.Max(c => c.Orden) + 10);
        }

        // ------------------------------------------------------------------
        // Formato
        // ------------------------------------------------------------------
        protected static string EtiquetaEstadoSuscriptor(object estado)
        {
            switch (Convert.ToString(estado))
            {
                case SuscriptorNewsletter.EstadoActivo: return "Confirmado";
                case SuscriptorNewsletter.EstadoPendiente: return "Sin confirmar";
                case SuscriptorNewsletter.EstadoBaja: return "Dado de baja";
                default: return Convert.ToString(estado);
            }
        }

        protected static string ClaseEstadoSuscriptor(object estado)
        {
            switch (Convert.ToString(estado))
            {
                case SuscriptorNewsletter.EstadoActivo: return "nl-estado nl-estado-ok";
                case SuscriptorNewsletter.EstadoPendiente: return "nl-estado nl-estado-alerta";
                default: return "nl-estado";
            }
        }

        protected static string ClaseEstadoEnvio(object estado)
        {
            switch (Convert.ToString(estado))
            {
                case EnvioNewsletter.EstadoCompleto: return "nl-estado nl-estado-ok";
                case EnvioNewsletter.EstadoParcial: return "nl-estado nl-estado-alerta";
                case EnvioNewsletter.EstadoFallido: return "nl-estado nl-estado-error";
                default: return "nl-estado";
            }
        }

        protected static string ClaseEstadoDestinatario(object estado)
        {
            switch (Convert.ToString(estado))
            {
                case EnvioNewsletterDestinatario.EstadoEnviado: return "nl-estado nl-estado-ok";
                case EnvioNewsletterDestinatario.EstadoFallido: return "nl-estado nl-estado-error";
                case EnvioNewsletterDestinatario.EstadoPendiente: return "nl-estado nl-estado-alerta";
                default: return "nl-estado";
            }
        }

        protected static string TextoCuenta(object dato)
        {
            SuscriptorNewsletter suscriptor = dato as SuscriptorNewsletter;
            if (suscriptor == null || !suscriptor.TieneCuenta)
            {
                return "Sin cuenta";
            }

            switch (suscriptor.PerfilCuenta)
            {
                case "GestorEspacios": return "Gestor";
                case "ExternoSolicitante": return "Solicitante";
                default: return "Con cuenta";
            }
        }

        protected string TextoCategorias(object dato)
        {
            SuscriptorNewsletter suscriptor = dato as SuscriptorNewsletter;
            if (suscriptor == null || suscriptor.Categorias.Count == 0)
            {
                return "Todas";
            }

            if (_etiquetasCategorias == null)
            {
                _etiquetasCategorias = BLL_CategoriaNovedad.ObtenerEtiquetas();
            }

            return string.Join(", ", suscriptor.Categorias.Select(c => _etiquetasCategorias.ContainsKey(c) ? _etiquetasCategorias[c] : c));
        }

        protected static string TextoFechas(object dato)
        {
            SuscriptorNewsletter suscriptor = dato as SuscriptorNewsletter;
            if (suscriptor == null)
            {
                return string.Empty;
            }

            if (suscriptor.Estado == SuscriptorNewsletter.EstadoBaja)
            {
                return suscriptor.FechaBaja.HasValue ? "Baja el " + suscriptor.FechaBaja.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "Baja";
            }

            if (suscriptor.Estado == SuscriptorNewsletter.EstadoPendiente)
            {
                return suscriptor.FechaUltimoEnvioConfirmacion.HasValue
                    ? "Confirmación enviada el " + suscriptor.FechaUltimoEnvioConfirmacion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                    : "El correo de confirmación no salió";
            }

            return suscriptor.FechaConfirmacion.HasValue
                ? "Confirmó el " + suscriptor.FechaConfirmacion.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : "Confirmado";
        }

        private string UrlBase()
        {
            return new Uri(Request.Url, ResolveUrl("~/")).ToString();
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

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
