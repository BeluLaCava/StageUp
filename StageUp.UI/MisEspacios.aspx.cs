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
        private readonly BLL_UsuarioExterno _bllUsuario = new BLL_UsuarioExterno();
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

            pnlPendienteGestor.Visible = perfil == PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString();
            pnlNoGestor.Visible = perfil == PerfilUsuarioExterno.ExternoSolicitante.ToString();
            pnlPanelGestor.Visible = perfil == PerfilUsuarioExterno.GestorEspacios.ToString();
            lnkNuevoEspacio.Visible = pnlPanelGestor.Visible;

            if (pnlPanelGestor.Visible)
            {
                if (!IsPostBack)
                {
                    CargarMisEspacios();
                }

                CantidadSolicitudesPendientes = ContarSolicitudesPendientes();
                litBadgeSolicitudes.Text = CantidadSolicitudesPendientes > 0
                    ? " <span class=\"gestor-subnav-badge\">" + CantidadSolicitudesPendientes + "</span>"
                    : string.Empty;
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
            MostrarMensaje(resultado.Mensaje, esError: false);
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
            foreach (ListItem item in cblEquipamiento.Items)
                item.Selected = ficha.Equipamiento != null && ficha.Equipamiento.Contains(item.Value);
            hdnDisponibilidad.Value = new JavaScriptSerializer().Serialize(ficha.Disponibilidad ?? new List<FranjaEspacio>());
        }

        private void GuardarFichaCompleta(int idUsuarioGestor)
        {
            try
            {
                int capacidad;
                decimal precio;
                if (!int.TryParse(txtCapacidad.Text, out capacidad) ||
                    !decimal.TryParse(txtPrecioHora.Text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out precio))
                {
                    ErrorFormulario("Revisá la capacidad y el precio por hora. Usá coma o punto para los decimales, sin separador de miles.");
                    return;
                }
                if (hdnDisponibilidad.Value.Length > 64000)
                {
                    ErrorFormulario("Hay demasiados horarios cargados.");
                    return;
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
                    ErrorFormulario("Podés cargar hasta " + MaxFotosPorEspacio + " fotos por espacio. Quitá alguna antes de agregar más.");
                    return;
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
                        CapacidadMaxima = capacidad,
                        PrecioHora = precio,
                        Moneda = ddlMoneda.SelectedValue,
                        TipoPiso = txtTipoPiso.Text.Trim(),
                        DetalleEquipamiento = txtEquipamientoDetalle.Text.Trim(),
                        Disponibilidad = new JavaScriptSerializer().Deserialize<List<FranjaEspacio>>(hdnDisponibilidad.Value)
                    }
                };
                foreach (ListItem item in cblEquipamiento.Items)
                    if (item.Selected) espacio.Ficha.Equipamiento.Add(item.Value);

                if (archivoFoto.HasFiles)
                {
                    foreach (HttpPostedFile archivo in archivoFoto.PostedFiles)
                    {
                        if (archivo == null || archivo.ContentLength == 0) continue;
                        espacio.Ficha.Fotos.Add(GuardarFoto(archivo));
                    }
                    if (espacio.Ficha.Fotos.Count > 0)
                    {
                        espacio.Ficha.FotoRuta = espacio.Ficha.Fotos[0];
                    }
                }

                ResultadoOperacion validacion = _bllEspacio.ValidarFicha(espacio);
                if (!validacion.Exitoso)
                {
                    ErrorFormulario(validacion.Mensaje);
                    return;
                }

                ResultadoOperacion<int> resultado = _bllEspacio.GuardarFicha(espacio, idUsuarioGestor);
                if (!resultado.Exitoso)
                {
                    ErrorFormulario(resultado.Mensaje);
                    return;
                }
                LimpiarFormulario();
                MostrarMensaje(resultado.Mensaje, false);
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
