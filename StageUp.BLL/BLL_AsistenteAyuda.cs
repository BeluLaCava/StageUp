using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-003-001 A4 a A7: asistente guiado del centro de ayuda. No usa IA:
    // responde con el contenido de ayuda activo marcado "usar en el
    // asistente" (CU-003-004). Se puede navegar por temas y preguntas o
    // escribir una consulta, que se compara con títulos, palabras clave y
    // respuestas. Si no reconoce la consulta ofrece registrar una solicitud
    // de soporte (CU-003-002) y la guarda para que el equipo cree contenido.
    public class BLL_AsistenteAyuda
    {
        public const string MensajeNoDisponible = "El asistente no está disponible en este momento. Podés consultar las preguntas frecuentes o registrar una solicitud de soporte.";
        private const int LongitudMinimaConsulta = 3;
        private const int LongitudMaximaConsulta = 200;
        private const double PuntajeMinimo = 3;
        private const double PuntajeSugerencia = 2;

        // Palabras que no aportan para reconocer la consulta.
        private static readonly HashSet<string> PalabrasVacias = new HashSet<string>
        {
            "a", "al", "algo", "como", "con", "cual", "cuales", "cuando", "de", "del", "donde", "el", "en", "es", "esta", "este",
            "hay", "hola", "la", "las", "lo", "los", "me", "mi", "mis", "necesito", "no", "o", "para", "pero", "por", "puedo",
            "que", "quiero", "se", "si", "sin", "su", "sus", "te", "tengo", "un", "una", "uno", "y", "ya", "yo", "hacer", "ayuda",
            "gracias", "porque", "puede", "tiene", "son", "ser", "le", "les"
        };

        private readonly MPP_Faq _mpp = new MPP_Faq();

        // Temas (categorías con al menos un contenido para el asistente).
        public ResultadoOperacion<List<CategoriaAyuda>> ObtenerTemas()
        {
            try
            {
                List<CategoriaAyuda> temas = _mpp.ListarCategorias().Where(c => c.Activa && c.CantidadAsistente > 0).ToList();
                return temas.Count == 0
                    ? ResultadoOperacion<List<CategoriaAyuda>>.Error(MensajeNoDisponible, "A7")
                    : ResultadoOperacion<List<CategoriaAyuda>>.Ok(temas);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<CategoriaAyuda>>.Error(MensajeNoDisponible, "A7");
            }
        }

        public ResultadoOperacion<List<Faq>> ObtenerPreguntas(int idCategoriaAyuda)
        {
            try
            {
                return ResultadoOperacion<List<Faq>>.Ok(ContenidoDelAsistente().Where(f => f.IdCategoriaAyuda == idCategoriaAyuda).ToList());
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<Faq>>.Error(MensajeNoDisponible, "A7");
            }
        }

        public ResultadoOperacion<Faq> ObtenerRespuesta(int idFaq)
        {
            try
            {
                Faq faq = ContenidoDelAsistente().FirstOrDefault(f => f.IdFaq == idFaq);
                return faq == null
                    ? ResultadoOperacion<Faq>.Error("Esa respuesta ya no está disponible. Elegí otro tema.")
                    : ResultadoOperacion<Faq>.Ok(faq);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<Faq>.Error(MensajeNoDisponible, "A7");
            }
        }

        // A4 pasos 4 a 8 y A5.
        public ResultadoOperacion<RespuestaAsistente> Consultar(string texto, int? idUsuarioExterno)
        {
            string limpio = (texto ?? string.Empty).Trim();
            if (limpio.Length < LongitudMinimaConsulta)
            {
                return ResultadoOperacion<RespuestaAsistente>.Error("Escribí tu consulta (por ejemplo: \"cómo cancelo una reserva\").");
            }

            if (limpio.Length > LongitudMaximaConsulta)
            {
                limpio = limpio.Substring(0, LongitudMaximaConsulta);
            }

            List<Faq> contenido;
            try
            {
                contenido = ContenidoDelAsistente();
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<RespuestaAsistente>.Error(MensajeNoDisponible, "A7");
            }

            string normalizada = BLL_Faq.Normalizar(limpio);
            List<string> palabras = Palabras(normalizada);
            List<KeyValuePair<Faq, double>> puntajes = contenido
                .Select(f => new KeyValuePair<Faq, double>(f, Puntuar(f, palabras)))
                .Where(p => p.Value > 0)
                .OrderByDescending(p => p.Value)
                .ToList();

            RespuestaAsistente respuesta = new RespuestaAsistente();
            if (puntajes.Count > 0 && puntajes[0].Value >= PuntajeMinimo)
            {
                respuesta.Reconocida = true;
                respuesta.Respuesta = puntajes[0].Key;
                respuesta.Sugerencias = puntajes.Skip(1).Where(p => p.Value >= PuntajeSugerencia).Take(2).Select(p => p.Key).ToList();
            }
            else
            {
                // A5: no se reconoce; igual se sugieren las más parecidas.
                respuesta.Sugerencias = puntajes.Take(3).Select(p => p.Key).ToList();
            }

            try
            {
                _mpp.RegistrarConsultaAsistente(limpio, normalizada.Length > 200 ? normalizada.Substring(0, 200) : normalizada,
                    respuesta.Reconocida ? respuesta.Respuesta.IdFaq : (int?)null, idUsuarioExterno);
            }
            catch (ErrorAccesoDatosException)
            {
                // Registrar la consulta es secundario.
            }

            return ResultadoOperacion<RespuestaAsistente>.Ok(respuesta);
        }

        private List<Faq> ContenidoDelAsistente()
        {
            return _mpp.ListarActivas().Where(f => f.UsarEnAsistente).ToList();
        }

        private static List<string> Palabras(string normalizada)
        {
            return normalizada.Split(' ')
                .Where(p => p.Length > 1 && !PalabrasVacias.Contains(p))
                .Distinct()
                .ToList();
        }

        // Palabras clave pesan 3, el título 2 y la respuesta 0,5. Las palabras
        // largas se comparan por la raíz (las primeras 5 letras), así
        // "cancelo", "cancelar" y "cancelacion" coinciden.
        private static double Puntuar(Faq faq, List<string> palabras)
        {
            if (palabras.Count == 0)
            {
                return 0;
            }

            List<string> clave = Palabras(BLL_Faq.Normalizar(faq.PalabrasClave));
            List<string> titulo = Palabras(BLL_Faq.Normalizar(faq.Pregunta));
            List<string> cuerpo = Palabras(BLL_Faq.Normalizar(faq.Respuesta));

            double puntaje = 0;
            foreach (string palabra in palabras)
            {
                if (Coincide(palabra, clave)) puntaje += 3;
                if (Coincide(palabra, titulo)) puntaje += 2;
                if (Coincide(palabra, cuerpo)) puntaje += 0.5;
            }

            return puntaje;
        }

        private static bool Coincide(string palabra, List<string> lista)
        {
            string raiz = Raiz(palabra);
            foreach (string otra in lista)
            {
                if (otra == palabra || (raiz.Length >= 5 && Raiz(otra) == raiz))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Raiz(string palabra)
        {
            return palabra.Length >= 6 ? palabra.Substring(0, 5) : palabra;
        }
    }
}
