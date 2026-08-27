using System;
using System.Web;
using StageUp.BE.Entidades;

namespace StageUp.Seguridad
{
    /// <summary>
    /// Manejo de la sesión del usuario externo autenticado. Encapsula el
    /// uso de HttpContext.Current.Session para que la UI y la BLL no
    /// dependan directamente de la API de sesión de ASP.NET.
    /// </summary>
    public static class GestorDeSesion
    {
        private const string ClaveIdUsuario = "StageUp.Sesion.IdUsuarioExterno";
        private const string ClaveNombreCompleto = "StageUp.Sesion.NombreCompleto";
        private const string ClavePerfil = "StageUp.Sesion.PerfilUsuario";

        public static void IniciarSesion(UsuarioExterno usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException("usuario");
            }

            HttpContext.Current.Session[ClaveIdUsuario] = usuario.IdUsuarioExterno;
            HttpContext.Current.Session[ClaveNombreCompleto] = usuario.Nombre + " " + usuario.Apellido;
            HttpContext.Current.Session[ClavePerfil] = usuario.PerfilUsuario;
        }

        public static void CerrarSesion()
        {
            HttpContext.Current.Session.Remove(ClaveIdUsuario);
            HttpContext.Current.Session.Remove(ClaveNombreCompleto);
            HttpContext.Current.Session.Remove(ClavePerfil);
            HttpContext.Current.Session.Abandon();
        }

        public static bool EstaAutenticado()
        {
            return HttpContext.Current.Session[ClaveIdUsuario] != null;
        }

        public static int? ObtenerIdUsuarioActual()
        {
            object valor = HttpContext.Current.Session[ClaveIdUsuario];
            return valor == null ? (int?)null : (int)valor;
        }

        public static string ObtenerNombreCompletoActual()
        {
            object valor = HttpContext.Current.Session[ClaveNombreCompleto];
            return valor == null ? null : valor.ToString();
        }

        public static string ObtenerPerfilActual()
        {
            object valor = HttpContext.Current.Session[ClavePerfil];
            return valor == null ? null : valor.ToString();
        }
    }
}
