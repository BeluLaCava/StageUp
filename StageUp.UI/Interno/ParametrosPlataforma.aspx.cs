using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // CU-002-004 Configurar parámetros administrativos de la plataforma
    // (permiso CONFIGURAR_PARAMETROS). Primero se consultan (A2); "Editar
    // parámetros" habilita la edición, "Guardar cambios" valida y guarda con
    // la vigencia elegida y "Cancelar" descarta lo no guardado (A8).
    public partial class ParametrosPlataforma : Page
    {
        private const string PermisoRequerido = "CONFIGURAR_PARAMETROS";

        private readonly BLL_ParametroPlataforma _bllParametros = new BLL_ParametroPlataforma();
        private List<ParametroPlataforma> _parametros = new List<ParametroPlataforma>();

        private bool Editando
        {
            get { return ViewState["Editando"] is bool && (bool)ViewState["Editando"]; }
            set { ViewState["Editando"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarFiltroHistorial();
                CargarParametros();
                CargarHistorial();
            }
        }

        protected void btnEditar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            pnlMensaje.Visible = false;
            Editando = true;
            CargarParametros();
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            // A8: se descarta lo no guardado y sigue la configuración vigente.
            Editando = false;
            CargarParametros();
            MostrarMensaje("Se descartaron los cambios sin guardar. Sigue vigente la configuración anterior.", false);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !Editando)
            {
                return;
            }

            Dictionary<string, string> valores = new Dictionary<string, string>();
            foreach (RepeaterItem grupo in rptGrupos.Items)
            {
                Repeater rptParametros = (Repeater)grupo.FindControl("rptParametros");
                foreach (RepeaterItem item in rptParametros.Items)
                {
                    string clave = ((HiddenField)item.FindControl("hfClave")).Value;
                    valores[clave] = BLL_ParametroPlataforma.ObtenerTipo(clave) == BLL_ParametroPlataforma.TipoParametro.SiNo
                        ? ((DropDownList)item.FindControl("ddlSiNo")).SelectedValue
                        : ((TextBox)item.FindControl("txtValor")).Text;
                }
            }

            DateTime? vigencia = null;
            if (rblVigencia.SelectedValue == "Fecha")
            {
                DateTime fecha;
                if (!DateTime.TryParseExact(txtVigencia.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                {
                    MostrarMensaje("Elegí la fecha desde la que rigen los cambios.", true);
                    return;
                }

                vigencia = fecha;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllParametros.GuardarCambios(valores, vigencia, idResponsable);
            if (!resultado.Exitoso)
            {
                // A9 a A15: se queda en edición con lo que escribió el usuario.
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            // Pasos 15 y 16.
            Editando = false;
            rblVigencia.SelectedValue = "Ahora";
            txtVigencia.Text = string.Empty;
            CargarParametros();
            CargarHistorial();
            MostrarMensaje(string.IsNullOrEmpty(resultado.Mensaje) ? "No había cambios para guardar." : resultado.Mensaje, false);
        }

        protected void ddlHistorialParametro_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TieneAcceso())
            {
                CargarHistorial();
            }
        }

        protected void rptGrupos_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            BLL_ParametroPlataforma.GrupoParametros grupo = (BLL_ParametroPlataforma.GrupoParametros)e.Item.DataItem;
            Repeater rptParametros = (Repeater)e.Item.FindControl("rptParametros");
            rptParametros.DataSource = _parametros.Where(p => BLL_ParametroPlataforma.ObtenerGrupo(p.Clave) == grupo.Codigo).ToList();
            rptParametros.DataBind();
        }

        protected void rptParametros_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            ParametroPlataforma parametro = (ParametroPlataforma)e.Item.DataItem;
            BLL_ParametroPlataforma.TipoParametro tipo = BLL_ParametroPlataforma.ObtenerTipo(parametro.Clave);
            string nombre = BLL_ParametroPlataforma.ObtenerNombre(parametro.Clave);

            Literal litSubgrupo = (Literal)e.Item.FindControl("litSubgrupo");
            string subgrupo = BLL_ParametroPlataforma.ObtenerSubgrupo(parametro.Clave);
            litSubgrupo.Text = subgrupo == null ? string.Empty : "<span class=\"param-subgrupo\">" + HttpUtility.HtmlEncode(subgrupo) + "</span>";

            Literal litProgramado = (Literal)e.Item.FindControl("litProgramado");
            litProgramado.Text = parametro.FechaVigenciaProgramada.HasValue
                ? "<span class=\"param-programado\">Cambio programado: " +
                  HttpUtility.HtmlEncode(BLL_ParametroPlataforma.FormatearValor(parametro.Clave, parametro.ValorProgramado)) + " desde el " +
                  parametro.FechaVigenciaProgramada.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + "</span>"
                : string.Empty;

            Literal litValor = (Literal)e.Item.FindControl("litValor");
            TextBox txtValor = (TextBox)e.Item.FindControl("txtValor");
            DropDownList ddlSiNo = (DropDownList)e.Item.FindControl("ddlSiNo");
            Literal litUnidad = (Literal)e.Item.FindControl("litUnidad");

            litValor.Visible = !Editando;
            litValor.Text = "<span class=\"param-valor-texto\">" + HttpUtility.HtmlEncode(BLL_ParametroPlataforma.FormatearValor(parametro.Clave, parametro.Valor)) + "</span>";

            bool esSiNo = tipo == BLL_ParametroPlataforma.TipoParametro.SiNo;
            txtValor.Visible = Editando && !esSiNo;
            ddlSiNo.Visible = Editando && esSiNo;
            litUnidad.Visible = Editando;
            litUnidad.Text = BLL_ParametroPlataforma.ObtenerUnidad(parametro.Clave);

            txtValor.Text = parametro.Valor;
            txtValor.MaxLength = BLL_ParametroPlataforma.ObtenerLongitudMaxima(parametro.Clave);
            txtValor.Attributes["aria-label"] = nombre;
            if (tipo == BLL_ParametroPlataforma.TipoParametro.Numero)
            {
                txtValor.CssClass = "param-numero";
                txtValor.Attributes["inputmode"] = "decimal";
            }
            else if (tipo == BLL_ParametroPlataforma.TipoParametro.Correo)
            {
                txtValor.Attributes["inputmode"] = "email";
                txtValor.Attributes["placeholder"] = "Opcional";
            }
            else if (tipo == BLL_ParametroPlataforma.TipoParametro.Cuit)
            {
                txtValor.Attributes["placeholder"] = "00-00000000-0";
            }

            if (BLL_ParametroPlataforma.EsObligatorio(parametro.Clave) && !esSiNo)
            {
                txtValor.Attributes["aria-required"] = "true";
            }

            ddlSiNo.Attributes["aria-label"] = nombre;
            ddlSiNo.SelectedValue = parametro.Valor == BLL_ParametroPlataforma.ValorNo ? BLL_ParametroPlataforma.ValorNo : BLL_ParametroPlataforma.ValorSi;
        }

        protected static string DescribirEstadoCambio(string estado)
        {
            return estado == "Programado" ? "Programado" : estado == "Reemplazado" ? "Reemplazado" : "Aplicado";
        }

        private void CargarParametros()
        {
            _parametros = _bllParametros.Listar();
            rptGrupos.DataSource = BLL_ParametroPlataforma.ObtenerGrupos();
            rptGrupos.DataBind();

            btnEditar.Visible = !Editando && _parametros.Count > 0;
            pnlGuardar.Visible = Editando;
            litModo.Text = Editando
                ? "Modificá los parámetros que necesites, elegí desde cuándo rigen y guardá los cambios. Lo que no cambies queda igual."
                : _parametros.Count == 0
                    ? "No se pudieron leer los parámetros. Revisá que estén aplicados los scripts de la base."
                    : "Configuración vigente. Para modificarla, elegí \"Editar parámetros\".";
        }

        private void CargarFiltroHistorial()
        {
            ddlHistorialParametro.Items.Clear();
            ddlHistorialParametro.Items.Add(new ListItem("Todos", string.Empty));
            foreach (ParametroPlataforma parametro in _bllParametros.Listar())
            {
                ddlHistorialParametro.Items.Add(new ListItem(BLL_ParametroPlataforma.ObtenerNombre(parametro.Clave), parametro.Clave));
            }
        }

        private void CargarHistorial()
        {
            List<CambioParametroPlataforma> cambios = _bllParametros.ListarCambios(ddlHistorialParametro.SelectedValue);
            rptCambios.DataSource = cambios;
            rptCambios.DataBind();
            rptCambios.Visible = cambios.Count > 0;
            pnlSinCambios.Visible = cambios.Count == 0;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        // Paso 2 y A1.
        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
