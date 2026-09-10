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
    /// Core del negocio de StageUp (CU-001-005), versión simplificada para esta entrega:
    /// un usuario autenticado solicita una fecha para un espacio publicado, y el gestor
    /// del espacio la acepta o la rechaza. Sin franjas horarias, sin disponibilidad
    /// configurable y sin pago (esas partes del CU quedan para el Avance 2).
    /// </summary>
    public class BLL_Reserva
    {
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const int LongitudMaximaComentario = 1000;
        private const string TipoEntidadBitacora = "Reserva";

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

                if (minutoDesde.HasValue && duracionMinutos.HasValue)
                {
                    ResultadoOperacion validacionHorario = ValidarHorarioSolicitado(
                        espacio, fechaSolicitada.Date, minutoDesde.Value, duracionMinutos.Value);
                    if (!validacionHorario.Exitoso)
                    {
                        return ResultadoOperacion<int>.Error(validacionHorario.Mensaje);
                    }
                }

                string comentarioPersistido = ConstruirComentarioSolicitud(
                    espacio, fechaSolicitada.Date, minutoDesde, duracionMinutos, comentario);
                if (!string.IsNullOrEmpty(comentarioPersistido) && comentarioPersistido.Length > LongitudMaximaComentario)
                {
                    return ResultadoOperacion<int>.Error(
                        "El detalle completo de la solicitud no puede superar los " + LongitudMaximaComentario + " caracteres.");
                }

                var reserva = new Reserva
                {
                    IdEspacioArtistico = idEspacioArtistico,
                    IdUsuarioExternoSolicitante = idUsuarioExternoSolicitante,
                    FechaSolicitada = fechaSolicitada.Date,
                    ComentarioSolicitante = comentarioPersistido
                };

                int idReserva = _mppReserva.Insertar(reserva);

                _bitacora.Registrar(
                    idUsuarioExternoSolicitante, "ALTA", TipoEntidadBitacora, idReserva,
                    "Solicitud de reserva para el espacio \"" + espacio.NombreEspacio + "\" (" +
                    fechaSolicitada.Date.ToString("dd/MM/yyyy") +
                    (minutoDesde.HasValue ? " a las " + FormatearHora(minutoDesde.Value) : string.Empty) + ").");

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

        private static string ConstruirComentarioSolicitud(
            EspacioArtistico espacio,
            DateTime fecha,
            int? minutoDesde,
            int? duracionMinutos,
            string comentario)
        {
            string comentarioLimpio = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
            if (!minutoDesde.HasValue || !duracionMinutos.HasValue)
            {
                return comentarioLimpio;
            }

            int minutoHasta = minutoDesde.Value + duracionMinutos.Value;
            string detalle = "Horario solicitado: " + fecha.ToString("dd/MM/yyyy") + " de " +
                FormatearHora(minutoDesde.Value) + " a " + FormatearHora(minutoHasta) +
                " (" + FormatearDuracion(duracionMinutos.Value) + ").";

            if (espacio.Ficha != null && espacio.Ficha.PrecioHora.HasValue)
            {
                decimal importe = espacio.Ficha.PrecioHora.Value * duracionMinutos.Value / 60m;
                detalle += " Importe estimado: " + (espacio.Ficha.Moneda ?? "ARS") + " " +
                    importe.ToString("N2", CultureInfo.GetCultureInfo("es-AR")) + ".";
            }

            return comentarioLimpio == null ? detalle : detalle + " Mensaje: " + comentarioLimpio;
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
                            if (reserva.EstadoReserva == EstadoReserva.Aceptada.ToString())
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
            }
        }

        public ResultadoOperacion Aceptar(int idReserva, int idUsuarioGestorSolicitante, string comentarioResolucion)
        {
            return ResolverComoGestor(idReserva, idUsuarioGestorSolicitante, EstadoReserva.Aceptada, comentarioResolucion,
                "aceptó", "Tu reserva fue aceptada. ¡Ya está confirmada!");
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
                Reserva reserva = _mppReserva.ObtenerPorId(idReserva);
                if (reserva == null)
                {
                    return ResultadoOperacion.Error("No se encontró la reserva indicada.");
                }

                if (reserva.IdUsuarioExternoSolicitante != idUsuarioExternoSolicitante)
                {
                    return ResultadoOperacion.Error("No tenés permiso para cancelar esta reserva.");
                }

                if (reserva.EstadoReserva != EstadoReserva.Pendiente.ToString())
                {
                    return ResultadoOperacion.Error("Esta reserva ya fue resuelta y no se puede cancelar.");
                }

                _mppReserva.Cancelar(idReserva);

                _bitacora.Registrar(
                    idUsuarioExternoSolicitante, "MODIFICACION", TipoEntidadBitacora, idReserva,
                    "El solicitante canceló su reserva del espacio \"" + reserva.NombreEspacio + "\".");

                return ResultadoOperacion.Ok("Tu reserva fue cancelada.");
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
