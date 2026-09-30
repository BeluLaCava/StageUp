using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI
{
    // Búsqueda global pública (ítem 4 de la segunda entrega). La búsqueda
    // viaja en la URL (?q=...&tipos=...) para que se pueda compartir o volver
    // atrás con el navegador.
    public partial class Buscar : Page
    {
        private readonly BLL_Busqueda _bllBusqueda = new BLL_Busqueda();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
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
            List<string> tipos = cblTipos.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList();
            string url = "~/Buscar.aspx?q=" + HttpUtility.UrlEncode(txtBusqueda.Text.Trim());
            if (tipos.Count > 0)
            {
                url += "&tipos=" + HttpUtility.UrlEncode(string.Join(",", tipos));
            }

            Response.Redirect(url);
        }

        private void CargarTipos()
        {
            cblTipos.Items.Clear();
            foreach (KeyValuePair<string, string> tipo in BLL_Busqueda.ObtenerTiposPublicos())
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
            ResultadoOperacion<List<GrupoResultadosBusqueda>> resultado = _bllBusqueda.BuscarPublico(texto, tipos);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            int total = resultado.Valor.Sum(g => g.Resultados.Count);
            pnlResumen.Visible = true;
            litResumen.Text = total == 0
                ? "No encontramos resultados para \"" + Server.HtmlEncode(texto.Trim()) + "\". Probá con otra palabra o buscá en todos los tipos de contenido."
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
    }
}
