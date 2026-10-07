using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class MisActividades : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_Actividad _bllActividad = new BLL_Actividad();
        private readonly BLL_Participante _bllParticipante = new BLL_Participante();

        // Misma convención que FranjaEspacio.DiaSemana / BLL_Reserva: 1 = lunes ... 7 = domingo.
        private static readonly Dictionary<string, int> DiaCodigoANumero = new Dictionary<string, int>
        {
            { "Lun", 1 }, { "Mar", 2 }, { "Mié", 3 }, { "Jue", 4 }, { "Vie", 5 }, { "Sáb", 6 }, { "Dom", 7 }
        };

        private static readonly Dictionary<int, string> NumeroADiaCodigo =
            DiaCodigoANumero.ToDictionary(par => par.Value, par => par.Key);

        private static readonly Dictionary<string, int> SemanaDelMesCodigoANumero = new Dictionary<string, int>
        {
            { "first", 1 }, { "second", 2 }, { "third", 3 }, { "fourth", 4 }, { "last", 5 }
        };

        private static readonly Dictionary<int, string> SemanaDelMesTexto = new Dictionary<int, string>
        {
            { 1, "1ra" }, { 2, "2da" }, { 3, "3ra" }, { 4, "4ta" }, { 5, "última" }
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            string perfil = GestorDeSesion.ObtenerPerfilActual();

            // CU-001-007 (A1, A3 a A6): quien todavía no es gestor pide la
            // habilitación o consulta su estado en "Ofrecer espacio".
            if (perfil != PerfilUsuarioExterno.GestorEspacios.ToString())
            {
                Response.Redirect("~/OfrecerEspacio.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            pnlPanelGestor.Visible = true;
            lnkNuevaActividad.Visible = true;

            if (pnlPanelGestor.Visible && !IsPostBack)
            {
                CargarFiltroEspacios();

                // CU-001-009 paso 7: llegada desde el detalle del espacio.
                int idEspacio;
                if (int.TryParse(Request.QueryString["espacio"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idEspacio) &&
                    ddlFiltroEspacio.Items.FindByValue(idEspacio.ToString(CultureInfo.InvariantCulture)) != null)
                {
                    ddlFiltroEspacio.SelectedValue = idEspacio.ToString(CultureInfo.InvariantCulture);
                }

                CargarPantallaGestor();

                int idActividad;
                if (int.TryParse(Request.QueryString["ver"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idActividad))
                {
                    MostrarDetalle(idActividad);
                }
                else if (int.TryParse(Request.QueryString["editar"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idActividad))
                {
                    AbrirEdicion(idActividad);
                }
                else if (int.TryParse(Request.QueryString["participante"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idActividad))
                {
                    hdnTabActividades.Value = "participantes";
                    MostrarDetalleParticipante(idActividad);
                }
            }
        }

        private int? IdActividadEnEdicion
        {
            get { return ViewState["IdActividadEnEdicion"] as int?; }
            set { ViewState["IdActividadEnEdicion"] = value; }
        }

        private int? IdActividadEnDetalle
        {
            get { return ViewState["IdActividadEnDetalle"] as int?; }
            set { ViewState["IdActividadEnDetalle"] = value; }
        }

        protected void Filtros_Changed(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarPantallaGestor();
        }

        protected void lnkNuevaActividad_Click(object sender, EventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisActividades.aspx");
                return;
            }

            CargarEspaciosEnSelector();
            CargarSugerenciasParticipantes();
            LimpiarFormulario();
            IdActividadEnEdicion = null;
            litTituloActividad.Text = "Nueva actividad interna";
            btnGuardarActividad.Text = "Guardar actividad";
            if (!string.IsNullOrEmpty(ddlFiltroEspacio.SelectedValue) && ddlEspacio.Items.FindByValue(ddlFiltroEspacio.SelectedValue) != null)
            {
                ddlEspacio.SelectedValue = ddlFiltroEspacio.SelectedValue;
            }

            pnlMensaje.Visible = false;
            pnlDetalleActividad.Visible = false;
            pnlFormularioActividad.Visible = true;
        }

        protected void lnkCerrarActividad_Click(object sender, EventArgs e)
        {
            pnlFormularioActividad.Visible = false;

            // Al cancelar una edición se vuelve al detalle (A7).
            if (IdActividadEnEdicion.HasValue)
            {
                int id = IdActividadEnEdicion.Value;
                IdActividadEnEdicion = null;
                MostrarDetalle(id);
            }
        }

        protected void btnGuardarActividad_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                pnlFormularioActividad.Visible = true;
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ProgramacionActividadDto programacion = ObtenerProgramacion();

            Actividad actividad = new Actividad
            {
                IdActividad = IdActividadEnEdicion ?? 0,
                IdEspacioArtistico = ConvertirEntero(ddlEspacio.SelectedValue),
                Nombre = txtNombreActividad.Text.Trim(),
                Tipo = string.IsNullOrWhiteSpace(txtTipoActividad.Text) ? null : txtTipoActividad.Text.Trim(),
                MinutoDesde = ConvertirHoraAMinutos(txtHoraInicio.Text, false),
                MinutoHasta = ConvertirHoraAMinutos(txtHoraFin.Text, true),
                CupoMaximo = string.IsNullOrWhiteSpace(txtCupoMaximo.Text) ? 0 : ConvertirEnteroOInvalido(txtCupoMaximo.Text),
                ParticipantesEstimados = string.IsNullOrWhiteSpace(txtParticipantes.Text) ? (int?)null : ConvertirEnteroOInvalido(txtParticipantes.Text),
                Notas = string.IsNullOrWhiteSpace(txtDescripcionActividad.Text) ? null : txtDescripcionActividad.Text.Trim()
            };

            string modo = programacion == null ? null : programacion.Modo;
            if (modo == "weekly")
            {
                actividad.ModoRecurrencia = ModoRecurrenciaActividad.Semanal.ToString();
                actividad.DiasSemana = cblDias.Items.Cast<ListItem>()
                    .Where(item => item.Selected && DiaCodigoANumero.ContainsKey(item.Value))
                    .Select(item => DiaCodigoANumero[item.Value])
                    .ToList();
            }
            else if (modo == "monthly")
            {
                actividad.ModoRecurrencia = ModoRecurrenciaActividad.Mensual.ToString();
                actividad.SemanaDelMes = programacion.SemanaDelMes != null && SemanaDelMesCodigoANumero.ContainsKey(programacion.SemanaDelMes)
                    ? SemanaDelMesCodigoANumero[programacion.SemanaDelMes]
                    : (int?)null;
                actividad.DiaSemanaMensual = programacion.DiaDelMes != null && DiaCodigoANumero.ContainsKey(programacion.DiaDelMes)
                    ? DiaCodigoANumero[programacion.DiaDelMes]
                    : (int?)null;
            }
            else if (modo == "date")
            {
                actividad.ModoRecurrencia = ModoRecurrenciaActividad.Fecha.ToString();
                actividad.Fecha = programacion.Fecha;
            }

            List<int> idsParticipantes = new List<int>();
            foreach (string idTexto in (hdnParticipantesActividad.Value ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int idParticipante;
                if (int.TryParse(idTexto, out idParticipante))
                {
                    idsParticipantes.Add(idParticipante);
                }
            }

            ResultadoOperacion<int> resultado = _bllActividad.Guardar(actividad, idUsuarioGestor, idsParticipantes);
            if (!resultado.Exitoso)
            {
                // A2 a A5 / A8: el formulario queda con lo cargado.
                MostrarMensaje(resultado.Mensaje, esError: true);
                CargarSugerenciasParticipantes();
                pnlFormularioActividad.Visible = true;
                return;
            }

            bool eraEdicion = IdActividadEnEdicion.HasValue;
            IdActividadEnEdicion = null;
            pnlFormularioActividad.Visible = false;
            CargarPantallaGestor();
            MostrarMensaje(resultado.Mensaje, esError: false);

            // A7 paso 9: después de editar se vuelve a mostrar el detalle.
            if (eraEdicion)
            {
                MostrarDetalle(resultado.Valor);
                MostrarMensajeDetalle(resultado.Mensaje, false);
            }
        }

        protected void rptActividades_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idActividad = ConvertirEntero(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture));
            pnlMensaje.Visible = false;

            if (e.CommandName == "Ver")
            {
                MostrarDetalle(idActividad);
            }
            else if (e.CommandName == "Editar")
            {
                AbrirEdicion(idActividad);
            }
            else if (e.CommandName == "Baja")
            {
                // A9 paso 2: la baja se confirma desde el detalle.
                if (MostrarDetalle(idActividad))
                {
                    pnlConfirmarBajaActividad.Visible = true;
                }
            }
        }

        // ------------------------------------------------------------------
        // A6: detalle de la actividad
        // ------------------------------------------------------------------
        private bool MostrarDetalle(int idActividad)
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            Actividad actividad = _bllActividad.ObtenerDetalle(idActividad, idUsuarioGestor);
            if (actividad == null)
            {
                pnlDetalleActividad.Visible = false;
                IdActividadEnDetalle = null;
                MostrarMensaje("No se encontró la actividad indicada.", esError: true);
                return false;
            }

            IdActividadEnDetalle = actividad.IdActividad;
            pnlFormularioActividad.Visible = false;
            pnlDetalleActividad.Visible = true;
            pnlConfirmarBajaActividad.Visible = false;
            pnlDetalleMensaje.Visible = false;

            CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
            litDetalleNombre.Text = actividad.Nombre;
            lblDetalleEstado.Text = actividad.Activa ? "Activa" : "Dada de baja";
            lblDetalleEstado.CssClass = "activities-status " + (actividad.Activa ? "activities-status-active" : "activities-status-inactive");
            litDetalleTipo.Text = string.IsNullOrEmpty(actividad.Tipo) ? string.Empty : " · " + actividad.Tipo;
            litDetalleEspacio.Text = actividad.NombreEspacio ?? "-";
            litDetalleProgramacion.Text = BLL_Actividad.DescribirProgramacion(actividad);
            litDetalleHorario.Text = BLL_Actividad.FormatearHorario(actividad.MinutoDesde, actividad.MinutoHasta);
            litDetalleDuracion.Text = BLL_Actividad.FormatearDuracion(actividad.DuracionMinutos);
            litDetalleCupo.Text = actividad.CupoMaximo + " participantes";
            litDetalleEstimados.Text = actividad.ParticipantesEstimados.HasValue ? actividad.ParticipantesEstimados.Value.ToString(CultureInfo.InvariantCulture) : "Sin indicar";
            litDetalleNotas.Text = string.IsNullOrEmpty(actividad.Notas) ? "Sin notas" : actividad.Notas;
            litDetalleFechas.Text = "Creada el " + actividad.FechaCreacion.ToString("dd/MM/yyyy HH:mm", cultura) +
                (actividad.FechaUltimaModificacion.HasValue
                    ? " · última modificación el " + actividad.FechaUltimaModificacion.Value.ToString("dd/MM/yyyy HH:mm", cultura)
                    : string.Empty);

            if (actividad.Activa)
            {
                List<DateTime> proximas = BLL_Actividad.ProximasFechas(actividad, 6);
                litDetalleProximas.Text = proximas.Count == 0
                    ? "No tiene fechas próximas."
                    : string.Join(" · ", proximas.Select(f => f.ToString("ddd dd/MM", cultura)));
            }
            else
            {
                litDetalleProximas.Text = "Ninguna: la actividad está dada de baja y ya no bloquea el espacio.";
            }

            litDetalleCantidadParticipantes.Text = "(" + actividad.Participantes.Count + " de " + actividad.CupoMaximo + ")";
            litDetalleSinParticipantes.Visible = actividad.Participantes.Count == 0;
            PuedeGestionarParticipantesDeActividad = actividad.Activa;
            rptDetalleParticipantes.DataSource = actividad.Participantes;
            rptDetalleParticipantes.DataBind();
            pnlConfirmarDesvincularActividad.Visible = false;
            pnlDetalleParticipante.Visible = false;

            // CU-001-010 pasos 13 y 14 y A7: agregar o asociar participantes.
            pnlParticipantesActividad.Visible = actividad.Activa;
            if (actividad.Activa)
            {
                List<Participante> disponibles = _bllParticipante.ListarPorUsuarioGestor(GestorDeSesion.ObtenerIdUsuarioActual().Value)
                    .Where(p => !actividad.Participantes.Exists(a => a.IdParticipante == p.IdParticipante))
                    .ToList();
                ddlAsociarExistente.Items.Clear();
                ddlAsociarExistente.Items.Add(new ListItem(disponibles.Count == 0 ? "No hay otros participantes activos" : "Elegí un participante", string.Empty));
                foreach (Participante disponible in disponibles)
                {
                    ddlAsociarExistente.Items.Add(new ListItem(disponible.NombreCompleto + " (DNI " + disponible.Dni + ")",
                        disponible.IdParticipante.ToString(CultureInfo.InvariantCulture)));
                }

                ddlAsociarExistente.Enabled = disponibles.Count > 0;
                btnAsociarExistente.Enabled = disponibles.Count > 0;
            }

            btnEditarDesdeDetalle.Visible = actividad.Activa;
            btnBajaDesdeDetalle.Visible = actividad.Activa;
            return true;
        }

        protected void lnkCerrarDetalle_Click(object sender, EventArgs e)
        {
            pnlDetalleActividad.Visible = false;
            IdActividadEnDetalle = null;
        }

        protected void btnEditarDesdeDetalle_Click(object sender, EventArgs e)
        {
            if (IdActividadEnDetalle.HasValue)
            {
                AbrirEdicion(IdActividadEnDetalle.Value);
            }
        }

        protected void btnBajaDesdeDetalle_Click(object sender, EventArgs e)
        {
            if (IdActividadEnDetalle.HasValue && MostrarDetalle(IdActividadEnDetalle.Value))
            {
                pnlConfirmarBajaActividad.Visible = true;
            }
        }

        // A11: se cancela la baja y se vuelve al detalle.
        protected void lnkCancelarBajaActividad_Click(object sender, EventArgs e)
        {
            if (IdActividadEnDetalle.HasValue)
            {
                MostrarDetalle(IdActividadEnDetalle.Value);
            }
        }

        protected void btnConfirmarBajaActividad_Click(object sender, EventArgs e)
        {
            if (!IdActividadEnDetalle.HasValue)
            {
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            int idActividad = IdActividadEnDetalle.Value;
            ResultadoOperacion resultado = _bllActividad.DarDeBaja(idActividad, idUsuarioGestor);
            if (!resultado.Exitoso)
            {
                // A10: la actividad mantiene su estado y se vuelve al detalle.
                MostrarDetalle(idActividad);
                MostrarMensajeDetalle(resultado.Mensaje, true);
                return;
            }

            // A9 pasos 9 y 10.
            pnlDetalleActividad.Visible = false;
            IdActividadEnDetalle = null;
            CargarPantallaGestor();
            MostrarMensaje(resultado.Mensaje, esError: false);
        }

        // ------------------------------------------------------------------
        // A7: edición con los datos actuales
        // ------------------------------------------------------------------
        private void AbrirEdicion(int idActividad)
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            Actividad actividad = _bllActividad.ObtenerDetalle(idActividad, idUsuarioGestor);
            if (actividad == null || !actividad.Activa)
            {
                MostrarMensaje(actividad == null ? "No se encontró la actividad indicada." : "La actividad está dada de baja: no se puede modificar.", esError: true);
                return;
            }

            CargarEspaciosEnSelector();
            CargarSugerenciasParticipantes();
            LimpiarFormulario();

            IdActividadEnEdicion = actividad.IdActividad;
            litTituloActividad.Text = "Editar actividad interna";
            btnGuardarActividad.Text = "Guardar cambios";

            if (ddlEspacio.Items.FindByValue(actividad.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture)) != null)
            {
                ddlEspacio.SelectedValue = actividad.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture);
            }

            txtNombreActividad.Text = actividad.Nombre;
            txtTipoActividad.Text = actividad.Tipo;
            txtHoraInicio.Text = FormatearHora(actividad.MinutoDesde);
            txtHoraFin.Text = actividad.MinutoHasta >= 1440 ? "00:00" : FormatearHora(actividad.MinutoHasta);
            txtCupoMaximo.Text = actividad.CupoMaximo.ToString(CultureInfo.InvariantCulture);
            txtParticipantes.Text = actividad.ParticipantesEstimados.HasValue ? actividad.ParticipantesEstimados.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
            txtDescripcionActividad.Text = actividad.Notas;

            string modo = actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString() ? "monthly"
                : actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString() ? "date" : "weekly";
            foreach (ListItem item in cblDias.Items)
            {
                item.Selected = modo == "weekly" && DiaCodigoANumero.ContainsKey(item.Value) &&
                    actividad.DiasSemana.Contains(DiaCodigoANumero[item.Value]);
            }

            string semana = actividad.SemanaDelMes.HasValue
                ? SemanaDelMesCodigoANumero.FirstOrDefault(par => par.Value == actividad.SemanaDelMes.Value).Key
                : null;
            string diaMes = actividad.DiaSemanaMensual.HasValue && NumeroADiaCodigo.ContainsKey(actividad.DiaSemanaMensual.Value)
                ? NumeroADiaCodigo[actividad.DiaSemanaMensual.Value]
                : null;
            hdnProgramacionActividad.Value = new JavaScriptSerializer().Serialize(new
            {
                modo = modo,
                fecha = actividad.Fecha ?? string.Empty,
                semanaDelMes = semana ?? string.Empty,
                diaDelMes = diaMes ?? string.Empty
            });
            hdnParticipantesActividad.Value = string.Join(",",
                actividad.Participantes.Select(p => p.IdParticipante.ToString(CultureInfo.InvariantCulture)));

            pnlMensaje.Visible = false;
            pnlDetalleActividad.Visible = false;
            pnlFormularioActividad.Visible = true;
        }

        // ------------------------------------------------------------------
        // CU-001-010: participantes
        // ------------------------------------------------------------------
        private int? IdParticipanteEnEdicion
        {
            get { return ViewState["IdParticipanteEnEdicion"] as int?; }
            set { ViewState["IdParticipanteEnEdicion"] = value; }
        }

        private int? IdParticipanteEnDetalle
        {
            get { return ViewState["IdParticipanteEnDetalle"] as int?; }
            set { ViewState["IdParticipanteEnDetalle"] = value; }
        }

        // Actividad desde cuyo detalle se abrió el formulario (paso 14): al
        // guardar se asocia y se vuelve a ese detalle (pasos 20 a 24).
        private int? IdActividadOrigenParticipante
        {
            get { return ViewState["IdActividadOrigenParticipante"] as int?; }
            set { ViewState["IdActividadOrigenParticipante"] = value; }
        }

        // A5: participante activo con el mismo DNI.
        private int? IdParticipanteExistente
        {
            get { return ViewState["IdParticipanteExistente"] as int?; }
            set { ViewState["IdParticipanteExistente"] = value; }
        }

        // A13: qué se desvincula y desde dónde se pidió.
        private int? IdActividadDesvincular
        {
            get { return ViewState["IdActividadDesvincular"] as int?; }
            set { ViewState["IdActividadDesvincular"] = value; }
        }

        private int? IdParticipanteDesvincular
        {
            get { return ViewState["IdParticipanteDesvincular"] as int?; }
            set { ViewState["IdParticipanteDesvincular"] = value; }
        }

        private string OrigenDesvincular
        {
            get { return ViewState["OrigenDesvincular"] as string; }
            set { ViewState["OrigenDesvincular"] = value; }
        }

        protected bool PuedeGestionarParticipantesDeActividad { get; private set; }

        protected bool ParticipanteEnDetalleActivo { get; private set; }

        protected void lnkNuevoParticipante_Click(object sender, EventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisActividades.aspx");
                return;
            }

            hdnTabActividades.Value = "participantes";
            AbrirFormularioParticipante(null);
        }

        // Paso 14: "Agregar participante" desde el detalle de la actividad.
        protected void btnAgregarParticipanteActividad_Click(object sender, EventArgs e)
        {
            if (IdActividadEnDetalle.HasValue)
            {
                AbrirFormularioParticipante(IdActividadEnDetalle.Value);
            }
        }

        private void AbrirFormularioParticipante(int? idActividadOrigen)
        {
            CargarActividadesEnSelector();
            LimpiarFormularioParticipante();
            IdParticipanteEnEdicion = null;
            IdActividadOrigenParticipante = idActividadOrigen;
            litTituloParticipante.Text = "Nuevo participante";
            btnGuardarParticipante.Text = "Guardar participante";
            pnlAsociarActividadParticipante.Visible = true;
            if (idActividadOrigen.HasValue &&
                ddlActividadParticipante.Items.FindByValue(idActividadOrigen.Value.ToString(CultureInfo.InvariantCulture)) != null)
            {
                ddlActividadParticipante.SelectedValue = idActividadOrigen.Value.ToString(CultureInfo.InvariantCulture);
            }

            pnlMensaje.Visible = false;
            pnlDetalleActividad.Visible = false;
            pnlDetalleParticipante.Visible = false;
            pnlFormularioParticipante.Visible = true;
        }

        private void AbrirEdicionParticipante(int idParticipante)
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            Participante participante = _bllParticipante.ObtenerParaEditar(idParticipante, idUsuarioGestor);
            if (participante == null)
            {
                MostrarMensaje("El participante no existe o está dado de baja: no se puede editar.", esError: true);
                return;
            }

            LimpiarFormularioParticipante();
            IdParticipanteEnEdicion = participante.IdParticipante;
            IdActividadOrigenParticipante = null;
            litTituloParticipante.Text = "Editar participante";
            btnGuardarParticipante.Text = "Guardar cambios";
            pnlAsociarActividadParticipante.Visible = false;
            txtNombreParticipante.Text = participante.Nombre;
            txtApellidoParticipante.Text = participante.Apellido;
            txtDniParticipante.Text = participante.Dni;
            txtCorreoParticipante.Text = participante.Correo;
            txtTelefonoParticipante.Text = participante.Telefono;
            txtNotasParticipante.Text = participante.Notas;

            pnlMensaje.Visible = false;
            pnlDetalleActividad.Visible = false;
            pnlDetalleParticipante.Visible = false;
            pnlFormularioParticipante.Visible = true;
        }

        protected void lnkCerrarParticipante_Click(object sender, EventArgs e)
        {
            pnlFormularioParticipante.Visible = false;

            // A11: al cancelar la edición se vuelve al detalle del participante;
            // si se abrió desde una actividad, al detalle de la actividad.
            if (IdParticipanteEnEdicion.HasValue)
            {
                int id = IdParticipanteEnEdicion.Value;
                IdParticipanteEnEdicion = null;
                MostrarDetalleParticipante(id);
            }
            else if (IdActividadOrigenParticipante.HasValue)
            {
                int id = IdActividadOrigenParticipante.Value;
                IdActividadOrigenParticipante = null;
                MostrarDetalle(id);
            }
        }

        protected void btnGuardarParticipante_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                pnlFormularioParticipante.Visible = true;
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            bool esEdicion = IdParticipanteEnEdicion.HasValue;
            Participante participante = new Participante
            {
                IdParticipante = IdParticipanteEnEdicion ?? 0,
                Nombre = txtNombreParticipante.Text,
                Apellido = txtApellidoParticipante.Text,
                Dni = txtDniParticipante.Text,
                Correo = txtCorreoParticipante.Text,
                Telefono = txtTelefonoParticipante.Text,
                Notas = txtNotasParticipante.Text
            };

            pnlParticipanteExistente.Visible = false;
            IdParticipanteExistente = null;

            ResultadoOperacion<int> resultado = _bllParticipante.Guardar(participante, idUsuarioGestor);
            if (!resultado.Exitoso)
            {
                // A3, A4, A12 o A5. En A5 se ofrece asociar el existente.
                MostrarMensajeFormularioParticipante(resultado.Mensaje);
                if (!esEdicion && resultado.CodigoAlternativo == "A5")
                {
                    OfrecerAsociarExistente(idUsuarioGestor);
                }

                pnlFormularioParticipante.Visible = true;
                return;
            }

            pnlFormularioParticipante.Visible = false;
            IdParticipanteEnEdicion = null;

            if (esEdicion)
            {
                // A11 pasos 7 y 8.
                CargarPantallaGestor();
                MostrarDetalleParticipante(resultado.Valor);
                MostrarMensajeParticipante(resultado.Mensaje, false);
                return;
            }

            string mensaje = resultado.Mensaje;
            bool advertencia = false;
            int idActividad = ConvertirEntero(ddlActividadParticipante.SelectedValue);
            if (idActividad > 0)
            {
                // Pasos 20 y 21 / A6: si la actividad está completa, el
                // participante queda registrado pero sin asociar.
                ResultadoOperacion asociacion = _bllActividad.AsociarParticipante(idActividad, resultado.Valor, idUsuarioGestor);
                mensaje = asociacion.Exitoso
                    ? "El participante fue registrado y asociado correctamente a la actividad."
                    : "El participante quedó registrado, pero no se asoció a la actividad: " + asociacion.Mensaje;
                advertencia = !asociacion.Exitoso;
            }

            CargarPantallaGestor();
            int? origen = IdActividadOrigenParticipante;
            IdActividadOrigenParticipante = null;
            if (origen.HasValue || idActividad > 0)
            {
                // Paso 24.
                MostrarDetalle(origen ?? idActividad);
                MostrarMensajeDetalle(mensaje, advertencia);
            }
            else
            {
                hdnTabActividades.Value = "participantes";
                MostrarMensaje(mensaje, esError: advertencia);
            }
        }

        private void OfrecerAsociarExistente(int idUsuarioGestor)
        {
            Participante existente = _bllParticipante.BuscarActivoPorDni(txtDniParticipante.Text, idUsuarioGestor);
            int idActividad = ConvertirEntero(ddlActividadParticipante.SelectedValue);
            if (existente == null || idActividad <= 0)
            {
                return;
            }

            IdParticipanteExistente = existente.IdParticipante;
            string actividad = ddlActividadParticipante.SelectedItem != null ? ddlActividadParticipante.SelectedItem.Text : "la actividad";
            litParticipanteExistente.Text = existente.NombreCompleto + " (DNI " + existente.Dni + ") ya está registrado. " +
                "¿Querés asociarlo a \"" + actividad + "\" en lugar de registrarlo de nuevo?";
            pnlParticipanteExistente.Visible = true;
        }

        // A5 paso 4: se asocia el existente (continúa en el paso 20).
        protected void btnAsociarExistenteFormulario_Click(object sender, EventArgs e)
        {
            int idActividad = ConvertirEntero(ddlActividadParticipante.SelectedValue);
            if (!IdParticipanteExistente.HasValue || idActividad <= 0)
            {
                pnlFormularioParticipante.Visible = true;
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado = _bllActividad.AsociarParticipante(idActividad, IdParticipanteExistente.Value, idUsuarioGestor);
            IdParticipanteExistente = null;
            IdActividadOrigenParticipante = null;
            pnlFormularioParticipante.Visible = false;
            CargarPantallaGestor();
            MostrarDetalle(idActividad);
            MostrarMensajeDetalle(resultado.Mensaje, !resultado.Exitoso);
        }

        // A5 paso 5: no se registra un participante nuevo.
        protected void lnkNoAsociarExistente_Click(object sender, EventArgs e)
        {
            IdParticipanteExistente = null;
            pnlParticipanteExistente.Visible = false;
            pnlFormularioParticipante.Visible = true;
        }

        protected void rptParticipantes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idParticipante = ConvertirEntero(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture));
            hdnTabActividades.Value = "participantes";
            pnlMensaje.Visible = false;

            if (e.CommandName == "Ver")
            {
                MostrarDetalleParticipante(idParticipante);
            }
            else if (e.CommandName == "Editar")
            {
                AbrirEdicionParticipante(idParticipante);
            }
            else if (e.CommandName == "Baja" && MostrarDetalleParticipante(idParticipante))
            {
                pnlConfirmarBajaParticipante.Visible = true;
            }
        }

        protected void btnBuscarParticipantes_Click(object sender, EventArgs e)
        {
            hdnTabActividades.Value = "participantes";
            pnlMensaje.Visible = false;
            CargarPantallaGestor();
        }

        // A10: detalle del participante.
        private bool MostrarDetalleParticipante(int idParticipante)
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            Participante participante = _bllParticipante.ObtenerDetalle(idParticipante, idUsuarioGestor);
            if (participante == null)
            {
                pnlDetalleParticipante.Visible = false;
                IdParticipanteEnDetalle = null;
                MostrarMensaje("No se encontró el participante indicado.", esError: true);
                return false;
            }

            CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
            IdParticipanteEnDetalle = participante.IdParticipante;
            ParticipanteEnDetalleActivo = participante.Activo;
            pnlDetalleActividad.Visible = false;
            pnlFormularioParticipante.Visible = false;
            pnlDetalleParticipante.Visible = true;
            pnlConfirmarBajaParticipante.Visible = false;
            pnlConfirmarDesvincularParticipante.Visible = false;
            pnlParticipanteMensaje.Visible = false;

            litParticipanteNombre.Text = participante.NombreCompleto;
            lblParticipanteEstado.Text = participante.Activo ? "Activo" : "Dado de baja";
            lblParticipanteEstado.CssClass = "activities-status " + (participante.Activo ? "activities-status-active" : "activities-status-inactive");
            litParticipanteNombreDato.Text = participante.Nombre;
            litParticipanteApellido.Text = participante.Apellido;
            litParticipanteDni.Text = participante.Dni;
            litParticipanteCorreo.Text = string.IsNullOrEmpty(participante.Correo) ? "No informado" : participante.Correo;
            litParticipanteTelefono.Text = string.IsNullOrEmpty(participante.Telefono) ? "No informado" : participante.Telefono;
            litParticipanteNotas.Text = string.IsNullOrEmpty(participante.Notas) ? "Sin notas" : participante.Notas;
            litParticipanteFechas.Text = "Alta el " + participante.FechaCreacion.ToString("dd/MM/yyyy", cultura) +
                (participante.FechaUltimaModificacion.HasValue ? " · modificado el " + participante.FechaUltimaModificacion.Value.ToString("dd/MM/yyyy", cultura) : string.Empty) +
                (participante.FechaBaja.HasValue ? " · baja el " + participante.FechaBaja.Value.ToString("dd/MM/yyyy", cultura) : string.Empty);

            List<ParticipacionActividad> vigentes = participante.Activo
                ? participante.Actividades.Where(a => a.Vigente).ToList()
                : new List<ParticipacionActividad>();
            List<ParticipacionActividad> historial = participante.Actividades.Where(a => !vigentes.Contains(a)).ToList();
            litParticipanteSinActividades.Visible = vigentes.Count == 0;
            rptParticipanteActividades.DataSource = vigentes;
            rptParticipanteActividades.DataBind();
            phParticipanteHistorial.Visible = historial.Count > 0;
            rptParticipanteHistorial.DataSource = historial;
            rptParticipanteHistorial.DataBind();

            btnEditarParticipante.Visible = participante.Activo;
            btnBajaParticipante.Visible = participante.Activo;
            return true;
        }

        protected string DescribirHistorial(object dato)
        {
            ParticipacionActividad participacion = (ParticipacionActividad)dato;
            CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
            string texto = "asociado el " + participacion.FechaAsociacion.ToString("dd/MM/yyyy", cultura);
            if (!participacion.AsociacionActiva && participacion.FechaDesvinculacion.HasValue)
            {
                return texto + ", desvinculado el " + participacion.FechaDesvinculacion.Value.ToString("dd/MM/yyyy", cultura);
            }

            return texto + (participacion.ActividadActiva ? " (participante dado de baja)" : " (actividad dada de baja)");
        }

        protected void lnkCerrarDetalleParticipante_Click(object sender, EventArgs e)
        {
            pnlDetalleParticipante.Visible = false;
            IdParticipanteEnDetalle = null;
            hdnTabActividades.Value = "participantes";
        }

        protected void btnEditarParticipante_Click(object sender, EventArgs e)
        {
            if (IdParticipanteEnDetalle.HasValue)
            {
                AbrirEdicionParticipante(IdParticipanteEnDetalle.Value);
            }
        }

        protected void btnBajaParticipante_Click(object sender, EventArgs e)
        {
            if (IdParticipanteEnDetalle.HasValue && MostrarDetalleParticipante(IdParticipanteEnDetalle.Value))
            {
                pnlConfirmarBajaParticipante.Visible = true;
            }
        }

        protected void lnkCancelarBajaParticipante_Click(object sender, EventArgs e)
        {
            if (IdParticipanteEnDetalle.HasValue)
            {
                MostrarDetalleParticipante(IdParticipanteEnDetalle.Value);
            }
        }

        // A14 pasos 3 a 7.
        protected void btnConfirmarBajaParticipante_Click(object sender, EventArgs e)
        {
            if (!IdParticipanteEnDetalle.HasValue)
            {
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            int idParticipante = IdParticipanteEnDetalle.Value;
            ResultadoOperacion resultado = _bllParticipante.DarDeBaja(idParticipante, idUsuarioGestor);
            CargarPantallaGestor();
            MostrarDetalleParticipante(idParticipante);
            MostrarMensajeParticipante(resultado.Mensaje, !resultado.Exitoso);
        }

        // A7: asociar un participante existente desde el detalle de la actividad.
        protected void btnAsociarExistente_Click(object sender, EventArgs e)
        {
            if (!IdActividadEnDetalle.HasValue)
            {
                return;
            }

            int idActividad = IdActividadEnDetalle.Value;
            int idParticipante = ConvertirEntero(ddlAsociarExistente.SelectedValue);
            if (idParticipante <= 0)
            {
                MostrarDetalle(idActividad);
                MostrarMensajeDetalle("Elegí el participante que querés asociar.", true);
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado = _bllActividad.AsociarParticipante(idActividad, idParticipante, idUsuarioGestor);
            CargarPantallaGestor();
            MostrarDetalle(idActividad);
            MostrarMensajeDetalle(resultado.Mensaje, !resultado.Exitoso);
        }

        protected void rptDetalleParticipantes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idParticipante = ConvertirEntero(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture));
            if (e.CommandName == "VerParticipante")
            {
                MostrarDetalleParticipante(idParticipante);
            }
            else if (e.CommandName == "Desvincular" && IdActividadEnDetalle.HasValue && MostrarDetalle(IdActividadEnDetalle.Value))
            {
                PedirConfirmacionDesvincular(IdActividadEnDetalle.Value, idParticipante, "Actividad");
            }
        }

        protected void rptParticipanteActividades_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idActividad = ConvertirEntero(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture));
            if (e.CommandName == "VerActividad")
            {
                MostrarDetalle(idActividad);
            }
            else if (e.CommandName == "Desvincular" && IdParticipanteEnDetalle.HasValue && MostrarDetalleParticipante(IdParticipanteEnDetalle.Value))
            {
                PedirConfirmacionDesvincular(idActividad, IdParticipanteEnDetalle.Value, "Participante");
            }
        }

        // A13 paso 2.
        private void PedirConfirmacionDesvincular(int idActividad, int idParticipante, string origen)
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            Actividad actividad = _bllActividad.ObtenerDetalle(idActividad, idUsuarioGestor);
            Participante participante = actividad == null ? null : actividad.Participantes.Find(p => p.IdParticipante == idParticipante);
            if (actividad == null || participante == null)
            {
                return;
            }

            IdActividadDesvincular = idActividad;
            IdParticipanteDesvincular = idParticipante;
            OrigenDesvincular = origen;
            string texto = "¿Desvincular a " + participante.NombreCompleto + " de \"" + actividad.Nombre + "\"? " +
                "Va a dejar de formar parte de esta actividad, pero sigue registrado para futuras asociaciones o consultas.";
            if (origen == "Actividad")
            {
                litConfirmarDesvincularActividad.Text = texto;
                pnlConfirmarDesvincularActividad.Visible = true;
            }
            else
            {
                litConfirmarDesvincularParticipante.Text = texto;
                pnlConfirmarDesvincularParticipante.Visible = true;
            }
        }

        // A13 pasos 3 a 7: se vuelve al detalle de la actividad.
        protected void btnConfirmarDesvincular_Click(object sender, EventArgs e)
        {
            if (!IdActividadDesvincular.HasValue || !IdParticipanteDesvincular.HasValue)
            {
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            int idActividad = IdActividadDesvincular.Value;
            ResultadoOperacion resultado = _bllActividad.DesasociarParticipante(idActividad, IdParticipanteDesvincular.Value, idUsuarioGestor);
            LimpiarDesvincular();
            CargarPantallaGestor();
            MostrarDetalle(idActividad);
            MostrarMensajeDetalle(resultado.Mensaje, !resultado.Exitoso);
        }

        // Si se cancela, no hay cambios y se vuelve a donde estaba.
        protected void lnkCancelarDesvincular_Click(object sender, EventArgs e)
        {
            string origen = OrigenDesvincular;
            int? idActividad = IdActividadDesvincular;
            int? idParticipante = IdParticipanteDesvincular;
            LimpiarDesvincular();
            if (origen == "Participante" && idParticipante.HasValue)
            {
                MostrarDetalleParticipante(idParticipante.Value);
            }
            else if (idActividad.HasValue)
            {
                MostrarDetalle(idActividad.Value);
            }
        }

        private void LimpiarDesvincular()
        {
            IdActividadDesvincular = null;
            IdParticipanteDesvincular = null;
            OrigenDesvincular = null;
        }

        private void MostrarMensajeParticipante(string mensaje, bool esError)
        {
            litParticipanteMensaje.Text = mensaje;
            pnlParticipanteMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlParticipanteMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private void MostrarMensajeFormularioParticipante(string mensaje)
        {
            litFormularioParticipanteMensaje.Text = mensaje;
            pnlFormularioParticipanteMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private void CargarPantallaGestor()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(idUsuarioGestor);
            List<Actividad> actividades = _bllActividad.ListarPorUsuarioGestor(idUsuarioGestor);
            List<Participante> participantes = _bllParticipante.ListarPorUsuarioGestor(idUsuarioGestor);
            string estadoParticipantes = ddlEstadoParticipantes.SelectedValue;
            string textoParticipantes = txtBuscarParticipante.Text.Trim();
            List<Participante> listadoParticipantes = estadoParticipantes == BLL_Participante.FiltroActivos && textoParticipantes.Length == 0
                ? participantes
                : _bllParticipante.Buscar(idUsuarioGestor, estadoParticipantes, textoParticipantes);

            pnlSinEspacios.Visible = espacios.Count == 0;
            pnlActividades.Visible = espacios.Count > 0;
            lnkNuevaActividad.Visible = espacios.Count > 0;

            // Paso 9: el listado aplica los filtros en la consulta. Si son los
            // de siempre (activas de todos los espacios) se reusa la lista.
            string estado = ddlFiltroEstado.SelectedValue;
            int idEspacioFiltro;
            int? espacioFiltro = int.TryParse(ddlFiltroEspacio.SelectedValue, out idEspacioFiltro) ? idEspacioFiltro : (int?)null;
            List<Actividad> listado = estado == BLL_Actividad.FiltroActivas && !espacioFiltro.HasValue
                ? actividades
                : _bllActividad.Listar(idUsuarioGestor, estado, espacioFiltro);

            List<ActividadInternaVista> vistaActividades = listado.Select(CrearVistaActividad).ToList();
            phSinActividades.Visible = vistaActividades.Count == 0;
            litSinActividades.Text = estado == BLL_Actividad.FiltroInactivas
                ? "No hay actividades dadas de baja con estos filtros."
                : "Todavía no hay actividades internas" + (espacioFiltro.HasValue ? " en este espacio" : string.Empty) + ". Usá «Nueva actividad» para cargar la primera.";
            List<ParticipanteVista> vistaActivos = participantes
                .Select(participante => CrearVistaParticipante(participante, actividades))
                .ToList();
            List<ParticipanteVista> vistaParticipantes = listadoParticipantes
                .Select(participante => CrearVistaParticipante(participante, actividades))
                .ToList();
            phSinParticipantes.Visible = vistaParticipantes.Count == 0;
            litSinParticipantes.Text = textoParticipantes.Length > 0 || estadoParticipantes != BLL_Participante.FiltroActivos
                ? "No hay participantes con estos filtros."
                : "Todavía no cargaste participantes. Usá «Nuevo participante».";

            litActividadesActivas.Text = actividades.Count.ToString(CultureInfo.InvariantCulture);
            litHorasBloqueadas.Text = CalcularHorasBloqueadas(actividades) + " h";
            litEspaciosProgramados.Text = actividades.Select(a => a.IdEspacioArtistico).Distinct().Count().ToString();
            litParticipantesActivos.Text = vistaActivos.Count.ToString(CultureInfo.InvariantCulture);
            litParticipantesAsignados.Text = vistaActivos.Count(p => p.Actividades != "Sin asignar").ToString(CultureInfo.InvariantCulture);
            litParticipantesSinAsignar.Text = vistaActivos.Count(p => p.Actividades == "Sin asignar").ToString(CultureInfo.InvariantCulture);

            rptActividades.DataSource = vistaActividades;
            rptActividades.DataBind();
            rptParticipantes.DataSource = vistaParticipantes;
            rptParticipantes.DataBind();
        }

        private void CargarFiltroEspacios()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ddlFiltroEspacio.Items.Clear();
            ddlFiltroEspacio.Items.Add(new ListItem("Todos los espacios", string.Empty));
            foreach (EspacioArtistico espacio in _bllEspacio.ListarMisEspacios(idUsuarioGestor))
            {
                ddlFiltroEspacio.Items.Add(new ListItem(espacio.NombreEspacio, espacio.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void CargarEspaciosEnSelector()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(idUsuarioGestor);

            ddlEspacio.Items.Clear();
            ddlEspacio.Items.Add(new ListItem("Seleccioná un espacio", string.Empty));

            foreach (EspacioArtistico espacio in espacios)
            {
                ddlEspacio.Items.Add(new ListItem(espacio.NombreEspacio, espacio.IdEspacioArtistico.ToString()));
            }
        }

        private void CargarActividadesEnSelector()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<Actividad> actividades = _bllActividad.ListarPorUsuarioGestor(idUsuarioGestor);

            ddlActividadParticipante.Items.Clear();
            ddlActividadParticipante.Items.Add(new ListItem("Sin asociar por ahora", string.Empty));

            foreach (Actividad actividad in actividades)
            {
                ddlActividadParticipante.Items.Add(new ListItem(
                    actividad.Nombre + " (" + actividad.NombreEspacio + ")", actividad.IdActividad.ToString(CultureInfo.InvariantCulture)));
            }

            // A2: sin actividades internas no hay a qué asociarlo todavía.
            litSinActividadesParticipante.Visible = actividades.Count == 0;
        }

        private void CargarSugerenciasParticipantes()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            rptSugerenciasParticipantes.DataSource = _bllParticipante.ListarPorUsuarioGestor(idUsuarioGestor);
            rptSugerenciasParticipantes.DataBind();
        }

        private static ActividadInternaVista CrearVistaActividad(Actividad actividad)
        {
            return new ActividadInternaVista
            {
                IdActividad = actividad.IdActividad,
                Nombre = actividad.Nombre,
                Tipo = string.IsNullOrEmpty(actividad.Tipo) ? "-" : actividad.Tipo,
                Espacio = actividad.NombreEspacio,
                Dias = FormatearDias(actividad),
                Horario = FormatearHorario(actividad.MinutoDesde, actividad.MinutoHasta),
                Cupo = actividad.Participantes.Count + " / " + actividad.CupoMaximo + " participantes",
                Activa = actividad.Activa,
                Estado = actividad.Activa ? "Activa" : "Dada de baja",
                ClaseEstado = "activities-status " + (actividad.Activa ? "activities-status-active" : "activities-status-inactive")
            };
        }

        private static ParticipanteVista CrearVistaParticipante(Participante participante, List<Actividad> actividades)
        {
            List<string> nombresActividades = actividades
                .Where(actividad => actividad.Participantes.Any(p => p.IdParticipante == participante.IdParticipante))
                .Select(actividad => actividad.Nombre)
                .ToList();

            List<string> contacto = new List<string>();
            if (!string.IsNullOrEmpty(participante.Correo)) contacto.Add(participante.Correo);
            if (!string.IsNullOrEmpty(participante.Telefono)) contacto.Add(participante.Telefono);

            return new ParticipanteVista
            {
                IdParticipante = participante.IdParticipante,
                NombreCompleto = participante.NombreCompleto,
                Iniciales = ObtenerIniciales(participante.Nombre, participante.Apellido),
                Contacto = contacto.Count == 0 ? "Sin datos de contacto" : string.Join(" · ", contacto),
                Dni = participante.Dni,
                Actividades = !participante.Activo ? "-" : nombresActividades.Count == 0 ? "Sin asignar" : string.Join(", ", nombresActividades),
                Activo = participante.Activo,
                Estado = participante.Activo ? "Activo" : "Dado de baja",
                ClaseEstado = "activities-status " + (participante.Activo ? "activities-status-active" : "activities-status-inactive")
            };
        }

        private static string FormatearDias(Actividad actividad)
        {
            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
            {
                List<string> dias = actividad.DiasSemana
                    .OrderBy(d => d)
                    .Where(d => NumeroADiaCodigo.ContainsKey(d))
                    .Select(d => NumeroADiaCodigo[d])
                    .ToList();
                return dias.Count > 0 ? string.Join(" y ", dias) : "-";
            }

            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString())
            {
                string semana = actividad.SemanaDelMes.HasValue && SemanaDelMesTexto.ContainsKey(actividad.SemanaDelMes.Value)
                    ? SemanaDelMesTexto[actividad.SemanaDelMes.Value]
                    : "?";
                string dia = actividad.DiaSemanaMensual.HasValue && NumeroADiaCodigo.ContainsKey(actividad.DiaSemanaMensual.Value)
                    ? NumeroADiaCodigo[actividad.DiaSemanaMensual.Value]
                    : "?";
                return semana + " semana, " + dia + " de cada mes";
            }

            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString() && !string.IsNullOrEmpty(actividad.Fecha))
            {
                DateTime fecha;
                return DateTime.TryParseExact(actividad.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                    ? fecha.ToString("dd/MM/yyyy") + " (única vez)"
                    : actividad.Fecha;
            }

            return "-";
        }

        private static string FormatearHorario(int minutoDesde, int minutoHasta)
        {
            return FormatearHora(minutoDesde) + " a " + FormatearHora(minutoHasta);
        }

        private static string FormatearHora(int minutos)
        {
            return (minutos / 60).ToString("00") + ":" + (minutos % 60).ToString("00");
        }

        private static string CalcularHorasBloqueadas(List<Actividad> actividades)
        {
            double minutosSemanales = actividades
                .Where(a => a.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
                .Sum(a => (a.MinutoHasta - a.MinutoDesde) * (double)Math.Max(a.DiasSemana.Count, 1));

            return Math.Round(minutosSemanales / 60.0, 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string ObtenerIniciales(string nombre, string apellido)
        {
            string inicialNombre = string.IsNullOrEmpty(nombre) ? string.Empty : nombre.Substring(0, 1).ToUpperInvariant();
            string inicialApellido = string.IsNullOrEmpty(apellido) ? string.Empty : apellido.Substring(0, 1).ToUpperInvariant();
            return inicialNombre + inicialApellido;
        }

        private ProgramacionActividadDto ObtenerProgramacion()
        {
            if (string.IsNullOrWhiteSpace(hdnProgramacionActividad.Value))
            {
                return null;
            }

            try
            {
                return new JavaScriptSerializer().Deserialize<ProgramacionActividadDto>(hdnProgramacionActividad.Value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // -1 si está vacío o no es una hora válida (la BLL lo informa como
        // dato faltante). Como fin, 00:00 es el cierre del día.
        private static int ConvertirHoraAMinutos(string hora, bool esFin)
        {
            int minutos;
            return BLL_DisponibilidadEspacio.TryLeerHora(hora, esFin, out minutos) ? minutos : -1;
        }

        private static int ConvertirEnteroOInvalido(string valor)
        {
            int resultado;
            return int.TryParse((valor ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out resultado) ? resultado : -1;
        }

        private void MostrarMensajeDetalle(string mensaje, bool esError)
        {
            litDetalleMensaje.Text = mensaje;
            pnlDetalleMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlDetalleMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private static int ConvertirEntero(string valor)
        {
            int resultado;
            return int.TryParse(valor, out resultado) ? resultado : 0;
        }

        private void LimpiarFormulario()
        {
            txtNombreActividad.Text = string.Empty;
            txtTipoActividad.Text = string.Empty;
            txtHoraInicio.Text = string.Empty;
            txtHoraFin.Text = string.Empty;
            txtCupoMaximo.Text = string.Empty;
            txtParticipantes.Text = string.Empty;
            txtDescripcionActividad.Text = string.Empty;
            hdnParticipantesActividad.Value = string.Empty;
            hdnProgramacionActividad.Value = string.Empty;

            foreach (ListItem item in cblDias.Items)
            {
                item.Selected = false;
            }
        }

        private void LimpiarFormularioParticipante()
        {
            txtNombreParticipante.Text = string.Empty;
            txtApellidoParticipante.Text = string.Empty;
            txtDniParticipante.Text = string.Empty;
            txtCorreoParticipante.Text = string.Empty;
            txtTelefonoParticipante.Text = string.Empty;
            if (ddlActividadParticipante.Items.Count > 0)
            {
                ddlActividadParticipante.SelectedIndex = 0;
            }

            txtNotasParticipante.Text = string.Empty;
            pnlFormularioParticipanteMensaje.Visible = false;
            pnlParticipanteExistente.Visible = false;
            IdParticipanteExistente = null;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private bool EsGestorEspacios()
        {
            return GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString();
        }

        private class ActividadInternaVista
        {
            public int IdActividad { get; set; }
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public string Espacio { get; set; }
            public string Dias { get; set; }
            public string Horario { get; set; }
            public string Cupo { get; set; }
            public bool Activa { get; set; }
            public string Estado { get; set; }
            public string ClaseEstado { get; set; }
        }

        private class ParticipanteVista
        {
            public int IdParticipante { get; set; }
            public string NombreCompleto { get; set; }
            public string Iniciales { get; set; }
            public string Contacto { get; set; }
            public string Dni { get; set; }
            public bool Activo { get; set; }
            public string Actividades { get; set; }
            public string Estado { get; set; }
            public string ClaseEstado { get; set; }
        }

        private class ProgramacionActividadDto
        {
            public string Modo { get; set; }
            public List<string> Dias { get; set; }
            public string Fecha { get; set; }
            public string SemanaDelMes { get; set; }
            public string DiaDelMes { get; set; }
            public string Desde { get; set; }
            public string Hasta { get; set; }
        }
    }
}
