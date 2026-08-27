using System;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.MPP;
using StageUp.Seguridad;
using StageUp.Servicios;

namespace StageUp.BLL
{
    /// <summary>
    /// Reglas de negocio de CU-001-001 (Registrar y activar usuario
    /// externo) y CU-001-002 (Recuperar acceso a la cuenta), más el login
    /// básico necesario para poder usar la cuenta ya activada. Cada
    /// método corresponde 1 a 1 a un camino (principal o alternativo) de
    /// esos casos de uso; los comentarios remiten al paso del CU.
    /// </summary>
    public class BLL_UsuarioExterno
    {
        private static readonly Regex PatronCorreo = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();
        private readonly MPP_CodigoActivacion _mppCodigoActivacion = new MPP_CodigoActivacion();
        private readonly MPP_CodigoRecuperacion _mppCodigoRecuperacion = new MPP_CodigoRecuperacion();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();

        // ------------------------------------------------------------
        // CU-001-001 Registrar y activar usuario externo
        // ------------------------------------------------------------

        public ResultadoOperacion<int> Registrar(
            string nombre, string apellido, string correoElectronico,
            string password, string confirmacionPassword,
            bool aceptaTerminos, bool aceptaPoliticaPrivacidad)
        {
            // Paso 7 / A1: campos obligatorios completos.
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmacionPassword))
            {
                return ResultadoOperacion<int>.Error(
                    "Completá todos los campos obligatorios para crear la cuenta.", "A1");
            }

            // A2: formato de correo válido.
            if (!PatronCorreo.IsMatch(correoElectronico.Trim()))
            {
                return ResultadoOperacion<int>.Error(
                    "El correo electrónico ingresado no tiene un formato válido.", "A2");
            }

            // A5: aceptación de términos y política de privacidad, ambas obligatorias.
            if (!aceptaTerminos || !aceptaPoliticaPrivacidad)
            {
                return ResultadoOperacion<int>.Error(
                    "Debés aceptar los Términos y condiciones y la Política de privacidad para continuar.", "A5");
            }

            // A4: contraseña y confirmación deben coincidir.
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

            // A3: correo no debe estar registrado previamente.
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

            // A6: si falla el envío del código, la cuenta queda igual registrada
            // y pendiente de activación; se lo informamos a la UI para que
            // ofrezca reintentar el envío.
            string mensaje = envioOk
                ? "Te enviamos un código de activación a tu correo electrónico."
                : "La cuenta se creó, pero no pudimos enviar el código de activación en este momento. Podés solicitar que lo reenviemos.";

