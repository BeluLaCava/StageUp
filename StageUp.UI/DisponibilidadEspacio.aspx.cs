using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // CU-001-008 Gestionar disponibilidad de espacios artísticos.
    //  - Escenario principal: agregar disponibilidad (uno o varios días).
    //  - A1: el gestor no tiene espacios.
    //  - A2 a A5: validaciones (BLL_DisponibilidadEspacio).
    //  - A6 / A7: ver el detalle de una franja y editarla.
    //  - A8 / A9: bloqueo manual con motivo.
    //  - A10 / A11: eliminar una franja con confirmación.
    public partial class DisponibilidadEspacio : Page
    {
        private const string ModoSemanal = "Semanal";
        private const string ModoFecha = "Fecha";

        private readonly BLL_DisponibilidadEspacio _bll = new BLL_DisponibilidadEspacio();
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

        private EspacioArtistico _espacio;

        private int? IdEspacio
        {
            get { return ViewState["IdEspacio"] as int?; }
            set { ViewState["IdEspacio"] = value; }
        }

        private int? IdFranjaSeleccionada
        {
            get { return ViewState["IdFranjaSeleccionada"] as int?; }
            set { ViewState["IdFranjaSeleccionada"] = value; }
        }

        private int? IdFranjaEnEdicion
        {
            get { return ViewState["IdFranjaEnEdicion"] as int?; }
            set { ViewState["IdFranjaEnEdicion"] = value; }
        }

        private DateTime InicioSemana
        {
            get { return ViewState["InicioSemana"] is DateTime ? (DateTime)ViewState["InicioSemana"] : LunesDe(DateTime.Now.Date); }
            set { ViewState["InicioSemana"] = value; }
        }

        private int IdUsuario
        {
            get { return GestorDeSesion.ObtenerIdUsuarioActual().Value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            // Precondición: habilitado como gestor.
            if (GestorDeSesion.ObtenerPerfilActual() != PerfilUsuarioExterno.GestorEspacios.ToString())
            {
                Response.Redirect("~/OfrecerEspacio.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                CargarListasFijas();
                Inicializar();
            }
        }

        // ------------------------------------------------------------------
        // Carga
        // ------------------------------------------------------------------
        private void Inicializar()
        {
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(IdUsuario);

            // A1.
            if (espacios.Count == 0)
            {
                pnlSinEspacios.Visible = true;
                pnlContenido.Visible = false;
                litNombreEspacio.Text = "Definí los días y horarios en que tus espacios reciben reservas.";
                return;
            }

            ddlEspacio.Items.Clear();
            foreach (EspacioArtistico espacio in espacios)
            {
                ddlEspacio.Items.Add(new ListItem(espacio.NombreEspacio, espacio.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture)));
            }

            int id;
            if (!int.TryParse(Request.QueryString["id"], NumberStyles.Integer, CultureInfo.InvariantCulture, out id) ||
                ddlEspacio.Items.FindByValue(id.ToString(CultureInfo.InvariantCulture)) == null)
            {
                if (!string.IsNullOrEmpty(Request.QueryString["id"]))
                {
                    MostrarNoDisponible("El espacio indicado no existe, ya fue dado de baja o no lo administrás.");
                    return;
                }

                id = espacios[0].IdEspacioArtistico;
            }

            ddlEspacio.SelectedValue = id.ToString(CultureInfo.InvariantCulture);
            IdEspacio = id;
            InicioSemana = LunesDe(DateTime.Now.Date);
            Cargar();
        }

        private bool CargarEspacio()
        {
            if (!IdEspacio.HasValue)
            {
                return false;
            }

            ResultadoOperacion<EspacioArtistico> resultado = _bll.ObtenerEspacioDelGestor(IdEspacio.Value, IdUsuario);
            if (!resultado.Exitoso)
            {
                MostrarNoDisponible(resultado.Mensaje);
                return false;
            }

            _espacio = resultado.Valor;
            return true;
        }

        // Paso 9: nombre, calendario y listado.
        private void Cargar()
        {
            if (!CargarEspacio())
            {
                return;
            }

            pnlContenido.Visible = true;
            pnlNoDisponible.Visible = false;
            litNombreEspacio.Text = _espacio.NombreEspacio;
            lnkDetalleEspacio.NavigateUrl = "~/Explorar/DetalleEspacio.aspx?id=" + _espacio.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture);

            ResultadoOperacion<List<Reserva>> consulta = _bll.ConsultarOperacionesVigentes(_espacio.IdEspacioArtistico);
            List<Reserva> operaciones = consulta.Exitoso ? consulta.Valor : new List<Reserva>();
            if (!consulta.Exitoso)
            {
                MostrarMensaje(consulta.Mensaje, true);
            }

            CargarCalendario(operaciones);

            List<FranjaEspacio> disponibilidad = BLL_DisponibilidadEspacio.ListarDisponibilidad(_espacio);
            rptDisponibilidad.DataSource = disponibilidad;
            rptDisponibilidad.DataBind();
            litSinDisponibilidad.Visible = disponibilidad.Count == 0;

            List<FranjaEspacio> bloqueos = BLL_DisponibilidadEspacio.ListarBloqueos(_espacio);
            rptBloqueos.DataSource = bloqueos;
            rptBloqueos.DataBind();
            litSinBloqueos.Visible = bloqueos.Count == 0;

            pnlDetalle.Visible = false;
            if (IdFranjaSeleccionada.HasValue && !IdFranjaEnEdicion.HasValue)
            {
                FranjaEspacio seleccionada = BLL_DisponibilidadEspacio.BuscarFranjaManual(_espacio, IdFranjaSeleccionada.Value);
                if (seleccionada == null || seleccionada.Bloqueado)
                {
                    CerrarDetalle();
                }
                else
                {
                    MostrarDetalle(seleccionada, operaciones);
                }
            }
        }

        private void CargarCalendario(List<Reserva> operaciones)
        {
            DateTime inicio = InicioSemana;
            List<DiaCalendarioDisponibilidad> dias = BLL_DisponibilidadEspacio.ArmarCalendario(_espacio, operaciones, inicio, 7);
            CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
            litRangoSemana.Text = "Semana del " + inicio.ToString("d 'de' MMMM", cultura) + " al " + inicio.AddDays(6).ToString("d 'de' MMMM 'de' yyyy", cultura);
            lnkSemanaAnterior.Visible = inicio > LunesDe(DateTime.Now.Date);

            StringBuilder html = new StringBuilder();
            foreach (DiaCalendarioDisponibilidad dia in dias)
            {
                html.Append("<div class=\"da-dia").Append(dia.EsHoy ? " da-dia-hoy" : string.Empty).Append("\">");
                html.Append("<h3>").Append(Codificar(BLL_DisponibilidadEspacio.NombreDia(BLL_DisponibilidadEspacio.DiaSemanaDe(dia.Fecha))))
                    .Append("<span>").Append(Codificar(dia.Fecha.ToString("dd/MM", CultureInfo.InvariantCulture)))
                    .Append(dia.EsHoy ? " · hoy" : string.Empty)
                    .Append(dia.TieneExcepcion ? " · horario especial" : string.Empty)
                    .Append("</span></h3>");

                if (dia.Items.Count == 0)
                {
                    html.Append("<span class=\"da-dia-vacio\">Sin horarios</span>");
                }

                foreach (ItemCalendarioDisponibilidad item in dia.Items)
                {
                    string clase = "da-item da-item-" + item.Tipo.ToLowerInvariant();
                    string rango = BLL_DisponibilidadEspacio.FormatearHora(item.MinutoDesde) + " a " + BLL_DisponibilidadEspacio.FormatearHora(item.MinutoHasta);
                    if (item.IdReserva.HasValue)
                    {
                        html.Append("<a class=\"").Append(clase).Append("\" href=\"")
                            .Append(Codificar(ResolveUrl(BLL_Reserva.UrlDetalle(item.IdReserva.Value)))).Append("\">");
                    }
                    else
                    {
                        html.Append("<span class=\"").Append(clase).Append("\">");
                    }

                    html.Append("<b>").Append(Codificar(rango)).Append("</b>").Append(Codificar(item.Detalle));
                    html.Append(item.IdReserva.HasValue ? "</a>" : "</span>");
                }

                html.Append("</div>");
            }

            litCalendario.Text = html.ToString();
        }

        // ------------------------------------------------------------------
        // Selección de espacio y calendario
        // ------------------------------------------------------------------
        protected void ddlEspacio_SelectedIndexChanged(object sender, EventArgs e)
        {
            int id;
            if (int.TryParse(ddlEspacio.SelectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
            {
                IdEspacio = id;
            }

            OcultarFormularios();
            CerrarDetalle();
            pnlMensaje.Visible = false;
            Cargar();
        }

        protected void lnkSemanaAnterior_Click(object sender, EventArgs e)
        {
            DateTime anterior = InicioSemana.AddDays(-7);
            InicioSemana = anterior < LunesDe(DateTime.Now.Date) ? LunesDe(DateTime.Now.Date) : anterior;
            Cargar();
        }

        protected void lnkSemanaActual_Click(object sender, EventArgs e)
        {
            InicioSemana = LunesDe(DateTime.Now.Date);
            Cargar();
        }

        protected void lnkSemanaSiguiente_Click(object sender, EventArgs e)
        {
            if (InicioSemana < LunesDe(DateTime.Now.Date).AddDays(7 * 52))
            {
                InicioSemana = InicioSemana.AddDays(7);
            }

            Cargar();
        }

        // ------------------------------------------------------------------
        // Escenario principal y A6: formulario de disponibilidad
        // ------------------------------------------------------------------
        protected void btnAgregar_Click(object sender, EventArgs e)
        {
            OcultarFormularios();
            CerrarDetalle();
            pnlMensaje.Visible = false;
            IdFranjaEnEdicion = null;
            litTituloFormulario.Text = "Agregar disponibilidad";
            btnGuardarDisponibilidad.Text = "Guardar disponibilidad";
            rblModo.Enabled = true;
            rblModo.SelectedValue = ModoSemanal;
            cblDias.ClearSelection();
            txtFecha.Text = string.Empty;
            txtDesde.Text = "09:00";
            txtHasta.Text = "18:00";
            AplicarModo();
            pnlFormulario.Visible = true;
            Cargar();
        }

        protected void rblModo_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarModo();
            Cargar();
        }

        private void AplicarModo()
        {
            bool porFecha = rblModo.SelectedValue == ModoFecha;
            bool editando = IdFranjaEnEdicion.HasValue;
            pnlFecha.Visible = porFecha;
            pnlDiasSemana.Visible = !porFecha && !editando;
            pnlDiaUnico.Visible = !porFecha && editando;
            txtFecha.Attributes["min"] = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        protected void btnGuardarDisponibilidad_Click(object sender, EventArgs e)
        {
            bool porFecha = rblModo.SelectedValue == ModoFecha;
            ResultadoOperacion resultado;

            if (IdFranjaEnEdicion.HasValue)
            {
                int dia;
                int? diaSemana = int.TryParse(ddlDiaEdicion.SelectedValue, out dia) ? dia : (int?)null;
                resultado = _bll.ModificarDisponibilidad(IdEspacio.Value, IdUsuario, IdFranjaEnEdicion.Value,
                    diaSemana, txtFecha.Text, txtDesde.Text, txtHasta.Text);
            }
            else
            {
                List<int> dias = cblDias.Items.Cast<ListItem>()
                    .Where(item => item.Selected)
                    .Select(item => int.Parse(item.Value, CultureInfo.InvariantCulture))
                    .ToList();
                resultado = _bll.AgregarDisponibilidad(IdEspacio.Value, IdUsuario, porFecha, dias, txtFecha.Text, txtDesde.Text, txtHasta.Text);
            }

            if (!resultado.Exitoso)
            {
                // A2 a A5 / A7: el formulario queda con lo cargado para corregir.
                MostrarMensaje(resultado.Mensaje, true);
                AplicarModo();
                Cargar();
                return;
            }

            // Pasos 15 a 18 / A6 pasos 8 a 11.
            int? editada = IdFranjaEnEdicion;
            OcultarFormularios();
            MostrarMensaje(resultado.Mensaje, false);
            IdFranjaSeleccionada = editada;
            Cargar();
        }

        protected void lnkCancelarFormulario_Click(object sender, EventArgs e)
        {
            OcultarFormularios();
            pnlMensaje.Visible = false;
            Cargar();
        }

        // ------------------------------------------------------------------
        // A6 / A10: detalle, edición y eliminación de una franja
        // ------------------------------------------------------------------
        protected void rptDisponibilidad_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            if (e.CommandName != "Ver" || !int.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out id))
            {
                return;
            }

            OcultarFormularios();
            pnlMensaje.Visible = false;
            pnlConfirmarEliminar.Visible = false;
            IdFranjaSeleccionada = id;
            Cargar();
        }

        private void MostrarDetalle(FranjaEspacio franja, List<Reserva> operaciones)
        {
            OperacionesAsociadasFranja asociadas = BLL_DisponibilidadEspacio.ContarOperacionesAsociadas(_espacio, franja, operaciones);
            pnlDetalle.Visible = true;
            litDetalleCuando.Text = BLL_DisponibilidadEspacio.Describir(franja);
            litDetalleAceptadas.Text = asociadas.ReservasAceptadas.ToString(CultureInfo.InvariantCulture);
            litDetallePendientes.Text = asociadas.SolicitudesPendientes.ToString(CultureInfo.InvariantCulture);
            litDetalleActividades.Text = asociadas.Actividades.ToString(CultureInfo.InvariantCulture);
            litDetalleNota.Text = asociadas.HayAlguna
                ? "Mientras tenga operaciones o actividades asociadas, no se puede eliminar ni modificar de forma que las deje afuera."
                : "No tiene reservas, solicitudes ni actividades asociadas: podés editarla o eliminarla.";
        }

        protected void btnEditarFranja_Click(object sender, EventArgs e)
        {
            if (!CargarEspacio() || !IdFranjaSeleccionada.HasValue)
            {
                return;
            }

            FranjaEspacio franja = BLL_DisponibilidadEspacio.BuscarFranjaManual(_espacio, IdFranjaSeleccionada.Value);
            if (franja == null || franja.Bloqueado)
            {
                MostrarMensaje("La disponibilidad seleccionada ya no existe.", true);
                CerrarDetalle();
                Cargar();
                return;
            }

            // A6 paso 4: se habilitan los campos con los datos actuales.
            OcultarFormularios();
            pnlMensaje.Visible = false;
            IdFranjaEnEdicion = franja.IdFranjaEspacio;
            litTituloFormulario.Text = "Editar disponibilidad";
            btnGuardarDisponibilidad.Text = "Guardar cambios";
            bool porFecha = BLL_DisponibilidadEspacio.EsDeFechaConcreta(franja);
            rblModo.SelectedValue = porFecha ? ModoFecha : ModoSemanal;
            rblModo.Enabled = false;
            txtFecha.Text = porFecha ? franja.Fecha : string.Empty;
            ddlDiaEdicion.SelectedValue = (franja.DiaSemana ?? 1).ToString(CultureInfo.InvariantCulture);
            txtDesde.Text = BLL_DisponibilidadEspacio.HoraParaFormulario(franja.MinutoDesde);
            txtHasta.Text = BLL_DisponibilidadEspacio.HoraParaFormulario(franja.MinutoHasta);
            AplicarModo();
            pnlFormulario.Visible = true;
            Cargar();
        }

        protected void btnEliminarFranja_Click(object sender, EventArgs e)
        {
            OcultarFormularios();
            pnlMensaje.Visible = false;
            pnlConfirmarEliminar.Visible = true;
            Cargar();
        }

        protected void lnkCancelarEliminar_Click(object sender, EventArgs e)
        {
            pnlConfirmarEliminar.Visible = false;
            Cargar();
        }

        protected void btnConfirmarEliminar_Click(object sender, EventArgs e)
        {
            if (!IdFranjaSeleccionada.HasValue)
            {
                Cargar();
                return;
            }

            ResultadoOperacion resultado = _bll.EliminarFranja(IdEspacio.Value, IdUsuario, IdFranjaSeleccionada.Value);
            pnlConfirmarEliminar.Visible = false;
            if (resultado.Exitoso)
            {
                CerrarDetalle();
            }

            // A11: la disponibilidad mantiene su configuración y se informa por qué.
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            Cargar();
        }

        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            CerrarDetalle();
            Cargar();
        }

        private void CerrarDetalle()
        {
            IdFranjaSeleccionada = null;
            pnlDetalle.Visible = false;
            pnlConfirmarEliminar.Visible = false;
        }

        // ------------------------------------------------------------------
        // A8 / A9: bloqueo manual
        // ------------------------------------------------------------------
        protected void btnBloquear_Click(object sender, EventArgs e)
        {
            OcultarFormularios();
            CerrarDetalle();
            pnlMensaje.Visible = false;
            rblModoBloqueo.SelectedValue = ModoFecha;
            txtFechaBloqueo.Text = string.Empty;
            chkDiaCompleto.Checked = false;
            txtDesdeBloqueo.Text = string.Empty;
            txtHastaBloqueo.Text = string.Empty;
            txtMotivoBloqueo.Text = string.Empty;
            AplicarModoBloqueo();
            pnlBloqueo.Visible = true;
            Cargar();
        }

        protected void rblModoBloqueo_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarModoBloqueo();
            Cargar();
        }

        protected void chkDiaCompleto_CheckedChanged(object sender, EventArgs e)
        {
            AplicarModoBloqueo();
            Cargar();
        }

        private void AplicarModoBloqueo()
        {
            bool porFecha = rblModoBloqueo.SelectedValue == ModoFecha;
            pnlFechaBloqueo.Visible = porFecha;
            pnlDiaBloqueo.Visible = !porFecha;
            txtFechaBloqueo.Attributes["min"] = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            txtDesdeBloqueo.Enabled = !chkDiaCompleto.Checked;
            txtHastaBloqueo.Enabled = !chkDiaCompleto.Checked;
            if (chkDiaCompleto.Checked)
            {
                txtDesdeBloqueo.Text = "00:00";
                txtHastaBloqueo.Text = "00:00";
            }
        }

        protected void btnGuardarBloqueo_Click(object sender, EventArgs e)
        {
            bool porFecha = rblModoBloqueo.SelectedValue == ModoFecha;
            int dia;
            int? diaSemana = !porFecha && int.TryParse(ddlDiaBloqueo.SelectedValue, out dia) ? dia : (int?)null;
            string desde = chkDiaCompleto.Checked ? "00:00" : txtDesdeBloqueo.Text;
            string hasta = chkDiaCompleto.Checked ? "00:00" : txtHastaBloqueo.Text;

            ResultadoOperacion resultado = _bll.BloquearHorario(IdEspacio.Value, IdUsuario, porFecha, diaSemana,
                porFecha ? txtFechaBloqueo.Text : null, desde, hasta, txtMotivoBloqueo.Text);

            if (!resultado.Exitoso)
            {
                // A9: no se registra el bloqueo y se vuelve al paso 3 de A8.
                MostrarMensaje(resultado.Mensaje, true);
                AplicarModoBloqueo();
                Cargar();
                return;
            }

            OcultarFormularios();
            MostrarMensaje(resultado.Mensaje, false);
            Cargar();
        }

        protected void lnkCancelarBloqueo_Click(object sender, EventArgs e)
        {
            OcultarFormularios();
            pnlMensaje.Visible = false;
            Cargar();
        }

        protected void rptBloqueos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            if (e.CommandName != "Quitar" || !int.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out id))
            {
                return;
            }

            ResultadoOperacion resultado = _bll.EliminarFranja(IdEspacio.Value, IdUsuario, id);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            Cargar();
        }

        // ------------------------------------------------------------------
        // Ayudas para la vista
        // ------------------------------------------------------------------
        protected string Cuando(object dato)
        {
            return BLL_DisponibilidadEspacio.DescribirCuando((FranjaEspacio)dato);
        }

        protected string Horario(object dato)
        {
            FranjaEspacio franja = (FranjaEspacio)dato;
            if (franja.MinutoDesde == 0 && franja.MinutoHasta >= 1440)
            {
                return "Todo el día";
            }

            return BLL_DisponibilidadEspacio.FormatearHora(franja.MinutoDesde) + " a " + BLL_DisponibilidadEspacio.FormatearHora(franja.MinutoHasta);
        }

        protected string Motivo(object dato)
        {
            string motivo = ((FranjaEspacio)dato).MotivoBloqueo;
            return string.IsNullOrWhiteSpace(motivo) ? "Sin motivo informado" : "Motivo: " + motivo;
        }

        protected bool EsSeleccionada(object dato)
        {
            return IdFranjaSeleccionada.HasValue && ((FranjaEspacio)dato).IdFranjaEspacio == IdFranjaSeleccionada.Value;
        }

        private void CargarListasFijas()
        {
            cblDias.Items.Clear();
            ddlDiaEdicion.Items.Clear();
            ddlDiaBloqueo.Items.Clear();
            for (int dia = 1; dia <= 7; dia++)
            {
                string valor = dia.ToString(CultureInfo.InvariantCulture);
                string nombre = BLL_DisponibilidadEspacio.NombreDia(dia);
                cblDias.Items.Add(new ListItem(nombre, valor));
                ddlDiaEdicion.Items.Add(new ListItem(nombre, valor));
                ddlDiaBloqueo.Items.Add(new ListItem(nombre, valor));
            }
        }

        private void OcultarFormularios()
        {
            pnlFormulario.Visible = false;
            pnlBloqueo.Visible = false;
            IdFranjaEnEdicion = null;
        }

        private void MostrarNoDisponible(string mensaje)
        {
            pnlContenido.Visible = false;
            pnlNoDisponible.Visible = true;
            litNoDisponible.Text = mensaje;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private static DateTime LunesDe(DateTime fecha)
        {
            return fecha.Date.AddDays(1 - BLL_DisponibilidadEspacio.DiaSemanaDe(fecha));
        }

        private static string Codificar(string texto)
        {
            return HttpUtility.HtmlEncode(texto ?? string.Empty);
        }
    }
}
