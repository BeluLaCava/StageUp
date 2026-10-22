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


















    public class BLL_AsistenteAyuda
    {
        public const string MensajeNoDisponible = "El asistente no está disponible en este momento. Podés consultar las preguntas básicas del centro de ayuda o registrar una solicitud de soporte.";
        public const string CodigoInicio = "inicio";
        private const int LongitudMinimaConsulta = 3;
        private const int LongitudMaximaConsulta = 200;
        private const double PuntajeMinimo = 3;
        private const double PuntajeSugerencia = 2;


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
            bool datosPrivados = ServicioAsistenteExterno.ContieneDatosPrivados(limpio);

            List<KeyValuePair<NodoAsistente, double>> puntajes = arbol.Values
                .Where(n => n.Codigo != CodigoInicio)
                .Select(n => new KeyValuePair<NodoAsistente, double>(n, PuntuarNodo(n, palabras)))
                .Where(p => p.Value > 0)
                .OrderByDescending(p => p.Value)
                .ThenBy(p => p.Key.Recorrido.Length)
                .ToList();

            RespuestaAsistente respuesta = new RespuestaAsistente { ConDatosPrivados = datosPrivados };
            if (puntajes.Count > 0 && puntajes[0].Value >= PuntajeMinimo)
            {
                respuesta.Reconocida = true;
                respuesta.Nodo = puntajes[0].Key;
                respuesta.Sugerencias = puntajes.Skip(1).Where(p => p.Value >= PuntajeSugerencia).Take(2).Select(p => p.Key).ToList();
            }
            else
            {
                respuesta.Reconocida = false;
                respuesta.Sugerencias = puntajes.Take(3).Select(p => p.Key).ToList();
            }




            string resultadoApi = null;
            if (!respuesta.Reconocida && !datosPrivados && _asistenteExterno.EstaHabilitado)
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




            string textoGuardado = datosPrivados
                ? "[Consulta con datos privados: el texto no se guardó]"
                : ServicioAsistenteExterno.OcultarDatosSensibles(limpio);
            string normalizadaGuardada = BLL_Faq.Normalizar(textoGuardado);
            if (datosPrivados)
            {
                resultadoApi = EstadoAsistenteExterno.Bloqueada.ToString();
            }

            try
            {
                _mppFaq.RegistrarConsultaAsistente(textoGuardado.Length > 200 ? textoGuardado.Substring(0, 200) : textoGuardado,
                    normalizadaGuardada.Length > 200 ? normalizadaGuardada.Substring(0, 200) : normalizadaGuardada,
                    respuesta.Nodo == null ? (int?)null : respuesta.Nodo.IdNodoAsistente,
                    null,
                    idUsuarioExterno, resultadoApi);
            }
            catch (ErrorAccesoDatosException)
            {

            }

            return ResultadoOperacion<RespuestaAsistente>.Ok(respuesta);
        }



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


                if (accion.TipoAccion == AccionNodoAsistente.TipoNodo && (accion.Destino == null || !arbol.ContainsKey(accion.Destino)))
                {
                    continue;
                }

                nodo.Acciones.Add(accion);
            }

            return arbol;
        }


        private static Dictionary<string, string> ValoresVariables()
        {
            Dictionary<string, string> valores = new Dictionary<string, string>();
            try
            {
                valores["{PoliticaCancelacion}"] = BLL_Reserva.DescribirPoliticaCancelacion(new BLL_Reserva().ObtenerPoliticaCancelacion());
            }
            catch (Exception)
            {
                valores["{PoliticaCancelacion}"] = "Una solicitud pendiente, o una reserva aceptada que todavía no pagaste, se cancela sin cargo; en una reserva pagada el cargo depende de la anticipación.";
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

        private static List<string> Palabras(string normalizada)
        {
            return normalizada.Split(' ')
                .Where(p => p.Length > 1 && !PalabrasVacias.Contains(p))
                .Distinct()
                .ToList();
        }



        private static double PuntuarNodo(NodoAsistente nodo, List<string> palabras)
        {
            double puntaje = Puntuar(palabras, nodo.PalabrasClave, nodo.TextoOpcion, nodo.Mensaje);
            return puntaje > 0 && nodo.EsRespuesta ? puntaje + 0.25 : puntaje;
        }




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
