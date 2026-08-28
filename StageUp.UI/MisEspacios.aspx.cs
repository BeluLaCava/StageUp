using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    /// <summary>
    /// Code-behind de MisEspacios.aspx. Implementa el ABMC de
    /// EspacioArtistico (CU-001-007 Gestionar espacios artísticos) para
    /// las columnas propias de la entidad: alta, modificación, publicar,
    /// pausar y baja lógica.
    ///
    /// Requiere que el usuario esté autenticado (GestorDeSesion). No se
    /// exige todavía la "habilitación como gestor" que describe el CU
    /// completo (ver nota en BLL_EspacioArtistico) — cualquier usuario
    /// externo logueado puede administrar sus propios espacios por ahora.
    /// </summary>
    public partial class MisEspacios : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

        /// <summary>
        /// Id del espacio que se está editando actualmente. Null cuando el
        /// formulario está en modo "alta de espacio nuevo".
        /// </summary>
        private int? IdEspacioEnEdicion
        {
            get { return ViewState["IdEspacioEnEdicion"] as int?; }
            set { ViewState["IdEspacioEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarMisEspacios();
            }
        }

        protected void btnGuardarEspacio_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            ResultadoOperacion resultado;
            if (IdEspacioEnEdicion == null)
            {
                ResultadoOperacion<int> resultadoAlta = _bllEspacio.Registrar(
                    idUsuarioGestor, txtNombreEspacio.Text, txtDescripcion.Text, txtTipoEspacio.Text);
                resultado = resultadoAlta;
            }
            else
            {
                resultado = _bllEspacio.Modificar(
                    IdEspacioEnEdicion.Value, idUsuarioGestor, txtNombreEspacio.Text, txtDescripcion.Text, txtTipoEspacio.Text);
            }

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                return;
            }

            LimpiarFormulario();
            MostrarMensaje(resultado.Mensaje, esError: false);
            CargarMisEspacios();
        }

        protected void lnkCancelarEdicion_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void rptMisEspacios_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idEspacioArtistico = Convert.ToInt32(e.CommandArgument);
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado;

            switch (e.CommandName)
            {
                case "Editar":
                    CargarEspacioEnFormulario(idEspacioArtistico);
                    return;

                case "Publicar":
                    resultado = _bllEspacio.Publicar(idEspacioArtistico, idUsuarioGestor);
                    break;

                case "Pausar":
                    resultado = _bllEspacio.Pausar(idEspacioArtistico, idUsuarioGestor);
                    break;

                case "BajaLogica":
                    resultado = _bllEspacio.DarDeBaja(idEspacioArtistico, idUsuarioGestor);
                    if (resultado.Exitoso && IdEspacioEnEdicion == idEspacioArtistico)
                    {
                        LimpiarFormulario();
                    }
                    break;

                default:
                    return;
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarMisEspacios();
        }

        protected void rptMisEspacios_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var espacio = (EspacioArtistico)e.Item.DataItem;
            var lnkPublicar = (LinkButton)e.Item.FindControl("lnkPublicar");
            var lnkPausar = (LinkButton)e.Item.FindControl("lnkPausar");

            lnkPublicar.Visible = !espacio.Publicado;
            lnkPausar.Visible = espacio.Publicado;
        }

        private void CargarMisEspacios()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(idUsuarioGestor);

            litSinEspacios.Visible = espacios.Count == 0;
            rptMisEspacios.DataSource = espacios;
            rptMisEspacios.DataBind();
        }

        private void CargarEspacioEnFormulario(int idEspacioArtistico)
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            EspacioArtistico espacio = _bllEspacio.ListarMisEspacios(idUsuarioGestor)
                .Find(e => e.IdEspacioArtistico == idEspacioArtistico);

            if (espacio == null)
            {
                MostrarMensaje("No se encontró el espacio seleccionado.", esError: true);
                return;
            }

            IdEspacioEnEdicion = espacio.IdEspacioArtistico;
            txtNombreEspacio.Text = espacio.NombreEspacio;
            txtTipoEspacio.Text = espacio.TipoEspacio;
            txtDescripcion.Text = espacio.Descripcion;
            litTituloFormulario.Text = "Editar espacio";
            lnkCancelarEdicion.Visible = true;
            btnGuardarEspacio.Text = "Guardar cambios";
        }

        private void LimpiarFormulario()
        {
            IdEspacioEnEdicion = null;
            txtNombreEspacio.Text = string.Empty;
            txtTipoEspacio.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            litTituloFormulario.Text = "Nuevo espacio";
            lnkCancelarEdicion.Visible = false;
            btnGuardarEspacio.Text = "Guardar espacio";
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
