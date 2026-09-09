using System;
using System.Globalization;
using System.Text.RegularExpressions;
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

        protected string ObtenerFoto(EspacioArtistico espacio)
        {
            string ruta = espacio.Ficha == null ? null : espacio.Ficha.FotoRuta;
            if (string.IsNullOrEmpty(ruta) && espacio.Ficha != null && espacio.Ficha.FotosRutas != null && espacio.Ficha.FotosRutas.Count > 0)
                ruta = espacio.Ficha.FotosRutas[0];
            if (!string.IsNullOrEmpty(ruta) && Regex.IsMatch(ruta, @"^~/Content/Uploads/Espacios/[a-f0-9]{32}\.jpg$"))
                return ruta;
            if (string.Equals(espacio.NombreEspacio, "Sala Principal StageUp", StringComparison.OrdinalIgnoreCase))
                return "~/Content/Images/Espacios/sala-principal-ia.png";
            if (string.Equals(espacio.NombreEspacio, "Estudio Fotográfico Norte", StringComparison.OrdinalIgnoreCase))
                return "~/Content/Images/Espacios/estudio-fotografico-ia.png";
            return string.Empty;
        }

        protected string ObtenerUbicacion(EspacioArtistico espacio)
        {
            if (espacio.Ficha != null && !string.IsNullOrWhiteSpace(espacio.Ficha.Ciudad))
                return string.IsNullOrWhiteSpace(espacio.Ficha.Provincia)
                    ? espacio.Ficha.Ciudad
                    : espacio.Ficha.Ciudad + ", " + espacio.Ficha.Provincia;
            return "Ubicación a confirmar";
        }

        protected string ObtenerPrecio(EspacioArtistico espacio)
        {
            if (espacio.Ficha == null || !espacio.Ficha.PrecioHora.HasValue)
                return "Valor a consultar";
            return (espacio.Ficha.Moneda ?? "ARS") + " " +
                espacio.Ficha.PrecioHora.Value.ToString("N2", CultureInfo.GetCultureInfo("es-AR")) + " / hora";
        }
    }
}
