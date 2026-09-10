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
    public class BLL_UsuarioExterno
    {
        private static readonly Regex PatronCorreo = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();
        private readonly BLL_CodigoActivacion _bllCodigoActivacion = new BLL_CodigoActivacion();
        private readonly BLL_CodigoRecuperacion _bllCodigoRecuperacion = new BLL_CodigoRecuperacion();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();

        public bool PerfilCompletoHabilitado
        {
            get { return MPP_UsuarioExterno.PerfilCompletoHabilitado; }
        }

        public ResultadoOperacion<int> Registrar(
            string nombre, string apellido, string correoElectronico,
            string password, string confirmacionPassword,
            bool aceptaTerminos, bool aceptaPoliticaPrivacidad)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
                    string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(confirmacionPassword))
                {
                    return ResultadoOperacion<int>.Error(
                        "Completá todos los campos obligatorios para crear la cuenta.", "A1");
                }

                if (!PatronCorreo.IsMatch(correoElectronico.Trim()))
                {
                    return ResultadoOperacion<int>.Error(
                        "El correo electrónico ingresado no tiene un formato válido.", "A2");
                }

                if (!aceptaTerminos || !aceptaPoliticaPrivacidad)
                {
                    return ResultadoOperacion<int>.Error(
                        "Debés aceptar los Términos y condiciones y la Política de privacidad para continuar.", "A5");
                }

                if (password != confirmacionPassword)
                {
                    return ResultadoOperacion<int>.Error(
                        "La contraseña y su confirmación no coinciden.", "A4");
                }

                if (!CumpleCriteriosDeSeguridad(password))
                {
                    return ResultadoOperacion<int>.Error(
                        string.Format(
                            "La contraseña debe tener al menos {0} caracteres e incluir letras y números.",
                            ConfiguracionSeguridad.LongitudMinimaPassword));
                }

                correoElectronico = correoElectronico.Trim().ToLowerInvariant();
                if (_mppUsuario.ObtenerPorCorreo(correoElectronico) != null)
                {
                    return ResultadoOperacion<int>.Error(
                        "Ya existe una cuenta registrada con ese correo electrónico.", "A3");
                }

                var nuevoUsuario = new UsuarioExterno
                {
                    Nombre = nombre.Trim(),
                    Apellido = apellido.Trim(),
                    CorreoElectronico = correoElectronico,
                    PasswordHash = HashDeContrasenas.CrearHash(password),
                    EstadoCuenta = EstadoCuentaExterno.PendienteActivacion.ToString(),
                    PerfilUsuario = PerfilUsuarioExterno.ExternoSolicitante.ToString(),
                    AceptaTerminos = true,
                    AceptaPoliticaPrivacidad = true,
                    FechaAceptacionTerminos = DateTime.Now
                };

                int idUsuarioExterno = _mppUsuario.Insertar(nuevoUsuario);

                _bitacora.Registrar(
                    idUsuarioExterno, "ALTA", "UsuarioExterno", idUsuarioExterno,
                    "Registro de cuenta de usuario externo (pendiente de activación).");

                bool envioOk = GenerarYEnviarCodigoActivacion(idUsuarioExterno, nuevoUsuario.CorreoElectronico, nuevoUsuario.Nombre);

                string mensaje = envioOk
                    ? "Te enviamos un código de activación a tu correo electrónico."
                    : "La cuenta se creó, pero no pudimos enviar el código de activación en este momento. Podés solicitar que lo reenviemos.";

                return ResultadoOperacion<int>.Ok(idUsuarioExterno, mensaje);
            });
        }

        public ResultadoOperacion ReenviarCodigoActivacion(int idUsuarioExterno)
        {
            return EjecutarProtegido(() =>
            {
                UsuarioExterno usuario = _mppUsuario.ObtenerPorId(idUsuarioExterno);
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("No se encontró la cuenta indicada.");
                }

                if (usuario.EstadoCuenta == EstadoCuentaExterno.Activa.ToString())
                {
                    return ResultadoOperacion.Error("Esta cuenta ya se encuentra activa.");
                }

                bool envioOk = GenerarYEnviarCodigoActivacion(usuario.IdUsuarioExterno, usuario.CorreoElectronico, usuario.Nombre);

                return envioOk
                    ? ResultadoOperacion.Ok("Te enviamos un nuevo código de activación a tu correo electrónico.")
                    : ResultadoOperacion.Error("No pudimos enviar el código en este momento. Probá nuevamente en unos minutos.");
            });
        }

        public ResultadoOperacion ValidarActivacion(int idUsuarioExterno, string codigoIngresado)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(codigoIngresado))
                {
                    return ResultadoOperacion.Error("Ingresá el código de activación que recibiste por correo.");
                }

                UsuarioExterno usuario = _mppUsuario.ObtenerPorId(idUsuarioExterno);
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("No se encontró la cuenta indicada.");
                }

                CodigoActivacion codigo = _bllCodigoActivacion.ObtenerVigentePorUsuario(idUsuarioExterno);

                if (codigo == null || codigo.Codigo != codigoIngresado.Trim())
                {
                    return ResultadoOperacion.Error("El código ingresado no es válido.", "A7");
                }

                if (!codigo.EstaVigente(DateTime.Now))
                {
                    return ResultadoOperacion.Error(
                        "El código ingresado ya no se encuentra vigente. Solicitá uno nuevo.", "A8");
                }

                _bllCodigoActivacion.MarcarUtilizado(codigo.IdCodigoActivacion);
                _mppUsuario.ActivarCuenta(usuario.IdUsuarioExterno, EstadoCuentaExterno.Activa.ToString());

                _bitacora.Registrar(
                    usuario.IdUsuarioExterno, "ACTIVACION", "UsuarioExterno", usuario.IdUsuarioExterno,
                    "Activación de cuenta de usuario externo.");

                _servicioCorreo.EnviarBienvenida(usuario.CorreoElectronico, usuario.Nombre);

                return ResultadoOperacion.Ok(
                    "Tu cuenta fue activada correctamente. Ya podés iniciar sesión.");
            });
        }

        public ResultadoOperacion<UsuarioExterno> IniciarSesion(string correoElectronico, string password)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password))
                {
                    return ResultadoOperacion<UsuarioExterno>.Error("Ingresá tu correo electrónico y tu contraseña.");
                }

                UsuarioExterno usuario = _mppUsuario.ObtenerPorCorreo(correoElectronico.Trim().ToLowerInvariant());

                if (usuario == null || !HashDeContrasenas.Verificar(password, usuario.PasswordHash))
                {
                    return ResultadoOperacion<UsuarioExterno>.Error("El correo electrónico o la contraseña son incorrectos.");
                }

                if (usuario.EstadoCuenta == EstadoCuentaExterno.PendienteActivacion.ToString())
                {
                    return ResultadoOperacion<UsuarioExterno>.Error(
                        "Tu cuenta todavía no fue activada. Revisá tu correo electrónico para activarla.");
                }

                if (usuario.EstadoCuenta != EstadoCuentaExterno.Activa.ToString())
                {
                    return ResultadoOperacion<UsuarioExterno>.Error("Esta cuenta no se encuentra habilitada.");
                }

                GestorDeSesion.IniciarSesion(usuario);

                _bitacora.Registrar(
                    usuario.IdUsuarioExterno, "LOGIN", "UsuarioExterno", usuario.IdUsuarioExterno,
                    "Inicio de sesión de usuario externo.");

                return ResultadoOperacion<UsuarioExterno>.Ok(usuario);
            });
        }

        public ResultadoOperacion SolicitarRecuperacion(string correoElectronico)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(correoElectronico))
                {
                    return ResultadoOperacion.Error(
                        "Ingresá el correo electrónico asociado a tu cuenta.", "A1");
                }

                if (!PatronCorreo.IsMatch(correoElectronico.Trim()))
                {
                    return ResultadoOperacion.Error(
                        "El correo electrónico ingresado no tiene un formato válido.", "A2");
                }

                UsuarioExterno usuario = _mppUsuario.ObtenerPorCorreo(correoElectronico.Trim().ToLowerInvariant());

                if (usuario == null)
                {
                    return ResultadoOperacion.Ok(
                        "Si el correo ingresado corresponde a una cuenta registrada, vas a recibir un código de recuperación.");
                }

                string codigo = GeneradorDeCodigos.GenerarCodigoNumerico();
                DateTime vencimiento = DateTime.Now.AddMinutes(ConfiguracionSeguridad.MinutosVigenciaCodigoRecuperacion);

                _bllCodigoRecuperacion.Insertar(new CodigoRecuperacion
                {
                    IdUsuarioExterno = usuario.IdUsuarioExterno,
                    Codigo = codigo,
                    FechaVencimiento = vencimiento
                });

                bool envioOk = _servicioCorreo.EnviarCodigoRecuperacion(usuario.CorreoElectronico, usuario.Nombre, codigo);

                _bitacora.Registrar(
                    usuario.IdUsuarioExterno, "RECUPERACION_SOLICITADA", "UsuarioExterno", usuario.IdUsuarioExterno,
                    "Generación de código de recuperación de contraseña.");

                return envioOk
                    ? ResultadoOperacion.Ok("Si el correo ingresado corresponde a una cuenta registrada, vas a recibir un código de recuperación.")
                    : ResultadoOperacion.Error("No pudimos enviar el código en este momento. Probá nuevamente en unos minutos.", "A4");
            });
        }

        public ResultadoOperacion ValidarCodigoYActualizarPassword(
            string correoElectronico, string codigoIngresado, string nuevaPassword, string confirmacionNuevaPassword)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(codigoIngresado))
                {
                    return ResultadoOperacion.Error("Ingresá el código de recuperación que recibiste por correo.");
                }

                UsuarioExterno usuario = _mppUsuario.ObtenerPorCorreo(correoElectronico.Trim().ToLowerInvariant());
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("El código ingresado no es válido.", "A5");
                }

                CodigoRecuperacion codigo = _bllCodigoRecuperacion.ObtenerVigentePorUsuario(usuario.IdUsuarioExterno);

                if (codigo == null || codigo.Codigo != codigoIngresado.Trim())
                {
                    return ResultadoOperacion.Error("El código ingresado no es válido.", "A5");
                }

                if (!codigo.EstaVigente(DateTime.Now))
                {
                    return ResultadoOperacion.Error(
                        "El código ingresado ya no se encuentra vigente. Solicitá uno nuevo.", "A6");
                }

                if (!CumpleCriteriosDeSeguridad(nuevaPassword))
                {
                    return ResultadoOperacion.Error(
                        string.Format(
                            "La nueva contraseña debe tener al menos {0} caracteres e incluir letras y números.",
                            ConfiguracionSeguridad.LongitudMinimaPassword),
                        "A7");
                }

                if (nuevaPassword != confirmacionNuevaPassword)
                {
                    return ResultadoOperacion.Error("La nueva contraseña y su confirmación no coinciden.", "A8");
                }

                _bllCodigoRecuperacion.MarcarUtilizado(codigo.IdCodigoRecuperacion);
                _mppUsuario.ActualizarPassword(usuario.IdUsuarioExterno, HashDeContrasenas.CrearHash(nuevaPassword));

                _bitacora.Registrar(
                    usuario.IdUsuarioExterno, "MODIFICACION", "UsuarioExterno", usuario.IdUsuarioExterno,
                    "Actualización de contraseña por recuperación de cuenta.");

                _servicioCorreo.EnviarNotificacionCambioPassword(usuario.CorreoElectronico, usuario.Nombre);

                return ResultadoOperacion.Ok("Tu contraseña fue actualizada correctamente. Ya podés iniciar sesión con tus nuevas credenciales.");
            });
        }

        public ResultadoOperacion CambiarPassword(int idUsuarioExterno, string passwordActual, string nuevaPassword, string confirmacionNuevaPassword)
        {
            return EjecutarProtegido(() =>
            {
                UsuarioExterno usuario = _mppUsuario.ObtenerPorId(idUsuarioExterno);
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("No se encontró la cuenta indicada.");
                }

                if (string.IsNullOrWhiteSpace(passwordActual) || !HashDeContrasenas.Verificar(passwordActual, usuario.PasswordHash))
                {
                    return ResultadoOperacion.Error("La contraseña actual ingresada es incorrecta.");
                }

                if (!CumpleCriteriosDeSeguridad(nuevaPassword))
                {
                    return ResultadoOperacion.Error(
                        string.Format(
                            "La nueva contraseña debe tener al menos {0} caracteres e incluir letras y números.",
                            ConfiguracionSeguridad.LongitudMinimaPassword));
                }

                if (nuevaPassword != confirmacionNuevaPassword)
                {
                    return ResultadoOperacion.Error("La nueva contraseña y su confirmación no coinciden.");
                }

                _mppUsuario.ActualizarPassword(usuario.IdUsuarioExterno, HashDeContrasenas.CrearHash(nuevaPassword));

                _bitacora.Registrar(
                    usuario.IdUsuarioExterno, "MODIFICACION", "UsuarioExterno", usuario.IdUsuarioExterno,
                    "Cambio de contraseña desde el perfil del usuario.");

                _servicioCorreo.EnviarNotificacionCambioPassword(usuario.CorreoElectronico, usuario.Nombre);

                return ResultadoOperacion.Ok("Tu contraseña fue actualizada correctamente.");
            });
        }

        public ResultadoOperacion<int> SolicitarHabilitacionComoGestor(int idUsuarioExterno)
        {
            return EjecutarProtegido(() =>
            {
                UsuarioExterno usuario = _mppUsuario.ObtenerPorId(idUsuarioExterno);
                if (usuario == null)
                {
                    return ResultadoOperacion<int>.Error("No se encontró la cuenta indicada.");
                }

                if (usuario.PerfilUsuario == PerfilUsuarioExterno.GestorEspacios.ToString())
                {
                    return ResultadoOperacion<int>.Error("Tu cuenta ya está habilitada como gestor de espacios.");
                }

                if (usuario.PerfilUsuario == PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString())
                {
                    return ResultadoOperacion<int>.Error("Ya tenés una solicitud de habilitación como gestor pendiente de aprobación.");
                }

                _mppUsuario.ActualizarPerfil(idUsuarioExterno, PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString());

                _bitacora.Registrar(
                    idUsuarioExterno, "MODIFICACION", "UsuarioExterno", idUsuarioExterno,
                    "Solicitud de habilitación como gestor de espacios (queda pendiente de aprobación).");

                return ResultadoOperacion<int>.Ok(idUsuarioExterno,
                    "Tu solicitud para ser gestor de espacios quedó registrada. Un administrador la va a revisar.");
            });
        }

        public List<UsuarioExterno> ListarPendientesHabilitacionGestor()
        {
            try
            {
                return _mppUsuario.ListarPorPerfil(PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString());
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<UsuarioExterno>();
            }
        }

        public ResultadoOperacion AprobarHabilitacionComoGestor(int idUsuarioExterno, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                UsuarioExterno usuario = _mppUsuario.ObtenerPorId(idUsuarioExterno);
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("No se encontró la cuenta indicada.");
                }

                if (usuario.PerfilUsuario != PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString())
                {
                    return ResultadoOperacion.Error("Esta cuenta no tiene una solicitud de habilitación pendiente.");
                }

                _mppUsuario.ActualizarPerfil(idUsuarioExterno, PerfilUsuarioExterno.GestorEspacios.ToString());

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "APROBACION", "UsuarioExterno", idUsuarioExterno,
                    "Aprobación de solicitud de habilitación como gestor de espacios.");

                return ResultadoOperacion.Ok("La cuenta fue habilitada como gestora de espacios.");
            });
        }

        public ResultadoOperacion RechazarHabilitacionComoGestor(int idUsuarioExterno, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                UsuarioExterno usuario = _mppUsuario.ObtenerPorId(idUsuarioExterno);
                if (usuario == null)
                {
                    return ResultadoOperacion.Error("No se encontró la cuenta indicada.");
                }

                if (usuario.PerfilUsuario != PerfilUsuarioExterno.PendienteHabilitacionGestor.ToString())
                {
                    return ResultadoOperacion.Error("Esta cuenta no tiene una solicitud de habilitación pendiente.");
                }

                _mppUsuario.ActualizarPerfil(idUsuarioExterno, PerfilUsuarioExterno.ExternoSolicitante.ToString());

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "RECHAZO", "UsuarioExterno", idUsuarioExterno,
                    "Rechazo de solicitud de habilitación como gestor de espacios.");

                return ResultadoOperacion.Ok("La solicitud fue rechazada.");
            });
        }

        public int? ObtenerIdPorCorreo(string correoElectronico)
        {
            if (string.IsNullOrWhiteSpace(correoElectronico))
            {
                return null;
            }

            try
            {
                UsuarioExterno usuario = _mppUsuario.ObtenerPorCorreo(correoElectronico.Trim());
                return usuario == null ? (int?)null : usuario.IdUsuarioExterno;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public UsuarioExterno ObtenerPorId(int idUsuarioExterno)
        {
            try
            {
                return _mppUsuario.ObtenerPorId(idUsuarioExterno);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public UsuarioExterno ObtenerPerfilPorId(int idUsuarioExterno)
        {
            try
            {
                return _mppUsuario.ObtenerPerfilPorId(idUsuarioExterno);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion ActualizarDatosPersonales(
            int idUsuarioExterno, string nombre, string apellido, string correoElectronico,
            string fotoPerfilRuta, string descripcionPerfil)
        {
            return EjecutarProtegido(() =>
            {
                if (idUsuarioExterno <= 0 || string.IsNullOrWhiteSpace(nombre) ||
                    string.IsNullOrWhiteSpace(apellido) || string.IsNullOrWhiteSpace(correoElectronico))
                {
                    return ResultadoOperacion.Error("Completá nombre, apellido y correo electrónico.");
                }

                nombre = nombre.Trim();
                apellido = apellido.Trim();
                correoElectronico = correoElectronico.Trim().ToLowerInvariant();
                descripcionPerfil = string.IsNullOrWhiteSpace(descripcionPerfil) ? null : descripcionPerfil.Trim();
                fotoPerfilRuta = string.IsNullOrWhiteSpace(fotoPerfilRuta) ? null : fotoPerfilRuta.Trim();

                if (nombre.Length > 100 || apellido.Length > 100)
                {
                    return ResultadoOperacion.Error("El nombre y el apellido no pueden superar los 100 caracteres.");
                }

                if (correoElectronico.Length > 300 || !PatronCorreo.IsMatch(correoElectronico))
                {
                    return ResultadoOperacion.Error("El correo electrónico ingresado no tiene un formato válido.");
                }

                if (descripcionPerfil != null && descripcionPerfil.Length > 1200)
                {
                    return ResultadoOperacion.Error("La descripción no puede superar los 1200 caracteres.");
                }

                if (fotoPerfilRuta != null && fotoPerfilRuta.Length > 500)
                {
                    return ResultadoOperacion.Error("La ruta de la foto de perfil es demasiado extensa.");
                }

                if (!PerfilCompletoHabilitado)
                {
                    return ResultadoOperacion.Error("La edición de datos personales está lista en la interfaz, pero todavía falta habilitar su integración con la base de datos.");
                }

                UsuarioExterno actual = _mppUsuario.ObtenerPorId(idUsuarioExterno);
                if (actual == null)
                {
                    return ResultadoOperacion.Error("No se encontró la cuenta indicada.");
                }

                UsuarioExterno usuarioConMismoCorreo = _mppUsuario.ObtenerPorCorreo(correoElectronico);
                if (usuarioConMismoCorreo != null && usuarioConMismoCorreo.IdUsuarioExterno != idUsuarioExterno)
                {
                    return ResultadoOperacion.Error("Ya existe una cuenta registrada con ese correo electrónico.");
                }

                var perfil = new UsuarioExterno
                {
                    IdUsuarioExterno = idUsuarioExterno,
                    Nombre = nombre,
                    Apellido = apellido,
                    CorreoElectronico = correoElectronico,
                    FotoPerfilRuta = fotoPerfilRuta,
                    DescripcionPerfil = descripcionPerfil
                };

                _mppUsuario.ActualizarDatosPersonales(perfil);

                _bitacora.Registrar(
                    idUsuarioExterno, "MODIFICACION", "UsuarioExterno", idUsuarioExterno,
                    "Actualización de datos personales desde Mi perfil.");

                return ResultadoOperacion.Ok("Tus datos personales fueron actualizados correctamente.");
            });
        }

        private bool GenerarYEnviarCodigoActivacion(int idUsuarioExterno, string correoElectronico, string nombre)
        {
            string codigo = GeneradorDeCodigos.GenerarCodigoNumerico();
            DateTime vencimiento = DateTime.Now.AddMinutes(ConfiguracionSeguridad.MinutosVigenciaCodigoActivacion);

            _bllCodigoActivacion.Insertar(new CodigoActivacion
            {
                IdUsuarioExterno = idUsuarioExterno,
                Codigo = codigo,
                FechaVencimiento = vencimiento
            });

            return _servicioCorreo.EnviarCodigoActivacion(correoElectronico, nombre, codigo);
        }

        private static bool CumpleCriteriosDeSeguridad(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < ConfiguracionSeguridad.LongitudMinimaPassword)
            {
                return false;
            }

            bool tieneLetra = false;
            bool tieneNumero = false;

            foreach (char c in password)
            {
                if (char.IsLetter(c)) tieneLetra = true;
                if (char.IsDigit(c)) tieneNumero = true;
            }

            return tieneLetra && tieneNumero;
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