            return ResultadoOperacion<int>.Ok(idUsuarioExterno, mensaje);
        }

        /// <summary>
        /// A6 (falla de envío) / A8 (código vencido): generar y reenviar un
        /// nuevo código de activación para una cuenta pendiente.
        /// </summary>
        public ResultadoOperacion ReenviarCodigoActivacion(int idUsuarioExterno)
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
        }

        /// <summary>
        /// Escenario principal paso 13-18 + A7 (código incorrecto) + A8 (vencido).
        /// </summary>
        public ResultadoOperacion ValidarActivacion(int idUsuarioExterno, string codigoIngresado)
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

            CodigoActivacion codigo = _mppCodigoActivacion.ObtenerVigentePorUsuario(idUsuarioExterno);

            // A7: el código ingresado no corresponde a la cuenta pendiente de activación.
            if (codigo == null || codigo.Codigo != codigoIngresado.Trim())
            {
                return ResultadoOperacion.Error("El código ingresado no es válido.", "A7");
            }

            // A8: el código se encuentra vencido.
            if (!codigo.EstaVigente(DateTime.Now))
            {
                return ResultadoOperacion.Error(
                    "El código ingresado ya no se encuentra vigente. Solicitá uno nuevo.", "A8");
            }

            _mppCodigoActivacion.MarcarUtilizado(codigo.IdCodigoActivacion);
            _mppUsuario.ActivarCuenta(usuario.IdUsuarioExterno, EstadoCuentaExterno.Activa.ToString());

            _bitacora.Registrar(
                usuario.IdUsuarioExterno, "ACTIVACION", "UsuarioExterno", usuario.IdUsuarioExterno,
                "Activación de cuenta de usuario externo.");

            _servicioCorreo.EnviarBienvenida(usuario.CorreoElectronico, usuario.Nombre);

            return ResultadoOperacion.Ok(
                "Tu cuenta fue activada correctamente. Ya podés iniciar sesión.");
        }

        // ------------------------------------------------------------
        // Autenticación (necesaria para poder usar la cuenta activada;
        // no tiene un CU propio en la documentación, se resuelve con el
        // criterio de seguridad estándar de no revelar si el correo o la
        // contraseña son los que fallaron).
        // ------------------------------------------------------------

        public ResultadoOperacion<UsuarioExterno> IniciarSesion(string correoElectronico, string password)
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
        }

        // ------------------------------------------------------------
        // CU-001-002 Recuperar acceso a la cuenta
        // ------------------------------------------------------------

        /// <summary>
        /// Escenario principal pasos 9-14 + A1/A2/A3. También se reutiliza
        /// para el reenvío de código (A4/A6): cada llamada genera y envía
        /// un código nuevo.
        /// </summary>
        public ResultadoOperacion SolicitarRecuperacion(string correoElectronico)
        {
            // A1: campo obligatorio.
            if (string.IsNullOrWhiteSpace(correoElectronico))
            {
                return ResultadoOperacion.Error(
                    "Ingresá el correo electrónico asociado a tu cuenta.", "A1");
            }

            // A2: formato válido.
            if (!PatronCorreo.IsMatch(correoElectronico.Trim()))
            {
                return ResultadoOperacion.Error(
                    "El correo electrónico ingresado no tiene un formato válido.", "A2");
            }

            UsuarioExterno usuario = _mppUsuario.ObtenerPorCorreo(correoElectronico.Trim().ToLowerInvariant());

            // A3: correo no registrado. Se responde con un mensaje genérico,
            // sin confirmar ni desmentir la existencia de la cuenta, tal
            // como pide la especificación ("sin exponer información sensible").
            if (usuario == null)
            {
                return ResultadoOperacion.Ok(
                    "Si el correo ingresado corresponde a una cuenta registrada, vas a recibir un código de recuperación.");
            }

            string codigo = GeneradorDeCodigos.GenerarCodigoNumerico();
            DateTime vencimiento = DateTime.Now.AddMinutes(ConfiguracionSeguridad.MinutosVigenciaCodigoRecuperacion);

            _mppCodigoRecuperacion.Insertar(new CodigoRecuperacion
            {
                IdUsuarioExterno = usuario.IdUsuarioExterno,
                Codigo = codigo,
                FechaVencimiento = vencimiento
            });

            bool envioOk = _servicioCorreo.EnviarCodigoRecuperacion(usuario.CorreoElectronico, usuario.Nombre, codigo);

            _bitacora.Registrar(
                usuario.IdUsuarioExterno, "RECUPERACION_SOLICITADA", "UsuarioExterno", usuario.IdUsuarioExterno,
                "Generación de código de recuperación de contraseña.");

            // A4: falla en el envío. Igual devolvemos éxito genérico (no exponemos
            // si el correo existe), pero con un mensaje que permite reintentar.
            return envioOk
                ? ResultadoOperacion.Ok("Si el correo ingresado corresponde a una cuenta registrada, vas a recibir un código de recuperación.")
                : ResultadoOperacion.Error("No pudimos enviar el código en este momento. Probá nuevamente en unos minutos.", "A4");
        }

        /// <summary>
        /// Escenario principal pasos 16-21 + A5 (código incorrecto),
        /// A6 (vencido), A7 (contraseña no cumple criterios), A8 (no coincide).
        /// </summary>
        public ResultadoOperacion ValidarCodigoYActualizarPassword(
            string correoElectronico, string codigoIngresado, string nuevaPassword, string confirmacionNuevaPassword)
        {
            if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(codigoIngresado))
            {
                return ResultadoOperacion.Error("Ingresá el código de recuperación que recibiste por correo.");
            }

            UsuarioExterno usuario = _mppUsuario.ObtenerPorCorreo(correoElectronico.Trim().ToLowerInvariant());
            if (usuario == null)
            {
                // Mismo criterio que en SolicitarRecuperacion: no confirmar/desmentir existencia de cuenta.
                return ResultadoOperacion.Error("El código ingresado no es válido.", "A5");
            }

            CodigoRecuperacion codigo = _mppCodigoRecuperacion.ObtenerVigentePorUsuario(usuario.IdUsuarioExterno);

            // A5: código incorrecto.
            if (codigo == null || codigo.Codigo != codigoIngresado.Trim())
            {
                return ResultadoOperacion.Error("El código ingresado no es válido.", "A5");
            }

            // A6: código vencido.
            if (!codigo.EstaVigente(DateTime.Now))
            {
                return ResultadoOperacion.Error(
                    "El código ingresado ya no se encuentra vigente. Solicitá uno nuevo.", "A6");
            }

            // A7: la nueva contraseña no cumple los criterios de seguridad.
            if (!CumpleCriteriosDeSeguridad(nuevaPassword))
            {
                return ResultadoOperacion.Error(
                    string.Format(
                        "La nueva contraseña debe tener al menos {0} caracteres e incluir letras y números.",
                        ConfiguracionSeguridad.LongitudMinimaPassword),
                    "A7");
            }

            // A8: no coincide con la confirmación.
            if (nuevaPassword != confirmacionNuevaPassword)
            {
                return ResultadoOperacion.Error("La nueva contraseña y su confirmación no coinciden.", "A8");
            }

            _mppCodigoRecuperacion.MarcarUtilizado(codigo.IdCodigoRecuperacion);
            _mppUsuario.ActualizarPassword(usuario.IdUsuarioExterno, HashDeContrasenas.CrearHash(nuevaPassword));

            _bitacora.Registrar(
                usuario.IdUsuarioExterno, "MODIFICACION", "UsuarioExterno", usuario.IdUsuarioExterno,
                "Actualización de contraseña por recuperación de cuenta.");

            _servicioCorreo.EnviarNotificacionCambioPassword(usuario.CorreoElectronico, usuario.Nombre);

            return ResultadoOperacion.Ok("Tu contraseña fue actualizada correctamente. Ya podés iniciar sesión con tus nuevas credenciales.");
        }

        // ------------------------------------------------------------
        // Privados
        // ------------------------------------------------------------

        private bool GenerarYEnviarCodigoActivacion(int idUsuarioExterno, string correoElectronico, string nombre)
        {
            string codigo = GeneradorDeCodigos.GenerarCodigoNumerico();
            DateTime vencimiento = DateTime.Now.AddMinutes(ConfiguracionSeguridad.MinutosVigenciaCodigoActivacion);

            _mppCodigoActivacion.Insertar(new CodigoActivacion
            {
                IdUsuarioExterno = idUsuarioExterno,
                Codigo = codigo,
                FechaVencimiento = vencimiento
            });

            return _servicioCorreo.EnviarCodigoActivacion(correoElectronico, nombre, codigo);
        }

        /// <summary>
        /// Criterio de seguridad para contraseñas (no definido explícitamente
        /// en la documentación de StageUp para este avance): longitud mínima,
        /// al menos una letra y al menos un número.
        /// </summary>
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
    }
}
