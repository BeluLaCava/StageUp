using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web.Script.Serialization;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BE.Interfaces;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class SiteMaster : MasterPage, IObservadorIdioma
    {
        private readonly BLL_Idioma _bllIdioma = new BLL_Idioma();
        private readonly BLL_Multidioma _bllMultidioma = new BLL_Multidioma();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            _bllMultidioma.RegistrarObservador(this);
            InicializarIdioma();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool autenticado = GestorDeSesion.EstaAutenticado();

            PublicActions.Visible = !autenticado;
            AuthenticatedTools.Visible = autenticado;
            AuthenticatedSidebar.Visible = autenticado;

            if (autenticado)
            {
                UserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoActual();
            }
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesion();
            Response.Redirect("~/Default.aspx");
        }

        public void ActualizarIdioma(Idioma idioma, IList<Traduccion> traducciones)
        {
            var porClave = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var porTexto = new Dictionary<string, string>(StringComparer.CurrentCulture);

            foreach (Traduccion traduccion in traducciones)
            {
                porClave[traduccion.ClaveEtiqueta] = traduccion.TextoTraducido;
                if (!string.IsNullOrWhiteSpace(traduccion.TextoPredeterminado))
                {
                    porTexto[traduccion.TextoPredeterminado.Trim()] = traduccion.TextoTraducido;
                }
            }

            var configuracion = new
            {
                codigoIdioma = idioma.CodigoIdioma,
                porClave,
                porTexto
            };

            hdnDiccionarioIdioma.Value = new JavaScriptSerializer().Serialize(configuracion);
            HtmlRoot.Attributes["lang"] = ObtenerCodigoHtml(idioma.CodigoIdioma);
        }

        protected override void OnUnload(EventArgs e)
        {
            _bllMultidioma.RetirarObservador(this);
            base.OnUnload(e);
        }

        private void InicializarIdioma()
        {
            List<Idioma> idiomas = _bllIdioma.Listar();
            if (idiomas.Count == 0)
            {
                LanguageSelector.Visible = false;
                return;
            }

            int? idSolicitado = ObtenerIdIdiomaSolicitado();
            Idioma idiomaSeleccionado = BuscarIdioma(idiomas, idSolicitado);
            if (idiomaSeleccionado == null)
            {
                idiomaSeleccionado = BuscarPredeterminado(idiomas) ?? idiomas[0];
            }

            ddlIdioma.DataSource = idiomas;
            ddlIdioma.DataBind();
            ddlIdioma.SelectedValue = idiomaSeleccionado.IdIdioma.ToString(CultureInfo.InvariantCulture);
            LanguageSelector.Visible = true;

            GestorDeSesion.EstablecerIdiomaActual(idiomaSeleccionado.IdIdioma, idiomaSeleccionado.CodigoIdioma);
            AplicarCultura(idiomaSeleccionado.CodigoIdioma);
            _bllMultidioma.NotificarObservadores(idiomaSeleccionado);
        }

        private int? ObtenerIdIdiomaSolicitado()
        {
            int idIdioma;
            string valorEnviado = Request.Form[ddlIdioma.UniqueID];
            if (int.TryParse(valorEnviado, out idIdioma))
            {
                return idIdioma;
            }

            return GestorDeSesion.ObtenerIdIdiomaActual();
        }

        private static Idioma BuscarIdioma(List<Idioma> idiomas, int? idIdioma)
        {
            if (!idIdioma.HasValue)
            {
                return null;
            }

            foreach (Idioma idioma in idiomas)
            {
                if (idioma.IdIdioma == idIdioma.Value && idioma.Activo)
                {
                    return idioma;
                }
            }

            return null;
        }

        private static Idioma BuscarPredeterminado(List<Idioma> idiomas)
        {
            foreach (Idioma idioma in idiomas)
            {
                if (idioma.EsPredeterminado && idioma.Activo)
                {
                    return idioma;
                }
            }

            return null;
        }

        private static void AplicarCultura(string codigoIdioma)
        {
            try
            {
                CultureInfo cultura = CultureInfo.GetCultureInfo(codigoIdioma);
                Thread.CurrentThread.CurrentCulture = cultura;
                Thread.CurrentThread.CurrentUICulture = cultura;
            }
            catch (CultureNotFoundException)
            {
            }
        }

        private static string ObtenerCodigoHtml(string codigoIdioma)
        {
            if (string.IsNullOrWhiteSpace(codigoIdioma))
            {
                return "es";
            }

            int separador = codigoIdioma.IndexOf('-');
            return separador > 0 ? codigoIdioma.Substring(0, separador) : codigoIdioma;
        }
    }
}
