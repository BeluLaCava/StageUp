using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // Ítem 1 del profesor: el gestor ve las preguntas públicas sobre sus
    // espacios, las responde y, si hace falta, las oculta con un motivo.
    //   PreguntasRecibidas.aspx[?espacio=ID]
    public partial class PreguntasRecibidas : Page
    {
        private readonly BLL_PreguntaEspacio _bll = new BLL_PreguntaEspacio();

        protected int PreguntaOcultando
        {
            get { return ViewState["PreguntaOcultando"] as int? ?? 0; }
            set { ViewState["PreguntaOcultando"] = value; }
        }

        private int IdGestor
        {
            get { return GestorDeSesion.ObtenerIdUsuarioActual().Value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (GestorDeSesion.ObtenerPerfilActual() != PerfilUsuarioExterno.GestorEspacios.ToString())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            if (IsPostBack)
            {
                return;
            }

            ddlEstado.Items.Add(new ListItem("Sin responder", PreguntaEspacio.EstadoPendiente));
            ddlEstado.Items.Add(new ListItem("Respondidas", PreguntaEspacio.EstadoRespondida));
            ddlEstado.Items.Add(new ListItem("Ocultas", PreguntaEspacio.EstadoOculta));
            ddlEstado.Items.Add(new ListItem("Todas", string.Empty));

            // Espacios del gestor que tienen preguntas.
            ddlEspacio.Items.Add(new ListItem("Todos", string.Empty));
            ResultadoOperacion<List<PreguntaEspacio>> todas = _bll.ListarParaGestor(IdGestor, null, null);
            if (todas.Exitoso)
            {
                foreach (PreguntaEspacio espacio in todas.Valor.GroupBy(p => p.IdEspacioArtistico).Select(g => g.First()).OrderBy(p => p.NombreEspacio))
                {
                    ddlEspacio.Items.Add(new ListItem(espacio.NombreEspacio, espacio.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture)));
                }

                // Desde una notificación: ese espacio y, si no tiene pendientes, todas.
                string idEspacio = Request.QueryString["espacio"];
                if (!string.IsNullOrEmpty(idEspacio) && ddlEspacio.Items.FindByValue(idEspacio) != null)
                {
                    ddlEspacio.SelectedValue = idEspacio;
                }

                if (!todas.Valor.Any(p => p.EstaPendiente))
                {
                    ddlEstado.SelectedValue = string.Empty;
                }
            }

            Cargar();
        }

        protected void Filtrar(object sender, EventArgs e)
        {
            PreguntaOcultando = 0;
            pnlMensaje.Visible = false;
            Cargar();
        }

        protected void rptPreguntas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idPregunta;
            int.TryParse(Convert.ToString(e.CommandArgument), NumberStyles.Integer, CultureInfo.InvariantCulture, out idPregunta);
            ResultadoOperacion resultado = null;

            switch (e.CommandName)
            {
                case "Responder":
                    resultado = _bll.Responder(idPregunta, IdGestor, ((TextBox)e.Item.FindControl("txtRespuesta")).Text);
                    break;
                case "Ocultar":
                    PreguntaOcultando = idPregunta;
                    break;
                case "CancelarOcultar":
                    PreguntaOcultando = 0;
                    break;
                case "ConfirmarOcultar":
                    resultado = _bll.Ocultar(idPregunta, IdGestor, ((TextBox)e.Item.FindControl("txtMotivo")).Text);
                    if (resultado.Exitoso)
                    {
                        PreguntaOcultando = 0;
                    }
                    break;
            }

            string escrito = e.CommandName == "Responder" && resultado != null && !resultado.Exitoso
                ? ((TextBox)e.Item.FindControl("txtRespuesta")).Text
                : null;

            if (resultado != null)
            {
                MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            }

            Cargar();

            // Si la respuesta no se pudo guardar, se conserva lo escrito.
            if (escrito != null)
            {
                foreach (RepeaterItem item in rptPreguntas.Items)
                {
                    Button boton = (Button)item.FindControl("btnResponder");
                    if (boton != null && Convert.ToString(boton.CommandArgument) == idPregunta.ToString(CultureInfo.InvariantCulture))
                    {
                        ((TextBox)item.FindControl("txtRespuesta")).Text = escrito;
                    }
                }
            }
        }

        private void Cargar()
        {
            int idEspacio;
            int? espacio = int.TryParse(ddlEspacio.SelectedValue, out idEspacio) ? idEspacio : (int?)null;
            string estado = string.IsNullOrEmpty(ddlEstado.SelectedValue) ? null : ddlEstado.SelectedValue;

            ResultadoOperacion<List<PreguntaEspacio>> resultado = _bll.ListarParaGestor(IdGestor, estado, espacio);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                rptPreguntas.Visible = false;
                return;
            }

            List<PreguntaEspacio> preguntas = resultado.Valor;
            rptPreguntas.Visible = preguntas.Count > 0;
            rptPreguntas.DataSource = preguntas;
            rptPreguntas.DataBind();

            litCantidad.Text = preguntas.Count == 1 ? "1 pregunta" : preguntas.Count + " preguntas";
            pnlVacio.Visible = preguntas.Count == 0;
            bool sinNinguna = ddlEspacio.Items.Count <= 1;
            litVacioTitulo.Text = sinNinguna ? "Todavía no te hicieron preguntas" : estado == PreguntaEspacio.EstadoPendiente ? "No tenés preguntas sin responder" : "No hay preguntas para este filtro";
            litVacioTexto.Text = sinNinguna
                ? "Cuando alguien pregunte sobre uno de tus espacios publicados, te avisamos y la vas a ver acá."
                : "Cambiá el filtro para ver las demás preguntas.";
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        protected static string TextoEstado(object estado)
        {
            switch (Convert.ToString(estado))
            {
                case PreguntaEspacio.EstadoPendiente: return "Sin responder";
                case PreguntaEspacio.EstadoRespondida: return "Respondida";
                case PreguntaEspacio.EstadoOculta: return "Oculta";
                default: return Convert.ToString(estado);
            }
        }
    }
}
