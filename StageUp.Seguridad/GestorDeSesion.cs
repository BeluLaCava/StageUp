using System;
using System.Collections.Generic;
using System.Web;
using StageUp.BE.Entidades;

namespace StageUp.Seguridad
{
    public static class GestorDeSesion
    {
        private const string ClaveIdUsuario = "StageUp.Sesion.IdUsuarioExterno";
        private const string ClaveNombreCompleto = "StageUp.Sesion.NombreCompleto";
        private const string ClavePerfil = "StageUp.Sesion.PerfilUsuario";
        private const string ClaveIdIdioma = "StageUp.Sesion.IdIdioma";
        private const string ClaveCodigoIdioma = "StageUp.Sesion.CodigoIdioma";

        private const string ClaveUltimaVerificacionCuenta = "StageUp.Sesion.UltimaVerificacionCuenta";
        private const int MinutosEntreVerificacionesCuenta = 5;

        private const string ClaveIdUsuarioInterno = "StageUp.Sesion.IdUsuarioInterno";
        private const string ClaveNombreCompletoInterno = "StageUp.Sesion.NombreCompletoInterno";
        private const string ClaveIdRolInterno = "StageUp.Sesion.IdRolInterno";
        private const string ClavePermisosInterno = "StageUp.Sesion.PermisosInterno";





        private const string ClaveAltaCuenta = "StageUp.Sesion.FechaAltaCuenta";
        private const string ClaveAltaCuentaInterna = "StageUp.Sesion.FechaAltaCuentaInterna";
        private const string ClaveMarcaBaseDatos = "StageUp.Sesion.MarcaBaseDatos";
        private const string ClaveAvisoRestauracion = "StageUp.Sesion.AvisoRestauracion";




        private const string ClaveSesionSuspendida = "StageUp.Pedido.SesionSuspendida";

