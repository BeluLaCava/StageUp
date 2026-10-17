using System;
using System.Web;
using System.Web.SessionState;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno.Comercializacion
{
    // CU-004-005: descarga del archivo de una pieza promocional, solo para
    // usuarios internos con el permiso. ?id={id} descarga; &ver=1 muestra
    // una imagen en el navegador (vista previa del detalle).
    public class DescargarContenido : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno() ||
                !GestorDeSesion.TienePermisoInterno(BLL_ContenidoPromocional.PermisoGestionar))
            {
                context.Response.StatusCode = 403;
                return;
            }

            int id;
            ContenidoPromocional contenido = int.TryParse(context.Request.QueryString["id"], out id)
                ? new BLL_ContenidoPromocional().Obtener(id)
                : null;
            string ruta = contenido == null ? null : ArchivoContenidoPromocional.RutaFisica(contenido.RutaArchivo);
            if (ruta == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            bool enLinea = context.Request.QueryString["ver"] == "1" && contenido.EsImagen;
            string nombre = (contenido.NombreArchivoOriginal ?? "contenido").Replace("\"", string.Empty);

            context.Response.Clear();
            context.Response.ContentType = contenido.TipoMimeArchivo ?? "application/octet-stream";
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            context.Response.AddHeader("Content-Disposition",
                (enLinea ? "inline" : "attachment") + "; filename=\"" + SoloAscii(nombre) + "\"; filename*=UTF-8''" + Uri.EscapeDataString(nombre));
            context.Response.Cache.SetCacheability(HttpCacheability.Private);
            context.Response.TransmitFile(ruta);
        }

        private static string SoloAscii(string nombre)
        {
            char[] letras = nombre.ToCharArray();
            for (int i = 0; i < letras.Length; i++)
            {
                if (letras[i] < 32 || letras[i] > 126 || letras[i] == ';' || letras[i] == '\\')
                {
                    letras[i] = '_';
                }
            }
            return new string(letras);
        }
    }
}
