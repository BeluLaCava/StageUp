using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class GestionFaq : Page
    {
        // Cantidad de preguntas activas que se muestran en la vista previa
        // (ya ordenadas por "orden", igual que las ve el público en el
        // centro de ayuda).
        private const int CantidadPreview = 4;

        private readonly BLL_Faq _bllFaq = new BLL_Faq();

        private int? IdFaqEnEdicion
        {
            get { return ViewState["IdFaqEnEdicion"] as int?; }
            set { ViewState["IdFaqEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarFaqs();
            }
        }

        protected void btnGuardarFaq_Click(object sender, EventArgs e)
        {
            Guardar(chkActiva.Checked);
        }

        protected void btnGuardarBorradorFaq_Click(object sender, EventArgs e)
        {
            // "Borrador": guarda igual que "Guardar", pero siempre inactiva,
            // sin importar lo que tenga tildado el checkbox — así la
            // pregunta no aparece todavía en el centro de ayuda público.
            Guardar(false);
        }

        protected void lnkCancelarFaq_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void rptFaq_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idFaq = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;

            switch (e.CommandName)
            {
                case "Editar":
                    CargarFaqEnFormulario(idFaq);
                    return;

                case "Baja":
                    resultado = _bllFaq.DarDeBaja(idFaq, idResponsable);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    if (resultado.Exitoso && IdFaqEnEdicion == idFaq)
                    {
                        LimpiarFormulario();
                    }
                    CargarFaqs();
                    return;

                case "Activar":
                    resultado = _bllFaq.Reactivar(idFaq, idResponsable);
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    CargarFaqs();
                    return;
            }
        }

        private void Guardar(bool activo)
        {
            if (!Page.IsValid || !TieneAcceso())
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            int orden;
            if (!int.TryParse(txtOrden.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out orden))
            {
                MostrarMensaje("Ingresá un orden válido.", true);
                return;
            }

            ResultadoOperacion resultado;
            if (IdFaqEnEdicion.HasValue)
            {
                resultado = _bllFaq.Modificar(
                    IdFaqEnEdicion.Value, txtPregunta.Text, txtRespuesta.Text, orden, activo, idResponsable);
            }
            else
            {
                ResultadoOperacion<int> resultadoAlta = _bllFaq.Registrar(
                    txtPregunta.Text, txtRespuesta.Text, orden, activo, idResponsable);
                resultado = resultadoAlta;
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (!resultado.Exitoso)
            {
                return;
            }

            LimpiarFormulario();
            CargarFaqs();
        }

        private void CargarFaqs()
        {
            List<Faq> todas = _bllFaq.Listar();
            int cantidadActivas = todas.Count(f => f.Activo);
            int cantidadBorrador = todas.Count - cantidadActivas;
            DateTime? ultimaEdicion = todas.Count == 0
                ? (DateTime?)null
                : todas.Max(f => f.FechaUltimaModificacion ?? f.FechaAlta);

            litCantidadActivas.Text = cantidadActivas.ToString(CultureInfo.InvariantCulture);
            litCantidadBorrador.Text = cantidadBorrador.ToString(CultureInfo.InvariantCulture);
            litUltimaEdicion.Text = FormatearUltimaEdicion(ultimaEdicion);

            pnlSinFaqs.Visible = todas.Count == 0;
            rptFaq.DataSource = todas;
            rptFaq.DataBind();

            List<Faq> preview = todas.Where(f => f.Activo).Take(CantidadPreview).ToList();
            pnlSinPreview.Visible = preview.Count == 0;
            rptFaqPreview.DataSource = preview;
            rptFaqPreview.DataBind();
        }

        private void CargarFaqEnFormulario(int idFaq)
        {
            Faq faq = _bllFaq.ObtenerPorId(idFaq);
            if (faq == null)
            {
                MostrarMensaje("No se encontró la pregunta frecuente seleccionada.", true);
                return;
            }

            IdFaqEnEdicion = faq.IdFaq;
            txtPregunta.Text = faq.Pregunta;
            txtRespuesta.Text = faq.Respuesta;
            txtOrden.Text = faq.Orden.ToString(CultureInfo.InvariantCulture);
            chkActiva.Checked = faq.Activo;
            litTituloFormulario.Text = "Editar pregunta";
            btnGuardarFaq.Text = "Guardar cambios";
            lnkCancelarFaq.Visible = true;
        }

        private void LimpiarFormulario()
        {
            IdFaqEnEdicion = null;
            txtPregunta.Text = string.Empty;
            txtRespuesta.Text = string.Empty;
            txtOrden.Text = string.Empty;
            chkActiva.Checked = true;
            litTituloFormulario.Text = "Nueva pregunta";
            btnGuardarFaq.Text = "Crear pregunta";
            lnkCancelarFaq.Visible = false;
        }

        private static string FormatearUltimaEdicion(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                return "Sin datos";
            }

            return fecha.Value.Date == DateTime.Today
                ? "Hoy"
                : fecha.Value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
        }

        protected static string ObtenerClaseFilaFaq(Faq faq)
        {
            return faq != null && faq.Activo ? "faq-admin-row" : "faq-admin-row faq-admin-row-muted";
        }

        protected static string ObtenerClaseEstadoFaq(Faq faq)
        {
            return faq != null && faq.Activo
                ? "admin-status-badge admin-status-badge-active"
                : "admin-status-badge admin-status-badge-inactive";
        }

        protected static string ObtenerTextoEstadoFaq(Faq faq)
        {
            return faq != null && faq.Activo ? "Activa" : "Borrador";
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_FAQ"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return false;
            }

            return true;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError
                ? "form-message form-message-error"
                : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
