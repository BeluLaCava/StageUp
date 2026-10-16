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
    //   1) Preguntas frecuentes activas en una lista simple, con un filtro
    //      opcional por tema (pasos 4 a 9, A2, A3).
    //   2) Asistente guiado por opciones, con contenido propio (A4 a A7,
    //      Scripts/asistente-ayuda.js + AsistenteAyuda.ashx).
    //   3) Solicitud de soporte (CU-003-002).
    public partial class CentroAyuda : Page
    {
        private readonly BLL_Faq _bllFaq = new BLL_Faq();
        private List<Faq> _contenido = new List<Faq>();

        protected int CategoriaElegida { get; private set; }

        protected string UrlSoporte
        {
            // CU-003-002 A1: el asistente deriva al formulario indicando el
            // origen (y la consulta escrita, que agrega el script).
            get { return ResolveUrl(GestorDeSesion.EstaAutenticado() ? "~/Soporte.aspx?nueva=1&origen=Asistente" : "~/IniciarSesion.aspx"); }
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

            int tema;
            CategoriaElegida = int.TryParse(Request.QueryString["tema"], NumberStyles.Integer, CultureInfo.InvariantCulture, out tema) ? tema : 0;

            // A1: si la ayuda no puede leerse se avisa (soporte sigue disponible).
            ResultadoOperacion<List<Faq>> contenido = _bllFaq.ObtenerContenidoPublico();
            if (!contenido.Exitoso)
            {
                pnlNoDisponible.Visible = true;
                litNoDisponible.Text = contenido.Mensaje;
                return;
            }

            _contenido = contenido.Valor;

            // Solo los temas que tienen preguntas activas (como filtro opcional).
            List<CategoriaAyuda> categorias = _bllFaq.ListarCategorias()
                .Where(c => c.Activa && _contenido.Any(f => f.IdCategoriaAyuda == c.IdCategoriaAyuda))
                .ToList();
            CategoriaAyuda elegida = _bllFaq.ListarCategorias().FirstOrDefault(c => c.Activa && c.IdCategoriaAyuda == CategoriaElegida);
            if (elegida == null)
            {
                CategoriaElegida = 0;
            }

            lnkTodas.CssClass = CategoriaElegida == 0 ? "is-current" : string.Empty;
            rptCategorias.DataSource = categorias;
            rptCategorias.DataBind();

            List<Faq> preguntas = CategoriaElegida == 0
                ? _contenido
                : _contenido.Where(f => f.IdCategoriaAyuda == CategoriaElegida).ToList();

            // A2: tema sin preguntas activas (o ninguna pregunta cargada).
            pnlCategoriaVacia.Visible = preguntas.Count == 0;
            litCategoriaVacia.Text = elegida == null ? "mostrar" : elegida.Nombre;

            rptFaq.DataSource = preguntas;
            rptFaq.DataBind();
        }
    }
}
