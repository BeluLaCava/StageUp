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
    public partial class SolicitudesRecibidas : Page
    {
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            // Esta pantalla es exclusiva del perfil GestorEspacios. Si el usuario
            // autenticado tiene otro perfil, lo mandamos a "Mis espacios", que ya
            // muestra el mensaje correspondiente (pendiente de aprobación o
            // todavía no es gestor).
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarSolicitudesRecibidas();
            }
        }

        protected void rptSolicitudes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

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

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        // Mismo chequeo explícito de perfil que en MisEspacios.aspx.cs: no alcanza
        // con ocultar el enlace en la sub-navegación, porque el perfil podría haber
        // cambiado en la base de datos sin que la sesión actual se actualice.
        private bool EsGestorEspacios()
        {
            return GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString();
        }
    }
}
