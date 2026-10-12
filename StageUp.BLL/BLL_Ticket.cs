using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // Solicitudes de soporte.
    //   CU-003-002 Registrar y realizar seguimiento de solicitudes de soporte
    //     (usuario externo: Soporte.aspx).
    //   CU-003-003 Gestionar solicitudes de soporte internamente (equipo
    //     interno: Interno/GestionSoporte.aspx, permiso GESTIONAR_SOPORTE).
    //   Ítem 12 del profesor: conversación entre el usuario y el equipo,
    //     leído / no leído de cada lado, actualización periódica de las
    //     pantallas (AyudaSoporte.ashx) y asignación de responsables.
    //
    // Estados: Pendiente -> En proceso -> Resuelta -> Cerrada. Una resuelta
    // puede volver a "en proceso" (el usuario sigue necesitando ayuda) y una
    // cerrada solo se reabre con el permiso REABRIR_SOLICITUD_SOPORTE (A10).
    public class BLL_Ticket
    {
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoEnProceso = "EnProceso";
        public const string EstadoResuelta = "Resuelta";
        public const string EstadoCerrada = "Cerrada";

        public const string PermisoGestionar = "GESTIONAR_SOPORTE";
        public const string PermisoReabrir = "REABRIR_SOLICITUD_SOPORTE";

        public const string OrigenSoporte = "Soporte";
        public const string OrigenCentroAyuda = "CentroAyuda";
        public const string OrigenAsistente = "Asistente";
        public const string OrigenReserva = "Reserva";

        public static readonly string[] Estados = { EstadoPendiente, EstadoEnProceso, EstadoResuelta, EstadoCerrada };

        // CU-003-002 paso 5: funcionalidad vinculada al inconveniente.
        public static readonly string[] Funcionalidades = { "Reserva", "Pago", "Espacio", "Cuenta", "Gestor", "Otra" };

        public const int LongitudMinimaMotivo = 5;
        public const int LongitudMaximaMotivo = 150;
        public const int LongitudMinimaDescripcion = 30;
        public const int LongitudMaximaDescripcion = 2000;
        public const int LongitudMaximaInformacion = 1000;
        public const int LongitudMaximaMensaje = 2000;
        public const int LongitudMinimaRespuesta = 10;
        private const int PalabrasMinimasDescripcion = 5;

        // Transiciones válidas (CU-003-003 paso 21 y A9). Cerrada -> En proceso
        // es la reapertura y requiere el permiso específico (A10).
        private static readonly Dictionary<string, string[]> Transiciones = new Dictionary<string, string[]>
        {
            { EstadoPendiente, new[] { EstadoEnProceso, EstadoResuelta } },
            { EstadoEnProceso, new[] { EstadoResuelta } },
            { EstadoResuelta, new[] { EstadoEnProceso, EstadoCerrada } },
            { EstadoCerrada, new[] { EstadoEnProceso } }
        };

        private const string TipoEntidadBitacora = "Ticket";
        private const string MensajeErrorRegistro = "No fue posible registrar la solicitud en este momento. Intentá nuevamente en unos minutos.";
        private const string MensajeErrorActualizacion = "No fue posible completar la operación en este momento. La solicitud mantiene la última información guardada; intentá nuevamente.";

        private readonly MPP_Ticket _mpp = new MPP_Ticket();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_Notificacion _bllNotificacion = new BLL_Notificacion();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();

        // ------------------------------------------------------------------
        // Textos para las pantallas
        // ------------------------------------------------------------------

        public static string TextoEstado(string estado)
        {
            switch (estado)
            {
                case EstadoPendiente: return "Pendiente";
                case EstadoEnProceso: return "En proceso";
                case EstadoResuelta: return "Resuelta";
                case EstadoCerrada: return "Cerrada";
                default: return estado;
            }
        }

        public static string ClaseEstado(string estado)
        {
            switch (estado)
            {
                case EstadoPendiente: return "ticket-badge-pendiente";
                case EstadoEnProceso: return "ticket-badge-enproceso";
                case EstadoResuelta: return "ticket-badge-resuelta";
                case EstadoCerrada: return "ticket-badge-cerrada";
                default: return string.Empty;
            }
        }

        public static string TextoFuncionalidad(string funcionalidad)
        {
            switch (funcionalidad)
            {
                case "Reserva": return "Reserva";
                case "Pago": return "Pago";
                case "Espacio": return "Espacio artístico";
                case "Cuenta": return "Cuenta de usuario";
                case "Gestor": return "Habilitación como gestor";
                case "Otra": return "Otra funcionalidad";
                default: return funcionalidad;
            }
        }

        public static string TextoOrigen(string origen)
        {
            switch (origen)
            {
                case OrigenCentroAyuda: return "Centro de ayuda";
                case OrigenAsistente: return "Asistente de ayuda";
                case OrigenReserva: return "Detalle de una reserva";
                default: return "Sección Soporte";
            }
        }

        // CU-003-002 A7: qué se le muestra al usuario según el estado.
        public static string MensajeSegunEstado(string estado)
        {
            switch (estado)
            {
                case EstadoPendiente:
                    return "Tu solicitud está a la espera de revisión por parte del equipo de soporte. Te avisamos cuando haya novedades.";
                case EstadoEnProceso:
                    return "El equipo de soporte está trabajando en tu solicitud. Abajo ves las respuestas y actualizaciones registradas.";
                case EstadoResuelta:
                    return "El equipo de soporte marcó tu solicitud como resuelta. Abajo está la respuesta y la información de la resolución. Si todavía necesitás ayuda, escribí un mensaje y el caso vuelve a quedar en proceso.";
                case EstadoCerrada:
                    return "Esta solicitud está cerrada. Podés consultar el historial del caso y las respuestas registradas; si tenés un inconveniente nuevo, registrá otra solicitud.";
                default:
                    return string.Empty;
            }
        }

        // ------------------------------------------------------------------
        // CU-003-002: registro (pasos 3 a 16, A2, A3 y A4)
        // ------------------------------------------------------------------

        public ResultadoOperacion<int> RegistrarSolicitud(SolicitudSoporteNueva datos)
        {
            string motivo = Limpiar(datos.Motivo);
            string funcionalidad = Limpiar(datos.FuncionalidadVinculada);
            string descripcion = Limpiar(datos.Descripcion);
            string informacion = Limpiar(datos.InformacionAdicional);
            string origen = Limpiar(datos.Origen);
            if (origen != OrigenCentroAyuda && origen != OrigenAsistente && origen != OrigenReserva)
            {
                origen = OrigenSoporte;
            }

            // A2: campos obligatorios.
            List<string> faltantes = new List<string>();
            if (motivo == null) faltantes.Add("motivo de la consulta");
            if (funcionalidad == null || !Funcionalidades.Contains(funcionalidad)) faltantes.Add("funcionalidad vinculada");
            if (descripcion == null) faltantes.Add("descripción del inconveniente");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion<int>.Error("Completá " + UnirConY(faltantes) + " antes de enviar la solicitud.", "A2");
            }

            if (motivo.Length < LongitudMinimaMotivo)
            {
                return ResultadoOperacion<int>.Error("El motivo de la consulta tiene que tener al menos " + LongitudMinimaMotivo + " caracteres.", "A2");
            }

            if (motivo.Length > LongitudMaximaMotivo)
            {
                return ResultadoOperacion<int>.Error("El motivo de la consulta no puede superar los " + LongitudMaximaMotivo + " caracteres.", "A2");
            }

            if (descripcion.Length > LongitudMaximaDescripcion)
            {
                return ResultadoOperacion<int>.Error("La descripción no puede superar los " + LongitudMaximaDescripcion + " caracteres.", "A2");
            }

            if (informacion != null && informacion.Length > LongitudMaximaInformacion)
            {
                return ResultadoOperacion<int>.Error("La información adicional no puede superar los " + LongitudMaximaInformacion + " caracteres.", "A2");
            }

            // A3: información insuficiente para analizar el caso.
            string insuficiente = EvaluarInformacionSuficiente(motivo, funcionalidad, descripcion, informacion, datos.IdReservaAsociada);
            if (insuficiente != null)
            {
                return ResultadoOperacion<int>.Error(insuficiente, "A3");
            }

            try
            {
                Reserva reserva = null;
                if (datos.IdReservaAsociada.HasValue)
                {
                    reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = datos.IdReservaAsociada.Value });
                    if (reserva == null || reserva.IdUsuarioExternoSolicitante != datos.IdUsuarioExterno)
                    {
                        return ResultadoOperacion<int>.Error("La reserva seleccionada no es válida. Elegí una de tus reservas.", "A2");
                    }
                }

                // Pasos 10 a 14 (el SP genera el identificador, deja el estado
                // pendiente y la primera entrada del historial).
                int idTicket = _mpp.Registrar(new Ticket
                {
                    IdUsuarioExterno = datos.IdUsuarioExterno,
                    IdReservaAsociada = datos.IdReservaAsociada,
                    FuncionalidadVinculada = funcionalidad,
                    Motivo = motivo,
                    Descripcion = descripcion,
                    InformacionAdicional = informacion,
                    Origen = origen
                });

                string codigo = Ticket.FormatearCodigo(idTicket);
                RegistrarBitacoraExterna(datos.IdUsuarioExterno, "ALTA", idTicket,
                    "Registro de la solicitud de soporte " + codigo + " \"" + motivo + "\" (funcionalidad: " + TextoFuncionalidad(funcionalidad) +
                    ", origen: " + TextoOrigen(origen) + (reserva != null ? ", reserva #" + reserva.IdReserva : string.Empty) + ").");

                // Paso 15.
                _bllNotificacion.Notificar(datos.IdUsuarioExterno, TipoNotificacion.SolicitudSoporteRegistrada,
                    "Registramos tu solicitud " + codigo + " (\"" + motivo + "\"). Estado: " + TextoEstado(EstadoPendiente) + ".",
                    UrlSeguimiento(idTicket));

                return ResultadoOperacion<int>.Ok(idTicket,
                    "Tu solicitud de soporte " + codigo + " se registró correctamente. Estado: " + TextoEstado(EstadoPendiente) + ". Te avisamos cuando haya novedades.");
            }
            catch (ErrorAccesoDatosException)
            {
                // A4: no queda registrada; el formulario conserva lo cargado.
                return ResultadoOperacion<int>.Error(MensajeErrorRegistro, "A4");
            }
        }

        // A3. Devuelve null si alcanza, o el mensaje con lo que falta.
        private static string EvaluarInformacionSuficiente(string motivo, string funcionalidad, string descripcion, string informacion, int? idReserva)
        {
            string normalizada = BLL_Faq.Normalizar(descripcion);
            int palabras = normalizada.Split(' ').Count(p => p.Length > 1);
            int caracteresDistintos = normalizada.Replace(" ", string.Empty).Distinct().Count();

            if (descripcion.Length < LongitudMinimaDescripcion || palabras < PalabrasMinimasDescripcion || caracteresDistintos < 6)
            {
                return "Necesitamos más detalle para analizar el caso: contanos qué intentabas hacer, qué pasó y, si apareció, qué mensaje viste (al menos " +
                    LongitudMinimaDescripcion + " caracteres).";
            }

            if (normalizada == BLL_Faq.Normalizar(motivo))
            {
                return "La descripción repite el motivo. Contanos qué intentabas hacer, qué pasó y qué mensaje viste.";
            }

            if ((funcionalidad == "Reserva" || funcionalidad == "Pago") && !idReserva.HasValue &&
                (informacion == null || informacion.Length < 3))
            {
                return "Indicá a qué reserva o pago se refiere: elegila en \"Reserva relacionada\" o escribí el número en \"Información adicional\".";
            }

            if (funcionalidad == "Otra" && (informacion == null || informacion.Length < 3))
            {
                return "Como elegiste \"Otra funcionalidad\", contanos en \"Información adicional\" en qué parte de StageUp ocurre.";
            }

            return null;
        }

        // ------------------------------------------------------------------
        // CU-003-002: seguimiento (A5 a A9)
        // ------------------------------------------------------------------

        public ResultadoOperacion<List<Ticket>> ListarDelUsuario(int idUsuarioExterno)
        {
            try
            {
                return ResultadoOperacion<List<Ticket>>.Ok(_mpp.ListarPorUsuario(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno }));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<Ticket>>.Error("No pudimos cargar tus solicitudes de soporte en este momento. Intentá nuevamente.");
            }
        }

        // A5 pasos 8 y 9, A7 y A8. Al abrirla, lo que había queda leído.
        public ResultadoOperacion<TicketCompleto> ObtenerSeguimiento(int idTicket, int idUsuarioExterno)
        {
            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                if (ticket == null || ticket.IdUsuarioExterno != idUsuarioExterno)
                {
                    return ResultadoOperacion<TicketCompleto>.Error("No se encontró la solicitud de soporte seleccionada.");
                }

                TicketCompleto completo = ArmarDetalle(ticket, false);
                if (completo.Novedades > 0 || !ticket.FechaUltimaLecturaUsuario.HasValue)
                {
                    MarcarLeidoSinFallar(ticket, "Usuario");
                }

                return ResultadoOperacion<TicketCompleto>.Ok(completo);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<TicketCompleto>.Error("No pudimos cargar la solicitud en este momento. Intentá nuevamente.");
            }
        }

        // Solo verifica que exista y sea del usuario (lo usa el centro de
        // notificaciones antes de redirigir); no marca nada como leído.
        public ResultadoOperacion<TicketCompleto> ObtenerDetalleParaUsuario(int idTicket, int idUsuarioExterno)
        {
            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                return ticket == null || ticket.IdUsuarioExterno != idUsuarioExterno
                    ? ResultadoOperacion<TicketCompleto>.Error("No se encontró la solicitud de soporte seleccionada.")
                    : ResultadoOperacion<TicketCompleto>.Ok(new TicketCompleto { Ticket = ticket });
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<TicketCompleto>.Error(ex.Message);
            }
        }

        // A8/A9: el usuario agrega información al mismo caso en lugar de
        // registrar otro. Si estaba resuelta, vuelve a "en proceso".
        public ResultadoOperacion AgregarMensajeUsuario(int idTicket, int idUsuarioExterno, string mensaje)
        {
            string texto;
            ResultadoOperacion validacion = ValidarMensaje(mensaje, 2, out texto);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                if (ticket == null || ticket.IdUsuarioExterno != idUsuarioExterno)
                {
                    return ResultadoOperacion.Error("No se encontró la solicitud de soporte seleccionada.");
                }

                if (ticket.Estado == EstadoCerrada)
                {
                    return ResultadoOperacion.Error("La solicitud está cerrada y ya no admite mensajes. Si tenés un inconveniente nuevo, registrá otra solicitud.");
                }

                string estadoFinal = _mpp.InsertarMensajeUsuario(new TicketMensaje
                {
                    IdTicket = idTicket,
                    IdUsuarioExterno = idUsuarioExterno,
                    Mensaje = texto
                });

                bool reabierta = ticket.Estado == EstadoResuelta && estadoFinal == EstadoEnProceso;
                RegistrarBitacoraExterna(idUsuarioExterno, "RESPUESTA", idTicket,
                    "Nuevo mensaje del usuario en la solicitud " + ticket.Codigo + " \"" + ticket.Motivo + "\"" +
                    (reabierta ? "; la solicitud resuelta volvió a quedar en proceso." : "."));

                return ResultadoOperacion.Ok(reabierta
                    ? "Tu mensaje se envió y la solicitud volvió a quedar en proceso para que el equipo la revise."
                    : "Tu mensaje se envió correctamente. El equipo de soporte lo va a ver en esta misma solicitud.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(NumeroErrorSql(ex) == 51032
                    ? "La solicitud está cerrada y ya no admite mensajes."
                    : "No pudimos enviar tu mensaje en este momento. Intentá nuevamente.");
            }
        }

        // Reservas del usuario que puede asociar a una solicitud.
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

        // Ítem 12: contador del menú del usuario.
        public int ContarNovedadesUsuario(int idUsuarioExterno)
        {
            try
            {
                return _mpp.ContarNovedadesUsuario(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return 0;
            }
        }

        // ------------------------------------------------------------------
        // CU-003-003: gestión interna
        // ------------------------------------------------------------------

        public ResultadoOperacion<ResumenSoporte> ObtenerResumen()
        {
            try
            {
                return ResultadoOperacion<ResumenSoporte>.Ok(_mpp.ObtenerResumen());
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<ResumenSoporte>.Error("No pudimos cargar las solicitudes de soporte en este momento. Intentá nuevamente.");
            }
        }

        // Pasos 4 a 6, A2, A3 y A4.
        public ResultadoOperacion<PaginaTickets> ListarInterno(FiltroTicket filtro)
        {
            FiltroTicket f = filtro ?? new FiltroTicket();
            f.Estado = Estados.Contains(f.Estado) ? f.Estado : null;
            f.FuncionalidadVinculada = Funcionalidades.Contains(f.FuncionalidadVinculada) ? f.FuncionalidadVinculada : null;
            f.Usuario = Limpiar(f.Usuario);
            if (f.Usuario != null && f.Usuario.Length > 150)
            {
                f.Usuario = f.Usuario.Substring(0, 150);
            }

            if (f.Asignacion != FiltroTicket.AsignacionSinAsignar && !(f.Asignacion == FiltroTicket.AsignacionUsuario && f.IdUsuarioInternoAsignado.HasValue))
            {
                f.Asignacion = null;
                f.IdUsuarioInternoAsignado = null;
            }

            if (f.Desde.HasValue && f.Hasta.HasValue && f.Desde.Value.Date > f.Hasta.Value.Date)
            {
                return ResultadoOperacion<PaginaTickets>.Error("La fecha \"desde\" no puede ser posterior a la fecha \"hasta\".", "A3");
            }

            if (f.Pagina < 1) f.Pagina = 1;
            if (f.TamanioPagina < 1 || f.TamanioPagina > 100) f.TamanioPagina = 20;

            try
            {
                int total = _mpp.ContarParaInterno(f);
                int paginas = Math.Max(1, (total + f.TamanioPagina - 1) / f.TamanioPagina);
                if (f.Pagina > paginas) f.Pagina = paginas;

                return ResultadoOperacion<PaginaTickets>.Ok(new PaginaTickets
                {
                    Tickets = total == 0 ? new List<Ticket>() : _mpp.ListarParaInterno(f),
                    Total = total,
                    Pagina = f.Pagina,
                    TamanioPagina = f.TamanioPagina
                });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<PaginaTickets>.Error("No pudimos cargar las solicitudes de soporte en este momento. Intentá nuevamente.");
            }
        }

        // Pasos 9 y 10, A5, A10 y A11. Abrir el detalle deja leídos los
        // mensajes del usuario para todo el equipo.
        public ResultadoOperacion<TicketCompleto> ObtenerDetalleInterno(int idTicket, bool puedeReabrir)
        {
            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                if (ticket == null)
                {
                    return ResultadoOperacion<TicketCompleto>.Error("No se encontró la solicitud de soporte seleccionada.");
                }

                TicketCompleto completo = ArmarDetalle(ticket, true);
                completo.EstadosPermitidos = EstadosPermitidos(ticket, puedeReabrir);
                if (completo.Novedades > 0 || !ticket.FechaUltimaLecturaSoporte.HasValue)
                {
                    MarcarLeidoSinFallar(ticket, "Soporte");
                }

                return ResultadoOperacion<TicketCompleto>.Ok(completo);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<TicketCompleto>.Error("No pudimos cargar la solicitud en este momento. Intentá nuevamente.");
            }
        }

        // Pasos 12 a 17 y 24 a 26, A6, A7 y A12. estadoVisto es el estado que
        // tenía la pantalla: si otro integrante lo cambió, no se guarda.
        public ResultadoOperacion RegistrarRespuesta(int idTicket, int idUsuarioInterno, string respuesta, bool marcarResuelta, string estadoVisto)
        {
            string texto;
            ResultadoOperacion validacion = ValidarMensaje(respuesta, LongitudMinimaRespuesta, out texto);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion.Error(
                    texto == null ? "Ingresá la respuesta para el usuario antes de guardar." :
                    texto.Length < LongitudMinimaRespuesta ? "La respuesta es demasiado corta: escribí al menos " + LongitudMinimaRespuesta + " caracteres para que el usuario entienda qué se hizo." :
                    validacion.Mensaje, "A6");
            }

            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                if (ticket == null)
                {
                    return ResultadoOperacion.Error("No se encontró la solicitud de soporte seleccionada.");
                }

                if (ticket.Estado == EstadoCerrada)
                {
                    return ResultadoOperacion.Error("La solicitud está cerrada: solo se puede consultar.", "A10");
                }

                if (!string.IsNullOrEmpty(estadoVisto) && estadoVisto != ticket.Estado)
                {
                    return ResultadoOperacion.Error(MensajeCambioConcurrente(ticket.Estado), "A12");
                }

                string estadoNuevo = _mpp.RegistrarRespuesta(
                    new TicketMensaje { IdTicket = idTicket, IdUsuarioInterno = idUsuarioInterno, Mensaje = texto },
                    ticket.Estado, marcarResuelta);

                bool cambioEstado = estadoNuevo != ticket.Estado;
                RegistrarBitacoraInterna(idUsuarioInterno, "RESPUESTA", idTicket,
                    "Respuesta de soporte en la solicitud " + ticket.Codigo + " \"" + ticket.Motivo + "\"" +
                    (cambioEstado ? "; estado: " + TextoEstado(ticket.Estado) + " -> " + TextoEstado(estadoNuevo) + "." : "."));

                // Paso 25 / A7 paso 8.
                _bllNotificacion.Notificar(ticket.IdUsuarioExterno, TipoNotificacion.RespuestaTicket,
                    "Soporte respondió tu solicitud " + ticket.Codigo + " (\"" + ticket.Motivo + "\")" +
                    (estadoNuevo == EstadoResuelta && cambioEstado ? " y la marcó como resuelta." :
                     cambioEstado ? ". Estado: " + TextoEstado(estadoNuevo) + "." : "."),
                    UrlSeguimiento(idTicket));
                EnviarCorreoSinFallar(ticket);

                return ResultadoOperacion.Ok(estadoNuevo == EstadoResuelta && cambioEstado
                    ? "La respuesta se guardó y la solicitud quedó resuelta. El usuario ya puede verla."
                    : "La respuesta se guardó correctamente" + (cambioEstado ? " y la solicitud quedó " + TextoEstado(estadoNuevo).ToLowerInvariant() : string.Empty) + ". El usuario ya puede verla.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ErrorDeActualizacion(ex);
            }
        }

        // Paso 21 y A9 (también lo usa la pantalla para avisar antes de pedir
        // la confirmación).
        public static ResultadoOperacion ValidarCambioEstado(Ticket ticket, string estadoNuevo, bool puedeReabrir)
        {
            if (!Estados.Contains(estadoNuevo))
            {
                return ResultadoOperacion.Error("Elegí uno de los estados disponibles.", "A9");
            }

            if (estadoNuevo == ticket.Estado)
            {
                return ResultadoOperacion.Error("La solicitud ya está en estado \"" + TextoEstado(estadoNuevo) + "\".", "A9");
            }

            if (ticket.Estado == EstadoCerrada && !puedeReabrir)
            {
                return ResultadoOperacion.Error("La solicitud está cerrada: solo se puede consultar. Para reabrirla hace falta el permiso \"Reabrir solicitudes de soporte cerradas\".", "A10");
            }

            string[] destinos;
            if (!Transiciones.TryGetValue(ticket.Estado, out destinos) || !destinos.Contains(estadoNuevo))
            {
                string posibles = destinos == null || destinos.Length == 0
                    ? "ninguno"
                    : UnirConY(destinos.Select(d => "\"" + TextoEstado(d) + "\"").ToList());
                return ResultadoOperacion.Error("No es posible pasar una solicitud de \"" + TextoEstado(ticket.Estado) + "\" a \"" +
                    TextoEstado(estadoNuevo) + "\". Desde \"" + TextoEstado(ticket.Estado) + "\" se puede pasar a " + posibles + ".", "A9");
            }

            if (estadoNuevo == EstadoResuelta && ticket.CantidadRespuestas == 0)
            {
                return ResultadoOperacion.Error("Para marcarla como resuelta primero registrá una respuesta para el usuario (podés usar \"Marcar como resuelta al guardar\").", "A9");
            }

            return ResultadoOperacion.Ok();
        }

        public static List<string> EstadosPermitidos(Ticket ticket, bool puedeReabrir)
        {
            return Estados.Where(e => ValidarCambioEstado(ticket, e, puedeReabrir).Exitoso).ToList();
        }

        // Pasos 18 a 26, A8, A9, A10 (reapertura), A11 (cierre) y A12.
        public ResultadoOperacion CambiarEstado(int idTicket, string estadoNuevo, int idUsuarioInterno, bool puedeReabrir, string estadoVisto)
        {
            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                if (ticket == null)
                {
                    return ResultadoOperacion.Error("No se encontró la solicitud de soporte seleccionada.");
                }

                if (!string.IsNullOrEmpty(estadoVisto) && estadoVisto != ticket.Estado)
                {
                    return ResultadoOperacion.Error(MensajeCambioConcurrente(ticket.Estado), "A12");
                }

                ResultadoOperacion validacion = ValidarCambioEstado(ticket, estadoNuevo, puedeReabrir);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                string detalle = DetalleTransicion(ticket.Estado, estadoNuevo);
                _mpp.CambiarEstado(new TicketActualizacion
                {
                    IdTicket = idTicket,
                    EstadoAnterior = ticket.Estado,
                    EstadoNuevo = estadoNuevo,
                    IdUsuarioInterno = idUsuarioInterno,
                    Detalle = detalle
                });

                string operacion = estadoNuevo == EstadoCerrada ? "CIERRE" : ticket.Estado == EstadoCerrada ? "ACTIVACION" : "MODIFICACION";
                RegistrarBitacoraInterna(idUsuarioInterno, operacion, idTicket,
                    "Cambio de estado de la solicitud " + ticket.Codigo + " \"" + ticket.Motivo + "\": " +
                    TextoEstado(ticket.Estado) + " -> " + TextoEstado(estadoNuevo) + ".");

                _bllNotificacion.Notificar(ticket.IdUsuarioExterno, TipoNotificacion.SolicitudSoporteActualizada,
                    "Tu solicitud " + ticket.Codigo + " (\"" + ticket.Motivo + "\") pasó a \"" + TextoEstado(estadoNuevo) + "\".",
                    UrlSeguimiento(idTicket));

                if (estadoNuevo == EstadoCerrada)
                {
                    return ResultadoOperacion.Ok("La solicitud " + ticket.Codigo + " se cerró correctamente.");
                }

                return ResultadoOperacion.Ok("El estado se actualizó correctamente: la solicitud " + ticket.Codigo + " ahora está " +
                    TextoEstado(estadoNuevo).ToLowerInvariant() + ".");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ErrorDeActualizacion(ex);
            }
        }

        // Ítem 12: asignar un responsable (o quitarlo).
        public ResultadoOperacion Asignar(int idTicket, int? idUsuarioInternoAsignado, int idUsuarioInternoResponsable)
        {
            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                if (ticket == null)
                {
                    return ResultadoOperacion.Error("No se encontró la solicitud de soporte seleccionada.");
                }

                if (ticket.Estado == EstadoCerrada)
                {
                    return ResultadoOperacion.Error("La solicitud está cerrada: no se puede cambiar el responsable.", "A10");
                }

                if (ticket.IdUsuarioInternoAsignado == idUsuarioInternoAsignado)
                {
                    return ResultadoOperacion.Error(idUsuarioInternoAsignado.HasValue
                        ? "La solicitud ya está asignada a esa persona."
                        : "La solicitud no tiene responsable asignado.");
                }

                UsuarioInterno responsable = null;
                if (idUsuarioInternoAsignado.HasValue)
                {
                    responsable = ListarResponsables().FirstOrDefault(u => u.IdUsuarioInterno == idUsuarioInternoAsignado.Value);
                    if (responsable == null)
                    {
                        return ResultadoOperacion.Error("Elegí un integrante activo del equipo con permiso para gestionar solicitudes de soporte.");
                    }
                }

                _mpp.Asignar(new Ticket { IdTicket = idTicket, IdUsuarioInternoAsignado = idUsuarioInternoAsignado }, idUsuarioInternoResponsable, false);

                RegistrarBitacoraInterna(idUsuarioInternoResponsable, "ASOCIACION", idTicket,
                    responsable == null
                        ? "Se quitó el responsable de la solicitud " + ticket.Codigo + "."
                        : "Solicitud " + ticket.Codigo + " asignada a " + responsable.Nombre + " " + responsable.Apellido + ".");

                return ResultadoOperacion.Ok(responsable == null
                    ? "La solicitud quedó sin responsable asignado."
                    : "La solicitud quedó asignada a " + responsable.Nombre + " " + responsable.Apellido + ".");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ErrorDeActualizacion(ex);
            }
        }

        // Ítem 12: "Tomar solicitud" (asignármela; si estaba pendiente pasa a
        // en proceso y se avisa al usuario).
        public ResultadoOperacion Tomar(int idTicket, int idUsuarioInterno)
        {
            try
            {
                Ticket ticket = _mpp.ObtenerPorId(new Ticket { IdTicket = idTicket });
                if (ticket == null)
                {
                    return ResultadoOperacion.Error("No se encontró la solicitud de soporte seleccionada.");
                }

                if (ticket.Estado == EstadoCerrada)
                {
                    return ResultadoOperacion.Error("La solicitud está cerrada: solo se puede consultar.", "A10");
                }

                if (ticket.IdUsuarioInternoAsignado == idUsuarioInterno)
                {
                    return ResultadoOperacion.Error("La solicitud ya está asignada a vos.");
                }

                string estadoFinal = _mpp.Asignar(new Ticket { IdTicket = idTicket, IdUsuarioInternoAsignado = idUsuarioInterno }, idUsuarioInterno, true);

                RegistrarBitacoraInterna(idUsuarioInterno, "ASOCIACION", idTicket,
                    "Se tomó la solicitud " + ticket.Codigo + " \"" + ticket.Motivo + "\"" +
                    (estadoFinal != ticket.Estado ? "; estado: " + TextoEstado(ticket.Estado) + " -> " + TextoEstado(estadoFinal) + "." : "."));

                if (estadoFinal != ticket.Estado)
                {
                    _bllNotificacion.Notificar(ticket.IdUsuarioExterno, TipoNotificacion.SolicitudSoporteActualizada,
                        "Tu solicitud " + ticket.Codigo + " (\"" + ticket.Motivo + "\") pasó a \"" + TextoEstado(estadoFinal) + "\": el equipo de soporte ya la está atendiendo.",
                        UrlSeguimiento(idTicket));
                }

                return ResultadoOperacion.Ok("Tomaste la solicitud " + ticket.Codigo +
                    (estadoFinal != ticket.Estado ? " y quedó en proceso." : "."));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ErrorDeActualizacion(ex);
            }
        }

        // Integrantes activos del equipo con permiso de gestionar soporte.
        public List<UsuarioInterno> ListarResponsables()
        {
            BLL_PermisoInterno bllPermiso = new BLL_PermisoInterno();
            Dictionary<int, bool> rolTienePermiso = new Dictionary<int, bool>();
            List<UsuarioInterno> resultado = new List<UsuarioInterno>();
            foreach (UsuarioInterno usuario in new BLL_UsuarioInterno().Listar())
            {
                if (!usuario.Activo)
                {
                    continue;
                }

                bool tiene;
                if (!rolTienePermiso.TryGetValue(usuario.IdRolInterno, out tiene))
                {
                    try
                    {
                        tiene = bllPermiso.ListarCodigosPermisosDeRol(usuario.IdRolInterno).Contains(PermisoGestionar);
                    }
                    catch (ErrorAccesoDatosException)
                    {
                        tiene = false;
                    }

                    rolTienePermiso[usuario.IdRolInterno] = tiene;
                }

                if (tiene)
                {
                    resultado.Add(usuario);
                }
            }

            return resultado.OrderBy(u => u.Apellido).ThenBy(u => u.Nombre).ToList();
        }

        // Contador del menú del panel interno: solicitudes que requieren
        // atención (pendientes o con mensajes del usuario sin leer).
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

        // ------------------------------------------------------------------
        // Ítem 12: actualización periódica. La "marca" cambia cada vez que la
        // solicitud (o el listado) tiene actividad; las pantallas la comparan
        // con la que tenían al cargarse.
        // ------------------------------------------------------------------

        public string ObtenerMarcaSolicitud(int idTicket, int? idUsuarioExterno)
        {
            try
            {
                Ticket marca = _mpp.ObtenerMarca(new Ticket { IdTicket = idTicket });
                if (marca == null || (idUsuarioExterno.HasValue && marca.IdUsuarioExterno != idUsuarioExterno.Value))
                {
                    return null;
                }

                return marca.Estado + "-" + marca.FechaUltimaActividad.Ticks;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public string ObtenerMarcaListado(int? idUsuarioExterno)
        {
            try
            {
                KeyValuePair<int, DateTime?> marca = _mpp.ObtenerMarcaListado(
                    idUsuarioExterno.HasValue ? new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno.Value } : null);
                return marca.Key + "-" + (marca.Value.HasValue ? marca.Value.Value.Ticks : 0);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        // Marca como nuevo lo posterior a la última lectura de quien mira, y
        // como leído por la otra parte lo anterior a su última lectura.
        private TicketCompleto ArmarDetalle(Ticket ticket, bool paraSoporte)
        {
            DateTime miLectura = (paraSoporte ? ticket.FechaUltimaLecturaSoporte : ticket.FechaUltimaLecturaUsuario) ?? DateTime.MinValue;
            DateTime suLectura = (paraSoporte ? ticket.FechaUltimaLecturaUsuario : ticket.FechaUltimaLecturaSoporte) ?? DateTime.MinValue;

            List<TicketMensaje> mensajes = _mpp.ListarMensajesPorTicket(ticket);
            foreach (TicketMensaje mensaje in mensajes)
            {
                bool deLaOtraParte = paraSoporte ? !mensaje.EsInterno : mensaje.EsInterno;
                mensaje.EsNuevo = deLaOtraParte && mensaje.FechaEnvio > miLectura;
                mensaje.LeidoPorLaOtraParte = !deLaOtraParte && mensaje.FechaEnvio <= suLectura;
            }

            List<TicketActualizacion> actualizaciones = _mpp.ListarActualizacionesPorTicket(ticket);
            foreach (TicketActualizacion actualizacion in actualizaciones)
            {
                bool deLaOtraParte = paraSoporte ? !actualizacion.EsInterno : actualizacion.EsInterno;
                actualizacion.EsNuevo = deLaOtraParte && actualizacion.TipoActualizacion == "CambioEstado" && actualizacion.Fecha > miLectura;
            }

            return new TicketCompleto
            {
                Ticket = ticket,
                Mensajes = mensajes,
                Actualizaciones = actualizaciones,
                Novedades = paraSoporte ? ticket.NovedadesSoporte : ticket.NovedadesUsuario
            };
        }

        private void MarcarLeidoSinFallar(Ticket ticket, string lado)
        {
            try
            {
                _mpp.MarcarLeido(ticket, lado);
            }
            catch (ErrorAccesoDatosException)
            {
                // Que no se marque como leído no impide ver la solicitud.
            }
        }

        private static string DetalleTransicion(string anterior, string nuevo)
        {
            if (anterior == EstadoCerrada) return "Se reabrió la solicitud cerrada.";
            if (anterior == EstadoResuelta && nuevo == EstadoEnProceso) return "La solicitud resuelta volvió a quedar en proceso.";
            if (nuevo == EstadoEnProceso) return "La solicitud está siendo atendida por el equipo de soporte.";
            if (nuevo == EstadoResuelta) return "Solicitud marcada como resuelta.";
            if (nuevo == EstadoCerrada) return "Solicitud cerrada.";
            return null;
        }

        private static string MensajeCambioConcurrente(string estadoActual)
        {
            return "La solicitud cambió mientras la estabas viendo (ahora está \"" + TextoEstado(estadoActual) +
                "\"). Revisá la información actualizada e intentá nuevamente.";
        }

        // A12: la solicitud mantiene la última información guardada.
        private static ResultadoOperacion ErrorDeActualizacion(ErrorAccesoDatosException ex)
        {
            switch (NumeroErrorSql(ex))
            {
                case 51031:
                    return ResultadoOperacion.Error("La solicitud cambió mientras la estabas viendo. Revisá la información actualizada e intentá nuevamente.", "A12");
                case 51032:
                    return ResultadoOperacion.Error("La solicitud está cerrada: solo se puede consultar.", "A10");
                default:
                    return ResultadoOperacion.Error(MensajeErrorActualizacion, "A12");
            }
        }

        private static int NumeroErrorSql(ErrorAccesoDatosException ex)
        {
            return ex.NumeroErrorSql;
        }

        private static ResultadoOperacion ValidarMensaje(string mensaje, int longitudMinima, out string texto)
        {
            texto = Limpiar(mensaje);
            if (texto == null)
            {
                return ResultadoOperacion.Error("Escribí un mensaje antes de enviar.");
            }

            if (texto.Length < longitudMinima)
            {
                return ResultadoOperacion.Error("El mensaje es demasiado corto: escribí al menos " + longitudMinima + " caracteres.");
            }

            if (texto.Length > LongitudMaximaMensaje)
            {
                return ResultadoOperacion.Error("El mensaje no puede superar los " + LongitudMaximaMensaje + " caracteres.");
            }

            return ResultadoOperacion.Ok();
        }

        private void EnviarCorreoSinFallar(Ticket ticket)
        {
            try
            {
                _servicioCorreo.EnviarNotificacionRespuestaTicket(ticket.CorreoUsuarioExterno, ticket.NombreUsuarioExterno,
                    ticket.Codigo + " - " + ticket.Motivo);
            }
            catch (Exception)
            {
                // Un correo que no sale no invalida la respuesta ya guardada.
            }
        }

        private void RegistrarBitacoraExterna(int idUsuarioExterno, string operacion, int idTicket, string descripcion)
        {
            try
            {
                _bitacora.Registrar(idUsuarioExterno, operacion, TipoEntidadBitacora, idTicket, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }

        private void RegistrarBitacoraInterna(int idUsuarioInterno, string operacion, int idTicket, string descripcion)
        {
            try
            {
                _bitacora.RegistrarInterno(idUsuarioInterno, operacion, TipoEntidadBitacora, idTicket, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }

        private static string UrlSeguimiento(int idTicket)
        {
            return "~/Soporte.aspx?ver=" + idTicket;
        }

        private static string Limpiar(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }

        private static string UnirConY(List<string> partes)
        {
            if (partes.Count == 1)
            {
                return partes[0];
            }

            return string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[partes.Count - 1];
        }
    }
}
