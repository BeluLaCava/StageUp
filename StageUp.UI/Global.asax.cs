using System;
using System.Web;
using StageUp.BLL;

namespace StageUp.UI
{
    public class Global : HttpApplication
    {
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
