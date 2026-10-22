using StageUp.BE.Entidades;
using StageUp.Servicios;

namespace StageUp.BLL
{














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

        internal static string ObtenerMensajeCaptcha(EstadoValidacionRecaptcha estado)
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
