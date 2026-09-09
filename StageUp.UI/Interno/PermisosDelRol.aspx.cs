using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
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

            var idsSeleccionados = new List<int>();

            foreach (RepeaterItem item in rptPermisos.Items)
            {
                var chkPermiso = (CheckBox)item.FindControl("chkPermiso");
                var hdnIdPermiso = (HiddenField)item.FindControl("hdnIdPermiso");

                if (chkPermiso.Checked)
                {
                    idsSeleccionados.Add(Convert.ToInt32(hdnIdPermiso.Value));
                }
            }

            int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado = _bllPermiso.AsignarPermisosARol(
                IdRolInterno.Value, idsSeleccionados, idUsuarioInternoResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarPermisos(IdRolInterno.Value);
        }

        private void CargarPermisos(int idRolInterno)
        {
            List<PermisoInterno> permisos = _bllPermiso.ListarConAsignacion(idRolInterno);
            rptPermisos.DataSource = permisos;
            rptPermisos.DataBind();
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
