using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class DetalleComprobante : Page
    {
        protected override void OnPreInit(EventArgs e)
        {
            if (GestorDeSesion.EstaAutenticadoComoInterno()) MasterPageFile = "~/Interno/PanelInterno.Master";
            base.OnPreInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            bool interno = GestorDeSesion.EstaAutenticadoComoInterno();
            if (!interno && !GestorDeSesion.EstaAutenticado()) { Response.Redirect("~/IniciarSesion.aspx"); return; }
            int id;
            if (interno)
            {
                lnkVolver.Text = "Volver al panel";
                lnkVolver.NavigateUrl = "~/Interno/PanelAdministrador.aspx";
                List<string> permisos;
                UsuarioInterno usuario;
                try { usuario = new BLL_UsuarioInterno().ObtenerParaSesion(GestorDeSesion.ObtenerIdUsuarioInternoActual().Value, out permisos); }
                catch (Exception) { MostrarError("No pudimos verificar tu acceso. Intentá nuevamente."); return; }
                if (usuario == null || !GestorDeSesion.ActualizarSesionInterna(usuario, permisos))
                { GestorDeSesion.CerrarSesionInterna(); Response.Redirect("~/IniciarSesion.aspx"); return; }
                if (permisos.Contains("GESTIONAR_PAGOS")) { lnkVolver.Text = "Volver a Cuentas corrientes"; lnkVolver.NavigateUrl = "~/Interno/GestionCuentasCorrientes.aspx"; }
                else if (permisos.Contains("CONSULTAR_HISTORIAL_FINANCIERO")) { lnkVolver.Text = "Volver al Historial financiero"; lnkVolver.NavigateUrl = "~/Interno/HistorialFinanciero.aspx"; }
            }
            else
            {
                var usuario = new BLL_UsuarioExterno().ObtenerPorId(GestorDeSesion.ObtenerIdUsuarioActual().Value);
                if (usuario == null || !GestorDeSesion.EsLaMismaCuentaExterna(usuario.IdUsuarioExterno, usuario.FechaAlta))
                { MostrarError("No pudimos verificar tu acceso. Volvé a iniciar sesión."); return; }
            }
            if (!int.TryParse(Request.QueryString["id"], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id <= 0)
            { MostrarError("No se indicó un comprobante válido."); return; }
            var bll = new BLL_Comprobante();
            var resultado = interno ? bll.ObtenerParaInterno(id, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value)
                : bll.ObtenerParaUsuario(id, GestorDeSesion.ObtenerIdUsuarioActual().Value);
            if (!resultado.Exitoso) { MostrarError(resultado.Mensaje); return; }
            Mostrar(resultado.Valor, interno);
        }

        private void Mostrar(Comprobante nota, bool interno)
        {
            var emisor = BLL_Pago.ObtenerDatosFacturacion(nota.FechaEmision);
            bool anulada = nota.Estado == "Anulado";
            litTipo.Text = nota.Tipo;
            litTitulo.Text = nota.Tipo == "NC" ? "Nota de crédito" : "Nota de débito";
            Title = litTitulo.Text + " " + nota.Numero + " | StageUp";
            litNumero.Text = nota.Numero;
            litEmisor.Text = emisor.RazonSocial;
            litEmisorDatos.Text = emisor.DatosFiscales;
            litFecha.Text = nota.FechaEmision.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            litUsuario.Text = nota.NombreUsuario;
            litCorreo.Text = nota.CorreoUsuario;
            litCuenta.Text = nota.RolCuenta == BLL_CuentaCorriente.RolGestor ? "Gestor" : "Solicitante";
            litReserva.Text = nota.IdReserva.HasValue ? "N° " + nota.IdReserva.Value.ToString(CultureInfo.InvariantCulture) : "Sin reserva asociada";
            litOrigen.Text = nota.Origen == "Manual" ? "Ajuste de cuenta corriente" : nota.Origen == "Cancelacion" ? "Cancelación de reserva" : nota.Origen == "PenalidadCancelacion" ? "Cargo por cancelación" : nota.Origen;
            litEstado.Text = nota.Estado;
            litMotivo.Text = nota.Motivo;
            litEtiquetaImporte.Text = anulada ? "Importe original" : nota.Tipo == "NC" ? "Importe acreditado" : "Importe debitado";
            litImporte.Text = BLL_CuentaCorriente.FormatearImporte(nota.Importe, nota.Moneda) + " " + nota.Moneda;
            litEfecto.Text = anulada ? "Consultá la cuenta corriente para ver la anulación y el saldo vigente."
                : nota.Tipo == "NC" ? "Este crédito se registra en la cuenta corriente de StageUp. No representa una devolución a la tarjeta. El saldo disponible depende del conjunto de movimientos de la cuenta."
                : "Este débito se registra en la cuenta corriente de StageUp. No representa un nuevo cobro a la tarjeta.";
            pnlAnulada.Visible = anulada;
            litAnulacion.Text = (nota.FechaAnulacion.HasValue ? nota.FechaAnulacion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " · " : "") + nota.MotivoAnulacion;
            pnlNota.Visible = phImprimir.Visible = true;
            if (!interno)
            {
                lnkVolver.NavigateUrl = "~/MiCuentaCorriente.aspx" + (nota.RolCuenta == BLL_CuentaCorriente.RolGestor ? "?cuenta=Gestor" : "");
                if (nota.IdReserva.HasValue && new BLL_Comprobante().PuedeConsultarReserva(nota.IdReserva.Value, nota.IdUsuarioExterno))
                { lnkReserva.Visible = true; lnkReserva.NavigateUrl = "~/DetalleReserva.aspx?id=" + nota.IdReserva.Value.ToString(CultureInfo.InvariantCulture); }
            }
        }
        private void MostrarError(string mensaje) { pnlError.Visible = true; pnlNota.Visible = phImprimir.Visible = false; litError.Text = mensaje; }
    }
}
