using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{



    public partial class HerramientasSeguridad : Page
    {
        private const string PermisoRequerido = "GESTIONAR_ENCRIPTACION";

        private readonly BLL_ExportacionSegura _bllExportacion = new BLL_ExportacionSegura();
        private readonly BLL_ClaveCifrado _bllClaves = new BLL_ClaveCifrado();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IsPostBack)
            {
                CargarClaves();
            }
        }

        protected void btnRotar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            ResultadoOperacion<string> resultado = _bllClaves.Rotar(
                GestorDeSesion.ObtenerIdUsuarioInternoActual().Value, txtMotivo.Text, chkConfirmar.Checked);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso || resultado.CodigoAlternativo != null);
            if (resultado.Exitoso)
            {
                txtMotivo.Text = string.Empty;
                chkConfirmar.Checked = false;
            }

            CargarClaves();
        }

        protected void btnReintentar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            ResultadoOperacion<string> resultado = _bllClaves.ReintentarPendientes(GestorDeSesion.ObtenerIdUsuarioInternoActual().Value);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarClaves();
        }

        protected void btnDescifrar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            pnlMensaje.Visible = false;
            pnlResultadoDescifrado.Visible = false;

            if (!fuArchivoCifrado.HasFile)
            {
                MostrarMensaje("Elegí un archivo exportado antes de descifrar.", true);
                return;
            }

            string envoltorioXml;
            using (StreamReader lector = new StreamReader(fuArchivoCifrado.FileContent, Encoding.UTF8))
            {
                envoltorioXml = lector.ReadToEnd();
            }

            ResultadoOperacion<string> resultado = _bllExportacion.ImportarYDescifrar(envoltorioXml);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            txtXmlDescifrado.Text = resultado.Valor;
            pnlResultadoDescifrado.Visible = true;
        }

        protected static string Responsable(object nombre)
        {
            string texto = Convert.ToString(nombre);
            return string.IsNullOrWhiteSpace(texto) ? "Automática (sistema)" : texto;
        }

        private void CargarClaves()
        {
            List<ClaveCifrado> versiones = _bllClaves.Listar();
            ClaveCifrado activa = versiones.FirstOrDefault(v => v.Estado == "Activa");

            litVersionActiva.Text = activa == null ? "Sin claves generadas" : activa.Version.ToString(CultureInfo.InvariantCulture);
            litHuellaActiva.Text = activa == null ? "—" : activa.Huella;
            litFechaActiva.Text = activa == null ? "—"
                : activa.FechaAlta.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " · " + Responsable(activa.NombreUsuarioAlta);
            litTarjetasActiva.Text = activa == null ? "0" : activa.TarjetasProtegidas.ToString(CultureInfo.InvariantCulture);

            bool hayMaestra = BLL_ClaveCifrado.HayClaveMaestra();
            pnlSinClaveMaestra.Visible = !hayMaestra;
            pnlRotar.Visible = hayMaestra;
            btnRotar.Text = activa == null ? "Generar el primer par de claves" : "Rotar claves";

            int pendientes = _bllClaves.ContarPendientesDeReenvolver();
            pnlPendientes.Visible = pendientes > 0;
            litPendientes.Text = pendientes == 1
                ? "Hay 1 tarjeta protegida con una versión retirada. "
                : "Hay " + pendientes.ToString(CultureInfo.InvariantCulture) + " tarjetas protegidas con una versión retirada. ";

            rptVersiones.DataSource = versiones;
            rptVersiones.DataBind();
            rptVersiones.Visible = versiones.Count > 0;
            pnlSinVersiones.Visible = versiones.Count == 0;
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
