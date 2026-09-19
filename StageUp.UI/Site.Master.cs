using System;
using System.Web.UI.WebControls;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{
    public partial class SiteMaster : MasterPageMultidioma
    {
        private readonly BLL_Notificacion _bllNotificacion = new BLL_Notificacion();
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool autenticado = GestorDeSesion.EstaAutenticado();

            PublicActions.Visible = !autenticado;
            AuthenticatedTools.Visible = autenticado;
            AuthenticatedSidebar.Visible = autenticado;

            if (autenticado)
            {
                UserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoActual();

                // "Mis actividades" es exclusivo de gestores de espacios: a diferencia
                // de "Mis espacios" (que se muestra siempre y es la propia pantalla la
                // que le explica al solicitante que todavía no es gestor), acá el pedido
                // puntual fue que el botón ni aparezca para el perfil ExternoSolicitante.
                bool esGestorEspacios = GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString();
                MisActividadesLink.Visible = esGestorEspacios;

                // No hay SQL Server Agent en la edición Express para un job
                // programado, así que el chequeo de recordatorios se
                // aprovecha de cualquier pageview autenticado (ver el
                // throttle en BLL_Reserva, que hace que esto sea barato).
                _bllReserva.GenerarRecordatorios24hsSiCorresponde();

                CargarNotificaciones();
            }
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesion();
            Response.Redirect("~/Default.aspx");
        }

        protected void rptNotificaciones_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Ir")
            {
                return;
            }

            string argumento = (string)e.CommandArgument;
            int separador = argumento.IndexOf('|');
            if (separador < 0)
            {
                return;
            }

            int idNotificacion;
            if (!int.TryParse(argumento.Substring(0, separador), out idNotificacion))
            {
                return;
            }

            string urlDestino = argumento.Substring(separador + 1);
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            _bllNotificacion.MarcarLeida(idNotificacion, idUsuarioExterno);

            Response.Redirect(string.IsNullOrEmpty(urlDestino) ? "~/Default.aspx" : ResolveUrl(urlDestino));
        }

        protected void lnkMarcarTodasLeidas_Click(object sender, EventArgs e)
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            _bllNotificacion.MarcarTodasLeidas(idUsuarioExterno);
            CargarNotificaciones();
        }

        private void CargarNotificaciones()
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            var notificaciones = _bllNotificacion.ListarPorUsuario(idUsuarioExterno);
            int noLeidas = _bllNotificacion.ContarNoLeidas(idUsuarioExterno);

            rptNotificaciones.DataSource = notificaciones;
            rptNotificaciones.DataBind();

            pnlSinNotificaciones.Visible = notificaciones.Count == 0;

            litNotificacionesCount.Text = noLeidas > 0
                ? "<span class=\"notifications-count\" aria-label=\"" + noLeidas + " notificaciones sin leer\">" + noLeidas + "</span>"
                : string.Empty;
        }

        protected string FormatearFechaNotificacion(DateTime fecha)
        {
            if (fecha.Date == DateTime.Today)
            {
                return "Hoy " + fecha.ToString("HH:mm");
            }

            if (fecha.Date == DateTime.Today.AddDays(-1))
            {
                return "Ayer";
            }

            return fecha.ToString("dd/MM/yyyy");
        }
    }
}
