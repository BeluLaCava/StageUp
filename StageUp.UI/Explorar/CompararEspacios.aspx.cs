using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Explorar
{
    public partial class CompararEspacios : Page
    {
        private const int CantidadMaximaEspacios = 3;
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            List<int> idsSolicitados = ParsearIds(Request.QueryString["ids"]);
            List<EspacioArtistico> espacios = new List<EspacioArtistico>();
            int noEncontrados = 0;

            foreach (int id in idsSolicitados)
            {
                if (espacios.Count >= CantidadMaximaEspacios)
                {
                    break;
                }

                EspacioArtistico espacio = _bllEspacio.ObtenerDetallePublicado(id);
                if (espacio == null)
                {
                    noEncontrados++;
                }
                else
                {
                    espacios.Add(espacio);
                }
            }

            if (espacios.Count < 2)
            {
                MostrarSinSeleccion(idsSolicitados.Count > 0);
                return;
            }

            MostrarComparacion(espacios, noEncontrados);
        }

        private static List<int> ParsearIds(string valorQueryString)
        {
            List<int> ids = new List<int>();
            if (string.IsNullOrWhiteSpace(valorQueryString))
            {
                return ids;
            }

            foreach (string parte in valorQueryString.Split(','))
            {
                int id;
                if (int.TryParse(parte.Trim(), out id) && !ids.Contains(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private void MostrarSinSeleccion(bool huboIdsInvalidos)
        {
            pnlSinSeleccion.Visible = true;
            pnlComparacion.Visible = false;

            if (huboIdsInvalidos)
            {
                litTituloSinSeleccion.Text = "No pudimos armar la comparación";
                litDescripcionSinSeleccion.Text =
                    "Los espacios elegidos ya no están publicados o la dirección está mal escrita. Volvé al catálogo y elegí de nuevo.";
            }
        }

        private void MostrarComparacion(List<EspacioArtistico> espacios, int noEncontrados)
        {
            pnlSinSeleccion.Visible = false;
            pnlComparacion.Visible = true;

            litTablaComparacion.Text = ArmarTablaComparacion(espacios);

            if (noEncontrados > 0)
            {
                pnlAlgunosNoEncontrados.Visible = true;
                litAlgunosNoEncontrados.Text = noEncontrados == 1
                    ? "Un espacio de tu selección ya no está disponible y no se incluyó en la comparación."
                    : noEncontrados + " espacios de tu selección ya no están disponibles y no se incluyeron en la comparación.";
            }

            Title = "Comparar espacios | StageUp";
        }

        private string ArmarTablaComparacion(List<EspacioArtistico> espacios)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<table class=\"comparison-table\">");

            html.Append("<thead><tr><th scope=\"col\" class=\"comparison-row-label\">&nbsp;</th>");
            foreach (EspacioArtistico espacio in espacios)
            {
                html.Append("<th scope=\"col\"><a href=\"DetalleEspacio.aspx?id=")
                    .Append(espacio.IdEspacioArtistico)
                    .Append("\">")
                    .Append(Server.HtmlEncode(espacio.NombreEspacio))
                    .Append("</a></th>");
            }
            html.Append("</tr></thead><tbody>");

            AgregarFilaComparacion(html, "Tipo de espacio", espacios,
                espacio => Server.HtmlEncode(espacio.TipoEspacio));

            AgregarFilaComparacion(html, "Publicado el", espacios,
                espacio => espacio.FechaPublicacion.HasValue
                    ? espacio.FechaPublicacion.Value.ToString("d", CultureInfo.CurrentCulture)
                    : "-");

            AgregarFilaComparacion(html, "Descripción", espacios,
                espacio => string.IsNullOrWhiteSpace(espacio.Descripcion)
                    ? "Sin descripción cargada."
                    : Server.HtmlEncode(espacio.Descripcion).Replace("\n", "<br />"));

            html.Append("<tr><th scope=\"row\" class=\"comparison-row-label\">&nbsp;</th>");
            foreach (EspacioArtistico espacio in espacios)
            {
                html.Append("<td><a class=\"button button-secondary button-small\" href=\"DetalleEspacio.aspx?id=")
                    .Append(espacio.IdEspacioArtistico)
                    .Append("\">Ver detalle</a></td>");
            }
            html.Append("</tr>");

            html.Append("</tbody></table>");
            return html.ToString();
        }

        private void AgregarFilaComparacion(
            StringBuilder html, string etiqueta, List<EspacioArtistico> espacios,
            Func<EspacioArtistico, string> obtenerValor)
        {
            html.Append("<tr><th scope=\"row\" class=\"comparison-row-label\">").Append(etiqueta).Append("</th>");

            foreach (EspacioArtistico espacio in espacios)
            {
                html.Append("<td>").Append(obtenerValor(espacio)).Append("</td>");
            }

            html.Append("</tr>");
        }
    }
}
