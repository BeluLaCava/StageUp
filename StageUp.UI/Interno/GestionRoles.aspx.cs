using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // CU-001-012 A11 a A15: pantalla "Roles y permisos". El rol se carga y se
    // edita junto con sus permisos, en el mismo formulario.
    public partial class GestionRoles : Page
    {
        private const string PermisoRequerido = "GESTIONAR_ROLES";

        private readonly BLL_RolInterno _bllRol = new BLL_RolInterno();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();
        private readonly BLL_AreaInterna _bllArea = new BLL_AreaInterna();

        private int? IdRolEnEdicion
        {
            get { return ViewState["IdRolEnEdicion"] as int?; }
            set { ViewState["IdRolEnEdicion"] = value; }
        }

        private int? IdRolEnDetalle
        {
            get { return ViewState["IdRolEnDetalle"] as int?; }
            set { ViewState["IdRolEnDetalle"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            if (!IsPostBack)
            {
                lnkUsuariosInternos.Visible = GestorDeSesion.TienePermisoInterno("GESTIONAR_USUARIOS_INTERNOS");
                CargarAreas();
                CargarRoles();

                int idRol;
                if (int.TryParse(Request.QueryString["editar"], out idRol))
                {
                    AbrirEdicion(idRol);
                }
                else if (int.TryParse(Request.QueryString["ver"], out idRol))
                {
                    MostrarDetalle(idRol, null, false);
                }
            }
        }

        protected void ddlFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarRoles();
        }

        protected void rptRoles_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idRolInterno;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idRolInterno))
            {
                return;
            }

            if (e.CommandName == "Ver")
            {
                MostrarDetalle(idRolInterno, null, false);
            }
            else if (e.CommandName == "Editar")
            {
                AbrirEdicion(idRolInterno);
            }
        }

        // ------------------------------------------------------------------
        // A11 (alta) y A12 (modificación)
        // ------------------------------------------------------------------
        protected void btnAgregarRol_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            IdRolEnEdicion = null;
            txtNombreRol.Text = string.Empty;
            txtDescripcionRol.Text = string.Empty;
            ddlAreaRol.ClearSelection();
            CargarArbol(new List<int>());
            litTituloFormulario.Text = "Nuevo rol";
            btnGuardarRol.Text = "Guardar rol";
            pnlMensajeFormulario.Visible = false;
            pnlDetalleRol.Visible = false;
            pnlFormularioRol.Visible = true;
        }

        protected void btnEditarDesdeDetalle_Click(object sender, EventArgs e)
        {
            if (IdRolEnDetalle.HasValue)
            {
                AbrirEdicion(IdRolEnDetalle.Value);
            }
        }

        protected void btnGuardarRol_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            List<int> idsSeleccionados = new List<int>();
            foreach (TreeNode nodo in tvPermisos.CheckedNodes)
            {
                int id;
                if (int.TryParse(nodo.Value, out id))
                {
                    idsSeleccionados.Add(id);
                }
            }

            int idArea;
            RolInterno datos = new RolInterno
            {
                IdRolInterno = IdRolEnEdicion ?? 0,
                NombreRol = txtNombreRol.Text,
                Descripcion = txtDescripcionRol.Text,
                IdAreaInterna = int.TryParse(ddlAreaRol.SelectedValue, out idArea) ? idArea : (int?)null
            };

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            bool esAlta = !IdRolEnEdicion.HasValue;
            ResultadoOperacion<int> resultado = _bllRol.Guardar(datos, idsSeleccionados, idResponsable);

            if (!resultado.Exitoso)
            {
                // A13, A14 y A15: el formulario queda abierto para corregir.
                litMensajeFormulario.Text = resultado.Mensaje;
                pnlMensajeFormulario.Visible = true;
                return;
            }

            // A12 paso 9: si el rol es el del usuario en sesión, sus
            // permisos se actualizan ya mismo (el resto, en su próxima página).
            if (GestorDeSesion.ObtenerIdRolInternoActual() == resultado.Valor)
            {
                Response.Redirect(Request.Path + "?ver=" + resultado.Valor + "&guardado=1", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            IdRolEnEdicion = null;
            pnlFormularioRol.Visible = false;
            CargarRoles();

            if (esAlta)
            {
                // A11 paso 13.
                MostrarMensaje(resultado.Mensaje, false);
            }
            else
            {
                // A12 paso 10.
                MostrarDetalle(resultado.Valor, resultado.Mensaje, false);
            }
        }

        protected void lnkCancelarEdicionRol_Click(object sender, EventArgs e)
        {
            int? idEditado = IdRolEnEdicion;
            IdRolEnEdicion = null;
            pnlFormularioRol.Visible = false;
            if (idEditado.HasValue)
            {
                MostrarDetalle(idEditado.Value, null, false);
            }
        }

        // ------------------------------------------------------------------
        // Detalle y baja
        // ------------------------------------------------------------------
        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            IdRolEnDetalle = null;
            pnlDetalleRol.Visible = false;
        }

        protected void btnBajaDesdeDetalle_Click(object sender, EventArgs e)
        {
            if (IdRolEnDetalle.HasValue)
            {
                MostrarDetalle(IdRolEnDetalle.Value, null, false, true);
            }
        }

        protected void lnkCancelarBaja_Click(object sender, EventArgs e)
        {
            if (IdRolEnDetalle.HasValue)
            {
                MostrarDetalle(IdRolEnDetalle.Value, null, false);
            }
        }

        protected void btnConfirmarBaja_Click(object sender, EventArgs e)
        {
            if (!IdRolEnDetalle.HasValue)
            {
                return;
            }

            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllRol.DarDeBaja(IdRolEnDetalle.Value, idResponsable);
            CargarRoles();

            if (resultado.Exitoso)
            {
                IdRolEnDetalle = null;
                pnlDetalleRol.Visible = false;
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            MostrarDetalle(IdRolEnDetalle.Value, resultado.Mensaje, true);
        }

        // ------------------------------------------------------------------
        // Carga de datos
        // ------------------------------------------------------------------
        private void CargarAreas()
        {
            ddlAreaRol.DataSource = _bllArea.ListarActivas();
            ddlAreaRol.DataBind();
            ddlAreaRol.Items.Insert(0, new ListItem("Sin área específica (todas)", string.Empty));
        }

        private void CargarRoles()
        {
            List<RolInterno> roles = _bllRol.ListarResumen(ddlFiltroEstado.SelectedValue);
            pnlSinRoles.Visible = roles.Count == 0;
            litSinRoles.Text = ddlFiltroEstado.SelectedValue == BLL_RolInterno.FiltroInactivos
                ? "No hay roles dados de baja."
                : "Todavía no hay roles cargados. Usá “Agregar rol” para crear el primero.";
            litCantidad.Text = roles.Count == 1 ? "1 rol" : roles.Count + " roles";
            rptRoles.DataSource = roles;
            rptRoles.DataBind();
        }

        private void AbrirEdicion(int idRolInterno)
        {
            RolInterno rol = _bllRol.ObtenerPorId(idRolInterno);
            if (rol == null)
            {
                MostrarMensaje("No se encontró el rol seleccionado.", true);
                return;
            }

            if (!rol.Activo)
            {
                MostrarDetalle(idRolInterno, "El rol está dado de baja: no se puede editar.", true);
                return;
            }

            IdRolEnEdicion = rol.IdRolInterno;
            IdRolEnDetalle = rol.IdRolInterno;
            txtNombreRol.Text = rol.NombreRol;
            txtDescripcionRol.Text = rol.Descripcion;
            ddlAreaRol.ClearSelection();
            if (rol.IdAreaInterna.HasValue)
            {
                ListItem item = ddlAreaRol.Items.FindByValue(rol.IdAreaInterna.Value.ToString(CultureInfo.InvariantCulture));
                if (item != null)
                {
                    item.Selected = true;
                }
            }

            CargarArbol(_bllPermiso.ListarIdsAsignados(rol.IdRolInterno));
            litTituloFormulario.Text = "Editar rol: " + rol.NombreRol;
            btnGuardarRol.Text = "Guardar cambios";
            pnlMensajeFormulario.Visible = false;
            pnlDetalleRol.Visible = false;
            pnlFormularioRol.Visible = true;
        }

        private void CargarArbol(List<int> idsAsignados)
        {
            GrupoPermisos raiz = _bllPermiso.ListarArbolCompleto();
            HashSet<int> asignados = new HashSet<int>(idsAsignados);
            tvPermisos.Nodes.Clear();
            foreach (PermisoComponente hijo in raiz.Hijos)
            {
                tvPermisos.Nodes.Add(ConstruirNodo(hijo, asignados));
            }

            tvPermisos.ExpandAll();
        }

        private static TreeNode ConstruirNodo(PermisoComponente componente, HashSet<int> asignados)
        {
            TreeNode nodo = new TreeNode(componente.Nombre, componente.IdComponentePermiso.ToString(CultureInfo.InvariantCulture))
            {
                Checked = asignados.Contains(componente.IdComponentePermiso),
                SelectAction = TreeNodeSelectAction.None
            };

            PermisoHoja hoja = componente as PermisoHoja;
            if (hoja != null && !string.IsNullOrWhiteSpace(hoja.Descripcion))
            {
                nodo.ToolTip = hoja.Descripcion;
            }

            GrupoPermisos grupo = componente as GrupoPermisos;
            if (grupo != null)
            {
                foreach (PermisoComponente hijo in grupo.Hijos)
                {
                    nodo.ChildNodes.Add(ConstruirNodo(hijo, asignados));
                }
            }

            return nodo;
        }

        private void MostrarDetalle(int idRolInterno, string mensaje, bool mensajeEsError, bool confirmarBaja = false)
        {
            DetalleRolInterno detalle = _bllRol.ObtenerDetalle(idRolInterno);
            if (detalle == null)
            {
                pnlDetalleRol.Visible = false;
                MostrarMensaje("No se encontró el rol seleccionado.", true);
                return;
            }

            RolInterno rol = detalle.Rol;
            IdRolEnDetalle = rol.IdRolInterno;

            litDetalleNombre.Text = rol.NombreRol;
            lblDetalleEstado.Text = HttpUtility.HtmlEncode(rol.Activo ? "Rol activo" : "Rol dado de baja");
            lblDetalleEstado.CssClass = "admin-status-badge " + (rol.Activo ? "admin-status-badge-active" : "admin-status-badge-inactive");
            litDetalleDescripcion.Text = string.IsNullOrWhiteSpace(rol.Descripcion) ? "Sin descripción" : rol.Descripcion;
            litDetalleArea.Text = rol.NombreArea ?? "Sin área específica";
            litDetalleEstadoDato.Text = rol.EstadoRol;
            litDetalleFechas.Text = "Alta " + rol.FechaAlta.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) +
                (rol.FechaUltimaModificacion.HasValue
                    ? " · modificado " + rol.FechaUltimaModificacion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                    : string.Empty) +
                (rol.FechaBaja.HasValue
                    ? " · baja " + rol.FechaBaja.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                    : string.Empty);

            litDetalleAsignados.Text = detalle.ComponentesAsignados.Count == 0
                ? "Nada asignado"
                : string.Join(", ", detalle.ComponentesAsignados.Select(c =>
                    c is GrupoPermisos
                        ? "Grupo " + c.Nombre + " (" + c.Listar().Count + (c.Listar().Count == 1 ? " permiso)" : " permisos)")
                        : c.Nombre));

            litDetalleCantidadPermisos.Text = "(" + detalle.PermisosEfectivos.Count + ")";
            litDetalleSinPermisos.Visible = detalle.PermisosEfectivos.Count == 0;
            rptDetallePermisos.DataSource = AgruparPermisos(detalle.PermisosEfectivos);
            rptDetallePermisos.DataBind();

            litDetalleCantidadUsuarios.Text = "(" + detalle.Usuarios.Count + ")";
            litDetalleSinUsuarios.Visible = detalle.Usuarios.Count == 0;
            rptDetalleUsuarios.DataSource = detalle.Usuarios;
            rptDetalleUsuarios.DataBind();

            btnEditarDesdeDetalle.Visible = rol.Activo && !confirmarBaja;
            btnBajaDesdeDetalle.Visible = rol.Activo && !confirmarBaja;
            pnlConfirmarBaja.Visible = rol.Activo && confirmarBaja;
            litConfirmarBajaNombre.Text = rol.NombreRol;

            litDetalleMensaje.Text = mensaje ?? string.Empty;
            pnlDetalleMensaje.CssClass = mensajeEsError ? "form-message form-message-error" : "form-message form-message-success";
            pnlDetalleMensaje.Visible = !string.IsNullOrEmpty(mensaje);

            pnlFormularioRol.Visible = false;
            pnlDetalleRol.Visible = true;
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // Vuelta después de guardar el rol propio (ver btnGuardarRol_Click).
            if (!IsPostBack && Request.QueryString["guardado"] == "1" && IdRolEnDetalle.HasValue)
            {
                pnlDetalleMensaje.CssClass = "form-message form-message-success";
                litDetalleMensaje.Text = "El rol y sus permisos fueron actualizados correctamente. Tus permisos ya se actualizaron.";
                pnlDetalleMensaje.Visible = true;
            }
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

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
