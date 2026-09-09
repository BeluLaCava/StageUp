using System;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Explorar
{
    public partial class DetalleEspacio : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();

        private int? IdEspacioArtistico
        {
            get { return ViewState["IdEspacioArtistico"] as int?; }
            set { ViewState["IdEspacioArtistico"] = value; }
        }

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

            IdEspacioArtistico = idEspacioArtistico;
            MostrarDetalle(espacio);
        }

        protected void btnSolicitarReserva_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || IdEspacioArtistico == null)
            {
                return;
            }

            DateTime fechaSolicitada;
            if (!DateTime.TryParse(txtFechaReserva.Text, out fechaSolicitada))
            {
                MostrarMensajeReserva("Ingresá una fecha válida.", esError: true);
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            ResultadoOperacion<int> resultado = _bllReserva.SolicitarReserva(
                idUsuarioExterno, IdEspacioArtistico.Value, fechaSolicitada, txtComentarioReserva.Text);

            MostrarMensajeReserva(resultado.Mensaje, !resultado.Exitoso);

            if (resultado.Exitoso)
            {
                txtFechaReserva.Text = string.Empty;
                txtComentarioReserva.Text = string.Empty;
            }
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

            int? idUsuarioActual = GestorDeSesion.EstaAutenticado() ? GestorDeSesion.ObtenerIdUsuarioActual() : null;

            pnlReservarInvitado.Visible = idUsuarioActual == null;
            pnlReservarPropio.Visible = idUsuarioActual != null && idUsuarioActual.Value == espacio.IdUsuarioGestor;
            pnlReservarFormulario.Visible = idUsuarioActual != null && idUsuarioActual.Value != espacio.IdUsuarioGestor;
        }

        private void MostrarMensajeReserva(string mensaje, bool esError)
        {
            litReservarMensaje.Text = mensaje;
            pnlReservarMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlReservarMensaje.Visible = !string.IsNullOrEmpty(mensaje);
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
