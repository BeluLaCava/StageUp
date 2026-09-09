using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class GestionRoles : Page
    {
        private readonly BLL_RolInterno _bllRol = new BLL_RolInterno();

        private int? IdRolEnEdicion
        {
            get { return ViewState["IdRolEnEdicion"] as int?; }
            set { ViewState["IdRolEnEdicion"] = value; }
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
                CargarRoles();
            }
        }

        protected void btnGuardarRol_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            ResultadoOperacion resultado;

            if (IdRolEnEdicion == null)
            {
                ResultadoOperacion<int> resultadoAlta = _bllRol.Registrar(
                    txtNombreRol.Text, txtDescripcionRol.Text, null, idUsuarioInternoResponsable);
                resultado = resultadoAlta;
            }
            else
            {
                resultado = _bllRol.Modificar(
                    IdRolEnEdicion.Value, txtNombreRol.Text, txtDescripcionRol.Text, null, idUsuarioInternoResponsable);
            }

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                return;
            }

            LimpiarFormulario();
            MostrarMensaje(resultado.Mensaje, esError: false);
            CargarRoles();
        }

        protected void lnkCancelarEdicionRol_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void rptRoles_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idRolInterno = Convert.ToInt32(e.CommandArgument);

            switch (e.CommandName)
            {
                case "Editar":
                    CargarRolEnFormulario(idRolInterno);
                    return;

                case "Permisos":
                    Response.Redirect("~/Interno/PermisosDelRol.aspx?idRol=" + idRolInterno);
                    return;

                case "Baja":
                    int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
                    ResultadoOperacion resultado = _bllRol.DarDeBaja(idRolInterno, idUsuarioInternoResponsable);
                    if (resultado.Exitoso && IdRolEnEdicion == idRolInterno)
                    {
                        LimpiarFormulario();
                    }
                    MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                    CargarRoles();
                    return;

                default:
                    return;
            }
        }

        private void CargarRoles()
        {
            List<RolInterno> roles = _bllRol.Listar();
            litSinRoles.Visible = roles.Count == 0;
            rptRoles.DataSource = roles;
            rptRoles.DataBind();
        }

        private void CargarRolEnFormulario(int idRolInterno)
        {
            RolInterno rol = _bllRol.ObtenerPorId(idRolInterno);
            if (rol == null)
            {
                MostrarMensaje("No se encontró el rol seleccionado.", esError: true);
                return;
            }

            IdRolEnEdicion = rol.IdRolInterno;
            txtNombreRol.Text = rol.NombreRol;
            txtDescripcionRol.Text = rol.Descripcion;
            litTituloFormulario.Text = "Editar rol";
            lnkCancelarEdicionRol.Visible = true;
            btnGuardarRol.Text = "Guardar cambios";
        }

        private void LimpiarFormulario()
        {
            IdRolEnEdicion = null;
            txtNombreRol.Text = string.Empty;
            txtDescripcionRol.Text = string.Empty;
            litTituloFormulario.Text = "Nuevo rol";
            lnkCancelarEdicionRol.Visible = false;
            btnGuardarRol.Text = "Guardar rol";
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
