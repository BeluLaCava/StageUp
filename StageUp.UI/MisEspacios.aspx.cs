using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class MisEspacios : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_UsuarioExterno _bllUsuario = new BLL_UsuarioExterno();
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();

        private int? IdEspacioEnEdicion
        {
            get { return ViewState["IdEspacioEnEdicion"] as int?; }
            set { ViewState["IdEspacioEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            string perfil = GestorDeSesion.ObtenerPerfilActual();

            pnlPendienteGestor.Visible = perfil == PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString();
            pnlNoGestor.Visible = perfil == PerfilUsuarioExterno.ExternoSolicitante.ToString();
            pnlPanelGestor.Visible = perfil == PerfilUsuarioExterno.GestorEspacios.ToString();

            if (!IsPostBack && pnlPanelGestor.Visible)
            {
                CargarMisEspacios();
                CargarSolicitudesRecibidas();
            }
        }

        protected void btnSolicitarGestor_Click(object sender, EventArgs e)
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<int> resultado = _bllUsuario.SolicitarHabilitacionComoGestor(idUsuarioExterno);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                return;
            }

            // La solicitud quedó registrada: refrescamos la sesión para que el perfil
            // actualizado (PendienteHabilitacionGestor) se refleje sin pedir un nuevo login.
            GestorDeSesion.ActualizarPerfilEnSesion(PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString());

            pnlNoGestor.Visible = false;
            pnlPendienteGestor.Visible = true;
            MostrarMensaje(resultado.Mensaje, esError: false);
        }

        protected void btnGuardarEspacio_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            ResultadoOperacion resultado;
            if (IdEspacioEnEdicion == null)
            {
                ResultadoOperacion<int> resultadoAlta = _bllEspacio.Registrar(
                    idUsuarioGestor, txtNombreEspacio.Text, txtDescripcion.Text, txtTipoEspacio.Text);
                resultado = resultadoAlta;
            }
            else
            {
                resultado = _bllEspacio.Modificar(
                    IdEspacioEnEdicion.Value, idUsuarioGestor, txtNombreEspacio.Text, txtDescripcion.Text, txtTipoEspacio.Text);
            }

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                return;
            }

            LimpiarFormulario();
            MostrarMensaje(resultado.Mensaje, esError: false);
            CargarMisEspacios();
        }

        protected void lnkCancelarEdicion_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void rptMisEspacios_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idEspacioArtistico = Convert.ToInt32(e.CommandArgument);
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado;

            switch (e.CommandName)
            {
                case "Editar":
                    CargarEspacioEnFormulario(idEspacioArtistico);
                    return;

                case "Publicar":
                    resultado = _bllEspacio.Publicar(idEspacioArtistico, idUsuarioGestor);
                    break;

                case "Pausar":
                    resultado = _bllEspacio.Pausar(idEspacioArtistico, idUsuarioGestor);
                    break;

                case "BajaLogica":
                    resultado = _bllEspacio.DarDeBaja(idEspacioArtistico, idUsuarioGestor);
                    if (resultado.Exitoso && IdEspacioEnEdicion == idEspacioArtistico)
                    {
                        LimpiarFormulario();
                    }
                    break;

                default:
                    return;
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarMisEspacios();
        }

        protected void rptSolicitudes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idReserva = Convert.ToInt32(e.CommandArgument);
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado;

            switch (e.CommandName)
            {
                case "Aceptar":
                    resultado = _bllReserva.Aceptar(idReserva, idUsuarioGestor, null);
                    break;

                case "Rechazar":
                    resultado = _bllReserva.Rechazar(idReserva, idUsuarioGestor, null);
                    break;

                default:
                    return;
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarSolicitudesRecibidas();
        }

        protected void rptSolicitudes_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var reserva = (Reserva)e.Item.DataItem;
            var lnkAceptar = (LinkButton)e.Item.FindControl("lnkAceptar");
            var lnkRechazar = (LinkButton)e.Item.FindControl("lnkRechazar");

            bool esPendiente = reserva.EstadoReserva == "Pendiente";
            lnkAceptar.Visible = esPendiente;
            lnkRechazar.Visible = esPendiente;
        }

        private void CargarSolicitudesRecibidas()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<Reserva> solicitudes = _bllReserva.ListarSolicitudesRecibidas(idUsuarioGestor);

            litSinSolicitudes.Visible = solicitudes.Count == 0;
            rptSolicitudes.DataSource = solicitudes;
            rptSolicitudes.DataBind();
        }

        protected void rptMisEspacios_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var espacio = (EspacioArtistico)e.Item.DataItem;
            var lnkPublicar = (LinkButton)e.Item.FindControl("lnkPublicar");
            var lnkPausar = (LinkButton)e.Item.FindControl("lnkPausar");

            lnkPublicar.Visible = !espacio.Publicado;
            lnkPausar.Visible = espacio.Publicado;
        }

        private void CargarMisEspacios()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(idUsuarioGestor);

            litSinEspacios.Visible = espacios.Count == 0;
            rptMisEspacios.DataSource = espacios;
            rptMisEspacios.DataBind();
        }

        private void CargarEspacioEnFormulario(int idEspacioArtistico)
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            EspacioArtistico espacio = _bllEspacio.ListarMisEspacios(idUsuarioGestor)
                .Find(e => e.IdEspacioArtistico == idEspacioArtistico);

            if (espacio == null)
            {
                MostrarMensaje("No se encontró el espacio seleccionado.", esError: true);
                return;
            }

            IdEspacioEnEdicion = espacio.IdEspacioArtistico;
            txtNombreEspacio.Text = espacio.NombreEspacio;
            txtTipoEspacio.Text = espacio.TipoEspacio;
            txtDescripcion.Text = espacio.Descripcion;
            litTituloFormulario.Text = "Editar espacio";
            lnkCancelarEdicion.Visible = true;
            btnGuardarEspacio.Text = "Guardar cambios";
        }

        private void LimpiarFormulario()
        {
            IdEspacioEnEdicion = null;
            txtNombreEspacio.Text = string.Empty;
            txtTipoEspacio.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            litTituloFormulario.Text = "Nuevo espacio";
            lnkCancelarEdicion.Visible = false;
            btnGuardarEspacio.Text = "Guardar espacio";
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
