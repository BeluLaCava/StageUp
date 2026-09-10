using System;
using System.Collections.Generic;
using System.Globalization;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    /// <summary>
    /// Core del negocio de StageUp (CU-001-005): un usuario autenticado solicita un
    /// horario para un espacio publicado, y el gestor del espacio la acepta o la
    /// rechaza. Horario, precio pactado e importe estimado son columnas normalizadas
    /// de Reserva (tanda 10/09) — antes viajaban como texto dentro del comentario.
    /// Incluye validación de solapamiento (ítem 2) y comisión de cancelación del 10%
    /// si se cancela una reserva ya Aceptada con menos de 24hs de anticipación
    /// (tanda 5). Sigue sin pago real ni facturación (Avance 2).
    /// </summary>
    public class BLL_Reserva
    {
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();
        private readonly MPP_Calificacion _mppCalificacion = new MPP_Calificacion();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const int LongitudMaximaComentario = 1000;
        private const string TipoEntidadBitacora = "Reserva";
        private const decimal PorcentajeComisionCancelacion = 0.10m;
        private const int HorasLimiteSinComision = 24;

        public ResultadoOperacion<int> SolicitarReserva(
            int idUsuarioExternoSolicitante, int idEspacioArtistico, DateTime fechaSolicitada, string comentario)
        {
            return SolicitarReservaInterna(
                idUsuarioExternoSolicitante, idEspacioArtistico, fechaSolicitada, null, null, comentario);
        }

        public ResultadoOperacion<int> SolicitarReserva(
            int idUsuarioExternoSolicitante,
            int idEspacioArtistico,
            DateTime fechaSolicitada,
            int minutoDesde,
            int duracionMinutos,
            string comentario)
        {
            return SolicitarReservaInterna(
                idUsuarioExternoSolicitante,
                idEspacioArtistico,
                fechaSolicitada,
                minutoDesde,
                duracionMinutos,
                comentario);
        }

        private ResultadoOperacion<int> SolicitarReservaInterna(
            int idUsuarioExternoSolicitante,
            int idEspacioArtistico,
            DateTime fechaSolicitada,
            int? minutoDesde,
            int? duracionMinutos,
            string comentario)
        {
            return EjecutarProtegido(() =>
            {
                if (fechaSolicitada.Date < DateTime.Now.Date)
                {
                    return ResultadoOperacion<int>.Error("Elegí una fecha a partir de hoy.");
                }

                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
                if (espacio == null || !espacio.Activo || !espacio.Publicado)
                {
                    return ResultadoOperacion<int>.Error("Este espacio no está disponible para reservar.");
                }

                if (espacio.IdUsuarioGestor == idUsuarioExternoSolicitante)
                {
                    return ResultadoOperacion<int>.Error("No podés solicitar una reserva sobre tu propio espacio.");
                }

                int? minutoHasta = null;
                decimal? precioHoraPactado = null;
                string moneda = null;
                decimal? importeEstimado = null;

                if (minutoDesde.HasValue && duracionMinutos.HasValue)
                {
                    ResultadoOperacion validacionHorario = ValidarHorarioSolicitado(
                        espacio, fechaSolicitada.Date, minutoDesde.Value, duracionMinutos.Value);
                    if (!validacionHorario.Exitoso)
                    {
                        return ResultadoOperacion<int>.Error(validacionHorario.Mensaje);
                    }

                    minutoHasta = minutoDesde.Value + duracionMinutos.Value;

                    // Ítem 2: aviso temprano de solapamiento. Se vuelve a revisar (y
                    // ahí sí de forma atómica) en el momento de aceptar, porque puede
                    // haber pasado tiempo entre que se pidió y que el gestor resuelve.
                    if (_mppReserva.ExisteSolapamiento(idEspacioArtistico, fechaSolicitada.Date, minutoDesde.Value, minutoHasta.Value))
                    {
                        return ResultadoOperacion<int>.Error(
                            "Ese horario ya tiene otra solicitud pendiente o aceptada para este espacio. Elegí otro horario.");
                    }

                    if (espacio.Ficha != null && espacio.Ficha.PrecioHora.HasValue)
                    {
                        precioHoraPactado = espacio.Ficha.PrecioHora.Value;
                        moneda = espacio.Ficha.Moneda ?? "ARS";
                        importeEstimado = CalcularImporte(precioHoraPactado.Value, duracionMinutos.Value);
                    }
                }

                string comentarioLimpio = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
                if (!string.IsNullOrEmpty(comentarioLimpio) && comentarioLimpio.Length > LongitudMaximaComentario)
                {
                    return ResultadoOperacion<int>.Error(
                        "El comentario no puede superar los " + LongitudMaximaComentario + " caracteres.");
                }

                var reserva = new Reserva
                {
                    IdEspacioArtistico = idEspacioArtistico,
                    IdUsuarioExternoSolicitante = idUsuarioExternoSolicitante,
                    FechaSolicitada = fechaSolicitada.Date,
                    ComentarioSolicitante = comentarioLimpio,
                    MinutoDesde = minutoDesde,
                    MinutoHasta = minutoHasta,
                    PrecioHoraPactado = precioHoraPactado,
                    Moneda = moneda,
                    ImporteEstimado = importeEstimado
                };

                int idReserva = _mppReserva.Insertar(reserva);

                _bitacora.Registrar(
                    idUsuarioExternoSolicitante, "ALTA", TipoEntidadBitacora, idReserva,
                    "Solicitud de reserva para el espacio \"" + espacio.NombreEspacio + "\" (" +
                    fechaSolicitada.Date.ToString("dd/MM/yyyy") +
                    (minutoDesde.HasValue && duracionMinutos.HasValue
                        ? " a las " + FormatearHora(minutoDesde.Value) + ", " + FormatearDuracion(duracionMinutos.Value)
                        : string.Empty) + ").");

                return ResultadoOperacion<int>.Ok(idReserva,
                    "Enviamos tu solicitud de reserva. El gestor del espacio la va a revisar y te vamos a avisar cuando la resuelva.");
            });
        }

        private static ResultadoOperacion ValidarHorarioSolicitado(
            EspacioArtistico espacio, DateTime fecha, int minutoDesde, int duracionMinutos)
        {
            int minutoHasta = minutoDesde + duracionMinutos;
            if (minutoDesde < 0 || minutoDesde >= 1440 || minutoDesde % 30 != 0 ||
                duracionMinutos < 30 || duracionMinutos % 30 != 0 || minutoHasta > 1440)
            {
                return ResultadoOperacion.Error("Elegí un horario y una duración válidos, en intervalos de 30 minutos.");
            }

            if (fecha.Date == DateTime.Now.Date && fecha.Date.AddMinutes(minutoDesde) <= DateTime.Now)
            {
                return ResultadoOperacion.Error("Elegí un horario posterior al momento actual.");
            }

            FichaEspacio ficha = espacio.Ficha ?? new FichaEspacio();
            List<FranjaEspacio> franjas = ficha.Disponibilidad ?? new List<FranjaEspacio>();
            string fechaTexto = fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            bool tieneExcepcionParaFecha = franjas.Exists(franja =>
                franja != null && string.Equals(franja.Fecha, fechaTexto, StringComparison.Ordinal));
            int diaSemana = fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

            bool estaDisponible = franjas.Exists(franja =>
                franja != null &&
                !franja.Bloqueado &&
                (tieneExcepcionParaFecha
                    ? string.Equals(franja.Fecha, fechaTexto, StringComparison.Ordinal)
                    : string.IsNullOrEmpty(franja.Fecha) && franja.DiaSemana == diaSemana) &&
                minutoDesde >= franja.MinutoDesde &&
                minutoHasta <= franja.MinutoHasta);

            return estaDisponible
                ? ResultadoOperacion.Ok()
                : ResultadoOperacion.Error("La franja elegida no está dentro de la disponibilidad informada para esa fecha.");
        }

        private static decimal CalcularImporte(decimal precioHora, int duracionMinutos)
        {
            return decimal.Round(precioHora * duracionMinutos / 60m, 2);
        }

        private static string FormatearHora(int minutos)
        {
            return minutos == 1440
                ? "24:00"
                : (minutos / 60).ToString("00") + ":" + (minutos % 60).ToString("00");
        }

        private static string FormatearDuracion(int minutos)
        {
            int horas = minutos / 60;
            int resto = minutos % 60;
            if (horas == 0)
            {
                return resto + " minutos";
            }

            string texto = horas + (horas == 1 ? " hora" : " horas");
            return resto == 0 ? texto : texto + " y " + resto + " minutos";
        }

        public List<Reserva> ListarMisReservas(int idUsuarioExternoSolicitante)
        {
            try
            {
                _mppReserva.FinalizarVencidas();
                return _mppReserva.ListarPorSolicitante(idUsuarioExternoSolicitante);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Reserva>();
            }
        }

        public List<Reserva> ListarSolicitudesRecibidas(int idUsuarioGestor)
        {
            try
            {
                _mppReserva.FinalizarVencidas();
                List<Reserva> solicitudes = _mppReserva.ListarPorGestor(idUsuarioGestor);
                CompletarReputacionSolicitantes(solicitudes);
                return solicitudes;
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Reserva>();
            }
        }

        private void CompletarReputacionSolicitantes(List<Reserva> solicitudes)
        {
            var usuarios = new Dictionary<int, UsuarioExterno>();
            var reservasAceptadas = new Dictionary<int, int>();
            var calificacionesPorUsuario = new Dictionary<int, List<Calificacion>>();
            Dictionary<int, ResumenReputacion> resumenes = new Dictionary<int, ResumenReputacion>();

            try
            {
                resumenes = _mppCalificacion.ListarResumenesUsuarios();
            }
            catch (ErrorAccesoDatosException)
            {
            }

            foreach (Reserva solicitud in solicitudes)
            {
                int idSolicitante = solicitud.IdUsuarioExternoSolicitante;
                if (!usuarios.ContainsKey(idSolicitante))
                {
                    UsuarioExterno usuario = null;
                    int cantidadAceptadas = 0;

                    try
                    {
                        usuario = _mppUsuario.ObtenerPorId(idSolicitante);
                    }
                    catch (ErrorAccesoDatosException)
                    {
                    }

                    try
                    {
                        List<Reserva> historial = _mppReserva.ListarPorSolicitante(idSolicitante);
                        foreach (Reserva reserva in historial)
                        {
                            if (reserva.EstadoReserva == EstadoReserva.Aceptada.ToString() ||
                                reserva.EstadoReserva == EstadoReserva.Finalizada.ToString())
                            {
                                cantidadAceptadas++;
                            }
                        }
                    }
                    catch (ErrorAccesoDatosException)
                    {
                    }

                    usuarios[idSolicitante] = usuario;
                    reservasAceptadas[idSolicitante] = cantidadAceptadas;

                    try
                    {
                        List<Calificacion> calificaciones = _mppCalificacion.ListarRecibidasPorUsuario(idSolicitante);
                        calificacionesPorUsuario[idSolicitante] = calificaciones.Count <= 3
                            ? calificaciones
                            : calificaciones.GetRange(0, 3);
                    }
                    catch (ErrorAccesoDatosException)
                    {
                        calificacionesPorUsuario[idSolicitante] = new List<Calificacion>();
                    }
                }

                UsuarioExterno solicitante = usuarios[idSolicitante];
                if (solicitante != null)
                {
                    solicitud.SolicitanteDesde = solicitante.FechaActivacion ?? solicitante.FechaAlta;
                    if (string.IsNullOrWhiteSpace(solicitud.NombreSolicitante))
                    {
                        solicitud.NombreSolicitante = (solicitante.Nombre + " " + solicitante.Apellido).Trim();
                    }

                    if (string.IsNullOrWhiteSpace(solicitud.CorreoSolicitante))
                    {
                        solicitud.CorreoSolicitante = solicitante.CorreoElectronico;
                    }
                }

                solicitud.CantidadReservasAceptadasSolicitante = reservasAceptadas[idSolicitante];
                solicitud.CalificacionesSolicitante = calificacionesPorUsuario[idSolicitante];
                ResumenReputacion resumen;
                if (resumenes.TryGetValue(idSolicitante, out resumen))
                {
                    solicitud.PromedioCalificacionSolicitante = resumen.Promedio;
                    solicitud.CantidadCalificacionesSolicitante = resumen.CantidadCalificaciones;
                }
            }
        }

        public ResultadoOperacion Aceptar(int idReserva, int idUsuarioGestorSolicitante, string comentarioResolucion)
        {
            return EjecutarProtegido(() =>
            {
                if (!string.IsNullOrEmpty(comentarioResolucion) && comentarioResolucion.Trim().Length > LongitudMaximaComentario)
                {
                    return ResultadoOperacion.Error(
                        "El comentario no puede superar los " + LongitudMaximaComentario + " caracteres.");
                }

                Reserva reserva = _mppReserva.ObtenerPorId(idReserva);
                ResultadoOperacion validacion = ValidarPropiedadGestor(reserva, idUsuarioGestorSolicitante);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                DateTime finalReserva = reserva.FechaSolicitada.Date.AddMinutes(reserva.MinutoHasta ?? 1440);
                if (finalReserva <= DateTime.Now)
                {
                    return ResultadoOperacion.Error("El horario solicitado ya terminó y no se puede aceptar.");
                }

                string comentarioLimpio = string.IsNullOrWhiteSpace(comentarioResolucion) ? null : comentarioResolucion.Trim();

                // Ítem 2: revalidación atómica. Puede haber pasado tiempo desde que se
                // solicitó, y otra reserva para el mismo horario pudo haberse aceptado
                // primero, o esta solicitud pudo haber dejado de estar Pendiente
                // mientras tanto.
                bool aceptada = _mppReserva.AceptarSiDisponible(idReserva, comentarioLimpio);
                if (!aceptada)
                {
                    return ResultadoOperacion.Error(
                        "No se pudo aceptar la solicitud: ya no está pendiente o el horario dejó de estar " +
                        "disponible (es posible que hayas aceptado otra reserva para el mismo horario).");
                }

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idReserva,
                    "El gestor aceptó la solicitud de reserva del espacio \"" + reserva.NombreEspacio + "\".");

                return ResultadoOperacion.Ok("Tu reserva fue aceptada. ¡Ya está confirmada!");
            });
        }

        public ResultadoOperacion Rechazar(int idReserva, int idUsuarioGestorSolicitante, string comentarioResolucion)
        {
            return ResolverComoGestor(idReserva, idUsuarioGestorSolicitante, EstadoReserva.Rechazada, comentarioResolucion,
                "rechazó", "Tu reserva fue rechazada.");
        }

        private ResultadoOperacion ResolverComoGestor(
            int idReserva, int idUsuarioGestorSolicitante, EstadoReserva nuevoEstado,
            string comentarioResolucion, string verboBitacora, string mensajeExito)
        {
            return EjecutarProtegido(() =>
            {
                if (!string.IsNullOrEmpty(comentarioResolucion) && comentarioResolucion.Trim().Length > LongitudMaximaComentario)
                {
                    return ResultadoOperacion.Error(
                        "El comentario no puede superar los " + LongitudMaximaComentario + " caracteres.");
                }

                Reserva reserva = _mppReserva.ObtenerPorId(idReserva);
                ResultadoOperacion validacion = ValidarPropiedadGestor(reserva, idUsuarioGestorSolicitante);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                _mppReserva.Resolver(idReserva, nuevoEstado.ToString(),
                    string.IsNullOrWhiteSpace(comentarioResolucion) ? null : comentarioResolucion.Trim());

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idReserva,
                    "El gestor " + verboBitacora + " la solicitud de reserva del espacio \"" + reserva.NombreEspacio + "\".");

                return ResultadoOperacion.Ok(mensajeExito);
            });
        }

        public ResultadoOperacion Cancelar(int idReserva, int idUsuarioExternoSolicitante)
        {
            return EjecutarProtegido(() =>
            {
                _mppReserva.FinalizarVencidas();
                Reserva reserva = _mppReserva.ObtenerPorId(idReserva);
                if (reserva == null)
                {
                    return ResultadoOperacion.Error("No se encontró la reserva indicada.");
                }

                if (reserva.IdUsuarioExternoSolicitante != idUsuarioExternoSolicitante)
                {
                    return ResultadoOperacion.Error("No tenés permiso para cancelar esta reserva.");
                }

                bool esPendiente = reserva.EstadoReserva == EstadoReserva.Pendiente.ToString();
                bool esAceptada = reserva.EstadoReserva == EstadoReserva.Aceptada.ToString();
                if (!esPendiente && !esAceptada)
                {
                    return ResultadoOperacion.Error("Esta reserva ya fue resuelta y no se puede cancelar.");
                }

                bool comisionAplicada = false;
                decimal? importeComision = null;

                // Tanda 5: cancelar una reserva ya Aceptada con menos de 24hs de
                // anticipación respecto del horario solicitado aplica una comisión
                // del 10% del importe estimado. Cancelar mientras sigue Pendiente
                // nunca tiene comisión.
                if (esAceptada && reserva.MinutoDesde.HasValue && reserva.ImporteEstimado.HasValue)
                {
                    DateTime momentoReservado = reserva.FechaSolicitada.Date.AddMinutes(reserva.MinutoDesde.Value);
                    double horasRestantes = (momentoReservado - DateTime.Now).TotalHours;
                    if (horasRestantes < HorasLimiteSinComision)
                    {
                        comisionAplicada = true;
                        importeComision = decimal.Round(reserva.ImporteEstimado.Value * PorcentajeComisionCancelacion, 2);
                    }
                }

                _mppReserva.Cancelar(idReserva, comisionAplicada, importeComision);

                string mensaje = comisionAplicada
                    ? "Tu reserva fue cancelada. Como faltaban menos de " + HorasLimiteSinComision +
                      "hs para el horario reservado, se aplicó una comisión de cancelación de " +
                      importeComision.Value.ToString("0.##", CultureInfo.InvariantCulture) + " " + (reserva.Moneda ?? "ARS") + "."
                    : "Tu reserva fue cancelada.";

                _bitacora.Registrar(
                    idUsuarioExternoSolicitante, "MODIFICACION", TipoEntidadBitacora, idReserva,
                    "El solicitante canceló su reserva del espacio \"" + reserva.NombreEspacio + "\"." +
                    (comisionAplicada ? " Se aplicó comisión de cancelación." : string.Empty));

                return ResultadoOperacion.Ok(mensaje);
            });
        }

        private static ResultadoOperacion ValidarPropiedadGestor(Reserva reserva, int idUsuarioGestorSolicitante)
        {
            if (reserva == null)
            {
                return ResultadoOperacion.Error("No se encontró la reserva indicada.");
            }

            if (reserva.IdUsuarioGestor != idUsuarioGestorSolicitante)
            {
                return ResultadoOperacion.Error("No tenés permiso para resolver esta reserva.");
            }

            if (reserva.EstadoReserva != EstadoReserva.Pendiente.ToString())
            {
                return ResultadoOperacion.Error("Esta reserva ya fue resuelta anteriormente.");
            }

            return ResultadoOperacion.Ok();
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
