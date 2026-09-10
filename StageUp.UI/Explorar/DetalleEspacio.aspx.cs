using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Explorar
{
    public partial class DetalleEspacio : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();

        private int? IdEspacioArtistico
        {
            get { return ViewState["IdEspacioArtistico"] as int?; }
            set { ViewState["IdEspacioArtistico"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            int idEspacioArtistico;
            if (!int.TryParse(Request.QueryString["id"], out idEspacioArtistico))
            {
                MostrarNoEncontrado();
                return;
            }

            EspacioArtistico espacio = _bllEspacio.ObtenerDetallePublicado(idEspacioArtistico);
            if (espacio == null)
            {
                MostrarNoEncontrado(
                    "No encontramos este espacio",
                    "Puede que ya no esté publicado o que la dirección esté mal escrita.");
                return;
            }

            IdEspacioArtistico = idEspacioArtistico;
            MostrarDetalle(espacio);
        }

        protected void btnSolicitarReserva_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || IdEspacioArtistico == null)
            {
                return;
            }

            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            DateTime fechaSolicitada;
            int minutoDesde;
            int duracionMinutos;
            if (!DateTime.TryParseExact(txtFechaReserva.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out fechaSolicitada) ||
                !int.TryParse(hdnMinutoDesdeReserva.Value, out minutoDesde) ||
                !int.TryParse(hdnDuracionReserva.Value, out duracionMinutos))
            {
                MostrarMensajeReserva("Elegí una fecha, un horario de inicio y una duración válidos.", true);
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<int> resultado = _bllReserva.SolicitarReserva(
                idUsuarioExterno,
                IdEspacioArtistico.Value,
                fechaSolicitada,
                minutoDesde,
                duracionMinutos,
                txtComentarioReserva.Text);

            MostrarMensajeReserva(resultado.Mensaje, !resultado.Exitoso);

            if (resultado.Exitoso)
            {
                txtFechaReserva.Text = string.Empty;
                txtComentarioReserva.Text = string.Empty;
                hdnMinutoDesdeReserva.Value = string.Empty;
                hdnDuracionReserva.Value = string.Empty;
            }
        }

        private void MostrarDetalle(EspacioArtistico espacio)
        {
            pnlEspacioNoEncontrado.Visible = false;
            pnlDetalleEspacio.Visible = true;

            FichaEspacio ficha = espacio.Ficha ?? new FichaEspacio();
            List<string> fotos = ObtenerFotos(espacio);
            pnlGaleria.Visible = fotos.Count > 0;
            pnlGaleriaVacia.Visible = fotos.Count == 0;

            if (fotos.Count > 0)
            {
                imgGaleriaPrincipal.ImageUrl = fotos[0];
                imgGaleriaPrincipal.AlternateText = "Fotografía principal de " + espacio.NombreEspacio;
                rptGaleriaFotos.DataSource = fotos;
                rptGaleriaFotos.DataBind();
                litEtiquetaGaleria.Text = fotos[0].Contains("/Images/Espacios/")
                    ? "IA · Imagen ilustrativa"
                    : fotos.Count == 1 ? "Foto del espacio" : fotos.Count + " fotografías";
            }

            litUbicacionPrincipal.Text = Server.HtmlEncode(ObtenerUbicacion(ficha));
            litTipoEspacio.Text = Server.HtmlEncode(espacio.TipoEspacio);
            litNombreEspacio.Text = Server.HtmlEncode(espacio.NombreEspacio);
            litDescripcion.Text = string.IsNullOrWhiteSpace(espacio.Descripcion)
                ? "Este espacio todavía no tiene una descripción cargada."
                : Server.HtmlEncode(espacio.Descripcion).Replace("\r\n", "<br />").Replace("\n", "<br />");

            pnlResumenCapacidad.Visible = ficha.CapacidadMaxima.HasValue;
            litResumenCapacidad.Text = ficha.CapacidadMaxima.HasValue
                ? "Hasta " + ficha.CapacidadMaxima.Value + " personas"
                : string.Empty;
            pnlResumenPrecio.Visible = ficha.PrecioHora.HasValue;
            litResumenPrecio.Text = ficha.PrecioHora.HasValue ? FormatearPrecio(ficha) : string.Empty;

            CargarGestor(espacio);
            CargarFicha(ficha);
            CargarReserva(espacio, ficha);

            Title = espacio.NombreEspacio + " | StageUp";
        }

        private void CargarGestor(EspacioArtistico espacio)
        {
            string nombre = string.IsNullOrWhiteSpace(espacio.NombreCompletoGestor)
                ? "Gestor del espacio"
                : espacio.NombreCompletoGestor;

            litNombreGestor.Text = Server.HtmlEncode(nombre);
            litInicialesGestor.Text = Server.HtmlEncode(ObtenerIniciales(nombre));

            var datos = new List<string> { "Sin calificaciones todavía" };
            if (espacio.GestorDesde.HasValue)
            {
                datos.Add("en StageUp desde " + espacio.GestorDesde.Value.ToString("MM/yyyy"));
            }

            if (espacio.CantidadEspaciosPublicadosGestor > 0)
            {
                datos.Add(espacio.CantidadEspaciosPublicadosGestor == 1
                    ? "1 espacio publicado"
                    : espacio.CantidadEspaciosPublicadosGestor + " espacios publicados");
            }

            litReputacionGestor.Text = Server.HtmlEncode(string.Join(" · ", datos));
        }

        private void CargarFicha(FichaEspacio ficha)
        {
            bool tieneFicha = TieneFicha(ficha);
            pnlFicha.Visible = tieneFicha;
            pnlSinFicha.Visible = !tieneFicha;
            if (!tieneFicha)
            {
                return;
            }

            litDireccion.Text = Server.HtmlEncode(ObtenerDireccion(ficha));
            litCapacidad.Text = ficha.CapacidadMaxima.HasValue
                ? "Hasta " + ficha.CapacidadMaxima.Value + " personas"
                : "No informada";
            litTipoPiso.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(ficha.TipoPiso) ? "No informado" : ficha.TipoPiso);
            litPrecioHora.Text = ficha.PrecioHora.HasValue ? FormatearPrecio(ficha) : "A consultar";

            var equipamiento = new List<string>();
            foreach (string codigo in ficha.Equipamiento ?? new List<string>())
            {
                equipamiento.Add(ObtenerNombreEquipamiento(codigo));
            }

            pnlCaracteristicas.Visible = equipamiento.Count > 0;
            pnlSinCaracteristicas.Visible = equipamiento.Count == 0;
            rptEquipamiento.DataSource = equipamiento;
            rptEquipamiento.DataBind();

            pnlDetalleEquipamiento.Visible = !string.IsNullOrWhiteSpace(ficha.DetalleEquipamiento);
            litDetalleEquipamiento.Text = Server.HtmlEncode(ficha.DetalleEquipamiento);

            List<string> disponibilidad = FormatearDisponibilidad(ficha.Disponibilidad);
            pnlDisponibilidad.Visible = disponibilidad.Count > 0;
            pnlSinDisponibilidad.Visible = disponibilidad.Count == 0;
            rptDisponibilidad.DataSource = disponibilidad;
            rptDisponibilidad.DataBind();
        }

        private void CargarReserva(EspacioArtistico espacio, FichaEspacio ficha)
        {
            List<FranjaEspacio> disponibilidad = ficha.Disponibilidad ?? new List<FranjaEspacio>();
            bool puedePlanificar = ficha.PrecioHora.HasValue &&
                disponibilidad.Exists(franja => franja != null && !franja.Bloqueado);

            pnlPlanificadorDisponible.Visible = puedePlanificar;
            pnlPlanificadorNoDisponible.Visible = !puedePlanificar;
            litPrecioReserva.Text = ficha.PrecioHora.HasValue ? FormatearPrecio(ficha) : "Valor a consultar";
            hdnDisponibilidadDetalle.Value = new JavaScriptSerializer().Serialize(disponibilidad);
            hdnPrecioHoraDetalle.Value = ficha.PrecioHora.HasValue
                ? ficha.PrecioHora.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : string.Empty;
            hdnMonedaDetalle.Value = ficha.Moneda == "USD" ? "USD" : "ARS";
            txtFechaReserva.Attributes["min"] = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            int? idUsuarioActual = GestorDeSesion.EstaAutenticado()
                ? GestorDeSesion.ObtenerIdUsuarioActual()
                : null;
            bool esPropio = idUsuarioActual.HasValue && idUsuarioActual.Value == espacio.IdUsuarioGestor;

            pnlReservarInvitado.Visible = !idUsuarioActual.HasValue;
            pnlReservarPropio.Visible = esPropio;
            pnlReservarFormulario.Visible = idUsuarioActual.HasValue && !esPropio && puedePlanificar;
        }

        private static bool TieneFicha(FichaEspacio ficha)
        {
            return ficha != null &&
                (!string.IsNullOrWhiteSpace(ficha.Provincia) ||
                 !string.IsNullOrWhiteSpace(ficha.Ciudad) ||
                 !string.IsNullOrWhiteSpace(ficha.Direccion) ||
                 ficha.CapacidadMaxima.HasValue ||
                 ficha.PrecioHora.HasValue);
        }

        private static string ObtenerUbicacion(FichaEspacio ficha)
        {
            if (!string.IsNullOrWhiteSpace(ficha.Ciudad) && !string.IsNullOrWhiteSpace(ficha.Provincia))
            {
                return ficha.Ciudad + ", " + ficha.Provincia;
            }

            if (!string.IsNullOrWhiteSpace(ficha.Ciudad))
            {
                return ficha.Ciudad;
            }

            return string.IsNullOrWhiteSpace(ficha.Provincia) ? "Ubicación a confirmar" : ficha.Provincia;
        }

        private static string ObtenerDireccion(FichaEspacio ficha)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(ficha.Direccion))
            {
                partes.Add(ficha.Direccion);
            }

            if (!string.IsNullOrWhiteSpace(ficha.Ciudad))
            {
                partes.Add(ficha.Ciudad);
            }

            if (!string.IsNullOrWhiteSpace(ficha.Provincia))
            {
                partes.Add(ficha.Provincia);
            }

            return partes.Count == 0 ? "No informada" : string.Join(", ", partes);
        }

        private static string FormatearPrecio(FichaEspacio ficha)
        {
            return (ficha.Moneda ?? "ARS") + " " +
                ficha.PrecioHora.Value.ToString("N2", CultureInfo.GetCultureInfo("es-AR")) + " / hora";
        }

        private static List<string> FormatearDisponibilidad(List<FranjaEspacio> franjas)
        {
            var resultado = new List<string>();
            string[] dias = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

            foreach (FranjaEspacio franja in franjas ?? new List<FranjaEspacio>())
            {
                if (franja == null)
                {
                    continue;
                }

                string dia = franja.DiaSemana.HasValue &&
                    franja.DiaSemana.Value >= 1 &&
                    franja.DiaSemana.Value <= 7
                    ? dias[franja.DiaSemana.Value]
                    : FormatearFecha(franja.Fecha);

                resultado.Add(franja.Bloqueado
                    ? dia + " · Cerrado"
                    : dia + " · " + FormatearHora(franja.MinutoDesde) + " a " + FormatearHora(franja.MinutoHasta));
            }

            return resultado;
        }

        private static string FormatearFecha(string valor)
        {
            DateTime fecha;
            return DateTime.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out fecha)
                ? fecha.ToString("dd/MM/yyyy")
                : valor;
        }

        private static string FormatearHora(int minutos)
        {
            if (minutos == 1440)
            {
                return "24:00";
            }

            return (minutos / 60).ToString("00") + ":" + (minutos % 60).ToString("00");
        }

        private static string ObtenerNombreEquipamiento(string codigo)
        {
            switch (codigo)
            {
                case "ESPEJOS": return "Espejos";
                case "SONIDO": return "Sonido y acústica";
                case "INSTRUMENTOS": return "Instrumentos";
                case "EQUIPAMIENTO": return "Equipamiento";
                case "ESCENARIO": return "Escenario";
                case "ILUMINACION": return "Iluminación";
                default: return codigo;
            }
        }

        private static string ObtenerIniciales(string nombre)
        {
            string[] partes = nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0)
            {
                return "GE";
            }

            return partes.Length == 1
                ? partes[0].Substring(0, 1).ToUpperInvariant()
                : (partes[0].Substring(0, 1) + partes[partes.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }

        private static List<string> ObtenerFotos(EspacioArtistico espacio)
        {
            var fotos = new List<string>();

            // Ítem 3 (varias fotografías): Ficha.Fotos es la fuente de verdad para
            // espacios guardados con el formulario nuevo. Si viene vacía (espacio
            // cargado antes de esta tanda, con una sola foto), se cae a FotoRuta.
            List<string> fotosFicha = espacio.Ficha == null ? null : espacio.Ficha.Fotos;
            if (fotosFicha != null)
            {
                foreach (string rutaFoto in fotosFicha)
                {
                    if (EsFotoValida(rutaFoto))
                    {
                        fotos.Add(rutaFoto);
                    }
                }
            }

            if (fotos.Count == 0)
            {
                string ruta = espacio.Ficha == null ? null : espacio.Ficha.FotoRuta;
                if (EsFotoValida(ruta))
                {
                    fotos.Add(ruta);
                }
            }

            if (fotos.Count == 0 &&
                string.Equals(espacio.NombreEspacio, "Sala Principal StageUp", StringComparison.OrdinalIgnoreCase))
            {
                fotos.Add("~/Content/Images/Espacios/sala-principal-ia.png");
            }

            if (fotos.Count == 0 &&
                string.Equals(espacio.NombreEspacio, "Estudio Fotográfico Norte", StringComparison.OrdinalIgnoreCase))
            {
                fotos.Add("~/Content/Images/Espacios/estudio-fotografico-ia.png");
            }

            return fotos;
        }

        private static bool EsFotoValida(string ruta)
        {
            return !string.IsNullOrEmpty(ruta) &&
                Regex.IsMatch(ruta, @"^~/Content/Uploads/Espacios/[a-f0-9]{32}\.jpg$");
        }

        private void MostrarMensajeReserva(string mensaje, bool esError)
        {
            litReservarMensaje.Text = Server.HtmlEncode(mensaje);
            pnlReservarMensaje.CssClass = esError
                ? "form-message form-message-error"
                : "form-message form-message-success";
            pnlReservarMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        private void MostrarNoEncontrado(string titulo = null, string descripcion = null)
        {
            pnlDetalleEspacio.Visible = false;
            pnlEspacioNoEncontrado.Visible = true;

            if (titulo != null)
            {
                litTituloNoEncontrado.Text = titulo;
            }

            if (descripcion != null)
            {
                litDescripcionNoEncontrado.Text = descripcion;
            }
        }
    }
}
