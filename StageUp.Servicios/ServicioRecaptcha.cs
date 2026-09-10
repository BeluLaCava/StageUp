using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace StageUp.Servicios
{
    public enum EstadoValidacionRecaptcha
    {
        Valido,
        RespuestaVacia,
        RespuestaInvalida,
        ConfiguracionIncompleta,
        ServicioNoDisponible
    }

    public sealed class ResultadoValidacionRecaptcha
    {
        public EstadoValidacionRecaptcha Estado { get; private set; }

        public bool EsValido
        {
            get { return Estado == EstadoValidacionRecaptcha.Valido; }
        }

        private ResultadoValidacionRecaptcha(EstadoValidacionRecaptcha estado)
        {
            Estado = estado;
        }

        public static ResultadoValidacionRecaptcha Crear(EstadoValidacionRecaptcha estado)
        {
            return new ResultadoValidacionRecaptcha(estado);
        }
    }

    public class ServicioRecaptcha
    {
        private const string UrlVerificacion = "https://www.google.com/recaptcha/api/siteverify";
        private const int TiempoEsperaMilisegundos = 10000;

        public ResultadoValidacionRecaptcha Validar(string respuestaCaptcha)
        {
            if (string.IsNullOrWhiteSpace(respuestaCaptcha))
            {
                return ResultadoValidacionRecaptcha.Crear(EstadoValidacionRecaptcha.RespuestaVacia);
            }

            string claveSecreta = ObtenerClaveSecreta();
            if (string.IsNullOrWhiteSpace(claveSecreta))
            {
                return ResultadoValidacionRecaptcha.Crear(EstadoValidacionRecaptcha.ConfiguracionIncompleta);
            }

            try
            {
                byte[] contenido = Encoding.UTF8.GetBytes(
                    "secret=" + Uri.EscapeDataString(claveSecreta) +
                    "&response=" + Uri.EscapeDataString(respuestaCaptcha));

                var solicitud = (HttpWebRequest)WebRequest.Create(UrlVerificacion);
                solicitud.Method = "POST";
                solicitud.ContentType = "application/x-www-form-urlencoded";
                solicitud.ContentLength = contenido.Length;
                solicitud.Timeout = TiempoEsperaMilisegundos;
                solicitud.ReadWriteTimeout = TiempoEsperaMilisegundos;

                using (Stream cuerpoSolicitud = solicitud.GetRequestStream())
                {
                    cuerpoSolicitud.Write(contenido, 0, contenido.Length);
                }

                using (var respuesta = (HttpWebResponse)solicitud.GetResponse())
                using (Stream cuerpoRespuesta = respuesta.GetResponseStream())
                {
                    if (cuerpoRespuesta == null)
                    {
                        return ResultadoValidacionRecaptcha.Crear(EstadoValidacionRecaptcha.ServicioNoDisponible);
                    }

                    var serializador = new DataContractJsonSerializer(typeof(RespuestaGoogleRecaptcha));
                    var resultadoGoogle = serializador.ReadObject(cuerpoRespuesta) as RespuestaGoogleRecaptcha;

                    return ResultadoValidacionRecaptcha.Crear(
                        resultadoGoogle != null && resultadoGoogle.Exitoso
                            ? EstadoValidacionRecaptcha.Valido
                            : EstadoValidacionRecaptcha.RespuestaInvalida);
                }
            }
            catch (WebException)
            {
                return ResultadoValidacionRecaptcha.Crear(EstadoValidacionRecaptcha.ServicioNoDisponible);
            }
            catch (SerializationException)
            {
                return ResultadoValidacionRecaptcha.Crear(EstadoValidacionRecaptcha.ServicioNoDisponible);
            }
            catch (IOException)
            {
                return ResultadoValidacionRecaptcha.Crear(EstadoValidacionRecaptcha.ServicioNoDisponible);
            }
            catch (Exception)
            {
                return ResultadoValidacionRecaptcha.Crear(EstadoValidacionRecaptcha.ServicioNoDisponible);
            }
        }

        private static string ObtenerClaveSecreta()
        {
            string claveEntorno = Environment.GetEnvironmentVariable("STAGEUP_RECAPTCHA_SECRET_KEY");
            return string.IsNullOrWhiteSpace(claveEntorno)
                ? ConfigurationManager.AppSettings["RecaptchaSecretKey"]
                : claveEntorno;
        }

        [DataContract]
        private sealed class RespuestaGoogleRecaptcha
        {
            [DataMember(Name = "success")]
            public bool Exitoso { get; set; }
        }
    }
}
