using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BE.Permisos;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // CU-001-012 Gestionar usuarios internos y permisos: pantalla "Usuarios
    // internos y permisos" (escenario principal, A2 a A10).
    public partial class GestionUsuariosInternos : Page
    {
        private const string PermisoRequerido = "GESTIONAR_USUARIOS_INTERNOS";

        private readonly BLL_UsuarioInterno _bllUsuario = new BLL_UsuarioInterno();
        private readonly BLL_AreaInterna _bllArea = new BLL_AreaInterna();
        private readonly BLL_RolInterno _bllRol = new BLL_RolInterno();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        private int? IdUsuarioEnEdicion
        {
            get { return ViewState["IdUsuarioEnEdicion"] as int?; }
            set { ViewState["IdUsuarioEnEdicion"] = value; }
        }

        private int? IdUsuarioEnDetalle
        {
            get { return ViewState["IdUsuarioEnDetalle"] as int?; }
            set { ViewState["IdUsuarioEnDetalle"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            // Paso 2 y A1.
            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            if (!IsPostBack)
            {
                lnkGestionarRoles.Visible = GestorDeSesion.TienePermisoInterno("GESTIONAR_ROLES");
                CargarCatalogos();
                CargarUsuarios();

                int idVer;
                if (int.TryParse(Request.QueryString["ver"], out idVer))
                {
                    MostrarDetalle(idVer, null, false);
                }
                else if (Request.QueryString["nuevo"] == "1")
                {
                    AbrirAlta();
                }
            }
        }

        // ------------------------------------------------------------------
        // Listado y búsqueda (pasos 4 a 6, A2)
        // ------------------------------------------------------------------
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarUsuarios();
        }

        protected void lnkLimpiarFiltros_Click(object sender, EventArgs e)
        {
            txtBuscar.Text = string.Empty;
            ddlFiltroRol.ClearSelection();
            ddlFiltroEstado.ClearSelection();
            CargarUsuarios();
        }

        protected void rptUsuarios_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idUsuarioInterno;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idUsuarioInterno))
            {
                return;
            }

            if (e.CommandName == "Ver")
            {
                MostrarDetalle(idUsuarioInterno, null, false);
            }
            else if (e.CommandName == "Editar")
            {
                AbrirEdicion(idUsuarioInterno);
            }
        }

        // ------------------------------------------------------------------
        // Alta y edición (pasos 7 a 17, A3 a A6, A8 y A9)
        // ------------------------------------------------------------------
        protected void btnAgregarUsuario_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            AbrirAlta();
        }

        protected void btnEditarDesdeDetalle_Click(object sender, EventArgs e)
        {
            if (IdUsuarioEnDetalle.HasValue)
            {
                AbrirEdicion(IdUsuarioEnDetalle.Value);
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
            int.TryParse(ddlAreaInterna.SelectedValue, out idAreaInterna);
            int.TryParse(ddlRolInterno.SelectedValue, out idRolInterno);

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;
            bool esAlta = !IdUsuarioEnEdicion.HasValue;

            if (esAlta)
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
                // A3 a A6 / A9: el formulario queda abierto con los datos para corregir.
                litMensajeFormulario.Text = resultado.Mensaje;
                pnlMensajeFormulario.Visible = true;
                return;
            }

            int? idEditado = IdUsuarioEnEdicion;
            if (idEditado == idResponsable)
            {
                ActualizarSesionPropia(idResponsable);
            }

            CerrarFormulario();
            CargarUsuarios();

            if (esAlta)
            {
                // Pasos 16 y 17.
                MostrarMensaje(resultado.Mensaje, false);
            }
            else
            {
                // A8 pasos 8 y 9.
                MostrarDetalle(idEditado.Value, resultado.Mensaje, false);
            }
        }

        protected void lnkCancelarEdicion_Click(object sender, EventArgs e)
        {
            // A8: al cancelar la edición se descarta lo no guardado y se vuelve
            // al detalle con la información registrada.
            int? idEditado = IdUsuarioEnEdicion;
            CerrarFormulario();
            if (idEditado.HasValue)
            {
                MostrarDetalle(idEditado.Value, null, false);
            }
        }

        // ------------------------------------------------------------------
        // Detalle y baja (A7 y A10)
        // ------------------------------------------------------------------
        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            IdUsuarioEnDetalle = null;
            pnlDetalleUsuario.Visible = false;
        }

        protected void btnBajaDesdeDetalle_Click(object sender, EventArgs e)
        {
            if (IdUsuarioEnDetalle.HasValue)
            {
                MostrarDetalle(IdUsuarioEnDetalle.Value, null, false, true);
            }
        }

        protected void lnkCancelarBaja_Click(object sender, EventArgs e)
        {
            if (IdUsuarioEnDetalle.HasValue)
            {
                MostrarDetalle(IdUsuarioEnDetalle.Value, null, false);
            }
        }

        protected void btnConfirmarBaja_Click(object sender, EventArgs e)
        {
            if (!IdUsuarioEnDetalle.HasValue)
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllUsuario.DarDeBaja(IdUsuarioEnDetalle.Value, idResponsable);

            CargarUsuarios();
            if (resultado.Exitoso)
            {
                IdUsuarioEnDetalle = null;
                pnlDetalleUsuario.Visible = false;
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            MostrarDetalle(IdUsuarioEnDetalle.Value, resultado.Mensaje, true);
        }

        // ------------------------------------------------------------------
        // Ayudas para la grilla
        // ------------------------------------------------------------------
        protected string ObtenerClaseEstado(object activo)
        {
            return Convert.ToBoolean(activo) ? string.Empty : "internal-user-row-inactive";
        }

        protected string ObtenerClaseInsignia(object activo)
        {
            return Convert.ToBoolean(activo) ? "admin-status-badge-active" : "admin-status-badge-inactive";
        }

        // ------------------------------------------------------------------
        // Carga de datos
        // ------------------------------------------------------------------
        private void CargarCatalogos()
        {
            ddlAreaInterna.DataSource = _bllArea.ListarActivas();
            ddlAreaInterna.DataBind();
            ddlAreaInterna.Items.Insert(0, new ListItem("Seleccioná un área", string.Empty));

            List<RolInterno> roles = _bllRol.Listar();
            ddlRolInterno.DataSource = roles;
            ddlRolInterno.DataBind();
            ddlRolInterno.Items.Insert(0, new ListItem("Seleccioná un rol", string.Empty));

            ddlFiltroRol.Items.Clear();
            ddlFiltroRol.Items.Add(new ListItem("Todos los roles", string.Empty));
            foreach (RolInterno rol in roles)
            {
                ddlFiltroRol.Items.Add(new ListItem(rol.NombreRol, rol.IdRolInterno.ToString(CultureInfo.InvariantCulture)));
            }

            ddlEstadoCuenta.Items.Clear();
            ddlEstadoCuenta.Items.Add(new ListItem("Activa", EstadoCuentaInterno.Activa.ToString()));
            ddlEstadoCuenta.Items.Add(new ListItem("Inactiva", EstadoCuentaInterno.Inactiva.ToString()));

            // Paso 15: permisos de cada rol, para mostrarlos al elegirlo.
            Dictionary<string, List<string>> permisosPorRol = new Dictionary<string, List<string>>();
            foreach (RolInterno rol in roles)
            {
                permisosPorRol[rol.IdRolInterno.ToString(CultureInfo.InvariantCulture)] =
                    _bllPermiso.ListarPermisosDeRol(rol.IdRolInterno).Select(h => h.Nombre).ToList();
            }

            hdnPermisosPorRol.Value = new JavaScriptSerializer().Serialize(permisosPorRol);
        }

        private void CargarUsuarios()
        {
            int idRolFiltro;
            int? rol = int.TryParse(ddlFiltroRol.SelectedValue, out idRolFiltro) ? idRolFiltro : (int?)null;
            string estado = ddlFiltroEstado.SelectedValue;
            bool hayFiltros = txtBuscar.Text.Trim().Length > 0 || rol.HasValue || estado != BLL_UsuarioInterno.FiltroTodos;

            List<UsuarioInterno> usuarios = _bllUsuario.Buscar(txtBuscar.Text, rol, estado);

            pnlSinUsuarios.Visible = usuarios.Count == 0 && !hayFiltros;
            pnlSinResultados.Visible = usuarios.Count == 0 && hayFiltros;
            litCantidad.Text = usuarios.Count == 0
                ? string.Empty
                : usuarios.Count == 1 ? "1 usuario interno" : usuarios.Count + " usuarios internos";

            rptUsuarios.DataSource = usuarios;
            rptUsuarios.DataBind();
        }

        private void AbrirAlta()
        {
            IdUsuarioEnEdicion = null;
            txtNombre.Text = string.Empty;
            txtApellido.Text = string.Empty;
            txtCorreoElectronico.Text = string.Empty;
            txtPassword.Text = string.Empty;
            txtConfirmacionPassword.Text = string.Empty;
            ddlAreaInterna.ClearSelection();
            ddlRolInterno.ClearSelection();
            SeleccionarSiExiste(ddlEstadoCuenta, EstadoCuentaInterno.Activa.ToString());
            litTituloFormulario.Text = "Nuevo usuario interno";
            litAyudaFormulario.Text = "Todos los campos identificados con un asterisco son obligatorios.";
            litTituloPassword.Text = "Contraseña inicial";
            litAyudaPassword.Text = "Debe tener al menos 8 caracteres, letras y números.";
            btnGuardarUsuario.Text = "Guardar usuario";
            pnlMensajeFormulario.Visible = false;
            pnlDetalleUsuario.Visible = false;
            pnlFormularioUsuario.Visible = true;
        }

        private void AbrirEdicion(int idUsuarioInterno)
        {
            UsuarioInterno usuario = _bllUsuario.ObtenerPorId(idUsuarioInterno);
            if (usuario == null)
            {
                MostrarMensaje("No se encontró el usuario interno seleccionado.", true);
                return;
            }

            IdUsuarioEnEdicion = usuario.IdUsuarioInterno;
            IdUsuarioEnDetalle = usuario.IdUsuarioInterno;
            txtNombre.Text = usuario.Nombre;
            txtApellido.Text = usuario.Apellido;
            txtCorreoElectronico.Text = usuario.CorreoElectronico;
            SeleccionarSiExiste(ddlAreaInterna, usuario.IdAreaInterna.ToString(CultureInfo.InvariantCulture));
            SeleccionarSiExiste(ddlRolInterno, usuario.IdRolInterno.ToString(CultureInfo.InvariantCulture));
            SeleccionarSiExiste(ddlEstadoCuenta, usuario.EstadoCuenta);
            txtPassword.Text = string.Empty;
            txtConfirmacionPassword.Text = string.Empty;
            litTituloFormulario.Text = "Editar usuario: " + HttpUtility.HtmlEncode(usuario.Nombre + " " + usuario.Apellido);
            litAyudaFormulario.Text = "Modificá los datos que quieras actualizar. Si cambiás el rol, el usuario pasa a tener los permisos del rol nuevo.";
            litTituloPassword.Text = "Nueva contraseña (opcional)";
            litAyudaPassword.Text = "Dejá ambos campos vacíos para conservar la contraseña actual.";
            btnGuardarUsuario.Text = "Guardar cambios";
            pnlMensajeFormulario.Visible = false;
            pnlDetalleUsuario.Visible = false;
            pnlFormularioUsuario.Visible = true;
        }

        private void CerrarFormulario()
        {
            IdUsuarioEnEdicion = null;
            pnlMensajeFormulario.Visible = false;
            pnlFormularioUsuario.Visible = false;
        }

        // A7: detalle con los permisos vinculados por el rol.
        private void MostrarDetalle(int idUsuarioInterno, string mensaje, bool mensajeEsError, bool confirmarBaja = false)
        {
            UsuarioInterno usuario = _bllUsuario.ObtenerPorId(idUsuarioInterno);
            if (usuario == null)
            {
                pnlDetalleUsuario.Visible = false;
                MostrarMensaje("No se encontró el usuario interno seleccionado.", true);
                return;
            }

            IdUsuarioEnDetalle = usuario.IdUsuarioInterno;
            int idActual = GestorDeSesion.ObtenerIdUsuarioInternoActual() ?? 0;

            litDetalleNombreCompleto.Text = usuario.Nombre + " " + usuario.Apellido;
            lblDetalleEstado.Text = HttpUtility.HtmlEncode(usuario.Activo ? "Cuenta activa" : "Cuenta inactiva");
            lblDetalleEstado.CssClass = "admin-status-badge " + ObtenerClaseInsignia(usuario.Activo);
            litDetalleNombre.Text = usuario.Nombre;
            litDetalleApellido.Text = usuario.Apellido;
            litDetalleCorreo.Text = usuario.CorreoElectronico;
            litDetalleArea.Text = usuario.NombreArea;
            litDetalleRol.Text = usuario.NombreRol;
            litDetalleEstadoDato.Text = usuario.EstadoCuenta + (usuario.IdUsuarioInterno == idActual ? " (es tu cuenta)" : string.Empty);
            litDetalleFechas.Text = "Alta " + usuario.FechaAlta.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) +
                (usuario.FechaUltimaModificacion.HasValue
                    ? " · modificado " + usuario.FechaUltimaModificacion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                    : string.Empty) +
                (usuario.FechaBaja.HasValue
                    ? " · baja " + usuario.FechaBaja.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                    : string.Empty);

            List<PermisoHoja> permisos = _bllPermiso.ListarPermisosDeRol(usuario.IdRolInterno);
            litDetalleCantidadPermisos.Text = "(" + permisos.Count + ", por el rol " + usuario.NombreRol + ")";
            litDetalleSinPermisos.Visible = permisos.Count == 0;
            rptDetallePermisos.DataSource = AgruparPermisos(permisos);
            rptDetallePermisos.DataBind();

            bool puedeDarDeBaja = usuario.Activo && usuario.IdUsuarioInterno != idActual;
            btnBajaDesdeDetalle.Visible = puedeDarDeBaja && !confirmarBaja;
            btnEditarDesdeDetalle.Visible = !confirmarBaja;
            pnlConfirmarBaja.Visible = puedeDarDeBaja && confirmarBaja;
            litConfirmarBajaNombre.Text = usuario.Nombre + " " + usuario.Apellido;

            litDetalleMensaje.Text = mensaje ?? string.Empty;
            pnlDetalleMensaje.CssClass = mensajeEsError ? "form-message form-message-error" : "form-message form-message-success";
            pnlDetalleMensaje.Visible = !string.IsNullOrEmpty(mensaje);

            pnlFormularioUsuario.Visible = false;
            pnlDetalleUsuario.Visible = true;
        }

        private static List<object> AgruparPermisos(List<PermisoHoja> permisos)
        {
            List<object> grupos = new List<object>();
            foreach (IGrouping<string, PermisoHoja> grupo in permisos.GroupBy(p => string.IsNullOrWhiteSpace(p.NombreGrupo) ? "General" : p.NombreGrupo))
            {
                StringBuilder etiquetas = new StringBuilder();
                foreach (PermisoHoja permiso in grupo)
                {
                    etiquetas.Append("<span title=\"")
                        .Append(HttpUtility.HtmlAttributeEncode(permiso.Descripcion ?? permiso.CodigoPermiso))
                        .Append("\">")
                        .Append(HttpUtility.HtmlEncode(permiso.Nombre))
                        .Append("</span>");
                }

                grupos.Add(new { Grupo = grupo.Key, Permisos = etiquetas.ToString() });
            }

            return grupos;
        }

        private void ActualizarSesionPropia(int idUsuarioInterno)
        {
            List<string> permisos;
            UsuarioInterno usuario = _bllUsuario.ObtenerParaSesion(idUsuarioInterno, out permisos);
            if (usuario != null)
            {
                GestorDeSesion.IniciarSesionInterna(usuario, permisos);
            }
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
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrWhiteSpace(mensaje);
        }
    }
}
