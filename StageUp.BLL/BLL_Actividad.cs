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
    // Actividades internas de un espacio (clases, talleres, ensayos). Cargar
    // una actividad bloquea automáticamente la disponibilidad del espacio
    // para reservas externas en los horarios que le correspondan.
    //
    // El bloqueo se materializa como filas en FranjaEspacio con
    // origen='Actividad', separadas de las franjas manuales que carga el
    // gestor en Mis espacios (ver comentario en
    // Database/26_ActividadesYNotificaciones.sql). Cada vez que se guarda o
    // se da de baja una actividad, se recalculan desde cero SUS PROPIAS
    // franjas (se identifican por idActividad, así que nunca se tocan las de
    // otra actividad ni las manuales).
    public class BLL_Actividad
    {
        private readonly MPP_Actividad _mppActividad = new MPP_Actividad();
        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly MPP_Participante _mppParticipante = new MPP_Participante();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const string TipoEntidadBitacora = "Actividad";

        // Horizonte hacia adelante para materializar fechas concretas de la
        // recurrencia "Mensual" (no representable como día de semana fijo en
        // FranjaEspacio). Se recalcula en cada guardado de la actividad, así
        // que alcanza para el uso normal de la materia.
        private const int HorizonteMesesRecurrenciaMensual = 12;

        public ResultadoOperacion<int> Guardar(Actividad actividad, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                ResultadoOperacion validacion = Validar(actividad, idUsuarioGestor);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                if (ExisteConflictoConReservas(actividad))
                {
                    return ResultadoOperacion<int>.Error(
                        "El horario elegido coincide con una reserva ya aceptada o una solicitud pendiente " +
                        "para este espacio. Elegí otro horario para la actividad.");
                }

                bool esNueva = actividad.IdActividad == 0;
                int idActividad = esNueva
                    ? _mppActividad.Insertar(actividad)
                    : ModificarYDevolverId(actividad);

                _mppActividad.EliminarDiasSemana(idActividad);
                if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
                {
                    foreach (int dia in actividad.DiasSemana.Distinct())
                    {
                        _mppActividad.InsertarDiaSemana(idActividad, dia);
                    }
                }

                RegenerarFranjasBloqueadas(idActividad, actividad);

                _bitacora.Registrar(
                    idUsuarioGestor, esNueva ? "ALTA" : "MODIFICACION", TipoEntidadBitacora, idActividad,
                    (esNueva ? "Alta de la actividad \"" : "Modificación de la actividad \"") + actividad.Nombre + "\".");

                return ResultadoOperacion<int>.Ok(idActividad, "Actividad guardada correctamente.");
            });
        }

        private int ModificarYDevolverId(Actividad actividad)
        {
            _mppActividad.Modificar(actividad);
            return actividad.IdActividad;
        }

        public ResultadoOperacion DarDeBaja(int idActividad, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                Actividad actividad = _mppActividad.ObtenerPorId(idActividad);
                ResultadoOperacion validacion = ValidarPertenencia(actividad, idUsuarioGestor);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                // Chequeo defensivo pedido por el Word (CU-001-009, alternativo A10):
                // en la práctica esto ya no debería encontrar conflictos, porque
                // mientras la actividad está activa bloquea el horario en
                // FranjaEspacio y una reserva nueva no puede crearse ahí. Se deja
                // igual para cubrir el caso de una reserva ya existente antes de
                // que se cargara la actividad.
                if (ExisteConflictoConReservas(actividad))
                {
                    return ResultadoOperacion.Error(
                        "No se puede dar de baja: hay una reserva ya aceptada o una solicitud pendiente " +
                        "para el horario de esta actividad.");
                }

                _mppActividad.DarDeBaja(idActividad);
                _mppActividad.EliminarFranjasPorActividad(idActividad);

                _bitacora.Registrar(
                    idUsuarioGestor, "BAJA", TipoEntidadBitacora, idActividad,
                    "Baja de la actividad \"" + actividad.Nombre + "\".");

                return ResultadoOperacion.Ok("La actividad fue dada de baja y se liberó el horario que tenía bloqueado.");
            });
        }

        public List<Actividad> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            try
            {
                List<Actividad> actividades = _mppActividad.ListarPorUsuarioGestor(idUsuarioGestor);
                foreach (Actividad actividad in actividades)
                {
                    actividad.Participantes = _mppActividad.ListarParticipantesDeActividad(actividad.IdActividad);
                }

                return actividades;
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Actividad>();
            }
        }

        public Actividad ObtenerParaEditar(int idActividad, int idUsuarioGestor)
        {
            Actividad actividad = _mppActividad.ObtenerPorId(idActividad);
            if (actividad == null || !EsDelGestor(actividad, idUsuarioGestor))
            {
                return null;
            }

            actividad.Participantes = _mppActividad.ListarParticipantesDeActividad(idActividad);
            return actividad;
        }

        public ResultadoOperacion AsociarParticipante(int idActividad, int idParticipante, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                Actividad actividad = _mppActividad.ObtenerPorId(idActividad);
                ResultadoOperacion validacionActividad = ValidarPertenencia(actividad, idUsuarioGestor);
                if (!validacionActividad.Exitoso)
                {
                    return validacionActividad;
                }

                Participante participante = _mppParticipante.ObtenerPorId(idParticipante);
                if (participante == null || participante.IdUsuarioGestor != idUsuarioGestor)
                {
                    return ResultadoOperacion.Error("El participante indicado no existe o no te pertenece.");
                }

                List<Participante> participantesActuales = _mppActividad.ListarParticipantesDeActividad(idActividad);
                bool yaAsociado = participantesActuales.Exists(p => p.IdParticipante == idParticipante);
                if (!yaAsociado && participantesActuales.Count >= actividad.CupoMaximo)
                {
                    return ResultadoOperacion.Error(
                        "No es posible asociar más participantes a la actividad seleccionada: se alcanzó el cupo máximo (" +
                        actividad.CupoMaximo + ").");
                }

                _mppActividad.AsociarParticipante(idActividad, idParticipante);

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
                Actividad actividad = _mppActividad.ObtenerPorId(idActividad);
                ResultadoOperacion validacion = ValidarPertenencia(actividad, idUsuarioGestor);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                Participante participante = _mppParticipante.ObtenerPorId(idParticipante);

                _mppActividad.DesasociarParticipante(idActividad, idParticipante);

                _bitacora.Registrar(
                    idUsuarioGestor, "DESVINCULACION", TipoEntidadBitacora, idActividad,
                    "Se desvinculó a " + (participante != null ? participante.NombreCompleto : "un participante") +
                    " de la actividad \"" + actividad.Nombre + "\".");

                return ResultadoOperacion.Ok();
            });
        }

        // ---- Validaciones ----

        private ResultadoOperacion Validar(Actividad actividad, int idUsuarioGestor)
        {
            if (string.IsNullOrWhiteSpace(actividad.Nombre))
            {
                return ResultadoOperacion.Error("Ingresá el nombre de la actividad.");
            }

            EspacioArtistico espacio = _mppEspacio.ObtenerPorId(
                new EspacioArtistico { IdEspacioArtistico = actividad.IdEspacioArtistico });
            if (espacio == null || espacio.IdUsuarioGestor != idUsuarioGestor)
            {
                return ResultadoOperacion.Error("El espacio indicado no existe o no te pertenece.");
            }

            if (actividad.MinutoDesde < 0 || actividad.MinutoHasta > 1440 || actividad.MinutoHasta <= actividad.MinutoDesde)
            {
                return ResultadoOperacion.Error("Ingresá un horario de inicio y fin válidos.");
            }

            if (actividad.CupoMaximo < 1 || actividad.CupoMaximo > 10000)
            {
                return ResultadoOperacion.Error("Ingresá un cupo máximo válido.");
            }

            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
            {
                if (actividad.DiasSemana == null || actividad.DiasSemana.Count == 0)
                {
                    return ResultadoOperacion.Error("Seleccioná uno o más días de la semana.");
                }
            }
            else if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString())
            {
                if (!actividad.SemanaDelMes.HasValue || !actividad.DiaSemanaMensual.HasValue)
                {
                    return ResultadoOperacion.Error("Indicá la semana del mes y el día para la recurrencia mensual.");
                }
            }
            else if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString())
            {
                if (string.IsNullOrEmpty(actividad.Fecha))
                {
                    return ResultadoOperacion.Error("Indicá la fecha puntual de la actividad.");
                }
            }
            else
            {
                return ResultadoOperacion.Error("El modo de recurrencia indicado no es válido.");
            }

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
            return espacio != null && espacio.IdUsuarioGestor == idUsuarioGestor;
        }

        // ---- Validación contra reservas existentes ----

        // El Word (CU-001-009, alternativos A5/A8/A10) exige que no se pueda
        // crear, modificar ni dar de baja una actividad si su horario
        // coincide con una reserva ya aceptada o una solicitud pendiente
        // para el mismo espacio. El sentido inverso (una reserva nueva
        // contra un horario ya bloqueado por una actividad) ya está cubierto
        // en BLL_Reserva.ValidarHorarioSolicitado, que sólo permite reservar
        // franjas no bloqueadas; este chequeo cubre el caso contrario: que
        // ya exista una reserva para ese horario antes de crear/modificar la
        // actividad.
        private bool ExisteConflictoConReservas(Actividad actividad)
        {
            List<Reserva> reservasActivas = _mppReserva.ListarActivasPorEspacio(actividad.IdEspacioArtistico);
            if (reservasActivas.Count == 0)
            {
                return false;
            }

            List<DateTime> fechasMensuales = actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString()
                ? CalcularFechasMensuales(
                    actividad.SemanaDelMes.Value, actividad.DiaSemanaMensual.Value,
                    DateTime.Now.Date, HorizonteMesesRecurrenciaMensual)
                : null;

            DateTime? fechaPuntual = actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString()
                ? DateTime.ParseExact(actividad.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                : (DateTime?)null;

            foreach (Reserva reserva in reservasActivas)
            {
                if (!reserva.MinutoDesde.HasValue || !reserva.MinutoHasta.HasValue)
                {
                    continue;
                }

                bool seSuperponenHorarios = actividad.MinutoDesde < reserva.MinutoHasta.Value
                    && reserva.MinutoDesde.Value < actividad.MinutoHasta;
                if (!seSuperponenHorarios)
                {
                    continue;
                }

                bool coincideFecha;
                if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
                {
                    int diaSemanaReserva = reserva.FechaSolicitada.DayOfWeek == DayOfWeek.Sunday
                        ? 7 : (int)reserva.FechaSolicitada.DayOfWeek;
                    coincideFecha = actividad.DiasSemana.Contains(diaSemanaReserva);
                }
                else if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString())
                {
                    coincideFecha = fechasMensuales.Exists(fecha => fecha.Date == reserva.FechaSolicitada.Date);
                }
                else
                {
                    coincideFecha = fechaPuntual.HasValue && fechaPuntual.Value.Date == reserva.FechaSolicitada.Date;
                }

                if (coincideFecha)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- Cálculo y regeneración de las franjas bloqueadas ----

        private void RegenerarFranjasBloqueadas(int idActividad, Actividad actividad)
        {
            _mppActividad.EliminarFranjasPorActividad(idActividad);

            if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Semanal.ToString())
            {
                foreach (int dia in actividad.DiasSemana.Distinct())
                {
                    _mppActividad.InsertarFranjaDesdeActividad(
                        actividad.IdEspacioArtistico, idActividad, dia, null,
                        actividad.MinutoDesde, actividad.MinutoHasta);
                }
            }
            else if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Fecha.ToString())
            {
                _mppActividad.InsertarFranjaDesdeActividad(
                    actividad.IdEspacioArtistico, idActividad, null, actividad.Fecha,
                    actividad.MinutoDesde, actividad.MinutoHasta);
            }
            else if (actividad.ModoRecurrencia == ModoRecurrenciaActividad.Mensual.ToString())
            {
                foreach (DateTime fecha in CalcularFechasMensuales(
                    actividad.SemanaDelMes.Value, actividad.DiaSemanaMensual.Value,
                    DateTime.Now.Date, HorizonteMesesRecurrenciaMensual))
                {
                    _mppActividad.InsertarFranjaDesdeActividad(
                        actividad.IdEspacioArtistico, idActividad, null,
                        fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        actividad.MinutoDesde, actividad.MinutoHasta);
                }
            }
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
