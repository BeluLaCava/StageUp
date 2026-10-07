using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{
    // CU-001-011 Consultar notificaciones y acceder a elementos relacionados:
    // centro de notificaciones ("Ver todas" del menú). Lista con título,
    // descripción, fecha, tipo y estado de lectura (A2/A3), filtro, marcar
    // como leídas solo las del usuario y aviso cuando el elemento ya no está
    // disponible (A9).
    public partial class Notificaciones : Page
    {
        private readonly BLL_Notificacion _bll = new BLL_Notificacion();

        private int IdUsuario
        {
            get { return GestorDeSesion.ObtenerIdUsuarioActual().Value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!IsPostBack)
            {
                // A9: se llegó desde el menú porque el elemento no estaba disponible.
                int idNotificacion;
                if (int.TryParse(Request.QueryString["no_disponible"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idNotificacion))
                {
                    string error;
                    AperturaNotificacion.ObtenerDestino(idNotificacion, IdUsuario, out error);
                    MostrarMensaje(error ?? "No es posible acceder al detalle relacionado.", true);
                }

                Cargar();
            }
        }

        protected void ddlEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            Cargar();
        }

        protected void btnMarcarTodas_Click(object sender, EventArgs e)
        {
            _bll.MarcarTodasLeidas(IdUsuario);
            MostrarMensaje("Todas tus notificaciones quedaron marcadas como leídas.", false);
            Cargar();
            RecargarMenu();
        }

        protected void rptNotificaciones_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idNotificacion;
            if (!int.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out idNotificacion))
            {
                return;
            }

            if (e.CommandName == "MarcarLeida")
            {
                _bll.MarcarLeida(idNotificacion, IdUsuario);
                pnlMensaje.Visible = false;
                Cargar();
                RecargarMenu();
                return;
            }

            if (e.CommandName == "Abrir")
            {
                string error;
                string destino = AperturaNotificacion.ObtenerDestino(idNotificacion, IdUsuario, out error);
                if (destino != null)
                {
                    Response.Redirect(ResolveUrl(destino), false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // A9: se informa y se mantiene el centro de notificaciones.
                MostrarMensaje(error, true);
                Cargar();
                RecargarMenu();
            }
        }

        private void Cargar()
        {
            List<Notificacion> notificaciones = _bll.Listar(IdUsuario, ddlEstado.SelectedValue, BLL_Notificacion.CantidadCentro);
            int noLeidas = _bll.ContarNoLeidas(IdUsuario);

            rptNotificaciones.DataSource = notificaciones;
            rptNotificaciones.DataBind();

            pnlVacio.Visible = notificaciones.Count == 0;
            litVacio.Text = ddlEstado.SelectedValue == BLL_Notificacion.FiltroNoLeidas
                ? "No tenés notificaciones sin leer."
                : ddlEstado.SelectedValue == BLL_Notificacion.FiltroLeidas
                    ? "Todavía no hay notificaciones leídas."
                    : "Todavía no tenés notificaciones. Acá vas a ver los avisos sobre tus reservas, pagos y solicitudes.";
            litResumen.Text = noLeidas == 0 ? "No tenés notificaciones sin leer" : noLeidas + " sin leer";
            btnMarcarTodas.Visible = noLeidas > 0;
            litNota.Text = notificaciones.Count >= BLL_Notificacion.CantidadCentro
                ? "Se muestran las últimas " + BLL_Notificacion.CantidadCentro + " notificaciones."
                : string.Empty;
        }

        // El contador y el menú del encabezado se cargan antes que la acción:
        // se vuelven a pedir para que muestren lo actualizado.
        private void RecargarMenu()
        {
            SiteMaster master = Master as SiteMaster;
            if (master != null)
            {
                master.ActualizarNotificaciones();
            }
        }

        protected string FormatearFecha(DateTime fecha)
        {
            CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
            if (fecha.Date == DateTime.Today)
            {
                return "Hoy a las " + fecha.ToString("HH:mm", cultura);
            }

            if (fecha.Date == DateTime.Today.AddDays(-1))
            {
                return "Ayer a las " + fecha.ToString("HH:mm", cultura);
            }

            return fecha.ToString("dd/MM/yyyy HH:mm", cultura);
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
