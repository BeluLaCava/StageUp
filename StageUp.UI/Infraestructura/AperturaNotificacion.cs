using System;
using System.Web;
using System.Web.Hosting;
using StageUp.BLL;

namespace StageUp.UI.Infraestructura
{




    public static class AperturaNotificacion
    {
        public static string ObtenerDestino(int idNotificacion, int idUsuarioExterno, out string error)
        {
            ResultadoOperacion<string> resultado = new BLL_Notificacion().ResolverDestino(idNotificacion, idUsuarioExterno);
            if (!resultado.Exitoso)
            {
                error = resultado.Mensaje;
                return null;
            }

            string pagina = resultado.Valor;
            int signo = pagina.IndexOf('?');
            if (signo >= 0)
            {
                pagina = pagina.Substring(0, signo);
            }

            bool existe;
            try
            {
                existe = HostingEnvironment.VirtualPathProvider == null ||
                    HostingEnvironment.VirtualPathProvider.FileExists(VirtualPathUtility.ToAbsolute(pagina));
            }
            catch (HttpException)
            {
                existe = false;
            }

            if (!existe)
            {
                error = "No es posible acceder al detalle relacionado: la página ya no existe.";
                return null;
            }

            error = null;
            return resultado.Valor;
        }
    }
}
