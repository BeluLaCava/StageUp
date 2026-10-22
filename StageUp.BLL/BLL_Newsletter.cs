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
























    public class BLL_Newsletter
    {
        public const int HorasValidezConfirmacion = 48;
        private const int LongitudMaximaCorreo = 254;
        private const int LongitudMaximaNombre = 100;
        private static readonly TimeSpan EsperaEntreCorreos = TimeSpan.FromMinutes(5);
        private const string TipoEntidadBitacora = "SuscriptorNewsletter";
        private static readonly Regex FormatoToken = new Regex("^[0-9A-F]{64}$", RegexOptions.Compiled);
        private static readonly Regex FormatoCorreo = new Regex(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);

        public const string MensajeFalloCorreo =
            "No pudimos enviar el correo en este momento. Intentá nuevamente en unos minutos.";

        public const string MensajeSuscripcion =
            "¡Listo! Si el correo es válido, te enviamos un mensaje para confirmar la suscripción. Revisá tu bandeja de entrada (y la carpeta de spam). El enlace vence en 48 horas.";

        private readonly MPP_SuscriptorNewsletter _mpp = new MPP_SuscriptorNewsletter();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();
        private readonly ServicioRecaptcha _servicioRecaptcha = new ServicioRecaptcha();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();


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

            return PedirConfirmacion(correoNormalizado, nombreNormalizado, elegidas, null, urlBase, false);
        }




        private ResultadoOperacion PedirConfirmacion(string correoNormalizado, string nombreNormalizado, List<string> elegidas,
            int? idUsuarioExterno, string urlBase, bool desdeCuenta)
        {
            SuscriptorNewsletter existente;
            try
            {
                existente = _mpp.ObtenerPorCorreo(new SuscriptorNewsletter { CorreoElectronico = correoNormalizado });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos registrar la suscripción en este momento. Intentá nuevamente.");
            }



            if (existente != null && existente.FechaUltimoEnvioConfirmacion.HasValue &&
                DateTime.Now - existente.FechaUltimoEnvioConfirmacion.Value < EsperaEntreCorreos)
            {
                return desdeCuenta
                    ? ResultadoOperacion.Error("Ya te enviamos un correo hace menos de 5 minutos. Revisá tu bandeja de entrada (y spam) antes de pedir otro.")
                    : ResultadoOperacion.Ok(MensajeSuscripcion);
            }

            if (existente != null && existente.Estado == SuscriptorNewsletter.EstadoActivo)
            {
                if (desdeCuenta)
                {
                    return ResultadoOperacion.Error("Tu correo ya recibe el newsletter.");
                }

                bool avisoEnviado = _servicioCorreo.EnviarAvisoYaSuscriptoNewsletter(existente.CorreoElectronico, existente.Nombre ?? nombreNormalizado,
                    UrlPreferencias(urlBase, existente.TokenPreferencias));
                if (!avisoEnviado)
                {
                    Bitacora("ERROR", existente.IdSuscriptorNewsletter, "No se pudo enviar el aviso de suscripción existente al newsletter.");
                    return ResultadoOperacion.Error(MensajeFalloCorreo);
                }

                try
                {
                    _mpp.MarcarAvisoEnviado(existente);
                }
                catch (ErrorAccesoDatosException)
                {
                }
                return ResultadoOperacion.Ok(MensajeSuscripcion);
            }

            string tokenConfirmacion = GenerarToken();
            SuscriptorNewsletter suscriptor = existente ?? new SuscriptorNewsletter
            {
                CorreoElectronico = correoNormalizado,
                TokenPreferencias = GenerarToken(),
                Origen = idUsuarioExterno.HasValue ? SuscriptorNewsletter.OrigenCuenta : SuscriptorNewsletter.OrigenFormulario
            };
            suscriptor.Nombre = nombreNormalizado;
            suscriptor.IdUsuarioExterno = idUsuarioExterno;
            suscriptor.VencimientoConfirmacion = DateTime.Now.AddHours(HorasValidezConfirmacion);
            suscriptor.Categorias = elegidas;

            try
            {
                if (existente == null)
                {
                    suscriptor.IdSuscriptorNewsletter = _mpp.Insertar(suscriptor, Hash(tokenConfirmacion));
                    Bitacora("ALTA", suscriptor.IdSuscriptorNewsletter, "Pedido de suscripción al newsletter (pendiente de confirmación)" +
                        (desdeCuenta ? " desde Mi perfil." : "."));
                }
                else
                {
                    _mpp.RenovarConfirmacion(suscriptor, Hash(tokenConfirmacion));
                    Bitacora("MODIFICACION", suscriptor.IdSuscriptorNewsletter, "Nuevo pedido de suscripción al newsletter (pendiente de confirmación)" +
                        (desdeCuenta ? " desde Mi perfil." : "."));
                }

                _mpp.GuardarCategorias(suscriptor);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos registrar la suscripción en este momento. Intentá nuevamente.");
            }

            bool enviado = _servicioCorreo.EnviarConfirmacionNewsletter(suscriptor.CorreoElectronico, suscriptor.Nombre,
                UrlBaseConBarra(urlBase) + "Newsletter.aspx?confirmar=" + tokenConfirmacion, HorasValidezConfirmacion);
            if (!enviado)
            {

                try
                {
                    _mpp.AnularEnvioConfirmacion(suscriptor);
                }
                catch (ErrorAccesoDatosException)
                {
                }

                Bitacora("ERROR", suscriptor.IdSuscriptorNewsletter, "No se pudo enviar el correo de confirmación del newsletter.");
                return ResultadoOperacion.Error(MensajeFalloCorreo);
            }

            return ResultadoOperacion.Ok(desdeCuenta
                ? "Te enviamos un correo a " + suscriptor.CorreoElectronico + " para confirmar la suscripción. El enlace vence en " + HorasValidezConfirmacion + " horas."
                : MensajeSuscripcion);
        }



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



        public ResultadoOperacion Reactivar(string tokenPreferencias)
        {
            return CambiarEstado(tokenPreferencias, SuscriptorNewsletter.EstadoBaja, SuscriptorNewsletter.EstadoActivo,
                "¡Bienvenido de nuevo! Vas a volver a recibir el newsletter de StageUp.",
                "Nueva suscripción al newsletter desde el enlace de preferencias.");
        }




        public ResultadoOperacion<SuscripcionDeCuenta> ObtenerDeCuenta(int idUsuarioExterno)
        {
            UsuarioExterno cuenta;
            ResultadoOperacion validacion = ObtenerCuentaActiva(idUsuarioExterno, out cuenta);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<SuscripcionDeCuenta>.Error(validacion.Mensaje);
            }

            try
            {
                string correo = cuenta.CorreoElectronico.Trim().ToLowerInvariant();
                SuscriptorNewsletter suscripcion = _mpp.ObtenerPorCorreo(new SuscriptorNewsletter { CorreoElectronico = correo });
                return ResultadoOperacion<SuscripcionDeCuenta>.Ok(new SuscripcionDeCuenta
                {
                    CorreoElectronico = correo,
                    Suscripcion = suscripcion,
                    Vinculada = suscripcion != null && suscripcion.IdUsuarioExterno == idUsuarioExterno
                });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<SuscripcionDeCuenta>.Error("No pudimos consultar tu suscripción al newsletter en este momento.");
            }
        }


        public ResultadoOperacion SuscribirDesdeCuenta(int idUsuarioExterno, IEnumerable<string> categorias, string urlBase)
        {
            UsuarioExterno cuenta;
            ResultadoOperacion validacion = ObtenerCuentaActiva(idUsuarioExterno, out cuenta);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            List<string> elegidas;
            ResultadoOperacion validacionCategorias = ValidarCategorias(categorias, out elegidas);
            if (!validacionCategorias.Exitoso)
            {
                return validacionCategorias;
            }

            string correo = cuenta.CorreoElectronico.Trim().ToLowerInvariant();
            if (!EsCorreoValido(correo))
            {
                return ResultadoOperacion.Error("El correo de tu cuenta no es válido para recibir el newsletter. Revisalo en Tus datos.");
            }

            string nombre = string.IsNullOrWhiteSpace(cuenta.Nombre) ? null : cuenta.Nombre.Trim();
            if (nombre != null && nombre.Length > LongitudMaximaNombre)
            {
                nombre = nombre.Substring(0, LongitudMaximaNombre);
            }

            return PedirConfirmacion(correo, nombre, elegidas, idUsuarioExterno, urlBase, true);
        }

        public ResultadoOperacion GuardarPreferenciasDeCuenta(int idUsuarioExterno, IEnumerable<string> categorias)
        {
            SuscriptorNewsletter suscriptor;
            ResultadoOperacion validacion = ObtenerVinculadaActiva(idUsuarioExterno, out suscriptor);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            List<string> elegidas;
            ResultadoOperacion validacionCategorias = ValidarCategorias(categorias, out elegidas);
            if (!validacionCategorias.Exitoso)
            {
                return validacionCategorias;
            }

            try
            {
                suscriptor.Categorias = elegidas;
                _mpp.GuardarCategorias(suscriptor);
                Bitacora("MODIFICACION", suscriptor.IdSuscriptorNewsletter, "Cambio de categorías del newsletter desde Mi perfil.", idUsuarioExterno);
                return ResultadoOperacion.Ok("Guardamos tus preferencias del newsletter.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos guardar tus preferencias en este momento. Intentá nuevamente.");
            }
        }

        public ResultadoOperacion DarDeBajaDesdeCuenta(int idUsuarioExterno)
        {
            SuscriptorNewsletter suscriptor;
            ResultadoOperacion validacion = ObtenerVinculadaActiva(idUsuarioExterno, out suscriptor);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            try
            {
                if (!_mpp.CambiarEstado(suscriptor, SuscriptorNewsletter.EstadoBaja))
                {
                    return ResultadoOperacion.Error("No pudimos actualizar la suscripción. Recargá la página e intentá nuevamente.");
                }

                Bitacora("BAJA", suscriptor.IdSuscriptorNewsletter, "Baja del newsletter desde Mi perfil.", idUsuarioExterno);
                return ResultadoOperacion.Ok("Listo, te diste de baja: no vas a recibir más el newsletter de StageUp.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos actualizar la suscripción en este momento. Intentá nuevamente.");
            }
        }







        public ResultadoOperacion SolicitarVinculoDesdeCuenta(int idUsuarioExterno, string urlBase)
        {
            ResultadoOperacion<SuscripcionDeCuenta> actual = ObtenerDeCuenta(idUsuarioExterno);
            if (!actual.Exitoso)
            {
                return ResultadoOperacion.Error(actual.Mensaje);
            }

            SuscriptorNewsletter suscriptor = actual.Valor.Suscripcion;
            if (suscriptor == null || suscriptor.Estado != SuscriptorNewsletter.EstadoActivo)
            {
                return ResultadoOperacion.Error("Tu correo no tiene una suscripción activa.");
            }

            if (actual.Valor.Vinculada)
            {
                return ResultadoOperacion.Error("La suscripción ya está vinculada a tu cuenta.");
            }

            if (suscriptor.FechaUltimoEnvioConfirmacion.HasValue && DateTime.Now - suscriptor.FechaUltimoEnvioConfirmacion.Value < EsperaEntreCorreos)
            {
                return ResultadoOperacion.Error("Ya te enviamos un correo hace menos de 5 minutos. Revisá tu bandeja de entrada (y spam) antes de pedir otro.");
            }

            string token = GenerarToken();
            try
            {
                if (!_mpp.SolicitarVinculo(suscriptor, new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno }, Hash(token),
                    DateTime.Now.AddHours(HorasValidezConfirmacion)))
                {
                    return ResultadoOperacion.Error("La suscripción cambió mientras tanto. Recargá la página e intentá nuevamente.");
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos preparar la vinculación en este momento. Intentá nuevamente.");
            }

            if (!_servicioCorreo.EnviarVinculoNewsletter(suscriptor.CorreoElectronico, suscriptor.Nombre,
                UrlBaseConBarra(urlBase) + "Newsletter.aspx?vincular=" + token, HorasValidezConfirmacion))
            {
                try
                {
                    _mpp.AnularEnvioConfirmacion(suscriptor);
                }
                catch (ErrorAccesoDatosException)
                {
                }
                return ResultadoOperacion.Error(MensajeFalloCorreo);
            }

            Bitacora("MODIFICACION", suscriptor.IdSuscriptorNewsletter, "Pedido de vinculación de la suscripción al newsletter con una cuenta (pendiente de confirmación).", idUsuarioExterno);
            return ResultadoOperacion.Ok("Te enviamos un correo a " + suscriptor.CorreoElectronico + " para confirmar la vinculación. El enlace vence en " +
                HorasValidezConfirmacion + " horas.");
        }


        public ResultadoOperacion ConfirmarVinculo(string tokenVinculo)
        {
            const string invalido = "El enlace para vincular la suscripción no es válido o ya venció. Pedí uno nuevo desde Mi perfil.";
            if (!EsToken(tokenVinculo))
            {
                return ResultadoOperacion.Error(invalido);
            }

            string hash = Hash(tokenVinculo.ToUpperInvariant());
            try
            {
                SuscriptorNewsletter suscriptor = _mpp.ObtenerPorHashVinculo(hash);
                if (suscriptor == null || suscriptor.Estado != SuscriptorNewsletter.EstadoActivo || !suscriptor.IdUsuarioExternoVinculoPendiente.HasValue ||
                    !suscriptor.VencimientoVinculo.HasValue || suscriptor.VencimientoVinculo.Value < DateTime.Now)
                {
                    return ResultadoOperacion.Error(invalido);
                }


                UsuarioExterno cuenta = _mppUsuario.ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = suscriptor.IdUsuarioExternoVinculoPendiente.Value });
                if (cuenta == null || !cuenta.Activo ||
                    !string.Equals((cuenta.CorreoElectronico ?? string.Empty).Trim(), suscriptor.CorreoElectronico, StringComparison.OrdinalIgnoreCase))
                {
                    return ResultadoOperacion.Error("No se pudo vincular: la cuenta que lo pidió ya no está activa o ya no usa este correo.");
                }

                if (!_mpp.ConfirmarVinculo(suscriptor, hash))
                {
                    return ResultadoOperacion.Error(invalido);
                }

                Bitacora("ASOCIACION", suscriptor.IdSuscriptorNewsletter, "Suscripción al newsletter vinculada a la cuenta (confirmado desde el correo).", cuenta.IdUsuarioExterno);
                return ResultadoOperacion.Ok("¡Listo! Tu suscripción quedó vinculada a tu cuenta: ya podés elegir categorías o darte de baja desde Mi perfil.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos vincular la suscripción en este momento. Intentá nuevamente.");
            }
        }




        public List<SuscriptorNewsletter> Listar(FiltroSuscriptoresNewsletter filtro, out int totalFiltrado)
        {
            totalFiltrado = 0;
            FiltroSuscriptoresNewsletter limpio = new FiltroSuscriptoresNewsletter
            {
                Texto = string.IsNullOrWhiteSpace(filtro == null ? null : filtro.Texto) ? null : filtro.Texto.Trim(),
                Estado = filtro != null && (filtro.Estado == SuscriptorNewsletter.EstadoActivo || filtro.Estado == SuscriptorNewsletter.EstadoPendiente ||
                    filtro.Estado == SuscriptorNewsletter.EstadoBaja) ? filtro.Estado : null,
                ConCuenta = filtro == null ? null : filtro.ConCuenta,
                Maximo = filtro == null || filtro.Maximo < 1 || filtro.Maximo > 1000 ? 200 : filtro.Maximo
            };

            if (limpio.Texto != null && limpio.Texto.Length > LongitudMaximaCorreo)
            {
                limpio.Texto = limpio.Texto.Substring(0, LongitudMaximaCorreo);
            }

            try
            {
                return _mpp.Listar(limpio, out totalFiltrado);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<SuscriptorNewsletter>();
            }
        }



        public ResultadoOperacion DarDeBajaAdministrativa(int idSuscriptorNewsletter, int idUsuarioInternoResponsable)
        {
            SuscriptorNewsletter suscriptor = ObtenerPorIdSeguro(idSuscriptorNewsletter);
            if (suscriptor == null)
            {
                return ResultadoOperacion.Error("No se encontró el suscriptor.");
            }

            if (suscriptor.Estado == SuscriptorNewsletter.EstadoBaja)
            {
                return ResultadoOperacion.Error("El suscriptor ya estaba dado de baja.");
            }

            try
            {
                if (!_mpp.CambiarEstado(suscriptor, SuscriptorNewsletter.EstadoBaja))
                {
                    return ResultadoOperacion.Error("El estado del suscriptor cambió mientras tanto. Recargá la lista.");
                }

                BitacoraInterna(idUsuarioInternoResponsable, "BAJA", suscriptor.IdSuscriptorNewsletter,
                    "Baja administrativa del suscriptor " + suscriptor.CorreoEnmascarado + " (estaba " + suscriptor.Estado + ").");
                return ResultadoOperacion.Ok("Se dio de baja a " + suscriptor.CorreoElectronico + ".");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No se pudo dar de baja al suscriptor. Intentá nuevamente.");
            }
        }


        public ResultadoOperacion ReenviarConfirmacion(int idSuscriptorNewsletter, int idUsuarioInternoResponsable, string urlBase)
        {
            SuscriptorNewsletter suscriptor = ObtenerPorIdSeguro(idSuscriptorNewsletter);
            if (suscriptor == null)
            {
                return ResultadoOperacion.Error("No se encontró el suscriptor.");
            }

            if (suscriptor.Estado != SuscriptorNewsletter.EstadoPendiente)
            {
                return ResultadoOperacion.Error("Solo se reenvía la confirmación a suscriptores pendientes.");
            }

            if (suscriptor.FechaUltimoEnvioConfirmacion.HasValue && DateTime.Now - suscriptor.FechaUltimoEnvioConfirmacion.Value < EsperaEntreCorreos)
            {
                return ResultadoOperacion.Error("Ya se le envió un correo hace menos de 5 minutos. Esperá un poco antes de reenviarlo.");
            }

            string tokenConfirmacion = GenerarToken();
            suscriptor.VencimientoConfirmacion = DateTime.Now.AddHours(HorasValidezConfirmacion);
            suscriptor.Nombre = null;
            suscriptor.IdUsuarioExterno = null;
            try
            {
                if (!_mpp.RenovarConfirmacion(suscriptor, Hash(tokenConfirmacion)))
                {
                    return ResultadoOperacion.Error("El estado del suscriptor cambió mientras tanto. Recargá la lista.");
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No se pudo preparar el reenvío. Intentá nuevamente.");
            }

            if (!_servicioCorreo.EnviarConfirmacionNewsletter(suscriptor.CorreoElectronico, null,
                UrlBaseConBarra(urlBase) + "Newsletter.aspx?confirmar=" + tokenConfirmacion, HorasValidezConfirmacion))
            {
                try
                {
                    _mpp.AnularEnvioConfirmacion(suscriptor);
                }
                catch (ErrorAccesoDatosException)
                {
                }
                return ResultadoOperacion.Error("No se pudo enviar el correo de confirmación. Revisá la configuración de correo saliente e intentá de nuevo.");
            }

            BitacoraInterna(idUsuarioInternoResponsable, "MODIFICACION", suscriptor.IdSuscriptorNewsletter,
                "Reenvío de la confirmación del newsletter a " + suscriptor.CorreoEnmascarado + ".");
            return ResultadoOperacion.Ok("Se reenvió el correo de confirmación a " + suscriptor.CorreoElectronico + ".");
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
            List<string> activas = BLL_CategoriaNovedad.ListarActivas().Select(c => c.Codigo).ToList();
            elegidas = (categorias ?? Enumerable.Empty<string>())
                .Select(c => activas.FirstOrDefault(a => string.Equals(a, (c ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
                .Where(c => c != null)
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

        private ResultadoOperacion ObtenerCuentaActiva(int idUsuarioExterno, out UsuarioExterno cuenta)
        {
            cuenta = null;
            try
            {
                cuenta = _mppUsuario.ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No pudimos consultar tu cuenta en este momento.");
            }

            if (cuenta == null || !cuenta.Activo || string.IsNullOrWhiteSpace(cuenta.CorreoElectronico))
            {
                return ResultadoOperacion.Error("Tu cuenta no está activa.");
            }

            return ResultadoOperacion.Ok();
        }


        private ResultadoOperacion ObtenerVinculadaActiva(int idUsuarioExterno, out SuscriptorNewsletter suscriptor)
        {
            suscriptor = null;
            ResultadoOperacion<SuscripcionDeCuenta> actual = ObtenerDeCuenta(idUsuarioExterno);
            if (!actual.Exitoso)
            {
                return ResultadoOperacion.Error(actual.Mensaje);
            }

            if (actual.Valor.Suscripcion == null || actual.Valor.Estado != SuscriptorNewsletter.EstadoActivo)
            {
                return ResultadoOperacion.Error("No tenés una suscripción activa al newsletter.");
            }

            if (!actual.Valor.Vinculada)
            {
                return ResultadoOperacion.Error("Esta suscripción se hizo sin iniciar sesión: administrala desde el enlace que figura en cada newsletter.");
            }

            suscriptor = actual.Valor.Suscripcion;
            return ResultadoOperacion.Ok();
        }

        private SuscriptorNewsletter ObtenerPorIdSeguro(int idSuscriptorNewsletter)
        {
            try
            {
                return idSuscriptorNewsletter <= 0 ? null : _mpp.ObtenerPorId(new SuscriptorNewsletter { IdSuscriptorNewsletter = idSuscriptorNewsletter });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        private void Bitacora(string operacion, int idSuscriptor, string descripcion, int? idUsuarioExterno = null)
        {
            try
            {
                _bitacora.Registrar(idUsuarioExterno, operacion, TipoEntidadBitacora, idSuscriptor, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }

        private void BitacoraInterna(int idUsuarioInterno, string operacion, int idSuscriptor, string descripcion)
        {
            try
            {
                _bitacora.RegistrarInterno(idUsuarioInterno, operacion, TipoEntidadBitacora, idSuscriptor, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }
    }
}
