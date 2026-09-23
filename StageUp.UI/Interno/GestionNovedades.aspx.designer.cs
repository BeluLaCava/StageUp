namespace StageUp.UI.Interno
{
    public partial class GestionNovedades
    {
        protected global::System.Web.UI.WebControls.Panel pnlMensaje;
        protected global::System.Web.UI.WebControls.Literal litMensaje;
        protected global::System.Web.UI.WebControls.Literal litCantidadBorradores;
        protected global::System.Web.UI.WebControls.Literal litCantidadPublicadas;
        protected global::System.Web.UI.WebControls.Literal litUltimoEnvio;
        protected global::System.Web.UI.WebControls.Panel pnlFormularioNovedad;
        protected global::System.Web.UI.WebControls.Literal litTituloFormulario;
        protected global::System.Web.UI.WebControls.TextBox txtTitulo;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvTitulo;
        protected global::System.Web.UI.WebControls.TextBox txtResumen;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvResumen;
        protected global::System.Web.UI.WebControls.TextBox txtContenido;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvContenido;
        protected global::System.Web.UI.WebControls.DropDownList ddlCategoria;
        protected global::System.Web.UI.WebControls.TextBox txtFechaPublicacion;
        protected global::System.Web.UI.WebControls.TextBox txtUrlImagen;
        protected global::System.Web.UI.WebControls.CheckBox chkEnviarMail;
        protected global::System.Web.UI.WebControls.DropDownList ddlDestinatarios;
        protected global::System.Web.UI.WebControls.Button btnGuardarBorrador;
        protected global::System.Web.UI.WebControls.Button btnPublicar;
        protected global::System.Web.UI.WebControls.Button btnEnviarPrueba;
        protected global::System.Web.UI.WebControls.LinkButton lnkCancelarNovedad;
        protected global::System.Web.UI.WebControls.Literal litPreviewCategoria;
        protected global::System.Web.UI.WebControls.Literal litPreviewTitulo;
        protected global::System.Web.UI.WebControls.Literal litPreviewResumen;
        protected global::System.Web.UI.WebControls.Panel pnlSinNovedades;
        protected global::System.Web.UI.WebControls.Repeater rptNovedades;
    }
}