        public static void SuspenderSesionEnEstePedido()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items[ClaveSesionSuspendida] = true;
            }
        }

        public static bool SesionSuspendida
        {
            get { return HttpContext.Current != null && HttpContext.Current.Items[ClaveSesionSuspendida] is bool; }
        }

        public static void IniciarSesion(UsuarioExterno usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException("usuario");
            }

            HttpContext.Current.Session[ClaveIdUsuario] = usuario.IdUsuarioExterno;
            HttpContext.Current.Session[ClaveNombreCompleto] = usuario.Nombre + " " + usuario.Apellido;
            HttpContext.Current.Session[ClavePerfil] = usuario.PerfilUsuario;
            HttpContext.Current.Session[ClaveUltimaVerificacionCuenta] = DateTime.Now;
            GuardarFechaAlta(ClaveAltaCuenta, usuario.FechaAlta);
        }





        public static bool CorrespondeVerificarCuenta()
        {
            object valor = HttpContext.Current.Session[ClaveUltimaVerificacionCuenta];
            return !(valor is DateTime) ||
                (DateTime.Now - (DateTime)valor).TotalMinutes >= MinutosEntreVerificacionesCuenta;
        }

        public static void MarcarCuentaVerificada()
        {
            HttpContext.Current.Session[ClaveUltimaVerificacionCuenta] = DateTime.Now;
        }

        public static void CerrarSesion()
        {
            HttpContext.Current.Session.Remove(ClaveIdUsuario);
            HttpContext.Current.Session.Remove(ClaveNombreCompleto);
            HttpContext.Current.Session.Remove(ClavePerfil);
            HttpContext.Current.Session.Remove(ClaveUltimaVerificacionCuenta);
            HttpContext.Current.Session.Remove(ClaveAltaCuenta);
            HttpContext.Current.Session.Remove(ClaveMarcaBaseDatos);
            HttpContext.Current.Session.Abandon();
        }

        public static bool EstaAutenticado()
        {
            return !SesionSuspendida && HttpContext.Current.Session[ClaveIdUsuario] != null;
        }

        public static int? ObtenerIdUsuarioActual()
        {
            object valor = SesionSuspendida ? null : HttpContext.Current.Session[ClaveIdUsuario];
            return valor == null ? (int?)null : (int)valor;
        }

        public static string ObtenerNombreCompletoActual()
        {
            object valor = SesionSuspendida ? null : HttpContext.Current.Session[ClaveNombreCompleto];
            return valor == null ? null : valor.ToString();
        }

        public static string ObtenerPerfilActual()
        {
            object valor = SesionSuspendida ? null : HttpContext.Current.Session[ClavePerfil];
            return valor == null ? null : valor.ToString();
        }

        public static void EstablecerIdiomaActual(int idIdioma, string codigoIdioma)
        {
            HttpContext.Current.Session[ClaveIdIdioma] = idIdioma;
            HttpContext.Current.Session[ClaveCodigoIdioma] = codigoIdioma;
        }

        public static int? ObtenerIdIdiomaActual()
        {
            object valor = HttpContext.Current.Session[ClaveIdIdioma];
            return valor == null ? (int?)null : Convert.ToInt32(valor);
        }

        public static string ObtenerCodigoIdiomaActual()
        {
            object valor = HttpContext.Current.Session[ClaveCodigoIdioma];
            return valor == null ? null : valor.ToString();
        }

        public static void ActualizarPerfilEnSesion(string perfilUsuario)
        {
            HttpContext.Current.Session[ClavePerfil] = perfilUsuario;
        }

        public static void ActualizarNombreCompletoEnSesion(string nombre, string apellido)
        {
            HttpContext.Current.Session[ClaveNombreCompleto] = (nombre + " " + apellido).Trim();
        }

        public static void IniciarSesionInterna(UsuarioInterno usuario, List<string> codigosPermisos)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException("usuario");
            }

            HttpContext.Current.Session[ClaveIdUsuarioInterno] = usuario.IdUsuarioInterno;
            HttpContext.Current.Session[ClaveNombreCompletoInterno] = usuario.Nombre + " " + usuario.Apellido;
            HttpContext.Current.Session[ClaveIdRolInterno] = usuario.IdRolInterno;
            HttpContext.Current.Session[ClavePermisosInterno] = codigosPermisos == null
                ? string.Empty
                : string.Join(",", codigosPermisos);
            GuardarFechaAlta(ClaveAltaCuentaInterna, usuario.FechaAlta);
        }






        public static bool ActualizarSesionInterna(UsuarioInterno usuario, List<string> codigosPermisos)
        {
            if (usuario == null || !EsLaMismaCuentaInterna(usuario.IdUsuarioInterno, usuario.FechaAlta))
            {
                return false;
            }

            HttpContext.Current.Session[ClaveNombreCompletoInterno] = usuario.Nombre + " " + usuario.Apellido;
            HttpContext.Current.Session[ClaveIdRolInterno] = usuario.IdRolInterno;
            HttpContext.Current.Session[ClavePermisosInterno] = codigosPermisos == null
                ? string.Empty
                : string.Join(",", codigosPermisos);
            return true;
        }

        public static bool EsLaMismaCuentaInterna(int idUsuarioInterno, DateTime fechaAlta)
        {
            object id = HttpContext.Current.Session[ClaveIdUsuarioInterno];
            DateTime? alta = HttpContext.Current.Session[ClaveAltaCuentaInterna] as DateTime?;
            return id is int && (int)id == idUsuarioInterno && alta.HasValue && alta.Value == fechaAlta;
        }

        public static bool EsLaMismaCuentaExterna(int idUsuarioExterno, DateTime fechaAlta)
        {
            object id = HttpContext.Current.Session[ClaveIdUsuario];
            DateTime? alta = HttpContext.Current.Session[ClaveAltaCuenta] as DateTime?;
            return id is int && (int)id == idUsuarioExterno && alta.HasValue && alta.Value == fechaAlta;
        }

        public static void CerrarSesionInterna()
        {
            HttpContext.Current.Session.Remove(ClaveIdUsuarioInterno);
            HttpContext.Current.Session.Remove(ClaveNombreCompletoInterno);
            HttpContext.Current.Session.Remove(ClaveIdRolInterno);
            HttpContext.Current.Session.Remove(ClavePermisosInterno);
            HttpContext.Current.Session.Remove(ClaveAltaCuentaInterna);
            HttpContext.Current.Session.Remove(ClaveMarcaBaseDatos);
            HttpContext.Current.Session.Abandon();
        }

        public static bool EstaAutenticadoComoInterno()
        {
            return !SesionSuspendida && HttpContext.Current.Session[ClaveIdUsuarioInterno] != null;
        }

        public static int? ObtenerIdUsuarioInternoActual()
        {
            object valor = SesionSuspendida ? null : HttpContext.Current.Session[ClaveIdUsuarioInterno];
            return valor == null ? (int?)null : (int)valor;
        }

        public static string ObtenerNombreCompletoInternoActual()
        {
            object valor = SesionSuspendida ? null : HttpContext.Current.Session[ClaveNombreCompletoInterno];
            return valor == null ? null : valor.ToString();
        }

        public static int? ObtenerIdRolInternoActual()
        {
            object valor = SesionSuspendida ? null : HttpContext.Current.Session[ClaveIdRolInterno];
            return valor == null ? (int?)null : (int)valor;
        }

        public static bool TienePermisoInterno(string codigoPermiso)
        {
            object valor = SesionSuspendida ? null : HttpContext.Current.Session[ClavePermisosInterno];
            if (valor == null)
            {
                return false;
            }

            string[] codigos = valor.ToString().Split(',');
            foreach (string codigo in codigos)
            {
                if (codigo == codigoPermiso)
                {
                    return true;
                }
            }
            return false;
        }

        public static DateTime? ObtenerFechaAltaCuentaActual()
        {
            return HttpContext.Current.Session[ClaveAltaCuenta] as DateTime?;
        }

        public static DateTime? ObtenerFechaAltaCuentaInternaActual()
        {
            return HttpContext.Current.Session[ClaveAltaCuentaInterna] as DateTime?;
        }

        public static string ObtenerMarcaBaseDatos()
        {
            return HttpContext.Current.Session[ClaveMarcaBaseDatos] as string;
        }

        public static void EstablecerMarcaBaseDatos(string marca)
        {
            HttpContext.Current.Session[ClaveMarcaBaseDatos] = marca;
        }

        public static void MarcarAvisoRestauracion()
        {
            HttpContext.Current.Session[ClaveAvisoRestauracion] = true;
        }


        public static bool TomarAvisoRestauracion()
        {
            bool hayAviso = HttpContext.Current.Session[ClaveAvisoRestauracion] is bool;
            HttpContext.Current.Session.Remove(ClaveAvisoRestauracion);
            return hayAviso;
        }

        private static void GuardarFechaAlta(string clave, DateTime fechaAlta)
        {
            if (fechaAlta != default(DateTime))
            {
                HttpContext.Current.Session[clave] = fechaAlta;
            }
        }
    }
}
