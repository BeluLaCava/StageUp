using System;
using System.Collections.Generic;
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
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const int LongitudMaximaComentario = 1000;
        private const string TipoEntidadBitacora = "Reserva";

        public ResultadoOperacion<int> SolicitarReserva(
            int idUsuarioExternoSolicitante, int idEspacioArtistico, DateTime fechaSolicitada, string comentario)
        {
            return EjecutarProtegido(() =>
            {
                if (fechaSolicitada.Date < DateTime.Now.Date)
                {
                    return ResultadoOperacion<int>.Error("Elegí una fecha a partir de hoy.");
                }

                if (!string.IsNullOrEmpty(comentario) && comentario.Trim().Length > LongitudMaximaComentario)
                {
                    return ResultadoOperacion<int>.Error(
                        "El comentario no puede superar los " + LongitudMaximaComentario + " caracteres.");
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

                var reserva = new Reserva
                {
                    IdEspacioArtistico = idEspacioArtistico,
                    IdUsuarioExternoSolicitante = idUsuarioExternoSolicitante,
                    FechaSolicitada = fechaSolicitada.Date,
                    ComentarioSolicitante = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim()
                };

                int idReserva = _mppReserva.Insertar(reserva);

                _bitacora.Registrar(
                    idUsuarioExternoSolicitante, "ALTA", TipoEntidadBitacora, idReserva,
                    "Solicitud de reserva para el espacio \"" + espacio.NombreEspacio + "\" (" + fechaSolicitada.Date.ToString("dd/MM/yyyy") + ").");

                return ResultadoOperacion<int>.Ok(idReserva,
                    "Enviamos tu solicitud de reserva. El gestor del espacio la va a revisar y te vamos a avisar cuando la resuelva.");
            });
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
                return _mppReserva.ListarPorGestor(idUsuarioGestor);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Reserva>();
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
