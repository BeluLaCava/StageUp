using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Ítem 1 del profesor: preguntas y respuestas públicas sobre los espacios
    // (estilo Mercado Libre).
    //   * Cualquier visitante ve las preguntas y respuestas del espacio.
    //   * Para preguntar hay que iniciar sesión; el gestor no pregunta en su
    //     propio espacio.
    //   * Solo el gestor dueño del espacio responde; se le avisa de cada
    //     pregunta nueva y a quien preguntó se le avisa cuando le responden.
    //   * Moderación: el gestor puede ocultar una pregunta indicando el
    //     motivo; quien preguntó puede retirarla mientras siga sin respuesta.
    //     Nada se borra (baja lógica).
    //   * Como todo queda público, no se aceptan correos, teléfonos ni enlaces.
    public class BLL_PreguntaEspacio
    {
        public const int LongitudMinimaPregunta = 10;
        public const int LongitudMaximaPregunta = 500;
        public const int LongitudMinimaRespuesta = 2;
        public const int LongitudMaximaRespuesta = 1000;
        public const int LongitudMinimaMotivo = 5;
        public const int LongitudMaximaMotivo = 300;
        public const int PendientesMaximasPorUsuario = 3;
        public const int CantidadEnDetalle = 10;

        private const string TipoEntidadBitacora = "PreguntaEspacio";
        private const string MensajeError = "No pudimos completar la operación en este momento. Intentá nuevamente.";

        private static readonly Regex Correo = new Regex(@"[A-Za-z0-9._%+\-]+\s*(@|\(at\)|\[at\])\s*[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Enlace = new Regex(@"(https?://|www\.)\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Telefono = new Regex(@"(\+?\d[\s\-\.\(\)]?){8,}", RegexOptions.Compiled);

        private readonly MPP_PreguntaEspacio _mpp = new MPP_PreguntaEspacio();
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();
        private readonly BLL_Notificacion _bllNotificacion = new BLL_Notificacion();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // ------------------------------------------------------------------
        // Detalle público del espacio
        // ------------------------------------------------------------------

        public ResultadoOperacion<PreguntasDeEspacio> ListarPublicas(int idEspacioArtistico, bool todas)
        {
            try
            {
                EspacioArtistico espacio = new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico };
                return ResultadoOperacion<PreguntasDeEspacio>.Ok(new PreguntasDeEspacio
                {
                    Preguntas = _mpp.ListarPublicas(espacio, todas ? (int?)null : CantidadEnDetalle),
                    Total = _mpp.ContarPublicas(espacio)
                });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<PreguntasDeEspacio>.Error("No pudimos cargar las preguntas del espacio en este momento.");
            }
        }

        public ResultadoOperacion<int> Preguntar(int idEspacioArtistico, int? idUsuarioExterno, string texto)
        {
            if (!idUsuarioExterno.HasValue)
            {
                return ResultadoOperacion<int>.Error("Iniciá sesión para hacer una pregunta sobre el espacio.");
            }

            string pregunta = Limpiar(texto);
            ResultadoOperacion validacion = ValidarTexto(pregunta, LongitudMinimaPregunta, LongitudMaximaPregunta, "la pregunta");
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<int>.Error(validacion.Mensaje);
            }

            try
            {
                EspacioArtistico espacio = _bllEspacio.ObtenerDetallePublicado(idEspacioArtistico);
                if (espacio == null)
                {
                    return ResultadoOperacion<int>.Error("Este espacio no está publicado: no se pueden hacer preguntas.");
                }

                if (espacio.IdUsuarioGestor == idUsuarioExterno.Value)
                {
                    return ResultadoOperacion<int>.Error("Sos el gestor de este espacio: no podés preguntarte a vos mismo. Respondé las preguntas desde \"Preguntas recibidas\".");
                }

                List<PreguntaEspacio> pendientes = _mpp.ListarPendientesDeUsuario(espacio, new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno.Value });
                string normalizada = BLL_Faq.Normalizar(pregunta);
                if (pendientes.Any(p => BLL_Faq.Normalizar(p.Pregunta) == normalizada))
                {
                    return ResultadoOperacion<int>.Error("Ya hiciste esta misma pregunta y todavía no la respondieron.");
                }

                if (pendientes.Count >= PendientesMaximasPorUsuario)
                {
                    return ResultadoOperacion<int>.Error("Tenés " + pendientes.Count + " preguntas sin responder en este espacio. Esperá la respuesta del gestor antes de hacer otra.");
                }

                int idPregunta = _mpp.Insertar(new PreguntaEspacio
                {
                    IdEspacioArtistico = idEspacioArtistico,
                    IdUsuarioExterno = idUsuarioExterno.Value,
                    Pregunta = pregunta
                });

                Bitacora(idUsuarioExterno.Value, "ALTA", idPregunta,
                    "Pregunta pública sobre el espacio \"" + espacio.NombreEspacio + "\": \"" + Recortar(pregunta, 120) + "\".");

                _bllNotificacion.Notificar(espacio.IdUsuarioGestor, TipoNotificacion.PreguntaEspacioRecibida,
                    "Te preguntaron sobre \"" + espacio.NombreEspacio + "\": \"" + Recortar(pregunta, 120) + "\"",
                    "~/PreguntasRecibidas.aspx?espacio=" + idEspacioArtistico);

                return ResultadoOperacion<int>.Ok(idPregunta, "Tu pregunta se publicó. Te avisamos cuando el gestor responda.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<int>.Error(MensajeError);
            }
        }

        // Quien preguntó la retira mientras siga sin respuesta.
        public ResultadoOperacion Retirar(int idPreguntaEspacio, int idUsuarioExterno)
        {
            try
            {
                PreguntaEspacio pregunta = _mpp.ObtenerPorId(new PreguntaEspacio { IdPreguntaEspacio = idPreguntaEspacio });
                if (pregunta == null || pregunta.IdUsuarioExterno != idUsuarioExterno)
                {
                    return ResultadoOperacion.Error("No se encontró la pregunta.");
                }

                if (!pregunta.EstaPendiente)
                {
                    return ResultadoOperacion.Error("La pregunta ya fue respondida o dada de baja: no se puede retirar.");
                }

                pregunta.Estado = PreguntaEspacio.EstadoEliminada;
                pregunta.MotivoOcultamiento = null;
                if (!_mpp.DarDeBaja(pregunta, PreguntaEspacio.EstadoPendiente, idUsuarioExterno))
                {
                    return ResultadoOperacion.Error("El gestor respondió la pregunta mientras tanto: ya no se puede retirar.");
                }

                Bitacora(idUsuarioExterno, "BAJA", idPreguntaEspacio,
                    "Pregunta retirada por su autor en el espacio \"" + pregunta.NombreEspacio + "\".");
                return ResultadoOperacion.Ok("Retiraste tu pregunta.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error(MensajeError);
            }
        }

        // ------------------------------------------------------------------
        // Gestor del espacio
        // ------------------------------------------------------------------

        public ResultadoOperacion Responder(int idPreguntaEspacio, int idUsuarioGestor, string texto)
        {
            string respuesta = Limpiar(texto);
            ResultadoOperacion validacion = ValidarTexto(respuesta, LongitudMinimaRespuesta, LongitudMaximaRespuesta, "la respuesta");
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            try
            {
                PreguntaEspacio pregunta = _mpp.ObtenerPorId(new PreguntaEspacio { IdPreguntaEspacio = idPreguntaEspacio });
                if (pregunta == null || pregunta.IdUsuarioGestorEspacio != idUsuarioGestor)
                {
                    return ResultadoOperacion.Error("Solo el gestor del espacio puede responder esta pregunta.");
                }

                if (!pregunta.EstaPendiente)
                {
                    return ResultadoOperacion.Error(pregunta.EstaRespondida
                        ? "Esta pregunta ya tiene respuesta."
                        : "Esta pregunta fue dada de baja: no se puede responder.");
                }

                pregunta.Respuesta = respuesta;
                if (!_mpp.Responder(pregunta, idUsuarioGestor))
                {
                    return ResultadoOperacion.Error("La pregunta cambió mientras la respondías (puede que la hayan retirado). Actualizá la página.");
                }

                Bitacora(idUsuarioGestor, "RESPUESTA", idPreguntaEspacio,
                    "Respuesta del gestor a una pregunta sobre el espacio \"" + pregunta.NombreEspacio + "\".");

                _bllNotificacion.Notificar(pregunta.IdUsuarioExterno, TipoNotificacion.PreguntaEspacioRespondida,
                    "El gestor de \"" + pregunta.NombreEspacio + "\" respondió tu pregunta: \"" + Recortar(respuesta, 120) + "\"",
                    "~/Explorar/DetalleEspacio.aspx?id=" + pregunta.IdEspacioArtistico + "&pregunta=" + idPreguntaEspacio);

                return ResultadoOperacion.Ok("Tu respuesta se publicó y le avisamos a quien preguntó.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error(MensajeError);
            }
        }

        // Moderación: el gestor oculta una pregunta indicando el motivo.
        public ResultadoOperacion Ocultar(int idPreguntaEspacio, int idUsuarioGestor, string motivo)
        {
            string motivoLimpio = Limpiar(motivo);
            if (motivoLimpio == null || motivoLimpio.Length < LongitudMinimaMotivo)
            {
                return ResultadoOperacion.Error("Indicá el motivo por el que ocultás la pregunta (al menos " + LongitudMinimaMotivo + " caracteres).");
            }

            if (motivoLimpio.Length > LongitudMaximaMotivo)
            {
                return ResultadoOperacion.Error("El motivo no puede superar los " + LongitudMaximaMotivo + " caracteres.");
            }

            try
            {
                PreguntaEspacio pregunta = _mpp.ObtenerPorId(new PreguntaEspacio { IdPreguntaEspacio = idPreguntaEspacio });
                if (pregunta == null || pregunta.IdUsuarioGestorEspacio != idUsuarioGestor)
                {
                    return ResultadoOperacion.Error("Solo el gestor del espacio puede ocultar esta pregunta.");
                }

                if (!pregunta.EstaPendiente && !pregunta.EstaRespondida)
                {
                    return ResultadoOperacion.Error("Esta pregunta ya no está publicada.");
                }

                string estadoAnterior = pregunta.Estado;
                pregunta.Estado = PreguntaEspacio.EstadoOculta;
                pregunta.MotivoOcultamiento = motivoLimpio;
                if (!_mpp.DarDeBaja(pregunta, estadoAnterior, idUsuarioGestor))
                {
                    return ResultadoOperacion.Error("La pregunta cambió mientras tanto. Actualizá la página.");
                }

                Bitacora(idUsuarioGestor, "BAJA", idPreguntaEspacio,
                    "Pregunta ocultada por el gestor en el espacio \"" + pregunta.NombreEspacio + "\". Motivo: " + motivoLimpio);

                _bllNotificacion.Notificar(pregunta.IdUsuarioExterno, TipoNotificacion.PreguntaEspacioOcultada,
                    "El gestor de \"" + pregunta.NombreEspacio + "\" ocultó tu pregunta. Motivo: " + Recortar(motivoLimpio, 150),
                    "~/Explorar/DetalleEspacio.aspx?id=" + pregunta.IdEspacioArtistico);

                return ResultadoOperacion.Ok("La pregunta quedó oculta: ya no se ve en el espacio.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error(MensajeError);
            }
        }

        public ResultadoOperacion<List<PreguntaEspacio>> ListarParaGestor(int idUsuarioGestor, string estado, int? idEspacioArtistico)
        {
            string filtro = estado == PreguntaEspacio.EstadoPendiente || estado == PreguntaEspacio.EstadoRespondida || estado == PreguntaEspacio.EstadoOculta
                ? estado
                : null;
            try
            {
                return ResultadoOperacion<List<PreguntaEspacio>>.Ok(
                    _mpp.ListarPorGestor(new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor }, filtro, idEspacioArtistico));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<PreguntaEspacio>>.Error("No pudimos cargar las preguntas en este momento. Intentá nuevamente.");
            }
        }

        public int ContarPendientesGestor(int idUsuarioGestor)
        {
            try
            {
                return _mpp.ContarPendientesGestor(new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor });
            }
            catch (ErrorAccesoDatosException)
            {
                return 0;
            }
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        private static ResultadoOperacion ValidarTexto(string texto, int minimo, int maximo, string nombre)
        {
            if (texto == null)
            {
                return ResultadoOperacion.Error("Escribí " + nombre + ".");
            }

            if (texto.Length < minimo)
            {
                return ResultadoOperacion.Error("Escribí " + nombre + " con al menos " + minimo + " caracteres.");
            }

            if (texto.Length > maximo)
            {
                return ResultadoOperacion.Error("No puede superar los " + maximo + " caracteres.");
            }

            if (Correo.IsMatch(texto) || Enlace.IsMatch(texto) || Telefono.IsMatch(texto))
            {
                return ResultadoOperacion.Error("Como las preguntas y respuestas son públicas, no incluyas correos, teléfonos ni enlaces. Para coordinar detalles de una reserva, usá los mensajes de la reserva.");
            }

            return ResultadoOperacion.Ok();
        }

        private void Bitacora(int idUsuarioExterno, string operacion, int idPregunta, string descripcion)
        {
            try
            {
                _bitacora.Registrar(idUsuarioExterno, operacion, TipoEntidadBitacora, idPregunta, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }

        private static string Limpiar(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : Regex.Replace(texto.Trim(), @"[ \t]+", " ");
        }

        private static string Recortar(string texto, int largo)
        {
            return texto.Length <= largo ? texto : texto.Substring(0, largo - 3) + "...";
        }
    }
}
