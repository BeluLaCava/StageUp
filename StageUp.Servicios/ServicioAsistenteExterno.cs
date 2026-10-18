using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace StageUp.Servicios
{
    public enum EstadoAsistenteExterno
    {
        Respondida,
        SinRespuesta,
        NoConfigurado,
        TiempoAgotado,
        NoDisponible,
        // La consulta tenía datos privados (contraseña, código de seguridad,
        // número de tarjeta, clave o token): no se envió al proveedor.
        Bloqueada
    }

    public sealed class ResultadoAsistenteExterno
    {
        public EstadoAsistenteExterno Estado { get; private set; }
        public string Respuesta { get; private set; }

        public static ResultadoAsistenteExterno Crear(EstadoAsistenteExterno estado, string respuesta = null)
        {
            return new ResultadoAsistenteExterno { Estado = estado, Respuesta = respuesta };
        }
    }

    // Asistente automático de soporte por API (servicio externo de lenguaje).
    // Lo usa BLL_AsistenteAyuda solo cuando una consulta escrita no coincide
    // con el árbol del asistente.
    //   * API compatible con "chat completions" (OpenAI, Azure OpenAI o un
    //     servidor local como Ollama o LM Studio).
    //   * Configuración privada (AppSettings.private.config o variables de
    //     entorno): AsistenteApiHabilitado, AsistenteApiUrl, AsistenteApiClave,
    //     AsistenteApiModelo y AsistenteApiTiempoEsperaMs. Nada va en el código.
    //   * Tiempo de espera corto (por defecto 8 s). Si la API falla o tarda,
    //     se deja de llamar durante un minuto para no hacer esperar a los
    //     siguientes usuarios, y el asistente deriva a soporte.
    //   * Una consulta con datos privados (contraseñas, claves, PIN, código de
    //     seguridad/CVV, números de tarjeta válidos, tokens o claves de API)
    //     no se envía: el proveedor nunca la recibe y se ofrece soporte.
    //     Pedirle al modelo que no repita datos no alcanza, porque ya los
    //     habría recibido.
    //   * En el resto se ocultan correos, teléfonos, documentos y CBU antes
    //     de enviar. Nunca se envían datos de la cuenta del usuario.
    public class ServicioAsistenteExterno
    {
        private const int TiempoEsperaPorDefecto = 8000;
        private const int TiempoEsperaMinimo = 1000;
        private const int TiempoEsperaMaximo = 15000;
        private const int LongitudMaximaRespuesta = 900;
        private const string MarcaSinRespuesta = "NO_SE";
        private static readonly TimeSpan PausaTrasFalla = TimeSpan.FromMinutes(1);

        private static readonly object Bloqueo = new object();
        private static DateTime _pausadoHasta = DateTime.MinValue;

        private static readonly Regex Correo = new Regex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
        private static readonly Regex Tarjeta = new Regex(@"\b\d(?:[ \-]?\d){12,18}\b", RegexOptions.Compiled);
        private static readonly Regex Cbu = new Regex(@"\b\d{22}\b", RegexOptions.Compiled);
        private static readonly Regex Telefono = new Regex(@"(\+?\d[\s\-\.\(\)]?){8,}", RegexOptions.Compiled);
        private static readonly Regex Documento = new Regex(@"\b\d{1,2}\.?\d{3}\.?\d{3}\b", RegexOptions.Compiled);

        // Palabra que anuncia un secreto, seguida (o no) de un conector y del
        // valor: "mi contraseña es ...", "clave: ...", "cvv 123".
        private static readonly Regex Secreto = new Regex(
            @"\b(contrasena|contrasenia|password|passwd|pass|clave|pin|token|cvv2?|cvc|codigo de seguridad|codigo de verificacion|codigo verificador)\b" +
            @"(?:\s+(?:de\s+\w+\s+)?(?<conector>es|era|seria|sera|son|:|=)\s*|\s*(?<conector>[:=])\s*|\s+)(?<valor>[^\s,;]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Token = new Regex(@"\b(?:sk-[A-Za-z0-9_\-]{10,}|[A-Za-z0-9_\-]{24,})\b", RegexOptions.Compiled);

        // Palabras que pueden seguir a "contraseña" o "clave" en una consulta
        // común ("olvidé mi contraseña", "la clave no funciona").
        private static readonly HashSet<string> NoEsValor = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "no", "nueva", "nuevo", "anterior", "actual", "vieja", "incorrecta", "incorrecto", "invalida", "invalido", "correcta", "correcto",
            "y", "o", "que", "para", "por", "del", "de", "la", "el", "mi", "en", "con", "sin", "se", "me", "a", "al", "si", "funciona", "anda",
            "olvide", "olvidada", "cambiar", "cambio", "recuperar", "restablecer", "segura", "temporal", "vencida", "expiro", "caduco",
            "bloqueada", "bloqueado", "funcionaba", "sirve", "aparece", "pide", "llega", "llego", "valida", "valido", "porque", "cuando"
        };

        public bool EstaHabilitado
        {
            get
            {
                return string.Equals(Leer("AsistenteApiHabilitado", "STAGEUP_ASISTENTE_API_HABILITADO"), "true", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(Leer("AsistenteApiUrl", "STAGEUP_ASISTENTE_API_URL"));
            }
        }

        // contexto: contenido público de ayuda de StageUp para orientar la
        // respuesta (no incluye datos de usuarios).
        public ResultadoAsistenteExterno Consultar(string consulta, string contexto)
        {
            if (!EstaHabilitado)
            {
                return ResultadoAsistenteExterno.Crear(EstadoAsistenteExterno.NoConfigurado);
            }

            string url = Leer("AsistenteApiUrl", "STAGEUP_ASISTENTE_API_URL");
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                return ResultadoAsistenteExterno.Crear(EstadoAsistenteExterno.NoConfigurado);
            }

            lock (Bloqueo)
            {
                if (DateTime.UtcNow < _pausadoHasta)
                {
                    return ResultadoAsistenteExterno.Crear(EstadoAsistenteExterno.NoDisponible);
                }
            }

            // Con datos privados no se envía nada: el proveedor no los recibe.
            if (ContieneDatosPrivados(consulta))
            {
                return ResultadoAsistenteExterno.Crear(EstadoAsistenteExterno.Bloqueada);
            }

            string textoSeguro = OcultarDatosSensibles(consulta);
            int tiempoEspera = ObtenerTiempoEspera();

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                SolicitudChat cuerpo = new SolicitudChat
                {
                    Modelo = Leer("AsistenteApiModelo", "STAGEUP_ASISTENTE_API_MODELO") ?? "gpt-4o-mini",
                    Temperatura = 0.2,
                    MaximoTokens = 300,
                    Mensajes = new[]
                    {
                        new MensajeChat { Rol = "system", Contenido = InstruccionesSistema(contexto) },
                        new MensajeChat { Rol = "user", Contenido = textoSeguro }
                    }
                };

                byte[] contenido;
                using (MemoryStream memoria = new MemoryStream())
                {
                    new DataContractJsonSerializer(typeof(SolicitudChat)).WriteObject(memoria, cuerpo);
                    contenido = memoria.ToArray();
                }

                HttpWebRequest solicitud = (HttpWebRequest)WebRequest.Create(uri);
                solicitud.Method = "POST";
                solicitud.ContentType = "application/json; charset=utf-8";
                solicitud.Accept = "application/json";
                solicitud.ContentLength = contenido.Length;
                solicitud.Timeout = tiempoEspera;
                solicitud.ReadWriteTimeout = tiempoEspera;
                solicitud.KeepAlive = false;
                solicitud.UserAgent = "StageUp/1.0";

                string clave = Leer("AsistenteApiClave", "STAGEUP_ASISTENTE_API_CLAVE");
                if (!string.IsNullOrWhiteSpace(clave))
                {
                    solicitud.Headers[HttpRequestHeader.Authorization] = "Bearer " + clave.Trim();
                }

                using (Stream cuerpoSolicitud = solicitud.GetRequestStream())
                {
                    cuerpoSolicitud.Write(contenido, 0, contenido.Length);
                }

                using (HttpWebResponse respuesta = (HttpWebResponse)solicitud.GetResponse())
                using (Stream cuerpoRespuesta = respuesta.GetResponseStream())
                {
                    RespuestaChat resultado = cuerpoRespuesta == null
                        ? null
                        : new DataContractJsonSerializer(typeof(RespuestaChat)).ReadObject(cuerpoRespuesta) as RespuestaChat;

                    string texto = resultado != null && resultado.Opciones != null && resultado.Opciones.Length > 0 && resultado.Opciones[0].Mensaje != null
                        ? resultado.Opciones[0].Mensaje.Contenido
                        : null;

                    texto = LimpiarRespuesta(texto);
                    return texto == null
                        ? ResultadoAsistenteExterno.Crear(EstadoAsistenteExterno.SinRespuesta)
                        : ResultadoAsistenteExterno.Crear(EstadoAsistenteExterno.Respondida, texto);
                }
            }
            catch (WebException ex)
            {
                Pausar();
                return ResultadoAsistenteExterno.Crear(ex.Status == WebExceptionStatus.Timeout
                    ? EstadoAsistenteExterno.TiempoAgotado
                    : EstadoAsistenteExterno.NoDisponible);
            }
            catch (Exception ex) when (ex is SerializationException || ex is IOException || ex is InvalidOperationException ||
                                       ex is NotSupportedException || ex is UriFormatException)
            {
                Pausar();
                return ResultadoAsistenteExterno.Crear(EstadoAsistenteExterno.NoDisponible);
            }
        }

        // true si la consulta trae un secreto que no puede salir de StageUp.
        public static bool ContieneDatosPrivados(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            string normalizado = SinTildes(texto);
            foreach (Match coincidencia in Secreto.Matches(normalizado))
            {
                string valor = coincidencia.Groups["valor"].Value.Trim('.', '"', '\'', '(', ')');
                bool conConector = coincidencia.Groups["conector"].Success;
                bool pareceValor = Regex.IsMatch(valor, @"\d") || Regex.IsMatch(valor, @"[^\p{L}\p{N}]") ||
                    (Regex.IsMatch(valor, @"\p{Lu}") && Regex.IsMatch(valor, @"\p{Ll}") && valor.Length >= 6);
                if (valor.Length > 0 && !NoEsValor.Contains(valor) && (conConector || pareceValor))
                {
                    return true;
                }
            }

            foreach (Match posible in Tarjeta.Matches(texto))
            {
                string digitos = Regex.Replace(posible.Value, @"\D", string.Empty);
                if (digitos.Length >= 13 && digitos.Length <= 19 && CumpleLuhn(digitos))
                {
                    return true;
                }
            }

            foreach (Match posible in Token.Matches(texto))
            {
                if (posible.Value.StartsWith("sk-", StringComparison.Ordinal) ||
                    (Regex.IsMatch(posible.Value, @"\d") && Regex.IsMatch(posible.Value, @"[A-Za-z]")))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CumpleLuhn(string digitos)
        {
            int suma = 0;
            bool doble = false;
            for (int i = digitos.Length - 1; i >= 0; i--)
            {
                int d = digitos[i] - '0';
                if (doble)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                suma += d;
                doble = !doble;
            }
            return suma % 10 == 0;
        }

        private static string SinTildes(string texto)
        {
            string descompuesto = texto.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder resultado = new System.Text.StringBuilder(descompuesto.Length);
            foreach (char c in descompuesto)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(c == 'ñ' || c == 'Ñ' ? 'n' : c);
                }
            }
            return resultado.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // Oculta datos personales o de pago que el usuario haya escrito.
        public static string OcultarDatosSensibles(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }

            string resultado = Correo.Replace(texto, "[correo oculto]");
            resultado = Cbu.Replace(resultado, "[dato oculto]");
            resultado = Tarjeta.Replace(resultado, "[número oculto]");
            resultado = Telefono.Replace(resultado, "[número oculto]");
            resultado = Documento.Replace(resultado, "[número oculto]");
            return resultado;
        }

        private static string InstruccionesSistema(string contexto)
        {
            StringBuilder texto = new StringBuilder();
            texto.Append("Sos el asistente de ayuda de StageUp, una plataforma argentina para reservar espacios artísticos ");
            texto.Append("(salas de ensayo, estudios, teatros). Respondé en español rioplatense, en 2 a 5 oraciones, ");
            texto.Append("solo sobre el uso de StageUp: reservas, pagos, cuenta corriente, espacios, gestores y la cuenta del usuario. ");
            texto.Append("No pidas ni repitas datos personales, contraseñas, tarjetas ni documentos. No inventes funciones ni montos. ");
            texto.Append("Si la consulta no es sobre StageUp o no sabés la respuesta con seguridad, respondé exactamente: ");
            texto.Append(MarcaSinRespuesta).Append(". ");
            texto.Append("Usá esta información de ayuda de StageUp:\n");
            texto.Append(string.IsNullOrWhiteSpace(contexto) ? "(sin información adicional)" : contexto);
            return texto.ToString();
        }

        private static string LimpiarRespuesta(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            string limpio = Regex.Replace(texto, "<[^>]*>", string.Empty).Trim();
            if (limpio.Length == 0 || limpio.IndexOf(MarcaSinRespuesta, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }

            limpio = OcultarDatosSensibles(limpio);
            return limpio.Length <= LongitudMaximaRespuesta ? limpio : limpio.Substring(0, LongitudMaximaRespuesta - 3).TrimEnd() + "...";
        }

        private static void Pausar()
        {
            lock (Bloqueo)
            {
                _pausadoHasta = DateTime.UtcNow.Add(PausaTrasFalla);
            }
        }

        // Para pruebas: vuelve a habilitar las llamadas después de una falla.
        public static void ReiniciarPausa()
        {
            lock (Bloqueo)
            {
                _pausadoHasta = DateTime.MinValue;
            }
        }

        private static int ObtenerTiempoEspera()
        {
            int valor;
            if (!int.TryParse(Leer("AsistenteApiTiempoEsperaMs", "STAGEUP_ASISTENTE_API_TIEMPO_MS"), NumberStyles.Integer, CultureInfo.InvariantCulture, out valor))
            {
                return TiempoEsperaPorDefecto;
            }

            return Math.Max(TiempoEsperaMinimo, Math.Min(TiempoEsperaMaximo, valor));
        }

        private static string Leer(string clave, string variableEntorno)
        {
            string entorno = Environment.GetEnvironmentVariable(variableEntorno);
            string valor = string.IsNullOrWhiteSpace(entorno) ? ConfigurationManager.AppSettings[clave] : entorno;
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        [DataContract]
        private sealed class SolicitudChat
        {
            [DataMember(Name = "model", Order = 1)]
            public string Modelo { get; set; }

            [DataMember(Name = "messages", Order = 2)]
            public MensajeChat[] Mensajes { get; set; }

            [DataMember(Name = "temperature", Order = 3)]
            public double Temperatura { get; set; }

            [DataMember(Name = "max_tokens", Order = 4)]
            public int MaximoTokens { get; set; }
        }

        [DataContract]
        private sealed class MensajeChat
        {
            [DataMember(Name = "role", Order = 1)]
            public string Rol { get; set; }

            [DataMember(Name = "content", Order = 2)]
            public string Contenido { get; set; }
        }

        [DataContract]
        private sealed class RespuestaChat
        {
            [DataMember(Name = "choices")]
            public OpcionChat[] Opciones { get; set; }
        }

        [DataContract]
        private sealed class OpcionChat
        {
            [DataMember(Name = "message")]
            public MensajeChat Mensaje { get; set; }
        }
    }
}
