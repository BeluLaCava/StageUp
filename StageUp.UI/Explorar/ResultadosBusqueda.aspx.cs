using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Explorar
{
    public partial class ResultadosBusqueda : Page
    {
        private const int LongitudMaximaResumen = 160;
        private const int CantidadRanking = 5;
        private static readonly List<string> CategoriasServicio = new List<string> { "Estudio", "Sala", "Teatro" };
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

            List<EspacioArtistico> espacios = _bllEspacio.Buscar(filtro);
            _bllCalificacion.CompletarReputacionesEspacios(espacios);

            bool hayFiltrosAplicados = !string.IsNullOrWhiteSpace(textoBusqueda) || !string.IsNullOrWhiteSpace(tipoEspacio) ||
                !string.IsNullOrWhiteSpace(filtro.Ubicacion) || !string.IsNullOrWhiteSpace(filtro.TipoPiso) ||
                !string.IsNullOrWhiteSpace(filtro.Provincia) || !string.IsNullOrWhiteSpace(filtro.Ciudad) ||
                !string.IsNullOrWhiteSpace(filtro.Zona) || !string.IsNullOrWhiteSpace(filtro.Caracteristicas) ||
                filtro.PrecioMinimo.HasValue || filtro.PrecioMaximo.HasValue || filtro.CalificacionMinima.HasValue || filtro.CapacidadMinima.HasValue ||
                !string.IsNullOrWhiteSpace(filtro.FechaDisponibilidad) || (filtro.Equipamiento != null && filtro.Equipamiento.Count > 0);




            List<EspacioArtistico> espaciosPublicados = hayFiltrosAplicados
                ? _bllEspacio.Buscar(new FiltroBusquedaEspacios())
                : espacios;

            string orden = CargarOpcionesOrden();
            espacios = BLL_EspacioArtistico.Ordenar(espacios, orden);

            CargarTiposServicio(espaciosPublicados);

            rptEspaciosPublicados.DataSource = espacios;
            rptEspaciosPublicados.DataBind();

            pnlSinResultados.Visible = espacios.Count == 0;

            CargarRanking(espaciosPublicados, hayFiltrosAplicados);

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



        private string CargarOpcionesOrden()
        {
            ddlOrden.Items.Clear();
            ddlOrden.Items.Add(new ListItem("Relevancia", BLL_EspacioArtistico.OrdenRelevancia));
            ddlOrden.Items.Add(new ListItem("Mejor valorados", BLL_EspacioArtistico.OrdenMejorValorados));
            ddlOrden.Items.Add(new ListItem("Menor precio", BLL_EspacioArtistico.OrdenPrecioMenor));
            ddlOrden.Items.Add(new ListItem("Mayor precio", BLL_EspacioArtistico.OrdenPrecioMayor));
            ddlOrden.Items.Add(new ListItem("Mayor capacidad", BLL_EspacioArtistico.OrdenCapacidad));
            ddlOrden.Items.Add(new ListItem("Más recientes", BLL_EspacioArtistico.OrdenRecientes));

            string orden = Request.QueryString["orden"] ?? string.Empty;
            if (ddlOrden.Items.FindByValue(orden) == null)
            {
                orden = BLL_EspacioArtistico.OrdenRelevancia;
            }

            ddlOrden.SelectedValue = orden;
            return orden;
        }



        private void CargarRanking(List<EspacioArtistico> espaciosPublicados, bool hayFiltrosAplicados)
        {
            if (hayFiltrosAplicados)
            {
                pnlRanking.Visible = false;
                return;
            }


            List<EspacioArtistico> ranking = BLL_Calificacion.ObtenerRankingMejorValorados(espaciosPublicados, CantidadRanking);

            pnlRanking.Visible = ranking.Count > 0;
            litCriterioRanking.Text = "Ordenados por promedio ponderado por cantidad de reseñas. Solo participan espacios con al menos " +
                BLL_Calificacion.MinimoResenasRanking + " reseñas, para que una sola calificación no defina el ranking.";
            rptRanking.DataSource = ranking;
            rptRanking.DataBind();
        }

        private void CargarTiposServicio(List<EspacioArtistico> espaciosPublicados)
        {
            List<string> categoriasDisponibles = CategoriasServicio
                .Where(categoria => espaciosPublicados.Any(espacio => PerteneceACategoriaServicio(espacio, categoria)))
                .ToList();

            pnlComparacionServicios.Visible = categoriasDisponibles.Count >= 2;
            rptTiposServicio.DataSource = categoriasDisponibles;
            rptTiposServicio.DataBind();
        }

        private static bool PerteneceACategoriaServicio(EspacioArtistico espacio, string categoria)
        {
            if (espacio == null || string.IsNullOrWhiteSpace(espacio.TipoEspacio))
            {
                return false;
            }

            return espacio.TipoEspacio.IndexOf(categoria, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private static FiltroBusquedaEspacios ArmarFiltroDesdeQueryString(string textoBusqueda, string tipoEspacio)
        {
            FiltroBusquedaEspacios filtro = new FiltroBusquedaEspacios
            {
                TextoBusqueda = textoBusqueda,
                TipoEspacio = tipoEspacio,
                Ubicacion = HttpContext.Current.Request.QueryString["ubicacion"],
                Provincia = HttpContext.Current.Request.QueryString["provincia"],
                Ciudad = HttpContext.Current.Request.QueryString["ciudad"],
                Zona = HttpContext.Current.Request.QueryString["zona"],
                TipoPiso = HttpContext.Current.Request.QueryString["piso"],
                Caracteristicas = HttpContext.Current.Request.QueryString["caracteristicas"],
                FechaDisponibilidad = HttpContext.Current.Request.QueryString["fecha"]
            };

            decimal precioMinimo;
            if (decimal.TryParse(HttpContext.Current.Request.QueryString["precioMin"], NumberStyles.Number, CultureInfo.InvariantCulture, out precioMinimo))
            {
                filtro.PrecioMinimo = precioMinimo;
            }

            decimal precioMaximo;
            if (decimal.TryParse(HttpContext.Current.Request.QueryString["precioMax"], NumberStyles.Number, CultureInfo.InvariantCulture, out precioMaximo))
            {
                filtro.PrecioMaximo = precioMaximo;
            }

            decimal calificacionMinima;
            if (decimal.TryParse(HttpContext.Current.Request.QueryString["calificacionMin"], NumberStyles.Number, CultureInfo.InvariantCulture, out calificacionMinima))
            {
                filtro.CalificacionMinima = calificacionMinima;
            }

            int capacidadMinima;
            if (int.TryParse(HttpContext.Current.Request.QueryString["capacidadMin"], out capacidadMinima))
            {
                filtro.CapacidadMinima = capacidadMinima;
            }

            filtro.MinutoDesde = LeerMinutos(HttpContext.Current.Request.QueryString["horaDesde"]);
            filtro.MinutoHasta = LeerMinutos(HttpContext.Current.Request.QueryString["horaHasta"]);

            string equipamiento = HttpContext.Current.Request.QueryString["equip"];
            if (!string.IsNullOrWhiteSpace(equipamiento))
            {
                filtro.Equipamiento = new List<string>(equipamiento.Split(','));
            }

            return filtro;
        }

        private static int? LeerMinutos(string valor)
        {
            TimeSpan hora;
            return TimeSpan.TryParseExact(valor ?? string.Empty, @"hh\:mm", CultureInfo.InvariantCulture, out hora)
                ? (int?)hora.TotalMinutes
                : null;
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
            EspacioArtistico espacio = dataItem as EspacioArtistico;
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
