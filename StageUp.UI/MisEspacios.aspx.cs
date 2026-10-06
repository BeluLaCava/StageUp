using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class MisEspacios : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_Calificacion _bllCalificacion = new BLL_Calificacion();
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();

        protected bool FichaCompletaActiva { get { return _bllEspacio.FichaCompletaHabilitada; } }

        protected int CantidadSolicitudesPendientes { get; private set; }

        private const int MaxFotosPorEspacio = 8;

        private int? IdEspacioEnEdicion
        {
            get { return ViewState["IdEspacioEnEdicion"] as int?; }
            set { ViewState["IdEspacioEnEdicion"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Form.Enctype = "multipart/form-data";
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
            lnkNuevoEspacio.Visible = true;

            if (pnlPanelGestor.Visible)
            {
                if (!IsPostBack)
                {
                    CargarMisEspacios();

                    // CU-001-007 A8/A9 y A10: llegadas desde el detalle del espacio.
                    int idEditar;
                    if (int.TryParse(Request.QueryString["editar"], out idEditar))
                    {
                        CargarEspacioEnFormulario(idEditar);
                    }
                    else if (Request.QueryString["nuevo"] == "1")
                    {
                        // CU-001-008 A1: llegada desde "Disponibilidad del espacio" sin espacios.
                        LimpiarFormulario();
                        pnlFormularioEspacio.Visible = true;
                    }
                    else if (Request.QueryString["baja"] == "1")
                    {
                        MostrarMensaje("El espacio fue dado de baja. Ya no está disponible para nuevas reservas y su información histórica se conserva.", false);
                    }
                }

                CantidadSolicitudesPendientes = ContarSolicitudesPendientes();
                litBadgeSolicitudes.Text = CantidadSolicitudesPendientes > 0
                    ? " <span class=\"gestor-subnav-badge\">" + CantidadSolicitudesPendientes + "</span>"
                    : string.Empty;
            }
        }

        protected void lnkNuevoEspacio_Click(object sender, EventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            LimpiarFormulario();
            pnlMensaje.Visible = false;
            pnlFormularioEspacio.Visible = true;
        }

        protected void btnGuardarEspacio_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            if (FichaCompletaActiva)
            {
                GuardarFichaCompleta(idUsuarioGestor);
                return;
            }

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
                litFormularioMensaje.Text = resultado.Mensaje;
                pnlFormularioMensaje.Visible = true;
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
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

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

            EspacioArtistico espacio = (EspacioArtistico)e.Item.DataItem;
            LinkButton lnkPublicar = (LinkButton)e.Item.FindControl("lnkPublicar");
            LinkButton lnkPausar = (LinkButton)e.Item.FindControl("lnkPausar");

            lnkPublicar.Visible = !espacio.Publicado;
            lnkPausar.Visible = espacio.Publicado;
        }

        private void CargarMisEspacios()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<EspacioArtistico> espacios = _bllEspacio.ListarMisEspacios(idUsuarioGestor);
            _bllCalificacion.CompletarReputacionesEspacios(espacios);

            litSinEspacios.Visible = espacios.Count == 0;
            rptMisEspacios.DataSource = espacios;
            rptMisEspacios.DataBind();
        }

        private int ContarSolicitudesPendientes()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            return _bllReserva.ContarSolicitudesPendientes(idUsuarioGestor);
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
            CargarFicha(espacio);
            litTituloFormulario.Text = "Editar espacio";
            lnkCancelarEdicion.Visible = true;
            btnGuardarEspacio.Text = "Guardar cambios";
            pnlFormularioMensaje.Visible = false;
            pnlMensaje.Visible = false;
            pnlFormularioEspacio.Visible = true;
        }

        private void LimpiarFormulario()
        {
            IdEspacioEnEdicion = null;
            txtNombreEspacio.Text = string.Empty;
            txtTipoEspacio.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            CargarFicha(new EspacioArtistico());
            litTituloFormulario.Text = "Nuevo espacio";
            lnkCancelarEdicion.Visible = true;
            btnGuardarEspacio.Text = "Guardar espacio";
            pnlFormularioEspacio.Visible = false;
            pnlFormularioMensaje.Visible = false;
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

        protected override void OnPreRender(EventArgs e)
        {
            if (pnlFormularioEspacio.Visible)
                btnGuardarEspacio.Text = FichaCompletaActiva ? "Guardar espacio" : "Guardar datos básicos";
            base.OnPreRender(e);
        }

        private void CargarFicha(EspacioArtistico espacio)
        {
            FichaEspacio ficha = espacio.Ficha ?? new FichaEspacio();

            List<string> fotos = ficha.Fotos != null && ficha.Fotos.Count > 0
                ? ficha.Fotos
                : (!string.IsNullOrEmpty(ficha.FotoRuta) ? new List<string> { ficha.FotoRuta } : new List<string>());
            hdnFotosActuales.Value = new JavaScriptSerializer().Serialize(fotos);

            txtProvincia.Text = ficha.Provincia;
            txtCiudad.Text = ficha.Ciudad;
            txtDireccion.Text = ficha.Direccion;
            txtCapacidad.Text = ficha.CapacidadMaxima.HasValue ? ficha.CapacidadMaxima.Value.ToString() : "";
            txtPrecioHora.Text = ficha.PrecioHora.HasValue ? ficha.PrecioHora.Value.ToString("0.00", CultureInfo.InvariantCulture) : "";
            ddlMoneda.SelectedValue = ficha.Moneda == "USD" ? "USD" : "ARS";
            txtTipoPiso.Text = ficha.TipoPiso;
            txtEquipamientoDetalle.Text = ficha.DetalleEquipamiento;
            txtSuperficie.Text = ficha.SuperficieM2.HasValue ? ficha.SuperficieM2.Value.ToString("0.##", CultureInfo.InvariantCulture) : "";
            txtAltura.Text = ficha.AlturaM.HasValue ? ficha.AlturaM.Value.ToString("0.##", CultureInfo.InvariantCulture) : "";
            txtCondicionesUso.Text = ficha.CondicionesUso;
            txtReglasUso.Text = ficha.ReglasUso;
            // Alta: se publica al guardar. Edición: se mantiene el estado actual.
            ddlEstadoPublicacion.SelectedValue = espacio.IdEspacioArtistico == 0 || espacio.Publicado ? "Publicar" : "NoPublicar";
            pnlSugerencia.Visible = false;
            foreach (ListItem item in cblEquipamiento.Items)
                item.Selected = ficha.Equipamiento != null && ficha.Equipamiento.Contains(item.Value);
            // Solo las franjas manuales: los bloqueos de "Mis actividades" no se
            // editan desde la ficha (si se mandaran, volverían como manuales).
            hdnDisponibilidad.Value = new JavaScriptSerializer().Serialize(BLL_EspacioArtistico.ObtenerFranjasEditables(ficha));
            ConfigurarSeccionDisponibilidad(espacio);
        }

        // CU-001-008: al crear un espacio se cargan los horarios iniciales en
        // la ficha; al editarlo, la disponibilidad se gestiona desde
        // "Disponibilidad del espacio" (con sus validaciones) y la ficha no
        // la modifica. El editor queda oculto pero en la página, porque el
        // script de la ficha también maneja el precio y las fotos.
        private void ConfigurarSeccionDisponibilidad(EspacioArtistico espacio)
        {
            bool editando = espacio.IdEspacioArtistico != 0;
            pnlDisponibilidadGestionada.Visible = editando;
            if (editando)
            {
                divEditorDisponibilidad.Attributes["hidden"] = "hidden";
                int abiertas = BLL_DisponibilidadEspacio.ListarDisponibilidad(espacio).Count;
                int bloqueos = BLL_DisponibilidadEspacio.ListarBloqueos(espacio).Count;
                litResumenDisponibilidad.Text = abiertas == 0
                    ? "Este espacio todavía no tiene horarios disponibles configurados."
                    : "Tiene " + abiertas + (abiertas == 1 ? " franja disponible" : " franjas disponibles") +
                      (bloqueos > 0 ? " y " + bloqueos + (bloqueos == 1 ? " bloqueo" : " bloqueos") : string.Empty) + " configurados.";
                lnkConfigurarDisponibilidad.NavigateUrl = "~/DisponibilidadEspacio.aspx?id=" +
                    espacio.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                divEditorDisponibilidad.Attributes.Remove("hidden");
            }
        }

        private void GuardarFichaCompleta(int idUsuarioGestor)
        {
            try
            {
                string error;
                EspacioArtistico espacio = ArmarEspacioDesdeFormulario(out error);
                if (espacio == null)
                {
                    ErrorFormulario(error);
                    return;
                }

                ResultadoOperacion validacion = _bllEspacio.ValidarFicha(espacio);
                if (!validacion.Exitoso)
                {
                    ErrorFormulario(validacion.Mensaje);
                    return;
                }

                bool publicadoAntes = false;
                if (espacio.IdEspacioArtistico != 0)
                {
                    EspacioArtistico anterior = _bllEspacio.ObtenerDetalleParaGestor(espacio.IdEspacioArtistico, idUsuarioGestor);
                    publicadoAntes = anterior != null && anterior.Publicado;
                }

                ResultadoOperacion<int> resultado = _bllEspacio.GuardarFicha(espacio, idUsuarioGestor);
                if (!resultado.Exitoso)
                {
                    ErrorFormulario(resultado.Mensaje);
                    return;
                }

                // Paso 14 / A9: el estado elegido en el formulario.
                string mensaje = resultado.Mensaje;
                bool publicar = ddlEstadoPublicacion.SelectedValue == "Publicar";
                ResultadoOperacion cambioEstado = null;
                if (publicar && !publicadoAntes)
                {
                    cambioEstado = _bllEspacio.Publicar(resultado.Valor, idUsuarioGestor);
                }
                else if (!publicar && publicadoAntes)
                {
                    cambioEstado = _bllEspacio.Pausar(resultado.Valor, idUsuarioGestor);
                }

                if (cambioEstado != null)
                {
                    mensaje += " " + cambioEstado.Mensaje;
                }

                LimpiarFormulario();
                MostrarMensaje(mensaje, cambioEstado != null && !cambioEstado.Exitoso);
                CargarMisEspacios();
            }
            catch (ArgumentException ex)
            {
                ErrorFormulario("Revisá las imágenes y los horarios ingresados.");
                System.Diagnostics.Trace.TraceError(ex.ToString());
            }
            catch (Exception ex)
            {
                ErrorFormulario("No se pudo completar el guardado. Revisá si el espacio aparece en la lista antes de volver a intentar.");
                System.Diagnostics.Trace.TraceError(ex.ToString());
            }
        }

        // CU-001-007 A12: "Generar descripción automática".
        protected void lnkGenerarDescripcion_Click(object sender, EventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            string error;
            EspacioArtistico espacio = ArmarEspacioDesdeFormulario(out error, exigirNumeros: false);
            if (espacio == null)
            {
                ErrorFormulario(error);
                return;
            }

            txtDescripcion.Text = BLL_EspacioArtistico.GenerarDescripcion(espacio);
            pnlFormularioMensaje.Visible = false;
            pnlFormularioEspacio.Visible = true;
        }

        // CU-001-007 A13: "Sugerir valores".
        protected void lnkSugerirValores_Click(object sender, EventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            string error;
            EspacioArtistico espacio = ArmarEspacioDesdeFormulario(out error, exigirNumeros: false);
            pnlFormularioEspacio.Visible = true;
            if (espacio == null)
            {
                ErrorFormulario(error);
                return;
            }

            ResultadoOperacion<SugerenciaPrecio> resultado = _bllEspacio.SugerirValores(espacio);
            pnlFormularioMensaje.Visible = false;
            pnlSugerencia.Visible = true;
            if (!resultado.Exitoso)
            {
                litSugerencia.Text = resultado.Mensaje;
                return;
            }

            SugerenciaPrecio sugerencia = resultado.Valor;
            string simbolo = sugerencia.Moneda == "USD" ? "US$ " : "$ ";
            litSugerencia.Text = "Valor sugerido por hora: entre " + simbolo + sugerencia.Minimo.ToString("N2", CultureInfo.CurrentCulture) +
                " y " + simbolo + sugerencia.Maximo.ToString("N2", CultureInfo.CurrentCulture) +
                " (valor medio " + simbolo + sugerencia.Mediana.ToString("N2", CultureInfo.CurrentCulture) + "), según " +
                sugerencia.CantidadComparados + " " + sugerencia.Criterio + ". Es orientativo: el valor final lo definís vos.";
        }

        // Arma el espacio con lo que hay en el formulario. Las fotos nuevas se
        // guardan y se suman a la lista del formulario, así no se pierden si
        // la validación falla o si se usa una de las ayudas (A12, A13).
        private EspacioArtistico ArmarEspacioDesdeFormulario(out string error, bool exigirNumeros = true)
        {
            error = null;
            int capacidad;
            decimal precio;
            bool capacidadOk = int.TryParse(txtCapacidad.Text, out capacidad);
            bool precioOk = TryLeerDecimal(txtPrecioHora.Text, out precio);
            if (exigirNumeros && (!capacidadOk || !precioOk))
            {
                error = "Revisá la capacidad y el precio por hora. Usá coma o punto para los decimales, sin separador de miles.";
                return null;
            }

            decimal superficie;
            decimal altura;
            bool superficieOk = TryLeerDecimal(txtSuperficie.Text, out superficie);
            bool alturaOk = TryLeerDecimal(txtAltura.Text, out altura);
            if (exigirNumeros && !string.IsNullOrWhiteSpace(txtSuperficie.Text) && !superficieOk)
            {
                error = "La superficie tiene que ser un número (por ejemplo 80 o 80,5).";
                return null;
            }

            if (exigirNumeros && !string.IsNullOrWhiteSpace(txtAltura.Text) && !alturaOk)
            {
                error = "La altura tiene que ser un número en metros (por ejemplo 3,5).";
                return null;
            }

            if (hdnDisponibilidad.Value.Length > 64000)
            {
                error = "Hay demasiados horarios cargados.";
                return null;
            }

            List<string> fotos;
            try
            {
                fotos = new JavaScriptSerializer().Deserialize<List<string>>(hdnFotosActuales.Value) ?? new List<string>();
            }
            catch (ArgumentException)
            {
                fotos = new List<string>();
            }

            int cantidadNuevas = archivoFoto.HasFiles ? archivoFoto.PostedFiles.Count : 0;
            if (fotos.Count + cantidadNuevas > MaxFotosPorEspacio)
            {
                error = "Podés cargar hasta " + MaxFotosPorEspacio + " fotos por espacio. Quitá alguna antes de agregar más.";
                return null;
            }

            if (archivoFoto.HasFiles)
            {
                foreach (HttpPostedFile archivo in archivoFoto.PostedFiles)
                {
                    if (archivo == null || archivo.ContentLength == 0) continue;
                    fotos.Add(GuardarFoto(archivo));
                }

                hdnFotosActuales.Value = new JavaScriptSerializer().Serialize(fotos);
            }

            List<FranjaEspacio> disponibilidad;
            try
            {
                disponibilidad = new JavaScriptSerializer().Deserialize<List<FranjaEspacio>>(hdnDisponibilidad.Value) ?? new List<FranjaEspacio>();
            }
            catch (ArgumentException)
            {
                disponibilidad = new List<FranjaEspacio>();
            }

            EspacioArtistico espacio = new EspacioArtistico
            {
                IdEspacioArtistico = IdEspacioEnEdicion ?? 0,
                NombreEspacio = txtNombreEspacio.Text,
                TipoEspacio = txtTipoEspacio.Text,
                Descripcion = txtDescripcion.Text,
                Ficha = new FichaEspacio
                {
                    FotoRuta = fotos.Count > 0 ? fotos[0] : null,
                    Fotos = fotos,
                    Provincia = txtProvincia.Text.Trim(),
                    Ciudad = txtCiudad.Text.Trim(),
                    Direccion = txtDireccion.Text.Trim(),
                    CapacidadMaxima = capacidadOk ? capacidad : (int?)null,
                    PrecioHora = precioOk ? precio : (decimal?)null,
                    Moneda = ddlMoneda.SelectedValue,
                    TipoPiso = txtTipoPiso.Text.Trim(),
                    DetalleEquipamiento = txtEquipamientoDetalle.Text.Trim(),
                    SuperficieM2 = superficieOk ? superficie : (decimal?)null,
                    AlturaM = alturaOk ? altura : (decimal?)null,
                    CondicionesUso = txtCondicionesUso.Text.Trim(),
                    ReglasUso = txtReglasUso.Text.Trim(),
                    // CU-001-008: al editar, la ficha no toca la disponibilidad.
                    Disponibilidad = IdEspacioEnEdicion.HasValue ? null : disponibilidad
                }
            };

            foreach (ListItem item in cblEquipamiento.Items)
                if (item.Selected) espacio.Ficha.Equipamiento.Add(item.Value);

            return espacio;
        }

        private static bool TryLeerDecimal(string texto, out decimal valor)
        {
            return decimal.TryParse((texto ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out valor);
        }

        private string GuardarFoto(HttpPostedFile archivo)
        {
            string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (archivo.ContentLength > 3 * 1024 * 1024 ||
                (extension != ".jpg" && extension != ".jpeg" && extension != ".png"))
                throw new ArgumentException("Formato o tamaño de imagen no válido.");
            using (System.Drawing.Image original = System.Drawing.Image.FromStream(archivo.InputStream, true, true))
            {
                if ((original.RawFormat.Guid != System.Drawing.Imaging.ImageFormat.Jpeg.Guid &&
                     original.RawFormat.Guid != System.Drawing.Imaging.ImageFormat.Png.Guid) ||
                    (long)original.Width * original.Height > 20000000)
                    throw new ArgumentException("Imagen no válida o demasiado grande.");
                double escala = Math.Min(1.0, 1600.0 / Math.Max(original.Width, original.Height));
                using (System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(Math.Max(1, (int)(original.Width * escala)), Math.Max(1, (int)(original.Height * escala))))
                {
                    using (System.Drawing.Graphics dibujo = System.Drawing.Graphics.FromImage(imagen))
                    {
                        dibujo.Clear(System.Drawing.Color.White);
                        dibujo.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        dibujo.DrawImage(original, 0, 0, imagen.Width, imagen.Height);
                    }
                    string ruta = "~/Content/Uploads/Espacios/" + Guid.NewGuid().ToString("N") + ".jpg";
                    Directory.CreateDirectory(Server.MapPath("~/Content/Uploads/Espacios"));
                    imagen.Save(Server.MapPath(ruta), System.Drawing.Imaging.ImageFormat.Jpeg);
                    return ruta;
                }
            }
        }

        private void ErrorFormulario(string mensaje)
        {
            pnlFormularioMensaje.Visible = true;
            litFormularioMensaje.Text = mensaje;
        }

        protected string ObtenerFoto(EspacioArtistico espacio)
        {
            string ruta = espacio.Ficha != null && espacio.Ficha.Fotos != null && espacio.Ficha.Fotos.Count > 0
                ? espacio.Ficha.Fotos[0]
                : (espacio.Ficha == null ? null : espacio.Ficha.FotoRuta);
            if (!string.IsNullOrEmpty(ruta) && Regex.IsMatch(ruta, @"^~/Content/Uploads/Espacios/[a-f0-9]{32}\.jpg$"))
                return ruta;
            if (string.Equals(espacio.NombreEspacio, "Sala Principal StageUp", StringComparison.OrdinalIgnoreCase))
                return "~/Content/Images/Espacios/sala-principal-ia.png";
            if (string.Equals(espacio.NombreEspacio, "Estudio Fotográfico Norte", StringComparison.OrdinalIgnoreCase))
                return "~/Content/Images/Espacios/estudio-fotografico-ia.png";
            return "";
        }

        protected string EtiquetaFoto(EspacioArtistico espacio)
        {
            string ruta = ObtenerFoto(espacio);
            return string.IsNullOrEmpty(ruta) ? "Sin fotografía" : ruta.Contains("/Images/Espacios/") ? "IA · Imagen ilustrativa" : "Foto del espacio";
        }

        protected string ResumenFicha(EspacioArtistico espacio)
        {
            FichaEspacio ficha = espacio.Ficha;
            if (ficha == null) return "";
            List<string> partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(ficha.Ciudad)) partes.Add(ficha.Ciudad);
            if (ficha.CapacidadMaxima.HasValue) partes.Add("Hasta " + ficha.CapacidadMaxima + " personas");
            if (ficha.PrecioHora.HasValue) partes.Add(ficha.Moneda + " " + ficha.PrecioHora.Value.ToString("N2", CultureInfo.CurrentCulture) + " / hora");
            return string.Join(" · ", partes);
        }

        protected string ObtenerReputacion(EspacioArtistico espacio)
        {
            if (espacio == null || espacio.CantidadCalificaciones == 0)
            {
                return "Sin reseñas todavía";
            }

            return "★ " + espacio.PromedioCalificacion.ToString("0.0", CultureInfo.CurrentCulture) + " de 5 · " +
                espacio.CantidadCalificaciones + (espacio.CantidadCalificaciones == 1 ? " reseña" : " reseñas");
        }
    }
}
