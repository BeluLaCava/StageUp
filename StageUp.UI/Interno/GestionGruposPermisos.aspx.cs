using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Permisos;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class GestionGruposPermisos : Page
    {
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        private int? IdComponenteSeleccionado
        {
            get { return ViewState["IdComponenteSeleccionado"] as int?; }
            set { ViewState["IdComponenteSeleccionado"] = value; }
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
                CargarArbol();
                CargarDropdownGrupoPadreNuevo();
                ActualizarPanelSeleccion();
            }
        }

        protected void tvGrupos_SelectedNodeChanged(object sender, EventArgs e)
        {
            if (tvGrupos.SelectedNode != null)
            {
                IdComponenteSeleccionado = Convert.ToInt32(tvGrupos.SelectedNode.Value);
            }

            ActualizarPanelSeleccion();
        }

        protected void btnCrearGrupo_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int? idGrupoPadre = string.IsNullOrEmpty(ddlGrupoPadreNuevo.SelectedValue)
                ? (int?)null
                : Convert.ToInt32(ddlGrupoPadreNuevo.SelectedValue);

            int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion<int> resultado = _bllPermiso.CrearGrupo(idGrupoPadre, txtNombreGrupo.Text, idUsuarioInternoResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                txtNombreGrupo.Text = string.Empty;
            }

            RecargarTodo();
        }

        protected void btnMover_Click(object sender, EventArgs e)
        {
            if (IdComponenteSeleccionado == null)
            {
                return;
            }

            int? idNuevoPadre = string.IsNullOrEmpty(ddlNuevoPadre.SelectedValue)
                ? (int?)null
                : Convert.ToInt32(ddlNuevoPadre.SelectedValue);

            int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllPermiso.MoverComponente(
                IdComponenteSeleccionado.Value, idNuevoPadre, idUsuarioInternoResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            RecargarTodo();
        }

        protected void btnEliminarGrupo_Click(object sender, EventArgs e)
        {
            if (IdComponenteSeleccionado == null)
            {
                return;
            }

            int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllPermiso.EliminarGrupo(IdComponenteSeleccionado.Value, idUsuarioInternoResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                IdComponenteSeleccionado = null;
            }

            RecargarTodo();
        }

        private void RecargarTodo()
        {
            CargarArbol();
            CargarDropdownGrupoPadreNuevo();
            ActualizarPanelSeleccion();
        }

        private void CargarArbol()
        {
            GrupoPermisos raiz = _bllPermiso.ListarArbolCompleto();

            tvGrupos.Nodes.Clear();
            foreach (PermisoComponente hijo in raiz.Hijos)
            {
                tvGrupos.Nodes.Add(ConstruirNodo(hijo));
            }
            tvGrupos.ExpandAll();

            if (IdComponenteSeleccionado.HasValue)
            {
                TreeNode nodoSeleccionado = BuscarNodoPorValor(tvGrupos.Nodes, IdComponenteSeleccionado.Value.ToString());
                if (nodoSeleccionado != null)
                {
                    nodoSeleccionado.Selected = true;
                }
            }
        }

        private static TreeNode ConstruirNodo(PermisoComponente componente)
        {
            TreeNode nodo = new TreeNode(componente.Nombre, componente.IdComponentePermiso.ToString())
            {
                SelectAction = TreeNodeSelectAction.Select
            };

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

        private static TreeNode BuscarNodoPorValor(TreeNodeCollection nodos, string valor)
        {
            foreach (TreeNode nodo in nodos)
            {
                if (nodo.Value == valor)
                {
                    return nodo;
                }

                TreeNode enHijos = BuscarNodoPorValor(nodo.ChildNodes, valor);
                if (enHijos != null)
                {
                    return enHijos;
                }
            }
            return null;
        }

        private void ActualizarPanelSeleccion()
        {
            if (IdComponenteSeleccionado == null)
            {
                pnlSeleccion.Visible = false;
                return;
            }

            GrupoPermisos raiz = _bllPermiso.ListarArbolCompleto();
            PermisoComponente seleccionado = BuscarComponente(raiz, IdComponenteSeleccionado.Value);
            if (seleccionado == null)
            {
                IdComponenteSeleccionado = null;
                pnlSeleccion.Visible = false;
                return;
            }

            pnlSeleccion.Visible = true;
            litNombreSeleccionado.Text = Server.HtmlEncode(seleccionado.Nombre);

            bool esGrupo = seleccionado is GrupoPermisos;
            btnEliminarGrupo.Visible = esGrupo;

            HashSet<int> excluidos = new HashSet<int> { seleccionado.IdComponentePermiso };
            GrupoPermisos grupoSeleccionado = seleccionado as GrupoPermisos;
            if (grupoSeleccionado != null)
            {
                AgregarIdsDescendientes(grupoSeleccionado, excluidos);
            }

            CargarDropdownGrupos(ddlNuevoPadre, raiz, excluidos);
        }

        private static PermisoComponente BuscarComponente(PermisoComponente nodo, int idComponentePermiso)
        {
            if (nodo.IdComponentePermiso == idComponentePermiso)
            {
                return nodo;
            }

            GrupoPermisos grupo = nodo as GrupoPermisos;
            if (grupo != null)
            {
                foreach (PermisoComponente hijo in grupo.Hijos)
                {
                    PermisoComponente encontrado = BuscarComponente(hijo, idComponentePermiso);
                    if (encontrado != null)
                    {
                        return encontrado;
                    }
                }
            }

            return null;
        }

        private static void AgregarIdsDescendientes(GrupoPermisos grupo, HashSet<int> destino)
        {
            foreach (PermisoComponente hijo in grupo.Hijos)
            {
                destino.Add(hijo.IdComponentePermiso);
                GrupoPermisos subgrupo = hijo as GrupoPermisos;
                if (subgrupo != null)
                {
                    AgregarIdsDescendientes(subgrupo, destino);
                }
            }
        }

        private void CargarDropdownGrupoPadreNuevo()
        {
            GrupoPermisos raiz = _bllPermiso.ListarArbolCompleto();
            CargarDropdownGrupos(ddlGrupoPadreNuevo, raiz, new HashSet<int>());
        }

        private static void CargarDropdownGrupos(DropDownList lista, GrupoPermisos raiz, HashSet<int> excluidos)
        {
            lista.Items.Clear();
            lista.Items.Add(new ListItem("— Raíz —", string.Empty));
            AgregarOpcionesDeGrupo(raiz, 0, excluidos, lista);
        }

        private static void AgregarOpcionesDeGrupo(GrupoPermisos grupo, int profundidad, HashSet<int> excluidos, DropDownList destino)
        {
            foreach (PermisoComponente hijo in grupo.Hijos)
            {
                GrupoPermisos subgrupo = hijo as GrupoPermisos;
                if (subgrupo == null || excluidos.Contains(subgrupo.IdComponentePermiso))
                {
                    continue;
                }

                string prefijo = new string('—', profundidad + 1) + " ";
                destino.Items.Add(new ListItem(prefijo + subgrupo.Nombre, subgrupo.IdComponentePermiso.ToString()));
                AgregarOpcionesDeGrupo(subgrupo, profundidad + 1, excluidos, destino);
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
