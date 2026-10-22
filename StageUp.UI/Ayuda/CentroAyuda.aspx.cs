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






    public partial class CentroAyuda : Page
    {
        private readonly BLL_Faq _bllFaq = new BLL_Faq();
        protected string UrlSoporte
        {


            get { return ResolveUrl(GestorDeSesion.EstaAutenticado() ? "~/Soporte.aspx?nueva=1&origen=Asistente" : "~/IniciarSesion.aspx"); }
        }



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



            if (GestorDeSesion.EstaAutenticado())
            {
                lnkContactarSoporte.NavigateUrl = "~/Soporte.aspx?nueva=1&origen=CentroAyuda";
                lnkContactarSoporte.Text = "Registrar solicitud de soporte";
                lnkMisSolicitudes.Visible = true;
            }




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
