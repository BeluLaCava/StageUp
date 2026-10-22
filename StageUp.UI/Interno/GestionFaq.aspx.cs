using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{




    public partial class GestionFaq : Page
    {
        private const string PermisoRequerido = "GESTIONAR_FAQ";

        private readonly BLL_Faq _bll = new BLL_Faq();

        private int IdEnDetalle
        {
            get { return ViewState["IdEnDetalle"] as int? ?? 0; }
            set { ViewState["IdEnDetalle"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarOpciones();
                CargarListado();
                CargarConsultas();
            }
        }



        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarListado();
        }

        protected void lnkLimpiar_Click(object sender, EventArgs e)
        {
            ddlFiltroEstado.ClearSelection();
            ddlFiltroCategoria.ClearSelection();
            txtFiltroTexto.Text = string.Empty;
            pnlMensaje.Visible = false;
            CargarListado();
        }

        protected void rptFaq_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            if (e.CommandName == "Ver" && int.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                pnlMensaje.Visible = false;
                MostrarDetalle(id);
            }
        }

        protected static string Fecha(object dato)
        {
            Faq faq = (Faq)dato;
            DateTime fecha = faq.FechaUltimaModificacion ?? faq.FechaAlta;
            return fecha == DateTime.MinValue ? "—" : fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }



        protected void btnAgregar_Click(object sender, EventArgs e)
        {
            AbrirFormulario(null, null);
        }

        protected void btnEditar_Click(object sender, EventArgs e)
        {
            Faq faq = _bll.ObtenerPorId(IdEnDetalle);
            if (faq == null)
            {
                MostrarMensaje("No se encontró el contenido de ayuda seleccionado.", true);
                VolverAlListado();
                return;
            }

            AbrirFormulario(faq, null);
        }

        protected void btnCancelarFormulario_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            int id;

            if (int.TryParse(hfIdFaq.Value, out id) && id > 0)
            {
                MostrarDetalle(id);
                return;
            }

            VolverAlListado();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {

            string estadoOriginal = Convert.ToString(ViewState["EstadoOriginal"]);
            if (ddlEstado.SelectedValue == BLL_Faq.EstadoActivo && estadoOriginal != BLL_Faq.EstadoActivo)
            {
                pnlMensaje.Visible = false;
                pnlConfirmarGuardar.Visible = true;
                btnGuardar.Visible = false;
                return;
            }

            Guardar();
        }

        protected void btnConfirmarGuardar_Click(object sender, EventArgs e)
        {
            pnlConfirmarGuardar.Visible = false;
            btnGuardar.Visible = true;
            Guardar();
        }


        protected void btnCancelarGuardar_Click(object sender, EventArgs e)
        {
            pnlConfirmarGuardar.Visible = false;
            btnGuardar.Visible = true;
        }

        private void Guardar()
        {
            int orden;
            int.TryParse(txtOrden.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out orden);
            int idCategoria;
            Faq datos = new Faq
            {
                Pregunta = txtPregunta.Text,
                Respuesta = txtRespuesta.Text,
                Orden = orden,
                Estado = ddlEstado.SelectedValue,
                IdCategoriaAyuda = int.TryParse(ddlCategoria.SelectedValue, out idCategoria) ? idCategoria : (int?)null,
                EsBasica = chkBasica.Checked,
                PalabrasClave = txtPalabrasClave.Text
            };

            int idFaq;
            int idResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            if (int.TryParse(hfIdFaq.Value, out idFaq) && idFaq > 0)
            {
                datos.IdFaq = idFaq;
                ResultadoOperacion resultado = _bll.Modificar(datos, idResponsable);
                if (!resultado.Exitoso)
                {

                    MostrarMensaje(resultado.Mensaje, true);
                    return;
                }

                MostrarDetalle(idFaq);
                MostrarMensaje(resultado.Mensaje, false);
            }
            else
            {
                ResultadoOperacion<int> resultado = _bll.Registrar(datos, idResponsable, hfConsultaOrigen.Value);
                if (!resultado.Exitoso)
                {
                    MostrarMensaje(resultado.Mensaje, true);
                    return;
                }


                VolverAlListado();
                CargarConsultas();
                MostrarMensaje(resultado.Mensaje, false);
            }
        }




        protected void btnPublicar_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            pnlConfirmarDesactivar.Visible = false;
            pnlConfirmarPublicar.Visible = true;
            btnPublicar.Visible = false;
        }

        protected void btnConfirmarPublicar_Click(object sender, EventArgs e)
        {
            ResultadoOperacion resultado = _bll.Publicar(IdEnDetalle, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
            MostrarDetalle(IdEnDetalle);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }


        protected void btnCancelarPublicar_Click(object sender, EventArgs e)
        {
            MostrarDetalle(IdEnDetalle);
            MostrarMensaje("No se publicó: el contenido mantiene su estado.", false);
        }

        protected void btnDesactivar_Click(object sender, EventArgs e)
        {

            pnlMensaje.Visible = false;
            pnlConfirmarPublicar.Visible = false;
            pnlConfirmarDesactivar.Visible = true;
            btnDesactivar.Visible = false;
        }

        protected void btnConfirmarDesactivar_Click(object sender, EventArgs e)
        {
            ResultadoOperacion resultado = _bll.Desactivar(IdEnDetalle, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
            MostrarDetalle(IdEnDetalle);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }

        protected void btnCancelarDesactivar_Click(object sender, EventArgs e)
        {
            MostrarDetalle(IdEnDetalle);
        }

        protected void lnkVolverListado_Click(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            VolverAlListado();
        }



        protected void rptConsultas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string argumento = Convert.ToString(e.CommandArgument);
            if (e.CommandName == "Descartar")
            {
                ResultadoOperacion resultado = _bll.DescartarConsultaSinRespuesta(argumento, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
                MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
                CargarConsultas();
                return;
            }

            if (e.CommandName == "Crear")
            {
                int separador = argumento.IndexOf('|');
                string normalizada = separador < 0 ? argumento : argumento.Substring(0, separador);
                string texto = separador < 0 ? argumento : argumento.Substring(separador + 1);
                pnlMensaje.Visible = false;
                AbrirFormulario(null, normalizada);
                txtPregunta.Text = PrimeraMayuscula(texto);
            }
        }



        private void AbrirFormulario(Faq faq, string consultaOrigen)
        {
            List<CategoriaAyuda> categorias = _bll.ListarCategorias().Where(c => c.Activa).ToList();
            ddlCategoria.Items.Clear();
            ddlCategoria.Items.Add(new ListItem("Elegí una categoría", string.Empty));
            foreach (CategoriaAyuda categoria in categorias)
            {
                ddlCategoria.Items.Add(new ListItem(categoria.Nombre, categoria.IdCategoriaAyuda.ToString(CultureInfo.InvariantCulture)));
            }


            pnlSinCategorias.Visible = categorias.Count == 0;
            ddlEstado.Items.Clear();
            ddlEstado.Items.Add(new ListItem("Borrador (no se muestra)", BLL_Faq.EstadoBorrador));
            if (categorias.Count > 0)
            {
                ddlEstado.Items.Add(new ListItem("Publicado (activo)", BLL_Faq.EstadoActivo));
            }

            if (faq != null && faq.Estado == BLL_Faq.EstadoInactivo)
            {
                ddlEstado.Items.Add(new ListItem("Desactivado", BLL_Faq.EstadoInactivo));
            }

            hfIdFaq.Value = faq == null ? string.Empty : faq.IdFaq.ToString(CultureInfo.InvariantCulture);
            hfConsultaOrigen.Value = consultaOrigen ?? string.Empty;
            litTituloFormulario.Text = faq == null ? "Agregar contenido de ayuda" : "Editar contenido de ayuda";
            txtPregunta.Text = faq == null ? string.Empty : faq.Pregunta;
            txtRespuesta.Text = faq == null ? string.Empty : faq.Respuesta;
            txtOrden.Text = (faq == null ? 10 : faq.Orden).ToString(CultureInfo.InvariantCulture);
            txtPalabrasClave.Text = faq == null ? string.Empty : faq.PalabrasClave;
            chkBasica.Checked = faq != null && faq.EsBasica;
            ViewState["EstadoOriginal"] = faq == null ? string.Empty : faq.Estado;
            pnlConfirmarGuardar.Visible = false;
            btnGuardar.Visible = true;
            Seleccionar(ddlCategoria, faq == null || !faq.IdCategoriaAyuda.HasValue ? null : faq.IdCategoriaAyuda.Value.ToString(CultureInfo.InvariantCulture));
            Seleccionar(ddlEstado, faq == null ? BLL_Faq.EstadoBorrador : faq.Estado);

            pnlFormulario.Visible = true;
            pnlDetalle.Visible = false;
            pnlListado.Visible = false;
            pnlConsultas.Visible = false;
        }

        private void MostrarDetalle(int idFaq)
        {
            Faq faq = _bll.ObtenerPorId(idFaq);
            if (faq == null)
            {
                MostrarMensaje("No se encontró el contenido de ayuda seleccionado.", true);
                VolverAlListado();
                return;
            }

            IdEnDetalle = idFaq;
            litDetallePregunta.Text = faq.Pregunta;
            lblDetalleEstado.Text = Server.HtmlEncode(BLL_Faq.DescribirEstado(faq.Estado));
            lblDetalleEstado.CssClass = "faq-estado faq-estado-" + faq.Estado;
            litDetalleRespuesta.Text = faq.Respuesta;
            litDetalleCategoria.Text = faq.NombreCategoria ?? "Sin categoría";
            litDetalleBasica.Text = faq.EsBasica ? "Pregunta básica: aparece en la lista del centro de ayuda" : "No aparece en la lista del centro de ayuda (sí en la búsqueda, si está publicada)";
            litDetalleOrden.Text = faq.Orden.ToString(CultureInfo.InvariantCulture);
            litDetallePalabras.Text = string.IsNullOrWhiteSpace(faq.PalabrasClave) ? "—" : faq.PalabrasClave;
            litDetalleAlta.Text = faq.FechaAlta == DateTime.MinValue ? "—" : faq.FechaAlta.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            litDetalleActualizacion.Text = Fecha(faq);

            pnlAvisoEstado.Visible = faq.Estado != BLL_Faq.EstadoActivo;
            litAvisoEstado.Text = faq.Estado == BLL_Faq.EstadoInactivo
                ? "Este contenido está desactivado: no lo ven los usuarios. Podés editarlo y volver a publicarlo."
                : "Este contenido es un borrador: todavía no lo ven los usuarios.";

            btnPublicar.Visible = faq.Estado != BLL_Faq.EstadoActivo;
            btnPublicar.Text = faq.Estado == BLL_Faq.EstadoInactivo ? "Publicar de nuevo" : "Publicar contenido";
            btnDesactivar.Visible = faq.Estado == BLL_Faq.EstadoActivo;
            pnlConfirmarDesactivar.Visible = false;
            pnlConfirmarPublicar.Visible = false;
            litConfirmarBasica.Visible = faq.EsBasica;

            pnlDetalle.Visible = true;
            pnlFormulario.Visible = false;
            pnlListado.Visible = false;
            pnlConsultas.Visible = false;
        }

        private void VolverAlListado()
        {
            pnlFormulario.Visible = false;
            pnlDetalle.Visible = false;
            pnlListado.Visible = true;
            pnlConsultas.Visible = true;
            CargarListado();
        }

        private void CargarOpciones()
        {
            ddlFiltroEstado.Items.Clear();
            ddlFiltroEstado.Items.Add(new ListItem("Todos", string.Empty));
            ddlFiltroEstado.Items.Add(new ListItem("Publicados", BLL_Faq.EstadoActivo));
            ddlFiltroEstado.Items.Add(new ListItem("Borradores", BLL_Faq.EstadoBorrador));
            ddlFiltroEstado.Items.Add(new ListItem("Desactivados", BLL_Faq.EstadoInactivo));

            ddlFiltroCategoria.Items.Clear();
            ddlFiltroCategoria.Items.Add(new ListItem("Todas", string.Empty));
            foreach (CategoriaAyuda categoria in _bll.ListarCategorias())
            {
                ddlFiltroCategoria.Items.Add(new ListItem(categoria.Nombre, categoria.IdCategoriaAyuda.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void CargarListado()
        {
            List<Faq> todos = _bll.Listar();
            litCantidadActivos.Text = todos.Count(f => f.Estado == BLL_Faq.EstadoActivo).ToString(CultureInfo.InvariantCulture);
            litCantidadBorradores.Text = todos.Count(f => f.Estado == BLL_Faq.EstadoBorrador).ToString(CultureInfo.InvariantCulture);
            litCantidadInactivos.Text = todos.Count(f => f.Estado == BLL_Faq.EstadoInactivo).ToString(CultureInfo.InvariantCulture);
            litCantidadBasicas.Text = todos.Count(f => f.Estado == BLL_Faq.EstadoActivo && f.EsBasica).ToString(CultureInfo.InvariantCulture);

            int idCategoria;
            FiltroFaq filtro = new FiltroFaq
            {
                Estado = ddlFiltroEstado.SelectedValue,
                IdCategoriaAyuda = int.TryParse(ddlFiltroCategoria.SelectedValue, out idCategoria) ? idCategoria : (int?)null,
                Texto = txtFiltroTexto.Text
            };
            bool hayFiltros = !string.IsNullOrEmpty(filtro.Estado) || filtro.IdCategoriaAyuda.HasValue || !string.IsNullOrWhiteSpace(filtro.Texto);
            List<Faq> lista = hayFiltros ? _bll.Listar(filtro) : todos;

            rptFaq.DataSource = lista;
            rptFaq.DataBind();
            rptFaq.Visible = lista.Count > 0;
            pnlSinContenido.Visible = lista.Count == 0;
            litSinContenido.Text = todos.Count == 0 ? "No hay contenidos de ayuda cargados" : "No hay contenidos para los filtros aplicados";
        }

        private void CargarConsultas()
        {
            List<ConsultaAsistenteSinRespuesta> consultas = _bll.ListarConsultasSinRespuesta();
            rptConsultas.DataSource = consultas;
            rptConsultas.DataBind();
            rptConsultas.Visible = consultas.Count > 0;
            pnlSinConsultas.Visible = consultas.Count == 0;
        }

        private static void Seleccionar(DropDownList lista, string valor)
        {
            lista.ClearSelection();
            ListItem item = valor == null ? null : lista.Items.FindByValue(valor);
            if (item != null)
            {
                item.Selected = true;
            }
        }

        private static string PrimeraMayuscula(string texto)
        {
            string limpio = (texto ?? string.Empty).Trim();
            return limpio.Length == 0 ? limpio : char.ToUpper(limpio[0], CultureInfo.CurrentCulture) + limpio.Substring(1);
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }


        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
