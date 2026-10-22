using System;
using System.Collections.Generic;
using System.Globalization;
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
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);
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

        protected string ObtenerIconoMenu(object url)
        {
            string direccion = Convert.ToString(url) ?? string.Empty;
            if (direccion.IndexOf("AprobacionGestores", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M7.5 12.5l3 3 6-7'/><path d='M12 3.5a4 4 0 110 8 4 4 0 010-8z'/><path d='M4.5 20c.7-3.3 3.1-5 6-5'/>");
            if (direccion.IndexOf("BackupRestore", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M4 8V4m0 0h4M4 4l3.2 3.2A7 7 0 1112 19'/><path d='M12 8v4l2.5 1.5'/>");
            if (direccion.IndexOf("BusquedaInterna", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<circle cx='10.5' cy='10.5' r='5.5'/><path d='M15 15l5 5'/><path d='M7.8 10.5h5.4M10.5 7.8v5.4'/>");
            if (direccion.IndexOf("CampaniasComerciales", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M4 13V9l11-4v12L4 13z'/><path d='M15 9.2c2 .4 3 1.3 3 1.8s-1 1.4-3 1.8M6 13l1.5 6h3L9 14'/>");
            if (direccion.IndexOf("ContactosComerciales", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<circle cx='9' cy='8' r='3'/><path d='M3.5 19c.5-3.6 2.3-5.5 5.5-5.5s5 1.9 5.5 5.5'/><path d='M16 7h4M18 5v4M16 14h4M18 12v4'/>");
            if (direccion.IndexOf("AlianzasEstrategicas", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M8.5 14.5l-2 2a3 3 0 01-4.2-4.2l3-3a3 3 0 014.2 0'/><path d='M15.5 9.5l2-2a3 3 0 014.2 4.2l-3 3a3 3 0 01-4.2 0'/><path d='M8 16l8-8'/>");
            if (direccion.IndexOf("MetricasComerciales", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M4 19V9M10 19V5M16 19v-7M22 19H2'/><path d='M4 6l5-3 6 4 5-4'/>");
            if (direccion.IndexOf("ContenidoPromocional", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<rect x='3' y='4' width='18' height='16' rx='2'/><path d='M7 15l3-3 3 3 2-2 3 3'/><circle cx='16.5' cy='8.5' r='1.5'/>");
            if (direccion.IndexOf("GestionFaq", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M5 5.5A3.5 3.5 0 018.5 2h7A3.5 3.5 0 0119 5.5v7a3.5 3.5 0 01-3.5 3.5H10l-5 4v-4.8a3.5 3.5 0 01-2-3.2V5.5'/><path d='M9.5 8a2.5 2.5 0 014.8.9c0 1.8-2.3 2-2.3 3.6M12 15.5h.01'/>");
            if (direccion.IndexOf("GestionEncuestas", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M5 3h14v18H5z'/><path d='M8 8h5M8 12h8M8 16h4'/><path d='M16 6l1 1 2-2'/>");
            if (direccion.IndexOf("GestionIdiomas", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<circle cx='12' cy='12' r='9'/><path d='M3 12h18M12 3c2.4 2.5 3.5 5.5 3.5 9S14.4 18.5 12 21M12 3c-2.4 2.5-3.5 5.5-3.5 9s1.1 6.5 3.5 9'/>");
            if (direccion.IndexOf("GestionMenu", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M8 6h12M8 12h12M8 18h12'/><circle cx='4' cy='6' r='1'/><circle cx='4' cy='12' r='1'/><circle cx='4' cy='18' r='1'/>");
            if (direccion.IndexOf("GestionNovedades", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M4 5h16v14H4z'/><path d='M8 9h8M8 13h8M8 17h5'/><path d='M4 8h16'/>");
            if (direccion.IndexOf("GestionRoles", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M7 11a4 4 0 110-8 4 4 0 010 8zM2 21c.4-4.3 2.1-6.5 5-6.5 1.6 0 2.8.6 3.6 1.7'/><circle cx='16.5' cy='15.5' r='3.5'/><path d='M16.5 12v-2M16.5 21v-2M13 15.5h-2M22 15.5h-2M14 13l-1.4-1.4M20.4 19.4L19 18M19 13l1.4-1.4M12.6 19.4L14 18'/>");
            if (direccion.IndexOf("GestionSoporte", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M4 13v-2a8 8 0 0116 0v2'/><path d='M4 12H2v5a2 2 0 002 2h2v-7H4zM20 12h2v5a2 2 0 01-2 2h-2v-7h2zM18 19c-1 2-3 2-5 2'/>");
            if (direccion.IndexOf("GestionUsuariosInternos", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<circle cx='9' cy='8' r='3.5'/><path d='M2.5 20c.5-4 2.7-6 6.5-6s6 2 6.5 6'/><path d='M18 8v6M15 11h6'/>");
            if (direccion.IndexOf("HerramientasSeguridad", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M12 2l8 3v6c0 5.2-3 8.7-8 11-5-2.3-8-5.8-8-11V5l8-3z'/><rect x='8.5' y='10' width='7' height='6' rx='1'/><path d='M10 10V8.5a2 2 0 014 0V10'/>");
            if (direccion.IndexOf("Reportes", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M5 3h11l3 3v15H5z'/><path d='M16 3v4h4M8 17v-4M12 17V9M16 17v-6'/>");
            if (direccion.IndexOf("GestionCuentasCorrientes", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<rect x='3' y='5' width='18' height='14' rx='2'/><path d='M3 9h18M16 14h3'/><path d='M8 12v5M6.5 13.2H9a1.2 1.2 0 010 2.4H6.5'/>");
            if (direccion.IndexOf("ParametrosPlataforma", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M4 6h10M18 6h2M4 12h2M10 12h10M4 18h7M15 18h5'/><circle cx='16' cy='6' r='2'/><circle cx='8' cy='12' r='2'/><circle cx='13' cy='18' r='2'/>");
            if (direccion.IndexOf("PagosReservas", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<rect x='3' y='5' width='18' height='14' rx='2'/><path d='M3 9h18M7 15h4'/><path d='M16 12v5M14.5 13.2H17a1.2 1.2 0 010 2.4h-2.5'/>");
            if (direccion.IndexOf("TransaccionesEconomicas", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M4 8h15l-3-3M20 16H5l3 3'/>");
            if (direccion.IndexOf("HistorialFinanciero", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2M5.5 5.5L3 6V3'/>");
            if (direccion.IndexOf("RegistrosActividad", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<path d='M7 3h10v4H7zM5 5H3v16h14v-2M9 11h10M9 15h10M9 19h6'/><circle cx='5' cy='11' r='1'/><circle cx='5' cy='15' r='1'/>");
            if (direccion.IndexOf("GestionNewsletter", StringComparison.OrdinalIgnoreCase) >= 0) return CrearIcono("<rect x='3' y='5' width='18' height='14' rx='2'/><path d='M4 7l8 6 8-6'/><path d='M16 3v4M14 5h4'/>");
            return CrearIcono("<rect x='4' y='4' width='6' height='6' rx='1'/><rect x='14' y='4' width='6' height='6' rx='1'/><rect x='4' y='14' width='6' height='6' rx='1'/><rect x='14' y='14' width='6' height='6' rx='1'/>");
        }

        private static string CrearIcono(string contenido)
        {
            return "<svg viewBox='0 0 24 24' focusable='false' aria-hidden='true' fill='none' stroke='currentColor' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'>" + contenido + "</svg>";
        }

        private void CargarMenu()
        {
            int idRolInterno = GestorDeSesion.ObtenerIdRolInternoActual().Value;
            GrupoMenu raiz = _bllPermiso.ConstruirMenuParaRol(idRolInterno);
            List<ItemMenu> items = raiz.ObtenerItems();
            CompareInfo comparador = CultureInfo.GetCultureInfo("es-AR").CompareInfo;
            items.Sort((izquierda, derecha) => comparador.Compare(
                izquierda.Nombre ?? string.Empty,
                derecha.Nombre ?? string.Empty,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace));

            _ticketsAbiertos = GestorDeSesion.TienePermisoInterno(BLL_Ticket.PermisoGestionar)
                ? new BLL_Ticket().ContarTicketsAbiertos()
                : 0;

            rptMenu.DataSource = items;
            rptMenu.DataBind();
        }
    }
}
