using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Seguridad;
using StageUp.Servicios;

namespace StageUp.BLL
{
    public class BLL_UsuarioInterno
    {
        private readonly MPP_UsuarioInterno _mppUsuarioInterno = new MPP_UsuarioInterno();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();
        private readonly ServicioRecaptcha _servicioRecaptcha = new ServicioRecaptcha();

        public ResultadoOperacion<UsuarioInterno> IniciarSesion(
            string correoElectronico, string password, string respuestaCaptcha)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password))
                {
                    return ResultadoOperacion<UsuarioInterno>.Error("Ingresá tu correo electrónico y tu contraseña.");
                }

                ResultadoValidacionRecaptcha captcha = _servicioRecaptcha.Validar(respuestaCaptcha);
                if (!captcha.EsValido)
                {
                    return ResultadoOperacion<UsuarioInterno>.Error(ObtenerMensajeCaptcha(captcha.Estado));
                }

                UsuarioInterno usuario = _mppUsuarioInterno.ObtenerPorCorreo(correoElectronico.Trim().ToLowerInvariant());

                if (usuario == null || !HashDeContrasenas.Verificar(password, usuario.PasswordHash))
                {
                    return ResultadoOperacion<UsuarioInterno>.Error("El correo electrónico o la contraseña son incorrectos.");
                }

                if (usuario.EstadoCuenta != EstadoCuentaInterno.Activa.ToString())
                {
                    return ResultadoOperacion<UsuarioInterno>.Error("Esta cuenta interna no se encuentra habilitada.");
                }

                List<string> codigosPermisos = _bllPermiso.ListarCodigosPermisosDeRol(usuario.IdRolInterno);
                GestorDeSesion.IniciarSesionInterna(usuario, codigosPermisos);

                _bitacora.RegistrarInterno(
                    usuario.IdUsuarioInterno, "LOGIN", "UsuarioInterno", usuario.IdUsuarioInterno,
                    "Inicio de sesión de usuario interno.");

                return ResultadoOperacion<UsuarioInterno>.Ok(usuario);
            });
        }

        private static string ObtenerMensajeCaptcha(EstadoValidacionRecaptcha estado)
        {
            switch (estado)
            {
                case EstadoValidacionRecaptcha.RespuestaVacia:
                    return "Confirmá que no sos un robot.";
                case EstadoValidacionRecaptcha.RespuestaInvalida:
                    return "La verificación de seguridad no fue válida. Intentá nuevamente.";
                case EstadoValidacionRecaptcha.ConfiguracionIncompleta:
                    return "El CAPTCHA no está configurado. Contactá al administrador.";
                default:
                    return "No pudimos validar el CAPTCHA en este momento. Probá nuevamente.";
            }
        }

        private static ResultadoOperacion<T> EjecutarProtegido<T>(Func<ResultadoOperacion<T>> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<T>.Error(ex.Message);
            }
        }
    }
}
