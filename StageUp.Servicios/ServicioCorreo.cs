using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using StageUp.BE.Entidades;

namespace StageUp.Servicios
{
    public class ServicioCorreo
    {
        private const string LogoToken = "{{STAGEUP_LOGO}}";
        private const string LogoContentId = "stageup-logo";
        private const string NewsletterImageContentId = "stageup-newsletter-image";

        public bool EnviarCodigoActivacion(string destinatario, string nombreDestinatario, string codigo)
        {
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Activación de cuenta",
                Titulo = "Activá tu cuenta de StageUp",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    "Recibimos tu registro y necesitamos confirmar que este correo te pertenece.",
                    "Ingresá el siguiente código en StageUp para activar tu cuenta y empezar a usar la plataforma."
                },
                Codigo = codigo,
                Nota = "Si no creaste una cuenta en StageUp, podés ignorar este correo."
            });

            return Enviar(destinatario, "Activá tu cuenta de StageUp", cuerpo);
        }

        public bool EnviarBienvenida(string destinatario, string nombreDestinatario)
        {
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Cuenta activada",
                Titulo = "¡Bienvenido/a a StageUp!",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    "Tu cuenta fue activada correctamente.",
                    "Ya podés iniciar sesión, explorar espacios artísticos y gestionar tus próximas actividades desde StageUp."
                }
            });

            return Enviar(destinatario, "¡Bienvenido/a a StageUp!", cuerpo);
        }

        public bool EnviarCodigoRecuperacion(string destinatario, string nombreDestinatario, string codigo)
        {
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Recuperación de contraseña",
                Titulo = "Recuperá el acceso a tu cuenta",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    "Recibimos una solicitud para cambiar la contraseña de tu cuenta.",
                    "Usá este código para continuar con la recuperación."
                },
                Codigo = codigo,
                Nota = "Este código vence en 15 minutos. Si no solicitaste este cambio, podés ignorar este correo."
            });

            return Enviar(destinatario, "Recuperá el acceso a tu cuenta StageUp", cuerpo);
        }

        public bool EnviarNotificacionCambioPassword(string destinatario, string nombreDestinatario)
        {
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Seguridad de la cuenta",
                Titulo = "Tu contraseña fue actualizada",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    "Te confirmamos que la contraseña de tu cuenta de StageUp fue modificada correctamente."
                },
                Nota = "Si no realizaste este cambio, contactanos a la brevedad para proteger tu cuenta."
            });

            return Enviar(destinatario, "Tu contraseña de StageUp fue actualizada", cuerpo);
        }

        // CU-001-003 A8 (pasos 9 y 10): aviso de baja lógica de la cuenta.
        public bool EnviarConfirmacionBajaCuenta(string destinatario, string nombreDestinatario)
        {
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Cuenta",
                Titulo = "Tu cuenta fue dada de baja",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    "Te confirmamos que tu cuenta de StageUp fue dada de baja, tal como lo pediste desde tu perfil.",
                    "Desde ahora ya no podés ingresar con ella. Conservamos el historial de tus reservas, pagos y calificaciones, como indican nuestros términos y condiciones."
                },
                Nota = "Si no fuiste vos quien pidió la baja, escribinos a la brevedad desde la sección Contáctenos."
            });

            return Enviar(destinatario, "Tu cuenta de StageUp fue dada de baja", cuerpo);
        }

        public bool EnviarNewsletter(
            string destinatario,
            string nombreDestinatario,
            string titulo,
            string resumen,
            string llamadaAccionTexto,
            string llamadaAccionUrl,
            string imagenUrl = null,
            string rutaImagenNewsletter = null)
        {
            bool usarImagenEmbebida = !string.IsNullOrWhiteSpace(rutaImagenNewsletter) && File.Exists(rutaImagenNewsletter);
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Novedades StageUp",
                Titulo = titulo,
                NombreDestinatario = nombreDestinatario,
                ImagenUrl = usarImagenEmbebida ? "cid:" + NewsletterImageContentId : imagenUrl,
                Parrafos = new[]
                {
                    resumen
                },
                TextoBoton = llamadaAccionTexto,
                UrlBoton = llamadaAccionUrl,
                Nota = "Recibís este correo porque tenés una cuenta en StageUp. Próximamente vas a poder administrar tus preferencias de novedades."
            });

            return Enviar(destinatario, titulo + " | StageUp", cuerpo, usarImagenEmbebida ? rutaImagenNewsletter : null);
        }

        public bool EnviarNotificacionRespuestaTicket(string destinatario, string nombreDestinatario, string asuntoTicket)
        {
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Soporte StageUp",
                Titulo = "Tenés una respuesta de soporte",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    "Soporte respondió tu consulta \"" + asuntoTicket + "\".",
                    "Ingresá a tu cuenta de StageUp, sección Soporte, para ver la respuesta completa y seguir la conversación."
                },
                Nota = "Este es un mensaje automático. Por favor, no respondas directamente a este correo."
            });

            return Enviar(destinatario, "Respuesta de soporte StageUp: " + asuntoTicket, cuerpo);
        }

        // ------------------------------------------------------------------
        // Reservas (ítems 5A y 7 de la segunda entrega): todos los avisos de
        // una reserva por mail, con la misma plantilla StageUp que el resto
        // de los correos. Los datos de la reserva (espacio, fecha, horario,
        // importe) se muestran en un bloque de detalle común.
        // ------------------------------------------------------------------
        public bool EnviarSolicitudReservaEnviada(string destinatario, string nombreDestinatario, Reserva reserva)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Solicitud enviada",
                "Recibimos tu solicitud de reserva",
                new[]
                {
                    "Tu solicitud para \"" + reserva.NombreEspacio + "\" ya le llegó al gestor del espacio.",
                    "Te vamos a avisar por este medio y en StageUp apenas la acepte o la rechace. Mientras tanto podés seguirla desde Mis reservas."
                },
                null,
                "Recibimos tu solicitud de reserva: " + reserva.NombreEspacio);
        }

        public bool EnviarSolicitudReservaRecibida(
            string destinatario, string nombreDestinatario, Reserva reserva, string nombreSolicitante)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Nueva solicitud",
                "Tenés una nueva solicitud de reserva",
                new[]
                {
                    (string.IsNullOrWhiteSpace(nombreSolicitante) ? "Un artista" : nombreSolicitante.Trim()) +
                        " quiere reservar tu espacio \"" + reserva.NombreEspacio + "\".",
                    string.IsNullOrWhiteSpace(reserva.ComentarioSolicitante)
                        ? null
                        : "Comentario del solicitante: \"" + reserva.ComentarioSolicitante + "\"",
                    "Ingresá a StageUp, sección Solicitudes recibidas, para aceptarla o rechazarla."
                },
                null,
                "Nueva solicitud de reserva para " + reserva.NombreEspacio);
        }

        public bool EnviarReservaAceptada(string destinatario, string nombreDestinatario, Reserva reserva)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                EsperaPago(reserva) ? "Reserva aceptada" : "Reserva confirmada",
                "¡Tu reserva fue aceptada!",
                new[]
                {
                    EsperaPago(reserva)
                        ? "El gestor aceptó tu solicitud para \"" + reserva.NombreEspacio + "\". Para confirmarla, pagala desde Mis reservas antes del " +
                          reserva.FechaLimitePago.Value.ToString("dd/MM/yyyy 'a las' HH:mm", CultureInfo.InvariantCulture) + " hs."
                        : "El gestor aceptó tu solicitud para \"" + reserva.NombreEspacio + "\". Tu reserva ya está confirmada.",
                    string.IsNullOrWhiteSpace(reserva.ComentarioResolucion)
                        ? null
                        : "Mensaje del gestor: \"" + reserva.ComentarioResolucion + "\""
                },
                EsperaPago(reserva)
                    ? "Podés pagar con tarjeta, con tu saldo a favor o combinando los dos. Si no se paga a tiempo, la reserva se cancela sola, sin cargo."
                    : "Si necesitás cancelarla, hacelo desde Mis reservas. Según la anticipación con la que canceles puede corresponder un cargo: lo ves en el detalle de la reserva antes de confirmar.",
                "Tu reserva fue aceptada: " + reserva.NombreEspacio);
        }

        public bool EnviarReservaRechazada(string destinatario, string nombreDestinatario, Reserva reserva)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Reserva rechazada",
                "Tu solicitud de reserva no fue aceptada",
                new[]
                {
                    "El gestor de \"" + reserva.NombreEspacio + "\" no pudo aceptar tu solicitud para esta fecha.",
                    string.IsNullOrWhiteSpace(reserva.ComentarioResolucion)
                        ? null
                        : "Mensaje del gestor: \"" + reserva.ComentarioResolucion + "\"",
                    "Podés buscar otro horario u otro espacio desde Explorar espacios."
                },
                null,
                "Tu solicitud de reserva fue rechazada: " + reserva.NombreEspacio);
        }

        public bool EnviarCancelacionAlSolicitante(string destinatario, string nombreDestinatario, Reserva reserva)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Reserva cancelada",
                "Cancelaste tu reserva",
                new[]
                {
                    "Confirmamos la cancelación de tu reserva para \"" + reserva.NombreEspacio + "\".",
                    reserva.ComisionAplicada && reserva.ImporteComision.HasValue
                        ? "Como faltaba poco para el horario reservado, se aplicó una comisión de cancelación de " +
                          FormatearImporte(reserva.ImporteComision.Value, reserva.Moneda) + "."
                        : "La cancelación no tuvo ningún costo.",
                    reserva.EstadoPago == "Devuelto"
                        ? "Lo que pagaste quedó como saldo a favor en tu cuenta corriente (te mandamos las notas de crédito y débito en otro correo). Podés usarlo en tu próxima reserva."
                        : null
                },
                null,
                "Cancelaste tu reserva: " + reserva.NombreEspacio);
        }

        public bool EnviarCancelacionAlGestor(
            string destinatario, string nombreDestinatario, Reserva reserva, string nombreSolicitante)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Reserva cancelada",
                "Se canceló una reserva de tu espacio",
                new[]
                {
                    (string.IsNullOrWhiteSpace(nombreSolicitante) ? "El solicitante" : nombreSolicitante.Trim()) +
                        " canceló su reserva para \"" + reserva.NombreEspacio + "\".",
                    "Ese horario vuelve a quedar disponible para nuevas solicitudes."
                },
                null,
                "Se canceló una reserva de " + reserva.NombreEspacio);
        }

        public bool EnviarRecordatorioReserva(string destinatario, string nombreDestinatario, Reserva reserva)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Recordatorio",
                "Tu reserva es mañana",
                new[]
                {
                    "Te recordamos que tenés una reserva confirmada en \"" + reserva.NombreEspacio + "\" dentro de las próximas 24 horas."
                },
                "Si no vas a poder asistir, cancelala desde Mis reservas para liberar el horario.",
                "Recordatorio de tu reserva: " + reserva.NombreEspacio);
        }

        public bool EnviarReservaFinalizada(string destinatario, string nombreDestinatario, Reserva reserva)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Reserva finalizada",
                "¿Cómo te fue en " + reserva.NombreEspacio + "?",
                new[]
                {
                    "Tu reserva ya finalizó. ¡Esperamos que hayas tenido una gran experiencia!",
                    "Ingresá a Mis reservas para calificar el espacio: tu reseña ayuda a otros artistas a elegir y al gestor a mejorar."
                },
                null,
                "Contanos cómo te fue en " + reserva.NombreEspacio);
        }

        // ------------------------------------------------------------------
        // Pagos, notas de crédito/débito y cuenta corriente (ítems 5B, 5C,
        // 5D y 6B de la segunda entrega).
        // ------------------------------------------------------------------
        public bool EnviarPagoAprobado(string destinatario, string nombreDestinatario, Reserva reserva, Pago pago)
        {
            if (string.IsNullOrWhiteSpace(destinatario) || reserva == null || pago == null)
            {
                return false;
            }

            List<KeyValuePair<string, string>> detalles = ConstruirDetallesReserva(reserva);
            detalles.Add(new KeyValuePair<string, string>("N° de pago", pago.IdPago.ToString(CultureInfo.InvariantCulture)));
            detalles.Add(new KeyValuePair<string, string>("Total pagado", FormatearImporte(pago.ImporteTotal, pago.Moneda)));
            if (pago.ImporteTarjeta > 0)
            {
                detalles.Add(new KeyValuePair<string, string>("Con tarjeta",
                    FormatearImporte(pago.ImporteTarjeta, pago.Moneda) + " · " + pago.MarcaTarjeta + " terminada en " + pago.UltimosDigitos +
                    " (autorización " + pago.CodigoAutorizacion + ")"));
            }

            if (pago.ImporteSaldo > 0)
            {
                detalles.Add(new KeyValuePair<string, string>("Con saldo a favor", FormatearImporte(pago.ImporteSaldo, pago.Moneda)));
            }

            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Comprobante de pago",
                Titulo = "¡Pago recibido! Tu reserva está confirmada",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    "Recibimos el pago de tu reserva para \"" + reserva.NombreEspacio + "\". Ya está todo listo.",
                    "Podés ver el detalle del pago y tus movimientos en Mi cuenta corriente."
                },
                Detalles = detalles,
                Nota = "Si cancelás la reserva, lo que pagaste vuelve como saldo a favor, descontando el cargo por cancelación que corresponda según la anticipación."
            });

            return Enviar(destinatario, "Comprobante de pago: " + reserva.NombreEspacio, cuerpo);
        }

        public bool EnviarPagoRecibidoGestor(string destinatario, string nombreDestinatario, Reserva reserva, decimal importeNeto)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Reserva pagada",
                "Una reserva de tu espacio ya está pagada",
                new[]
                {
                    "El solicitante pagó su reserva para \"" + reserva.NombreEspacio + "\". La reserva queda confirmada.",
                    "Te acreditamos " + FormatearImporte(importeNeto, reserva.Moneda) +
                        " en tu cuenta corriente de gestor (el importe de la reserva menos la comisión de StageUp)."
                },
                null,
                "Reserva pagada: " + reserva.NombreEspacio);
        }

        public bool EnviarPagoVencido(string destinatario, string nombreDestinatario, Reserva reserva)
        {
            return EnviarAvisoReserva(destinatario, nombreDestinatario, reserva,
                "Reserva cancelada",
                "Se venció el plazo para pagar tu reserva",
                new[]
                {
                    "No recibimos el pago de tu reserva para \"" + reserva.NombreEspacio + "\" dentro del plazo, así que la cancelamos sin ningún cargo.",
                    "Si todavía querés ese espacio, podés volver a solicitarlo desde Explorar espacios."
                },
                null,
                "Se canceló tu reserva por falta de pago: " + reserva.NombreEspacio);
        }

        public bool EnviarComprobanteEmitido(string destinatario, string nombreDestinatario, Comprobante comprobante)
        {
            if (string.IsNullOrWhiteSpace(destinatario) || comprobante == null)
            {
                return false;
            }

            bool esCredito = comprobante.Tipo == "NC";
            string nombreTipo = esCredito ? "Nota de crédito" : "Nota de débito";
            List<KeyValuePair<string, string>> detalles = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Comprobante", nombreTipo + " " + comprobante.Numero),
                new KeyValuePair<string, string>("Fecha", comprobante.FechaEmision.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("Importe", FormatearImporte(comprobante.Importe, comprobante.Moneda)),
                new KeyValuePair<string, string>("Motivo", comprobante.Motivo)
            };

            if (comprobante.IdReserva.HasValue)
            {
                detalles.Add(new KeyValuePair<string, string>("N° de reserva", comprobante.IdReserva.Value.ToString(CultureInfo.InvariantCulture)));
            }

            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = nombreTipo,
                Titulo = esCredito ? "Te emitimos una nota de crédito" : "Te emitimos una nota de débito",
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    esCredito
                        ? "El importe se acreditó como saldo a favor en tu cuenta corriente de StageUp."
                        : "El importe se debitó de tu cuenta corriente de StageUp.",
                    "Podés ver todos tus movimientos en Mi cuenta corriente."
                },
                Detalles = detalles
            });

            return Enviar(destinatario, nombreTipo + " " + comprobante.Numero + " | StageUp", cuerpo);
        }

        private static bool EsperaPago(Reserva reserva)
        {
            return reserva.EstadoPago == "Pendiente" && reserva.FechaLimitePago.HasValue;
        }

        private static bool EnviarAvisoReserva(
            string destinatario, string nombreDestinatario, Reserva reserva,
            string etiqueta, string titulo, string[] parrafos, string nota, string asunto)
        {
            if (string.IsNullOrWhiteSpace(destinatario) || reserva == null)
            {
                return false;
            }

            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = etiqueta,
                Titulo = titulo,
                NombreDestinatario = nombreDestinatario,
                Parrafos = parrafos,
                Detalles = ConstruirDetallesReserva(reserva),
                Nota = nota
            });

            return Enviar(destinatario, asunto, cuerpo);
        }

        private static List<KeyValuePair<string, string>> ConstruirDetallesReserva(Reserva reserva)
        {
            List<KeyValuePair<string, string>> detalles = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Espacio", reserva.NombreEspacio),
                new KeyValuePair<string, string>("Fecha", reserva.FechaSolicitada.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))
            };

            if (reserva.MinutoDesde.HasValue && reserva.MinutoHasta.HasValue)
            {
                detalles.Add(new KeyValuePair<string, string>("Horario",
                    FormatearHora(reserva.MinutoDesde.Value) + " a " + FormatearHora(reserva.MinutoHasta.Value) + " hs"));
            }

            if (reserva.ImporteEstimado.HasValue)
            {
                detalles.Add(new KeyValuePair<string, string>("Importe estimado",
                    FormatearImporte(reserva.ImporteEstimado.Value, reserva.Moneda)));
            }

            detalles.Add(new KeyValuePair<string, string>("N° de reserva",
                reserva.IdReserva.ToString(CultureInfo.InvariantCulture)));

            return detalles;
        }

        private static string FormatearHora(int minutos)
        {
            return (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static string FormatearImporte(decimal importe, string moneda)
        {
            return (string.IsNullOrWhiteSpace(moneda) ? "ARS" : moneda) + " " +
                importe.ToString("N2", CultureInfo.GetCultureInfo("es-AR"));
        }

        private static bool Enviar(string destinatario, string asunto, string cuerpoHtml, string rutaImagenNewsletter = null)
        {
            try
            {
                string host = ConfigurationManager.AppSettings["SmtpHost"];
                int puerto = int.Parse(ConfigurationManager.AppSettings["SmtpPuerto"]);
                bool usarSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpUsarSsl"]);
                string usuario = ConfigurationManager.AppSettings["SmtpUsuario"];
                string password = ConfigurationManager.AppSettings["SmtpPassword"];
                string correoRemitente = ConfigurationManager.AppSettings["CorreoRemitente"];
                string nombreRemitente = ConfigurationManager.AppSettings["NombreRemitente"];

                using (MailMessage mensaje = new MailMessage())
                {
                    string cuerpoFinal = PrepararLogo(cuerpoHtml);

                    mensaje.From = new MailAddress(correoRemitente, nombreRemitente);
                    mensaje.To.Add(destinatario);
                    mensaje.Subject = asunto;
                    mensaje.Body = cuerpoFinal;
                    mensaje.IsBodyHtml = true;

                    AlternateView vistaHtml = AlternateView.CreateAlternateViewFromString(cuerpoFinal, null, MediaTypeNames.Text.Html);
                    AgregarLogoSiExiste(vistaHtml);
                    AgregarImagenNewsletterSiExiste(vistaHtml, rutaImagenNewsletter);
                    mensaje.AlternateViews.Add(vistaHtml);

                    using (SmtpClient cliente = new SmtpClient(host, puerto))
                    {
                        cliente.EnableSsl = usarSsl;
                        cliente.Credentials = new NetworkCredential(usuario, password);
                        cliente.Send(mensaje);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ConstruirPlantillaStageUp(ContenidoCorreo contenido)
        {
            string etiqueta = Codificar(contenido.Etiqueta);
            string titulo = Codificar(contenido.Titulo);
            string saludo = "Hola " + Codificar(ObtenerNombre(contenido.NombreDestinatario)) + ",";
            string parrafos = ConstruirParrafos(contenido.Parrafos);
            string detalles = ConstruirDetalles(contenido.Detalles);
            string bloqueCodigo = ConstruirBloqueCodigo(contenido.Codigo);
            string boton = ConstruirBoton(contenido.TextoBoton, contenido.UrlBoton);
            string nota = ConstruirNota(contenido.Nota);
            string imagen = ConstruirImagen(contenido.ImagenUrl);

            return
                "<!DOCTYPE html>" +
                "<html lang=\"es\">" +
                "<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"></head>" +
                "<body style=\"margin:0;padding:0;background:#f7eee8;font-family:Arial,Helvetica,sans-serif;color:#4b332a;\">" +
                "<span style=\"display:none!important;visibility:hidden;opacity:0;color:transparent;height:0;width:0;overflow:hidden;\">" +
                titulo +
                "</span>" +
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f7eee8;margin:0;padding:32px 12px;\">" +
                "<tr><td align=\"center\">" +
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:620px;background:#fffaf7;border:1px solid #e7d5cc;border-radius:20px;overflow:hidden;box-shadow:0 18px 42px rgba(86,45,31,.12);\">" +
                "<tr><td style=\"padding:28px 32px 18px;background:#fffaf7;border-bottom:1px solid #eadbd3;\">" +
                "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr>" +
                "<td style=\"vertical-align:middle;\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\"><tr>" +
                "<td style=\"vertical-align:middle;padding-right:12px;\">" + LogoToken + "</td>" +
                "<td style=\"vertical-align:middle;\">" +
                "<div style=\"font-family:Georgia,'Times New Roman',serif;font-size:28px;line-height:1;color:#7a0c20;font-weight:bold;\">StageUp</div>" +
                "<div style=\"font-size:11px;letter-spacing:2px;text-transform:uppercase;color:#9a796b;margin-top:5px;\">Encontrá tu espacio</div>" +
                "</td>" +
                "</tr></table>" +
                "</td>" +
                "<td align=\"right\" style=\"vertical-align:middle;font-size:12px;color:#8a6a5e;font-weight:bold;text-transform:uppercase;letter-spacing:1px;\">" +
                etiqueta +
                "</td>" +
                "</tr></table>" +
                "</td></tr>" +
                "<tr><td style=\"padding:34px 32px 28px;\">" +
                imagen +
                "<h1 style=\"margin:0 0 16px;font-family:Georgia,'Times New Roman',serif;font-size:34px;line-height:1.12;color:#7a0c20;font-weight:normal;\">" +
                titulo +
                "</h1>" +
                "<p style=\"margin:0 0 18px;font-size:17px;line-height:1.6;color:#4b332a;\">" + saludo + "</p>" +
                parrafos +
                detalles +
                bloqueCodigo +
                boton +
                nota +
                "</td></tr>" +
                "<tr><td style=\"padding:22px 32px;background:#7a0c20;color:#fff7f2;\">" +
                "<p style=\"margin:0;font-size:14px;line-height:1.5;\"><strong>StageUp</strong> · Encontrá tu espacio, potenciá tu arte.</p>" +
                "<p style=\"margin:8px 0 0;font-size:12px;line-height:1.5;color:#f1d9cf;\">Este es un mensaje automático. Por favor, no respondas directamente a este correo.</p>" +
                "</td></tr>" +
                "</table>" +
                "</td></tr>" +
                "</table>" +
                "</body></html>";
        }

        private static string ConstruirParrafos(string[] parrafos)
        {
            if (parrafos == null || parrafos.Length == 0)
            {
                return string.Empty;
            }

            string html = string.Empty;
            foreach (string parrafo in parrafos)
            {
                if (!string.IsNullOrWhiteSpace(parrafo))
                {
                    html += "<p style=\"margin:0 0 18px;font-size:16px;line-height:1.65;color:#6b4a3d;\">" +
                        Codificar(parrafo) +
                        "</p>";
                }
            }

            return html;
        }

        // Bloque "clave: valor" (por ejemplo, los datos de una reserva), con la
        // misma paleta que el bloque de código de verificación.
        private static string ConstruirDetalles(List<KeyValuePair<string, string>> detalles)
        {
            if (detalles == null || detalles.Count == 0)
            {
                return string.Empty;
            }

            string filas = string.Empty;
            foreach (KeyValuePair<string, string> detalle in detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.Value))
                {
                    continue;
                }

                filas +=
                    "<tr>" +
                    "<td style=\"padding:6px 12px 6px 0;font-size:13px;color:#8a6a5e;font-weight:bold;text-transform:uppercase;letter-spacing:1px;white-space:nowrap;vertical-align:top;\">" +
                    Codificar(detalle.Key) +
                    "</td>" +
                    "<td style=\"padding:6px 0;font-size:16px;color:#4b332a;\">" +
                    Codificar(detalle.Value) +
                    "</td>" +
                    "</tr>";
            }

            return
                "<div style=\"margin:8px 0 24px;padding:18px 22px;background:#fff2ec;border:1px solid #e5c5b8;border-radius:16px;\">" +
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\">" +
                filas +
                "</table>" +
                "</div>";
        }

        private static string ConstruirBloqueCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return string.Empty;
            }

            return
                "<div style=\"margin:28px 0;padding:22px 24px;background:#fff2ec;border:1px solid #e5c5b8;border-radius:16px;text-align:center;\">" +
                "<div style=\"font-size:12px;letter-spacing:2px;text-transform:uppercase;color:#8a6a5e;font-weight:bold;margin-bottom:10px;\">Código de verificación</div>" +
                "<div style=\"font-size:34px;line-height:1;letter-spacing:7px;color:#7a0c20;font-weight:bold;font-family:Arial,Helvetica,sans-serif;\">" +
                Codificar(codigo) +
                "</div>" +
                "</div>";
        }

        private static string ConstruirBoton(string texto, string url)
        {
            if (string.IsNullOrWhiteSpace(texto) || string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            return
                "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:28px 0 8px;\"><tr><td>" +
                "<a href=\"" + Codificar(url) + "\" style=\"display:inline-block;background:#7a0c20;color:#ffffff;text-decoration:none;font-weight:bold;font-size:15px;padding:14px 22px;border-radius:12px;\">" +
                Codificar(texto) +
                "</a>" +
                "</td></tr></table>";
        }

        private static string ConstruirImagen(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            return
                "<img src=\"" + Codificar(url) + "\" width=\"556\" alt=\"\" style=\"display:block;width:100%;max-width:556px;height:auto;border:0;border-radius:16px;margin:0 0 26px;\">";
        }

        private static string ConstruirNota(string nota)
        {
            if (string.IsNullOrWhiteSpace(nota))
            {
                return string.Empty;
            }

            return
                "<div style=\"margin-top:24px;padding:16px 18px;background:#fbf5f1;border-left:4px solid #7a0c20;border-radius:12px;\">" +
                "<p style=\"margin:0;font-size:14px;line-height:1.55;color:#6b4a3d;\">" +
                Codificar(nota) +
                "</p>" +
                "</div>";
        }

        private static string PrepararLogo(string cuerpoHtml)
        {
            string logoHtml = ExisteLogo()
                ? "<img src=\"cid:" + LogoContentId + "\" width=\"42\" height=\"42\" alt=\"StageUp\" style=\"display:block;border:0;border-radius:12px;\">"
                : "<div style=\"width:42px;height:42px;border-radius:12px;background:#f3dfd4;color:#7a0c20;text-align:center;line-height:42px;font-family:Georgia,'Times New Roman',serif;font-size:24px;font-weight:bold;\">S</div>";

            return cuerpoHtml.Replace(LogoToken, logoHtml);
        }

        private static void AgregarLogoSiExiste(AlternateView vistaHtml)
        {
            string rutaLogo = ObtenerRutaLogo();
            if (string.IsNullOrEmpty(rutaLogo) || !File.Exists(rutaLogo))
            {
                return;
            }

            LinkedResource logo = new LinkedResource(rutaLogo, "image/png")
            {
                ContentId = LogoContentId,
                TransferEncoding = TransferEncoding.Base64
            };

            logo.ContentType.Name = Path.GetFileName(rutaLogo);
            vistaHtml.LinkedResources.Add(logo);
        }

        private static void AgregarImagenNewsletterSiExiste(AlternateView vistaHtml, string rutaImagen)
        {
            if (string.IsNullOrWhiteSpace(rutaImagen) || !File.Exists(rutaImagen))
            {
                return;
            }

            LinkedResource imagen = new LinkedResource(rutaImagen, ObtenerMimeImagen(rutaImagen))
            {
                ContentId = NewsletterImageContentId,
                TransferEncoding = TransferEncoding.Base64
            };

            imagen.ContentType.Name = Path.GetFileName(rutaImagen);
            vistaHtml.LinkedResources.Add(imagen);
        }

        private static string ObtenerMimeImagen(string rutaImagen)
        {
            string extension = Path.GetExtension(rutaImagen);
            if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return "image/jpeg";
            }

            if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
            {
                return "image/gif";
            }

            return "image/png";
        }

        private static bool ExisteLogo()
        {
            string rutaLogo = ObtenerRutaLogo();
            return !string.IsNullOrEmpty(rutaLogo) && File.Exists(rutaLogo);
        }

        private static string ObtenerRutaLogo()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string rutaLogo = Path.Combine(baseDirectory, "Content", "Images", "favicon.png");

            return File.Exists(rutaLogo) ? rutaLogo : null;
        }

        private static string ObtenerNombre(string nombre)
        {
            return string.IsNullOrWhiteSpace(nombre) ? "artista" : nombre.Trim();
        }

        private static string Codificar(string valor)
        {
            return WebUtility.HtmlEncode(valor ?? string.Empty);
        }

        private class ContenidoCorreo
        {
            public string Etiqueta { get; set; }
            public string Titulo { get; set; }
            public string NombreDestinatario { get; set; }
            public string[] Parrafos { get; set; }
            public string ImagenUrl { get; set; }
            public string Codigo { get; set; }
            public List<KeyValuePair<string, string>> Detalles { get; set; }
            public string Nota { get; set; }
            public string TextoBoton { get; set; }
            public string UrlBoton { get; set; }
        }
    }
}
