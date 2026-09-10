using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class MiPerfil : Page
    {
        private const int MaximoBytesFoto = 5 * 1024 * 1024;
        private readonly BLL_UsuarioExterno _bllUsuario = new BLL_UsuarioExterno();
        private readonly BLL_Calificacion _bllCalificacion = new BLL_Calificacion();

        private string FotoPerfilRutaActual
        {
            get { return ViewState["FotoPerfilRutaActual"] as string; }
            set { ViewState["FotoPerfilRutaActual"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Form.Enctype = "multipart/form-data";

            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!IsPostBack)
            {
                ConfigurarIntegracionPerfil();
                CargarPerfil();
            }
        }

        protected void lnkEditarDatos_Click(object sender, EventArgs e)
        {
            pnlVistaDatos.Visible = false;
            pnlEditarDatos.Visible = true;
            lnkEditarDatos.Visible = false;
            OcultarMensaje();
        }

        protected void lnkCancelarEdicion_Click(object sender, EventArgs e)
        {
            pnlVistaDatos.Visible = true;
            pnlEditarDatos.Visible = false;
            lnkEditarDatos.Visible = true;
            OcultarMensaje();
        }

        protected void btnGuardarDatos_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            string nuevaFotoRuta = FotoPerfilRutaActual;
            string archivoNuevo = null;

            try
            {
                if (fuFotoPerfil.HasFile)
                {
                    nuevaFotoRuta = GuardarFotoPerfil(out archivoNuevo);
                }

                ResultadoOperacion resultado = _bllUsuario.ActualizarDatosPersonales(
                    idUsuario, txtNombre.Text, txtApellido.Text, txtCorreo.Text,
                    nuevaFotoRuta, txtDescripcion.Text);

                if (!resultado.Exitoso)
                {
                    EliminarArchivoNuevo(archivoNuevo);
                    MostrarMensaje(resultado.Mensaje, true);
                    return;
                }

                FotoPerfilRutaActual = nuevaFotoRuta;
                GestorDeSesion.ActualizarNombreCompletoEnSesion(txtNombre.Text.Trim(), txtApellido.Text.Trim());
                pnlVistaDatos.Visible = true;
                pnlEditarDatos.Visible = false;
                lnkEditarDatos.Visible = true;
                CargarPerfil();
                MostrarMensaje(resultado.Mensaje, false);
            }
            catch (InvalidOperationException ex)
            {
                EliminarArchivoNuevo(archivoNuevo);
                MostrarMensaje(ex.Message, true);
            }
        }

        protected void lnkCambiarPassword_Click(object sender, EventArgs e)
        {
            pnlPasswordResumen.Visible = false;
            pnlCambiarPassword.Visible = true;
            lnkCambiarPassword.Visible = false;
            OcultarMensaje();
        }

        protected void lnkCancelarPassword_Click(object sender, EventArgs e)
        {
            LimpiarFormularioPassword();
            pnlPasswordResumen.Visible = true;
            pnlCambiarPassword.Visible = false;
            lnkCambiarPassword.Visible = true;
            OcultarMensaje();
        }

        protected void btnCambiarPassword_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado = _bllUsuario.CambiarPassword(
                idUsuario, txtPasswordActual.Text, txtNuevaPassword.Text, txtConfirmarPassword.Text);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            LimpiarFormularioPassword();
            pnlPasswordResumen.Visible = true;
            pnlCambiarPassword.Visible = false;
            lnkCambiarPassword.Visible = true;
            MostrarMensaje(resultado.Mensaje, false);
        }

        private void ConfigurarIntegracionPerfil()
        {
            bool habilitada = _bllUsuario.PerfilCompletoHabilitado;
            pnlIntegracionPerfilPendiente.Visible = !habilitada;
            btnGuardarDatos.Enabled = habilitada;
            btnGuardarDatos.Attributes["aria-disabled"] = habilitada ? "false" : "true";
            btnGuardarDatos.ToolTip = habilitada
                ? "Guardar los cambios del perfil"
                : "Falta integrar los procedimientos de perfil en la base de datos";
        }

        private void CargarPerfil()
        {
            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            UsuarioExterno usuario = _bllUsuario.ObtenerPerfilPorId(idUsuario);

            if (usuario == null)
            {
                lnkEditarDatos.Visible = false;
                btnGuardarDatos.Enabled = false;
                MostrarMensaje("No pudimos cargar los datos de tu perfil. Probá nuevamente en unos minutos.", true);
                return;
            }

            FotoPerfilRutaActual = usuario.FotoPerfilRuta;
            litNombreResumen.Text = (usuario.Nombre + " " + usuario.Apellido).Trim();
            litCorreoResumen.Text = usuario.CorreoElectronico;
            litPerfilUsuario.Text = ObtenerNombrePerfil(usuario.PerfilUsuario);
            litIniciales.Text = ObtenerIniciales(usuario.Nombre, usuario.Apellido);
            litNombre.Text = usuario.Nombre;
            litApellido.Text = usuario.Apellido;
            litCorreo.Text = usuario.CorreoElectronico;
            litFechaAlta.Text = usuario.FechaAlta == DateTime.MinValue
                ? "No disponible"
                : usuario.FechaAlta.ToString("Y", CultureInfo.CurrentCulture);
            litDescripcion.Text = string.IsNullOrWhiteSpace(usuario.DescripcionPerfil)
                ? "Todavía no agregaste una descripción a tu perfil."
                : usuario.DescripcionPerfil;

            txtNombre.Text = usuario.Nombre;
            txtApellido.Text = usuario.Apellido;
            txtCorreo.Text = usuario.CorreoElectronico;
            txtDescripcion.Text = usuario.DescripcionPerfil;

            bool tieneFoto = !string.IsNullOrWhiteSpace(usuario.FotoPerfilRuta);
            imgPerfil.Visible = tieneFoto;
            pnlIniciales.Visible = !tieneFoto;
            if (tieneFoto)
            {
                imgPerfil.ImageUrl = usuario.FotoPerfilRuta;
            }

            CargarCalificaciones(idUsuario);
        }

        private void CargarCalificaciones(int idUsuario)
        {
            ResumenReputacion resumen = _bllCalificacion.ObtenerResumenUsuario(idUsuario);
            List<Calificacion> recibidas = _bllCalificacion.ListarRecibidasPorUsuario(idUsuario);
            List<Calificacion> realizadas = _bllCalificacion.ListarRealizadasPorUsuario(idUsuario);

            litEstrellasPerfil.Text = ObtenerEstrellas((int)Math.Round(resumen.Promedio));
            litResumenReputacionPerfil.Text = resumen.CantidadCalificaciones == 0
                ? "Sin calificaciones todavía"
                : resumen.Promedio.ToString("0.0", CultureInfo.CurrentCulture) + " de 5 · " +
                  resumen.CantidadCalificaciones + (resumen.CantidadCalificaciones == 1 ? " calificación" : " calificaciones");
            litAyudaReputacionPerfil.Text = resumen.CantidadCalificaciones == 0
                ? "Cuando finalicen tus primeras reservas, vas a poder ver acá tu puntaje y los comentarios recibidos."
                : "Este puntaje reúne las experiencias que los gestores registraron después de reservas finalizadas.";

            pnlSinCalificacionesRecibidas.Visible = recibidas.Count == 0;
            rptCalificacionesRecibidas.DataSource = recibidas;
            rptCalificacionesRecibidas.DataBind();

            pnlSinCalificacionesRealizadas.Visible = realizadas.Count == 0;
            rptCalificacionesRealizadas.DataSource = realizadas;
            rptCalificacionesRealizadas.DataBind();
        }

        protected string ObtenerEstrellas(int puntaje)
        {
            int valor = Math.Max(0, Math.Min(5, puntaje));
            return new string('★', valor) + new string('☆', 5 - valor);
        }

        protected string ObtenerDestinoCalificacion(Calificacion calificacion)
        {
            if (calificacion == null)
            {
                return "Experiencia StageUp";
            }

            return calificacion.TipoCalificacion == "Espacio"
                ? "Espacio: " + calificacion.NombreEspacio
                : "Solicitante: " + calificacion.NombreEvaluado;
        }

        private string GuardarFotoPerfil(out string rutaFisica)
        {
            rutaFisica = null;
            string extension = Path.GetExtension(fuFotoPerfil.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                throw new InvalidOperationException("La foto debe estar en formato JPG o PNG.");
            }

            if (fuFotoPerfil.PostedFile.ContentLength <= 0 || fuFotoPerfil.PostedFile.ContentLength > MaximoBytesFoto)
            {
                throw new InvalidOperationException("La foto debe pesar menos de 5 MB.");
            }

            string carpeta = Server.MapPath("~/Content/Uploads/Perfiles");
            Directory.CreateDirectory(carpeta);
            string nombreArchivo = Guid.NewGuid().ToString("N") + extension;
            rutaFisica = Path.Combine(carpeta, nombreArchivo);
            fuFotoPerfil.SaveAs(rutaFisica);
            return "~/Content/Uploads/Perfiles/" + nombreArchivo;
        }

        private static void EliminarArchivoNuevo(string rutaFisica)
        {
            if (!string.IsNullOrEmpty(rutaFisica) && File.Exists(rutaFisica))
            {
                File.Delete(rutaFisica);
            }
        }

        private static string ObtenerIniciales(string nombre, string apellido)
        {
            string inicialNombre = string.IsNullOrWhiteSpace(nombre) ? string.Empty : nombre.Trim().Substring(0, 1);
            string inicialApellido = string.IsNullOrWhiteSpace(apellido) ? string.Empty : apellido.Trim().Substring(0, 1);
            return (inicialNombre + inicialApellido).ToUpperInvariant();
        }

        private static string ObtenerNombrePerfil(string perfil)
        {
            if (string.Equals(perfil, "GestorEspacios", StringComparison.OrdinalIgnoreCase))
            {
                return "Gestor de espacios";
            }

            if (string.Equals(perfil, "PendienteHabilitacionGestor", StringComparison.OrdinalIgnoreCase))
            {
                return "Solicitud de gestor pendiente";
            }

            return "Usuario de StageUp";
        }

        private void LimpiarFormularioPassword()
        {
            txtPasswordActual.Text = string.Empty;
            txtNuevaPassword.Text = string.Empty;
            txtConfirmarPassword.Text = string.Empty;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlMensaje.Visible = true;
        }

        private void OcultarMensaje()
        {
            pnlMensaje.Visible = false;
            litMensaje.Text = string.Empty;
        }
    }
}
