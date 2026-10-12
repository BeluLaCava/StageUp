using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Ayuda
{
    // CU-003-001 A4 a A7: el asistente del centro de ayuda le pide los datos
    // a este handler (JSON). Solo lee contenido activo y registra las
    // consultas escritas; no modifica información operativa.
    //   GET  ?accion=temas
    //   GET  ?accion=preguntas&tema=ID
    //   GET  ?accion=respuesta&id=ID
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
                    case "temas":
                        ResultadoOperacion<List<CategoriaAyuda>> temas = asistente.ObtenerTemas();
                        respuesta = temas.Exitoso
                            ? (object)new { ok = true, temas = temas.Valor.Select(t => new { id = t.IdCategoriaAyuda, nombre = t.Nombre, cantidad = t.CantidadAsistente }) }
                            : Error(temas.Mensaje, temas.CodigoAlternativo);
                        break;

                    case "preguntas":
                        ResultadoOperacion<List<Faq>> preguntas = asistente.ObtenerPreguntas(Entero(context.Request["tema"]));
                        respuesta = preguntas.Exitoso
                            ? (object)new { ok = true, preguntas = preguntas.Valor.Select(Resumen) }
                            : Error(preguntas.Mensaje, preguntas.CodigoAlternativo);
                        break;

                    case "respuesta":
                        ResultadoOperacion<Faq> faq = asistente.ObtenerRespuesta(Entero(context.Request["id"]));
                        respuesta = faq.Exitoso
                            ? (object)new { ok = true, respuesta = Completa(faq.Valor) }
                            : Error(faq.Mensaje, faq.CodigoAlternativo);
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
                                respuesta = consulta.Valor.Respuesta == null ? null : Completa(consulta.Valor.Respuesta),
                                sugerencias = consulta.Valor.Sugerencias.Select(Resumen)
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

        private static object Error(string mensaje, string codigo)
        {
            return new { ok = false, mensaje = mensaje, codigo = codigo };
        }

        private static object Resumen(Faq faq)
        {
            return new { id = faq.IdFaq, pregunta = faq.Pregunta, categoria = faq.NombreCategoria };
        }

        private static object Completa(Faq faq)
        {
            return new { id = faq.IdFaq, pregunta = faq.Pregunta, respuesta = faq.Respuesta, categoria = faq.NombreCategoria, idCategoria = faq.IdCategoriaAyuda };
        }

        private static int Entero(string valor)
        {
            int numero;
            return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out numero) ? numero : 0;
        }
    }
}
