using System;
using System.Web;
using StageUp.BE.Entidades;

namespace StageUp.Seguridad
{
    public static class GestorDeSesion
    {
        private const string ClaveIdUsuario = "StageUp.Sesion.IdUsuarioExterno";
        private const string ClaveNombreCompleto = "StageUp.Sesion.NombreCompleto";
        private const string ClavePerfil = "StageUp.Sesion.PerfilUsuario";

        private const string ClaveIdUsuarioInterno = "StageUp.Sesion.IdUsuarioInterno";
        private const string ClaveNombreCompletoInterno = "StageUp.Sesion.NombreCompletoInterno";
        private const string ClaveIdRolInterno = "StageUp.Sesion.IdRolInterno";

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

        public static void ActualizarPerfilEnSesion(string perfilUsuario)
        {
            HttpContext.Current.Session[ClavePerfil] = perfilUsuario;
        }

        public static void IniciarSesionInterna(UsuarioInterno usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException("usuario");
            }

            HttpContext.Current.Session[ClaveIdUsuarioInterno] = usuario.IdUsuarioInterno;
            HttpContext.Current.Session[ClaveNombreCompletoInterno] = usuario.Nombre + " " + usuario.Apellido;
            HttpContext.Current.Session[ClaveIdRolInterno] = usuario.IdRolInterno;
        }

        public static void CerrarSesionInterna()
        {
            HttpContext.Current.Session.Remove(ClaveIdUsuarioInterno);
            HttpContext.Current.Session.Remove(ClaveNombreCompletoInterno);
            HttpContext.Current.Session.Remove(ClaveIdRolInterno);
            HttpContext.Current.Session.Abandon();
        }

        public static bool EstaAutenticadoComoInterno()
        {
            return HttpContext.Current.Session[ClaveIdUsuarioInterno] != null;
        }

        public static int? ObtenerIdUsuarioInternoActual()
        {
            object valor = HttpContext.Current.Session[ClaveIdUsuarioInterno];
            return valor == null ? (int?)null : (int)valor;
        }

        public static string ObtenerNombreCompletoInternoActual()
        {
            object valor = HttpContext.Current.Session[ClaveNombreCompletoInterno];
            return valor == null ? null : valor.ToString();
        }
    }
}
