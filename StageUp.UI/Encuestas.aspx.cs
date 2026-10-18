using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Configuration;
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
    // Sin iniciar sesión se pueden responder las encuestas para "Todos": el
    // navegador guarda una cookie con un identificador al azar para que no se
    // responda dos veces, y se pide el CAPTCHA. La cookie se crea al abrir la
    // página (antes de enviar) y en el envío se exige que el navegador la
    // devuelva: así dos envíos simultáneos llevan la misma identidad y un
    // navegador que bloquea cookies no puede responder como visitante. Con
    // sesión, la respuesta también guarda el navegador, para que después no
    // se pueda volver a responder desde ahí sin sesión.
    public partial class Encuestas : Page
    {
        private const string NombreCookieEncuestado = "StageUp_Encuestado";

        private readonly BLL_Encuesta _bllEncuesta = new BLL_Encuesta();
        protected bool UsuarioAutenticado { get; private set; }

        protected string ClaveSitioRecaptcha
        {
            get
            {
                string claveEntorno = Environment.GetEnvironmentVariable("STAGEUP_RECAPTCHA_SITE_KEY");
                string clave = string.IsNullOrWhiteSpace(claveEntorno)
                    ? WebConfigurationManager.AppSettings["RecaptchaSiteKey"]
                    : claveEntorno;
                return HttpUtility.HtmlAttributeEncode(clave ?? string.Empty);
            }
        }

        protected string CodigoIdiomaRecaptcha
        {
            get { return HttpUtility.UrlEncode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName); }
        }

        private string IdentificadorNavegador
        {
            get
            {
                HttpCookie cookie = Request.Cookies[NombreCookieEncuestado];
                return cookie == null ? null : cookie.Value;
            }
        }

        private Encuestado EncuestadoActual(string identificadorNavegador)
        {
            return UsuarioAutenticado
                ? BLL_Encuesta.EncuestadoConSesion(GestorDeSesion.ObtenerIdUsuarioActual().Value, GestorDeSesion.ObtenerPerfilActual(), identificadorNavegador)
                : BLL_Encuesta.EncuestadoSinSesion(identificadorNavegador);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            UsuarioAutenticado = GestorDeSesion.EstaAutenticado();

            if (IsPostBack)
            {
                return;
            }

            Encuestado encuestado = EncuestadoActual(AsegurarIdentificadorNavegador());

            int idResponder;
            int idVer;

            if (int.TryParse(Request.QueryString["responder"], out idResponder))
            {
                CargarResponder(idResponder, encuestado);
            }
            else if (int.TryParse(Request.QueryString["ver"], out idVer))
            {
                CargarResultadosPublico(idVer, encuestado);
            }
            else
            {
                CargarDashboard(encuestado);
            }
        }

        protected string ObtenerUrlRespuesta(object idEncuesta)
        {
            return ResolveUrl("~/Encuestas.aspx?responder=" + Convert.ToString(idEncuesta, CultureInfo.InvariantCulture));
        }

        protected string ObtenerTextoRespuesta()
        {
            return "Responder";
        }

        protected void btnEnviarRespuestas_Click(object sender, EventArgs e)
        {
            int idEncuesta;
            if (!int.TryParse(Request.QueryString["responder"], out idEncuesta))
            {
                Response.Redirect("~/Encuestas.aspx");
                return;
            }

            // La identidad del navegador se estableció al abrir la encuesta:
            // si el navegador no devuelve la cookie (bloqueada o borrada), sin
            // sesión no se acepta la respuesta.
            string identificadorNavegador = EsIdentificadorValido(IdentificadorNavegador) ? IdentificadorNavegador : null;
            if (!UsuarioAutenticado && identificadorNavegador == null)
            {
                AsegurarIdentificadorNavegador();
                MostrarMensaje("No pudimos comprobar este navegador: para responder sin iniciar sesión, tu navegador tiene que aceptar cookies de StageUp. " +
                    "Habilitalas y volvé a enviar la encuesta, o iniciá sesión.", true);
                CargarResponder(idEncuesta, EncuestadoActual(null));
                return;
            }

            Encuestado encuestado = EncuestadoActual(identificadorNavegador);

            ResultadoOperacion<EncuestaCompleta> completa = _bllEncuesta.ObtenerParaResponder(idEncuesta, encuestado);
            if (!completa.Exitoso)
            {
                MostrarMensaje(completa.Mensaje, true);
                CargarDashboard(encuestado);
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

            ResultadoOperacion resultado = _bllEncuesta.RegistrarRespuesta(idEncuesta, encuestado, respuestasPorPregunta, Request.Form["g-recaptcha-response"]);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarResponder(idEncuesta, encuestado);
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

            PreguntaEncuesta pregunta = e.Item.DataItem as PreguntaEncuesta;
            Repeater rptOpciones = (Repeater)e.Item.FindControl("rptOpcionesResponder");
            rptOpciones.DataSource = pregunta == null ? new List<OpcionPregunta>() : pregunta.Opciones;
            rptOpciones.DataBind();
        }

        protected void rptResultados_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            ResultadoPreguntaEncuesta resultado = e.Item.DataItem as ResultadoPreguntaEncuesta;
            Repeater rptOpciones = (Repeater)e.Item.FindControl("rptOpcionesResultado");
            rptOpciones.DataSource = resultado == null ? new List<ResultadoOpcionEncuesta>() : resultado.Opciones;
            rptOpciones.DataBind();
        }

        private static bool EsIdentificadorValido(string identificadorNavegador)
        {
            return BLL_Encuesta.EncuestadoSinSesion(identificadorNavegador).IdentificadorAnonimo != null;
        }

        // Devuelve el identificador del navegador; si no tiene uno válido,
        // genera uno nuevo y lo envía en la cookie en esta misma respuesta.
        private string AsegurarIdentificadorNavegador()
        {
            string actual = IdentificadorNavegador;
            if (EsIdentificadorValido(actual))
            {
                return actual;
            }

            string nuevo = BLL_Encuesta.GenerarIdentificadorNavegador();
            GuardarCookieEncuestado(nuevo);
            return nuevo;
        }

        // Solo guarda un identificador al azar (sin datos personales). Dura
        // un año, viaja solo por HTTPS cuando el sitio usa HTTPS y no es
        // accesible desde JavaScript.
        private void GuardarCookieEncuestado(string identificadorNavegador)
        {
            HttpCookie cookie = new HttpCookie(NombreCookieEncuestado, identificadorNavegador)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Path = "/",
                Expires = DateTime.Now.AddYears(1)
            };
            Response.Cookies.Add(cookie);
        }

        private void CargarDashboard(Encuestado encuestado)
        {
            MostrarSolo(pnlDashboard);
            pnlAvisoSinSesion.Visible = encuestado.EsAnonimo;

            List<Encuesta> pendientes = _bllEncuesta.ListarPendientesParaUsuario(encuestado);
            pnlSinPendientes.Visible = pendientes.Count == 0;
            rptPendientes.DataSource = pendientes;
            rptPendientes.DataBind();

            List<Encuesta> disponibles = _bllEncuesta.ListarConResultadosParaUsuario(encuestado);
            pnlSinDisponibles.Visible = disponibles.Count == 0;
            rptDisponibles.DataSource = disponibles;
            rptDisponibles.DataBind();
        }

        private void CargarResponder(int idEncuesta, Encuestado encuestado)
        {
            ResultadoOperacion<EncuestaCompleta> resultado = _bllEncuesta.ObtenerParaResponder(idEncuesta, encuestado);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarDashboard(encuestado);
                return;
            }

            MostrarSolo(pnlResponder);
            pnlResponderSinSesion.Visible = encuestado.EsAnonimo;
            phScriptRecaptcha.Visible = encuestado.EsAnonimo;
            litTituloResponder.Text = Server.HtmlEncode(resultado.Valor.Encuesta.Titulo);
            litDescripcionResponder.Text = string.IsNullOrEmpty(resultado.Valor.Encuesta.Descripcion)
                ? string.Empty
                : Server.HtmlEncode(resultado.Valor.Encuesta.Descripcion);

            rptPreguntasResponder.DataSource = resultado.Valor.Preguntas;
            rptPreguntasResponder.DataBind();
        }

        private void CargarResultadosPublico(int idEncuesta, Encuestado encuestado)
        {
            ResultadoOperacion<List<ResultadoPreguntaEncuesta>> resultado =
                _bllEncuesta.ObtenerResultadosParaUsuario(idEncuesta, encuestado);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarDashboard(encuestado);
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
