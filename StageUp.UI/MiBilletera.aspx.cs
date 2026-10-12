using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // Ítem 8: billetera de tarjetas del usuario (alias, preferida, alta y
    // baja). El código de seguridad nunca se pide acá.
    public partial class MiBilletera : Page
    {
        private readonly BLL_Billetera _bll = new BLL_Billetera();

        // Tarjeta con el alias en edición o con la baja por confirmar.
        private int IdEnEdicion
        {
            get { return ViewState["IdEnEdicion"] as int? ?? 0; }
            set { ViewState["IdEnEdicion"] = value; }
        }

        private string AccionEnCurso
        {
            get { return ViewState["AccionEnCurso"] as string; }
            set { ViewState["AccionEnCurso"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarVencimientos();
                CargarTarjetas();
            }
        }

        protected void btnAgregar_Click(object sender, EventArgs e)
        {
            int mes;
            int anio;
            int.TryParse(ddlMes.SelectedValue, out mes);
            int.TryParse(ddlAnio.SelectedValue, out anio);
            ResultadoOperacion<TarjetaGuardada> resultado = _bll.Guardar(IdUsuario(), new DatosTarjeta
            {
                Titular = txtTitular.Text,
                Numero = txtNumero.Text,
                MesVencimiento = mes,
                AnioVencimiento = anio
            }, txtAliasNueva.Text, chkPreferida.Checked);

            // El número no se vuelve a mostrar en la página.
            txtNumero.Text = string.Empty;
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                txtTitular.Text = string.Empty;
                txtAliasNueva.Text = string.Empty;
                chkPreferida.Checked = false;
                ddlMes.SelectedIndex = 0;
                ddlAnio.SelectedIndex = 0;
            }

            CargarTarjetas();
        }

        protected void rptTarjetas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            int.TryParse(Convert.ToString(e.CommandArgument), NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
            ResultadoOperacion resultado = null;

            switch (e.CommandName)
            {
                case "Preferida":
                    resultado = _bll.MarcarPreferida(IdUsuario(), id);
                    break;
                case "EditarAlias":
                case "PedirEliminar":
                    IdEnEdicion = id;
                    AccionEnCurso = e.CommandName;
                    pnlMensaje.Visible = false;
                    break;
                case "GuardarAlias":
                    TextBox txtAlias = (TextBox)e.Item.FindControl("txtAlias");
                    resultado = _bll.CambiarAlias(IdUsuario(), id, txtAlias.Text);
                    if (!resultado.Exitoso)
                    {
                        // Se queda en edición para corregirlo.
                        MostrarMensaje(resultado.Mensaje, true);
                        return;
                    }
                    break;
                case "Eliminar":
                    resultado = _bll.Eliminar(IdUsuario(), id);
                    break;
                case "Cancelar":
                    pnlMensaje.Visible = false;
                    break;
            }

            if (e.CommandName != "EditarAlias" && e.CommandName != "PedirEliminar")
            {
                IdEnEdicion = 0;
                AccionEnCurso = null;
            }

            if (resultado != null)
            {
                MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            }

            CargarTarjetas();
        }

        protected void rptTarjetas_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            TarjetaGuardada tarjeta = (TarjetaGuardada)e.Item.DataItem;
            bool enEdicion = tarjeta.IdTarjetaGuardada == IdEnEdicion;

            Panel pnlTarjeta = (Panel)e.Item.FindControl("pnlTarjeta");
            pnlTarjeta.CssClass = "billetera-tarjeta" + (tarjeta.Preferida ? " es-preferida" : string.Empty);

            List<string> chips = new List<string>();
            if (tarjeta.Preferida) chips.Add("<span class=\"billetera-chip\">Preferida</span>");
            if (tarjeta.EstaVencida) chips.Add("<span class=\"billetera-chip billetera-chip-vencida\">Vencida</span>");
            ((Literal)e.Item.FindControl("litChips")).Text = string.Join(string.Empty, chips);

            ((Literal)e.Item.FindControl("litDetalle")).Text =
                "Vence " + tarjeta.MesVencimiento.ToString("00", CultureInfo.InvariantCulture) + "/" + tarjeta.AnioVencimiento.ToString(CultureInfo.InvariantCulture) +
                " · Guardada el " + tarjeta.FechaAlta.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) +
                (tarjeta.FechaUltimoUso.HasValue ? " · Último uso: " + tarjeta.FechaUltimoUso.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : " · Sin usar todavía");

            e.Item.FindControl("lnkPreferida").Visible = !tarjeta.Preferida && !tarjeta.EstaVencida;
            e.Item.FindControl("pnlAcciones").Visible = !enEdicion;
            e.Item.FindControl("pnlAlias").Visible = enEdicion && AccionEnCurso == "EditarAlias";
            e.Item.FindControl("pnlConfirmar").Visible = enEdicion && AccionEnCurso == "PedirEliminar";
        }

        private void CargarTarjetas()
        {
            List<TarjetaGuardada> tarjetas = _bll.Listar(IdUsuario());
            rptTarjetas.DataSource = tarjetas;
            rptTarjetas.DataBind();
            pnlSinTarjetas.Visible = tarjetas.Count == 0;

            bool hayLugar = tarjetas.Count < BLL_Billetera.MaximoTarjetas;
            pnlAgregar.Visible = hayLugar;
            pnlLimite.Visible = !hayLugar;
            litLimite.Text = "Ya tenés " + BLL_Billetera.MaximoTarjetas + " tarjetas guardadas, el máximo. Eliminá alguna para agregar otra.";
        }

        private void CargarVencimientos()
        {
            ddlMes.Items.Clear();
            ddlMes.Items.Add(new ListItem("Mes", string.Empty));
            for (int mes = 1; mes <= 12; mes++)
            {
                ddlMes.Items.Add(new ListItem(mes.ToString("00", CultureInfo.InvariantCulture), mes.ToString(CultureInfo.InvariantCulture)));
            }

            ddlAnio.Items.Clear();
            ddlAnio.Items.Add(new ListItem("Año", string.Empty));
            for (int anio = DateTime.Today.Year; anio <= DateTime.Today.Year + 15; anio++)
            {
                string texto = anio.ToString(CultureInfo.InvariantCulture);
                ddlAnio.Items.Add(new ListItem(texto, texto));
            }
        }

        private static int IdUsuario()
        {
            return GestorDeSesion.ObtenerIdUsuarioActual().Value;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
