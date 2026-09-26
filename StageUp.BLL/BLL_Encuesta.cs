using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Ítems 3 y 10 de la segunda entrega: encuestas dinámicas con fecha de
    // vencimiento y gráfico de resultados al instante. Mismo patrón que
    // BLL_Faq.cs (EjecutarProtegido + ResultadoOperacion).
    public class BLL_Encuesta
    {
        private const int LongitudMaximaTitulo = 200;
        private const int LongitudMaximaDescripcion = 1000;
        private const int LongitudMaximaTextoPregunta = 300;
        private const int LongitudMaximaTextoOpcion = 200;
        private const int CantidadMinimaOpciones = 2;

        private static readonly string[] PerfilesValidos = { "Todos", "GestorEspacios", "ExternoSolicitante" };

        private readonly MPP_Encuesta _mpp = new MPP_Encuesta();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const string TipoEntidadBitacora = "Encuesta";

        // ------------------------------------------------------------------
        // Administración
        // ------------------------------------------------------------------
        public List<Encuesta> Listar()
        {
            try
            {
                return _mpp.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Encuesta>();
            }
        }

        public Encuesta ObtenerPorId(int idEncuesta)
        {
            try
            {
                return _mpp.ObtenerPorId(idEncuesta);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public List<PreguntaEncuesta> ListarPreguntasConOpciones(int idEncuesta)
        {
            try
            {
                List<PreguntaEncuesta> preguntas = _mpp.ListarPreguntasPorEncuesta(idEncuesta);
                foreach (PreguntaEncuesta pregunta in preguntas)
                {
                    pregunta.Opciones = _mpp.ListarOpcionesPorPregunta(pregunta.IdPreguntaEncuesta);
                }
                return preguntas;
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<PreguntaEncuesta>();
            }
        }

        public ResultadoOperacion<int> Registrar(
            string titulo, string descripcion, DateTime fechaInicio, DateTime fechaVencimiento,
            string publicoObjetivo, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                string tituloNormalizado;
                string descripcionNormalizada;
                ResultadoOperacion validacion = ValidarDatosGenerales(
                    titulo, descripcion, fechaInicio, fechaVencimiento, publicoObjetivo,
                    out tituloNormalizado, out descripcionNormalizada);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                int idEncuesta = _mpp.Insertar(new Encuesta
                {
                    Titulo = tituloNormalizado,
                    Descripcion = descripcionNormalizada,
                    FechaInicio = fechaInicio,
                    FechaVencimiento = fechaVencimiento,
                    PublicoObjetivo = publicoObjetivo
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", TipoEntidadBitacora, idEncuesta,
                    "Alta de la encuesta \"" + tituloNormalizado + "\" (borrador).");

                return ResultadoOperacion<int>.Ok(idEncuesta, "La encuesta se creó como borrador. Agregale las preguntas y publicala cuando esté lista.");
            });
        }

        public ResultadoOperacion Modificar(
            int idEncuesta, string titulo, string descripcion, DateTime fechaInicio, DateTime fechaVencimiento,
            string publicoObjetivo, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta actual = _mpp.ObtenerPorId(idEncuesta);
                if (actual == null)
                {
                    return ResultadoOperacion.Error("No se encontró la encuesta seleccionada.");
                }

                if (actual.Estado != "Borrador")
                {
                    return ResultadoOperacion.Error("Solo se pueden modificar los datos de una encuesta mientras está en borrador.");
                }

                string tituloNormalizado;
                string descripcionNormalizada;
                ResultadoOperacion validacion = ValidarDatosGenerales(
                    titulo, descripcion, fechaInicio, fechaVencimiento, publicoObjetivo,
                    out tituloNormalizado, out descripcionNormalizada);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                _mpp.Modificar(new Encuesta
                {
                    IdEncuesta = idEncuesta,
                    Titulo = tituloNormalizado,
                    Descripcion = descripcionNormalizada,
                    FechaInicio = fechaInicio,
                    FechaVencimiento = fechaVencimiento,
                    PublicoObjetivo = publicoObjetivo
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idEncuesta,
                    "Modificación de los datos de la encuesta \"" + tituloNormalizado + "\".");

                return ResultadoOperacion.Ok("Los datos de la encuesta se actualizaron correctamente.");
            });
        }

        public ResultadoOperacion<int> AgregarPregunta(
            int idEncuesta, string texto, List<string> opciones, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta encuesta = _mpp.ObtenerPorId(idEncuesta);
                if (encuesta == null)
                {
                    return ResultadoOperacion<int>.Error("No se encontró la encuesta seleccionada.");
                }

                if (encuesta.Estado != "Borrador")
                {
                    return ResultadoOperacion<int>.Error("Solo se pueden agregar preguntas mientras la encuesta está en borrador.");
                }

                string textoNormalizado = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
                if (textoNormalizado == null)
                {
                    return ResultadoOperacion<int>.Error("Ingresá el texto de la pregunta.");
                }

                if (textoNormalizado.Length > LongitudMaximaTextoPregunta)
                {
                    return ResultadoOperacion<int>.Error(
                        "El texto de la pregunta no puede superar los " + LongitudMaximaTextoPregunta + " caracteres.");
                }

                List<string> opcionesNormalizadas = NormalizarOpciones(opciones);
                if (opcionesNormalizadas.Count < CantidadMinimaOpciones)
                {
                    return ResultadoOperacion<int>.Error(
                        "Cada pregunta necesita al menos " + CantidadMinimaOpciones + " opciones distintas (una por línea).");
                }

                string opcionLarga = opcionesNormalizadas.FirstOrDefault(o => o.Length > LongitudMaximaTextoOpcion);
                if (opcionLarga != null)
                {
                    return ResultadoOperacion<int>.Error(
                        "Ninguna opción puede superar los " + LongitudMaximaTextoOpcion + " caracteres.");
                }

                int cantidadPreguntasActual = _mpp.ListarPreguntasPorEncuesta(idEncuesta).Count;

                int idPregunta = _mpp.InsertarPregunta(new PreguntaEncuesta
                {
                    IdEncuesta = idEncuesta,
                    Texto = textoNormalizado,
                    Orden = cantidadPreguntasActual + 1
                });

                int ordenOpcion = 1;
                foreach (string opcion in opcionesNormalizadas)
                {
                    _mpp.InsertarOpcion(new OpcionPregunta
                    {
                        IdPreguntaEncuesta = idPregunta,
                        Texto = opcion,
                        Orden = ordenOpcion
                    });
                    ordenOpcion++;
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idEncuesta,
                    "Se agregó la pregunta \"" + textoNormalizado + "\" a la encuesta \"" + encuesta.Titulo + "\".");

                return ResultadoOperacion<int>.Ok(idPregunta, "La pregunta se agregó correctamente.");
            });
        }

        public ResultadoOperacion EliminarPregunta(int idPreguntaEncuesta, int idEncuesta, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta encuesta = _mpp.ObtenerPorId(idEncuesta);
                if (encuesta == null)
                {
                    return ResultadoOperacion.Error("No se encontró la encuesta seleccionada.");
                }

                if (encuesta.Estado != "Borrador")
                {
                    return ResultadoOperacion.Error("Solo se pueden quitar preguntas mientras la encuesta está en borrador.");
                }

                List<PreguntaEncuesta> preguntas = _mpp.ListarPreguntasPorEncuesta(idEncuesta);
                PreguntaEncuesta pregunta = preguntas.FirstOrDefault(p => p.IdPreguntaEncuesta == idPreguntaEncuesta);
                if (pregunta == null)
                {
                    return ResultadoOperacion.Error("No se encontró la pregunta seleccionada en esta encuesta.");
                }

                _mpp.EliminarPregunta(idPreguntaEncuesta);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idEncuesta,
                    "Se quitó la pregunta \"" + pregunta.Texto + "\" de la encuesta \"" + encuesta.Titulo + "\".");

                return ResultadoOperacion.Ok("La pregunta se quitó correctamente.");
            });
        }

        public ResultadoOperacion Publicar(int idEncuesta, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta encuesta = _mpp.ObtenerPorId(idEncuesta);
                if (encuesta == null)
                {
                    return ResultadoOperacion.Error("No se encontró la encuesta seleccionada.");
                }

                if (encuesta.Estado != "Borrador")
                {
                    return ResultadoOperacion.Error("La encuesta ya fue publicada.");
                }

                if (encuesta.FechaVencimiento <= DateTime.Now)
                {
                    return ResultadoOperacion.Error("La fecha de vencimiento ya pasó. Modificala antes de publicar la encuesta.");
                }

                List<PreguntaEncuesta> preguntas = ListarPreguntasConOpciones(idEncuesta);
                if (preguntas.Count == 0)
                {
                    return ResultadoOperacion.Error("Agregá al menos una pregunta antes de publicar la encuesta.");
                }

                PreguntaEncuesta preguntaIncompleta = preguntas.FirstOrDefault(p => p.Opciones.Count < CantidadMinimaOpciones);
                if (preguntaIncompleta != null)
                {
                    return ResultadoOperacion.Error(
                        "La pregunta \"" + preguntaIncompleta.Texto + "\" necesita al menos " + CantidadMinimaOpciones + " opciones.");
                }

                _mpp.CambiarEstado(idEncuesta, "Activa");

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ACTIVACION", TipoEntidadBitacora, idEncuesta,
                    "Publicación de la encuesta \"" + encuesta.Titulo + "\" (pasa a estar activa para los usuarios).");

                return ResultadoOperacion.Ok("La encuesta se publicó y ya está disponible para los usuarios.");
            });
        }

        public ResultadoOperacion Cerrar(int idEncuesta, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta encuesta = _mpp.ObtenerPorId(idEncuesta);
                if (encuesta == null)
                {
                    return ResultadoOperacion.Error("No se encontró la encuesta seleccionada.");
                }

                if (encuesta.Estado != "Activa")
                {
                    return ResultadoOperacion.Error("Solo se pueden cerrar encuestas que estén activas.");
                }

                _mpp.CambiarEstado(idEncuesta, "Cerrada");

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", TipoEntidadBitacora, idEncuesta,
                    "Cierre anticipado de la encuesta \"" + encuesta.Titulo + "\" (deja de aceptar respuestas).");

                return ResultadoOperacion.Ok("La encuesta se cerró. Ya no acepta nuevas respuestas.");
            });
        }

        public List<ResultadoPreguntaEncuesta> ObtenerResultadosAdmin(int idEncuesta)
        {
            try
            {
                return AgruparResultados(_mpp.ConsultarResultadosFilas(idEncuesta));
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<ResultadoPreguntaEncuesta>();
            }
        }

        // ------------------------------------------------------------------
        // Usuario externo
        // ------------------------------------------------------------------
        public List<Encuesta> ListarPendientesParaUsuario(int idUsuarioExterno, string perfilUsuario)
        {
            try
            {
                return _mpp.ListarPendientesParaUsuario(idUsuarioExterno, perfilUsuario);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Encuesta>();
            }
        }

        public List<Encuesta> ListarConResultadosParaUsuario(int idUsuarioExterno, string perfilUsuario)
        {
            try
            {
                return _mpp.ListarConResultadosParaUsuario(idUsuarioExterno, perfilUsuario);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Encuesta>();
            }
        }

        public ResultadoOperacion<EncuestaCompleta> ObtenerParaResponder(int idEncuesta, int idUsuarioExterno, string perfilUsuario)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta encuesta;
                ResultadoOperacion validacionAcceso = ValidarPuedeResponder(idEncuesta, idUsuarioExterno, perfilUsuario, out encuesta);
                if (!validacionAcceso.Exitoso)
                {
                    return ResultadoOperacion<EncuestaCompleta>.Error(validacionAcceso.Mensaje);
                }

                EncuestaCompleta completa = new EncuestaCompleta
                {
                    Encuesta = encuesta,
                    Preguntas = ListarPreguntasConOpciones(idEncuesta)
                };

                return ResultadoOperacion<EncuestaCompleta>.Ok(completa);
            });
        }

        public ResultadoOperacion RegistrarRespuesta(
            int idEncuesta, int idUsuarioExterno, string perfilUsuario, Dictionary<int, int> respuestasPorPregunta)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta encuesta;
                ResultadoOperacion validacionAcceso = ValidarPuedeResponder(idEncuesta, idUsuarioExterno, perfilUsuario, out encuesta);
                if (!validacionAcceso.Exitoso)
                {
                    return validacionAcceso;
                }

                List<PreguntaEncuesta> preguntas = ListarPreguntasConOpciones(idEncuesta);
                if (respuestasPorPregunta == null || respuestasPorPregunta.Count != preguntas.Count)
                {
                    return ResultadoOperacion.Error("Respondé todas las preguntas antes de enviar la encuesta.");
                }

                foreach (PreguntaEncuesta pregunta in preguntas)
                {
                    int idOpcionElegida;
                    if (!respuestasPorPregunta.TryGetValue(pregunta.IdPreguntaEncuesta, out idOpcionElegida))
                    {
                        return ResultadoOperacion.Error("Respondé todas las preguntas antes de enviar la encuesta.");
                    }

                    if (!pregunta.Opciones.Any(o => o.IdOpcionPregunta == idOpcionElegida))
                    {
                        return ResultadoOperacion.Error("Una de las respuestas seleccionadas no es válida. Volvé a intentarlo.");
                    }
                }

                int idRespuestaEncuesta = _mpp.InsertarRespuesta(idEncuesta, idUsuarioExterno);
                foreach (KeyValuePair<int, int> respuesta in respuestasPorPregunta)
                {
                    _mpp.InsertarRespuestaDetalle(idRespuestaEncuesta, respuesta.Key, respuesta.Value);
                }

                _bitacora.Registrar(
                    idUsuarioExterno, "ASOCIACION", TipoEntidadBitacora, idEncuesta,
                    "El usuario respondió la encuesta \"" + encuesta.Titulo + "\".");

                return ResultadoOperacion.Ok("¡Gracias por responder! Ya podés ver los resultados al instante.");
            });
        }

        public ResultadoOperacion<List<ResultadoPreguntaEncuesta>> ObtenerResultadosParaUsuario(
            int idEncuesta, int idUsuarioExterno, string perfilUsuario)
        {
            return EjecutarProtegido(() =>
            {
                Encuesta encuesta = _mpp.ObtenerPorId(idEncuesta);
                if (encuesta == null)
                {
                    return ResultadoOperacion<List<ResultadoPreguntaEncuesta>>.Error("No se encontró la encuesta seleccionada.");
                }

                bool dirigidaAlPerfil = encuesta.PublicoObjetivo == "Todos" || encuesta.PublicoObjetivo == perfilUsuario;
                if (!dirigidaAlPerfil || encuesta.FechaInicio > DateTime.Now)
                {
                    return ResultadoOperacion<List<ResultadoPreguntaEncuesta>>.Error("Esta encuesta no está disponible para tu perfil.");
                }

                bool yaRespondida = _mpp.ExisteRespuestaDeUsuario(idEncuesta, idUsuarioExterno);
                bool vencidaOCerrada = encuesta.Estado == "Cerrada" || encuesta.FechaVencimiento < DateTime.Now;

                if (!yaRespondida && !vencidaOCerrada)
                {
                    return ResultadoOperacion<List<ResultadoPreguntaEncuesta>>.Error(
                        "Todavía no respondiste esta encuesta. Respondela para ver los resultados.");
                }

                List<ResultadoPreguntaEncuesta> resultados = AgruparResultados(_mpp.ConsultarResultadosFilas(idEncuesta));
                return ResultadoOperacion<List<ResultadoPreguntaEncuesta>>.Ok(resultados);
            });
        }

        // ------------------------------------------------------------------
        // Privados
        // ------------------------------------------------------------------
        private ResultadoOperacion ValidarPuedeResponder(
            int idEncuesta, int idUsuarioExterno, string perfilUsuario, out Encuesta encuesta)
        {
            encuesta = _mpp.ObtenerPorId(idEncuesta);
            if (encuesta == null)
            {
                return ResultadoOperacion.Error("No se encontró la encuesta seleccionada.");
            }

            if (encuesta.Estado != "Activa")
            {
                return ResultadoOperacion.Error("Esta encuesta no está disponible en este momento.");
            }

            if (encuesta.FechaInicio > DateTime.Now)
            {
                return ResultadoOperacion.Error("Esta encuesta todavía no está disponible.");
            }

            if (encuesta.FechaVencimiento < DateTime.Now)
            {
                return ResultadoOperacion.Error("Esta encuesta ya venció.");
            }

            bool dirigidaAlPerfil = encuesta.PublicoObjetivo == "Todos" || encuesta.PublicoObjetivo == perfilUsuario;
            if (!dirigidaAlPerfil)
            {
                return ResultadoOperacion.Error("Esta encuesta no está disponible para tu perfil.");
            }

            if (_mpp.ExisteRespuestaDeUsuario(idEncuesta, idUsuarioExterno))
            {
                return ResultadoOperacion.Error("Ya respondiste esta encuesta.");
            }

            return ResultadoOperacion.Ok();
        }

        private static ResultadoOperacion ValidarDatosGenerales(
            string titulo, string descripcion, DateTime fechaInicio, DateTime fechaVencimiento, string publicoObjetivo,
            out string tituloNormalizado, out string descripcionNormalizada)
        {
            tituloNormalizado = string.IsNullOrWhiteSpace(titulo) ? null : titulo.Trim();
            descripcionNormalizada = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

            if (tituloNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá el título de la encuesta.");
            }

            if (tituloNormalizado.Length > LongitudMaximaTitulo)
            {
                return ResultadoOperacion.Error("El título no puede superar los " + LongitudMaximaTitulo + " caracteres.");
            }

            if (descripcionNormalizada != null && descripcionNormalizada.Length > LongitudMaximaDescripcion)
            {
                return ResultadoOperacion.Error("La descripción no puede superar los " + LongitudMaximaDescripcion + " caracteres.");
            }

            if (!PerfilesValidos.Contains(publicoObjetivo))
            {
                return ResultadoOperacion.Error("Seleccioná a quién va dirigida la encuesta.");
            }

            if (fechaVencimiento <= fechaInicio)
            {
                return ResultadoOperacion.Error("La fecha de vencimiento tiene que ser posterior a la fecha de inicio.");
            }

            if (fechaVencimiento <= DateTime.Now)
            {
                return ResultadoOperacion.Error("La fecha de vencimiento tiene que ser posterior a este momento.");
            }

            return ResultadoOperacion.Ok();
        }

        private static List<string> NormalizarOpciones(List<string> opciones)
        {
            List<string> normalizadas = new List<string>();
            if (opciones == null)
            {
                return normalizadas;
            }

            foreach (string opcion in opciones)
            {
                if (string.IsNullOrWhiteSpace(opcion))
                {
                    continue;
                }

                string valor = opcion.Trim();
                bool yaExiste = normalizadas.Any(o => string.Equals(o, valor, StringComparison.CurrentCultureIgnoreCase));
                if (!yaExiste)
                {
                    normalizadas.Add(valor);
                }
            }

            return normalizadas;
        }

        private static List<ResultadoPreguntaEncuesta> AgruparResultados(List<FilaResultadoEncuesta> filas)
        {
            List<ResultadoPreguntaEncuesta> resultados = new List<ResultadoPreguntaEncuesta>();

            var preguntasAgrupadas = filas
                .GroupBy(f => f.IdPreguntaEncuesta)
                .OrderBy(g => g.First().OrdenPregunta);

            foreach (var grupoPregunta in preguntasAgrupadas)
            {
                FilaResultadoEncuesta primeraFila = grupoPregunta.First();
                int totalRespuestas = primeraFila.TotalRespuestasPregunta;

                ResultadoPreguntaEncuesta resultadoPregunta = new ResultadoPreguntaEncuesta
                {
                    IdPreguntaEncuesta = primeraFila.IdPreguntaEncuesta,
                    TextoPregunta = primeraFila.TextoPregunta,
                    Orden = primeraFila.OrdenPregunta,
                    TotalRespuestas = totalRespuestas
                };

                foreach (FilaResultadoEncuesta fila in grupoPregunta.OrderBy(f => f.OrdenOpcion))
                {
                    resultadoPregunta.Opciones.Add(new ResultadoOpcionEncuesta
                    {
                        IdOpcionPregunta = fila.IdOpcionPregunta,
                        TextoOpcion = fila.TextoOpcion,
                        Orden = fila.OrdenOpcion,
                        CantidadRespuestas = fila.CantidadRespuestas,
                        Porcentaje = totalRespuestas > 0
                            ? Math.Round(fila.CantidadRespuestas * 100.0 / totalRespuestas, 1)
                            : 0
                    });
                }

                resultados.Add(resultadoPregunta);
            }

            return resultados;
        }

        private static ResultadoOperacion EjecutarProtegido(Func<ResultadoOperacion> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        private static ResultadoOperacion<T> EjecutarProtegido<T>(Func<ResultadoOperacion<T>> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<T>.Error(ex.Message);
            }
        }
    }
}
