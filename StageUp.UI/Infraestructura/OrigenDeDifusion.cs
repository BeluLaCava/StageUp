using System;
using System.Text.RegularExpressions;
using System.Web;

namespace StageUp.UI.Infraestructura
{
    // CU-004-004: código del origen de un enlace de difusión
    // (cualquier página con ?origen=codigo). Se guarda en una cookie por 30
    // días para que, si la persona se registra después, la cuenta quede
    // asociada a ese origen.
    public static class OrigenDeDifusion
    {
        private const string NombreCookie = "StageUp_Origen";
        private const int DiasVigencia = 30;
        private static readonly Regex PatronCodigo = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");

        public static void Capturar(HttpContext contexto)
        {
            string codigo = Normalizar(contexto.Request.QueryString["origen"]);
            if (codigo == null)
            {
                return;
            }

            HttpCookie cookie = new HttpCookie(NombreCookie, codigo)
            {
                HttpOnly = true,
                Secure = contexto.Request.IsSecureConnection,
                Expires = DateTime.Now.AddDays(DiasVigencia),
                Path = "/"
            };
            contexto.Response.Cookies.Set(cookie);
        }

        // El código de la URL actual o, si no hay, el guardado.
        public static string Leer(HttpContext contexto)
        {
            string codigo = Normalizar(contexto.Request.QueryString["origen"]);
            if (codigo != null)
            {
                return codigo;
            }

            HttpCookie cookie = contexto.Request.Cookies[NombreCookie];
            return cookie == null ? null : Normalizar(cookie.Value);
        }

        public static void Olvidar(HttpContext contexto)
        {
            if (contexto.Request.Cookies[NombreCookie] == null)
            {
                return;
            }

            contexto.Response.Cookies.Set(new HttpCookie(NombreCookie, string.Empty)
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(-1),
                Path = "/"
            });
        }

        private static string Normalizar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            string codigo = valor.Trim().ToLowerInvariant();
            return codigo.Length >= 2 && codigo.Length <= 40 && PatronCodigo.IsMatch(codigo) ? codigo : null;
        }
    }
}
