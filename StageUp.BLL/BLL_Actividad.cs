using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-001-009 Gestionar actividades internas del espacio.
    //
    // Actividades internas de un espacio (clases, talleres, ensayos). Cargar
    // una actividad bloquea automáticamente la disponibilidad del espacio
    // para reservas externas en los horarios que le correspondan.
    //
    // El bloqueo se materializa como filas en FranjaEspacio con
    // origen='Actividad', separadas de las franjas manuales del gestor. Cada
    // vez que se guarda o se da de baja una actividad se borran y se vuelven
    // a generar SUS PROPIAS franjas (se identifican por idActividad), así que
    // nunca quedan duplicadas ni se tocan las de otra actividad.
    //
    // Validaciones del documento: campos obligatorios (A2, se informan todos
    // juntos), horario (A3), cupo (A4), superposición con reservas aceptadas,
    // solicitudes pendientes u otras actividades del mismo espacio (A5/A8) y
    // baja con operaciones asociadas (A10).
    public class BLL_Actividad
    {
        private readonly MPP_Actividad _mppActividad = new MPP_Actividad();
        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly MPP_Participante _mppParticipante = new MPP_Participante();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const string TipoEntidadBitacora = "Actividad";

        public const string FiltroActivas = "Activas";
        public const string FiltroInactivas = "Inactivas";
        public const string FiltroTodas = "Todas";

        public const int LongitudMaximaNombre = 200;
        public const int CupoMaximoPermitido = 10000;

        // Horizonte hacia adelante para materializar fechas concretas de la
        // recurrencia "Mensual" (no representable como día de semana fijo en
        // FranjaEspacio). Se recalcula en cada guardado de la actividad.
        private const int HorizonteMesesRecurrenciaMensual = 12;

        private static readonly string[] OrdinalesSemana = { string.Empty, "primer", "segundo", "tercer", "cuarto", "último" };

        // ------------------------------------------------------------------
        // Alta y modificación (pasos 10 a 18, A7)
        // ------------------------------------------------------------------

        public ResultadoOperacion<int> Guardar(Actividad actividad, int idUsuarioGestor)
        {
            return Guardar(actividad, idUsuarioGestor, null);
        }

        // idsParticipantes: participantes que tienen que quedar asociados
        // (null = no se cambian los participantes).
        public ResultadoOperacion<int> Guardar(Actividad actividad, int idUsuarioGestor, List<int> idsParticipantes)
        {
            return EjecutarProtegido(() =>
            {
                if (actividad == null)
                {
                    return ResultadoOperacion<int>.Error("Completá los datos de la actividad.", "A2");
                }

                bool esNueva = actividad.IdActividad == 0;
                Actividad anterior = null;
                if (!esNueva)
                {
                    anterior = _mppActividad.ObtenerPorId(new Actividad { IdActividad = actividad.IdActividad });
                    ResultadoOperacion pertenencia = ValidarPertenencia(anterior, idUsuarioGestor);
                    if (!pertenencia.Exitoso)
                    {
                        return ResultadoOperacion<int>.Error(pertenencia.Mensaje);
                    }

                    if (!anterior.Activa)
                    {
                        return ResultadoOperacion<int>.Error("La actividad está dada de baja: no se puede modificar.");
                    }
                }

                Normalizar(actividad);

                // A2, A3 y A4.
                ResultadoOperacion validacion = Validar(actividad, idUsuarioGestor);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje, validacion.CodigoAlternativo);
                }

                // Participantes: tienen que ser del gestor y entrar en el cupo.
                List<Participante> actuales = esNueva
                    ? new List<Participante>()
                    : _mppActividad.ListarParticipantesDeActividad(actividad);
                List<Participante> deseados = null;
                if (idsParticipantes != null)
                {
                    deseados = new List<Participante>();
                    foreach (int idParticipante in idsParticipantes.Distinct())
                    {
                        Participante participante = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = idParticipante });
                        if (participante == null || participante.IdUsuarioGestor != idUsuarioGestor || !participante.Activo)
                        {
                            return ResultadoOperacion<int>.Error("Uno de los participantes elegidos no existe, fue dado de baja o no te pertenece.", "A2");
                        }

                        deseados.Add(participante);
                    }
                }

                int cantidadParticipantes = deseados != null ? deseados.Count : actuales.Count;
                if (cantidadParticipantes > actividad.CupoMaximo)
                {
                    return ResultadoOperacion<int>.Error(
                        "El cupo máximo (" + actividad.CupoMaximo + ") no puede ser menor que la cantidad de participantes asociados (" +
                        cantidadParticipantes + "). Aumentá el cupo o quitá participantes.", "A4");
                }

                // A5: otras actividades internas del mismo espacio.
                Actividad superpuesta = BuscarActividadSuperpuesta(actividad);
                if (superpuesta != null)
                {
                    return ResultadoOperacion<int>.Error(
                        "La actividad no puede registrarse porque el horario está ocupado por otra actividad interna del espacio: «" +
                        superpuesta.Nombre + "» (" + DescribirProgramacion(superpuesta) + ", " +
                        FormatearHorario(superpuesta.MinutoDesde, superpuesta.MinutoHasta) + "). Cambiá el día, el horario o la duración.", "A5");
                }

                // A5 (alta) y A8 (modificación): reservas aceptadas y solicitudes pendientes.
                Reserva reserva = BuscarReservaEnConflicto(actividad);
                if (reserva != null)
                {
                    string operacion = DescribirReserva(reserva);
                    return esNueva
                        ? ResultadoOperacion<int>.Error(
                            "La actividad no puede registrarse porque el horario está comprometido por " + operacion +
                            ". Cambiá el día, el horario o la duración.", "A5")
                        : ResultadoOperacion<int>.Error(
                            "No es posible modificar la actividad interna porque el nuevo horario afectaría " + operacion +
                            ". La actividad mantiene sus datos anteriores: cambiá los datos o resolvé antes esa operación.", "A8");
                }

                int idActividad = esNueva
                    ? _mppActividad.Insertar(actividad)
                    : ModificarYDevolverId(actividad);
                actividad.IdActividad = idActividad;

                _mppActividad.EliminarDiasSemana(actividad);
                if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
                {
                    foreach (int dia in actividad.DiasSemana.Distinct())
                    {
                        _mppActividad.InsertarDiaSemana(actividad, dia);
                    }
                }

                // Pasos 15 y 16 / A7 paso 7: se borran los bloqueos anteriores
                // de esta actividad y se generan los nuevos.
                RegenerarFranjasBloqueadas(actividad);

                _bitacora.Registrar(
                    idUsuarioGestor, esNueva ? "ALTA" : "MODIFICACION", TipoEntidadBitacora, idActividad,
                    (esNueva ? "Alta de la actividad interna \"" : "Modificación de la actividad interna \"") + actividad.Nombre +
                    "\" en \"" + NombreEspacio(actividad) + "\": " + DescribirProgramacion(actividad) + ", " +
                    FormatearHorario(actividad.MinutoDesde, actividad.MinutoHasta) + ", cupo " + actividad.CupoMaximo +
                    (anterior != null ? " (antes: " + DescribirProgramacion(anterior) + ", " + FormatearHorario(anterior.MinutoDesde, anterior.MinutoHasta) +
                        ", cupo " + anterior.CupoMaximo + ")" : string.Empty) + ".");

                if (deseados != null)
                {
                    SincronizarParticipantes(actividad, actuales, deseados, idUsuarioGestor);
                }

                return ResultadoOperacion<int>.Ok(idActividad, esNueva
                    ? "La actividad interna fue guardada correctamente. Su horario ya bloquea el espacio para reservas externas."
                    : "La actividad interna fue actualizada correctamente.");
            });
        }

        private int ModificarYDevolverId(Actividad actividad)
        {
            _mppActividad.Modificar(actividad);
            return actividad.IdActividad;
        }

        private void SincronizarParticipantes(Actividad actividad, List<Participante> actuales, List<Participante> deseados, int idUsuarioGestor)
        {
            foreach (Participante participante in deseados.Where(d => !actuales.Exists(a => a.IdParticipante == d.IdParticipante)))
            {
                _mppActividad.AsociarParticipante(actividad, participante);
                _bitacora.Registrar(idUsuarioGestor, "ASOCIACION", TipoEntidadBitacora, actividad.IdActividad,
                    "Se asoció a " + participante.NombreCompleto + " a la actividad \"" + actividad.Nombre + "\".");
            }

            foreach (Participante participante in actuales.Where(a => !deseados.Exists(d => d.IdParticipante == a.IdParticipante)))
            {
                _mppActividad.DesasociarParticipante(actividad, participante);
                _bitacora.Registrar(idUsuarioGestor, "DESVINCULACION", TipoEntidadBitacora, actividad.IdActividad,
                    "Se desvinculó a " + participante.NombreCompleto + " de la actividad \"" + actividad.Nombre + "\".");
            }
        }

        // ------------------------------------------------------------------
        // A9 / A10: baja lógica
        // ------------------------------------------------------------------
        public ResultadoOperacion DarDeBaja(int idActividad, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                Actividad actividad = _mppActividad.ObtenerPorId(new Actividad { IdActividad = idActividad });
                ResultadoOperacion validacion = ValidarPertenencia(actividad, idUsuarioGestor);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                if (!actividad.Activa)
                {
                    return ResultadoOperacion.Error("La actividad ya estaba dada de baja.");
                }

                // A10. En la práctica no debería encontrar conflictos (mientras
                // la actividad está activa su horario está bloqueado), pero
                // cubre reservas que ya existían antes de cargarla.
                Reserva reserva = BuscarReservaEnConflicto(actividad);
                if (reserva != null)
                {
                    return ResultadoOperacion.Error(
                        "No es posible dar de baja la actividad interna porque afectaría " + DescribirReserva(reserva) +
                        ". La actividad mantiene su estado anterior.", "A10");
                }

                _mppActividad.DarDeBaja(actividad);
                _mppActividad.EliminarFranjasPorActividad(actividad);

                _bitacora.Registrar(
                    idUsuarioGestor, "BAJA", TipoEntidadBitacora, idActividad,
                    "Baja lógica de la actividad interna \"" + actividad.Nombre + "\" en \"" + NombreEspacio(actividad) + "\".");

                return ResultadoOperacion.Ok(
                    "La actividad interna fue dada de baja correctamente. Dejó de bloquear el horario del espacio y se conserva su historial.");
            });
        }

        // ------------------------------------------------------------------
        // Consultas
        // ------------------------------------------------------------------

        public List<Actividad> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            return Listar(idUsuarioGestor, FiltroActivas, null);
        }

        // Paso 9: listado con filtro de estado y de espacio.
        public List<Actividad> Listar(int idUsuarioGestor, string estado, int? idEspacioArtistico)
        {
            try
            {
                string filtro = estado == FiltroInactivas || estado == FiltroTodas ? estado : FiltroActivas;
                return _mppActividad.ListarPorUsuarioGestor(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor }, filtro, idEspacioArtistico);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Actividad>();
            }
        }

        public Actividad ObtenerParaEditar(int idActividad, int idUsuarioGestor)
        {
            return ObtenerDetalle(idActividad, idUsuarioGestor);
        }

        // A6: detalle con participantes (también de actividades dadas de baja).
        public Actividad ObtenerDetalle(int idActividad, int idUsuarioGestor)
        {
            try
            {
                Actividad actividad = _mppActividad.ObtenerPorId(new Actividad { IdActividad = idActividad });
                if (actividad == null || !EsDelGestor(actividad, idUsuarioGestor))
                {
                    return null;
                }

                actividad.Participantes = _mppActividad.ListarParticipantesDeActividad(actividad);
                return actividad;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion AsociarParticipante(int idActividad, int idParticipante, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                Actividad actividad = _mppActividad.ObtenerPorId(new Actividad { IdActividad = idActividad });
                ResultadoOperacion validacionActividad = ValidarPertenencia(actividad, idUsuarioGestor);
                if (!validacionActividad.Exitoso)
                {
                    return validacionActividad;
                }

                if (!actividad.Activa)
                {
                    return ResultadoOperacion.Error("La actividad está dada de baja: no se le pueden asociar participantes.");
                }

                Participante participante = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = idParticipante });
                if (participante == null || participante.IdUsuarioGestor != idUsuarioGestor)
                {
                    return ResultadoOperacion.Error("El participante indicado no existe o no te pertenece.");
                }

                List<Participante> participantesActuales = _mppActividad.ListarParticipantesDeActividad(actividad);
                bool yaAsociado = participantesActuales.Exists(p => p.IdParticipante == idParticipante);
                if (yaAsociado)
                {
                    return ResultadoOperacion.Ok();
                }

                if (participantesActuales.Count >= actividad.CupoMaximo)
                {
                    return ResultadoOperacion.Error(
                        "No es posible asociar más participantes a la actividad seleccionada: se alcanzó el cupo máximo (" +
                        actividad.CupoMaximo + ").");
                }

                _mppActividad.AsociarParticipante(actividad, participante);

                _bitacora.Registrar(
                    idUsuarioGestor, "ASOCIACION", TipoEntidadBitacora, idActividad,
                    "Se asoció a " + participante.NombreCompleto + " a la actividad \"" + actividad.Nombre + "\".");

                return ResultadoOperacion.Ok();
            });
        }

        public ResultadoOperacion DesasociarParticipante(int idActividad, int idParticipante, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                Actividad actividad = _mppActividad.ObtenerPorId(new Actividad { IdActividad = idActividad });
                ResultadoOperacion validacion = ValidarPertenencia(actividad, idUsuarioGestor);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                Participante participante = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = idParticipante });

                _mppActividad.DesasociarParticipante(actividad, new Participante { IdParticipante = idParticipante });

                _bitacora.Registrar(
                    idUsuarioGestor, "DESVINCULACION", TipoEntidadBitacora, idActividad,
                    "Se desvinculó a " + (participante != null ? participante.NombreCompleto : "un participante") +
                    " de la actividad \"" + actividad.Nombre + "\".");

                return ResultadoOperacion.Ok();
            });
        }

        // ------------------------------------------------------------------
        // Textos (listado, detalle y bitácora)
        // ------------------------------------------------------------------

        public static string DescribirProgramacion(Actividad actividad)
        {
            if (actividad == null)
            {
                return string.Empty;
            }

            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
            {
                List<string> dias = (actividad.DiasSemana ?? new List<int>())
                    .Distinct().OrderBy(d => d)
                    .Select(d => BLL_DisponibilidadEspacio.NombreDia(d).ToLowerInvariant())
                    .Where(d => d.Length > 0)
                    .ToList();
                return "Todas las semanas: " + (dias.Count == 0 ? "sin días" : UnirConY(dias));
            }

            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString())
            {
                int semana = actividad.SemanaDelMes ?? 0;
                string ordinal = semana >= 1 && semana <= 5 ? OrdinalesSemana[semana] : "?";
                return "Una vez al mes: " + ordinal + " " +
                    BLL_DisponibilidadEspacio.NombreDia(actividad.DiaSemanaMensual ?? 0).ToLowerInvariant() + " de cada mes";
            }

            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString())
            {
                DateTime fecha;
                return DateTime.TryParseExact(actividad.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                    ? "Fecha única: " + BLL_DisponibilidadEspacio.NombreDia(BLL_DisponibilidadEspacio.DiaSemanaDe(fecha)).ToLowerInvariant() +
                      " " + fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                    : "Fecha única";
            }

            return "-";
        }

        public static string FormatearHorario(int minutoDesde, int minutoHasta)
        {
            return BLL_DisponibilidadEspacio.FormatearHora(minutoDesde) + " a " + BLL_DisponibilidadEspacio.FormatearHora(minutoHasta);
        }

        public static string FormatearDuracion(int minutos)
        {
            int horas = minutos / 60;
            int resto = minutos % 60;
            if (horas == 0)
            {
                return resto + " min";
            }

            return horas + (horas == 1 ? " hora" : " horas") + (resto > 0 ? " " + resto + " min" : string.Empty);
        }

        // Próximas fechas en que la actividad ocupa el espacio.
        public static List<DateTime> ProximasFechas(Actividad actividad, int cantidad)
        {
            return GenerarFranjas(actividad, DateTime.Now.Date)
                .SelectMany(f => FechasDeFranja(f, DateTime.Now.Date, 8 * 7))
                .Distinct()
                .OrderBy(f => f)
                .Take(cantidad)
                .ToList();
        }

        // ------------------------------------------------------------------
        // Validaciones
        // ------------------------------------------------------------------

        private static void Normalizar(Actividad actividad)
        {
            actividad.Nombre = (actividad.Nombre ?? string.Empty).Trim();
            actividad.Tipo = string.IsNullOrWhiteSpace(actividad.Tipo) ? null : actividad.Tipo.Trim();
            actividad.Notas = string.IsNullOrWhiteSpace(actividad.Notas) ? null : actividad.Notas.Trim();
            actividad.DiasSemana = (actividad.DiasSemana ?? new List<int>()).Distinct().OrderBy(d => d).ToList();
            if (actividad.ModoRecurrencia != ModoRecurrenciaActividad.Semanal.ToString())
            {
                actividad.DiasSemana = new List<int>();
            }

            if (actividad.ModoRecurrencia != ModoRecurrenciaActividad.Mensual.ToString())
            {
                actividad.SemanaDelMes = null;
                actividad.DiaSemanaMensual = null;
            }

            if (actividad.ModoRecurrencia != ModoRecurrenciaActividad.Fecha.ToString())
            {
                actividad.Fecha = null;
            }
        }

        // A2 (todos los faltantes juntos), A3 y A4.
        private ResultadoOperacion Validar(Actividad actividad, int idUsuarioGestor)
        {
            List<string> faltantes = new List<string>();
            if (string.IsNullOrWhiteSpace(actividad.Nombre))
            {
                faltantes.Add("el nombre de la actividad");
            }

            if (actividad.IdEspacioArtistico <= 0)
            {
                faltantes.Add("el espacio asociado");
            }

            bool semanal = actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString();
            bool mensual = actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString();
            bool porFecha = actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString();
            if (!semanal && !mensual && !porFecha)
            {
                faltantes.Add("cuándo se repite la actividad");
            }
            else if (semanal && actividad.DiasSemana.Count == 0)
            {
                faltantes.Add("al menos un día de la semana");
            }
            else if (mensual && (!actividad.SemanaDelMes.HasValue || !actividad.DiaSemanaMensual.HasValue))
            {
                faltantes.Add("la semana del mes y el día");
            }
            else if (porFecha && string.IsNullOrEmpty(actividad.Fecha))
            {
                faltantes.Add("la fecha");
            }

            if (actividad.MinutoDesde < 0)
            {
                faltantes.Add("el horario de inicio");
            }

            if (actividad.MinutoHasta < 0)
            {
                faltantes.Add("el horario de fin");
            }

            if (actividad.CupoMaximo == 0)
            {
                faltantes.Add("el cupo máximo");
            }

            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Error("Completá " + UnirConY(faltantes) + ".", "A2");
            }

            if (actividad.Nombre.Length > LongitudMaximaNombre)
            {
                return ResultadoOperacion.Error("El nombre de la actividad no puede superar los " + LongitudMaximaNombre + " caracteres.", "A2");
            }

            // A3.
            if (actividad.MinutoHasta > 1440 || actividad.MinutoDesde >= 1440)
            {
                return ResultadoOperacion.Error("El horario no es válido: tiene que estar dentro del mismo día.", "A3");
            }

            if (actividad.MinutoHasta <= actividad.MinutoDesde)
            {
                return ResultadoOperacion.Error(
                    "El horario no es válido: el horario de fin tiene que ser posterior al de inicio (la duración tiene que ser mayor a cero).", "A3");
            }

            if (semanal && actividad.DiasSemana.Exists(d => d < 1 || d > 7))
            {
                return ResultadoOperacion.Error("Elegí días de la semana válidos.", "A3");
            }

            if (mensual && (actividad.SemanaDelMes < 1 || actividad.SemanaDelMes > 5 || actividad.DiaSemanaMensual < 1 || actividad.DiaSemanaMensual > 7))
            {
                return ResultadoOperacion.Error("Elegí una semana del mes y un día válidos.", "A3");
            }

            if (porFecha)
            {
                DateTime fecha;
                if (!DateTime.TryParseExact(actividad.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                {
                    return ResultadoOperacion.Error("La fecha de la actividad no es válida.", "A3");
                }

                if (fecha.Date < DateTime.Now.Date)
                {
                    return ResultadoOperacion.Error("La fecha de la actividad tiene que ser de hoy en adelante.", "A3");
                }
            }

            // A4.
            if (actividad.CupoMaximo < 1 || actividad.CupoMaximo > CupoMaximoPermitido)
            {
                return ResultadoOperacion.Error("El cupo máximo tiene que ser un número entero entre 1 y " + CupoMaximoPermitido + ".", "A4");
            }

            if (actividad.ParticipantesEstimados.HasValue &&
                (actividad.ParticipantesEstimados < 0 || actividad.ParticipantesEstimados > CupoMaximoPermitido))
            {
                return ResultadoOperacion.Error("Los participantes estimados tienen que ser un número entero entre 0 y " + CupoMaximoPermitido + ".", "A4");
            }

            // Reglas: el espacio tiene que ser del gestor y estar activo.
            EspacioArtistico espacio = _mppEspacio.ObtenerPorId(
                new EspacioArtistico { IdEspacioArtistico = actividad.IdEspacioArtistico });
            if (espacio == null || espacio.IdUsuarioGestor != idUsuarioGestor)
            {
                return ResultadoOperacion.Error("El espacio indicado no existe o no te pertenece.");
            }

            if (!espacio.Activo)
            {
                return ResultadoOperacion.Error("El espacio fue dado de baja: no se pueden gestionar sus actividades.");
            }

            actividad.NombreEspacio = espacio.NombreEspacio;
            return ResultadoOperacion.Ok();
        }

        private ResultadoOperacion ValidarPertenencia(Actividad actividad, int idUsuarioGestor)
        {
            if (actividad == null)
            {
                return ResultadoOperacion.Error("No se encontró la actividad indicada.");
            }

            return EsDelGestor(actividad, idUsuarioGestor)
                ? ResultadoOperacion.Ok()
                : ResultadoOperacion.Error("No tenés permiso sobre esta actividad.");
        }

        private bool EsDelGestor(Actividad actividad, int idUsuarioGestor)
        {
            EspacioArtistico espacio = _mppEspacio.ObtenerPorId(
                new EspacioArtistico { IdEspacioArtistico = actividad.IdEspacioArtistico });
            if (espacio != null && string.IsNullOrEmpty(actividad.NombreEspacio))
            {
                actividad.NombreEspacio = espacio.NombreEspacio;
            }

            return espacio != null && espacio.IdUsuarioGestor == idUsuarioGestor;
        }

        // A5: otra actividad activa del mismo espacio que ocupe el espacio
        // algún mismo día en un horario que se superpone.
        private Actividad BuscarActividadSuperpuesta(Actividad actividad)
        {
            DateTime hoy = DateTime.Now.Date;
            List<FranjaEspacio> propias = GenerarFranjas(actividad, hoy);
            List<Actividad> otras = _mppActividad.ListarPorEspacio(
                new EspacioArtistico { IdEspacioArtistico = actividad.IdEspacioArtistico });

            foreach (Actividad otra in otras.Where(o => o.IdActividad != actividad.IdActividad && o.Activa))
            {
                foreach (FranjaEspacio franjaOtra in GenerarFranjas(otra, hoy).Where(f => !BLL_DisponibilidadEspacio.YaPaso(f)))
                {
                    if (propias.Exists(f =>
                        f.MinutoDesde < franjaOtra.MinutoHasta && franjaOtra.MinutoDesde < f.MinutoHasta &&
                        BLL_DisponibilidadEspacio.CompartenDias(f, franjaOtra)))
                    {
                        return otra;
                    }
                }
            }

            return null;
        }

        // ---- Validación contra reservas existentes ----

        // A5, A8 y A10: reserva aceptada o solicitud pendiente (de hoy en
        // adelante) que cae en alguno de los días y horarios de la actividad.
        private Reserva BuscarReservaEnConflicto(Actividad actividad)
        {
            List<Reserva> reservasActivas = _mppReserva.ListarActivasPorEspacio(
                new EspacioArtistico { IdEspacioArtistico = actividad.IdEspacioArtistico });
            if (reservasActivas.Count == 0)
            {
                return null;
            }

            List<FranjaEspacio> franjas = GenerarFranjas(actividad, DateTime.Now.Date);
            return reservasActivas
                .Where(r => r.MinutoDesde.HasValue && r.MinutoHasta.HasValue)
                .OrderBy(r => r.FechaSolicitada)
                .FirstOrDefault(r => franjas.Exists(f =>
                    f.MinutoDesde < r.MinutoHasta.Value && r.MinutoDesde.Value < f.MinutoHasta &&
                    (string.IsNullOrEmpty(f.Fecha)
                        ? f.DiaSemana == BLL_DisponibilidadEspacio.DiaSemanaDe(r.FechaSolicitada)
                        : f.Fecha == r.FechaSolicitada.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));
        }

        private static string DescribirReserva(Reserva reserva)
        {
            return (reserva.EstadoReserva == "Aceptada" ? "una reserva aceptada" : "una solicitud de reserva pendiente") +
                " el " + reserva.FechaSolicitada.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) +
                " de " + FormatearHorario(reserva.MinutoDesde ?? 0, reserva.MinutoHasta ?? 0);
        }

        // ---- Franjas bloqueadas de la actividad ----

        // Llamado desde BLL_EspacioArtistico después de guardar la ficha:
        // vuelve a generar las franjas de todas las actividades activas del
        // espacio (sin duplicar: cada actividad borra primero las suyas).
        public void RegenerarFranjasBloqueadasDelEspacio(int idEspacioArtistico)
        {
            List<Actividad> actividadesActivas = _mppActividad.ListarPorEspacio(
                new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
            foreach (Actividad actividad in actividadesActivas)
            {
                RegenerarFranjasBloqueadas(actividad);
            }
        }

        private void RegenerarFranjasBloqueadas(Actividad actividad)
        {
            _mppActividad.EliminarFranjasPorActividad(actividad);
            foreach (FranjaEspacio franja in GenerarFranjas(actividad, DateTime.Now.Date))
            {
                _mppActividad.InsertarFranjaDesdeActividad(actividad, franja);
            }
        }

        // Franjas que ocupa la actividad: una por día de la semana (semanal),
        // una por fecha (mensual, en el horizonte) o la fecha única.
        internal static List<FranjaEspacio> GenerarFranjas(Actividad actividad, DateTime desde)
        {
            List<FranjaEspacio> franjas = new List<FranjaEspacio>();
            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
            {
                foreach (int dia in (actividad.DiasSemana ?? new List<int>()).Distinct())
                {
                    franjas.Add(new FranjaEspacio { DiaSemana = dia, Fecha = null, MinutoDesde = actividad.MinutoDesde, MinutoHasta = actividad.MinutoHasta });
                }
            }
            else if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString())
            {
                if (!string.IsNullOrEmpty(actividad.Fecha))
                {
                    franjas.Add(new FranjaEspacio { DiaSemana = null, Fecha = actividad.Fecha, MinutoDesde = actividad.MinutoDesde, MinutoHasta = actividad.MinutoHasta });
                }
            }
            else if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString() &&
                     actividad.SemanaDelMes.HasValue && actividad.DiaSemanaMensual.HasValue)
            {
                foreach (DateTime fecha in CalcularFechasMensuales(
                    actividad.SemanaDelMes.Value, actividad.DiaSemanaMensual.Value, desde, HorizonteMesesRecurrenciaMensual))
                {
                    franjas.Add(new FranjaEspacio
                    {
                        DiaSemana = null,
                        Fecha = fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        MinutoDesde = actividad.MinutoDesde,
                        MinutoHasta = actividad.MinutoHasta
                    });
                }
            }

            foreach (FranjaEspacio franja in franjas)
            {
                franja.Bloqueado = true;
                franja.Origen = "Actividad";
                franja.IdActividad = actividad.IdActividad;
                franja.NombreActividad = actividad.Nombre;
            }

            return franjas;
        }

        private static IEnumerable<DateTime> FechasDeFranja(FranjaEspacio franja, DateTime desde, int dias)
        {
            if (!string.IsNullOrEmpty(franja.Fecha))
            {
                DateTime fecha;
                if (DateTime.TryParseExact(franja.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha) &&
                    fecha.Date >= desde.Date)
                {
                    yield return fecha.Date;
                }

                yield break;
            }

            for (int i = 0; i < dias; i++)
            {
                DateTime fecha = desde.Date.AddDays(i);
                if (BLL_DisponibilidadEspacio.DiaSemanaDe(fecha) == franja.DiaSemana)
                {
                    yield return fecha;
                }
            }
        }

        private static string NombreEspacio(Actividad actividad)
        {
            return string.IsNullOrEmpty(actividad.NombreEspacio) ? "espacio " + actividad.IdEspacioArtistico : actividad.NombreEspacio;
        }

        private static string UnirConY(List<string> partes)
        {
            if (partes.Count <= 1)
            {
                return partes.Count == 0 ? string.Empty : partes[0];
            }

            return string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[partes.Count - 1];
        }

        // Calcula, para los próximos "horizonteMeses" meses a partir de
        // "desde", la fecha concreta del "semanaDelMes"-ésimo
        // "diaSemanaMensual" de cada mes (1..4 = primera..cuarta semana,
        // 5 = última), descartando las que ya pasaron. diaSemanaMensual usa
        // la misma convención que FranjaEspacio.DiaSemana (1 = lunes ... 7 =
        // domingo).
        internal static List<DateTime> CalcularFechasMensuales(
            int semanaDelMes, int diaSemanaMensual, DateTime desde, int horizonteMeses)
        {
            List<DateTime> fechas = new List<DateTime>();
            DayOfWeek diaObjetivo = (DayOfWeek)(diaSemanaMensual % 7);

            for (int offset = 0; offset < horizonteMeses; offset++)
            {
                DateTime primerDiaDelMes = new DateTime(desde.Year, desde.Month, 1).AddMonths(offset);
                int diasEnElMes = DateTime.DaysInMonth(primerDiaDelMes.Year, primerDiaDelMes.Month);

                List<DateTime> ocurrenciasDelMes = new List<DateTime>();
                for (int dia = 1; dia <= diasEnElMes; dia++)
                {
                    DateTime fechaCandidata = new DateTime(primerDiaDelMes.Year, primerDiaDelMes.Month, dia);
                    if (fechaCandidata.DayOfWeek == diaObjetivo)
                    {
                        ocurrenciasDelMes.Add(fechaCandidata);
                    }
                }

                if (ocurrenciasDelMes.Count == 0)
                {
                    continue;
                }

                DateTime? fechaDelMes = semanaDelMes >= 5
                    ? ocurrenciasDelMes[ocurrenciasDelMes.Count - 1]
                    : (semanaDelMes - 1 < ocurrenciasDelMes.Count ? ocurrenciasDelMes[semanaDelMes - 1] : (DateTime?)null);

                if (fechaDelMes.HasValue && fechaDelMes.Value.Date >= desde.Date)
                {
                    fechas.Add(fechaDelMes.Value);
                }
            }

            return fechas;
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
    }
}
