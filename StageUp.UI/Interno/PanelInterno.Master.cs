using System;
using System.Collections.Generic;
using System.Globalization;
using StageUp.BE.Entidades;
using StageUp.BE.Menu;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno
{
    public partial class PanelInterno : MasterPageMultidioma
    {
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();
        private int _ticketsAbiertos;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ActualizarSesionInterna();
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);
        }







        private void ActualizarSesionInterna()
        {
            int? idUsuarioInterno = GestorDeSesion.ObtenerIdUsuarioInternoActual();
            if (!idUsuarioInterno.HasValue)
            {
                return;
            }

            List<string> permisos;
            UsuarioInterno usuario;
            try
            {
                usuario = new BLL_UsuarioInterno().ObtenerParaSesion(idUsuarioInterno.Value, out permisos);
            }
            catch (Exception)
            {



                GestorDeSesion.SuspenderSesionEnEstePedido();
                string volver = Request.HttpMethod == "GET" ? Request.RawUrl : string.Empty;
                Response.Redirect(ControlSesionBaseDatos.PaginaVerificacion +
                    (volver.Length > 0 ? "?volver=" + Server.UrlEncode(volver) : string.Empty));
                return;
            }

            if (usuario == null)
            {
                GestorDeSesion.CerrarSesionInterna();
                Response.Redirect("~/IniciarSesion.aspx?motivo=cuenta_interna_inactiva");
                return;
            }




            if (!GestorDeSesion.ActualizarSesionInterna(usuario, permisos))
            {
                GestorDeSesion.CerrarSesionInterna();
                Response.Redirect("~/IniciarSesion.aspx?motivo=" + ControlSesionBaseDatos.MotivoCierre);
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            InternalUserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoInternoActual();
            pnlAvisoRestauracion.Visible = GestorDeSesion.TomarAvisoRestauracion();
            CargarMenu();
        }

        protected void lnkCerrarSesionInterna_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesionInterna();
            Response.Redirect("~/IniciarSesion.aspx");
        }

        protected string ObtenerIndicadorPendientes(object url)
        {
            string direccion = Convert.ToString(url);
            if (_ticketsAbiertos <= 0
                || direccion.IndexOf("GestionSoporte.aspx", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return string.Empty;
            }

            string cantidad = _ticketsAbiertos.ToString(CultureInfo.InvariantCulture);
            return " <span class=\"internal-nav-badge\" title=\"Solicitudes de soporte que requieren atención (pendientes o con mensajes sin leer)\" "
                + "style=\"display:inline-block;min-width:20px;padding:1px 7px;margin-left:6px;border-radius:999px;"
                + "background:#7a0c20;color:#fff;font-size:0.75em;font-weight:bold;text-align:center;\">"
                + cantidad + "</span>";
        }

        private void CargarMenu()
        {
            int idRolInterno = GestorDeSesion.ObtenerIdRolInternoActual().Value;
            GrupoMenu raiz = _bllPermiso.ConstruirMenuParaRol(idRolInterno);
            List<ItemMenu> items = raiz.ObtenerItems();




            _ticketsAbiertos = GestorDeSesion.TienePermisoInterno(BLL_Ticket.PermisoGestionar)
                ? new BLL_Ticket().ContarTicketsAbiertos()
                : 0;

            rptMenu.DataSource = items;
            rptMenu.DataBind();
        }
    }
}
