using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI
{
    public partial class Novedades : Page
    {
        private readonly BLL_Novedad _bllNovedad = new BLL_Novedad();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarNovedades();
            }
        }

        private void CargarNovedades()
        {
            List<Novedad> publicadas = _bllNovedad.ListarPublicadas();

            pnlSinNovedades.Visible = publicadas.Count == 0;
            pnlDestacada.Visible = publicadas.Count > 0;

            if (publicadas.Count > 0)
            {
                Novedad destacada = publicadas[0];
                string categoria = Server.HtmlEncode(ObtenerEtiquetaCategoria(destacada.Categoria));
                string titulo = Server.HtmlEncode(destacada.Titulo);
                string resumen = Server.HtmlEncode(destacada.Resumen);
                litFeaturedCategoria.Text = categoria;
                litFeaturedCategoriaDialogo.Text = categoria;
                litFeaturedTitulo.Text = titulo;
                litFeaturedTituloDialogo.Text = titulo;
                litFeaturedResumen.Text = resumen;
                litFeaturedResumenDialogo.Text = resumen;
                litFeaturedContenido.Text = Server.HtmlEncode(destacada.Contenido);
                imgFeatured.ImageUrl = ResolverUrlImagen(destacada.UrlImagen);
                imgFeatured.AlternateText = destacada.Titulo;
                imgFeatured.Visible = TieneImagen(destacada.UrlImagen);
            }

            List<Novedad> resto = publicadas.Skip(1).ToList();
            pnlSinMasNovedades.Visible = resto.Count == 0 && publicadas.Count > 0;
            rptNovedades.DataSource = resto;
            rptNovedades.DataBind();
        }


        protected static string ObtenerEtiquetaCategoria(string categoria)
        {
            return BLL_CategoriaNovedad.ObtenerEtiqueta(categoria);
        }

        protected string ResolverUrlImagen(object valor)
        {
            string url = valor == null ? null : valor.ToString();
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            url = url.Trim();
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            return ResolveUrl(url);
        }

        protected static bool TieneImagen(object valor)
        {
            return valor != null && !string.IsNullOrWhiteSpace(valor.ToString());
        }
    }
}
