
namespace StageUp.UI
{
    public partial class MisEspacios
    {
        protected global::System.Web.UI.WebControls.Image imgFotoActual;
        protected global::System.Web.UI.WebControls.FileUpload archivoFoto;
        protected global::System.Web.UI.WebControls.TextBox txtProvincia;
        protected global::System.Web.UI.WebControls.TextBox txtCiudad;
        protected global::System.Web.UI.WebControls.TextBox txtDireccion;
        protected global::System.Web.UI.WebControls.TextBox txtCapacidad;
        protected global::System.Web.UI.WebControls.TextBox txtPrecioHora;
        protected global::System.Web.UI.WebControls.TextBox txtTipoPiso;
        protected global::System.Web.UI.WebControls.TextBox txtEquipamientoDetalle;
        protected global::System.Web.UI.WebControls.DropDownList ddlMoneda;
        protected global::System.Web.UI.WebControls.CheckBoxList cblEquipamiento;
        protected global::System.Web.UI.WebControls.HiddenField hdnDisponibilidad;
        protected global::System.Web.UI.WebControls.LinkButton lnkNuevoEspacio;
        protected global::System.Web.UI.WebControls.Panel pnlFormularioMensaje;
        protected global::System.Web.UI.WebControls.Literal litFormularioMensaje;
        protected global::System.Web.UI.WebControls.Panel pnlMensaje;
        protected global::System.Web.UI.WebControls.Literal litMensaje;

        protected global::System.Web.UI.WebControls.Panel pnlPendienteGestor;

        protected global::System.Web.UI.WebControls.Panel pnlNoGestor;
        protected global::System.Web.UI.WebControls.Button btnSolicitarGestor;
        protected global::System.Web.UI.HtmlControls.HtmlAnchor lnkVerMisReservas;

        protected global::System.Web.UI.WebControls.Panel pnlPanelGestor;

        protected global::System.Web.UI.WebControls.Panel pnlFormularioEspacio;
        protected global::System.Web.UI.WebControls.Literal litTituloFormulario;

        protected global::System.Web.UI.WebControls.TextBox txtNombreEspacio;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvNombreEspacio;

        protected global::System.Web.UI.WebControls.TextBox txtTipoEspacio;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvTipoEspacio;

        protected global::System.Web.UI.WebControls.TextBox txtDescripcion;

        protected global::System.Web.UI.WebControls.Button btnGuardarEspacio;
        protected global::System.Web.UI.WebControls.LinkButton lnkCancelarEdicion;

        protected global::System.Web.UI.WebControls.Literal litSinEspacios;
        protected global::System.Web.UI.WebControls.Repeater rptMisEspacios;

        protected global::System.Web.UI.WebControls.Literal litSinSolicitudes;
        protected global::System.Web.UI.WebControls.Repeater rptSolicitudes;
    }
}
