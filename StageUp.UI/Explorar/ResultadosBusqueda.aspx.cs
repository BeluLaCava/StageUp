using System;
using System.Web.UI;
using StageUp.BLL;

namespace StageUp.UI.Explorar
{
    /// <summary>
    /// Code-behind de ResultadosBusqueda.aspx. Catálogo público de
    /// espacios artísticos publicados (CU-001-006): búsqueda simple por
    /// texto libre contra nombre/tipo/descripción, combinable con el
    /// filtro de tipo de espacio de la búsqueda avanzada (ítem 15 de la
    /// planilla de revisión, parámetro "tipo" en la URL).
    ///
    /// El resto de la búsqueda avanzada (ubicación, capacidad, valor de
    /// referencia, disponibilidad, características artísticas) que se ve
    /// en el panel de "Más filtros" queda como vista previa visual: esos
    /// datos viven en entidades relacionadas de EspacioArtistico que
    /// todavía no se crearon (quedan para el Avance 2, ver
    /// BLL_EspacioArtistico), así que por ahora esos filtros no están
    /// conectados a resultados reales.
    /// </summary>
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
    }
}
