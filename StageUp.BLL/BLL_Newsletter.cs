using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // Newsletter: suscripción sin cuenta, categorías, doble confirmación y
    // baja por token (script 78).
    //   * Suscribirse: correo, nombre opcional, categorías y aceptación; con
    //     CAPTCHA. Queda Pendiente y se envía un enlace de confirmación que
    //     vence a las 48 horas (del token se guarda solo el hash).
    //   * La respuesta en pantalla es siempre la misma, exista o no el
    //     correo, para no revelar quién está suscripto. Si ya estaba activo,
    //     al dueño del correo le llega un aviso con su enlace de preferencias.
    //   * No se reenvía un correo al mismo suscriptor más de una vez cada
    //     5 minutos.
    //   * Preferencias: con el token de cada newsletter se eligen categorías,
    //     se da de baja o se vuelve a suscribir, sin iniciar sesión.
    public class BLL_Newsletter
    {
        public const int HorasValidezConfirmacion = 48;
        private const int LongitudMaximaCorreo = 254;
        private const int LongitudMaximaNombre = 100;
        private static readonly TimeSpan EsperaEntreCorreos = TimeSpan.FromMinutes(5);
        private const string TipoEntidadBitacora = "SuscriptorNewsletter";
        private static readonly Regex FormatoToken = new Regex("^[0-9A-F]{64}$", RegexOptions.Compiled);
        private static readonly Regex FormatoCorreo = new Regex(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);

        public const string MensajeSuscripcion =
            "¡Listo! Si el correo es válido, te enviamos un mensaje para confirmar la suscripción. Revisá tu bandeja de entrada (y la carpeta de spam). El enlace vence en 48 horas.";

        private readonly MPP_SuscriptorNewsletter _mpp = new MPP_SuscriptorNewsletter();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();
        private readonly ServicioRecaptcha _servicioRecaptcha = new ServicioRecaptcha();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // urlBase: dirección de la aplicación ("https://.../"), para los enlaces.
        public ResultadoOperacion Suscribir(string correo, string nombre, IEnumerable<string> categorias, bool aceptaRecibir,
            string respuestaCaptcha, string urlBase)
        {
            string correoNormalizado = (correo ?? string.Empty).Trim().ToLowerInvariant();
            if (!EsCorreoValido(correoNormalizado))
            {
                return ResultadoOperacion.Error("Ingresá un correo electrónico válido.");
            }

            string nombreNormalizado = string.IsNullOrWhiteSpace(nombre) ? null : Regex.Replace(nombre.Trim(), @"\s+", " ");
            if (nombreNormalizado != null && nombreNormalizado.Length > LongitudMaximaNombre)
            {
                return ResultadoOperacion.Error("El nombre no puede superar los " + LongitudMaximaNombre + " caracteres.");
            }

            List<string> elegidas;
            ResultadoOperacion validacionCategorias = ValidarCategorias(categorias, out elegidas);
            if (!validacionCategorias.Exitoso)
            {
                return validacionCategorias;
            }

            if (!aceptaRecibir)
            {
                return ResultadoOperacion.Error("Para suscribirte tenés que aceptar recibir las novedades de StageUp por correo.");
            }

            ResultadoValidacionRecaptcha captcha = _servicioRecaptcha.Validar(respuestaCaptcha);
            if (!captcha.EsValido)
            {
                return ResultadoOperacion.Error(BLL_Autenticacion.ObtenerMensajeCaptcha(captcha.Estado));
            }

            try
            {
                SuscriptorNewsletter existente = _mpp.ObtenerPorCorreo(new SuscriptorNewsletter { CorreoElectronico = correoNormalizado });

                // Pedido repetido hace muy poco: no se vuelve a mandar nada.
                if (existente != null && existente.FechaUltimoEnvioConfirmacion.HasValue &&
                    DateTime.Now - existente.FechaUltimoEnvioConfirmacion.Value < EsperaEntreCorreos)
                {
                    return ResultadoOperacion.Ok(MensajeSuscripcion);
                }

                if (existente != null && existente.Estado == SuscriptorNewsletter.EstadoActivo)
                {
                    _servicioCorreo.EnviarAvisoYaSuscriptoNewsletter(existente.CorreoElectronico, existente.Nombre ?? nombreNormalizado,
                        UrlPreferencias(urlBase, existente.TokenPreferencias));
                    _mpp.MarcarAvisoEnviado(existente);
                    return ResultadoOperacion.Ok(MensajeSuscripcion);
                }

                string tokenConfirmacion = GenerarToken();
                SuscriptorNewsletter suscriptor = existente ?? new SuscriptorNewsletter
                {
                    CorreoElectronico = correoNormalizado,
                    TokenPreferencias = GenerarToken()
                };
                suscriptor.Nombre = nombreNormalizado;
                suscriptor.VencimientoConfirmacion = DateTime.Now.AddHours(HorasValidezConfirmacion);
                suscriptor.Categorias = elegidas;

                if (existente == null)
                {
                    suscriptor.IdSuscriptorNewsletter = _mpp.Insertar(suscriptor, Hash(tokenConfirmacion));
                    Bitacora("ALTA", suscriptor.IdSuscriptorNewsletter, "Pedido de suscripción al newsletter (pendiente de confirmación).");
                }
                else
                {
                    _mpp.RenovarConfirmacion(suscriptor, Hash(tokenConfirmacion));
                    Bitacora("MODIFICACION", suscriptor.IdSuscriptorNewsletter, "Nuevo pedido de suscripción al newsletter (pendiente de confirmación).");
                }

                _mpp.GuardarCategorias(suscriptor);
                _servicioCorreo.EnviarConfirmacionNewsletter(suscriptor.CorreoElectronico, suscriptor.Nombre,
                    UrlBaseConBarra(urlBase) + "Newsletter.aspx?confirmar=" + tokenConfirmacion, HorasValidezConfirmacion);

                return ResultadoOperacion.Ok(MensajeSuscripcion);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos registrar la suscripción en este momento. Intentá nuevamente.");
            }
        }

        // Doble confirmación. Devuelve el suscriptor confirmado (para mostrar
        // el enlace a sus preferencias).
        public ResultadoOperacion<SuscriptorNewsletter> Confirmar(string tokenConfirmacion)
        {
            const string invalido = "El enlace de confirmación no es válido o ya venció. Volvé a suscribirte para recibir uno nuevo.";
            if (!EsToken(tokenConfirmacion))
            {
                return ResultadoOperacion<SuscriptorNewsletter>.Error(invalido);
            }

            try
            {
                string hash = Hash(tokenConfirmacion.ToUpperInvariant());
                SuscriptorNewsletter suscriptor = _mpp.ObtenerPorHashConfirmacion(hash);
                if (suscriptor == null || suscriptor.Estado != SuscriptorNewsletter.EstadoPendiente)
                {
                    return ResultadoOperacion<SuscriptorNewsletter>.Error(invalido);
                }

                if (!_mpp.Confirmar(suscriptor, hash))
                {
                    return ResultadoOperacion<SuscriptorNewsletter>.Error(invalido);
                }

                suscriptor.Estado = SuscriptorNewsletter.EstadoActivo;
                Bitacora("MODIFICACION", suscriptor.IdSuscriptorNewsletter, "Suscripción al newsletter confirmada.");
                return ResultadoOperacion<SuscriptorNewsletter>.Ok(suscriptor, "¡Suscripción confirmada! Vas a recibir las novedades de StageUp en tu correo.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<SuscriptorNewsletter>.Error("No pudimos confirmar la suscripción en este momento. Intentá nuevamente.");
            }
        }

        public ResultadoOperacion<SuscriptorNewsletter> ObtenerPorToken(string tokenPreferencias)
        {
            const string invalido = "El enlace no es válido. Usá el enlace que aparece al pie de cualquier newsletter de StageUp.";
            if (!EsToken(tokenPreferencias))
            {
                return ResultadoOperacion<SuscriptorNewsletter>.Error(invalido);
            }

            try
            {
                SuscriptorNewsletter suscriptor = _mpp.ObtenerPorToken(new SuscriptorNewsletter { TokenPreferencias = tokenPreferencias.ToUpperInvariant() });
                return suscriptor == null
                    ? ResultadoOperacion<SuscriptorNewsletter>.Error(invalido)
                    : ResultadoOperacion<SuscriptorNewsletter>.Ok(suscriptor);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<SuscriptorNewsletter>.Error("No pudimos cargar tus preferencias en este momento. Intentá nuevamente.");
            }
        }

        public ResultadoOperacion GuardarPreferencias(string tokenPreferencias, IEnumerable<string> categorias)
        {
            ResultadoOperacion<SuscriptorNewsletter> resultado = ObtenerPorToken(tokenPreferencias);
            if (!resultado.Exitoso)
            {
                return ResultadoOperacion.Error(resultado.Mensaje);
            }

            SuscriptorNewsletter suscriptor = resultado.Valor;
            if (suscriptor.Estado != SuscriptorNewsletter.EstadoActivo)
            {
                return ResultadoOperacion.Error("La suscripción no está activa. Volvé a suscribirte para elegir categorías.");
            }

            List<string> elegidas;
            ResultadoOperacion validacion = ValidarCategorias(categorias, out elegidas);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            try
            {
                suscriptor.Categorias = elegidas;
                _mpp.GuardarCategorias(suscriptor);
                Bitacora("MODIFICACION", suscriptor.IdSuscriptorNewsletter, "Cambio de categorías del newsletter.");
                return ResultadoOperacion.Ok("Guardamos tus preferencias.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos guardar tus preferencias en este momento. Intentá nuevamente.");
            }
        }

        public ResultadoOperacion DarDeBaja(string tokenPreferencias)
        {
            return CambiarEstado(tokenPreferencias, SuscriptorNewsletter.EstadoActivo, SuscriptorNewsletter.EstadoBaja,
                "Listo, te diste de baja: no vas a recibir más el newsletter de StageUp.",
                "Baja del newsletter desde el enlace de preferencias.");
        }

        // Volver a suscribirse desde el enlace de preferencias (el token
        // prueba que es el dueño del correo, no hace falta otra confirmación).
        public ResultadoOperacion Reactivar(string tokenPreferencias)
        {
            return CambiarEstado(tokenPreferencias, SuscriptorNewsletter.EstadoBaja, SuscriptorNewsletter.EstadoActivo,
                "¡Bienvenido de nuevo! Vas a volver a recibir el newsletter de StageUp.",
                "Nueva suscripción al newsletter desde el enlace de preferencias.");
        }

        public ResumenSuscriptoresNewsletter ObtenerResumen()
        {
            try
            {
                return _mpp.ObtenerResumen();
            }
            catch (ErrorAccesoDatosException)
            {
                return new ResumenSuscriptoresNewsletter();
            }
        }

        // Destinatarios de un envío (lo usa BLL_Novedad).
        public List<DestinatarioNewsletter> ListarDestinatarios(string criterio, Novedad novedad)
        {
            return _mpp.ListarDestinatarios(criterio, novedad);
        }

        public static string UrlPreferencias(string urlBase, string tokenPreferencias)
        {
            return string.IsNullOrWhiteSpace(urlBase) || string.IsNullOrWhiteSpace(tokenPreferencias)
                ? null
                : UrlBaseConBarra(urlBase) + "Newsletter.aspx?token=" + tokenPreferencias;
        }

        private ResultadoOperacion CambiarEstado(string token, string estadoAnterior, string estadoNuevo, string mensajeOk, string descripcion)
        {
            ResultadoOperacion<SuscriptorNewsletter> resultado = ObtenerPorToken(token);
            if (!resultado.Exitoso)
            {
                return ResultadoOperacion.Error(resultado.Mensaje);
            }

            SuscriptorNewsletter suscriptor = resultado.Valor;
            if (suscriptor.Estado != estadoAnterior)
            {
                return ResultadoOperacion.Error(estadoNuevo == SuscriptorNewsletter.EstadoBaja
                    ? "La suscripción ya no estaba activa."
                    : "La suscripción ya está activa.");
            }

            try
            {
                if (!_mpp.CambiarEstado(suscriptor, estadoNuevo))
                {
                    return ResultadoOperacion.Error("No pudimos actualizar la suscripción. Recargá la página e intentá nuevamente.");
                }

                Bitacora(estadoNuevo == SuscriptorNewsletter.EstadoBaja ? "BAJA" : "MODIFICACION", suscriptor.IdSuscriptorNewsletter, descripcion);
                return ResultadoOperacion.Ok(mensajeOk);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos actualizar la suscripción en este momento. Intentá nuevamente.");
            }
        }

        private static ResultadoOperacion ValidarCategorias(IEnumerable<string> categorias, out List<string> elegidas)
        {
            elegidas = (categorias ?? Enumerable.Empty<string>())
                .Where(c => BLL_Novedad.Categorias.Contains(c))
                .Distinct()
                .ToList();

            return elegidas.Count == 0
                ? ResultadoOperacion.Error("Elegí al menos una categoría de novedades.")
                : ResultadoOperacion.Ok();
        }

        private static bool EsCorreoValido(string correo)
        {
            if (correo.Length == 0 || correo.Length > LongitudMaximaCorreo || !FormatoCorreo.IsMatch(correo))
            {
                return false;
            }

            try
            {
                return new MailAddress(correo).Address == correo;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool EsToken(string token)
        {
            return !string.IsNullOrEmpty(token) && FormatoToken.IsMatch(token.ToUpperInvariant());
        }

        // 32 bytes aleatorios en hexadecimal (64 caracteres).
        private static string GenerarToken()
        {
            byte[] bytes = new byte[32];
            using (RandomNumberGenerator generador = RandomNumberGenerator.Create())
            {
                generador.GetBytes(bytes);
            }

            return string.Concat(bytes.Select(b => b.ToString("X2")));
        }

        private static string Hash(string token)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.ASCII.GetBytes(token)).Select(b => b.ToString("x2")));
            }
        }

        private static string UrlBaseConBarra(string urlBase)
        {
            string url = urlBase ?? string.Empty;
            return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
        }

        private void Bitacora(string operacion, int idSuscriptor, string descripcion)
        {
            try
            {
                _bitacora.Registrar(null, operacion, TipoEntidadBitacora, idSuscriptor, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }
    }
}
