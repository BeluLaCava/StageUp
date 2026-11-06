using System;
using System.Globalization;
using System.Web.UI;
using StageUp.BLL;
using StageUp.Seguridad;
namespace StageUp.UI.Interno
{
    public partial class HistorialCorreos : Page
    {
        private bool PuedeConsultar()
        {
            return GestorDeSesion.EstaAutenticadoComoInterno() && GestorDeSesion.TienePermisoInterno(BLL_CorreoAdministrativo.PermisoConsultar);
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PuedeConsultar()) { Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado"); return; }
            if (!IsPostBack) Cargar();
        }
        protected void Filtrar(object sender, EventArgs e) { if (PuedeConsultar()) Cargar(); }
        private void Cargar()
        {
            pnlMensaje.Visible = false;
            DateTime d, h; int id;
            DateTime? desde = null, hasta = null; int? reserva = null;
            if (!string.IsNullOrWhiteSpace(txtDesde.Text))
            {
                if (!DateTime.TryParseExact(txtDesde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) { MostrarError("Ingresá una fecha desde válida."); return; }
                desde = d;
            }
            if (!string.IsNullOrWhiteSpace(txtHasta.Text))
            {
                if (!DateTime.TryParseExact(txtHasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out h)) { MostrarError("Ingresá una fecha hasta válida."); return; }
                hasta = h;
            }
            if (!string.IsNullOrWhiteSpace(txtReserva.Text))
            {
                if (!int.TryParse(txtReserva.Text, out id) || id <= 0) { MostrarError("Ingresá un número de reserva válido."); return; }
                reserva = id;
            }
            var resultado = new BLL_CorreoAdministrativo().Listar(ddlEstado.SelectedValue, desde, hasta, reserva, txtDestinatario.Text);
            if (!resultado.Exitoso) { MostrarError(resultado.Mensaje); return; }
            grilla.DataSource = resultado.Valor; grilla.DataBind();
        }
        private void MostrarError(string mensaje) { pnlMensaje.Visible = true; litMensaje.Text = mensaje; grilla.DataSource = null; grilla.DataBind(); }
    }
}
