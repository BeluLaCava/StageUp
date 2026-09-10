using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class GestionUsuariosInternos : Page
    {
        private readonly BLL_UsuarioInterno _bllUsuario = new BLL_UsuarioInterno();
        private readonly BLL_AreaInterna _bllArea = new BLL_AreaInterna();
        private readonly BLL_RolInterno _bllRol = new BLL_RolInterno();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        private int? IdUsuarioEnEdicion
        {
            get { return ViewState["IdUsuarioEnEdicion"] as int?; }
            set { ViewState["IdUsuarioEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_USUARIOS_INTERNOS"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return;
            }

            if (!IsPostBack)
            {
                lnkGestionarRoles.Visible = GestorDeSesion.TienePermisoInterno("GESTIONAR_ROLES");
                CargarCatalogos();
                CargarUsuarios();
                PrepararAlta();
            }
        }

        protected void btnGuardarUsuario_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int idAreaInterna;
            int idRolInterno;
            if (!int.TryParse(ddlAreaInterna.SelectedValue, out idAreaInterna) ||
                !int.TryParse(ddlRolInterno.SelectedValue, out idRolInterno))
            {
                MostrarMensaje("Seleccioná un área y un rol válidos.", true);
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;

            if (!IdUsuarioEnEdicion.HasValue)
            {
                resultado = _bllUsuario.Registrar(
                    txtNombre.Text, txtApellido.Text, txtCorreoElectronico.Text,
                    idAreaInterna, idRolInterno, ddlEstadoCuenta.SelectedValue,
                    txtPassword.Text, txtConfirmacionPassword.Text, idResponsable);
            }
            else
            {
                resultado = _bllUsuario.Modificar(
                    IdUsuarioEnEdicion.Value, txtNombre.Text, txtApellido.Text,
                    txtCorreoElectronico.Text, idAreaInterna, idRolInterno,
                    ddlEstadoCuenta.SelectedValue, txtPassword.Text,
                    txtConfirmacionPassword.Text, idResponsable);
            }

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            if (IdUsuarioEnEdicion == idResponsable)
            {
                ActualizarSesionPropia(idResponsable);
            }

            PrepararAlta();
            CargarUsuarios();
            MostrarMensaje(resultado.Mensaje, false);
        }

        protected void lnkCancelarEdicion_Click(object sender, EventArgs e)
        {
            PrepararAlta();
            pnlMensaje.Visible = false;
        }

        protected void rptUsuarios_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idUsuarioInterno = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "Editar")
            {
                CargarUsuarioEnFormulario(idUsuarioInterno);
                return;
            }

            if (e.CommandName == "Baja")
            {
                int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
                ResultadoOperacion resultado = _bllUsuario.DarDeBaja(idUsuarioInterno, idResponsable);

                if (resultado.Exitoso && IdUsuarioEnEdicion == idUsuarioInterno)
                {
                    PrepararAlta();
                }

                CargarUsuarios();
                MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            }
        }

        protected string ObtenerClaseEstado(object activo)
        {
            return Convert.ToBoolean(activo) ? string.Empty : "internal-user-row-inactive";
        }

        protected string ObtenerClaseInsignia(object activo)
        {
            return Convert.ToBoolean(activo) ? "admin-status-badge-active" : "admin-status-badge-inactive";
        }

        protected bool PuedeDarDeBaja(object idUsuarioInterno, object activo)
        {
            int? idActual = GestorDeSesion.ObtenerIdUsuarioInternoActual();
            return Convert.ToBoolean(activo) && (!idActual.HasValue || Convert.ToInt32(idUsuarioInterno) != idActual.Value);
        }

        private void CargarCatalogos()
        {
            ddlAreaInterna.DataSource = _bllArea.ListarActivas();
            ddlAreaInterna.DataBind();
            ddlAreaInterna.Items.Insert(0, new ListItem("Seleccioná un área", string.Empty));

            ddlRolInterno.DataSource = _bllRol.Listar();
            ddlRolInterno.DataBind();
            ddlRolInterno.Items.Insert(0, new ListItem("Seleccioná un rol", string.Empty));

            ddlEstadoCuenta.Items.Clear();
            ddlEstadoCuenta.Items.Add(new ListItem("Activa", EstadoCuentaInterno.Activa.ToString()));
            ddlEstadoCuenta.Items.Add(new ListItem("Inactiva", EstadoCuentaInterno.Inactiva.ToString()));
        }

        private void CargarUsuarios()
        {
            List<UsuarioInterno> usuarios = _bllUsuario.Listar();
            pnlSinUsuarios.Visible = usuarios.Count == 0;
            rptUsuarios.DataSource = usuarios;
            rptUsuarios.DataBind();
        }

        private void CargarUsuarioEnFormulario(int idUsuarioInterno)
        {
            UsuarioInterno usuario = _bllUsuario.ObtenerPorId(idUsuarioInterno);
            if (usuario == null)
            {
                MostrarMensaje("No se encontró el usuario interno seleccionado.", true);
                return;
            }

            IdUsuarioEnEdicion = usuario.IdUsuarioInterno;
            txtNombre.Text = usuario.Nombre;
            txtApellido.Text = usuario.Apellido;
            txtCorreoElectronico.Text = usuario.CorreoElectronico;
            SeleccionarSiExiste(ddlAreaInterna, usuario.IdAreaInterna.ToString());
            SeleccionarSiExiste(ddlRolInterno, usuario.IdRolInterno.ToString());
            SeleccionarSiExiste(ddlEstadoCuenta, usuario.EstadoCuenta);
            txtPassword.Text = string.Empty;
            txtConfirmacionPassword.Text = string.Empty;
            litTituloFormulario.Text = "Editar usuario interno";
            litAyudaFormulario.Text = "Actualizá los datos de la cuenta seleccionada.";
            litTituloPassword.Text = "Nueva contraseña";
            litAyudaPassword.Text = "Dejá ambos campos vacíos para conservar la contraseña actual.";
            btnGuardarUsuario.Text = "Guardar cambios";
            lnkCancelarEdicion.Visible = true;
            CargarPermisosDelRol(usuario.IdRolInterno);
        }

        private void CargarPermisosDelRol(int idRolInterno)
        {
            List<string> permisos = _bllPermiso.ListarCodigosPermisosDeRol(idRolInterno);
            pnlPermisosRol.Visible = true;
            litSinPermisosRol.Visible = permisos.Count == 0;
            rptPermisosRol.DataSource = permisos;
            rptPermisosRol.DataBind();
        }

        private void PrepararAlta()
        {
            IdUsuarioEnEdicion = null;
            txtNombre.Text = string.Empty;
            txtApellido.Text = string.Empty;
            txtCorreoElectronico.Text = string.Empty;
            txtPassword.Text = string.Empty;
            txtConfirmacionPassword.Text = string.Empty;
            ddlAreaInterna.SelectedIndex = 0;
            ddlRolInterno.SelectedIndex = 0;
            ddlEstadoCuenta.SelectedValue = EstadoCuentaInterno.Activa.ToString();
            litTituloFormulario.Text = "Nuevo usuario interno";
            litAyudaFormulario.Text = "Todos los campos identificados con un asterisco son obligatorios.";
            litTituloPassword.Text = "Contraseña inicial";
            litAyudaPassword.Text = "Debe tener al menos 8 caracteres, letras y números.";
            btnGuardarUsuario.Text = "Guardar usuario";
            lnkCancelarEdicion.Visible = false;
            pnlPermisosRol.Visible = false;
            rptPermisosRol.DataSource = null;
            rptPermisosRol.DataBind();
        }

        private void ActualizarSesionPropia(int idUsuarioInterno)
        {
            UsuarioInterno usuario = _bllUsuario.ObtenerPorId(idUsuarioInterno);
            if (usuario == null || !usuario.Activo)
            {
                return;
            }

            List<string> permisos = _bllPermiso.ListarCodigosPermisosDeRol(usuario.IdRolInterno);
            GestorDeSesion.IniciarSesionInterna(usuario, permisos);
        }

        private static void SeleccionarSiExiste(ListControl control, string valor)
        {
            ListItem item = control.Items.FindByValue(valor);
            if (item != null)
            {
                control.ClearSelection();
                item.Selected = true;
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrWhiteSpace(mensaje);
        }
    }
}
