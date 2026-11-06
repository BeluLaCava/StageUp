using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace StageUp.UI.Infraestructura
{
    public static class RespuestaErrorSegura
    {
        public static void Escribir(HttpContext contexto, Exception error)
        {
            bool datosInvalidos = false;
            int estado = 500;
            for (Exception actual = error; actual != null; actual = actual.InnerException)
            {
                HttpException http = actual as HttpException;
                if (http != null && http.GetHttpCode() >= 400 && http.GetHttpCode() < 500) estado = http.GetHttpCode();
                if (actual is HttpRequestValidationException || actual is ViewStateException) datosInvalidos = true;
            }
            if (datosInvalidos) estado = 400;
            string titulo = estado == 400 ? "No pudimos procesar el formulario" : estado == 404 ? "No encontramos esta página" : "No pudimos completar la operación";
            string mensaje = estado == 400
                ? "El formulario puede haber vencido o contener datos inválidos o demasiado largos. Volvé a abrir la página, revisá los campos e intentá nuevamente."
                : estado == 404 ? "Revisá la dirección o volvé al inicio de StageUp."
                : "Intentá nuevamente en unos minutos. Si el problema continúa, contactá a soporte.";
            HttpResponse respuesta = contexto.Response;
            respuesta.Clear();
            respuesta.StatusCode = estado;
            respuesta.TrySkipIisCustomErrors = true;
            respuesta.Cache.SetNoStore();
            respuesta.Headers["X-Content-Type-Options"] = "nosniff";
            if (contexto.Request.AppRelativeCurrentExecutionFilePath.EndsWith(".ashx", StringComparison.OrdinalIgnoreCase))
            {
                respuesta.ContentType = "application/json";
                respuesta.Write(new JavaScriptSerializer().Serialize(new { ok = false, mensaje = mensaje, codigo = "SOLICITUD_INVALIDA" }));
                return;
            }
            respuesta.ContentType = "text/html; charset=utf-8";
            string inicio = HttpUtility.HtmlAttributeEncode(VirtualPathUtility.ToAbsolute("~/Default.aspx"));
            respuesta.Write("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>StageUp</title>" +
                "<style>body{margin:0;background:#faf6f2;color:#482a27;font:17px/1.6 system-ui,sans-serif}main{max-width:620px;margin:12vh auto;padding:32px;background:#fffaf8;border:1px solid #e5d5cd;border-radius:24px}h1{color:#750d25;font-family:Georgia,serif;line-height:1.2}a{display:inline-block;margin-top:12px;padding:10px 20px;border-radius:12px;background:#750d25;color:white;text-decoration:none}a:focus-visible{outline:3px solid #bd8d63;outline-offset:4px}@media(max-width:700px){main{margin:24px;padding:24px}}</style></head><body><main>" +
                "<strong>StageUp</strong><h1>" + titulo + "</h1><p>" + mensaje + "</p><a href=\"" + inicio + "\">Volver al inicio</a></main></body></html>");
        }
    }
}
