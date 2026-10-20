using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Ayuda
{
    // CU-003-001 Consultar ayuda y asistencia automatizada.
    //   1) Preguntas frecuentes básicas en una lista breve, sin filtros
    //      (pasos 4 a 9, A2, A3). Las categorías se usan solo internamente.
    //   2) Asistente guiado por opciones, con contenido propio (A4 a A7,
    //      Scripts/asistente-ayuda.js + AsistenteAyuda.ashx).
    //   3) Solicitud de soporte (CU-003-002).
    public partial class CentroAyuda : Page
    {
        private readonly BLL_Faq _bllFaq = new BLL_Faq();
        protected string UrlSoporte
        {
            // CU-003-002 A1: el asistente deriva al formulario indicando el
            // origen (y la consulta escrita, que agrega el script).
            get { return ResolveUrl(GestorDeSesion.EstaAutenticado() ? "~/Soporte.aspx?nueva=1&origen=Asistente" : "~/IniciarSesion.aspx"); }
        }

        // Primer nombre del usuario con sesión, para que el asistente lo
        // salude ("¡Hola, Belén!"). Vacío si es un visitante.
        protected string NombreUsuario
        {
            get
            {
                string nombre = GestorDeSesion.EstaAutenticado() ? GestorDeSesion.ObtenerNombreCompletoActual() : null;
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    return string.Empty;
                }

                return nombre.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            // Un usuario logueado va directo al formulario (CU-003-002 A1) y
            // puede seguir sus solicitudes (A5).
            if (GestorDeSesion.EstaAutenticado())
            {
                lnkContactarSoporte.NavigateUrl = "~/Soporte.aspx?nueva=1&origen=CentroAyuda";
                lnkContactarSoporte.Text = "Registrar solicitud de soporte";
                lnkMisSolicitudes.Visible = true;
            }

            // A1: si la ayuda no puede leerse se avisa (soporte sigue disponible).
            // Solo la selección breve de preguntas básicas, sin filtros: lo
            // específico lo resuelve el asistente.
            ResultadoOperacion<List<Faq>> basicas = _bllFaq.ObtenerPreguntasBasicas();
            if (!basicas.Exitoso)
            {
                pnlNoDisponible.Visible = true;
                litNoDisponible.Text = basicas.Mensaje;
                return;
            }

            pnlSinBasicas.Visible = basicas.Valor.Count == 0;
            rptFaq.DataSource = basicas.Valor;
            rptFaq.DataBind();
            MostrarPreguntaBuscada();
        }

        // ?faq={id}: la pregunta que se eligió en los resultados de la
        // búsqueda, con su respuesta. La lista sigue siendo la breve.
        private void MostrarPreguntaBuscada()
        {
            int idFaq;
            if (!int.TryParse(Request.QueryString["faq"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idFaq))
            {
                return;
            }

            ResultadoOperacion<Faq> resultado = _bllFaq.ObtenerPublicada(idFaq);
            if (!resultado.Exitoso || resultado.Valor == null)
            {
                pnlFaqBuscadaNoDisponible.Visible = true;
                return;
            }

            pnlFaqBuscada.Visible = true;
            litFaqBuscadaPregunta.Text = resultado.Valor.Pregunta;
            litFaqBuscadaRespuesta.Text = resultado.Valor.Respuesta;
        }
    }
}
