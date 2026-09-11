using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Explorar
{
    public partial class ResultadosBusqueda : Page
    {
        private const int LongitudMaximaResumen = 160;
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_Calificacion _bllCalificacion = new BLL_Calificacion();

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
            FiltroBusquedaEspacios filtro = ArmarFiltroDesdeQueryString(textoBusqueda, tipoEspacio);

            var espacios = _bllEspacio.Buscar(filtro);
            _bllCalificacion.CompletarReputacionesEspacios(espacios);

            rptEspaciosPublicados.DataSource = espacios;
            rptEspaciosPublicados.DataBind();

            pnlSinResultados.Visible = espacios.Count == 0;

            bool hayFiltrosAplicados = !string.IsNullOrWhiteSpace(textoBusqueda) || !string.IsNullOrWhiteSpace(tipoEspacio) ||
                !string.IsNullOrWhiteSpace(filtro.Ubicacion) || !string.IsNullOrWhiteSpace(filtro.TipoPiso) ||
                filtro.PrecioMaximo.HasValue || filtro.CapacidadMinima.HasValue ||
                !string.IsNullOrWhiteSpace(filtro.FechaDisponibilidad) || (filtro.Equipamiento != null && filtro.Equipamiento.Count > 0);

            if (espacios.Count == 0 && hayFiltrosAplicados)
            {
                if (!string.IsNullOrWhiteSpace(textoBusqueda))
                {
                    litTituloSinResultados.Text = "No encontramos espacios para \"" + Server.HtmlEncode(textoBusqueda) + "\"";
                }
                else if (!string.IsNullOrWhiteSpace(tipoEspacio))
                {
                    litTituloSinResultados.Text = "No encontramos espacios de tipo \"" + Server.HtmlEncode(tipoEspacio) + "\"";
                }
                else
                {
                    litTituloSinResultados.Text = "No encontramos espacios con esos filtros";
                }

                litDescripcionSinResultados.Text = "Probá con otra palabra clave o revisá los filtros.";
            }
        }

        private static FiltroBusquedaEspacios ArmarFiltroDesdeQueryString(string textoBusqueda, string tipoEspacio)
        {
            var filtro = new FiltroBusquedaEspacios
            {
                TextoBusqueda = textoBusqueda,
                TipoEspacio = tipoEspacio,
                Ubicacion = HttpContext.Current.Request.QueryString["ubicacion"],
                TipoPiso = HttpContext.Current.Request.QueryString["piso"],
                FechaDisponibilidad = HttpContext.Current.Request.QueryString["fecha"]
            };

            decimal precioMaximo;
            if (decimal.TryParse(HttpContext.Current.Request.QueryString["precioMax"], NumberStyles.Number, CultureInfo.InvariantCulture, out precioMaximo))
            {
                filtro.PrecioMaximo = precioMaximo;
            }

            int capacidadMinima;
            if (int.TryParse(HttpContext.Current.Request.QueryString["capacidadMin"], out capacidadMinima))
            {
                filtro.CapacidadMinima = capacidadMinima;
            }

            string equipamiento = HttpContext.Current.Request.QueryString["equip"];
            if (!string.IsNullOrWhiteSpace(equipamiento))
            {
                filtro.Equipamiento = new List<string>(equipamiento.Split(','));
            }

            return filtro;
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
            string ruta = espacio.Ficha != null && espacio.Ficha.Fotos != null && espacio.Ficha.Fotos.Count > 0
                ? espacio.Ficha.Fotos[0]
                : (espacio.Ficha == null ? null : espacio.Ficha.FotoRuta);
            return EsFotoValida(ruta) ? ruta : string.Empty;
        }

        protected string ObtenerUbicacion(EspacioArtistico espacio)
        {
            FichaEspacio ficha = espacio.Ficha;
            if (ficha == null)
            {
                return "Ubicación a confirmar";
            }

            if (!string.IsNullOrWhiteSpace(ficha.Ciudad) && !string.IsNullOrWhiteSpace(ficha.Provincia))
            {
                return ficha.Ciudad + ", " + ficha.Provincia;
            }

            if (!string.IsNullOrWhiteSpace(ficha.Ciudad))
            {
                return ficha.Ciudad;
            }

            return string.IsNullOrWhiteSpace(ficha.Provincia) ? "Ubicación a confirmar" : ficha.Provincia;
        }

        protected string ObtenerCapacidad(EspacioArtistico espacio)
        {
            return espacio.Ficha != null && espacio.Ficha.CapacidadMaxima.HasValue
                ? "Hasta " + espacio.Ficha.CapacidadMaxima.Value + " personas"
                : "Capacidad a consultar";
        }

        protected string ObtenerPrecio(EspacioArtistico espacio)
        {
            if (espacio.Ficha == null || !espacio.Ficha.PrecioHora.HasValue)
            {
                return "Valor a consultar";
            }

            return (espacio.Ficha.Moneda ?? "ARS") + " " +
                espacio.Ficha.PrecioHora.Value.ToString("N2", CultureInfo.CurrentCulture) + " / hora";
        }

        protected string ObtenerReputacion(EspacioArtistico espacio)
        {
            if (espacio == null || espacio.CantidadCalificaciones == 0)
            {
                return "Nuevo · Sin reseñas";
            }

            return "★ " + espacio.PromedioCalificacion.ToString("0.0", CultureInfo.CurrentCulture) +
                " · " + espacio.CantidadCalificaciones +
                (espacio.CantidadCalificaciones == 1 ? " reseña" : " reseñas");
        }

        private static bool EsFotoValida(string ruta)
        {
            return !string.IsNullOrEmpty(ruta) &&
                Regex.IsMatch(ruta, @"^~/Content/Uploads/Espacios/[a-f0-9]{32}\.jpg$");
        }

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
