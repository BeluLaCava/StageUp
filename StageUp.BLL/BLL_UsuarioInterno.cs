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
        private readonly BLL_ControlAdministradores _control = new BLL_ControlAdministradores();

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

                return IniciarSesionSinCaptchaInterno(correoElectronico, password);
            });
        }

        // Ítems 28/29 del checklist de correcciones (login unificado): variante
        // sin validación de CAPTCHA, usada por BLL_Autenticacion para poder
        // probar el login como usuario externo y, si no corresponde, como
        // usuario interno, validando el token de reCAPTCHA una sola vez (es de
        // un solo uso) desde un único lugar.
        internal ResultadoOperacion<UsuarioInterno> IniciarSesionSinCaptcha(
            string correoElectronico, string password)
        {
            return EjecutarProtegido(() => IniciarSesionSinCaptchaInterno(correoElectronico, password));
        }

        private ResultadoOperacion<UsuarioInterno> IniciarSesionSinCaptchaInterno(
            string correoElectronico, string password)
        {
            if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password))
            {
                return ResultadoOperacion<UsuarioInterno>.Error("Ingresá tu correo electrónico y tu contraseña.");
            }

            UsuarioInterno usuario = _mppUsuarioInterno.ObtenerPorCorreo(new UsuarioInterno
            {
                CorreoElectronico = correoElectronico.Trim().ToLowerInvariant()
            });

            if (usuario == null || !ProtectorDeCredenciales.VerificarPassword(usuario, password))
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
        }

        public const string FiltroActivos = "Activos";
        public const string FiltroInactivos = "Inactivos";
        public const string FiltroTodos = "Todos";

        private const string TipoEntidadBitacora = "UsuarioInterno";

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

        // Pasos 4 y 5 del escenario principal: listado con búsqueda por
        // nombre, apellido o correo y filtros de rol y estado.
        public List<UsuarioInterno> Buscar(string texto, int? idRolInterno, string estado)
        {
            string busqueda = (texto ?? string.Empty).Trim().ToLowerInvariant();
            List<UsuarioInterno> resultado = new List<UsuarioInterno>();
            foreach (UsuarioInterno usuario in Listar())
            {
                if (estado == FiltroActivos && !usuario.Activo || estado == FiltroInactivos && usuario.Activo)
                {
                    continue;
                }

                if (idRolInterno.HasValue && usuario.IdRolInterno != idRolInterno.Value)
                {
                    continue;
                }

                if (busqueda.Length > 0 &&
                    ((usuario.Nombre + " " + usuario.Apellido).ToLowerInvariant().IndexOf(busqueda, StringComparison.Ordinal) < 0) &&
                    ((usuario.Apellido + " " + usuario.Nombre).ToLowerInvariant().IndexOf(busqueda, StringComparison.Ordinal) < 0) &&
                    (usuario.CorreoElectronico ?? string.Empty).ToLowerInvariant().IndexOf(busqueda, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                resultado.Add(usuario);
            }

            return resultado;
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

        // A10 paso 4 y A16: en cada página del panel interno se vuelve a leer
        // la cuenta, para que una baja o un cambio de rol o de permisos rija
        // enseguida aunque la persona ya tenga la sesión abierta. Devuelve
        // null si la cuenta ya no está activa.
        public UsuarioInterno ObtenerParaSesion(int idUsuarioInterno, out List<string> codigosPermisos)
        {
            codigosPermisos = null;
            UsuarioInterno usuario = _mppUsuarioInterno.ObtenerPorId(
                new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno });
            if (usuario == null || !usuario.Activo || usuario.EstadoCuenta != EstadoCuentaInterno.Activa.ToString())
            {
                return null;
            }

            codigosPermisos = _bllPermiso.ListarCodigosPermisosDeRol(usuario.IdRolInterno);
            return usuario;
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
                    return ResultadoOperacion<int>.Error(validacion.Mensaje, validacion.CodigoAlternativo);
                }

                string correoNormalizado = correoElectronico.Trim().ToLowerInvariant();
                UsuarioInterno usuario = new UsuarioInterno
                {
                    IdAreaInterna = idAreaInterna,
                    IdRolInterno = idRolInterno,
                    Nombre = nombre.Trim(),
                    Apellido = apellido.Trim(),
                    CorreoElectronico = correoNormalizado,
                    EstadoCuenta = estadoCuenta
                };

                // La contraseña pasa por Seguridad como objeto completo, tanto acá
                // (alta) como en el cambio de contraseña de Modificar más abajo.
                ProtectorDeCredenciales.ProtegerPassword(usuario, password);

                int idUsuarioInterno = _mppUsuarioInterno.Insertar(usuario);

                RolInterno rol = _mppRolInterno.ObtenerPorId(new RolInterno { IdRolInterno = idRolInterno });
                AreaInterna area = _mppAreaInterna.ObtenerPorId(new AreaInterna { IdAreaInterna = idAreaInterna });
                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", TipoEntidadBitacora, idUsuarioInterno,
                    "Alta del usuario interno " + usuario.Nombre + " " + usuario.Apellido + " (" + correoNormalizado + ")" +
                    ", área " + (area != null ? area.NombreArea : "#" + idAreaInterna) +
                    ", rol " + (rol != null ? rol.NombreRol : "#" + idRolInterno) +
                    ", estado " + estadoCuenta + ".");

                return ResultadoOperacion<int>.Ok(idUsuarioInterno,
                    "El usuario interno " + usuario.Nombre + " " + usuario.Apellido +
                    " fue registrado correctamente con los permisos del rol " + (rol != null ? rol.NombreRol : string.Empty) + ".");
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
                    return ResultadoOperacion.Error("No podés cambiar tu propio rol mientras tu sesión está iniciada.", "A9");
                }

                bool quedaInactivo = estadoCuenta == EstadoCuentaInterno.Inactiva.ToString();
                if ((quedaInactivo || actual.IdRolInterno != idRolInterno) &&
                    _control.CambioDejaSinAdministradores(new BLL_ControlAdministradores.Cambio
                    {
                        IdUsuario = idUsuarioInterno,
                        UsuarioQuedaInactivo = quedaInactivo,
                        NuevoRolUsuario = idRolInterno
                    }))
                {
                    return ResultadoOperacion.Error(
                        "No se puede guardar el cambio: " + BLL_ControlAdministradores.MensajeSinAdministradores, "A9");
                }

                UsuarioInterno usuarioModificado = new UsuarioInterno
                {
                    IdUsuarioInterno = idUsuarioInterno,
                    IdAreaInterna = idAreaInterna,
                    IdRolInterno = idRolInterno,
                    Nombre = nombre.Trim(),
                    Apellido = apellido.Trim(),
                    CorreoElectronico = correoElectronico.Trim().ToLowerInvariant(),
                    EstadoCuenta = estadoCuenta
                };

                bool cambiaPassword = !string.IsNullOrWhiteSpace(password);
                if (cambiaPassword)
                {
                    ProtectorDeCredenciales.ProtegerPassword(usuarioModificado, password);
                }

                _mppUsuarioInterno.Modificar(usuarioModificado);

                string cambios = DescribirCambios(actual, usuarioModificado, cambiaPassword);
                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idUsuarioInterno,
                    "Modificación del usuario interno " + usuarioModificado.CorreoElectronico +
                    (cambios.Length > 0 ? ": " + cambios : " (sin cambios en sus datos)") + ".");

                string mensaje = "El usuario interno fue actualizado correctamente.";
                if (actual.IdRolInterno != idRolInterno)
                {
                    mensaje += " Sus permisos ahora son los del rol nuevo.";
                }

                return ResultadoOperacion.Ok(mensaje);
            });
        }

        // A10: baja lógica (deja de tener acceso y se conserva su historial).
        public ResultadoOperacion DarDeBaja(int idUsuarioInterno, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                if (idUsuarioInterno == idUsuarioInternoResponsable)
                {
                    return ResultadoOperacion.Error("No podés dar de baja tu propia cuenta mientras la estás usando.", "A10");
                }

                UsuarioInterno usuario = _mppUsuarioInterno.ObtenerPorId(
                    new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno });
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("No se encontró el usuario interno seleccionado.", "A10");
                }

                if (!usuario.Activo)
                {
                    return ResultadoOperacion.Error("El usuario interno ya se encuentra inactivo.", "A10");
                }

                if (_control.CambioDejaSinAdministradores(new BLL_ControlAdministradores.Cambio
                    {
                        IdUsuario = idUsuarioInterno,
                        UsuarioQuedaInactivo = true
                    }))
                {
                    return ResultadoOperacion.Error(
                        "No se puede dar de baja a " + usuario.Nombre + " " + usuario.Apellido + ": " +
                        BLL_ControlAdministradores.MensajeSinAdministradores, "A10");
                }

                _mppUsuarioInterno.DarDeBaja(usuario);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", TipoEntidadBitacora, idUsuarioInterno,
                    "Baja lógica del usuario interno " + usuario.Nombre + " " + usuario.Apellido +
                    " (" + usuario.CorreoElectronico + "), rol " + usuario.NombreRol + ". Se conserva su historial.");

                return ResultadoOperacion.Ok(
                    "El usuario interno " + usuario.Nombre + " " + usuario.Apellido +
                    " fue dado de baja correctamente. Ya no puede ingresar al entorno administrativo y se conserva su historial.");
            });
        }

        private string DescribirCambios(UsuarioInterno anterior, UsuarioInterno nuevo, bool cambiaPassword)
        {
            List<string> cambios = new List<string>();
            if (anterior.Nombre != nuevo.Nombre || anterior.Apellido != nuevo.Apellido)
            {
                cambios.Add("nombre " + anterior.Nombre + " " + anterior.Apellido + " → " + nuevo.Nombre + " " + nuevo.Apellido);
            }

            if (!string.Equals(anterior.CorreoElectronico, nuevo.CorreoElectronico, StringComparison.OrdinalIgnoreCase))
            {
                cambios.Add("correo " + anterior.CorreoElectronico + " → " + nuevo.CorreoElectronico);
            }

            if (anterior.IdAreaInterna != nuevo.IdAreaInterna)
            {
                AreaInterna area = _mppAreaInterna.ObtenerPorId(new AreaInterna { IdAreaInterna = nuevo.IdAreaInterna });
                cambios.Add("área " + anterior.NombreArea + " → " + (area != null ? area.NombreArea : "#" + nuevo.IdAreaInterna));
            }

            if (anterior.IdRolInterno != nuevo.IdRolInterno)
            {
                RolInterno rol = _mppRolInterno.ObtenerPorId(new RolInterno { IdRolInterno = nuevo.IdRolInterno });
                cambios.Add("rol " + anterior.NombreRol + " → " + (rol != null ? rol.NombreRol : "#" + nuevo.IdRolInterno));
            }

            if (anterior.EstadoCuenta != nuevo.EstadoCuenta)
            {
                cambios.Add("estado " + anterior.EstadoCuenta + " → " + nuevo.EstadoCuenta);
            }

            if (cambiaPassword)
            {
                cambios.Add("se cambió la contraseña");
            }

            return string.Join("; ", cambios);
        }

        // Paso 12 del escenario principal y A8 paso 5: A3 (obligatorios),
        // A4 (formato del correo), A5 (correo repetido) y A6 (rol válido). En
        // la modificación todos se informan como A9.
        private ResultadoOperacion ValidarDatos(
            string nombre, string apellido, string correoElectronico, int idAreaInterna,
            int idRolInterno, string estadoCuenta, string password, string confirmacionPassword,
            bool passwordObligatoria, int? idUsuarioInternoExcluido, int idUsuarioInternoResponsable)
        {
            bool esAlta = !idUsuarioInternoExcluido.HasValue;
            Func<string, string> codigo = c => esAlta ? c : "A9";

            bool seIngresoPassword = !string.IsNullOrWhiteSpace(password) ||
                                     !string.IsNullOrWhiteSpace(confirmacionPassword);

            // A3: se informan juntos todos los datos que faltan.
            List<string> faltantes = new List<string>();
            if (string.IsNullOrWhiteSpace(nombre)) faltantes.Add("el nombre");
            if (string.IsNullOrWhiteSpace(apellido)) faltantes.Add("el apellido");
            if (string.IsNullOrWhiteSpace(correoElectronico)) faltantes.Add("el correo electrónico");
            if (idAreaInterna <= 0) faltantes.Add("el área interna");
            if (idRolInterno <= 0) faltantes.Add("el rol");
            if (string.IsNullOrWhiteSpace(estadoCuenta)) faltantes.Add("el estado de la cuenta");
            if (passwordObligatoria && !seIngresoPassword) faltantes.Add("la contraseña inicial");

            if (faltantes.Count > 0)
            {
                string lista = faltantes.Count == 1
                    ? faltantes[0]
                    : string.Join(", ", faltantes.GetRange(0, faltantes.Count - 1)) + " y " + faltantes[faltantes.Count - 1];
                return ResultadoOperacion.Error("Completá " + lista + " del usuario interno.", codigo("A3"));
            }

            if (nombre.Trim().Length > 200 || apellido.Trim().Length > 200)
            {
                return ResultadoOperacion.Error("El nombre y el apellido no pueden superar los 200 caracteres.", codigo("A3"));
            }

            // A4.
            string correoNormalizado = correoElectronico.Trim().ToLowerInvariant();
            if (correoNormalizado.Length > 300 || !PatronCorreo.IsMatch(correoNormalizado))
            {
                return ResultadoOperacion.Error(
                    "El correo electrónico no tiene un formato válido (por ejemplo, persona@artera.com). Corregilo para continuar.", codigo("A4"));
            }

            // A5.
            UsuarioInterno usuarioBuscado = new UsuarioInterno
            {
                IdUsuarioInterno = idUsuarioInternoExcluido ?? 0,
                CorreoElectronico = correoNormalizado
            };

            if (_mppUsuarioInterno.ExisteCorreo(usuarioBuscado))
            {
                UsuarioInterno existente = _mppUsuarioInterno.Listar().Find(u =>
                    string.Equals(u.CorreoElectronico, correoNormalizado, StringComparison.OrdinalIgnoreCase) &&
                    u.IdUsuarioInterno != (idUsuarioInternoExcluido ?? 0));

                string detalle = existente == null
                    ? string.Empty
                    : existente.Activo
                        ? " (" + existente.Nombre + " " + existente.Apellido + ")"
                        : " (" + existente.Nombre + " " + existente.Apellido + ", dado de baja: si vuelve al equipo, reactivá esa cuenta desde su detalle)";

                return ResultadoOperacion.Error(
                    "No es posible registrar otro usuario interno con el mismo correo electrónico: ese correo ya está asociado a otro usuario" +
                    detalle + ".", codigo("A5"));
            }

            AreaInterna area = _mppAreaInterna.ObtenerPorId(
                new AreaInterna { IdAreaInterna = idAreaInterna });
            if (area == null || !area.Activo || area.EstadoArea != "Activa")
            {
                return ResultadoOperacion.Error("El área interna seleccionada no existe o no está activa. Seleccioná un área válida.", codigo("A3"));
            }

            // A6: el rol existe, está activo y tiene permisos.
            RolInterno rol = _mppRolInterno.ObtenerPorId(
                new RolInterno { IdRolInterno = idRolInterno });
            if (rol == null || !rol.Activo || rol.EstadoRol != "Activo")
            {
                return ResultadoOperacion.Error("El rol seleccionado no existe o no está disponible. Seleccioná un rol válido.", codigo("A6"));
            }

            if (_bllPermiso.ListarCodigosPermisosDeRol(idRolInterno).Count == 0)
            {
                return ResultadoOperacion.Error(
                    "El rol \"" + rol.NombreRol + "\" no tiene permisos configurados, así que el usuario no podría operar. Seleccioná otro rol o configurá sus permisos en Roles y permisos.",
                    codigo("A6"));
            }

            if (estadoCuenta != EstadoCuentaInterno.Activa.ToString() &&
                estadoCuenta != EstadoCuentaInterno.Inactiva.ToString())
            {
                return ResultadoOperacion.Error("Seleccioná un estado de cuenta válido.", codigo("A3"));
            }

            if (idUsuarioInternoExcluido == idUsuarioInternoResponsable &&
                estadoCuenta == EstadoCuentaInterno.Inactiva.ToString())
            {
                return ResultadoOperacion.Error("No podés desactivar tu propia cuenta mientras la estás usando.", codigo("A3"));
            }

            if (seIngresoPassword)
            {
                if (password != confirmacionPassword)
                {
                    return ResultadoOperacion.Error("La contraseña y su confirmación no coinciden.", codigo("A3"));
                }

                if (!CumpleCriteriosDeSeguridad(password))
                {
                    return ResultadoOperacion.Error(
                        string.Format(
                            "La contraseña debe tener al menos {0} caracteres e incluir letras y números.",
                            ConfiguracionSeguridad.LongitudMinimaPassword),
                        codigo("A3"));
                }
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
