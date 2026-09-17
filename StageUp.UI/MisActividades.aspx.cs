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
        private readonly BLL_UsuarioExterno _bllUsuario = new BLL_UsuarioExterno();
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

            pnlPendienteGestor.Visible = perfil == PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString();
            pnlNoGestor.Visible = perfil == PerfilUsuarioExterno.ExternoSolicitante.ToString();
            pnlPanelGestor.Visible = perfil == PerfilUsuarioExterno.GestorEspacios.ToString();
            lnkNuevaActividad.Visible = pnlPanelGestor.Visible;

            if (pnlPanelGestor.Visible && !IsPostBack)
            {
                CargarPantallaGestor();
            }
        }

        protected void btnSolicitarGestor_Click(object sender, EventArgs e)
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<int> resultado = _bllUsuario.SolicitarHabilitacionComoGestor(idUsuarioExterno);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                return;
            }

            GestorDeSesion.ActualizarPerfilEnSesion(PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString());

            pnlNoGestor.Visible = false;
            pnlPendienteGestor.Visible = true;
            lnkNuevaActividad.Visible = false;
            MostrarMensaje(resultado.Mensaje, esError: false);
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
            pnlMensaje.Visible = false;
            pnlFormularioActividad.Visible = true;
        }

        protected void lnkCerrarActividad_Click(object sender, EventArgs e)
        {
            pnlFormularioActividad.Visible = false;
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

            if (programacion == null || string.IsNullOrEmpty(programacion.Modo))
            {
                MostrarMensaje("Definí la programación de la actividad (cuándo se repite y el horario).", esError: true);
                pnlFormularioActividad.Visible = true;
                return;
            }

            Actividad actividad = new Actividad
            {
                IdEspacioArtistico = ConvertirEntero(ddlEspacio.SelectedValue),
                Nombre = txtNombreActividad.Text.Trim(),
                Tipo = string.IsNullOrWhiteSpace(txtTipoActividad.Text) ? null : txtTipoActividad.Text.Trim(),
                MinutoDesde = ConvertirHoraAMinutos(txtHoraInicio.Text),
                MinutoHasta = ConvertirHoraAMinutos(txtHoraFin.Text),
                CupoMaximo = ConvertirEntero(txtCupoMaximo.Text),
                ParticipantesEstimados = string.IsNullOrWhiteSpace(txtParticipantes.Text) ? (int?)null : ConvertirEntero(txtParticipantes.Text),
                Notas = string.IsNullOrWhiteSpace(txtDescripcionActividad.Text) ? null : txtDescripcionActividad.Text.Trim()
            };

            if (programacion.Modo == "weekly")
            {
                actividad.ModoRecurrencia = ModoRecurrenciaActividad.Semanal.ToString();
                actividad.DiasSemana = cblDias.Items.Cast<ListItem>()
                    .Where(item => item.Selected && DiaCodigoANumero.ContainsKey(item.Value))
                    .Select(item => DiaCodigoANumero[item.Value])
                    .ToList();
            }
            else if (programacion.Modo == "monthly")
            {
                actividad.ModoRecurrencia = ModoRecurrenciaActividad.Mensual.ToString();
                actividad.SemanaDelMes = programacion.SemanaDelMes != null && SemanaDelMesCodigoANumero.ContainsKey(programacion.SemanaDelMes)
                    ? SemanaDelMesCodigoANumero[programacion.SemanaDelMes]
                    : (int?)null;
                actividad.DiaSemanaMensual = programacion.DiaDelMes != null && DiaCodigoANumero.ContainsKey(programacion.DiaDelMes)
                    ? DiaCodigoANumero[programacion.DiaDelMes]
                    : (int?)null;
            }
            else if (programacion.Modo == "date")
            {
                actividad.ModoRecurrencia = ModoRecurrenciaActividad.Fecha.ToString();
                actividad.Fecha = programacion.Fecha;
            }
            else
            {
                MostrarMensaje("El modo de recurrencia indicado no es válido.", esError: true);
                pnlFormularioActividad.Visible = true;
                return;
            }

            ResultadoOperacion<int> resultado = _bllActividad.Guardar(actividad, idUsuarioGestor);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                pnlFormularioActividad.Visible = true;
                return;
            }

            foreach (string idTexto in (hdnParticipantesActividad.Value ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int idParticipante;
                if (int.TryParse(idTexto, out idParticipante))
                {
                    _bllActividad.AsociarParticipante(resultado.Valor, idParticipante, idUsuarioGestor);
                }
            }

            pnlFormularioActividad.Visible = false;
            CargarPantallaGestor();
            MostrarMensaje("Actividad guardada. El horario indicado ya bloquea ese espacio para reservas externas.", esError: false);
        }

        protected void rptActividades_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Baja")
            {
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            int idActividad = ConvertirEntero((string)e.CommandArgument);
            ResultadoOperacion resultado = _bllActividad.DarDeBaja(idActividad, idUsuarioGestor);

            CargarPantallaGestor();
            MostrarMensaje(resultado.Mensaje ?? "Actividad dada de baja.", esError: !resultado.Exitoso);
        }

        protected void lnkNuevoParticipante_Click(object sender, EventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisActividades.aspx");
                return;
            }

            CargarActividadesEnSelector();
            LimpiarFormularioParticipante();
            pnlMensaje.Visible = false;
            pnlFormularioParticipante.Visible = true;
        }

        protected void lnkCerrarParticipante_Click(object sender, EventArgs e)
        {
            pnlFormularioParticipante.Visible = false;
        }

        protected void btnGuardarParticipante_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                pnlFormularioParticipante.Visible = true;
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            Participante participante = new Participante
            {
                Nombre = txtNombreParticipante.Text.Trim(),
                Apellido = txtApellidoParticipante.Text.Trim(),
                Dni = txtDniParticipante.Text.Trim(),
                Notas = string.IsNullOrWhiteSpace(txtNotasParticipante.Text) ? null : txtNotasParticipante.Text.Trim()
            };

            ResultadoOperacion<int> resultado = _bllParticipante.Guardar(participante, idUsuarioGestor);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, esError: true);
                pnlFormularioParticipante.Visible = true;
                return;
            }

            if (!string.IsNullOrEmpty(ddlActividadParticipante.SelectedValue))
            {
                int idActividadSeleccionada = ConvertirEntero(ddlActividadParticipante.SelectedValue);
                _bllActividad.AsociarParticipante(idActividadSeleccionada, resultado.Valor, idUsuarioGestor);
            }

            pnlFormularioParticipante.Visible = false;
            CargarPantallaGestor();
            MostrarMensaje("Participante guardado correctamente.", esError: false);
        }

        protected void rptParticipantes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Baja")
            {
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            int idParticipante = ConvertirEntero((string)e.CommandArgument);
            ResultadoOperacion resultado = _bllParticipante.DarDeBaja(idParticipante, idUsuarioGestor);

            CargarPantallaGestor();
            MostrarMensaje(resultado.Exitoso ? "Participante dado de baja." : resultado.Mensaje, esError: !resultado.Exitoso);
        }

        private void CargarPantallaGestor()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(idUsuarioGestor);
            List<Actividad> actividades = _bllActividad.ListarPorUsuarioGestor(idUsuarioGestor);
            List<Participante> participantes = _bllParticipante.ListarPorUsuarioGestor(idUsuarioGestor);

            pnlSinEspacios.Visible = espacios.Count == 0;
            pnlActividades.Visible = espacios.Count > 0;
            lnkNuevaActividad.Visible = espacios.Count > 0;

            List<ActividadInternaVista> vistaActividades = actividades.Select(CrearVistaActividad).ToList();
            List<ParticipanteVista> vistaParticipantes = participantes
                .Select(participante => CrearVistaParticipante(participante, actividades))
                .ToList();

            litActividadesActivas.Text = vistaActividades.Count.ToString();
            litHorasBloqueadas.Text = CalcularHorasBloqueadas(actividades) + " h";
            litEspaciosProgramados.Text = actividades.Select(a => a.IdEspacioArtistico).Distinct().Count().ToString();
            litParticipantesActivos.Text = vistaParticipantes.Count.ToString();
            litParticipantesAsignados.Text = vistaParticipantes.Count(p => p.Actividades != "Sin asignar").ToString();
            litParticipantesSinAsignar.Text = vistaParticipantes.Count(p => p.Actividades == "Sin asignar").ToString();

            rptActividades.DataSource = vistaActividades;
            rptActividades.DataBind();
            rptParticipantes.DataSource = vistaParticipantes;
            rptParticipantes.DataBind();
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
                ddlActividadParticipante.Items.Add(new ListItem(actividad.Nombre, actividad.IdActividad.ToString()));
            }
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
                Cupo = actividad.CupoMaximo + " participantes",
                Estado = "Activa",
                ClaseEstado = "activities-status activities-status-active"
            };
        }

        private static ParticipanteVista CrearVistaParticipante(Participante participante, List<Actividad> actividades)
        {
            List<string> nombresActividades = actividades
                .Where(actividad => actividad.Participantes.Any(p => p.IdParticipante == participante.IdParticipante))
                .Select(actividad => actividad.Nombre)
                .ToList();

            return new ParticipanteVista
            {
                IdParticipante = participante.IdParticipante,
                NombreCompleto = participante.NombreCompleto,
                Iniciales = ObtenerIniciales(participante.Nombre, participante.Apellido),
                Dni = participante.Dni,
                Actividades = nombresActividades.Count == 0 ? "Sin asignar" : string.Join(", ", nombresActividades),
                Estado = "Activo",
                ClaseEstado = "activities-status activities-status-active"
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

        private static int ConvertirHoraAMinutos(string hora)
        {
            TimeSpan valor;
            return TimeSpan.TryParse(hora, CultureInfo.InvariantCulture, out valor) ? (int)valor.TotalMinutes : 0;
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
            ddlActividadParticipante.SelectedIndex = 0;
            txtNotasParticipante.Text = string.Empty;
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
            public string Estado { get; set; }
            public string ClaseEstado { get; set; }
        }

        private class ParticipanteVista
        {
            public int IdParticipante { get; set; }
            public string NombreCompleto { get; set; }
            public string Iniciales { get; set; }
            public string Dni { get; set; }
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
