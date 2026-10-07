using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // ABMC del menú dinámico (ítem 19 de la segunda entrega).
    public partial class GestionMenu : Page
    {
        private readonly BLL_OpcionMenu _bllOpcionMenu = new BLL_OpcionMenu();

        private int? IdOpcionEnEdicion
        {
            get { return ViewState["IdOpcionEnEdicion"] as int?; }
            set { ViewState["IdOpcionEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarPermisos();
                CargarOpciones();
            }
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !Page.IsValid)
            {
                return;
            }

            int idPermiso;
            int.TryParse(ddlPermiso.SelectedValue, out idPermiso);

            OpcionMenu opcion = new OpcionMenu
            {
                IdOpcionMenu = IdOpcionEnEdicion ?? 0,
                Texto = txtTexto.Text,
                Descripcion = txtDescripcion.Text,
                Url = txtUrl.Text,
                Modulo = txtModulo.Text,
                IdComponentePermiso = idPermiso,
                Activo = chkActiva.Checked
            };

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;
            if (IdOpcionEnEdicion.HasValue)
            {
                resultado = _bllOpcionMenu.Modificar(opcion, idResponsable);
            }
            else
            {
                ResultadoOperacion<int> alta = _bllOpcionMenu.Registrar(opcion, idResponsable);
                resultado = alta.Exitoso ? ResultadoOperacion.Ok(alta.Mensaje) : ResultadoOperacion.Error(alta.Mensaje);
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                LimpiarFormulario();
                CargarOpciones();
            }
        }

        protected void lnkCancelar_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void rptOpciones_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idOpcion = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = null;

            switch (e.CommandName)
            {
                case "Editar":
                    CargarParaEdicion(idOpcion);
                    return;
                case "Subir":
                    resultado = _bllOpcionMenu.Mover(idOpcion, true, idResponsable);
                    break;
                case "Bajar":
                    resultado = _bllOpcionMenu.Mover(idOpcion, false, idResponsable);
                    break;
                case "Baja":
                    resultado = _bllOpcionMenu.CambiarEstado(idOpcion, false, idResponsable);
                    break;
                case "Activar":
                    resultado = _bllOpcionMenu.CambiarEstado(idOpcion, true, idResponsable);
                    break;
            }

            if (resultado != null)
            {
                MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            }

            CargarOpciones();
        }

        private void CargarPermisos()
        {
            ddlPermiso.Items.Clear();
            ddlPermiso.Items.Add(new ListItem("Seleccioná un permiso", string.Empty));
            foreach (PermisoHoja hoja in _bllOpcionMenu.ListarPermisosDisponibles())
            {
                ddlPermiso.Items.Add(new ListItem(
                    hoja.Nombre + " (" + hoja.CodigoPermiso + ")",
                    hoja.IdComponentePermiso.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void CargarOpciones()
        {
            List<OpcionMenu> opciones = _bllOpcionMenu.Listar();
            pnlSinOpciones.Visible = opciones.Count == 0;
            rptOpciones.DataSource = opciones;
            rptOpciones.DataBind();
        }

        private void CargarParaEdicion(int idOpcion)
        {
            OpcionMenu opcion = _bllOpcionMenu.ObtenerPorId(idOpcion);
            if (opcion == null)
            {
                MostrarMensaje("No se encontró la opción de menú seleccionada.", true);
                return;
            }

            IdOpcionEnEdicion = opcion.IdOpcionMenu;
            litTituloFormulario.Text = "Editar opción";
            btnGuardar.Text = "Guardar cambios";
            lnkCancelar.Visible = true;
            txtTexto.Text = opcion.Texto;
            txtUrl.Text = opcion.Url;
            txtModulo.Text = opcion.Modulo;
            txtDescripcion.Text = opcion.Descripcion;
            chkActiva.Checked = opcion.Activo;

            string valorPermiso = opcion.IdComponentePermiso.ToString(CultureInfo.InvariantCulture);
            if (ddlPermiso.Items.FindByValue(valorPermiso) != null)
            {
                ddlPermiso.SelectedValue = valorPermiso;
            }
        }

        private void LimpiarFormulario()
        {
            IdOpcionEnEdicion = null;
            litTituloFormulario.Text = "Nueva opción";
            btnGuardar.Text = "Agregar opción";
            lnkCancelar.Visible = false;
            txtTexto.Text = string.Empty;
            txtUrl.Text = string.Empty;
            txtModulo.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            chkActiva.Checked = true;
            ddlPermiso.SelectedIndex = 0;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_MENU"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
