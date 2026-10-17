using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno.Comercializacion
{
    // Base de las pantallas de Comercialización (CU-004): acceso por
    // permiso (A1 de cada CU), mensajes que sobreviven a la redirección
    // después de guardar y utilidades de formularios.
    public abstract class PaginaComercial : Page
    {
        private const string ClaveMensaje = "StageUp.Comercializacion.Mensaje";
        private const string ClaveMensajeError = "StageUp.Comercializacion.MensajeError";

        protected readonly BLL_CatalogoComercial Catalogos = new BLL_CatalogoComercial();

        protected abstract string PermisoRequerido { get; }

        protected int IdUsuarioInterno
        {
            get { return GestorDeSesion.ObtenerIdUsuarioInternoActual().Value; }
        }

        protected bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado", false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }

            return true;
        }

        protected void Redirigir(string url, string mensaje, bool esError = false)
        {
            if (!string.IsNullOrEmpty(mensaje))
            {
                Session[ClaveMensaje] = mensaje;
                Session[ClaveMensajeError] = esError;
            }

            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // Mensaje dejado antes de la última redirección (se muestra una vez).
        protected void MostrarMensajePendiente(Panel panel, Literal literal)
        {
            string mensaje = Session[ClaveMensaje] as string;
            if (mensaje == null)
            {
                return;
            }

            bool esError = Session[ClaveMensajeError] is bool && (bool)Session[ClaveMensajeError];
            Session.Remove(ClaveMensaje);
            Session.Remove(ClaveMensajeError);
            MostrarMensaje(panel, literal, mensaje, esError);
        }

        protected static void MostrarMensaje(Panel panel, Literal literal, string mensaje, bool esError)
        {
            literal.Text = mensaje;
            panel.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            panel.Visible = !string.IsNullOrEmpty(mensaje);
        }

        protected int? LeerEntero(string clave)
        {
            int valor;
            return int.TryParse(Request.QueryString[clave], NumberStyles.Integer, CultureInfo.InvariantCulture, out valor) && valor > 0
                ? valor
                : (int?)null;
        }

        protected string LeerTexto(string clave)
        {
            string valor = Request.QueryString[clave];
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        protected DateTime? LeerFechaQuery(string clave)
        {
            DateTime fecha;
            return DateTime.TryParseExact(Request.QueryString[clave], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                ? fecha
                : (DateTime?)null;
        }

        protected void CargarCatalogo(ListControl lista, string tipoCatalogo, string textoVacio)
        {
            lista.Items.Clear();
            if (textoVacio != null)
            {
                lista.Items.Add(new ListItem(textoVacio, string.Empty));
            }

            foreach (ItemCatalogoComercial item in Catalogos.Listar(tipoCatalogo))
            {
                lista.Items.Add(new ListItem(item.Nombre, item.Codigo));
            }
        }

        protected static void CargarDiccionario(ListControl lista, IDictionary<string, string> valores, string textoVacio)
        {
            lista.Items.Clear();
            if (textoVacio != null)
            {
                lista.Items.Add(new ListItem(textoVacio, string.Empty));
            }

            foreach (KeyValuePair<string, string> par in valores)
            {
                lista.Items.Add(new ListItem(par.Value, par.Key));
            }
        }

        protected static void Seleccionar(ListControl lista, string valor)
        {
            if (valor != null && lista.Items.FindByValue(valor) != null)
            {
                lista.ClearSelection();
                lista.Items.FindByValue(valor).Selected = true;
            }
        }

        protected static void MarcarVarios(CheckBoxList lista, IEnumerable<string> valores)
        {
            HashSet<string> elegidos = new HashSet<string>(valores ?? Enumerable.Empty<string>());
            foreach (ListItem item in lista.Items)
            {
                item.Selected = elegidos.Contains(item.Value);
            }
        }

        protected static List<string> Elegidos(CheckBoxList lista)
        {
            return lista.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList();
        }

        protected static List<int> IdsElegidos(CheckBoxList lista)
        {
            List<int> ids = new List<int>();
            foreach (ListItem item in lista.Items)
            {
                int id;
                if (item.Selected && int.TryParse(item.Value, out id))
                {
                    ids.Add(id);
                }
            }
            return ids;
        }

        protected static int? EnteroElegido(ListControl lista)
        {
            int id;
            return int.TryParse(lista.SelectedValue, out id) && id > 0 ? id : (int?)null;
        }

        protected static string Vacio(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        protected static string FechaParaCampo(DateTime? fecha)
        {
            return fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
        }

        protected static void Agregar(List<string> partes, string clave, string valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                partes.Add(clave + "=" + HttpUtility.UrlEncode(valor.Trim()));
            }
        }

        protected static string ArmarUrl(string pagina, List<string> partes)
        {
            return partes.Count == 0 ? pagina : pagina + "?" + string.Join("&", partes);
        }

        protected string TextoCatalogo(string tipoCatalogo, object codigo)
        {
            return Catalogos.Nombre(tipoCatalogo, Convert.ToString(codigo));
        }

        protected static string TextoOGuion(object valor)
        {
            string texto = Convert.ToString(valor);
            return string.IsNullOrWhiteSpace(texto) ? "—" : texto;
        }
    }
}
