using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno
{
    // Novedades con vigencia (Borrador, Programada, Vigente, Vencida, Dada de
    // baja) e imagen subida como archivo (Infraestructura/ImagenSubida).
    public partial class GestionNovedades : Page
    {
        private const string FormatoFechaHora = "yyyy-MM-ddTHH:mm";

        private readonly BLL_Novedad _bllNovedad = new BLL_Novedad();
        private readonly BLL_UsuarioInterno _bllUsuarioInterno = new BLL_UsuarioInterno();

        private int? IdNovedadEnEdicion
        {
            get { return ViewState["IdNovedadEnEdicion"] as int?; }
            set { ViewState["IdNovedadEnEdicion"] = value; }
        }

        // Imagen que tiene la novedad en el formulario (la guardada o la que
        // se acaba de subir). Va en ViewState para que no se pueda cambiar
        // desde el navegador.
        private string ImagenActual
        {
            get { return ViewState["ImagenActual"] as string; }
            set { ViewState["ImagenActual"] = value; }
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

            string errorImagen;
            string imagen = ResolverImagenFormulario(out errorImagen);
            if (errorImagen != null)
            {
                MostrarMensaje(errorImagen, true);
                return;
            }

            ResultadoOperacion resultado = _bllNovedad.EnviarPrueba(
                usuarioInterno.CorreoElectronico,
                usuarioInterno.Nombre,
                txtTitulo.Text,
                txtResumen.Text,
                ConstruirUrlNovedadesPublicas(),
                imagen,
                ConstruirUrlBaseAplicacion(),
                Server.MapPath("~/"));

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }

        protected void lnkCancelarNovedad_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void ddlFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TieneAcceso())
            {
                CargarNovedades();
            }
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

                case "DarDeBaja":
                    resultado = _bllNovedad.DarDeBaja(idNovedad, idResponsable);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    if (resultado.Exitoso && IdNovedadEnEdicion == idNovedad)
                    {
                        LimpiarFormulario();
                    }
                    CargarNovedades();
                    return;

                case "Restaurar":
                    resultado = _bllNovedad.Restaurar(idNovedad, idResponsable);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    CargarNovedades();
                    return;

                case "EnviarNewsletter":
                    ResultadoOperacion<int> resultadoEnvio = _bllNovedad.EnviarNewsletter(
                        idNovedad, ddlDestinatarios.SelectedValue, ConstruirUrlNovedadesPublicas(), idResponsable,
                        ConstruirUrlBaseAplicacion(), Server.MapPath("~/"));
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

            DateTime? fechaPublicacion;
            DateTime? fechaVencimiento;
            if (!TryParsearFechaHora(txtFechaPublicacion.Text, out fechaPublicacion) ||
                !TryParsearFechaHora(txtFechaVencimiento.Text, out fechaVencimiento))
            {
                MostrarMensaje("Revisá las fechas: elegí día y hora con el selector.", true);
                return;
            }

            string errorImagen;
            string urlImagen = ResolverImagenFormulario(out errorImagen);
            if (errorImagen != null)
            {
                MostrarMensaje(errorImagen, true);
                return;
            }

            ResultadoOperacion resultado;
            int idNovedadGuardada;

            if (IdNovedadEnEdicion.HasValue)
            {
                idNovedadGuardada = IdNovedadEnEdicion.Value;
                resultado = _bllNovedad.Modificar(
                    idNovedadGuardada, txtTitulo.Text, txtResumen.Text, txtContenido.Text,
                    ddlCategoria.SelectedValue, urlImagen, fechaPublicacion, fechaVencimiento, publicado, idResponsable);
            }
            else
            {
                ResultadoOperacion<int> resultadoAlta = _bllNovedad.Registrar(
                    txtTitulo.Text, txtResumen.Text, txtContenido.Text,
                    ddlCategoria.SelectedValue, urlImagen, fechaPublicacion, fechaVencimiento, publicado, idResponsable);
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
                    idNovedadGuardada, ddlDestinatarios.SelectedValue, ConstruirUrlNovedadesPublicas(), idResponsable,
                    ConstruirUrlBaseAplicacion(), Server.MapPath("~/"));
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
            int cantidadPublicadas = todas.Count(n => n.Estado == Novedad.EstadoVigente);
            int cantidadProgramadas = todas.Count(n => n.Estado == Novedad.EstadoProgramada);
            int cantidadBorradores = todas.Count(n => n.Estado == Novedad.EstadoBorrador);
            List<DateTime> fechasDeEnvio = todas
                .Where(n => n.EnviadaPorCorreo && n.FechaEnvioNewsletter.HasValue)
                .Select(n => n.FechaEnvioNewsletter.Value)
                .ToList();
            DateTime? ultimoEnvio = fechasDeEnvio.Count == 0 ? (DateTime?)null : fechasDeEnvio.Max();

            litCantidadBorradores.Text = cantidadBorradores.ToString(CultureInfo.InvariantCulture);
            litCantidadPublicadas.Text = cantidadPublicadas.ToString(CultureInfo.InvariantCulture);
            litCantidadProgramadas.Text = cantidadProgramadas.ToString(CultureInfo.InvariantCulture);
            litUltimoEnvio.Text = ultimoEnvio.HasValue
                ? FormatearFecha(ultimoEnvio.Value)
                : "Sin envíos";

            string filtro = ddlFiltroEstado.SelectedValue;
            List<Novedad> listado = string.IsNullOrEmpty(filtro) ? todas : _bllNovedad.Listar(filtro);
            litSinNovedades.Text = string.IsNullOrEmpty(filtro)
                ? "Todavía no cargaste ninguna novedad. Creá la primera desde el formulario."
                : "No hay novedades en ese estado.";
            pnlSinNovedades.Visible = listado.Count == 0;
            rptNovedades.DataSource = listado;
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

            if (novedad.Estado == Novedad.EstadoDadaDeBaja)
            {
                MostrarMensaje("La novedad está dada de baja. Restaurala antes de editarla.", true);
                return;
            }

            IdNovedadEnEdicion = novedad.IdNovedad;
            txtTitulo.Text = novedad.Titulo;
            txtResumen.Text = novedad.Resumen;
            txtContenido.Text = novedad.Contenido;
            ddlCategoria.SelectedValue = novedad.Categoria;
            txtFechaPublicacion.Text = novedad.FechaPublicacion.HasValue
                ? novedad.FechaPublicacion.Value.ToString(FormatoFechaHora, CultureInfo.InvariantCulture)
                : string.Empty;
            txtFechaVencimiento.Text = novedad.FechaVencimiento.HasValue
                ? novedad.FechaVencimiento.Value.ToString(FormatoFechaHora, CultureInfo.InvariantCulture)
                : string.Empty;
            MostrarImagenActual(novedad.UrlImagen);
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
            txtFechaVencimiento.Text = string.Empty;
            MostrarImagenActual(null);
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

        // Imagen a guardar: la nueva si se eligió un archivo (se valida y se
        // guarda en Content/Uploads/Novedades), ninguna si se tildó "Quitar",
        // o la que ya tenía.
        private string ResolverImagenFormulario(out string error)
        {
            error = null;
            if (fuImagen.HasFile)
            {
                string ruta = ImagenSubida.GuardarComoJpeg(fuImagen.PostedFile, BLL_Novedad.CarpetaImagenes, Server, out error);
                if (ruta == null)
                {
                    return null;
                }

                MostrarImagenActual(ruta);
                return ruta;
            }

            return chkQuitarImagen.Checked ? null : ImagenActual;
        }

        private void MostrarImagenActual(string urlImagen)
        {
            ImagenActual = string.IsNullOrWhiteSpace(urlImagen) ? null : urlImagen;
            chkQuitarImagen.Checked = false;
            pnlImagenActual.Visible = ImagenActual != null;
            if (ImagenActual != null)
            {
                imgImagenActual.ImageUrl = ImagenActual.StartsWith("~/", StringComparison.Ordinal) ? ResolveUrl(ImagenActual) : ImagenActual;
            }
        }

        private string ConstruirUrlNovedadesPublicas()
        {
            string urlRelativa = ResolveUrl("~/Novedades.aspx");
            return new Uri(Request.Url, urlRelativa).ToString();
        }

        private string ConstruirUrlBaseAplicacion()
        {
            return new Uri(Request.Url, ResolveUrl("~/")).ToString();
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

        protected static bool EsEstado(object dato, string estado)
        {
            Novedad novedad = dato as Novedad;
            return novedad != null && novedad.Estado == estado;
        }

        protected static bool PuedeEditar(object dato)
        {
            Novedad novedad = dato as Novedad;
            return novedad != null && novedad.Estado != Novedad.EstadoDadaDeBaja;
        }

        protected static string ObtenerClaseEstadoNovedad(Novedad novedad)
        {
            switch (novedad == null ? null : novedad.Estado)
            {
                case Novedad.EstadoVigente:
                    return "admin-status-badge admin-status-badge-active";
                case Novedad.EstadoProgramada:
                    return "admin-status-badge admin-status-badge-programada";
                case Novedad.EstadoVencida:
                    return "admin-status-badge admin-status-badge-vencida";
                default:
                    return "admin-status-badge admin-status-badge-inactive";
            }
        }

        protected static string ObtenerTextoEstadoNovedad(Novedad novedad)
        {
            return novedad == null ? string.Empty : BLL_Novedad.ObtenerEtiquetaEstado(novedad.Estado);
        }

        protected static string ObtenerTextoFechaNovedad(Novedad novedad)
        {
            if (novedad == null)
            {
                return string.Empty;
            }

            string vencimiento = novedad.FechaVencimiento.HasValue
                ? " · Vence el " + FormatearFechaHora(novedad.FechaVencimiento.Value)
                : " · Sin vencimiento";
            string correo = novedad.EnviadaPorCorreo ? " · Enviada por correo" : string.Empty;

            switch (novedad.Estado)
            {
                case Novedad.EstadoProgramada:
                    return "Se publica el " + FormatearFechaHora(novedad.FechaPublicacion.Value) + vencimiento;
                case Novedad.EstadoVigente:
                    return "Publicada el " + (novedad.FechaPublicacion.HasValue ? FormatearFechaHora(novedad.FechaPublicacion.Value) : "-") + vencimiento + correo;
                case Novedad.EstadoVencida:
                    return "Venció el " + FormatearFechaHora(novedad.FechaVencimiento.Value) + correo;
                case Novedad.EstadoDadaDeBaja:
                    return "Dada de baja" + (novedad.FechaBaja.HasValue ? " el " + FormatearFechaHora(novedad.FechaBaja.Value) : string.Empty);
                default:
                    return "Borrador · Sin publicar";
            }
        }

        // Valor de un campo datetime-local ("2026-10-20T18:30"). Vacío = null.
        private static bool TryParsearFechaHora(string texto, out DateTime? valor)
        {
            valor = null;
            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            DateTime fecha;
            if (DateTime.TryParseExact(texto.Trim(), new[] { FormatoFechaHora, "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            {
                valor = fecha;
                return true;
            }

            return false;
        }

        private static string FormatearFechaHora(DateTime fecha)
        {
            return fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
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
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
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
