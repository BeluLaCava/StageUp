using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // CU-003-001 A4 a A7: asistente guiado del centro de ayuda (sin IA).
    //
    // Corrección de María: el asistente es el segundo nivel de ayuda y no
    // repite las preguntas frecuentes. Funciona como un árbol de decisión con
    // contenido propio (tablas NodoAsistente y AccionNodoAsistente, script
    // 70): el usuario elige un tema, después un subtema, responde preguntas
    // simples y llega a una respuesta con acciones (ir a la pantalla que
    // corresponde, ver otra respuesta o registrar una solicitud de soporte).
    //
    // También se puede escribir la consulta (A4 paso 4): se compara con las
    // opciones y palabras clave del árbol y, si no coincide con ninguna, con
    // las preguntas frecuentes que se marcaron para el asistente en
    // "Contenido de ayuda" (CU-003-004). Si tampoco hay coincidencia y está
    // configurado el asistente automático por API (ServicioAsistenteExterno),
    // se le consulta con el contenido público de ayuda como referencia y sin
    // datos personales; si responde, se muestra como respuesta automática.
    // Si no hay respuesta, o la API falla o tarda, se ofrece registrar una
    // solicitud de soporte (A5). La consulta queda guardada para que el
    // equipo cree contenido.
    public class BLL_AsistenteAyuda
    {
        public const string MensajeNoDisponible = "El asistente no está disponible en este momento. Podés consultar las preguntas frecuentes o registrar una solicitud de soporte.";
        public const string CodigoInicio = "inicio";
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

        private readonly MPP_NodoAsistente _mppNodos = new MPP_NodoAsistente();
        private readonly MPP_Faq _mppFaq = new MPP_Faq();
        private readonly ServicioAsistenteExterno _asistenteExterno = new ServicioAsistenteExterno();
        private const int LongitudMaximaContexto = 6000;

        // Paso del asistente con sus opciones y acciones. Sin código, el inicio.
        public ResultadoOperacion<NodoAsistente> ObtenerNodo(string codigo)
        {
            Dictionary<string, NodoAsistente> arbol;
            try
            {
                arbol = CargarArbol();
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<NodoAsistente>.Error(MensajeNoDisponible, "A7");
            }

            NodoAsistente inicio;
            if (!arbol.TryGetValue(CodigoInicio, out inicio) || inicio.Opciones.Count == 0)
            {
                return ResultadoOperacion<NodoAsistente>.Error(MensajeNoDisponible, "A7");
            }

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return ResultadoOperacion<NodoAsistente>.Ok(inicio);
            }

            NodoAsistente nodo;
            return arbol.TryGetValue(codigo.Trim(), out nodo)
                ? ResultadoOperacion<NodoAsistente>.Ok(nodo)
                : ResultadoOperacion<NodoAsistente>.Error("Esa opción ya no está disponible. Volvé al inicio para elegir otra.");
        }

        // A4 pasos 4 a 8 y A5: consulta escrita.
        public ResultadoOperacion<RespuestaAsistente> Consultar(string texto, int? idUsuarioExterno)
        {
            string limpio = (texto ?? string.Empty).Trim();
            if (limpio.Length < LongitudMinimaConsulta)
            {
                return ResultadoOperacion<RespuestaAsistente>.Error("Escribí tu consulta (por ejemplo: \"cómo cancelo una reserva\") o elegí una de las opciones.");
            }

            if (limpio.Length > LongitudMaximaConsulta)
            {
                limpio = limpio.Substring(0, LongitudMaximaConsulta);
            }

            Dictionary<string, NodoAsistente> arbol;
            try
            {
                arbol = CargarArbol();
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<RespuestaAsistente>.Error(MensajeNoDisponible, "A7");
            }

            if (!arbol.ContainsKey(CodigoInicio))
            {
                return ResultadoOperacion<RespuestaAsistente>.Error(MensajeNoDisponible, "A7");
            }

            string normalizada = BLL_Faq.Normalizar(limpio);
            List<string> palabras = Palabras(normalizada);

            List<KeyValuePair<NodoAsistente, double>> puntajes = arbol.Values
                .Where(n => n.Codigo != CodigoInicio)
                .Select(n => new KeyValuePair<NodoAsistente, double>(n, PuntuarNodo(n, palabras)))
                .Where(p => p.Value > 0)
                .OrderByDescending(p => p.Value)
                .ThenBy(p => p.Key.Recorrido.Length)
                .ToList();

            RespuestaAsistente respuesta = new RespuestaAsistente();
            if (puntajes.Count > 0 && puntajes[0].Value >= PuntajeMinimo)
            {
                respuesta.Reconocida = true;
                respuesta.Nodo = puntajes[0].Key;
                respuesta.Sugerencias = puntajes.Skip(1).Where(p => p.Value >= PuntajeSugerencia).Take(2).Select(p => p.Key).ToList();
            }
            else
            {
                // Sin coincidencia en el árbol: preguntas frecuentes marcadas
                // para el asistente (CU-003-004), si las hay.
                respuesta.PreguntaFrecuente = BuscarPreguntaFrecuente(palabras);
                respuesta.Reconocida = respuesta.PreguntaFrecuente != null;
                respuesta.Sugerencias = puntajes.Take(3).Select(p => p.Key).ToList();
            }

            // Sin contenido propio: asistente automático por API, si está
            // configurado. Su respuesta no cuenta como "reconocida" para
            // la lista de consultas sin respuesta (CU-003-004 A15).
            string resultadoApi = null;
            if (!respuesta.Reconocida && _asistenteExterno.EstaHabilitado)
            {
                ResultadoAsistenteExterno externo = _asistenteExterno.Consultar(limpio, ArmarContexto(arbol));
                resultadoApi = externo.Estado.ToString();
                if (externo.Estado == EstadoAsistenteExterno.Respondida)
                {
                    respuesta.RespuestaAutomatica = externo.Respuesta;
                }
                else if (externo.Estado == EstadoAsistenteExterno.TiempoAgotado || externo.Estado == EstadoAsistenteExterno.NoDisponible)
                {
                    respuesta.AsistenteAutomaticoNoDisponible = true;
                }
            }

            try
            {
                _mppFaq.RegistrarConsultaAsistente(limpio, normalizada.Length > 200 ? normalizada.Substring(0, 200) : normalizada,
                    respuesta.Nodo == null ? (int?)null : respuesta.Nodo.IdNodoAsistente,
                    respuesta.PreguntaFrecuente == null ? (int?)null : respuesta.PreguntaFrecuente.IdFaq,
                    idUsuarioExterno, resultadoApi);
            }
            catch (ErrorAccesoDatosException)
            {
                // Registrar la consulta es secundario.
            }

            return ResultadoOperacion<RespuestaAsistente>.Ok(respuesta);
        }

        // Contenido público de ayuda (respuestas del árbol) que se le pasa al
        // asistente automático como referencia. No incluye datos de usuarios.
        private static string ArmarContexto(Dictionary<string, NodoAsistente> arbol)
        {
            StringBuilder texto = new StringBuilder();
            foreach (NodoAsistente nodo in arbol.Values.Where(n => n.EsRespuesta).OrderBy(n => n.Recorrido))
            {
                string linea = "- " + nodo.Recorrido + ": " + nodo.Mensaje + "\n";
                if (texto.Length + linea.Length > LongitudMaximaContexto)
                {
                    break;
                }

                texto.Append(linea);
            }

            return texto.ToString();
        }

        // Árbol activo indexado por código. Solo quedan los pasos alcanzables
        // desde el inicio (un paso cuyo padre está inactivo no se muestra).
        private Dictionary<string, NodoAsistente> CargarArbol()
        {
            List<NodoAsistente> nodos = _mppNodos.ListarActivos();
            List<AccionNodoAsistente> acciones = _mppNodos.ListarAccionesActivas();

            Dictionary<int, NodoAsistente> porId = nodos.ToDictionary(n => n.IdNodoAsistente);
            NodoAsistente inicio = nodos.FirstOrDefault(n => n.Codigo == CodigoInicio && !n.IdNodoPadre.HasValue);
            Dictionary<string, NodoAsistente> arbol = new Dictionary<string, NodoAsistente>(StringComparer.OrdinalIgnoreCase);
            if (inicio == null)
            {
                return arbol;
            }

            Dictionary<string, string> valores = ValoresVariables();
            foreach (NodoAsistente nodo in nodos)
            {
                nodo.Mensaje = Reemplazar(nodo.Mensaje, valores);
                NodoAsistente padre;
                if (nodo.IdNodoPadre.HasValue && porId.TryGetValue(nodo.IdNodoPadre.Value, out padre))
                {
                    padre.Opciones.Add(nodo);
                }
            }

            // Recorrido en anchura desde el inicio.
            inicio.Recorrido = string.Empty;
            Queue<NodoAsistente> pendientes = new Queue<NodoAsistente>();
            pendientes.Enqueue(inicio);
            while (pendientes.Count > 0)
            {
                NodoAsistente actual = pendientes.Dequeue();
                if (arbol.ContainsKey(actual.Codigo))
                {
                    continue;
                }

                arbol[actual.Codigo] = actual;
                actual.Opciones = actual.Opciones.OrderBy(o => o.Orden).ThenBy(o => o.IdNodoAsistente).ToList();
                foreach (NodoAsistente hijo in actual.Opciones)
                {
                    hijo.Recorrido = actual.Recorrido.Length == 0 ? hijo.TextoOpcion : actual.Recorrido + " > " + hijo.TextoOpcion;
                    pendientes.Enqueue(hijo);
                }
            }

            foreach (AccionNodoAsistente accion in acciones)
            {
                NodoAsistente nodo;
                if (!porId.TryGetValue(accion.IdNodoAsistente, out nodo) || !arbol.ContainsKey(nodo.Codigo))
                {
                    continue;
                }

                // Una acción que lleva a un paso inactivo no se ofrece.
                if (accion.TipoAccion == AccionNodoAsistente.TipoNodo && (accion.Destino == null || !arbol.ContainsKey(accion.Destino)))
                {
                    continue;
                }

                nodo.Acciones.Add(accion);
            }

            return arbol;
        }

        // Datos que dependen de parámetros vigentes (CU-002-004).
        private static Dictionary<string, string> ValoresVariables()
        {
            Dictionary<string, string> valores = new Dictionary<string, string>();
            try
            {
                valores["{PoliticaCancelacion}"] = BLL_Reserva.DescribirPoliticaCancelacion(new BLL_Reserva().ObtenerPoliticaCancelacion());
            }
            catch (Exception)
            {
                valores["{PoliticaCancelacion}"] = "Una solicitud pendiente se cancela sin cargo; en una reserva aceptada el cargo depende de la anticipación.";
            }

            try
            {
                valores["{HorasLimitePago}"] = new BLL_ParametroPlataforma().ObtenerEntero(BLL_ParametroPlataforma.HorasLimitePago).ToString();
            }
            catch (Exception)
            {
                valores["{HorasLimitePago}"] = "48";
            }

            return valores;
        }

        private static string Reemplazar(string texto, Dictionary<string, string> valores)
        {
            if (string.IsNullOrEmpty(texto) || texto.IndexOf('{') < 0)
            {
                return texto;
            }

            foreach (KeyValuePair<string, string> valor in valores)
            {
                texto = texto.Replace(valor.Key, valor.Value);
            }

            return texto;
        }

        private Faq BuscarPreguntaFrecuente(List<string> palabras)
        {
            try
            {
                return _mppFaq.ListarActivas()
                    .Where(f => f.UsarEnAsistente)
                    .Select(f => new KeyValuePair<Faq, double>(f, Puntuar(palabras, f.PalabrasClave, f.Pregunta, f.Respuesta)))
                    .Where(p => p.Value >= PuntajeMinimo)
                    .OrderByDescending(p => p.Value)
                    .Select(p => p.Key)
                    .FirstOrDefault();
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        private static List<string> Palabras(string normalizada)
        {
            return normalizada.Split(' ')
                .Where(p => p.Length > 1 && !PalabrasVacias.Contains(p))
                .Distinct()
                .ToList();
        }

        // Una respuesta pesa un poco más que una pregunta intermedia, para
        // llevar al usuario lo más cerca posible de la solución.
        private static double PuntuarNodo(NodoAsistente nodo, List<string> palabras)
        {
            double puntaje = Puntuar(palabras, nodo.PalabrasClave, nodo.TextoOpcion, nodo.Mensaje);
            return puntaje > 0 && nodo.EsRespuesta ? puntaje + 0.25 : puntaje;
        }

        // Palabras clave pesan 3, el título (opción o pregunta) 2 y el texto
        // 0,5. Las palabras largas se comparan por la raíz (las primeras 5
        // letras), así "cancelo", "cancelar" y "cancelacion" coinciden.
        private static double Puntuar(List<string> palabras, string clave, string titulo, string cuerpo)
        {
            if (palabras.Count == 0)
            {
                return 0;
            }

            List<string> listaClave = Palabras(BLL_Faq.Normalizar(clave));
            List<string> listaTitulo = Palabras(BLL_Faq.Normalizar(titulo));
            List<string> listaCuerpo = Palabras(BLL_Faq.Normalizar(cuerpo));

            double puntaje = 0;
            foreach (string palabra in palabras)
            {
                if (Coincide(palabra, listaClave)) puntaje += 3;
                if (Coincide(palabra, listaTitulo)) puntaje += 2;
                if (Coincide(palabra, listaCuerpo)) puntaje += 0.5;
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
