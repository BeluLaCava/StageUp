using System;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Explorar
{
    /// <summary>
    /// Code-behind de DetalleEspacio.aspx. Muestra el detalle público de
    /// un espacio artístico publicado (CU-001-006/CU-001-008), recibido
    /// por querystring (?id=...). Solo se muestran las columnas propias
    /// de EspacioArtistico; las secciones de imágenes, características
    /// físicas, condiciones de uso, disponibilidad y calificaciones que
    /// describe el CU completo dependen de entidades relacionadas que
    /// todavía no se crearon (quedan para el Avance 2).
    /// </summary>
    public partial class DetalleEspacio : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            int idEspacioArtistico;
            if (!int.TryParse(Request.QueryString["id"], out idEspacioArtistico))
            {
                MostrarNoEncontrado();
                return;
            }

            EspacioArtistico espacio = _bllEspacio.ObtenerDetallePublicado(idEspacioArtistico);
            if (espacio == null)
            {
                MostrarNoEncontrado(
                    "No encontramos este espacio",
                    "Puede que ya no esté publicado o que la dirección esté mal escrita.");
                return;
            }

            MostrarDetalle(espacio);
        }

        private void MostrarDetalle(EspacioArtistico espacio)
        {
            pnlEspacioNoEncontrado.Visible = false;
            pnlDetalleEspacio.Visible = true;

            litTipoEspacio.Text = Server.HtmlEncode(espacio.TipoEspacio);
            litNombreEspacio.Text = Server.HtmlEncode(espacio.NombreEspacio);
            litDescripcion.Text = string.IsNullOrWhiteSpace(espacio.Descripcion)
                ? "Este espacio todavía no tiene una descripción cargada."
                : Server.HtmlEncode(espacio.Descripcion).Replace("\n", "<br />");

            litFechaPublicacion.Text = espacio.FechaPublicacion.HasValue
                ? "Publicado el " + espacio.FechaPublicacion.Value.ToString("dd/MM/yyyy")
                : string.Empty;

            Title = espacio.NombreEspacio + " | StageUp";
        }

        private void MostrarNoEncontrado(string titulo = null, string descripcion = null)
        {
            pnlDetalleEspacio.Visible = false;
            pnlEspacioNoEncontrado.Visible = true;

            if (titulo != null)
            {
                litTituloNoEncontrado.Text = titulo;
            }

            if (descripcion != null)
            {
                litDescripcionNoEncontrado.Text = descripcion;
            }
        }
    }
}
