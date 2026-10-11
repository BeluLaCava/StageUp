using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{
    public partial class SiteMaster : MasterPageMultidioma
    {
        private readonly BLL_Notificacion _bllNotificacion = new BLL_Notificacion();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);

            // CU-001-003: si la cuenta se dio de baja (por ejemplo desde otro
            // navegador), la sesión se corta antes de que la página procese nada.
            int? idUsuario = GestorDeSesion.ObtenerIdUsuarioActual();
            if (idUsuario.HasValue && GestorDeSesion.CorrespondeVerificarCuenta())
            {
                string perfilActual;
                if (!new BLL_UsuarioExterno().CuentaSigueActiva(idUsuario.Value, out perfilActual))
                {
                    GestorDeSesion.CerrarSesion();
                    Response.Redirect("~/Default.aspx?cuenta=baja", true);
                    return;
                }

                // CU-001-007: si un administrador resolvió la habilitación como
                // gestor, el menú se actualiza sin volver a iniciar sesión.
                if (perfilActual != null && perfilActual != GestorDeSesion.ObtenerPerfilActual())
                {
                    GestorDeSesion.ActualizarPerfilEnSesion(perfilActual);
                }

                GestorDeSesion.MarcarCuentaVerificada();
            }
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

                // CU-001-007: quien todavía no es gestor ve "Ofrecer espacio"
                // (solicitud de habilitación y su estado) en lugar de "Mis espacios".
                if (!esGestorEspacios)
                {
                    MisEspaciosLink.InnerText = "Ofrecer espacio";
                    MisEspaciosLink.HRef = "~/OfrecerEspacio.aspx";
                    MisEspaciosLink.Attributes.Remove("data-i18n");
                }

                // No hay SQL Server Agent en la edición Express para un job
                // programado, así que el chequeo de recordatorios se
                // aprovecha de cualquier pageview autenticado (ver el
                // throttle en BLL_Reserva, que hace que esto sea barato).
                // Ítem 36: corre en segundo plano, para que el usuario que
                // dispara el chequeo no espere el envío de los mails.
                if (BLL_Reserva.CorrespondeGenerarRecordatorios())
                {
                    try
                    {
                        System.Web.Hosting.HostingEnvironment.QueueBackgroundWorkItem(
                            cancelacion => new BLL_Reserva().GenerarRecordatorios24hsSiCorresponde());
                    }
                    catch (InvalidOperationException)
                    {
                        // Fuera de IIS / IIS Express no hay cola en segundo plano:
                        // se genera en el mismo request, como antes.
                        new BLL_Reserva().GenerarRecordatorios24hsSiCorresponde();
                    }
                }

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

            // CU-001-011 pasos 7 a 9: el destino sale de la notificación
            // guardada (no del navegador). Si el elemento ya no se puede
            // consultar (A9) se muestra el motivo en el centro de notificaciones.
            int idNotificacion;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idNotificacion))
            {
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            string error;
            string destino = AperturaNotificacion.ObtenerDestino(idNotificacion, idUsuarioExterno, out error);
            Response.Redirect(destino != null
                ? ResolveUrl(destino)
                : "~/Notificaciones.aspx?no_disponible=" + idNotificacion, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lnkMarcarTodasLeidas_Click(object sender, EventArgs e)
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            _bllNotificacion.MarcarTodasLeidas(idUsuarioExterno);
            CargarNotificaciones();
        }

        // Para páginas que cambian el estado de las notificaciones después de
        // que el menú ya se cargó (centro de notificaciones).
        public void ActualizarNotificaciones()
        {
            if (GestorDeSesion.EstaAutenticado())
            {
                CargarNotificaciones();
            }
        }

        private void CargarNotificaciones()
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            List<Notificacion> notificaciones = _bllNotificacion.ListarPorUsuario(idUsuarioExterno);
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
