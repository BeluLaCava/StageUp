namespace StageUp.UI.Interno
{
    public partial class GestionUsuariosInternos
    {
        protected global::System.Web.UI.WebControls.Panel pnlMensaje;
        protected global::System.Web.UI.WebControls.Literal litMensaje;
        protected global::System.Web.UI.WebControls.Panel pnlFormularioUsuario;
        protected global::System.Web.UI.WebControls.Literal litTituloFormulario;
        protected global::System.Web.UI.WebControls.Literal litAyudaFormulario;
        protected global::System.Web.UI.WebControls.TextBox txtNombre;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvNombre;
        protected global::System.Web.UI.WebControls.TextBox txtApellido;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvApellido;
        protected global::System.Web.UI.WebControls.TextBox txtCorreoElectronico;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvCorreo;
        protected global::System.Web.UI.WebControls.RegularExpressionValidator revCorreo;
        protected global::System.Web.UI.WebControls.DropDownList ddlAreaInterna;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvArea;
        protected global::System.Web.UI.WebControls.DropDownList ddlRolInterno;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvRol;
        protected global::System.Web.UI.WebControls.DropDownList ddlEstadoCuenta;
        protected global::System.Web.UI.WebControls.Literal litTituloPassword;
        protected global::System.Web.UI.WebControls.Literal litAyudaPassword;
        protected global::System.Web.UI.WebControls.TextBox txtPassword;
        protected global::System.Web.UI.WebControls.TextBox txtConfirmacionPassword;
        protected global::System.Web.UI.WebControls.CompareValidator cvPassword;
        protected global::System.Web.UI.WebControls.Panel pnlPermisosRol;
        protected global::System.Web.UI.WebControls.Repeater rptPermisosRol;
        protected global::System.Web.UI.WebControls.Literal litSinPermisosRol;
        protected global::System.Web.UI.WebControls.Button btnGuardarUsuario;
        protected global::System.Web.UI.WebControls.LinkButton lnkCancelarEdicion;
        protected global::System.Web.UI.WebControls.Panel pnlSinUsuarios;
        protected global::System.Web.UI.HtmlControls.HtmlAnchor lnkGestionarRoles;
        protected global::System.Web.UI.WebControls.Repeater rptUsuarios;
    }
}
