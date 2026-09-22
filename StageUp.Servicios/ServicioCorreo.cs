using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace StageUp.Servicios
{
    public class ServicioCorreo
    {
        private const string LogoToken = "{{STAGEUP_LOGO}}";
        private const string LogoContentId = "stageup-logo";

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

        public bool EnviarNewsletter(
            string destinatario,
            string nombreDestinatario,
            string titulo,
            string resumen,
            string llamadaAccionTexto,
            string llamadaAccionUrl)
        {
            string cuerpo = ConstruirPlantillaStageUp(new ContenidoCorreo
            {
                Etiqueta = "Novedades StageUp",
                Titulo = titulo,
                NombreDestinatario = nombreDestinatario,
                Parrafos = new[]
                {
                    resumen
                },
                TextoBoton = llamadaAccionTexto,
                UrlBoton = llamadaAccionUrl,
                Nota = "Recibís este correo porque tenés una cuenta en StageUp. Próximamente vas a poder administrar tus preferencias de novedades."
            });

            return Enviar(destinatario, titulo + " | StageUp", cuerpo);
        }

        private static bool Enviar(string destinatario, string asunto, string cuerpoHtml)
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
            string bloqueCodigo = ConstruirBloqueCodigo(contenido.Codigo);
            string boton = ConstruirBoton(contenido.TextoBoton, contenido.UrlBoton);
            string nota = ConstruirNota(contenido.Nota);

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
                "<h1 style=\"margin:0 0 16px;font-family:Georgia,'Times New Roman',serif;font-size:34px;line-height:1.12;color:#7a0c20;font-weight:normal;\">" +
                titulo +
                "</h1>" +
                "<p style=\"margin:0 0 18px;font-size:17px;line-height:1.6;color:#4b332a;\">" + saludo + "</p>" +
                parrafos +
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
            public string Codigo { get; set; }
            public string Nota { get; set; }
            public string TextoBoton { get; set; }
            public string UrlBoton { get; set; }
        }
    }
}
