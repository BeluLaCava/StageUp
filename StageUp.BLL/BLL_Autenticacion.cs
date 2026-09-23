using StageUp.BE.Entidades;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // Ítems 28/29 del checklist de correcciones: "unificar de verdad" el
    // login de usuarios externos e internos en una sola pantalla
    // (IniciarSesion.aspx), sin fusionar los modelos de datos ni los
    // sistemas de permisos de UsuarioExterno/UsuarioInterno, que siguen
    // siendo completamente independientes.
    //
    // El token de reCAPTCHA es de un solo uso: ServicioRecaptcha.Validar
    // hace una llamada real a la API de Google en cada invocación, y
    // validar el mismo token dos veces haría fallar la segunda validación.
    // Por eso esta clase valida el CAPTCHA UNA sola vez acá, y después
    // intenta autenticar primero como usuario externo y, si no corresponde,
    // como usuario interno, usando las variantes internas sin CAPTCHA
    // (BLL_UsuarioExterno.IniciarSesionSinCaptcha /
    // BLL_UsuarioInterno.IniciarSesionSinCaptcha).
    public class BLL_Autenticacion
    {
        private readonly BLL_UsuarioExterno _bllUsuarioExterno = new BLL_UsuarioExterno();
        private readonly BLL_UsuarioInterno _bllUsuarioInterno = new BLL_UsuarioInterno();
        private readonly ServicioRecaptcha _servicioRecaptcha = new ServicioRecaptcha();

        public ResultadoInicioSesion IniciarSesion(string correoElectronico, string password, string respuestaCaptcha)
        {
            if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password))
            {
                return ResultadoInicioSesion.Error("Ingresá tu correo electrónico y tu contraseña.");
            }

            ResultadoValidacionRecaptcha captcha = _servicioRecaptcha.Validar(respuestaCaptcha);
            if (!captcha.EsValido)
            {
                return ResultadoInicioSesion.Error(ObtenerMensajeCaptcha(captcha.Estado));
            }

            ResultadoOperacion<UsuarioExterno> resultadoExterno =
                _bllUsuarioExterno.IniciarSesionSinCaptcha(correoElectronico, password);
            if (resultadoExterno.Exitoso)
            {
                return ResultadoInicioSesion.OkExterno(resultadoExterno.Mensaje);
            }

            ResultadoOperacion<UsuarioInterno> resultadoInterno =
                _bllUsuarioInterno.IniciarSesionSinCaptcha(correoElectronico, password);
            if (resultadoInterno.Exitoso)
            {
                return ResultadoInicioSesion.OkInterno(resultadoInterno.Mensaje);
            }

            return ResultadoInicioSesion.Error(ElegirMensajeDeError(resultadoExterno.Mensaje, resultadoInterno.Mensaje));
        }

        // Cuando el login falla de los dos lados, preferimos mostrar el
        // mensaje más específico (por ej. "tu cuenta todavía no fue
        // activada" o "esta cuenta interna no se encuentra habilitada")
        // antes que el genérico "correo o contraseña incorrectos" que
        // comparten ambos flujos, porque el específico le sirve más al
        // usuario para entender qué le pasa a su cuenta.
        private static string ElegirMensajeDeError(string mensajeExterno, string mensajeInterno)
        {
            const string mensajeGenerico = "El correo electrónico o la contraseña son incorrectos.";

            if (!string.IsNullOrEmpty(mensajeExterno) && mensajeExterno != mensajeGenerico)
            {
                return mensajeExterno;
            }

            if (!string.IsNullOrEmpty(mensajeInterno) && mensajeInterno != mensajeGenerico)
            {
                return mensajeInterno;
            }

            return mensajeGenerico;
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
    }
}
