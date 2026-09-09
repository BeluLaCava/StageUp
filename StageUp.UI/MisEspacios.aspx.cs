using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class MisEspacios : Page
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

<<<<<<< Updated upstream
=======
        protected bool FichaCompletaActiva { get { return _bllEspacio.FichaCompletaHabilitada; } }

        private string FotoRutaActual
        {
            get { return ViewState["FotoRutaActual"] as string; }
            set { ViewState["FotoRutaActual"] = value; }
        }

        private List<string> FotosRutasActuales
        {
            get
            {
                string valor = ViewState["FotosRutasActuales"] as string;
                return string.IsNullOrEmpty(valor)
                    ? new List<string>()
                    : new JavaScriptSerializer().Deserialize<List<string>>(valor);
            }
            set { ViewState["FotosRutasActuales"] = new JavaScriptSerializer().Serialize(value ?? new List<string>()); }
        }

>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
=======

        protected override void OnPreRender(EventArgs e)
        {
            if (pnlFormularioEspacio.Visible)
                btnGuardarEspacio.Text = FichaCompletaActiva ? "Guardar espacio" : "Guardar datos básicos";
            base.OnPreRender(e);
        }

        private void CargarFicha(EspacioArtistico espacio)
        {
            FichaEspacio ficha = espacio.Ficha ?? new FichaEspacio();
            FotoRutaActual = ficha.FotoRuta;
            var fotos = ficha.FotosRutas == null ? new List<string>() : new List<string>(ficha.FotosRutas);
            if (!string.IsNullOrEmpty(ficha.FotoRuta) && !fotos.Contains(ficha.FotoRuta))
                fotos.Insert(0, ficha.FotoRuta);
            FotosRutasActuales = fotos;
            imgFotoActual.ImageUrl = ObtenerFoto(espacio);
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
                var espacio = new EspacioArtistico
                {
                    IdEspacioArtistico = IdEspacioEnEdicion ?? 0,
                    NombreEspacio = txtNombreEspacio.Text,
                    TipoEspacio = txtTipoEspacio.Text,
                    Descripcion = txtDescripcion.Text,
                    Ficha = new FichaEspacio
                    {
                        FotoRuta = FotoRutaActual,
                        FotosRutas = FotosRutasActuales,
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
                ResultadoOperacion validacion = _bllEspacio.ValidarFicha(espacio);
                if (!validacion.Exitoso)
                {
                    ErrorFormulario(validacion.Mensaje);
                    return;
                }
                if (archivoFoto.PostedFiles.Count > 0)
                {
                    var fotos = FotosRutasActuales;
                    if (fotos.Count + archivoFoto.PostedFiles.Count > 8)
                    {
                        ErrorFormulario("Podés cargar hasta ocho fotografías por espacio.");
                        return;
                    }
                    foreach (HttpPostedFile archivo in archivoFoto.PostedFiles)
                        fotos.Add(GuardarFoto(archivo));
                    FotosRutasActuales = fotos;
                    if (string.IsNullOrEmpty(FotoRutaActual))
                        FotoRutaActual = fotos[0];
                    espacio.Ficha.FotoRuta = FotoRutaActual;
                    espacio.Ficha.FotosRutas = fotos;
                    imgFotoActual.ImageUrl = FotoRutaActual;
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
                ErrorFormulario("Revisá la imagen y los horarios ingresados.");
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
                using (var imagen = new System.Drawing.Bitmap(Math.Max(1, (int)(original.Width * escala)), Math.Max(1, (int)(original.Height * escala))))
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
            string ruta = espacio.Ficha == null ? null : espacio.Ficha.FotoRuta;
            if (string.IsNullOrEmpty(ruta) && espacio.Ficha != null && espacio.Ficha.FotosRutas != null && espacio.Ficha.FotosRutas.Count > 0)
                ruta = espacio.Ficha.FotosRutas[0];
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
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(ficha.Ciudad)) partes.Add(ficha.Ciudad);
            if (ficha.CapacidadMaxima.HasValue) partes.Add("Hasta " + ficha.CapacidadMaxima + " personas");
            if (ficha.PrecioHora.HasValue) partes.Add(ficha.Moneda + " " + ficha.PrecioHora.Value.ToString("N2", CultureInfo.GetCultureInfo("es-AR")) + " / hora");
            return string.Join(" · ", partes);
        }
>>>>>>> Stashed changes
    }
}
