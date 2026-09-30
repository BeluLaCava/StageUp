using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // Módulo de soporte/helpdesk (ítems 6C y 16 de la segunda entrega):
    // antes solo había una página estática de contacto, ahora un usuario
    // externo puede abrir un ticket y mantener una conversación con
    // soporte, y un interno puede tomarlo, responderlo y cerrarlo.
    //
    // Soporte por servicio contratado (corrección de María): el ticket se
    // puede asociar a una reserva propia del usuario (elegida en
    // Soporte.aspx o precargada desde el botón "Contactar soporte" de
    // MisReservas.aspx). Se valida que la reserva sea del usuario, y el
    // panel interno ve el espacio, la fecha, el horario y el estado de esa
    // reserva para responder con contexto. No hay un rol "Soporte" separado
    // todavía: el permiso
    // GESTIONAR_SOPORTE se asigna al rol Administrador, igual que el resto
    // de los permisos nuevos de esta tanda (Julian puede reasignarlo a otro
    // rol desde GestionRoles.aspx cuando lo necesite).
    public class BLL_Ticket
    {
        public static readonly string[] Categorias =
        {
            "Reserva", "Pago", "Cuenta", "Espacio", "Otro"
        };

        private const int LongitudMaximaAsunto = 200;
        private const int LongitudMaximaMensaje = 2000;

        private readonly MPP_Ticket _mpp = new MPP_Ticket();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_Notificacion _bllNotificacion = new BLL_Notificacion();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();

        private const string TipoEntidadBitacora = "Ticket";

        public ResultadoOperacion<int> CrearTicket(
            int idUsuarioExterno, string categoria, string asunto, string mensajeInicial, int? idReservaAsociada)
        {
            return EjecutarProtegido(() =>
            {
                string categoriaNormalizada;
                string asuntoNormalizado;
                string mensajeNormalizado;
                ResultadoOperacion validacion = ValidarDatosAlta(
                    categoria, asunto, mensajeInicial,
                    out categoriaNormalizada, out asuntoNormalizado, out mensajeNormalizado);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                Reserva reservaAsociada = null;
                if (idReservaAsociada.HasValue)
                {
                    // Un usuario solo puede pedir soporte sobre una reserva
                    // que él mismo solicitó (el SP vuelve a validarlo).
                    reservaAsociada = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReservaAsociada.Value });
                    if (reservaAsociada == null || reservaAsociada.IdUsuarioExternoSolicitante != idUsuarioExterno)
                    {
                        return ResultadoOperacion<int>.Error("La reserva seleccionada no es válida. Elegí una de tus reservas.");
                    }
                }

                int idTicket = _mpp.Crear(
                    new Ticket
                    {
                        IdUsuarioExterno = idUsuarioExterno,
                        IdReservaAsociada = idReservaAsociada,
                        Categoria = categoriaNormalizada,
                        Asunto = asuntoNormalizado
                    },
                    new TicketMensaje
                    {
                        IdUsuarioExterno = idUsuarioExterno,
                        Mensaje = mensajeNormalizado
                    });

                _bitacora.Registrar(
                    idUsuarioExterno, "ALTA", TipoEntidadBitacora, idTicket,
                    "Apertura del ticket de soporte \"" + asuntoNormalizado + "\" (categoría: " + categoriaNormalizada + ")"
                    + (reservaAsociada != null ? ", asociado a la reserva #" + reservaAsociada.IdReserva : string.Empty) + ".");

                return ResultadoOperacion<int>.Ok(idTicket, "Tu consulta se envió correctamente. Te vamos a responder a la brevedad.");
            });
        }

        public ResultadoOperacion AgregarMensajeUsuario(int idTicket, int idUsuarioExterno, string mensaje)
        {
            return EjecutarProtegido(() =>
            {
                Ticket ticket = _mpp.ObtenerPorId(TicketConId(idTicket));
                if (ticket == null || ticket.IdUsuarioExterno != idUsuarioExterno)
                {
                    return ResultadoOperacion.Error("No se encontró el ticket seleccionado.");
                }

                if (ticket.Estado == EstadoTicket.Cerrado.ToString())
                {
                    return ResultadoOperacion.Error("Este ticket ya está cerrado. Si necesitás ayuda adicional, abrí una nueva consulta.");
                }

                string mensajeNormalizado;
                ResultadoOperacion validacion = ValidarMensaje(mensaje, out mensajeNormalizado);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                _mpp.InsertarMensaje(new TicketMensaje
                {
                    IdTicket = idTicket,
                    IdUsuarioExterno = idUsuarioExterno,
                    Mensaje = mensajeNormalizado
                });

                // Si soporte ya había respondido, un mensaje nuevo del usuario
                // vuelve a dejar el ticket a la espera de atención.
                if (ticket.Estado == EstadoTicket.Respondido.ToString())
                {
                    CambiarEstadoTicket(idTicket, EstadoTicket.Abierto);
                }

                _bitacora.Registrar(
                    idUsuarioExterno, "RESPUESTA", TipoEntidadBitacora, idTicket,
                    "Nuevo mensaje del usuario en el ticket \"" + ticket.Asunto + "\".");

                return ResultadoOperacion.Ok("Tu mensaje se envió correctamente.");
            });
        }

        public ResultadoOperacion AgregarMensajeInterno(int idTicket, int idUsuarioInterno, string mensaje)
        {
            return EjecutarProtegido(() =>
            {
                Ticket ticket = _mpp.ObtenerPorId(TicketConId(idTicket));
                if (ticket == null)
                {
                    return ResultadoOperacion.Error("No se encontró el ticket seleccionado.");
                }

                if (ticket.Estado == EstadoTicket.Cerrado.ToString())
                {
                    return ResultadoOperacion.Error("El ticket ya está cerrado. Reabrilo si necesitás agregar algo más.");
                }

                string mensajeNormalizado;
                ResultadoOperacion validacion = ValidarMensaje(mensaje, out mensajeNormalizado);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                _mpp.InsertarMensaje(new TicketMensaje
                {
                    IdTicket = idTicket,
                    IdUsuarioInterno = idUsuarioInterno,
                    Mensaje = mensajeNormalizado
                });

                if (!ticket.IdUsuarioInternoAsignado.HasValue)
                {
                    _mpp.Asignar(new Ticket { IdTicket = idTicket, IdUsuarioInternoAsignado = idUsuarioInterno });
                }

                CambiarEstadoTicket(idTicket, EstadoTicket.Respondido);

                _bitacora.RegistrarInterno(
                    idUsuarioInterno, "RESPUESTA", TipoEntidadBitacora, idTicket,
                    "Respuesta de soporte en el ticket \"" + ticket.Asunto + "\".");

                NotificarRespuestaAlUsuario(ticket);

                return ResultadoOperacion.Ok("Tu respuesta se envió correctamente.");
            });
        }

        public ResultadoOperacion CambiarEstado(int idTicket, string nuevoEstado, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Ticket ticket = _mpp.ObtenerPorId(TicketConId(idTicket));
                if (ticket == null)
                {
                    return ResultadoOperacion.Error("No se encontró el ticket seleccionado.");
                }

                EstadoTicket estadoValidado;
                if (!Enum.TryParse(nuevoEstado, out estadoValidado))
                {
                    return ResultadoOperacion.Error("El estado indicado no es válido.");
                }

                CambiarEstadoTicket(idTicket, estadoValidado);

                string tipoOperacion = estadoValidado == EstadoTicket.Cerrado ? "CIERRE" : "MODIFICACION";
                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, tipoOperacion, TipoEntidadBitacora, idTicket,
                    "Cambio de estado del ticket \"" + ticket.Asunto + "\" a \"" + estadoValidado + "\".");

                return ResultadoOperacion.Ok("El estado del ticket se actualizó correctamente.");
            });
        }

        public ResultadoOperacion Asignarme(int idTicket, int idUsuarioInterno)
        {
            return EjecutarProtegido(() =>
            {
                Ticket ticket = _mpp.ObtenerPorId(TicketConId(idTicket));
                if (ticket == null)
                {
                    return ResultadoOperacion.Error("No se encontró el ticket seleccionado.");
                }

                _mpp.Asignar(new Ticket { IdTicket = idTicket, IdUsuarioInternoAsignado = idUsuarioInterno });

                if (ticket.Estado == EstadoTicket.Abierto.ToString())
                {
                    CambiarEstadoTicket(idTicket, EstadoTicket.EnRevision);
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInterno, "ASOCIACION", TipoEntidadBitacora, idTicket,
                    "Se tomó el ticket \"" + ticket.Asunto + "\".");

                return ResultadoOperacion.Ok("El ticket quedó asignado a vos.");
            });
        }

        public List<Ticket> ListarTicketsUsuario(int idUsuarioExterno)
        {
            try
            {
                return _mpp.ListarPorUsuario(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Ticket>();
            }
        }

        public List<Ticket> ListarTicketsInternos(string estado, string categoria)
        {
            try
            {
                return _mpp.ListarParaInterno(new Ticket
                {
                    Estado = string.IsNullOrWhiteSpace(estado) ? null : estado,
                    Categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria
                });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Ticket>();
            }
        }

        public ResultadoOperacion<TicketCompleto> ObtenerDetalleParaUsuario(int idTicket, int idUsuarioExterno)
        {
            return EjecutarProtegido(() =>
            {
                Ticket ticket = _mpp.ObtenerPorId(TicketConId(idTicket));
                if (ticket == null || ticket.IdUsuarioExterno != idUsuarioExterno)
                {
                    return ResultadoOperacion<TicketCompleto>.Error("No se encontró el ticket seleccionado.");
                }

                return ResultadoOperacion<TicketCompleto>.Ok(new TicketCompleto
                {
                    Ticket = ticket,
                    Mensajes = _mpp.ListarMensajesPorTicket(ticket)
                });
            });
        }

        public ResultadoOperacion<TicketCompleto> ObtenerDetalleParaInterno(int idTicket)
        {
            return EjecutarProtegido(() =>
            {
                Ticket ticket = _mpp.ObtenerPorId(TicketConId(idTicket));
                if (ticket == null)
                {
                    return ResultadoOperacion<TicketCompleto>.Error("No se encontró el ticket seleccionado.");
                }

                return ResultadoOperacion<TicketCompleto>.Ok(new TicketCompleto
                {
                    Ticket = ticket,
                    Mensajes = _mpp.ListarMensajesPorTicket(ticket)
                });
            });
        }

        // Reservas del usuario que puede asociar a una consulta (las que él
        // solicitó, en cualquier estado: también se puede pedir ayuda sobre
        // una reserva rechazada, cancelada o finalizada).
        public List<Reserva> ListarReservasParaAsociar(int idUsuarioExterno)
        {
            try
            {
                return _mppReserva.ListarPorSolicitante(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Reserva>();
            }
        }

        // Aviso al equipo interno: cantidad de tickets en estado Abierto
        // (nuevos, o con un mensaje nuevo del usuario después de una
        // respuesta). Se muestra como contador en el menú del panel interno.
        public int ContarTicketsAbiertos()
        {
            try
            {
                return _mpp.ContarAbiertos();
            }
            catch (ErrorAccesoDatosException)
            {
                return 0;
            }
        }

        private void CambiarEstadoTicket(int idTicket, EstadoTicket estado)
        {
            _mpp.CambiarEstado(new Ticket { IdTicket = idTicket, Estado = estado.ToString() });
        }

        private static Ticket TicketConId(int idTicket)
        {
            return new Ticket { IdTicket = idTicket };
        }

        private void NotificarRespuestaAlUsuario(Ticket ticket)
        {
            // Ninguna de las dos notificaciones (campanita interna / mail) debe
            // hacer fallar la respuesta si falla: la respuesta ya se guardó
            // correctamente, es lo que importa. Mismo criterio que
            // BLL_Notificacion.Notificar con las notificaciones de reserva.
            _bllNotificacion.Notificar(
                ticket.IdUsuarioExterno,
                TipoNotificacion.RespuestaTicket,
                "Recibiste una respuesta en tu ticket \"" + ticket.Asunto + "\".",
                "~/Soporte.aspx?ver=" + ticket.IdTicket);

            try
            {
                _servicioCorreo.EnviarNotificacionRespuestaTicket(
                    ticket.CorreoUsuarioExterno, ticket.NombreUsuarioExterno, ticket.Asunto);
            }
            catch (Exception)
            {
                // Igual que arriba: un mail que no sale no debe romper la
                // respuesta que soporte ya envió.
            }
        }

        private static ResultadoOperacion ValidarDatosAlta(
            string categoria, string asunto, string mensajeInicial,
            out string categoriaNormalizada, out string asuntoNormalizado, out string mensajeNormalizado)
        {
            categoriaNormalizada = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
            asuntoNormalizado = string.IsNullOrWhiteSpace(asunto) ? null : asunto.Trim();
            mensajeNormalizado = string.IsNullOrWhiteSpace(mensajeInicial) ? null : mensajeInicial.Trim();

            if (categoriaNormalizada == null || Array.IndexOf(Categorias, categoriaNormalizada) < 0)
            {
                return ResultadoOperacion.Error("Seleccioná una categoría válida para tu consulta.");
            }

            if (asuntoNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá un asunto para tu consulta.");
            }

            if (asuntoNormalizado.Length > LongitudMaximaAsunto)
            {
                return ResultadoOperacion.Error("El asunto no puede superar los " + LongitudMaximaAsunto + " caracteres.");
            }

            if (mensajeNormalizado == null)
            {
                return ResultadoOperacion.Error("Contanos tu consulta en el mensaje.");
            }

            if (mensajeNormalizado.Length > LongitudMaximaMensaje)
            {
                return ResultadoOperacion.Error("El mensaje no puede superar los " + LongitudMaximaMensaje + " caracteres.");
            }

            return ResultadoOperacion.Ok();
        }

        private static ResultadoOperacion ValidarMensaje(string mensaje, out string mensajeNormalizado)
        {
            mensajeNormalizado = string.IsNullOrWhiteSpace(mensaje) ? null : mensaje.Trim();

            if (mensajeNormalizado == null)
            {
                return ResultadoOperacion.Error("Escribí un mensaje antes de enviar.");
            }

            if (mensajeNormalizado.Length > LongitudMaximaMensaje)
            {
                return ResultadoOperacion.Error("El mensaje no puede superar los " + LongitudMaximaMensaje + " caracteres.");
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
