using System;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Explorar
{
    public partial class ResultadosBusqueda : Page
    {
        private const int LongitudMaximaResumen = 160;
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarResultados();
            }
        }

        private void CargarResultados()
        {
            string textoBusqueda = Request.QueryString["q"];
            string tipoEspacio = Request.QueryString["tipo"];
            var espacios = _bllEspacio.ListarPublicados(textoBusqueda, tipoEspacio);

            rptEspaciosPublicados.DataSource = espacios;
            rptEspaciosPublicados.DataBind();

            pnlSinResultados.Visible = espacios.Count == 0;

            bool hayFiltrosAplicados = !string.IsNullOrWhiteSpace(textoBusqueda) || !string.IsNullOrWhiteSpace(tipoEspacio);

            if (espacios.Count == 0 && hayFiltrosAplicados)
            {
                if (!string.IsNullOrWhiteSpace(textoBusqueda))
                {
                    litTituloSinResultados.Text = "No encontramos espacios para \"" + Server.HtmlEncode(textoBusqueda) + "\"";
                }
                else
                {
                    litTituloSinResultados.Text = "No encontramos espacios de tipo \"" + Server.HtmlEncode(tipoEspacio) + "\"";
                }

                litDescripcionSinResultados.Text = "Probá con otra palabra clave o revisá los filtros.";
            }
        }

        protected string ObtenerResumen(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                return string.Empty;
            }

            string texto = descripcion.Trim();
            if (texto.Length <= LongitudMaximaResumen)
            {
                return texto;
            }

            return texto.Substring(0, LongitudMaximaResumen).TrimEnd() + "…";
        }

        // "Reputación" liviana del gestor (tanda 4, sin sistema de calificaciones
        // todavía): antigüedad como gestor + cantidad de espacios publicados. Devuelve
        // vacío si el espacio no trae datos de gestor (ver EspacioArtistico.NombreGestor).
        protected string ObtenerInfoGestor(object dataItem)
        {
            var espacio = dataItem as EspacioArtistico;
            if (espacio == null || string.IsNullOrWhiteSpace(espacio.NombreCompletoGestor))
            {
                return string.Empty;
            }

            string texto = "Gestiona " + Server.HtmlEncode(espacio.NombreCompletoGestor);

            if (espacio.GestorDesde.HasValue)
            {
                texto += " · en StageUp desde " + espacio.GestorDesde.Value.ToString("MM/yyyy");
            }

            if (espacio.CantidadEspaciosPublicadosGestor > 1)
            {
                texto += " · " + espacio.CantidadEspaciosPublicadosGestor + " espacios publicados";
            }

            return texto;
        }
    }
}
