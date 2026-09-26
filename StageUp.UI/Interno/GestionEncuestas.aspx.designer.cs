namespace StageUp.UI.Interno
{
    public partial class GestionEncuestas
    {
        protected global::System.Web.UI.WebControls.Panel pnlMensaje;
        protected global::System.Web.UI.WebControls.Literal litMensaje;
        protected global::System.Web.UI.WebControls.Literal litCantidadActivas;
        protected global::System.Web.UI.WebControls.Literal litCantidadBorrador;
        protected global::System.Web.UI.WebControls.Literal litCantidadCerradas;
        protected global::System.Web.UI.WebControls.Panel pnlFormularioEncuesta;
        protected global::System.Web.UI.WebControls.Literal litTituloFormulario;
        protected global::System.Web.UI.WebControls.TextBox txtTitulo;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvTitulo;
        protected global::System.Web.UI.WebControls.TextBox txtDescripcion;
        protected global::System.Web.UI.WebControls.TextBox txtFechaInicio;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvFechaInicio;
        protected global::System.Web.UI.WebControls.TextBox txtFechaVencimiento;
        protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvFechaVencimiento;
        protected global::System.Web.UI.WebControls.DropDownList ddlPublicoObjetivo;
        protected global::System.Web.UI.WebControls.Button btnGuardarEncuesta;
        protected global::System.Web.UI.WebControls.LinkButton lnkCancelarEncuesta;
        protected global::System.Web.UI.WebControls.Panel pnlDatosSoloLectura;
        protected global::System.Web.UI.WebControls.Literal litTituloSoloLectura;
        protected global::System.Web.UI.WebControls.Literal litDescripcionSoloLectura;
        protected global::System.Web.UI.WebControls.Literal litVigenciaSoloLectura;
        protected global::System.Web.UI.WebControls.Literal litPublicoSoloLectura;
        protected global::System.Web.UI.WebControls.LinkButton lnkCancelarSoloLectura;
        protected global::System.Web.UI.WebControls.Panel pnlPreguntas;
        protected global::System.Web.UI.WebControls.Panel pnlSinPreguntas;
        protected global::System.Web.UI.WebControls.Repeater rptPreguntas;
        protected global::System.Web.UI.WebControls.Panel pnlAgregarPregunta;
        protected global::System.Web.UI.WebControls.TextBox txtTextoPregunta;
        protected global::System.Web.UI.WebControls.TextBox txtOpcionesPregunta;
        protected global::System.Web.UI.WebControls.Button btnAgregarPregunta;
        protected global::System.Web.UI.WebControls.Panel pnlEstadoAcciones;
        protected global::System.Web.UI.WebControls.Literal litEstadoBadge;
        protected global::System.Web.UI.WebControls.Button btnPublicar;
        protected global::System.Web.UI.WebControls.Button btnCerrar;
        protected global::System.Web.UI.WebControls.Literal litEstadoSinAcciones;
        protected global::System.Web.UI.WebControls.Panel pnlResultados;
        protected global::System.Web.UI.WebControls.Literal litTotalRespuestas;
        protected global::System.Web.UI.WebControls.Repeater rptResultados;
        protected global::System.Web.UI.WebControls.Panel pnlSinEncuestas;
        protected global::System.Web.UI.WebControls.Repeater rptEncuestas;
    }
}
