using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Explorar
{
    public partial class CompararServicios : Page
    {
        private const int CantidadMaximaTipos = 3;
        private static readonly List<string> CategoriasServicio = new List<string> { "Estudio", "Sala", "Teatro" };
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_Calificacion _bllCalificacion = new BLL_Calificacion();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            List<string> tiposSolicitados = ParsearTipos(Request.QueryString["tipos"]);
            if (tiposSolicitados.Count < 2)
            {
                MostrarSinSeleccion(false);
                return;
            }

            List<EspacioArtistico> espaciosPublicados = _bllEspacio.Buscar(new FiltroBusquedaEspacios());
            _bllCalificacion.CompletarReputacionesEspacios(espaciosPublicados);

            List<ResumenTipoServicio> resumenes = new List<ResumenTipoServicio>();
            int noEncontrados = 0;

            foreach (string tipo in tiposSolicitados)
            {
                if (resumenes.Count >= CantidadMaximaTipos)
                {
                    break;
                }

                List<EspacioArtistico> espaciosDelTipo = espaciosPublicados
                    .Where(espacio => PerteneceACategoriaServicio(espacio, tipo))
                    .ToList();

                if (espaciosDelTipo.Count == 0)
                {
                    noEncontrados++;
                    continue;
                }

                resumenes.Add(CrearResumen(tipo, espaciosDelTipo));
            }

            if (resumenes.Count < 2)
            {
                MostrarSinSeleccion(tiposSolicitados.Count > 0);
                return;
            }

            MostrarComparacion(resumenes, noEncontrados);
        }

        private static List<string> ParsearTipos(string valorQueryString)
        {
            List<string> tipos = new List<string>();
            if (string.IsNullOrWhiteSpace(valorQueryString))
            {
                return tipos;
            }

            foreach (string parte in valorQueryString.Split(','))
            {
                string tipo = NormalizarCategoria(parte.Trim());
                bool repetido = tipos.Any(tipoExistente =>
                    string.Equals(tipoExistente, tipo, StringComparison.CurrentCultureIgnoreCase));

                if (!string.IsNullOrWhiteSpace(tipo) && !repetido)
                {
                    tipos.Add(tipo);
                }
            }

            return tipos;
        }

        private static string NormalizarCategoria(string valor)
        {
            return CategoriasServicio.FirstOrDefault(categoria =>
                string.Equals(categoria, valor, StringComparison.CurrentCultureIgnoreCase)) ?? valor;
        }

        private static bool PerteneceACategoriaServicio(EspacioArtistico espacio, string categoria)
        {
            if (espacio == null || string.IsNullOrWhiteSpace(espacio.TipoEspacio))
            {
                return false;
            }

            return espacio.TipoEspacio.IndexOf(categoria, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void MostrarSinSeleccion(bool huboTiposInvalidos)
        {
            pnlSinSeleccion.Visible = true;
            pnlComparacion.Visible = false;

            if (huboTiposInvalidos)
            {
                litTituloSinSeleccion.Text = "No pudimos armar la comparación";
                litDescripcionSinSeleccion.Text =
                    "Los tipos elegidos no tienen espacios publicados o la dirección está mal escrita. Volvé al catálogo y elegí de nuevo.";
            }
        }

        private void MostrarComparacion(List<ResumenTipoServicio> resumenes, int noEncontrados)
        {
            pnlSinSeleccion.Visible = false;
            pnlComparacion.Visible = true;

            litTablaComparacion.Text = ArmarTablaComparacion(resumenes);

            if (noEncontrados > 0)
            {
                pnlAlgunosNoEncontrados.Visible = true;
                litAlgunosNoEncontrados.Text = noEncontrados == 1
                    ? "Un tipo elegido no tiene espacios publicados y no se incluyó en la comparación."
                    : noEncontrados + " tipos elegidos no tienen espacios publicados y no se incluyeron en la comparación.";
            }

            Title = "Comparar servicios | StageUp";
        }

        private ResumenTipoServicio CrearResumen(string categoria, List<EspacioArtistico> espacios)
        {
            return new ResumenTipoServicio
            {
                TipoEspacio = categoria,
                CantidadEspacios = espacios.Count,
                Capacidad = FormatearCapacidad(espacios),
                Precio = FormatearPrecio(espacios),
                Equipamiento = FormatearEquipamiento(espacios),
                Reputacion = FormatearReputacion(espacios)
            };
        }

        private string ArmarTablaComparacion(List<ResumenTipoServicio> resumenes)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<table class=\"comparison-table\">");

            html.Append("<thead><tr><th scope=\"col\" class=\"comparison-row-label\">&nbsp;</th>");
            foreach (ResumenTipoServicio resumen in resumenes)
            {
                html.Append("<th scope=\"col\">")
                    .Append(Server.HtmlEncode(resumen.TipoEspacio))
                    .Append("</th>");
            }
            html.Append("</tr></thead><tbody>");

            AgregarFilaComparacion(html, "Espacios publicados", resumenes,
                resumen => resumen.CantidadEspacios.ToString(CultureInfo.CurrentCulture));
            AgregarFilaComparacion(html, "Capacidad de referencia", resumenes,
                resumen => resumen.Capacidad);
            AgregarFilaComparacion(html, "Valor de referencia", resumenes,
                resumen => resumen.Precio);
            AgregarFilaComparacion(html, "Características frecuentes", resumenes,
                resumen => resumen.Equipamiento);
            AgregarFilaComparacion(html, "Reputación promedio", resumenes,
                resumen => resumen.Reputacion);

            html.Append("<tr><th scope=\"row\" class=\"comparison-row-label\">&nbsp;</th>");
            foreach (ResumenTipoServicio resumen in resumenes)
            {
                html.Append("<td><a class=\"button button-secondary button-small\" href=\"ResultadosBusqueda.aspx?tipo=")
                    .Append(Server.UrlEncode(resumen.TipoEspacio))
                    .Append("\">Ver espacios</a></td>");
            }
            html.Append("</tr>");

            html.Append("</tbody></table>");
            return html.ToString();
        }

        private void AgregarFilaComparacion(
            StringBuilder html, string etiqueta, List<ResumenTipoServicio> resumenes,
            Func<ResumenTipoServicio, string> obtenerValor)
        {
            html.Append("<tr><th scope=\"row\" class=\"comparison-row-label\">").Append(etiqueta).Append("</th>");

            foreach (ResumenTipoServicio resumen in resumenes)
            {
                html.Append("<td>").Append(obtenerValor(resumen)).Append("</td>");
            }

            html.Append("</tr>");
        }

        private static string FormatearCapacidad(List<EspacioArtistico> espacios)
        {
            List<int> capacidades = espacios
                .Where(espacio => espacio.Ficha != null && espacio.Ficha.CapacidadMaxima.HasValue)
                .Select(espacio => espacio.Ficha.CapacidadMaxima.Value)
                .OrderBy(capacidad => capacidad)
                .ToList();

            if (capacidades.Count == 0)
            {
                return "Capacidad a consultar";
            }

            int minima = capacidades.First();
            int maxima = capacidades.Last();
            double promedio = capacidades.Average();
            string promedioTexto = "Promedio " + Math.Round(promedio).ToString("N0", CultureInfo.CurrentCulture) + " personas";

            return minima == maxima
                ? promedioTexto
                : promedioTexto + "<br />Rango " + minima + " - " + maxima + " personas";
        }

        private static string FormatearPrecio(List<EspacioArtistico> espacios)
        {
            List<FichaEspacio> fichasConPrecio = espacios
                .Where(espacio => espacio.Ficha != null && espacio.Ficha.PrecioHora.HasValue)
                .Select(espacio => espacio.Ficha)
                .ToList();

            if (fichasConPrecio.Count == 0)
            {
                return "Valor a consultar";
            }

            List<string> preciosPorMoneda = fichasConPrecio
                .GroupBy(ficha => string.IsNullOrWhiteSpace(ficha.Moneda) ? "ARS" : ficha.Moneda)
                .OrderBy(grupo => grupo.Key)
                .Select(grupo => FormatearRangoPrecio(grupo.Key, grupo.Select(ficha => ficha.PrecioHora.Value).ToList()))
                .ToList();

            return string.Join("<br />", preciosPorMoneda);
        }

        private static string FormatearRangoPrecio(string moneda, List<decimal> precios)
        {
            decimal minimo = precios.Min();
            decimal maximo = precios.Max();

            if (minimo == maximo)
            {
                return moneda + " " + minimo.ToString("N2", CultureInfo.CurrentCulture) + " / hora";
            }

            return "Desde " + moneda + " " + minimo.ToString("N2", CultureInfo.CurrentCulture) +
                "<br />Hasta " + moneda + " " + maximo.ToString("N2", CultureInfo.CurrentCulture) + " / hora";
        }

        private string FormatearEquipamiento(List<EspacioArtistico> espacios)
        {
            List<string> equipamiento = espacios
                .Where(espacio => espacio.Ficha != null && espacio.Ficha.Equipamiento != null)
                .SelectMany(espacio => espacio.Ficha.Equipamiento)
                .Where(codigo => !string.IsNullOrWhiteSpace(codigo))
                .Select(ObtenerNombreEquipamiento)
                .GroupBy(nombre => nombre, StringComparer.CurrentCultureIgnoreCase)
                .OrderByDescending(grupo => grupo.Count())
                .ThenBy(grupo => grupo.Key)
                .Select(grupo => Server.HtmlEncode(grupo.Key) + " (" + grupo.Count() + " de " + espacios.Count + ")")
                .ToList();

            if (equipamiento.Count == 0)
            {
                return "Sin características cargadas";
            }

            List<string> primerasCaracteristicas = equipamiento.Take(5).ToList();
            string texto = string.Join("<br />", primerasCaracteristicas);
            int restantes = equipamiento.Count - primerasCaracteristicas.Count;

            return restantes > 0 ? texto + "<br />+" + restantes + " más" : texto;
        }

        private static string FormatearReputacion(List<EspacioArtistico> espacios)
        {
            int cantidadCalificaciones = espacios.Sum(espacio => espacio.CantidadCalificaciones);
            if (cantidadCalificaciones == 0)
            {
                return "Sin reseñas disponibles";
            }

            decimal sumaPonderada = espacios.Sum(espacio => espacio.PromedioCalificacion * espacio.CantidadCalificaciones);
            decimal promedio = sumaPonderada / cantidadCalificaciones;

            return "Promedio " + promedio.ToString("0.0", CultureInfo.CurrentCulture) +
                " · " + cantidadCalificaciones +
                (cantidadCalificaciones == 1 ? " reseña" : " reseñas");
        }

        private static string ObtenerNombreEquipamiento(string codigo)
        {
            switch (codigo)
            {
                case "ESPEJOS": return "Espejos";
                case "SONIDO": return "Sonido y acústica";
                case "INSTRUMENTOS": return "Instrumentos";
                case "EQUIPAMIENTO": return "Equipamiento";
                case "ESCENARIO": return "Escenario";
                case "ILUMINACION": return "Iluminación";
                default: return codigo;
            }
        }

        private sealed class ResumenTipoServicio
        {
            public string TipoEspacio { get; set; }
            public int CantidadEspacios { get; set; }
            public string Capacidad { get; set; }
            public string Precio { get; set; }
            public string Equipamiento { get; set; }
            public string Reputacion { get; set; }
        }
    }
}
