using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        private static readonly Regex PatronCorreo = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly MPP_UsuarioInterno _mppUsuarioInterno = new MPP_UsuarioInterno();
        private readonly MPP_AreaInterna _mppAreaInterna = new MPP_AreaInterna();
        private readonly MPP_RolInterno _mppRolInterno = new MPP_RolInterno();
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

                UsuarioInterno usuario = _mppUsuarioInterno.ObtenerPorCorreo(new UsuarioInterno
                {
                    CorreoElectronico = correoElectronico.Trim().ToLowerInvariant()
                });

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

        public List<UsuarioInterno> Listar()
        {
            try
            {
                return _mppUsuarioInterno.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<UsuarioInterno>();
            }
        }

        public UsuarioInterno ObtenerPorId(int idUsuarioInterno)
        {
            try
            {
                return _mppUsuarioInterno.ObtenerPorId(
                    new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion<int> Registrar(
            string nombre, string apellido, string correoElectronico, int idAreaInterna,
            int idRolInterno, string estadoCuenta, string password, string confirmacionPassword,
            int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                ResultadoOperacion validacion = ValidarDatos(
                    nombre, apellido, correoElectronico, idAreaInterna, idRolInterno,
                    estadoCuenta, password, confirmacionPassword, true, null, idUsuarioInternoResponsable);

                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                string correoNormalizado = correoElectronico.Trim().ToLowerInvariant();
                var usuario = new UsuarioInterno
                {
                    IdAreaInterna = idAreaInterna,
                    IdRolInterno = idRolInterno,
                    Nombre = nombre.Trim(),
                    Apellido = apellido.Trim(),
                    CorreoElectronico = correoNormalizado,
                    PasswordHash = HashDeContrasenas.CrearHash(password),
                    EstadoCuenta = estadoCuenta
                };

                int idUsuarioInterno = _mppUsuarioInterno.Insertar(usuario);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", "UsuarioInterno", idUsuarioInterno,
                    "Alta de usuario interno: " + correoNormalizado + ".");

                return ResultadoOperacion<int>.Ok(idUsuarioInterno, "El usuario interno se creó correctamente.");
            });
        }

        public ResultadoOperacion Modificar(
            int idUsuarioInterno, string nombre, string apellido, string correoElectronico,
            int idAreaInterna, int idRolInterno, string estadoCuenta, string password,
            string confirmacionPassword, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                UsuarioInterno actual = _mppUsuarioInterno.ObtenerPorId(
                    new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno });
                if (actual == null)
                {
                    return ResultadoOperacion.Error("No se encontró el usuario interno seleccionado.");
                }

                ResultadoOperacion validacion = ValidarDatos(
                    nombre, apellido, correoElectronico, idAreaInterna, idRolInterno,
                    estadoCuenta, password, confirmacionPassword, false, idUsuarioInterno,
                    idUsuarioInternoResponsable);

                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                if (idUsuarioInterno == idUsuarioInternoResponsable && actual.IdRolInterno != idRolInterno)
                {
                    return ResultadoOperacion.Error("No podés cambiar tu propio rol mientras tu sesión está iniciada.");
                }

                _mppUsuarioInterno.Modificar(new UsuarioInterno
                {
                    IdUsuarioInterno = idUsuarioInterno,
                    IdAreaInterna = idAreaInterna,
                    IdRolInterno = idRolInterno,
                    Nombre = nombre.Trim(),
                    Apellido = apellido.Trim(),
                    CorreoElectronico = correoElectronico.Trim().ToLowerInvariant(),
                    PasswordHash = string.IsNullOrWhiteSpace(password)
                        ? null : HashDeContrasenas.CrearHash(password),
                    EstadoCuenta = estadoCuenta
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", "UsuarioInterno", idUsuarioInterno,
                    "Modificación de usuario interno: " + correoElectronico.Trim().ToLowerInvariant() + ".");

                return ResultadoOperacion.Ok("El usuario interno se actualizó correctamente.");
            });
        }

        public ResultadoOperacion DarDeBaja(int idUsuarioInterno, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                if (idUsuarioInterno == idUsuarioInternoResponsable)
                {
                    return ResultadoOperacion.Error("No podés dar de baja tu propia cuenta mientras la estás usando.");
                }

                UsuarioInterno usuario = _mppUsuarioInterno.ObtenerPorId(
                    new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno });
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("No se encontró el usuario interno seleccionado.");
                }

                if (!usuario.Activo)
                {
                    return ResultadoOperacion.Error("El usuario interno ya se encuentra inactivo.");
                }

                _mppUsuarioInterno.DarDeBaja(usuario);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", "UsuarioInterno", idUsuarioInterno,
                    "Baja lógica de usuario interno: " + usuario.CorreoElectronico + ".");

                return ResultadoOperacion.Ok("El usuario interno se dio de baja correctamente.");
            });
        }

        private ResultadoOperacion ValidarDatos(
            string nombre, string apellido, string correoElectronico, int idAreaInterna,
            int idRolInterno, string estadoCuenta, string password, string confirmacionPassword,
            bool passwordObligatoria, int? idUsuarioInternoExcluido, int idUsuarioInternoResponsable)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(correoElectronico))
            {
                return ResultadoOperacion.Error("Completá todos los campos obligatorios del usuario interno.");
            }

            if (nombre.Trim().Length > 200 || apellido.Trim().Length > 200)
            {
                return ResultadoOperacion.Error("El nombre y el apellido no pueden superar los 200 caracteres.");
            }

            string correoNormalizado = correoElectronico.Trim().ToLowerInvariant();
            if (correoNormalizado.Length > 300 || !PatronCorreo.IsMatch(correoNormalizado))
            {
                return ResultadoOperacion.Error("El correo electrónico ingresado no tiene un formato válido.");
            }

            AreaInterna area = _mppAreaInterna.ObtenerPorId(
                new AreaInterna { IdAreaInterna = idAreaInterna });
            if (area == null || !area.Activo || area.EstadoArea != "Activa")
            {
                return ResultadoOperacion.Error("Seleccioná un área interna activa.");
            }

            RolInterno rol = _mppRolInterno.ObtenerPorId(
                new RolInterno { IdRolInterno = idRolInterno });
            if (rol == null || !rol.Activo || rol.EstadoRol != "Activo")
            {
                return ResultadoOperacion.Error("Seleccioná un rol interno activo.");
            }

            if (estadoCuenta != EstadoCuentaInterno.Activa.ToString() &&
                estadoCuenta != EstadoCuentaInterno.Inactiva.ToString())
            {
                return ResultadoOperacion.Error("Seleccioná un estado de cuenta válido.");
            }

            if (idUsuarioInternoExcluido == idUsuarioInternoResponsable &&
                estadoCuenta == EstadoCuentaInterno.Inactiva.ToString())
            {
                return ResultadoOperacion.Error("No podés desactivar tu propia cuenta mientras la estás usando.");
            }

            bool seIngresoPassword = !string.IsNullOrWhiteSpace(password) ||
                                     !string.IsNullOrWhiteSpace(confirmacionPassword);

            if (passwordObligatoria && !seIngresoPassword)
            {
                return ResultadoOperacion.Error("Ingresá una contraseña inicial para el usuario interno.");
            }

            if (seIngresoPassword)
            {
                if (password != confirmacionPassword)
                {
                    return ResultadoOperacion.Error("La contraseña y su confirmación no coinciden.");
                }

                if (!CumpleCriteriosDeSeguridad(password))
                {
                    return ResultadoOperacion.Error(
                        string.Format(
                            "La contraseña debe tener al menos {0} caracteres e incluir letras y números.",
                            ConfiguracionSeguridad.LongitudMinimaPassword));
                }
            }

            UsuarioInterno usuarioBuscado = new UsuarioInterno
            {
                IdUsuarioInterno = idUsuarioInternoExcluido ?? 0,
                CorreoElectronico = correoNormalizado
            };

            if (_mppUsuarioInterno.ExisteCorreo(usuarioBuscado))
            {
                return ResultadoOperacion.Error("Ya existe un usuario interno registrado con ese correo electrónico.");
            }

            return ResultadoOperacion.Ok();
        }

        private static bool CumpleCriteriosDeSeguridad(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < ConfiguracionSeguridad.LongitudMinimaPassword)
            {
                return false;
            }

            bool tieneLetra = false;
            bool tieneNumero = false;

            foreach (char caracter in password)
            {
                tieneLetra = tieneLetra || char.IsLetter(caracter);
                tieneNumero = tieneNumero || char.IsDigit(caracter);
            }

            return tieneLetra && tieneNumero;
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

        private static ResultadoOperacion EjecutarProtegido(Func<ResultadoOperacion> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }
    }
}
