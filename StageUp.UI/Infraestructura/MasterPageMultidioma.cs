using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Interfaces;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Infraestructura
{
    public abstract class MasterPageMultidioma : MasterPage, IObservadorIdioma
    {
        private readonly BLL_Idioma _bllIdioma = new BLL_Idioma();
        private readonly BLL_Multidioma _bllMultidioma = new BLL_Multidioma();
        private DropDownList _selectorIdioma;
        private HiddenField _diccionarioIdioma;
        private HtmlElement _htmlRoot;
        private Control _contenedorSelector;
        private Idioma _idiomaSeleccionado;

        protected void InicializarMultidioma(
            DropDownList selectorIdioma,
            HiddenField diccionarioIdioma,
            HtmlElement htmlRoot,
            Control contenedorSelector)
        {
            _selectorIdioma = selectorIdioma;
            _diccionarioIdioma = diccionarioIdioma;
            _htmlRoot = htmlRoot;
            _contenedorSelector = contenedorSelector;

            _bllMultidioma.RegistrarObservador(this);
            CargarIdiomaActual();
        }

        public void ActualizarIdioma(Idioma idioma, IList<Traduccion> traducciones)
        {
            var porClave = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var porTexto = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (Traduccion traduccion in traducciones)
            {
                if (string.IsNullOrWhiteSpace(traduccion.TextoTraducido))
                {
                    continue;
                }

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

            _diccionarioIdioma.Value = new JavaScriptSerializer().Serialize(configuracion);
            _htmlRoot.Attributes["lang"] = ObtenerCodigoHtml(idioma.CodigoIdioma);
        }

        protected override void OnUnload(EventArgs e)
        {
            _bllMultidioma.RetirarObservador(this);
            base.OnUnload(e);
        }

        protected override void OnPreRender(EventArgs e)
        {
            if (_idiomaSeleccionado != null)
            {
                _bllMultidioma.NotificarObservadores(_idiomaSeleccionado);
            }

            base.OnPreRender(e);
        }

        private void CargarIdiomaActual()
        {
            List<Idioma> idiomas = _bllIdioma.Listar();
            if (idiomas.Count == 0)
            {
                _contenedorSelector.Visible = false;
                return;
            }

            int? idSolicitado = ObtenerIdIdiomaSolicitado();
            Idioma idiomaSeleccionado = BuscarIdioma(idiomas, idSolicitado)
                ?? BuscarPredeterminado(idiomas)
                ?? idiomas[0];

            _selectorIdioma.DataSource = idiomas;
            _selectorIdioma.DataBind();
            _selectorIdioma.SelectedValue = idiomaSeleccionado.IdIdioma.ToString(CultureInfo.InvariantCulture);
            _contenedorSelector.Visible = true;
            _idiomaSeleccionado = idiomaSeleccionado;

            GestorDeSesion.EstablecerIdiomaActual(idiomaSeleccionado.IdIdioma, idiomaSeleccionado.CodigoIdioma);
            AplicarCultura(idiomaSeleccionado.CodigoIdioma);
        }

        private int? ObtenerIdIdiomaSolicitado()
        {
            int idIdioma;
            string valorEnviado = Request.Form[_selectorIdioma.UniqueID];
            if (int.TryParse(valorEnviado, out idIdioma))
            {
                return idIdioma;
            }

            return GestorDeSesion.ObtenerIdIdiomaActual();
        }

        private static Idioma BuscarIdioma(IEnumerable<Idioma> idiomas, int? idIdioma)
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

        private static Idioma BuscarPredeterminado(IEnumerable<Idioma> idiomas)
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
