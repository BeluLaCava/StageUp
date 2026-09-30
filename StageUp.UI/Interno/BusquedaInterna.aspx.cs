using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // Búsqueda interna de la plataforma (ítem 13 de la segunda entrega). Qué
    // tipos de información se pueden buscar depende de los permisos del rol
    // (lo resuelve BLL_Busqueda).
    public partial class BusquedaInterna : Page
    {
        private readonly BLL_Busqueda _bllBusqueda = new BLL_Busqueda();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso() || IsPostBack)
            {
                return;
            }

            CargarTipos();

            string texto = Request.QueryString["q"];
            List<string> tipos = LeerTiposDeQueryString();
            foreach (ListItem item in cblTipos.Items)
            {
                item.Selected = tipos.Contains(item.Value);
            }

            if (tipos.Count > 0)
            {
                busquedaAvanzada.Attributes["open"] = "open";
            }

            if (!string.IsNullOrWhiteSpace(texto))
            {
                txtBusqueda.Text = texto;
                EjecutarBusqueda(texto, tipos);
            }
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            List<string> tipos = cblTipos.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList();
            string url = "~/Interno/BusquedaInterna.aspx?q=" + HttpUtility.UrlEncode(txtBusqueda.Text.Trim());
            if (tipos.Count > 0)
            {
                url += "&tipos=" + HttpUtility.UrlEncode(string.Join(",", tipos));
            }

            Response.Redirect(url);
        }

        private void CargarTipos()
        {
            cblTipos.Items.Clear();
            int idRol = GestorDeSesion.ObtenerIdRolInternoActual().Value;
            foreach (KeyValuePair<string, string> tipo in _bllBusqueda.ObtenerTiposInternosPermitidos(idRol))
            {
                cblTipos.Items.Add(new ListItem(tipo.Value, tipo.Key));
            }
        }

        private List<string> LeerTiposDeQueryString()
        {
            string valor = Request.QueryString["tipos"];
            return string.IsNullOrWhiteSpace(valor)
                ? new List<string>()
                : valor.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        }

        private void EjecutarBusqueda(string texto, List<string> tipos)
        {
            int idRol = GestorDeSesion.ObtenerIdRolInternoActual().Value;
            ResultadoOperacion<List<GrupoResultadosBusqueda>> resultado = _bllBusqueda.BuscarInterno(texto, tipos, idRol);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            int total = resultado.Valor.Sum(g => g.Resultados.Count);
            pnlResumen.Visible = true;
            litResumen.Text = total == 0
                ? "No encontramos resultados para \"" + Server.HtmlEncode(texto.Trim()) + "\"."
                : total + (total == 1 ? " resultado" : " resultados") + " para \"" + Server.HtmlEncode(texto.Trim()) + "\".";

            rptGrupos.DataSource = resultado.Valor;
            rptGrupos.DataBind();
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("BUSCAR_EN_PLATAFORMA"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return false;
            }

            return true;
        }
    }
}
