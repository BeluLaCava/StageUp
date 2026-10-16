using System;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Ayuda
{
    // CU-003-001 A4 a A7: el asistente guiado del centro de ayuda le pide
    // los pasos a este handler (JSON). Solo lee contenido activo y registra
    // las consultas escritas; no modifica información operativa.
    //   GET  ?accion=nodo[&codigo=CODIGO]   (sin código: el inicio)
    //   POST accion=consultar&texto=...
    public class AsistenteAyuda : IHttpHandler, IReadOnlySessionState
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            BLL_AsistenteAyuda asistente = new BLL_AsistenteAyuda();
            string accion = context.Request["accion"];
            object respuesta;

            try
            {
                switch (accion)
                {
                    case "nodo":
                        ResultadoOperacion<NodoAsistente> nodo = asistente.ObtenerNodo(context.Request.QueryString["codigo"]);
                        respuesta = nodo.Exitoso
                            ? (object)new { ok = true, nodo = Paso(nodo.Valor) }
                            : Error(nodo.Mensaje, nodo.CodigoAlternativo);
                        break;

                    case "consultar":
                        if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = 405;
                            respuesta = Error("Método no permitido.", null);
                            break;
                        }

                        ResultadoOperacion<RespuestaAsistente> consulta = asistente.Consultar(
                            context.Request.Form["texto"], GestorDeSesion.ObtenerIdUsuarioActual());
                        respuesta = consulta.Exitoso
                            ? (object)new
                            {
                                ok = true,
                                reconocida = consulta.Valor.Reconocida,
                                nodo = consulta.Valor.Nodo == null ? null : Paso(consulta.Valor.Nodo),
                                preguntaFrecuente = consulta.Valor.PreguntaFrecuente == null
                                    ? null
                                    : new { pregunta = consulta.Valor.PreguntaFrecuente.Pregunta, respuesta = consulta.Valor.PreguntaFrecuente.Respuesta },
                                sugerencias = consulta.Valor.Sugerencias.Select(s => new { codigo = s.Codigo, texto = s.Recorrido })
                            }
                            : Error(consulta.Mensaje, consulta.CodigoAlternativo);
                        break;

                    default:
                        context.Response.StatusCode = 400;
                        respuesta = Error("Acción no válida.", null);
                        break;
                }
            }
            catch (Exception)
            {
                respuesta = Error(BLL_AsistenteAyuda.MensajeNoDisponible, "A7");
            }

            context.Response.Write(new JavaScriptSerializer().Serialize(respuesta));
        }

        private static object Paso(NodoAsistente nodo)
        {
            return new
            {
                codigo = nodo.Codigo,
                esInicio = nodo.Codigo == BLL_AsistenteAyuda.CodigoInicio,
                esRespuesta = nodo.EsRespuesta,
                mensaje = nodo.Mensaje,
                recorrido = nodo.Recorrido,
                textoBoton = nodo.TextoBotonLista,
                opciones = nodo.Opciones.Select(o => new { codigo = o.Codigo, texto = o.TextoOpcion, descripcion = o.DescripcionOpcion, icono = o.Icono }),
                acciones = nodo.Acciones
                    .Select(a => new { texto = a.Texto, tipo = a.TipoAccion, codigo = a.TipoAccion == AccionNodoAsistente.TipoNodo ? a.Destino : null, url = UrlInterna(a) })
                    .Where(a => a.tipo != AccionNodoAsistente.TipoEnlace || a.url != null)
            };
        }

        // Solo páginas internas de StageUp ("~/Pagina.aspx").
        private static string UrlInterna(AccionNodoAsistente accion)
        {
            if (accion.TipoAccion != AccionNodoAsistente.TipoEnlace || string.IsNullOrEmpty(accion.Destino))
            {
                return null;
            }

            string destino = accion.Destino.Trim();
            if (!destino.StartsWith("~/", StringComparison.Ordinal) || destino.Contains("//") || destino.Contains(":") || destino.Contains(".."))
            {
                return null;
            }

            return VirtualPathUtility.ToAbsolute(destino.Split('?')[0]) + (destino.Contains("?") ? destino.Substring(destino.IndexOf('?')) : string.Empty);
        }

        private static object Error(string mensaje, string codigo)
        {
            return new { ok = false, mensaje = mensaje, codigo = codigo };
        }
    }
}
