using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class PermisosDelRol : Page
    {
        private readonly BLL_RolInterno _bllRol = new BLL_RolInterno();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        private int? IdRolInterno
        {
            get { return ViewState["IdRolInterno"] as int?; }
            set { ViewState["IdRolInterno"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_ROLES"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return;
            }

            if (!IsPostBack)
            {
                int idRolInterno;
                if (!int.TryParse(Request.QueryString["idRol"], out idRolInterno))
                {
                    MostrarRolNoEncontrado();
                    return;
                }

                RolInterno rol = _bllRol.ObtenerPorId(idRolInterno);
                if (rol == null)
                {
                    MostrarRolNoEncontrado();
                    return;
                }

                IdRolInterno = rol.IdRolInterno;
                litNombreRol.Text = Server.HtmlEncode(rol.NombreRol);
                CargarPermisos(rol.IdRolInterno);
            }
        }

        protected void btnGuardarPermisos_Click(object sender, EventArgs e)
        {
            if (IdRolInterno == null)
            {
                return;
            }

            List<int> idsSeleccionados = new List<int>();
            foreach (TreeNode nodo in tvPermisos.CheckedNodes)
            {
                idsSeleccionados.Add(Convert.ToInt32(nodo.Value));
            }

            int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllPermiso.AsignarComponentesARol(
                IdRolInterno.Value, idsSeleccionados, idUsuarioInternoResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarPermisos(IdRolInterno.Value);
        }

        private void CargarPermisos(int idRolInterno)
        {
            GrupoPermisos raiz = _bllPermiso.ListarComponentesRaizConAsignacion(idRolInterno);

            tvPermisos.Nodes.Clear();
            foreach (PermisoComponente hijo in raiz.Hijos)
            {
                tvPermisos.Nodes.Add(ConstruirNodo(hijo));
            }
            tvPermisos.ExpandAll();
        }

        private static TreeNode ConstruirNodo(PermisoComponente componente)
        {
            TreeNode nodo = new TreeNode(componente.Nombre, componente.IdComponentePermiso.ToString())
            {
                Checked = componente.Asignado,
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
                    nodo.ChildNodes.Add(ConstruirNodo(hijo));
                }
            }

            return nodo;
        }

        private void MostrarRolNoEncontrado()
        {
            pnlSinRol.Visible = true;
            pnlFormularioPermisos.Visible = false;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
