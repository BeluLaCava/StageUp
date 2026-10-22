using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{








    public class SoporteNovedades : IHttpHandler, IReadOnlySessionState
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

            BLL_Ticket bll = new BLL_Ticket();
            string vista = context.Request.QueryString["vista"];
            int id;
            int.TryParse(context.Request.QueryString["id"], out id);
            string marca = null;
            bool autorizado;

            switch (vista)
            {
                case "usuario-listado":
                case "usuario-detalle":
                    int? idUsuario = GestorDeSesion.EstaAutenticado() ? GestorDeSesion.ObtenerIdUsuarioActual() : null;
                    autorizado = idUsuario.HasValue;
                    if (autorizado)
                    {
                        marca = vista == "usuario-listado"
                            ? bll.ObtenerMarcaListado(idUsuario.Value)
                            : bll.ObtenerMarcaSolicitud(id, idUsuario.Value);
                    }
                    break;

                case "interno-listado":
                case "interno-detalle":
                    autorizado = GestorDeSesion.EstaAutenticadoComoInterno() && GestorDeSesion.TienePermisoInterno(BLL_Ticket.PermisoGestionar);
                    if (autorizado)
                    {
                        marca = vista == "interno-listado"
                            ? bll.ObtenerMarcaListado(null)
                            : bll.ObtenerMarcaSolicitud(id, null);
                    }
                    break;

                default:
                    autorizado = false;
                    break;
            }

            if (!autorizado)
            {
                context.Response.StatusCode = 403;
            }

            context.Response.Write(new JavaScriptSerializer().Serialize(new { ok = autorizado && marca != null, marca = marca }));
        }
    }
}
