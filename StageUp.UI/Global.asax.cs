using System;
using System.Web;
using System.Web.UI;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{
    public class Global : HttpApplication
    {
        // Sesión abierta después de restaurar un backup (script 79): antes de
        // que la página o el handler procese nada, se comprueba que la cuenta
        // de la sesión siga existiendo en la base. Si no, se cierra la sesión
        // y las páginas llevan al inicio de sesión con el motivo; los handlers
        // (.ashx) siguen y responden como sin sesión.
        protected void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            HttpContext contexto = HttpContext.Current;
            if (contexto == null || contexto.Session == null)
            {
                return;
            }

            ResultadoRevalidacionSesion resultado;
            try
            {
                resultado = ControlSesionBaseDatos.Verificar();
            }
            catch (Exception)
            {
                // Un problema al verificar no debe impedir usar el sitio.
                return;
            }

            if (resultado == ResultadoRevalidacionSesion.Invalida && contexto.Handler is Page)
            {
                contexto.Response.Redirect("~/IniciarSesion.aspx?motivo=" + ControlSesionBaseDatos.MotivoCierre, false);
                contexto.ApplicationInstance.CompleteRequest();
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception excepcionOriginal = Server.GetLastError();
            if (excepcionOriginal == null)
            {
                return;
            }

            Exception excepcion = excepcionOriginal is HttpUnhandledException && excepcionOriginal.InnerException != null
                ? excepcionOriginal.InnerException
                : excepcionOriginal;

            RegistrarEnBitacora(excepcion);
        }

        private void RegistrarEnBitacora(Exception excepcion)
        {
            try
            {
                string url = Request != null ? Request.Url.ToString() : "(sin request)";
                string descripcion = excepcion.GetType().Name + " en " + url + ": " + excepcion.Message;
                if (descripcion.Length > 1900)
                {
                    descripcion = descripcion.Substring(0, 1900);
                }

                new BLL_Bitacora().Registrar(
                    idUsuarioExternoResponsable: null,
                    tipoOperacion: "ERROR",
                    tipoEntidadAfectada: "Sistema",
                    idEntidadAfectada: null,
                    descripcionOperacion: descripcion,
                    origenOperacion: "Global.asax");
            }
            catch
            {
                // Si falla el registro en bitácora, no debe tapar el error original.
            }
        }
    }
}
