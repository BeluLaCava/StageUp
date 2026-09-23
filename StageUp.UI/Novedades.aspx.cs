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
                litFeaturedCategoria.Text = Server.HtmlEncode(ObtenerEtiquetaCategoria(destacada.Categoria));
                litFeaturedTitulo.Text = Server.HtmlEncode(destacada.Titulo);
                litFeaturedResumen.Text = Server.HtmlEncode(destacada.Resumen);
            }

            List<Novedad> resto = publicadas.Skip(1).ToList();
            pnlSinMasNovedades.Visible = resto.Count == 0 && publicadas.Count > 0;
            rptNovedades.DataSource = resto;
            rptNovedades.DataBind();
        }

        protected static string ObtenerEtiquetaCategoria(string categoria)
        {
            switch (categoria)
            {
                case "Institucional":
                    return "Institucional";
                case "Catalogo":
                    return "Nuevos espacios";
                case "Consejos":
                    return "Guías StageUp";
                case "Comunidad":
                    return "Comunidad artística";
                default:
                    return "Novedades";
            }
        }
    }
}
