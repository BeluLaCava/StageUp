using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{







    public partial class OfrecerEspacio : Page
    {
        private readonly BLL_SolicitudHabilitacionGestor _bllSolicitud = new BLL_SolicitudHabilitacionGestor();
        private readonly BLL_UsuarioExterno _bllUsuario = new BLL_UsuarioExterno();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarCondiciones();
                Cargar();
            }
        }

        protected void btnNuevaSolicitud_Click(object sender, EventArgs e)
        {
            SolicitudHabilitacionGestor ultima = _bllSolicitud.ObtenerUltima(GestorDeSesion.ObtenerIdUsuarioActual().Value);
            if (ultima != null && ultima.Estado == SolicitudHabilitacionGestor.EstadoPendienteRevision)
            {
                Cargar();
                return;
            }



            MostrarFormulario(ultima);
            lnkCancelarFormulario.Visible = true;
        }

        protected void lnkCancelarFormulario_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            Cargar();
        }

        protected void btnEnviar_Click(object sender, EventArgs e)
        {
            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            SolicitudHabilitacionGestor datos = new SolicitudHabilitacionGestor
            {
                NombreResponsable = txtNombreResponsable.Text,
                DocumentoResponsable = txtDocumento.Text,
                TelefonoContacto = txtTelefono.Text,
                CorreoContacto = txtCorreoContacto.Text,
                CondicionFiscal = ddlCondicionFiscal.SelectedValue,
                Cuit = txtCuit.Text,
                RazonSocial = txtRazonSocial.Text,
                NombreEspacio = txtNombreEspacio.Text,
                TipoEspacio = txtTipoEspacio.Text,
                Provincia = txtProvincia.Text,
                Ciudad = txtCiudad.Text,
                DescripcionPropuesta = txtDescripcion.Text
            };

            ResultadoOperacion<int> resultado = _bllSolicitud.Registrar(idUsuario, datos);
            if (!resultado.Exitoso)
            {

                MostrarMensaje(resultado.Mensaje, true);
                pnlFormulario.Visible = true;
                return;
            }

            GestorDeSesion.ActualizarPerfilEnSesion(PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString());
            Cargar();
            MostrarMensaje(resultado.Mensaje, false);
        }

        private void Cargar()
        {
            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            UsuarioExterno usuario = _bllUsuario.ObtenerPerfilPorId(idUsuario);


            if (usuario != null && usuario.PerfilUsuario == PerfilUsuarioExterno.GestorEspacios.ToString())
            {
                GestorDeSesion.ActualizarPerfilEnSesion(usuario.PerfilUsuario);
                Response.Redirect("~/MisEspacios.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (usuario != null && usuario.PerfilUsuario != GestorDeSesion.ObtenerPerfilActual())
            {
                GestorDeSesion.ActualizarPerfilEnSesion(usuario.PerfilUsuario);
            }

            SolicitudHabilitacionGestor ultima = _bllSolicitud.ObtenerUltima(idUsuario);
            if (ultima == null)
            {

                pnlEstado.Visible = false;
                MostrarFormulario(null);
                lnkCancelarFormulario.Visible = false;
                return;
            }

            MostrarEstado(ultima);
            pnlFormulario.Visible = false;
        }

        private void MostrarEstado(SolicitudHabilitacionGestor solicitud)
        {
            pnlEstado.Visible = true;
            lblEstado.Text = Server.HtmlEncode(BLL_SolicitudHabilitacionGestor.NombreEstado(solicitud.Estado));
            lblEstado.CssClass = "oe-estado oe-estado-" + (solicitud.Estado ?? string.Empty).ToLowerInvariant();
            litFechaSolicitud.Text = solicitud.FechaSolicitud.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            litFechaRevision.Text = solicitud.FechaRevision.HasValue
                ? " · revisada el " + solicitud.FechaRevision.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                : string.Empty;

            litResumenEspacio.Text = solicitud.NombreEspacio + " (" + solicitud.TipoEspacio + ")";
            litResumenUbicacion.Text = solicitud.Ciudad + ", " + solicitud.Provincia;
            litResumenResponsable.Text = solicitud.NombreResponsable;
            litResumenContacto.Text = solicitud.CorreoContacto + " · " + solicitud.TelefonoContacto;

            bool pendiente = solicitud.Estado == SolicitudHabilitacionGestor.EstadoPendienteRevision;
            bool rechazada = solicitud.Estado == SolicitudHabilitacionGestor.EstadoRechazada;
            bool aprobada = solicitud.Estado == SolicitudHabilitacionGestor.EstadoAprobada;

            if (pendiente)
            {

                litExplicacionEstado.Text = "Todavía no fue revisada. No podés enviar otra solicitud mientras esta esté pendiente; " +
                    "hasta que se apruebe, seguís usando StageUp como solicitante (buscar y reservar espacios).";
            }
            else if (rechazada)
            {

                litExplicacionEstado.Text = "Tu solicitud fue rechazada. Podés revisar el motivo y enviar una nueva solicitud con los datos corregidos.";
            }
            else if (aprobada)
            {

                litExplicacionEstado.Text = "Tu solicitud fue aprobada: ya podés publicar y administrar tus espacios.";
            }

            pnlMotivoRechazo.Visible = rechazada && !string.IsNullOrWhiteSpace(solicitud.MotivoRechazo);
            litMotivoRechazo.Text = solicitud.MotivoRechazo;
            btnNuevaSolicitud.Visible = rechazada;
            lnkMisEspacios.Visible = aprobada;
        }

        private void MostrarFormulario(SolicitudHabilitacionGestor anterior)
        {
            pnlFormulario.Visible = true;

            if (anterior != null)
            {
                txtNombreResponsable.Text = anterior.NombreResponsable;
                txtDocumento.Text = anterior.DocumentoResponsable == "No informado" ? string.Empty : anterior.DocumentoResponsable;
                txtTelefono.Text = anterior.TelefonoContacto == "No informado" ? string.Empty : anterior.TelefonoContacto;
                txtCorreoContacto.Text = anterior.CorreoContacto;
                SeleccionarCondicion(anterior.CondicionFiscal);
                txtCuit.Text = anterior.Cuit;
                txtRazonSocial.Text = anterior.RazonSocial;
                txtNombreEspacio.Text = anterior.NombreEspacio == "No informado" ? string.Empty : anterior.NombreEspacio;
                txtTipoEspacio.Text = anterior.TipoEspacio == "No informado" ? string.Empty : anterior.TipoEspacio;
                txtProvincia.Text = anterior.Provincia == "No informado" ? string.Empty : anterior.Provincia;
                txtCiudad.Text = anterior.Ciudad == "No informado" ? string.Empty : anterior.Ciudad;
                txtDescripcion.Text = anterior.DescripcionPropuesta;
                return;
            }

            UsuarioExterno usuario = _bllUsuario.ObtenerPerfilPorId(GestorDeSesion.ObtenerIdUsuarioActual().Value);
            if (usuario != null)
            {
                txtNombreResponsable.Text = (usuario.Nombre + " " + usuario.Apellido).Trim();
                txtCorreoContacto.Text = usuario.CorreoElectronico;
                txtTelefono.Text = usuario.Telefono;
            }
        }

        private void CargarCondiciones()
        {
            ddlCondicionFiscal.Items.Clear();
            ddlCondicionFiscal.Items.Add(new ListItem("Elegí una opción", string.Empty));
            foreach (KeyValuePair<string, string> condicion in BLL_SolicitudHabilitacionGestor.CondicionesFiscales)
            {
                ddlCondicionFiscal.Items.Add(new ListItem(condicion.Value, condicion.Key));
            }
        }

        private void SeleccionarCondicion(string condicion)
        {
            ListItem item = ddlCondicionFiscal.Items.FindByValue(condicion ?? string.Empty);
            ddlCondicionFiscal.ClearSelection();
            if (item != null)
            {
                item.Selected = true;
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
