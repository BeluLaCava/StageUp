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
    // CU-003-001 Consultar ayuda y asistencia automatizada: preguntas
    // frecuentes activas por categoría (pasos 4 a 9, A2, A3), asistente
    // (A4 a A7, Scripts/asistente-ayuda.js + AsistenteAyuda.ashx) y acceso
    // a soporte.
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
            List<CategoriaAyuda> categorias = _bllFaq.ListarCategorias().Where(c => c.Activa).ToList();
            if (!categorias.Any(c => c.IdCategoriaAyuda == CategoriaElegida))
            {
                CategoriaElegida = 0;
            }

            lnkTodas.CssClass = CategoriaElegida == 0 ? "is-current" : string.Empty;
            rptCategorias.DataSource = categorias;
            rptCategorias.DataBind();

            List<CategoriaAyuda> grupos = categorias
                .Where(c => CategoriaElegida == 0 ? _contenido.Any(f => f.IdCategoriaAyuda == c.IdCategoriaAyuda) : c.IdCategoriaAyuda == CategoriaElegida)
                .ToList();

            CategoriaAyuda elegida = categorias.FirstOrDefault(c => c.IdCategoriaAyuda == CategoriaElegida);
            bool vacia = elegida != null && !_contenido.Any(f => f.IdCategoriaAyuda == CategoriaElegida);
            pnlCategoriaVacia.Visible = vacia || (CategoriaElegida == 0 && _contenido.Count == 0);
            litCategoriaVacia.Text = elegida == null ? "ningún tema" : elegida.Nombre;

            rptGrupos.DataSource = vacia ? new List<CategoriaAyuda>() : grupos;
            rptGrupos.DataBind();
        }

        protected void rptGrupos_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            CategoriaAyuda categoria = (CategoriaAyuda)e.Item.DataItem;
            Repeater rptFaq = (Repeater)e.Item.FindControl("rptFaq");
            rptFaq.DataSource = _contenido.Where(f => f.IdCategoriaAyuda == categoria.IdCategoriaAyuda).ToList();
            rptFaq.DataBind();
        }
    }
}
