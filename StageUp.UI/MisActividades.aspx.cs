using System;
using System.Collections.Generic;
using System.Linq;
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

            if (pnlPanelGestor.Visible)
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

            pnlFormularioActividad.Visible = false;
            MostrarMensaje("La pantalla de alta de actividad quedó preparada. El guardado se integrará cuando esté disponible la lógica de actividades internas.", esError: false);
        }

        protected void lnkNuevoParticipante_Click(object sender, EventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisActividades.aspx");
                return;
            }

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

            pnlFormularioParticipante.Visible = false;
            MostrarMensaje("La pantalla de participantes quedó preparada. El alta, modificación, baja lógica y asociación se integrarán con la lógica real.", esError: false);
        }

        private void CargarPantallaGestor()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(idUsuarioGestor);
            List<ActividadInternaVista> actividades = CrearActividadesDeMuestra(espacios);
            List<ParticipanteVista> participantes = CrearParticipantesDeMuestra();

            pnlSinEspacios.Visible = espacios.Count == 0;
            pnlActividades.Visible = espacios.Count > 0;
            lnkNuevaActividad.Visible = espacios.Count > 0;

            litActividadesActivas.Text = actividades.Count(a => a.Estado == "Activa").ToString();
            litHorasBloqueadas.Text = CalcularHorasBloqueadas(actividades);
            litEspaciosProgramados.Text = actividades.Select(a => a.Espacio).Distinct().Count().ToString();
            litParticipantesActivos.Text = participantes.Count(p => p.Estado == "Activo").ToString();
            litParticipantesAsignados.Text = participantes.Count(p => p.Actividades != "Sin asignar").ToString();
            litParticipantesSinAsignar.Text = participantes.Count(p => p.Actividades == "Sin asignar").ToString();

            rptActividades.DataSource = actividades;
            rptActividades.DataBind();
            rptParticipantes.DataSource = participantes;
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

        private List<ActividadInternaVista> CrearActividadesDeMuestra(List<EspacioArtistico> espacios)
        {
            List<ActividadInternaVista> actividades = new List<ActividadInternaVista>();
            if (espacios.Count == 0)
            {
                return actividades;
            }

            actividades.Add(new ActividadInternaVista
            {
                Nombre = "Ballet principiantes",
                Tipo = "Clase regular",
                Espacio = espacios[0].NombreEspacio,
                Dias = "Lun y Mié",
                Horario = "17:00 a 19:00",
                Cupo = "18 participantes",
                Estado = "Activa",
                ClaseEstado = "activities-status activities-status-active",
                HorasSemanales = 4
            });

            EspacioArtistico segundoEspacio = espacios.Count > 1 ? espacios[1] : espacios[0];
            actividades.Add(new ActividadInternaVista
            {
                Nombre = "Ensayo compañía estable",
                Tipo = "Ensayo fijo",
                Espacio = segundoEspacio.NombreEspacio,
                Dias = "Mar y Jue",
                Horario = "20:00 a 22:00",
                Cupo = "12 participantes",
                Estado = "Activa",
                ClaseEstado = "activities-status activities-status-active",
                HorasSemanales = 4
            });

            EspacioArtistico tercerEspacio = espacios.Count > 2 ? espacios[2] : espacios[0];
            actividades.Add(new ActividadInternaVista
            {
                Nombre = "Taller de montaje escénico",
                Tipo = "Taller interno",
                Espacio = tercerEspacio.NombreEspacio,
                Dias = "Sáb",
                Horario = "10:00 a 13:00",
                Cupo = "20 participantes",
                Estado = "Borrador",
                ClaseEstado = "activities-status activities-status-draft",
                HorasSemanales = 3
            });

            return actividades;
        }

        private List<ParticipanteVista> CrearParticipantesDeMuestra()
        {
            return new List<ParticipanteVista>
            {
                new ParticipanteVista
                {
                    NombreCompleto = "Juana López",
                    Iniciales = "JL",
                    Dni = "42111222",
                    Actividades = "Ballet principiantes",
                    Estado = "Activo",
                    ClaseEstado = "activities-status activities-status-active"
                },
                new ParticipanteVista
                {
                    NombreCompleto = "Lucía Fernández",
                    Iniciales = "LF",
                    Dni = "39888777",
                    Actividades = "Ballet principiantes, Taller de montaje escénico",
                    Estado = "Activo",
                    ClaseEstado = "activities-status activities-status-active"
                },
                new ParticipanteVista
                {
                    NombreCompleto = "Martín Álvarez",
                    Iniciales = "MA",
                    Dni = "40555111",
                    Actividades = "Ensayo compañía estable",
                    Estado = "Activo",
                    ClaseEstado = "activities-status activities-status-active"
                },
                new ParticipanteVista
                {
                    NombreCompleto = "Camila Ruiz",
                    Iniciales = "CR",
                    Dni = "44777222",
                    Actividades = "Sin asignar",
                    Estado = "Activo",
                    ClaseEstado = "activities-status activities-status-active"
                }
            };
        }

        private string CalcularHorasBloqueadas(List<ActividadInternaVista> actividades)
        {
            int horas = actividades
                .Where(a => a.Estado == "Activa")
                .Sum(a => a.HorasSemanales);

            return horas.ToString() + " h";
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
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public string Espacio { get; set; }
            public string Dias { get; set; }
            public string Horario { get; set; }
            public string Cupo { get; set; }
            public string Estado { get; set; }
            public string ClaseEstado { get; set; }
            public int HorasSemanales { get; set; }
        }

        private class ParticipanteVista
        {
            public string NombreCompleto { get; set; }
            public string Iniciales { get; set; }
            public string Dni { get; set; }
            public string Actividades { get; set; }
            public string Estado { get; set; }
            public string ClaseEstado { get; set; }
        }
    }
}
