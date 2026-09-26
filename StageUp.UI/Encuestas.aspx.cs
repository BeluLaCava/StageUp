using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // Encuestas dinámicas con fecha de vencimiento y gráfico de resultados
    // al instante, del lado del usuario externo. Tres vistas en una sola
    // página, según query string:
    //   Encuestas.aspx                 -> pendientes por responder + resultados disponibles
    //   Encuestas.aspx?responder={id}  -> formulario para responder
    //   Encuestas.aspx?ver={id}        -> resultados en barras, al instante
    public partial class Encuestas : Page
    {
        private readonly BLL_Encuesta _bllEncuesta = new BLL_Encuesta();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (IsPostBack)
            {
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            string perfilUsuario = GestorDeSesion.ObtenerPerfilActual();

            int idResponder;
            int idVer;

            if (int.TryParse(Request.QueryString["responder"], out idResponder))
            {
                CargarResponder(idResponder, idUsuarioExterno, perfilUsuario);
            }
            else if (int.TryParse(Request.QueryString["ver"], out idVer))
            {
                CargarResultadosPublico(idVer, idUsuarioExterno, perfilUsuario);
            }
            else
            {
                CargarDashboard(idUsuarioExterno, perfilUsuario);
            }
        }

        protected void btnEnviarRespuestas_Click(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            int idEncuesta;
            if (!int.TryParse(Request.QueryString["responder"], out idEncuesta))
            {
                Response.Redirect("~/Encuestas.aspx");
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            string perfilUsuario = GestorDeSesion.ObtenerPerfilActual();

            ResultadoOperacion<EncuestaCompleta> completa = _bllEncuesta.ObtenerParaResponder(idEncuesta, idUsuarioExterno, perfilUsuario);
            if (!completa.Exitoso)
            {
                MostrarMensaje(completa.Mensaje, true);
                CargarDashboard(idUsuarioExterno, perfilUsuario);
                return;
            }

            Dictionary<int, int> respuestasPorPregunta = new Dictionary<int, int>();
            foreach (PreguntaEncuesta pregunta in completa.Valor.Preguntas)
            {
                string valorSeleccionado = Request.Form["pregunta_" + pregunta.IdPreguntaEncuesta];
                int idOpcionSeleccionada;
                if (!string.IsNullOrEmpty(valorSeleccionado) && int.TryParse(valorSeleccionado, out idOpcionSeleccionada))
                {
                    respuestasPorPregunta[pregunta.IdPreguntaEncuesta] = idOpcionSeleccionada;
                }
            }

            ResultadoOperacion resultado = _bllEncuesta.RegistrarRespuesta(idEncuesta, idUsuarioExterno, perfilUsuario, respuestasPorPregunta);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarResponder(idEncuesta, idUsuarioExterno, perfilUsuario);
                return;
            }

            Response.Redirect("~/Encuestas.aspx?ver=" + idEncuesta);
        }

        protected void rptPreguntasResponder_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            Repeater rptOpciones = (Repeater)e.Item.FindControl("rptOpcionesResponder");
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

        private void CargarDashboard(int idUsuarioExterno, string perfilUsuario)
        {
            MostrarSolo(pnlDashboard);

            List<Encuesta> pendientes = _bllEncuesta.ListarPendientesParaUsuario(idUsuarioExterno, perfilUsuario);
            pnlSinPendientes.Visible = pendientes.Count == 0;
            rptPendientes.DataSource = pendientes;
            rptPendientes.DataBind();

            List<Encuesta> disponibles = _bllEncuesta.ListarConResultadosParaUsuario(idUsuarioExterno, perfilUsuario);
            pnlSinDisponibles.Visible = disponibles.Count == 0;
            rptDisponibles.DataSource = disponibles;
            rptDisponibles.DataBind();
        }

        private void CargarResponder(int idEncuesta, int idUsuarioExterno, string perfilUsuario)
        {
            ResultadoOperacion<EncuestaCompleta> resultado = _bllEncuesta.ObtenerParaResponder(idEncuesta, idUsuarioExterno, perfilUsuario);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarDashboard(idUsuarioExterno, perfilUsuario);
                return;
            }

            MostrarSolo(pnlResponder);
            litTituloResponder.Text = Server.HtmlEncode(resultado.Valor.Encuesta.Titulo);
            litDescripcionResponder.Text = string.IsNullOrEmpty(resultado.Valor.Encuesta.Descripcion)
                ? string.Empty
                : Server.HtmlEncode(resultado.Valor.Encuesta.Descripcion);

            rptPreguntasResponder.DataSource = resultado.Valor.Preguntas;
            rptPreguntasResponder.DataBind();
        }

        private void CargarResultadosPublico(int idEncuesta, int idUsuarioExterno, string perfilUsuario)
        {
            ResultadoOperacion<List<ResultadoPreguntaEncuesta>> resultado =
                _bllEncuesta.ObtenerResultadosParaUsuario(idEncuesta, idUsuarioExterno, perfilUsuario);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarDashboard(idUsuarioExterno, perfilUsuario);
                return;
            }

            Encuesta encuesta = _bllEncuesta.ObtenerPorId(idEncuesta);

            MostrarSolo(pnlVerResultados);
            litTituloResultados.Text = encuesta == null ? string.Empty : Server.HtmlEncode(encuesta.Titulo);
            litDescripcionResultados.Text = encuesta == null || string.IsNullOrEmpty(encuesta.Descripcion)
                ? string.Empty
                : Server.HtmlEncode(encuesta.Descripcion);

            List<ResultadoPreguntaEncuesta> resultados = resultado.Valor;
            int total = resultados.Count == 0 ? 0 : resultados[0].TotalRespuestas;
            litTotalRespuestas.Text = total == 0
                ? "Todavía no hay respuestas."
                : "Total de personas que respondieron: " + total.ToString(CultureInfo.InvariantCulture);

            rptResultados.DataSource = resultados;
            rptResultados.DataBind();
        }

        private void MostrarSolo(Panel panelVisible)
        {
            pnlDashboard.Visible = ReferenceEquals(panelVisible, pnlDashboard);
            pnlResponder.Visible = ReferenceEquals(panelVisible, pnlResponder);
            pnlVerResultados.Visible = ReferenceEquals(panelVisible, pnlVerResultados);
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
