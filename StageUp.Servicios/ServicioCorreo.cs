using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace StageUp.Servicios
{
    /// <summary>
    /// Envío de correo electrónico (activación de cuenta, bienvenida,
    /// recuperación de contraseña, notificación de cambio de contraseña).
    /// La configuración SMTP se lee de Web.config (appSettings) para no
    /// tener credenciales hardcodeadas en el código.
    ///
    /// Nota: por ahora Web.config tiene valores de ejemplo/placeholder
    /// (ver comentarios en el propio Web.config). Para que el envío real
    /// funcione hace falta cargar una cuenta de correo del proyecto
    /// (se recomienda una cuenta de Gmail propia, con contraseña de
    /// aplicación, como sugiere la pauta de la cátedra).
    /// Todos los métodos son tolerantes a fallas: si el envío no se puede
    /// completar, devuelven false en vez de romper el flujo del caso de
    /// uso (ver caminos alternativos A6 de CU-001-001 y A4 de CU-001-002).
    /// </summary>
    public class ServicioCorreo
    {
        public bool EnviarCodigoActivacion(string destinatario, string nombreDestinatario, string codigo)
        {
            string cuerpo = ConstruirCuerpoHtml(
                "Activá tu cuenta de StageUp",
                string.Format(
                    "Hola {0},<br/><br/>Tu código de activación es: <b style=\"font-size:20px;\">{1}</b>" +
                    "<br/><br/>Ingresalo en StageUp para activar tu cuenta.",
                    nombreDestinatario, codigo));

            return Enviar(destinatario, "Activá tu cuenta de StageUp", cuerpo);
        }

        public bool EnviarBienvenida(string destinatario, string nombreDestinatario)
        {
            string cuerpo = ConstruirCuerpoHtml(
                "¡Bienvenido/a a StageUp!",
                string.Format(
                    "Hola {0},<br/><br/>Tu cuenta fue activada correctamente. Ya podés iniciar sesión y empezar a usar StageUp.",
                    nombreDestinatario));

            return Enviar(destinatario, "¡Bienvenido/a a StageUp!", cuerpo);
        }

        public bool EnviarCodigoRecuperacion(string destinatario, string nombreDestinatario, string codigo)
        {
            string cuerpo = ConstruirCuerpoHtml(
                "Recuperá el acceso a tu cuenta",
                string.Format(
                    "Hola {0},<br/><br/>Tu código de recuperación es: <b style=\"font-size:20px;\">{1}</b>" +
                    "<br/><br/>Este código vence en 15 minutos. Si no solicitaste este cambio, podés ignorar este correo.",
                    nombreDestinatario, codigo));

            return Enviar(destinatario, "Recuperá el acceso a tu cuenta StageUp", cuerpo);
        }

        public bool EnviarNotificacionCambioPassword(string destinatario, string nombreDestinatario)
        {
            string cuerpo = ConstruirCuerpoHtml(
                "Tu contraseña fue actualizada",
                string.Format(
                    "Hola {0},<br/><br/>Te confirmamos que la contraseña de tu cuenta de StageUp fue modificada correctamente. " +
                    "Si no fuiste vos, contactanos a la brevedad.",
                    nombreDestinatario));

            return Enviar(destinatario, "Tu contraseña de StageUp fue actualizada", cuerpo);
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

                using (var mensaje = new MailMessage())
                {
                    mensaje.From = new MailAddress(correoRemitente, nombreRemitente);
                    mensaje.To.Add(destinatario);
                    mensaje.Subject = asunto;
                    mensaje.Body = cuerpoHtml;
                    mensaje.IsBodyHtml = true;

                    using (var cliente = new SmtpClient(host, puerto))
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
                // Envío no crítico para el flujo: se informa a la BLL como
                // fallo (return false) y el caso de uso sigue su camino
                // alternativo correspondiente, sin romper la operación.
                return false;
            }
        }

        private static string ConstruirCuerpoHtml(string titulo, string mensajeHtml)
        {
            return
                "<div style=\"font-family:Arial,sans-serif;max-width:480px;margin:0 auto;\">" +
                "<h2 style=\"color:#222;\">" + titulo + "</h2>" +
                "<p style=\"color:#444;font-size:15px;line-height:1.5;\">" + mensajeHtml + "</p>" +
                "<hr style=\"border:none;border-top:1px solid #eee;margin:24px 0;\"/>" +
                "<p style=\"color:#999;font-size:12px;\">StageUp - Encontrá tu espacio, potenciá tu arte.</p>" +
                "</div>";
        }
    }
}
