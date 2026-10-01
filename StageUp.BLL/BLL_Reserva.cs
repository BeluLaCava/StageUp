using System;
using System.Collections.Generic;
using System.Globalization;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    public class BLL_Reserva
    {
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();
        private readonly MPP_Calificacion _mppCalificacion = new MPP_Calificacion();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_Notificacion _notificacion = new BLL_Notificacion();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();
        private readonly BLL_ParametroPlataforma _bllParametros = new BLL_ParametroPlataforma();

        private const int LongitudMaximaComentario = 1000;
        private const string TipoEntidadBitacora = "Reserva";

        // Throttle en memoria para GenerarRecordatorios24hsSiCorresponde: no
        // hay SQL Server Agent disponible en la edición Express, así que en
        // vez de un job programado, el chequeo se dispara oportunísticamente
        // desde Site.Master en cada pageview autenticado, pero solo hace el
        // trabajo real (la consulta a la base) una vez cada
        // IntervaloMinutosRecordatorios minutos por proceso.
        private static DateTime? _ultimaGeneracionRecordatorios;
        private static readonly object _lockRecordatorios = new object();
        private const int IntervaloMinutosRecordatorios = 15;

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

                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
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
                Reserva reserva = new Reserva
                {
                    IdEspacioArtistico = idEspacioArtistico,
                    IdUsuarioExternoSolicitante = idUsuarioExternoSolicitante,
                    FechaSolicitada = fechaSolicitada.Date,
                    MinutoDesde = minutoDesde
                };

                if (minutoDesde.HasValue && duracionMinutos.HasValue)
                {
                    ResultadoOperacion validacionHorario = ValidarHorarioSolicitado(
                        espacio, fechaSolicitada.Date, minutoDesde.Value, duracionMinutos.Value);
                    if (!validacionHorario.Exitoso)
                    {
                        return ResultadoOperacion<int>.Error(validacionHorario.Mensaje);
                    }

                    minutoHasta = minutoDesde.Value + duracionMinutos.Value;
                    reserva.MinutoHasta = minutoHasta;

                    if (_mppReserva.ExisteSolapamiento(reserva))
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

                reserva.ComentarioSolicitante = comentarioLimpio;
                reserva.MinutoHasta = minutoHasta;
                reserva.PrecioHoraPactado = precioHoraPactado;
                reserva.Moneda = moneda;
                reserva.ImporteEstimado = importeEstimado;

                int idReserva = _mppReserva.Insertar(reserva);

                _bitacora.Registrar(
                    idUsuarioExternoSolicitante, "ALTA", TipoEntidadBitacora, idReserva,
                    "Solicitud de reserva para el espacio \"" + espacio.NombreEspacio + "\" (" +
                    fechaSolicitada.Date.ToString("dd/MM/yyyy") +
                    (minutoDesde.HasValue && duracionMinutos.HasValue
                        ? " a las " + FormatearHora(minutoDesde.Value) + ", " + FormatearDuracion(duracionMinutos.Value)
                        : string.Empty) + ").");

                _notificacion.Notificar(
                    espacio.IdUsuarioGestor, TipoNotificacion.SolicitudReserva,
                    "Recibiste una nueva solicitud de reserva para \"" + espacio.NombreEspacio + "\".",
                    "~/SolicitudesRecibidas.aspx");

                // Ítem 5A: aviso por mail al solicitante (solicitud enviada) y
                // al gestor (solicitud recibida).
                reserva.IdReserva = idReserva;
                reserva.NombreEspacio = espacio.NombreEspacio;
                UsuarioExterno solicitante = ObtenerUsuarioParaCorreo(idUsuarioExternoSolicitante);
                UsuarioExterno gestor = ObtenerUsuarioParaCorreo(espacio.IdUsuarioGestor);
                EnviarCorreoSinBloquear(solicitante, u =>
                    _servicioCorreo.EnviarSolicitudReservaEnviada(u.CorreoElectronico, u.Nombre, reserva));
                EnviarCorreoSinBloquear(gestor, u =>
                    _servicioCorreo.EnviarSolicitudReservaRecibida(u.CorreoElectronico, u.Nombre, reserva, NombreCompleto(solicitante)));

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

            // Misma regla para decidir qué franjas aplican a esta fecha
            // (si hay una franja de fecha puntual para ese día, manda por
            // sobre la recurrencia semanal) — se usa tanto para ver si hay
            // disponibilidad como para ver si hay un bloqueo, así las dos
            // lecturas quedan consistentes entre sí.
            Func<FranjaEspacio, bool> aplicaAEstaFecha = franja =>
                tieneExcepcionParaFecha
                    ? string.Equals(franja.Fecha, fechaTexto, StringComparison.Ordinal)
                    : string.IsNullOrEmpty(franja.Fecha) && franja.DiaSemana == diaSemana;

            bool estaDisponible = franjas.Exists(franja =>
                franja != null &&
                !franja.Bloqueado &&
                aplicaAEstaFecha(franja) &&
                minutoDesde >= franja.MinutoDesde &&
                minutoHasta <= franja.MinutoHasta);

            if (!estaDisponible)
            {
                return ResultadoOperacion.Error("La franja elegida no está dentro de la disponibilidad informada para esa fecha.");
            }

            // Ítem 27 del checklist de correcciones: no alcanza con que el
            // horario esté contenido en una franja disponible más amplia
            // (ej. disponible miércoles 9 a 22) — también hay que rechazar
            // si se superpone con una franja bloqueada por una actividad
            // dentro de esa misma ventana (ej. actividad miércoles 10 a
            // 12), que antes se podía pisar igual (reservando 10 a 11).
            bool existeBloqueoSuperpuesto = franjas.Exists(franja =>
                franja != null &&
                franja.Bloqueado &&
                aplicaAEstaFecha(franja) &&
                minutoDesde < franja.MinutoHasta &&
                franja.MinutoDesde < minutoHasta);

            return existeBloqueoSuperpuesto
                ? ResultadoOperacion.Error("Ese horario ya está bloqueado por una actividad de este espacio. Elegí otro horario.")
                : ResultadoOperacion.Ok();
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

        // Reservas Pendientes o Aceptadas, desde hoy en adelante, para un
        // espacio puntual. Pensado para que la UI pública (el planificador
        // de DetalleEspacio) pueda descontar visualmente estos horarios de
        // la disponibilidad publicada por el gestor (ítems 24/25 del
        // checklist de correcciones) — reutiliza la misma consulta que ya
        // usa BLL_Actividad.ExisteConflictoConReservas.
        public List<Reserva> ListarOcupacionVigente(int idEspacioArtistico)
        {
            try
            {
                return _mppReserva.ListarActivasPorEspacio(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Reserva>();
            }
        }

        public List<Reserva> ListarMisReservas(int idUsuarioExternoSolicitante)
        {
            try
            {
                ProcesarPagosVencidos();
                _mppReserva.FinalizarVencidas();
                return _mppReserva.ListarPorSolicitante(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioExternoSolicitante });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Reserva>();
            }
        }

        // ------------------------------------------------------------------
        // Historial y seguimiento de reservas (ítems 6A y 6D de la segunda
        // entrega): "Mis reservas" funciona como historial completo, con
        // filtro por estado, y cada reserva muestra su línea de tiempo.
        // ------------------------------------------------------------------
        public const string FiltroTodas = "Todas";
        public const string FiltroPendientes = "Pendientes";
        public const string FiltroPendientesDePago = "PendientesDePago";
        public const string FiltroProximas = "Proximas";
        public const string FiltroFinalizadas = "Finalizadas";
        public const string FiltroSinCalificar = "SinCalificar";
        public const string FiltroRechazadas = "Rechazadas";
        public const string FiltroCanceladas = "Canceladas";

        public List<Reserva> ListarMisReservas(int idUsuarioExternoSolicitante, string filtro)
        {
            List<Reserva> todas = ListarMisReservas(idUsuarioExternoSolicitante);
            List<Reserva> filtradas = new List<Reserva>();
            foreach (Reserva reserva in todas)
            {
                if (CumpleFiltroHistorial(reserva, filtro))
                {
                    filtradas.Add(reserva);
                }
            }
            return filtradas;
        }

        private static bool CumpleFiltroHistorial(Reserva reserva, string filtro)
        {
            string estado = reserva.EstadoReserva;
            switch (filtro)
            {
                case FiltroPendientes:
                    return estado == EstadoReserva.Pendiente.ToString();
                case FiltroPendientesDePago:
                    return BLL_Pago.EsperaPago(reserva);
                case FiltroProximas:
                    return estado == EstadoReserva.Aceptada.ToString();
                case FiltroFinalizadas:
                    return estado == EstadoReserva.Finalizada.ToString();
                case FiltroSinCalificar:
                    return estado == EstadoReserva.Finalizada.ToString() && !reserva.CalificacionEspacioRealizada;
                case FiltroRechazadas:
                    return estado == EstadoReserva.Rechazada.ToString();
                case FiltroCanceladas:
                    return estado == EstadoReserva.Cancelada.ToString();
                default:
                    return true;
            }
        }

        // Línea de tiempo de una reserva. Los pasos futuros quedan como
        // "Pendiente"; el paso en el que está la reserva ahora, como "Actual";
        // un rechazo, una cancelación o un pago vencido cortan la línea con un
        // paso "Interrumpido". Desde el módulo de pagos (script 49), entre la
        // aceptación y el día de la reserva está el paso del pago; las
        // reservas anteriores (estadoPago = NoRequerido) no lo muestran.
        public static List<PasoSeguimientoReserva> ConstruirSeguimiento(Reserva reserva)
        {
            return ConstruirSeguimiento(reserva, false);
        }

        public static List<PasoSeguimientoReserva> ConstruirSeguimientoGestor(Reserva reserva)
        {
            return ConstruirSeguimiento(reserva, true);
        }

        private static List<PasoSeguimientoReserva> ConstruirSeguimiento(Reserva reserva, bool paraGestor)
        {
            List<PasoSeguimientoReserva> pasos = new List<PasoSeguimientoReserva>();
            if (reserva == null)
            {
                return pasos;
            }

            string estado = reserva.EstadoReserva;
            string estadoPago = reserva.EstadoPago ?? BLL_Pago.EstadoNoRequerido;
            bool conPago = estadoPago != BLL_Pago.EstadoNoRequerido;
            DateTime inicioReserva = reserva.FechaSolicitada.Date.AddMinutes(reserva.MinutoDesde ?? 0);
            string momentoReserva = reserva.MinutoDesde.HasValue
                ? reserva.FechaSolicitada.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " a las " + FormatearHora(reserva.MinutoDesde.Value)
                : reserva.FechaSolicitada.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            pasos.Add(Paso(
                paraGestor ? "Solicitud recibida" : "Solicitud enviada",
                paraGestor ? "Recibiste una solicitud para reservar tu espacio." : "Le enviaste la solicitud al gestor del espacio.",
                reserva.FechaCreacion, "Completado"));

            if (estado == EstadoReserva.Pendiente.ToString())
            {
                pasos.Add(Paso("Revisión del gestor",
                    paraGestor ? "Tenés que aceptar o rechazar la solicitud." : "Esperando que el gestor acepte o rechace la solicitud.",
                    null, "Actual"));
                pasos.Add(Paso("Pago", null, null, "Pendiente"));
                pasos.Add(Paso("Día de la reserva", momentoReserva, null, "Pendiente"));
                pasos.Add(Paso("Calificación", null, null, "Pendiente"));
                return pasos;
            }

            if (estado == EstadoReserva.Rechazada.ToString())
            {
                pasos.Add(Paso(paraGestor ? "Solicitud rechazada" : "Solicitud rechazada",
                    paraGestor
                        ? (string.IsNullOrEmpty(reserva.ComentarioResolucion)
                            ? "Rechazaste la solicitud."
                            : "Rechazaste la solicitud: \"" + reserva.ComentarioResolucion + "\"")
                        : (string.IsNullOrEmpty(reserva.ComentarioResolucion)
                            ? "El gestor no aceptó la solicitud."
                            : "El gestor no aceptó la solicitud: \"" + reserva.ComentarioResolucion + "\""),
                    reserva.FechaResolucion, "Interrumpido"));
                return pasos;
            }

            bool fueAceptada = estado == EstadoReserva.Aceptada.ToString()
                || estado == EstadoReserva.Finalizada.ToString()
                || (estado == EstadoReserva.Cancelada.ToString() && reserva.FechaResolucion.HasValue);

            if (fueAceptada)
            {
                pasos.Add(conPago
                    ? Paso("Reserva aceptada", paraGestor ? "Aceptaste la solicitud." : "El gestor aceptó la solicitud.", reserva.FechaResolucion, "Completado")
                    : Paso("Reserva confirmada", paraGestor ? "Aceptaste la solicitud." : "El gestor aceptó la solicitud.", reserva.FechaResolucion, "Completado"));
            }

            if (fueAceptada && conPago)
            {
                if (estadoPago == BLL_Pago.EstadoPagado || estadoPago == BLL_Pago.EstadoDevuelto)
                {
                    pasos.Add(Paso("Pago recibido",
                        paraGestor ? "El solicitante pagó la reserva y quedó confirmada." : "La reserva quedó confirmada.",
                        reserva.FechaPago, "Completado"));
                }
                else if (estadoPago == BLL_Pago.EstadoVencido)
                {
                    pasos.Add(Paso("Pago vencido",
                        paraGestor ? "El solicitante no pagó dentro del plazo y la reserva se canceló sin cargo." : "No se pagó dentro del plazo y la reserva se canceló sin cargo.",
                        reserva.FechaCancelacion, "Interrumpido"));
                    return pasos;
                }
                else if (estado == EstadoReserva.Aceptada.ToString())
                {
                    string limite = reserva.FechaLimitePago.HasValue
                        ? " antes del " + reserva.FechaLimitePago.Value.ToString("dd/MM/yyyy 'a las' HH:mm", CultureInfo.InvariantCulture) + " hs"
                        : string.Empty;
                    pasos.Add(Paso("Pago",
                        paraGestor ? "Esperando el pago del solicitante" + limite + ". Si no paga a tiempo, se cancela sola." : "Pagala" + limite + " para confirmarla. Si no, se cancela sola, sin cargo.",
                        reserva.FechaLimitePago, "Actual"));
                    pasos.Add(Paso("Día de la reserva", momentoReserva, null, "Pendiente"));
                    pasos.Add(Paso("Calificación", null, null, "Pendiente"));
                    return pasos;
                }
            }

            if (estado == EstadoReserva.Cancelada.ToString())
            {
                string detalle = reserva.ComisionAplicada && reserva.ImporteComision.HasValue
                    ? (paraGestor ? "La reserva fue cancelada. Se aplicó una comisión de " : "Cancelaste la reserva. Se aplicó una comisión de ") +
                      reserva.ImporteComision.Value.ToString("0.##", CultureInfo.InvariantCulture) + " " + (reserva.Moneda ?? "ARS") + "."
                    : (paraGestor ? "La reserva fue cancelada, sin costo." : "Cancelaste la reserva, sin costo.");
                if (estadoPago == BLL_Pago.EstadoDevuelto)
                {
                    detalle += paraGestor
                        ? " El importe quedó como saldo a favor del solicitante."
                        : " Lo que pagaste quedó como saldo a favor en tu cuenta corriente.";
                }

                pasos.Add(Paso("Reserva cancelada", detalle, reserva.FechaCancelacion, "Interrumpido"));
                return pasos;
            }

            if (estado == EstadoReserva.Aceptada.ToString())
            {
                int dias = (int)Math.Ceiling((inicioReserva - DateTime.Now).TotalDays);
                string faltan = dias <= 0 ? "Es hoy." : dias == 1 ? "Falta 1 día." : "Faltan " + dias + " días.";
                pasos.Add(Paso("Día de la reserva",
                    paraGestor ? "La reserva está programada para " + momentoReserva + ". " + faltan : momentoReserva + ". " + faltan,
                    inicioReserva, "Actual"));
                pasos.Add(Paso("Reserva finalizada", null, null, "Pendiente"));
                pasos.Add(Paso("Calificación", null, null, "Pendiente"));
                return pasos;
            }

            // Finalizada
            pasos.Add(Paso("Día de la reserva", momentoReserva, inicioReserva, "Completado"));
            pasos.Add(Paso("Reserva finalizada", null, reserva.FechaFinalizacion, "Completado"));
            pasos.Add(paraGestor
                ? (reserva.CalificacionSolicitanteRealizada
                    ? Paso("Calificación", "Ya calificaste al solicitante.", null, "Completado")
                    : Paso("Calificación", "Calificá al solicitante para dejar registro de la experiencia.", null, "Actual"))
                : (reserva.CalificacionEspacioRealizada
                    ? Paso("Calificación", "Ya calificaste el espacio. ¡Gracias!", null, "Completado")
                    : Paso("Calificación", "Calificá el espacio para ayudar a otros artistas.", null, "Actual")));
            return pasos;
        }

        private static PasoSeguimientoReserva Paso(string titulo, string detalle, DateTime? fecha, string estado)
        {
            return new PasoSeguimientoReserva { Titulo = titulo, Detalle = detalle, Fecha = fecha, Estado = estado };
        }

        public List<Reserva> ListarSolicitudesRecibidas(int idUsuarioGestor)
        {
            try
            {
                ProcesarPagosVencidos();
                _mppReserva.FinalizarVencidas();
                List<Reserva> solicitudes = _mppReserva.ListarPorGestor(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor });
                CompletarReputacionSolicitantes(solicitudes);
                return solicitudes;
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Reserva>();
            }
        }

        public int ContarSolicitudesPendientes(int idUsuarioGestor)
        {
            try
            {
                _mppReserva.FinalizarVencidas();
                List<Reserva> solicitudes = _mppReserva.ListarPorGestor(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor });
                int cantidadPendientes = 0;

                foreach (Reserva solicitud in solicitudes)
                {
                    if (solicitud.EstadoReserva == EstadoReserva.Pendiente.ToString())
                    {
                        cantidadPendientes++;
                    }
                }

                return cantidadPendientes;
            }
            catch (ErrorAccesoDatosException)
            {
                return 0;
            }
        }

        // Ítem 36 del checklist de correcciones: esta versión resuelve la
        // reputación de TODOS los solicitantes distintos de "solicitudes" en
        // un puñado fijo de consultas (una por cada dato que hace falta),
        // en vez de repetir 3 consultas por cada solicitante distinto como
        // hacía antes (patrón N+1 — con muchas solicitudes de muchos
        // solicitantes distintos, eso significaba muchísimas idas y vueltas
        // a la base para una sola pantalla de "Solicitudes recibidas").
        private void CompletarReputacionSolicitantes(List<Reserva> solicitudes)
        {
            HashSet<int> idsDistintos = new HashSet<int>();
            foreach (Reserva solicitud in solicitudes)
            {
                idsDistintos.Add(solicitud.IdUsuarioExternoSolicitante);
            }
            List<int> idsSolicitantes = new List<int>(idsDistintos);

            Dictionary<int, ResumenReputacion> resumenes = new Dictionary<int, ResumenReputacion>();
            try
            {
                resumenes = _mppCalificacion.ListarResumenesUsuarios();
            }
            catch (ErrorAccesoDatosException)
            {
            }

            Dictionary<int, UsuarioExterno> usuarios = new Dictionary<int, UsuarioExterno>();
            try
            {
                foreach (UsuarioExterno usuario in _mppUsuario.ListarPorIds(idsSolicitantes))
                {
                    usuarios[usuario.IdUsuarioExterno] = usuario;
                }
            }
            catch (ErrorAccesoDatosException)
            {
            }

            Dictionary<int, int> reservasAceptadas;
            try
            {
                reservasAceptadas = _mppReserva.ContarAceptadasPorSolicitantes(idsSolicitantes);
            }
            catch (ErrorAccesoDatosException)
            {
                reservasAceptadas = new Dictionary<int, int>();
            }

            Dictionary<int, List<Calificacion>> calificacionesPorUsuario;
            try
            {
                calificacionesPorUsuario = _mppCalificacion.ListarRecibidasTop3PorUsuarios(idsSolicitantes);
            }
            catch (ErrorAccesoDatosException)
            {
                calificacionesPorUsuario = new Dictionary<int, List<Calificacion>>();
            }

            foreach (Reserva solicitud in solicitudes)
            {
                int idSolicitante = solicitud.IdUsuarioExternoSolicitante;

                UsuarioExterno solicitante;
                if (usuarios.TryGetValue(idSolicitante, out solicitante) && solicitante != null)
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

                int cantidadAceptadas;
                solicitud.CantidadReservasAceptadasSolicitante =
                    reservasAceptadas.TryGetValue(idSolicitante, out cantidadAceptadas) ? cantidadAceptadas : 0;

                List<Calificacion> calificaciones;
                solicitud.CalificacionesSolicitante =
                    calificacionesPorUsuario.TryGetValue(idSolicitante, out calificaciones)
                        ? calificaciones
                        : new List<Calificacion>();

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

                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReserva });
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
                reserva.ComentarioResolucion = comentarioLimpio;

                // Módulo de pagos: si la reserva tiene importe, el cliente
                // tiene HorasLimitePago para pagarla (o hasta que empiece, si
                // es antes). Si no paga, sp_Reserva_VencerPagosPendientes la
                // cancela sin cargo.
                bool requierePago = reserva.ImporteEstimado.HasValue && reserva.ImporteEstimado.Value > 0;
                if (requierePago)
                {
                    DateTime inicioReserva = reserva.FechaSolicitada.Date.AddMinutes(reserva.MinutoDesde ?? 0);
                    DateTime limite = DateTime.Now.AddHours(_bllParametros.ObtenerEntero(BLL_ParametroPlataforma.HorasLimitePago));
                    reserva.FechaLimitePago = inicioReserva < limite ? inicioReserva : limite;
                }

                bool aceptada = _mppReserva.AceptarSiDisponible(reserva);
                if (!aceptada)
                {
                    return ResultadoOperacion.Error(
                        "No se pudo aceptar la solicitud: ya no está pendiente o el horario dejó de estar " +
                        "disponible (es posible que hayas aceptado otra reserva para el mismo horario).");
                }

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idReserva,
                    "El gestor aceptó la solicitud de reserva del espacio \"" + reserva.NombreEspacio + "\".");

                reserva.EstadoPago = requierePago ? BLL_Pago.EstadoPendiente : BLL_Pago.EstadoNoRequerido;
                string textoLimite = requierePago
                    ? reserva.FechaLimitePago.Value.ToString("dd/MM/yyyy 'a las' HH:mm", CultureInfo.InvariantCulture) + " hs"
                    : null;

                _notificacion.Notificar(
                    reserva.IdUsuarioExternoSolicitante, TipoNotificacion.ReservaAceptada,
                    requierePago
                        ? "Tu reserva para \"" + reserva.NombreEspacio + "\" fue aceptada. Pagala antes del " + textoLimite + " para confirmarla."
                        : "Tu reserva para \"" + reserva.NombreEspacio + "\" fue aceptada.",
                    "~/MisReservas.aspx");

                EnviarCorreoSinBloquear(ObtenerUsuarioParaCorreo(reserva.IdUsuarioExternoSolicitante), u =>
                    _servicioCorreo.EnviarReservaAceptada(u.CorreoElectronico, u.Nombre, reserva));

                return ResultadoOperacion.Ok(requierePago
                    ? "Aceptaste la solicitud. El solicitante tiene hasta el " + textoLimite + " para pagarla; te avisamos cuando lo haga."
                    : "Aceptaste la solicitud. La reserva quedó confirmada.");
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

                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReserva });
                ResultadoOperacion validacion = ValidarPropiedadGestor(reserva, idUsuarioGestorSolicitante);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                reserva.EstadoReserva = nuevoEstado.ToString();
                reserva.ComentarioResolucion = string.IsNullOrWhiteSpace(comentarioResolucion)
                    ? null
                    : comentarioResolucion.Trim();
                _mppReserva.Resolver(reserva);

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idReserva,
                    "El gestor " + verboBitacora + " la solicitud de reserva del espacio \"" + reserva.NombreEspacio + "\".");

                if (nuevoEstado == EstadoReserva.Rechazada)
                {
                    _notificacion.Notificar(
                        reserva.IdUsuarioExternoSolicitante, TipoNotificacion.ReservaRechazada,
                        "Tu reserva para \"" + reserva.NombreEspacio + "\" fue rechazada.",
                        "~/MisReservas.aspx");

                    EnviarCorreoSinBloquear(ObtenerUsuarioParaCorreo(reserva.IdUsuarioExternoSolicitante), u =>
                        _servicioCorreo.EnviarReservaRechazada(u.CorreoElectronico, u.Nombre, reserva));
                }

                return ResultadoOperacion.Ok(mensajeExito);
            });
        }

        // Penalidad que correspondería si el cliente cancela ahora (0 si no
        // corresponde). Con el módulo de pagos, solo se cobra penalidad si la
        // reserva ya estaba pagada (o es anterior al módulo); cancelar una
        // reserva aceptada que todavía no se pagó no tiene costo.
        public decimal CalcularPenalidadCancelacion(Reserva reserva)
        {
            if (reserva == null
                || reserva.EstadoReserva != EstadoReserva.Aceptada.ToString()
                || !reserva.MinutoDesde.HasValue
                || !reserva.ImporteEstimado.HasValue
                || reserva.EstadoPago == BLL_Pago.EstadoPendiente)
            {
                return 0m;
            }

            DateTime momentoReservado = reserva.FechaSolicitada.Date.AddMinutes(reserva.MinutoDesde.Value);
            double horasRestantes = (momentoReservado - DateTime.Now).TotalHours;
            if (horasRestantes >= _bllParametros.ObtenerEntero(BLL_ParametroPlataforma.HorasCancelacionSinCargo))
            {
                return 0m;
            }

            decimal porcentaje = _bllParametros.ObtenerDecimal(BLL_ParametroPlataforma.PenalidadCancelacionPorcentaje);
            return decimal.Round(reserva.ImporteEstimado.Value * porcentaje / 100m, 2);
        }

        // Texto de confirmación que muestra Mis reservas antes de cancelar.
        public string ObtenerAvisoCancelacion(Reserva reserva)
        {
            const string mensajeBase = "¿Seguro que querés cancelar esta reserva?";
            if (reserva == null)
            {
                return mensajeBase;
            }

            string moneda = reserva.Moneda ?? "ARS";
            decimal penalidad = CalcularPenalidadCancelacion(reserva);
            bool pagada = reserva.EstadoPago == BLL_Pago.EstadoPagado;

            if (pagada && reserva.ImporteEstimado.HasValue)
            {
                string devolucion = "Te devolvemos " + (reserva.ImporteEstimado.Value - penalidad).ToString("0.##", CultureInfo.CurrentCulture) +
                    " " + moneda + " como saldo a favor en tu cuenta corriente";
                return penalidad > 0
                    ? "Falta poco para el horario reservado: si la cancelás ahora se cobra una penalidad de " +
                      penalidad.ToString("0.##", CultureInfo.CurrentCulture) + " " + moneda + ". " + devolucion + ". ¿Querés continuar?"
                    : devolucion + ". ¿Querés continuar?";
            }

            if (penalidad > 0)
            {
                return "Esta reserva ya está aceptada y falta poco para el horario reservado. Si la cancelás ahora se te va a aplicar una comisión de cancelación de " +
                    penalidad.ToString("0.##", CultureInfo.CurrentCulture) + " " + moneda + ". ¿Querés continuar?";
            }

            return mensajeBase;
        }

        public ResultadoOperacion Cancelar(int idReserva, int idUsuarioExternoSolicitante)
        {
            return EjecutarProtegido(() =>
            {
                ProcesarPagosVencidos();
                _mppReserva.FinalizarVencidas();
                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReserva });
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

                bool estabaPagada = reserva.EstadoPago == BLL_Pago.EstadoPagado;
                decimal penalidad = CalcularPenalidadCancelacion(reserva);
                bool comisionAplicada = penalidad > 0;

                reserva.ComisionAplicada = comisionAplicada;
                reserva.ImporteComision = comisionAplicada ? penalidad : (decimal?)null;
                ResultadoCancelacionReserva resultado = _mppReserva.Cancelar(reserva);
                if (!resultado.SeCancelo)
                {
                    return ResultadoOperacion.Error("Esta reserva ya fue resuelta y no se puede cancelar.");
                }

                if (estabaPagada)
                {
                    reserva.EstadoPago = BLL_Pago.EstadoDevuelto;
                }

                string moneda = reserva.Moneda ?? "ARS";
                string mensaje;
                if (estabaPagada && reserva.ImporteEstimado.HasValue)
                {
                    mensaje = "Tu reserva fue cancelada. Te devolvimos " +
                        (reserva.ImporteEstimado.Value - penalidad).ToString("0.##", CultureInfo.InvariantCulture) + " " + moneda +
                        " como saldo a favor" + (comisionAplicada
                            ? " (se descontó una penalidad de " + penalidad.ToString("0.##", CultureInfo.InvariantCulture) + " " + moneda + " por cancelar con poca anticipación)"
                            : string.Empty) +
                        ". Podés verlo en Mi cuenta corriente.";
                }
                else
                {
                    mensaje = comisionAplicada
                        ? "Tu reserva fue cancelada. Como faltaba poco para el horario reservado, se aplicó una comisión de cancelación de " +
                          penalidad.ToString("0.##", CultureInfo.InvariantCulture) + " " + moneda + "."
                        : "Tu reserva fue cancelada.";
                }

                _bitacora.Registrar(
                    idUsuarioExternoSolicitante, "MODIFICACION", TipoEntidadBitacora, idReserva,
                    "El solicitante canceló su reserva del espacio \"" + reserva.NombreEspacio + "\"." +
                    (comisionAplicada ? " Se aplicó comisión de cancelación." : string.Empty) +
                    (estabaPagada ? " Se emitió nota de crédito por el importe pagado." : string.Empty));

                _notificacion.Notificar(
                    reserva.IdUsuarioGestor, TipoNotificacion.ReservaCancelada,
                    "El solicitante canceló su reserva para \"" + reserva.NombreEspacio + "\".",
                    "~/SolicitudesRecibidas.aspx");

                UsuarioExterno solicitanteCancela = ObtenerUsuarioParaCorreo(idUsuarioExternoSolicitante);
                EnviarCorreoSinBloquear(solicitanteCancela, u =>
                    _servicioCorreo.EnviarCancelacionAlSolicitante(u.CorreoElectronico, u.Nombre, reserva));
                EnviarCorreoSinBloquear(ObtenerUsuarioParaCorreo(reserva.IdUsuarioGestor), u =>
                    _servicioCorreo.EnviarCancelacionAlGestor(u.CorreoElectronico, u.Nombre, reserva, NombreCompleto(solicitanteCancela)));

                BLL_CuentaCorriente bllCuenta = new BLL_CuentaCorriente();
                if (resultado.IdNotaCredito.HasValue)
                {
                    bllCuenta.AvisarComprobanteEmitido(resultado.IdNotaCredito.Value);
                }

                if (resultado.IdNotaDebito.HasValue)
                {
                    bllCuenta.AvisarComprobanteEmitido(resultado.IdNotaDebito.Value);
                }

                return ResultadoOperacion.Ok(mensaje);
            });
        }

        // Cancela (sin cargo) las reservas aceptadas que no se pagaron a
        // tiempo y avisa al cliente y al gestor. Se llama antes de listar o
        // cancelar reservas y desde el chequeo periódico de Site.Master.
        private void ProcesarPagosVencidos()
        {
            List<Reserva> vencidas;
            try
            {
                vencidas = _mppReserva.VencerPagosPendientes();
            }
            catch (ErrorAccesoDatosException)
            {
                return;
            }

            foreach (Reserva reserva in vencidas)
            {
                Reserva reservaVencida = reserva;
                try
                {
                    _bitacora.Registrar(
                        null, "MODIFICACION", TipoEntidadBitacora, reserva.IdReserva,
                        "Se canceló la reserva del espacio \"" + reserva.NombreEspacio + "\" porque venció el plazo de pago.",
                        "StageUp.BLL");

                    _notificacion.Notificar(
                        reserva.IdUsuarioExternoSolicitante, TipoNotificacion.PagoVencido,
                        "Se canceló tu reserva para \"" + reserva.NombreEspacio + "\" porque no se pagó a tiempo.",
                        "~/MisReservas.aspx");

                    _notificacion.Notificar(
                        reserva.IdUsuarioGestor, TipoNotificacion.PagoVencido,
                        "La reserva N° " + reserva.IdReserva + " de \"" + reserva.NombreEspacio + "\" se canceló por falta de pago. El horario quedó libre.",
                        "~/SolicitudesRecibidas.aspx");

                    EnviarCorreoSinBloquear(ObtenerUsuarioParaCorreo(reserva.IdUsuarioExternoSolicitante), u =>
                        _servicioCorreo.EnviarPagoVencido(u.CorreoElectronico, u.Nombre, reservaVencida));
                }
                catch (ErrorAccesoDatosException)
                {
                    // La reserva ya quedó cancelada: un aviso que falla no
                    // tiene que cortar el resto.
                }
            }
        }

        // Genera la notificación de "recordatorio 24hs antes" para las
        // reservas Aceptadas que entraron en esa ventana y todavía no la
        // tienen. Pensado para dispararse desde Site.Master en cualquier
        // pageview autenticado (ver comentario del throttle más arriba): la
        // llamada es barata cuando no corresponde volver a chequear, así que
        // no hace falta que el llamador se preocupe por eso.
        public void GenerarRecordatorios24hsSiCorresponde()
        {
            lock (_lockRecordatorios)
            {
                if (_ultimaGeneracionRecordatorios.HasValue &&
                    (DateTime.Now - _ultimaGeneracionRecordatorios.Value).TotalMinutes < IntervaloMinutosRecordatorios)
                {
                    return;
                }

                _ultimaGeneracionRecordatorios = DateTime.Now;
            }

            try
            {
                ProcesarPagosVencidos();
                List<Reserva> pendientes = _mppReserva.ListarPendientesDeRecordatorio();
                foreach (Reserva reserva in pendientes)
                {
                    try
                    {
                        string momento = reserva.MinutoDesde.HasValue
                            ? reserva.FechaSolicitada.Date.ToString("dd/MM/yyyy") + " a las " + FormatearHora(reserva.MinutoDesde.Value)
                            : reserva.FechaSolicitada.Date.ToString("dd/MM/yyyy");

                        _notificacion.Notificar(
                            reserva.IdUsuarioExternoSolicitante, TipoNotificacion.RecordatorioReserva,
                            "Recordatorio: tu reserva para \"" + reserva.NombreEspacio + "\" es el " + momento + ".",
                            "~/MisReservas.aspx");

                        // Un solo intento de mail: se marca como enviado igual,
                        // para no reintentar cada 15 minutos si el SMTP falla.
                        Reserva reservaRecordatorio = reserva;
                        EnviarCorreoSinBloquear(ObtenerUsuarioParaCorreo(reserva.IdUsuarioExternoSolicitante), u =>
                            _servicioCorreo.EnviarRecordatorioReserva(u.CorreoElectronico, u.Nombre, reservaRecordatorio));

                        _mppReserva.MarcarRecordatorioEnviado(reserva);
                    }
                    catch (ErrorAccesoDatosException)
                    {
                        // Si falla un recordatorio puntual seguimos con el resto;
                        // como no se marcó como enviado, se reintenta solo en la
                        // próxima corrida.
                    }
                }

                EnviarAvisosDeReservasFinalizadas();
            }
            catch (ErrorAccesoDatosException)
            {
                // Nadie está mirando esto en vivo (se dispara de fondo desde
                // Site.Master): si falla, no tiene que romper la página que
                // lo disparó.
            }
        }

        // Ítem 5A: mail de "reserva finalizada" (invitación a calificar). La
        // finalización la hace sp_Reserva_FinalizarVencidas en bloque, así que
        // se buscan las finalizadas todavía sin aviso (script 42). Un solo
        // intento por reserva, igual que el recordatorio.
        private void EnviarAvisosDeReservasFinalizadas()
        {
            _mppReserva.FinalizarVencidas();
            List<Reserva> finalizadas = _mppReserva.ListarFinalizadasSinAviso();
            foreach (Reserva reserva in finalizadas)
            {
                try
                {
                    Reserva reservaFinalizada = reserva;
                    EnviarCorreoSinBloquear(ObtenerUsuarioParaCorreo(reserva.IdUsuarioExternoSolicitante), u =>
                        _servicioCorreo.EnviarReservaFinalizada(u.CorreoElectronico, u.Nombre, reservaFinalizada));

                    _mppReserva.MarcarAvisoFinalizacionEnviado(reserva);
                }
                catch (ErrorAccesoDatosException)
                {
                    // Se reintenta en la próxima corrida.
                }
            }
        }

        // Los mails de reserva nunca hacen fallar la operación: la reserva ya
        // quedó guardada y notificada en StageUp (campanita), que es lo que
        // importa. Mismo criterio que BLL_Ticket con la respuesta de soporte.
        private UsuarioExterno ObtenerUsuarioParaCorreo(int idUsuarioExterno)
        {
            try
            {
                return _mppUsuario.ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void EnviarCorreoSinBloquear(UsuarioExterno destinatario, Func<UsuarioExterno, bool> envio)
        {
            if (destinatario == null || string.IsNullOrWhiteSpace(destinatario.CorreoElectronico))
            {
                return;
            }

            try
            {
                envio(destinatario);
            }
            catch (Exception)
            {
                // Un mail que no sale no debe romper la operación.
            }
        }

        private static string NombreCompleto(UsuarioExterno usuario)
        {
            if (usuario == null)
            {
                return null;
            }

            return ((usuario.Nombre ?? string.Empty) + " " + (usuario.Apellido ?? string.Empty)).Trim();
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
