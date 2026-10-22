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


        protected static string Resumir(object valor, int largo)
        {
            string texto = Convert.ToString(valor) ?? string.Empty;
            return texto.Length <= largo ? texto : texto.Substring(0, largo).TrimEnd() + "…";
        }

        protected static string Periodo(object desde, object hasta)
        {
            string inicio = desde is DateTime ? ((DateTime)desde).ToString("dd/MM/yyyy") : "—";
            string fin = hasta is DateTime ? ((DateTime)hasta).ToString("dd/MM/yyyy") : "sin fecha de fin";
            return inicio + " al " + fin;
        }


        protected void CargarVinculoUnico(DropDownList lista, string tipo, string textoVacio)
        {
            lista.Items.Clear();
            lista.Items.Add(new ListItem(textoVacio, string.Empty));
            foreach (OpcionVinculoComercial opcion in Catalogos.ListarParaVincular(tipo))
            {
                lista.Items.Add(new ListItem(opcion.Nombre, opcion.Id.ToString(CultureInfo.InvariantCulture)));
            }
        }




        protected static void AsegurarOpcion(DropDownList lista, int? id, string nombre)
        {
            if (!id.HasValue)
            {
                return;
            }

            string valor = id.Value.ToString(CultureInfo.InvariantCulture);
            if (lista.Items.FindByValue(valor) == null)
            {
                lista.Items.Add(new ListItem((nombre ?? "#" + valor) + " (no disponible para nuevas asociaciones)", valor));
            }

            Seleccionar(lista, valor);
        }

        protected static void MarcarVinculos(CheckBoxList lista, List<VinculoComercial> vinculos, string tipo)
        {
            foreach (VinculoComercial vinculo in vinculos.Where(v => v.Tipo == tipo))
            {
                string valor = vinculo.Id.ToString(CultureInfo.InvariantCulture);
                ListItem item = lista.Items.FindByValue(valor);
                if (item == null)
                {
                    item = new ListItem(vinculo.Nombre + " (" + TextoEstadoVinculo(vinculo) + ")", valor);
                    lista.Items.Add(item);
                }

                item.Selected = true;
            }
        }

        protected static string TextoTipoVinculo(object tipo)
        {
            switch (Convert.ToString(tipo))
            {
                case OpcionVinculoComercial.TipoContacto: return "Contacto comercial";
                case OpcionVinculoComercial.TipoCampania: return "Campaña comercial";
                case OpcionVinculoComercial.TipoAlianza: return "Alianza estratégica";
                case OpcionVinculoComercial.TipoContenido: return "Contenido promocional";
                case OpcionVinculoComercial.TipoOrigen: return "Origen de usuario";
                default: return Convert.ToString(tipo);
            }
        }

        protected static string TextoEstadoVinculo(object dato)
        {
            VinculoComercial vinculo = (VinculoComercial)dato;
            switch (vinculo.Tipo)
            {
                case OpcionVinculoComercial.TipoContacto: return BLL_ContactoComercial.TextoEstado(vinculo.Estado);
                case OpcionVinculoComercial.TipoAlianza: return BLL_AlianzaEstrategica.TextoEstado(vinculo.Estado);
                case OpcionVinculoComercial.TipoContenido: return BLL_ContenidoPromocional.TextoEstado(vinculo.Estado);
                case OpcionVinculoComercial.TipoCampania:
                    string texto;
                    return vinculo.Estado == "DadaDeBaja" ? "Dada de baja"
                        : BLL_CampaniaComercial.Estados.TryGetValue(vinculo.Estado ?? string.Empty, out texto) ? texto : vinculo.Estado;
                default: return TextoOGuion(vinculo.Estado);
            }
        }



        protected string EnlaceVinculo(object dato)
        {
            VinculoComercial vinculo = (VinculoComercial)dato;
            string nombre = HttpUtility.HtmlEncode(vinculo.Nombre);
            string url = null;
            switch (vinculo.Tipo)
            {
                case OpcionVinculoComercial.TipoContacto:
                    url = GestorDeSesion.TienePermisoInterno(BLL_ContactoComercial.PermisoGestionar) ? "ContactosComerciales.aspx?ver=" + vinculo.Id : null;
                    break;
                case OpcionVinculoComercial.TipoCampania:
                    url = GestorDeSesion.TienePermisoInterno(BLL_CampaniaComercial.PermisoGestionar) ? "CampaniasComerciales.aspx?ver=" + vinculo.Id : null;
                    break;
                case OpcionVinculoComercial.TipoOrigen:
                    url = GestorDeSesion.TienePermisoInterno(BLL_CampaniaComercial.PermisoGestionar) ? "CampaniasComerciales.aspx?vista=origenes&amp;origen=" + vinculo.Id : null;
                    break;
                case OpcionVinculoComercial.TipoAlianza:
                    url = GestorDeSesion.TienePermisoInterno(BLL_AlianzaEstrategica.PermisoGestionar) ? "AlianzasEstrategicas.aspx?ver=" + vinculo.Id : null;
                    break;
                case OpcionVinculoComercial.TipoContenido:
                    url = GestorDeSesion.TienePermisoInterno(BLL_ContenidoPromocional.PermisoGestionar) ? "ContenidoPromocional.aspx?ver=" + vinculo.Id : null;
                    break;
            }

            return url == null ? nombre : "<a class=\"text-link\" href=\"" + url + "\">" + nombre + "</a>";
        }
    }
}
