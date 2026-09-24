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
    public partial class GestionNovedades : Page
    {
        private readonly BLL_Novedad _bllNovedad = new BLL_Novedad();
        private readonly BLL_UsuarioInterno _bllUsuarioInterno = new BLL_UsuarioInterno();

        private int? IdNovedadEnEdicion
        {
            get { return ViewState["IdNovedadEnEdicion"] as int?; }
            set { ViewState["IdNovedadEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarNovedades();
            }
        }

        protected void btnGuardarBorrador_Click(object sender, EventArgs e)
        {
            Guardar(publicado: false);
        }

        protected void btnPublicar_Click(object sender, EventArgs e)
        {
            Guardar(publicado: true);
        }

        protected void btnEnviarPrueba_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || !TieneAcceso())
            {
                return;
            }

            int? idUsuarioInterno = GestorDeSesion.ObtenerIdUsuarioInternoActual();
            UsuarioInterno usuarioInterno = idUsuarioInterno.HasValue
                ? _bllUsuarioInterno.ObtenerPorId(idUsuarioInterno.Value)
                : null;

            if (usuarioInterno == null)
            {
                MostrarMensaje("No se pudo determinar tu usuario interno para enviar la prueba.", true);
                return;
            }

            ResultadoOperacion resultado = _bllNovedad.EnviarPrueba(
                usuarioInterno.CorreoElectronico,
                usuarioInterno.Nombre,
                txtTitulo.Text,
                txtResumen.Text,
                ConstruirUrlNovedadesPublicas());

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }

        protected void lnkCancelarNovedad_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void rptNovedades_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idNovedad = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;

            switch (e.CommandName)
            {
                case "Editar":
                    CargarNovedadEnFormulario(idNovedad);
                    return;

                case "Publicar":
                    resultado = _bllNovedad.Publicar(idNovedad, idResponsable);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    CargarNovedades();
                    return;

                case "VolverABorrador":
                    resultado = _bllNovedad.VolverABorrador(idNovedad, idResponsable);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    CargarNovedades();
                    return;

                case "EnviarNewsletter":
                    ResultadoOperacion<int> resultadoEnvio = _bllNovedad.EnviarNewsletter(
                        idNovedad, ddlDestinatarios.SelectedValue, ConstruirUrlNovedadesPublicas(), idResponsable);
                    MostrarMensaje(resultadoEnvio.Mensaje, !resultadoEnvio.Exitoso);
                    CargarNovedades();
                    return;
            }
        }

        private void Guardar(bool publicado)
        {
            if (!Page.IsValid || !TieneAcceso())
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            DateTime? fechaPublicacion = ParsearFecha(txtFechaPublicacion.Text);
            string urlImagen = string.IsNullOrWhiteSpace(txtUrlImagen.Text) ? null : txtUrlImagen.Text.Trim();

            ResultadoOperacion resultado;
            int idNovedadGuardada;

            if (IdNovedadEnEdicion.HasValue)
            {
                idNovedadGuardada = IdNovedadEnEdicion.Value;
                resultado = _bllNovedad.Modificar(
                    idNovedadGuardada, txtTitulo.Text, txtResumen.Text, txtContenido.Text,
                    ddlCategoria.SelectedValue, urlImagen, fechaPublicacion, publicado, idResponsable);
            }
            else
            {
                ResultadoOperacion<int> resultadoAlta = _bllNovedad.Registrar(
                    txtTitulo.Text, txtResumen.Text, txtContenido.Text,
                    ddlCategoria.SelectedValue, urlImagen, fechaPublicacion, publicado, idResponsable);
                resultado = resultadoAlta;
                idNovedadGuardada = resultadoAlta.Exitoso ? resultadoAlta.Valor : 0;
            }

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            string mensajeFinal = resultado.Mensaje;
            bool huboErrorEnEnvio = false;

            if (publicado && chkEnviarMail.Checked && idNovedadGuardada > 0)
            {
                ResultadoOperacion<int> resultadoEnvio = _bllNovedad.EnviarNewsletter(
                    idNovedadGuardada, ddlDestinatarios.SelectedValue, ConstruirUrlNovedadesPublicas(), idResponsable);
                mensajeFinal += " " + resultadoEnvio.Mensaje;
                huboErrorEnEnvio = !resultadoEnvio.Exitoso;
            }

            MostrarMensaje(mensajeFinal, huboErrorEnEnvio);
            LimpiarFormulario();
            CargarNovedades();
        }

        private void CargarNovedades()
        {
            List<Novedad> todas = _bllNovedad.Listar();
            int cantidadPublicadas = todas.Count(n => n.Publicado);
            int cantidadBorradores = todas.Count - cantidadPublicadas;
            List<DateTime> fechasDeEnvio = todas
                .Where(n => n.EnviadaPorCorreo && n.FechaEnvioNewsletter.HasValue)
                .Select(n => n.FechaEnvioNewsletter.Value)
                .ToList();
            DateTime? ultimoEnvio = fechasDeEnvio.Count == 0 ? (DateTime?)null : fechasDeEnvio.Max();

            litCantidadBorradores.Text = cantidadBorradores.ToString(CultureInfo.InvariantCulture);
            litCantidadPublicadas.Text = cantidadPublicadas.ToString(CultureInfo.InvariantCulture);
            litUltimoEnvio.Text = ultimoEnvio.HasValue
                ? FormatearFecha(ultimoEnvio.Value)
                : "Sin envíos";

            pnlSinNovedades.Visible = todas.Count == 0;
            rptNovedades.DataSource = todas;
            rptNovedades.DataBind();
        }

        private void CargarNovedadEnFormulario(int idNovedad)
        {
            Novedad novedad = _bllNovedad.ObtenerPorId(idNovedad);
            if (novedad == null)
            {
                MostrarMensaje("No se encontró la novedad seleccionada.", true);
                return;
            }

            IdNovedadEnEdicion = novedad.IdNovedad;
            txtTitulo.Text = novedad.Titulo;
            txtResumen.Text = novedad.Resumen;
            txtContenido.Text = novedad.Contenido;
            ddlCategoria.SelectedValue = novedad.Categoria;
            txtFechaPublicacion.Text = novedad.FechaPublicacion.HasValue
                ? novedad.FechaPublicacion.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;
            txtUrlImagen.Text = novedad.UrlImagen;
            chkEnviarMail.Checked = false;

            litTituloFormulario.Text = "Editar novedad";
            btnPublicar.Text = novedad.Publicado ? "Guardar cambios" : "Guardar y publicar";
            lnkCancelarNovedad.Visible = true;

            ActualizarVistaPrevia(novedad.Titulo, novedad.Resumen, novedad.Categoria);
        }

        private void LimpiarFormulario()
        {
            IdNovedadEnEdicion = null;
            txtTitulo.Text = string.Empty;
            txtResumen.Text = string.Empty;
            txtContenido.Text = string.Empty;
            ddlCategoria.SelectedIndex = 0;
            txtFechaPublicacion.Text = string.Empty;
            txtUrlImagen.Text = string.Empty;
            chkEnviarMail.Checked = false;

            litTituloFormulario.Text = "Nueva novedad";
            btnPublicar.Text = "Guardar y publicar";
            lnkCancelarNovedad.Visible = false;

            ActualizarVistaPrevia(null, null, null);
        }

        private void ActualizarVistaPrevia(string titulo, string resumen, string categoria)
        {
            litPreviewTitulo.Text = string.IsNullOrWhiteSpace(titulo)
                ? "Así se va a ver el correo"
                : Server.HtmlEncode(titulo);
            litPreviewResumen.Text = string.IsNullOrWhiteSpace(resumen)
                ? "Completá el formulario y guardá para actualizar esta vista previa."
                : Server.HtmlEncode(resumen);
            litPreviewCategoria.Text = string.IsNullOrWhiteSpace(categoria)
                ? "Novedades StageUp"
                : Server.HtmlEncode(ObtenerEtiquetaCategoria(categoria));
        }

        private string ConstruirUrlNovedadesPublicas()
        {
            string urlRelativa = ResolveUrl("~/Novedades.aspx");
            return new Uri(Request.Url, urlRelativa).ToString();
        }

        protected static string ObtenerEtiquetaCategoria(string categoria)
        {
            switch (categoria)
            {
                case "Institucional":
                    return "Novedad institucional";
                case "Catalogo":
                    return "Nuevos espacios";
                case "Consejos":
                    return "Consejos para usuarios";
                case "Comunidad":
                    return "Comunidad artística";
                default:
                    return "Novedades StageUp";
            }
        }

        protected static string ObtenerClaseFilaNovedad(Novedad novedad)
        {
            return "newsletter-campaign-row";
        }

        protected static string ObtenerClaseEstadoNovedad(Novedad novedad)
        {
            return novedad != null && novedad.Publicado
                ? "admin-status-badge admin-status-badge-active"
                : "admin-status-badge admin-status-badge-inactive";
        }

        protected static string ObtenerTextoEstadoNovedad(Novedad novedad)
        {
            return novedad != null && novedad.Publicado ? "Publicada" : "Borrador";
        }

        protected static string ObtenerTextoFechaNovedad(Novedad novedad)
        {
            if (novedad == null)
            {
                return string.Empty;
            }

            if (novedad.Publicado && novedad.FechaPublicacion.HasValue)
            {
                return "Publicada · " + FormatearFecha(novedad.FechaPublicacion.Value) +
                    (novedad.EnviadaPorCorreo ? " · Enviada por correo" : " · Sin enviar por correo");
            }

            return "Borrador · Sin publicar";
        }

        private static DateTime? ParsearFecha(string texto)
        {
            DateTime valor;
            return DateTime.TryParse(texto, out valor) ? valor : (DateTime?)null;
        }

        private static string FormatearFecha(DateTime fecha)
        {
            return fecha.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_NOVEDADES"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return false;
            }

            return true;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError
                ? "form-message form-message-error"
                : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
