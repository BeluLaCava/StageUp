
namespace StageUp.UI
{
    public partial class MisActividades
    {
        protected global::System.Web.UI.WebControls.LinkButton lnkNuevaActividad;

        protected global::System.Web.UI.WebControls.Panel pnlMensaje;
        protected global::System.Web.UI.WebControls.Literal litMensaje;

        protected global::System.Web.UI.WebControls.Panel pnlPendienteGestor;

        protected global::System.Web.UI.WebControls.Panel pnlNoGestor;
        protected global::System.Web.UI.WebControls.Button btnSolicitarGestor;
        protected global::System.Web.UI.HtmlControls.HtmlAnchor lnkVerMisReservas;

        protected global::System.Web.UI.WebControls.Panel pnlPanelGestor;

        protected global::System.Web.UI.WebControls.Panel pnlSinEspacios;

        protected global::System.Web.UI.WebControls.Panel pnlActividades;
        protected global::System.Web.UI.WebControls.Literal litActividadesActivas;
        protected global::System.Web.UI.WebControls.Literal litHorasBloqueadas;
        protected global::System.Web.UI.WebControls.Literal litEspaciosProgramados;
        protected global::System.Web.UI.WebControls.Repeater rptActividades;
        protected global::System.Web.UI.WebControls.LinkButton lnkNuevoParticipante;
        protected global::System.Web.UI.WebControls.Literal litParticipantesActivos;
        protected global::System.Web.UI.WebControls.Literal litParticipantesAsignados;
        protected global::System.Web.UI.WebControls.Literal litParticipantesSinAsignar;
        protected global::System.Web.UI.WebControls.Repeater rptParticipantes;

        protected global::System.Web.UI.WebControls.Panel pnlFormularioActividad;
        protected global::System.Web.UI.WebControls.LinkButton lnkCerrarActividad;

        protected global::System.Web.UI.WebControls.TextBox txtNombreActividad;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvNombreActividad;

        protected global::System.Web.UI.WebControls.DropDownList ddlEspacio;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvEspacio;

        protected global::System.Web.UI.WebControls.TextBox txtTipoActividad;
        protected global::System.Web.UI.WebControls.HiddenField hdnProgramacionActividad;
        protected global::System.Web.UI.WebControls.CheckBoxList cblDias;

        protected global::System.Web.UI.WebControls.TextBox txtHoraInicio;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvHoraInicio;

        protected global::System.Web.UI.WebControls.TextBox txtHoraFin;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvHoraFin;

        protected global::System.Web.UI.WebControls.TextBox txtCupoMaximo;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvCupoMaximo;

        protected global::System.Web.UI.WebControls.TextBox txtParticipantes;
        protected global::System.Web.UI.WebControls.HiddenField hdnParticipantesActividad;
        protected global::System.Web.UI.WebControls.TextBox txtDescripcionActividad;

        protected global::System.Web.UI.WebControls.Button btnGuardarActividad;
        protected global::System.Web.UI.WebControls.LinkButton lnkCancelarActividad;

        protected global::System.Web.UI.WebControls.Panel pnlFormularioParticipante;
        protected global::System.Web.UI.WebControls.LinkButton lnkCerrarParticipante;

        protected global::System.Web.UI.WebControls.TextBox txtNombreParticipante;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvNombreParticipante;

        protected global::System.Web.UI.WebControls.TextBox txtApellidoParticipante;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvApellidoParticipante;

        protected global::System.Web.UI.WebControls.TextBox txtDniParticipante;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvDniParticipante;

        protected global::System.Web.UI.WebControls.DropDownList ddlActividadParticipante;
        protected global::System.Web.UI.WebControls.TextBox txtNotasParticipante;

        protected global::System.Web.UI.WebControls.Button btnGuardarParticipante;
        protected global::System.Web.UI.WebControls.LinkButton lnkCancelarParticipante;
    }
}
