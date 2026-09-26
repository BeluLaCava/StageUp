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
    public partial class GestionEncuestas : Page
    {
        private const string FormatoFechaHoraLocal = "yyyy-MM-ddTHH:mm";

        private readonly BLL_Encuesta _bllEncuesta = new BLL_Encuesta();

        // Se completa en CargarPreguntas(), justo antes de bindear
        // rptPreguntas, para que el data-binding "Visible='<%# EsBorrador %>'"
        // del ItemTemplate refleje la encuesta que se está mostrando.
        private bool _esBorrador;
        protected bool EsBorrador
        {
            get { return _esBorrador; }
        }

        private int? IdEncuestaSeleccionada
        {
            get { return ViewState["IdEncuestaSeleccionada"] as int?; }
            set { ViewState["IdEncuestaSeleccionada"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarResumen();
                CargarEncuestas();
                CargarSeleccion();
            }
        }

        protected void btnGuardarEncuesta_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || !TieneAcceso())
            {
                return;
            }

            DateTime fechaInicio;
            DateTime fechaVencimiento;
            if (!TryParseFechaHoraLocal(txtFechaInicio.Text, out fechaInicio) ||
                !TryParseFechaHoraLocal(txtFechaVencimiento.Text, out fechaVencimiento))
            {
                MostrarMensaje("Revisá las fechas ingresadas.", true);
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;

            if (IdEncuestaSeleccionada.HasValue)
            {
                resultado = _bllEncuesta.Modificar(
                    IdEncuestaSeleccionada.Value, txtTitulo.Text, txtDescripcion.Text,
                    fechaInicio, fechaVencimiento, ddlPublicoObjetivo.SelectedValue, idResponsable);
            }
            else
            {
                ResultadoOperacion<int> resultadoAlta = _bllEncuesta.Registrar(
                    txtTitulo.Text, txtDescripcion.Text, fechaInicio, fechaVencimiento,
                    ddlPublicoObjetivo.SelectedValue, idResponsable);
                resultado = resultadoAlta;
                if (resultadoAlta.Exitoso)
                {
                    IdEncuestaSeleccionada = resultadoAlta.Valor;
                }
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarResumen();
            CargarEncuestas();
            CargarSeleccion();
        }

        protected void lnkCancelarEncuesta_Click(object sender, EventArgs e)
        {
            IdEncuestaSeleccionada = null;
            LimpiarFormulario();
            CargarSeleccion();
        }

        protected void btnAgregarPregunta_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdEncuestaSeleccionada.HasValue)
            {
                return;
            }

            List<string> opciones = (txtOpcionesPregunta.Text ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .ToList();

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion<int> resultado = _bllEncuesta.AgregarPregunta(
                IdEncuestaSeleccionada.Value, txtTextoPregunta.Text, opciones, idResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                txtTextoPregunta.Text = string.Empty;
                txtOpcionesPregunta.Text = string.Empty;
            }

            CargarSeleccion();
        }

        protected void rptPreguntas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || !IdEncuestaSeleccionada.HasValue || e.CommandName != "Quitar")
            {
                return;
            }

            int idPregunta = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;

            ResultadoOperacion resultado = _bllEncuesta.EliminarPregunta(idPregunta, IdEncuestaSeleccionada.Value, idResponsable);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarSeleccion();
        }

        protected void rptPreguntas_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            Repeater rptOpciones = (Repeater)e.Item.FindControl("rptOpcionesPregunta");
            rptOpciones.DataBind();
        }

        protected void rptResultados_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            Repeater rptOpciones = (Repeater)e.Item.FindControl("rptOpcionesResultado");
            rptOpciones.DataBind();
        }

        protected void btnPublicar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdEncuestaSeleccionada.HasValue)
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllEncuesta.Publicar(IdEncuestaSeleccionada.Value, idResponsable);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);

            CargarResumen();
            CargarEncuestas();
            CargarSeleccion();
        }

        protected void btnCerrar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdEncuestaSeleccionada.HasValue)
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllEncuesta.Cerrar(IdEncuestaSeleccionada.Value, idResponsable);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);

            CargarResumen();
            CargarEncuestas();
            CargarSeleccion();
        }

        protected void rptEncuestas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || e.CommandName != "Gestionar")
            {
                return;
            }

            IdEncuestaSeleccionada = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            CargarSeleccion();
        }

        private void CargarResumen()
        {
            List<Encuesta> todas = _bllEncuesta.Listar();
            litCantidadActivas.Text = todas.Count(x => x.Estado == "Activa").ToString(CultureInfo.InvariantCulture);
            litCantidadBorrador.Text = todas.Count(x => x.Estado == "Borrador").ToString(CultureInfo.InvariantCulture);
            litCantidadCerradas.Text = todas.Count(x => x.Estado == "Cerrada").ToString(CultureInfo.InvariantCulture);
        }

        private void CargarEncuestas()
        {
            List<Encuesta> todas = _bllEncuesta.Listar();
            pnlSinEncuestas.Visible = todas.Count == 0;
            rptEncuestas.DataSource = todas;
            rptEncuestas.DataBind();
        }

        private void CargarSeleccion()
        {
            Encuesta seleccionada = IdEncuestaSeleccionada.HasValue ? _bllEncuesta.ObtenerPorId(IdEncuestaSeleccionada.Value) : null;
            if (seleccionada == null)
            {
                IdEncuestaSeleccionada = null;
                LimpiarFormulario();
                pnlFormularioEncuesta.Visible = true;
                pnlDatosSoloLectura.Visible = false;
                pnlPreguntas.Visible = false;
                pnlEstadoAcciones.Visible = false;
                pnlResultados.Visible = false;
                return;
            }

            bool esBorrador = seleccionada.Estado == "Borrador";

            pnlFormularioEncuesta.Visible = esBorrador;
            pnlDatosSoloLectura.Visible = !esBorrador;

            if (esBorrador)
            {
                CargarFormularioParaEdicion(seleccionada);
            }
            else
            {
                CargarSoloLectura(seleccionada);
            }

            CargarPreguntas(seleccionada);
            CargarEstadoAcciones(seleccionada);

            pnlResultados.Visible = !esBorrador;
            if (!esBorrador)
            {
                CargarResultados(seleccionada.IdEncuesta);
            }
        }

        private void CargarFormularioParaEdicion(Encuesta encuesta)
        {
            litTituloFormulario.Text = "Editar encuesta";
            btnGuardarEncuesta.Text = "Guardar cambios";
            lnkCancelarEncuesta.Visible = true;
            txtTitulo.Text = encuesta.Titulo;
            txtDescripcion.Text = encuesta.Descripcion;
            txtFechaInicio.Text = encuesta.FechaInicio.ToString(FormatoFechaHoraLocal, CultureInfo.InvariantCulture);
            txtFechaVencimiento.Text = encuesta.FechaVencimiento.ToString(FormatoFechaHoraLocal, CultureInfo.InvariantCulture);
            ddlPublicoObjetivo.SelectedValue = encuesta.PublicoObjetivo;
        }

        private void CargarSoloLectura(Encuesta encuesta)
        {
            litTituloSoloLectura.Text = Server.HtmlEncode(encuesta.Titulo);
            litDescripcionSoloLectura.Text = string.IsNullOrEmpty(encuesta.Descripcion)
                ? "Sin descripción."
                : Server.HtmlEncode(encuesta.Descripcion);
            litVigenciaSoloLectura.Text = Server.HtmlEncode(
                encuesta.FechaInicio.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture) + " a " +
                encuesta.FechaVencimiento.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture));
            litPublicoSoloLectura.Text = Server.HtmlEncode(TextoPublicoObjetivo(encuesta.PublicoObjetivo));
        }

        private void CargarPreguntas(Encuesta encuesta)
        {
            _esBorrador = encuesta.Estado == "Borrador";
            pnlPreguntas.Visible = true;
            pnlAgregarPregunta.Visible = _esBorrador;

            List<PreguntaEncuesta> preguntas = _bllEncuesta.ListarPreguntasConOpciones(encuesta.IdEncuesta);
            pnlSinPreguntas.Visible = preguntas.Count == 0;
            rptPreguntas.DataSource = preguntas;
            rptPreguntas.DataBind();
        }

        private void CargarEstadoAcciones(Encuesta encuesta)
        {
            pnlEstadoAcciones.Visible = true;
            litEstadoBadge.Text = "<span class=\"" + ClaseBadgeEstado(encuesta) + "\">" + Server.HtmlEncode(TextoEstado(encuesta)) + "</span>";

            btnPublicar.Visible = encuesta.Estado == "Borrador";
            btnCerrar.Visible = encuesta.Estado == "Activa";
            litEstadoSinAcciones.Visible = encuesta.Estado == "Cerrada";
        }

        private void CargarResultados(int idEncuesta)
        {
            List<ResultadoPreguntaEncuesta> resultados = _bllEncuesta.ObtenerResultadosAdmin(idEncuesta);
            int total = resultados.Count == 0 ? 0 : resultados[0].TotalRespuestas;
            litTotalRespuestas.Text = total == 0
                ? "Todavía no hay respuestas."
                : "Total de personas que respondieron: " + total.ToString(CultureInfo.InvariantCulture);

            rptResultados.DataSource = resultados;
            rptResultados.DataBind();
        }

        private void LimpiarFormulario()
        {
            litTituloFormulario.Text = "Nueva encuesta";
            btnGuardarEncuesta.Text = "Crear encuesta";
            lnkCancelarEncuesta.Visible = false;
            txtTitulo.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtFechaInicio.Text = string.Empty;
            txtFechaVencimiento.Text = string.Empty;
            ddlPublicoObjetivo.SelectedIndex = 0;
        }

        private static bool TryParseFechaHoraLocal(string texto, out DateTime fecha)
        {
            if (DateTime.TryParseExact(
                texto, FormatoFechaHoraLocal, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            {
                return true;
            }

            return DateTime.TryParse(texto, CultureInfo.CurrentCulture, DateTimeStyles.None, out fecha);
        }

        private static string TextoPublicoObjetivo(string publicoObjetivo)
        {
            switch (publicoObjetivo)
            {
                case "GestorEspacios":
                    return "Gestores de espacios";
                case "ExternoSolicitante":
                    return "Solicitantes";
                default:
                    return "Todos los usuarios";
            }
        }

        protected static string ClaseBadgeEstado(Encuesta encuesta)
        {
            if (encuesta == null)
            {
                return "admin-status-badge";
            }

            if (encuesta.Estado == "Activa" && !encuesta.EstaVencida)
            {
                return "admin-status-badge admin-status-badge-active";
            }

            return "admin-status-badge admin-status-badge-inactive";
        }

        protected static string TextoEstado(Encuesta encuesta)
        {
            if (encuesta == null)
            {
                return string.Empty;
            }

            if (encuesta.Estado == "Activa")
            {
                return encuesta.EstaVencida ? "Vencida" : "Activa";
            }

            return encuesta.Estado == "Cerrada" ? "Cerrada" : "Borrador";
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_ENCUESTAS"))
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
