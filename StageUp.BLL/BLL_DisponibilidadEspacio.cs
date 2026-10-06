using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-001-008 Gestionar disponibilidad de espacios artísticos.
    //
    // La disponibilidad son las franjas con origen 'Manual' de FranjaEspacio:
    //  - abiertas (Bloqueado = false): semanales (día de la semana) o de una
    //    fecha concreta; la de fecha concreta reemplaza el horario semanal de
    //    ese día (excepción);
    //  - bloqueos manuales (Bloqueado = true, A8) con su motivo.
    // Las actividades internas generan sus propios bloqueos (origen
    // 'Actividad') y se administran desde "Mis actividades".
    //
    // Antes de guardar cualquier cambio se controla que no se superponga
    // con otra disponibilidad (A4), con un bloqueo, con reservas aceptadas,
    // solicitudes pendientes o actividades internas (A5), y que no deje sin
    // horario a una reserva o solicitud que ya existe (A7, A9, A11).
    public class BLL_DisponibilidadEspacio
    {
        public const string TipoEntidadBitacora = "Disponibilidad";

        public const int MaximoFranjasPorEspacio = 100;
        public const int LongitudMinimaMotivo = 5;
        public const int LongitudMaximaMotivo = 300;

        private const string OrigenActividad = "Actividad";
        private const string FormatoFecha = "yyyy-MM-dd";

        private static readonly string[] NombresDias =
            { string.Empty, "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly MPP_FranjaEspacio _mppFranja = new MPP_FranjaEspacio();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // ------------------------------------------------------------------
        // Reglas compartidas con BLL_Reserva, la búsqueda (script 56) y el
        // planificador del detalle del espacio.
        // ------------------------------------------------------------------

        public static int DiaSemanaDe(DateTime fecha)
        {
            return fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;
        }

        public static bool EsDeFechaConcreta(FranjaEspacio franja)
        {
            return franja != null && !string.IsNullOrEmpty(franja.Fecha);
        }

        public static bool EsBloqueoManual(FranjaEspacio franja)
        {
            return franja != null && franja.Bloqueado && franja.Origen != OrigenActividad;
        }

        public static bool EsBloqueoDeActividad(FranjaEspacio franja)
        {
            return franja != null && franja.Origen == OrigenActividad;
        }

        // ¿La franja abierta rige en esa fecha? Si ese día hay alguna franja
        // abierta de fecha concreta, el horario semanal no se usa.
        public static bool AbiertaRigeEnFecha(FranjaEspacio franja, List<FranjaEspacio> todas, DateTime fecha)
        {
            if (franja == null || franja.Bloqueado)
            {
                return false;
            }

            string fechaTexto = fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);
            bool hayExcepcion = (todas ?? new List<FranjaEspacio>()).Exists(f =>
                f != null && !f.Bloqueado && string.Equals(f.Fecha, fechaTexto, StringComparison.Ordinal));

            return hayExcepcion
                ? string.Equals(franja.Fecha, fechaTexto, StringComparison.Ordinal)
                : !EsDeFechaConcreta(franja) && franja.DiaSemana == DiaSemanaDe(fecha);
        }

        // Los bloqueos (manuales o de actividades) rigen en su fecha o, si son
        // semanales, todos los días de ese día de la semana.
        public static bool BloqueoRigeEnFecha(FranjaEspacio franja, DateTime fecha)
        {
            if (franja == null || !franja.Bloqueado)
            {
                return false;
            }

            return EsDeFechaConcreta(franja)
                ? string.Equals(franja.Fecha, fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture), StringComparison.Ordinal)
                : franja.DiaSemana == DiaSemanaDe(fecha);
        }

        // Un horario de una fecha se puede usar si está dentro de una franja
        // abierta que rige ese día y no se superpone con ningún bloqueo.
        public static bool HorarioCubierto(List<FranjaEspacio> franjas, DateTime fecha, int minutoDesde, int minutoHasta)
        {
            franjas = franjas ?? new List<FranjaEspacio>();
            bool dentroDeDisponibilidad = franjas.Exists(f =>
                AbiertaRigeEnFecha(f, franjas, fecha) && f.MinutoDesde <= minutoDesde && minutoHasta <= f.MinutoHasta);
            if (!dentroDeDisponibilidad)
            {
                return false;
            }

            return !franjas.Exists(f =>
                BloqueoRigeEnFecha(f, fecha) && SeSuperponen(f.MinutoDesde, f.MinutoHasta, minutoDesde, minutoHasta));
        }

        // ------------------------------------------------------------------
        // Consultas para la pantalla "Disponibilidad del espacio"
        // ------------------------------------------------------------------

        // Espacio activo del gestor, con sus franjas.
        public ResultadoOperacion<EspacioArtistico> ObtenerEspacioDelGestor(int idEspacioArtistico, int idUsuarioGestor)
        {
            try
            {
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
                if (espacio == null || !espacio.Activo)
                {
                    return ResultadoOperacion<EspacioArtistico>.Error("El espacio no existe o ya fue dado de baja.");
                }

                if (espacio.IdUsuarioGestor != idUsuarioGestor)
                {
                    return ResultadoOperacion<EspacioArtistico>.Error("No tenés permiso para administrar este espacio.");
                }

                if (espacio.Ficha == null)
                {
                    espacio.Ficha = new FichaEspacio();
                }

                return ResultadoOperacion<EspacioArtistico>.Ok(espacio);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<EspacioArtistico>.Error(ex.Message);
            }
        }

        // Para la pantalla: reservas aceptadas y solicitudes pendientes de hoy
        // en adelante, sin propagar errores de acceso a datos.
        public ResultadoOperacion<List<Reserva>> ConsultarOperacionesVigentes(int idEspacioArtistico)
        {
            try
            {
                return ResultadoOperacion<List<Reserva>>.Ok(ListarOperacionesVigentes(idEspacioArtistico));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<Reserva>>.Error(ex.Message);
            }
        }

        // Reservas aceptadas y solicitudes pendientes de hoy en adelante.
        private List<Reserva> ListarOperacionesVigentes(int idEspacioArtistico)
        {
            return _mppReserva.ListarActivasPorEspacio(new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico })
                .Where(r => r.MinutoDesde.HasValue && r.MinutoHasta.HasValue)
                .ToList();
        }

        // Disponibilidad configurada (abierta), sin las fechas que ya pasaron.
        public static List<FranjaEspacio> ListarDisponibilidad(EspacioArtistico espacio)
        {
            return Ordenar(Franjas(espacio).Where(f => !f.Bloqueado && f.Origen != OrigenActividad && !YaPaso(f)));
        }

        // Bloqueos manuales vigentes.
        public static List<FranjaEspacio> ListarBloqueos(EspacioArtistico espacio)
        {
            return Ordenar(Franjas(espacio).Where(f => EsBloqueoManual(f) && !YaPaso(f)));
        }

        public static FranjaEspacio BuscarFranjaManual(EspacioArtistico espacio, int idFranjaEspacio)
        {
            return Franjas(espacio).FirstOrDefault(f =>
                f.IdFranjaEspacio == idFranjaEspacio && f.Origen != OrigenActividad);
        }

        // A6/A10 paso 2: lo que tiene asociado una franja.
        public static OperacionesAsociadasFranja ContarOperacionesAsociadas(
            EspacioArtistico espacio, FranjaEspacio franja, List<Reserva> operaciones)
        {
            OperacionesAsociadasFranja resultado = new OperacionesAsociadasFranja();
            if (franja == null)
            {
                return resultado;
            }

            foreach (Reserva reserva in ReservasAsociadas(franja, operaciones))
            {
                if (reserva.EstadoReserva == "Aceptada")
                {
                    resultado.ReservasAceptadas++;
                }
                else
                {
                    resultado.SolicitudesPendientes++;
                }
            }

            resultado.Actividades = ActividadesAsociadas(espacio, franja).Count;
            return resultado;
        }

        // Calendario (paso 9) de "cantidadDias" días desde "desde".
        public static List<DiaCalendarioDisponibilidad> ArmarCalendario(
            EspacioArtistico espacio, List<Reserva> operaciones, DateTime desde, int cantidadDias)
        {
            List<FranjaEspacio> franjas = Franjas(espacio);
            List<DiaCalendarioDisponibilidad> dias = new List<DiaCalendarioDisponibilidad>();
            DateTime hoy = DateTime.Now.Date;

            for (int i = 0; i < cantidadDias; i++)
            {
                DateTime fecha = desde.Date.AddDays(i);
                string fechaTexto = fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);
                DiaCalendarioDisponibilidad dia = new DiaCalendarioDisponibilidad
                {
                    Fecha = fecha,
                    EsHoy = fecha == hoy,
                    TieneExcepcion = franjas.Exists(f => !f.Bloqueado && f.Fecha == fechaTexto)
                };

                foreach (FranjaEspacio franja in franjas)
                {
                    if (AbiertaRigeEnFecha(franja, franjas, fecha))
                    {
                        dia.Items.Add(new ItemCalendarioDisponibilidad
                        {
                            Tipo = ItemCalendarioDisponibilidad.TipoDisponible,
                            MinutoDesde = franja.MinutoDesde,
                            MinutoHasta = franja.MinutoHasta,
                            Detalle = dia.TieneExcepcion ? "Disponible (horario especial de esta fecha)" : "Disponible"
                        });
                    }
                    else if (BloqueoRigeEnFecha(franja, fecha))
                    {
                        bool deActividad = EsBloqueoDeActividad(franja);
                        dia.Items.Add(new ItemCalendarioDisponibilidad
                        {
                            Tipo = deActividad ? ItemCalendarioDisponibilidad.TipoActividad : ItemCalendarioDisponibilidad.TipoBloqueo,
                            MinutoDesde = franja.MinutoDesde,
                            MinutoHasta = franja.MinutoHasta,
                            Detalle = deActividad
                                ? "Actividad interna" + (string.IsNullOrWhiteSpace(franja.NombreActividad) ? string.Empty : ": " + franja.NombreActividad)
                                : "Bloqueado" + (string.IsNullOrWhiteSpace(franja.MotivoBloqueo) ? string.Empty : ": " + franja.MotivoBloqueo)
                        });
                    }
                }

                foreach (Reserva reserva in (operaciones ?? new List<Reserva>()).Where(r => r.FechaSolicitada.Date == fecha))
                {
                    bool aceptada = reserva.EstadoReserva == "Aceptada";
                    dia.Items.Add(new ItemCalendarioDisponibilidad
                    {
                        Tipo = aceptada ? ItemCalendarioDisponibilidad.TipoReservaAceptada : ItemCalendarioDisponibilidad.TipoSolicitudPendiente,
                        MinutoDesde = reserva.MinutoDesde.Value,
                        MinutoHasta = reserva.MinutoHasta.Value,
                        Detalle = aceptada ? "Reserva aceptada" : "Solicitud pendiente",
                        IdReserva = reserva.IdReserva
                    });
                }

                dia.Items = dia.Items
                    .OrderBy(item => item.MinutoDesde)
                    .ThenBy(item => item.Tipo == ItemCalendarioDisponibilidad.TipoDisponible ? 0 : 1)
                    .ToList();
                dias.Add(dia);
            }

            return dias;
        }

        // ------------------------------------------------------------------
        // Escenario principal: agregar disponibilidad (pasos 10 a 18)
        // ------------------------------------------------------------------
        // porFecha: true para una fecha concreta (horario especial de ese
        // día), false para uno o varios días de la semana.
        public ResultadoOperacion AgregarDisponibilidad(
            int idEspacioArtistico, int idUsuarioGestor, bool porFecha, List<int> diasSemana, string fecha, string horaDesde, string horaHasta)
        {
            ResultadoOperacion<EspacioArtistico> espacioResultado = ObtenerEspacioDelGestor(idEspacioArtistico, idUsuarioGestor);
            if (!espacioResultado.Exitoso)
            {
                return espacioResultado;
            }

            EspacioArtistico espacio = espacioResultado.Valor;
            List<int> dias = (diasSemana ?? new List<int>()).Distinct().OrderBy(d => d).ToList();

            // A2 y A3.
            int minutoDesde, minutoHasta;
            ResultadoOperacion datos = ValidarDatos(porFecha, dias, fecha, horaDesde, horaHasta, null, out minutoDesde, out minutoHasta);
            if (!datos.Exitoso)
            {
                return datos;
            }

            List<FranjaEspacio> nuevas = porFecha
                ? new List<FranjaEspacio> { NuevaFranja(null, fecha.Trim(), minutoDesde, minutoHasta, false, null) }
                : dias.Select(d => NuevaFranja(d, null, minutoDesde, minutoHasta, false, null)).ToList();

            if (Franjas(espacio).Count(f => f.Origen != OrigenActividad) + nuevas.Count > MaximoFranjasPorEspacio)
            {
                return ResultadoOperacion.Error("El espacio llegó al máximo de " + MaximoFranjasPorEspacio + " franjas. Eliminá alguna antes de agregar otra.", "A2");
            }

            try
            {
                List<Reserva> operaciones = ListarOperacionesVigentes(idEspacioArtistico);

                // A4 y A5: se informan todos los días con conflicto juntos.
                List<string> errores = new List<string>();
                string codigo = null;
                List<FranjaEspacio> simuladas = new List<FranjaEspacio>(Franjas(espacio));
                foreach (FranjaEspacio nueva in nuevas)
                {
                    ResultadoOperacion conflicto = ValidarConflictosDeAbierta(espacio, nueva, null, operaciones);
                    if (!conflicto.Exitoso)
                    {
                        errores.Add(conflicto.Mensaje);
                        codigo = codigo ?? conflicto.CodigoAlternativo;
                        continue;
                    }

                    simuladas.Add(nueva);
                }

                if (errores.Count == 0)
                {
                    Reserva afectada = PrimeraAfectada(Franjas(espacio), simuladas, operaciones);
                    if (afectada != null)
                    {
                        errores.Add("El horario no puede configurarse porque afectaría " + DescribirOperacion(afectada) +
                            ": ese día quedaría fuera de la disponibilidad.");
                        codigo = "A5";
                    }
                }

                if (errores.Count > 0)
                {
                    return ResultadoOperacion.Error(string.Join(" ", errores), codigo);
                }

                foreach (FranjaEspacio nueva in nuevas)
                {
                    int id = _mppFranja.InsertarManual(idEspacioArtistico, nueva);
                    _bitacora.Registrar(idUsuarioGestor, "ALTA", TipoEntidadBitacora, id,
                        "Alta de disponibilidad en \"" + espacio.NombreEspacio + "\": " + Describir(nueva) + ".");
                }

                return ResultadoOperacion.Ok(nuevas.Count == 1
                    ? "La disponibilidad se guardó correctamente."
                    : "La disponibilidad se guardó correctamente para " + nuevas.Count + " días.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // A6 / A7: modificar una disponibilidad existente
        // ------------------------------------------------------------------
        public ResultadoOperacion ModificarDisponibilidad(
            int idEspacioArtistico, int idUsuarioGestor, int idFranjaEspacio,
            int? diaSemana, string fecha, string horaDesde, string horaHasta)
        {
            ResultadoOperacion<EspacioArtistico> espacioResultado = ObtenerEspacioDelGestor(idEspacioArtistico, idUsuarioGestor);
            if (!espacioResultado.Exitoso)
            {
                return espacioResultado;
            }

            EspacioArtistico espacio = espacioResultado.Valor;
            FranjaEspacio original = BuscarFranjaManual(espacio, idFranjaEspacio);
            if (original == null || original.Bloqueado)
            {
                return ResultadoOperacion.Error("La disponibilidad que querés modificar ya no existe. Actualizá la pantalla.");
            }

            bool porFecha = EsDeFechaConcreta(original);
            List<int> dias = diaSemana.HasValue ? new List<int> { diaSemana.Value } : new List<int>();
            int minutoDesde, minutoHasta;
            ResultadoOperacion datos = ValidarDatos(porFecha, dias, fecha, horaDesde, horaHasta, original, out minutoDesde, out minutoHasta);
            if (!datos.Exitoso)
            {
                return datos;
            }

            FranjaEspacio nueva = NuevaFranja(porFecha ? (int?)null : diaSemana, porFecha ? fecha.Trim() : null, minutoDesde, minutoHasta, false, null);
            nueva.IdFranjaEspacio = original.IdFranjaEspacio;

            if (nueva.DiaSemana == original.DiaSemana && nueva.Fecha == original.Fecha &&
                nueva.MinutoDesde == original.MinutoDesde && nueva.MinutoHasta == original.MinutoHasta)
            {
                return ResultadoOperacion.Ok("No hubo cambios para guardar.");
            }

            try
            {
                List<Reserva> operaciones = ListarOperacionesVigentes(idEspacioArtistico);

                // A7: reservas o solicitudes de esta franja que quedarían afuera.
                List<FranjaEspacio> antes = Franjas(espacio);
                List<FranjaEspacio> despues = antes.Where(f => f.IdFranjaEspacio != original.IdFranjaEspacio).ToList();
                despues.Add(nueva);
                Reserva afectada = PrimeraAfectada(antes, despues, operaciones);
                if (afectada != null)
                {
                    return ResultadoOperacion.Error(
                        "No es posible modificar esta disponibilidad mientras existan operaciones vigentes o pendientes vinculadas a esa franja: " +
                        "el cambio dejaría afuera " + DescribirOperacion(afectada) +
                        ". Elegí otra franja o resolvé antes esa operación.", "A7");
                }

                // A6 paso 7: superposiciones con el resto.
                ResultadoOperacion conflicto = ValidarConflictosDeAbierta(espacio, nueva, original, operaciones);
                if (!conflicto.Exitoso)
                {
                    return conflicto;
                }

                if (!_mppFranja.ModificarManual(idEspacioArtistico, nueva))
                {
                    return ResultadoOperacion.Error("La disponibilidad que querés modificar ya no existe. Actualizá la pantalla.");
                }

                _bitacora.Registrar(idUsuarioGestor, "MODIFICACION", TipoEntidadBitacora, original.IdFranjaEspacio,
                    "Modificación de disponibilidad en \"" + espacio.NombreEspacio + "\": " + Describir(original) + " → " + Describir(nueva) + ".");
                return ResultadoOperacion.Ok("La disponibilidad se actualizó correctamente.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // A8 / A9: bloqueo manual de horario
        // ------------------------------------------------------------------
        public ResultadoOperacion BloquearHorario(
            int idEspacioArtistico, int idUsuarioGestor, bool porFecha, int? diaSemana, string fecha,
            string horaDesde, string horaHasta, string motivo)
        {
            ResultadoOperacion<EspacioArtistico> espacioResultado = ObtenerEspacioDelGestor(idEspacioArtistico, idUsuarioGestor);
            if (!espacioResultado.Exitoso)
            {
                return espacioResultado;
            }

            EspacioArtistico espacio = espacioResultado.Valor;
            List<int> dias = diaSemana.HasValue ? new List<int> { diaSemana.Value } : new List<int>();

            int minutoDesde, minutoHasta;
            ResultadoOperacion datos = ValidarDatos(porFecha, dias, fecha, horaDesde, horaHasta, null, out minutoDesde, out minutoHasta);
            string motivoLimpio = (motivo ?? string.Empty).Trim();
            List<string> faltantes = new List<string>();
            if (!datos.Exitoso && datos.CodigoAlternativo == "A2")
            {
                faltantes.Add(datos.Mensaje);
            }

            if (motivoLimpio.Length == 0)
            {
                faltantes.Add("Indicá el motivo del bloqueo.");
            }
            else if (motivoLimpio.Length < LongitudMinimaMotivo || motivoLimpio.Length > LongitudMaximaMotivo)
            {
                faltantes.Add("El motivo del bloqueo tiene que tener entre " + LongitudMinimaMotivo + " y " + LongitudMaximaMotivo + " caracteres.");
            }

            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Error(string.Join(" ", faltantes), "A2");
            }

            if (!datos.Exitoso)
            {
                return datos;
            }

            FranjaEspacio bloqueo = NuevaFranja(porFecha ? (int?)null : diaSemana, porFecha ? fecha.Trim() : null,
                minutoDesde, minutoHasta, true, motivoLimpio);

            if (Franjas(espacio).Count(f => f.Origen != OrigenActividad) + 1 > MaximoFranjasPorEspacio)
            {
                return ResultadoOperacion.Error("El espacio llegó al máximo de " + MaximoFranjasPorEspacio + " franjas. Eliminá alguna antes de agregar otra.", "A2");
            }

            try
            {
                List<Reserva> operaciones = ListarOperacionesVigentes(idEspacioArtistico);

                // A9: reservas aceptadas o solicitudes pendientes en ese horario.
                Reserva reserva = operaciones.FirstOrDefault(r =>
                    RigeEnFecha(bloqueo, r.FechaSolicitada) &&
                    SeSuperponen(bloqueo.MinutoDesde, bloqueo.MinutoHasta, r.MinutoDesde.Value, r.MinutoHasta.Value));
                if (reserva != null)
                {
                    return ResultadoOperacion.Error(
                        "No es posible bloquear el horario porque afectaría operaciones ya generadas: hay " + DescribirOperacion(reserva) +
                        ". Cambiá la franja del bloqueo o resolvé antes esa operación.", "A9");
                }

                // A8 paso 5: tampoco sobre actividades internas registradas.
                FranjaEspacio actividad = Franjas(espacio).FirstOrDefault(f =>
                    EsBloqueoDeActividad(f) && !YaPaso(f) && CompartenDias(f, bloqueo) &&
                    SeSuperponen(f.MinutoDesde, f.MinutoHasta, bloqueo.MinutoDesde, bloqueo.MinutoHasta));
                if (actividad != null)
                {
                    return ResultadoOperacion.Error(
                        "No es posible bloquear el horario porque se superpone con la actividad interna " + DescribirActividad(actividad) +
                        ". Cambiá la franja del bloqueo.", "A9");
                }

                FranjaEspacio otroBloqueo = ListarBloqueos(espacio).FirstOrDefault(f =>
                    CompartenDias(f, bloqueo) && SeSuperponen(f.MinutoDesde, f.MinutoHasta, bloqueo.MinutoDesde, bloqueo.MinutoHasta));
                if (otroBloqueo != null)
                {
                    return ResultadoOperacion.Error("Ese horario ya tiene un bloqueo (" + Describir(otroBloqueo) + ").", "A9");
                }

                int id = _mppFranja.InsertarManual(idEspacioArtistico, bloqueo);
                _bitacora.Registrar(idUsuarioGestor, "BLOQUEO", TipoEntidadBitacora, id,
                    "Bloqueo de horario en \"" + espacio.NombreEspacio + "\": " + Describir(bloqueo) + ". Motivo: " + motivoLimpio + ".");
                return ResultadoOperacion.Ok("El horario fue bloqueado correctamente. No se va a poder elegir para nuevas reservas.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // A10 / A11: eliminar una disponibilidad (o quitar un bloqueo manual)
        // ------------------------------------------------------------------
        public ResultadoOperacion EliminarFranja(int idEspacioArtistico, int idUsuarioGestor, int idFranjaEspacio)
        {
            ResultadoOperacion<EspacioArtistico> espacioResultado = ObtenerEspacioDelGestor(idEspacioArtistico, idUsuarioGestor);
            if (!espacioResultado.Exitoso)
            {
                return espacioResultado;
            }

            EspacioArtistico espacio = espacioResultado.Valor;
            FranjaEspacio franja = BuscarFranjaManual(espacio, idFranjaEspacio);
            if (franja == null)
            {
                return ResultadoOperacion.Error("La franja que querés eliminar ya no existe. Actualizá la pantalla.");
            }

            try
            {
                if (!franja.Bloqueado)
                {
                    List<Reserva> operaciones = ListarOperacionesVigentes(idEspacioArtistico);
                    Reserva asociada = ReservasAsociadas(franja, operaciones).FirstOrDefault();
                    List<FranjaEspacio> despues = Franjas(espacio).Where(f => f.IdFranjaEspacio != franja.IdFranjaEspacio).ToList();
                    Reserva afectada = asociada ?? PrimeraAfectada(Franjas(espacio), despues, operaciones);
                    if (afectada != null)
                    {
                        return ResultadoOperacion.Error(
                            "No es posible eliminar la disponibilidad porque afectaría operaciones ya generadas: hay " + DescribirOperacion(afectada) +
                            ". Elegí otra disponibilidad o resolvé antes esa operación.", "A11");
                    }

                    List<FranjaEspacio> actividades = ActividadesAsociadas(espacio, franja);
                    if (actividades.Count > 0)
                    {
                        return ResultadoOperacion.Error(
                            "No es posible eliminar la disponibilidad porque tiene asociada la actividad interna " + DescribirActividad(actividades[0]) +
                            ". Modificá o dá de baja la actividad desde Mis actividades, o elegí otra disponibilidad.", "A11");
                    }
                }

                if (!_mppFranja.EliminarManual(idEspacioArtistico, idFranjaEspacio))
                {
                    return ResultadoOperacion.Error("La franja que querés eliminar ya no existe. Actualizá la pantalla.");
                }

                _bitacora.Registrar(idUsuarioGestor, "BAJA", TipoEntidadBitacora, idFranjaEspacio,
                    (franja.Bloqueado ? "Se quitó el bloqueo" : "Baja de disponibilidad") +
                    " en \"" + espacio.NombreEspacio + "\": " + Describir(franja) + ".");

                if (franja.Bloqueado)
                {
                    return ResultadoOperacion.Ok("Se quitó el bloqueo. Ese horario vuelve a estar disponible si está dentro de la disponibilidad configurada.");
                }

                bool quedaDisponibilidad = Franjas(espacio).Exists(f =>
                    f.IdFranjaEspacio != franja.IdFranjaEspacio && !f.Bloqueado && !YaPaso(f));
                return ResultadoOperacion.Ok(quedaDisponibilidad
                    ? "La disponibilidad fue eliminada correctamente."
                    : "La disponibilidad fue eliminada correctamente. El espacio quedó sin horarios disponibles: no va a recibir reservas hasta que agregues uno.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // Textos
        // ------------------------------------------------------------------

        public static string NombreDia(int diaSemana)
        {
            return diaSemana >= 1 && diaSemana <= 7 ? NombresDias[diaSemana] : string.Empty;
        }

        public static string FormatearHora(int minutos)
        {
            if (minutos >= 1440)
            {
                return "24:00";
            }

            return (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        // Para los <input type="time">: el fin del día se carga como 00:00.
        public static string HoraParaFormulario(int minutos)
        {
            return minutos >= 1440 ? "00:00" : FormatearHora(minutos);
        }

        public static string DescribirCuando(FranjaEspacio franja)
        {
            if (EsDeFechaConcreta(franja))
            {
                DateTime fecha;
                return DateTime.TryParseExact(franja.Fecha, FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                    ? NombreDia(DiaSemanaDe(fecha)) + " " + fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                    : franja.Fecha;
            }

            return "Todos los " + (franja.DiaSemana == 6 || franja.DiaSemana == 7
                ? NombreDia(franja.DiaSemana ?? 0).ToLowerInvariant() + "s"
                : NombreDia(franja.DiaSemana ?? 0).ToLowerInvariant());
        }

        public static string Describir(FranjaEspacio franja)
        {
            return DescribirCuando(franja) + " de " + FormatearHora(franja.MinutoDesde) + " a " + FormatearHora(franja.MinutoHasta);
        }

        // ------------------------------------------------------------------
        // Validaciones internas
        // ------------------------------------------------------------------

        // A2 (campos obligatorios, todos juntos) y A3 (franja válida).
        private static ResultadoOperacion ValidarDatos(
            bool porFecha, List<int> dias, string fecha, string horaDesde, string horaHasta, FranjaEspacio original,
            out int minutoDesde, out int minutoHasta)
        {
            minutoDesde = 0;
            minutoHasta = 0;
            List<string> faltantes = new List<string>();

            if (porFecha && string.IsNullOrWhiteSpace(fecha))
            {
                faltantes.Add("Elegí la fecha.");
            }
            else if (!porFecha && dias.Count == 0)
            {
                faltantes.Add("Elegí al menos un día de la semana.");
            }

            if (string.IsNullOrWhiteSpace(horaDesde))
            {
                faltantes.Add("Indicá el horario de inicio.");
            }

            if (string.IsNullOrWhiteSpace(horaHasta))
            {
                faltantes.Add("Indicá el horario de finalización.");
            }

            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Error(string.Join(" ", faltantes), "A2");
            }

            if (!porFecha && dias.Exists(d => d < 1 || d > 7))
            {
                return ResultadoOperacion.Error("Elegí días de la semana válidos.", "A2");
            }

            if (porFecha)
            {
                DateTime fechaElegida;
                if (!DateTime.TryParseExact(fecha.Trim(), FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaElegida))
                {
                    return ResultadoOperacion.Error("La fecha ingresada no es válida.", "A2");
                }

                if (fechaElegida.Date < DateTime.Now.Date)
                {
                    return ResultadoOperacion.Error("Elegí una fecha de hoy en adelante.", "A3");
                }

                if (fechaElegida.Date > DateTime.Now.Date.AddYears(2))
                {
                    return ResultadoOperacion.Error("Elegí una fecha dentro de los próximos dos años.", "A3");
                }
            }

            if (!TryLeerHora(horaDesde, false, out minutoDesde) || !TryLeerHora(horaHasta, true, out minutoHasta))
            {
                return ResultadoOperacion.Error("Ingresá los horarios con el formato hh:mm.", "A3");
            }

            if (minutoDesde >= minutoHasta)
            {
                return ResultadoOperacion.Error("La franja horaria ingresada no es válida: el horario de inicio tiene que ser anterior al de finalización.", "A3");
            }

            if (minutoDesde % 30 != 0 || minutoHasta % 30 != 0)
            {
                return ResultadoOperacion.Error("Usá horarios en punto o y media (por ejemplo 09:00 o 09:30).", "A3");
            }

            return ResultadoOperacion.Ok();
        }

        // "HH:mm". Como fin, 00:00 (o 24:00) es el cierre del día.
        public static bool TryLeerHora(string texto, bool esFin, out int minutos)
        {
            minutos = 0;
            string limpio = (texto ?? string.Empty).Trim();
            if (esFin && (limpio == "24:00" || limpio == "00:00"))
            {
                minutos = 1440;
                return true;
            }

            DateTime hora;
            if (!DateTime.TryParseExact(limpio, new[] { "HH:mm", "H:mm", "HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out hora))
            {
                return false;
            }

            minutos = hora.Hour * 60 + hora.Minute;
            return true;
        }

        // A4, franja bloqueada y A5 para una franja abierta nueva o modificada.
        private static ResultadoOperacion ValidarConflictosDeAbierta(
            EspacioArtistico espacio, FranjaEspacio nueva, FranjaEspacio original, List<Reserva> operaciones)
        {
            List<FranjaEspacio> franjas = Franjas(espacio);

            // A4: otra disponibilidad del mismo tipo para el mismo día.
            FranjaEspacio superpuesta = franjas.FirstOrDefault(f =>
                !f.Bloqueado && f.Origen != OrigenActividad && !YaPaso(f) &&
                (original == null || f.IdFranjaEspacio != original.IdFranjaEspacio) &&
                EsDeFechaConcreta(f) == EsDeFechaConcreta(nueva) && CompartenDias(f, nueva) &&
                SeSuperponen(f.MinutoDesde, f.MinutoHasta, nueva.MinutoDesde, nueva.MinutoHasta));
            if (superpuesta != null)
            {
                return ResultadoOperacion.Error(
                    "Ya existe disponibilidad registrada para ese día y horario (" + Describir(superpuesta) + "). Cambiá el día o la franja horaria.", "A4");
            }

            // Paso 14: no puede caer sobre un horario bloqueado.
            FranjaEspacio bloqueo = franjas.FirstOrDefault(f =>
                EsBloqueoManual(f) && !YaPaso(f) && BloqueoCubreTodosLosDias(f, nueva) &&
                SeSuperponen(f.MinutoDesde, f.MinutoHasta, nueva.MinutoDesde, nueva.MinutoHasta));
            if (bloqueo != null)
            {
                return ResultadoOperacion.Error(
                    "Ese horario está bloqueado (" + Describir(bloqueo) +
                    (string.IsNullOrWhiteSpace(bloqueo.MotivoBloqueo) ? string.Empty : ", motivo: " + bloqueo.MotivoBloqueo) +
                    "). Quitá el bloqueo o elegí otra franja.", "A5");
            }

            // A5: actividades internas registradas (las que ya estaban dentro
            // de la franja original son de esa franja y no cuentan).
            FranjaEspacio actividad = franjas.FirstOrDefault(f =>
                EsBloqueoDeActividad(f) && !YaPaso(f) && CompartenDias(f, nueva) &&
                SeSuperponen(f.MinutoDesde, f.MinutoHasta, nueva.MinutoDesde, nueva.MinutoHasta) &&
                !(original != null && CompartenDias(f, original) &&
                  SeSuperponen(f.MinutoDesde, f.MinutoHasta, original.MinutoDesde, original.MinutoHasta)));
            if (actividad != null)
            {
                return ResultadoOperacion.Error(
                    "El horario no puede configurarse porque afectaría la actividad interna " + DescribirActividad(actividad) +
                    ". Cambiá el día o la franja horaria.", "A5");
            }

            // A5: reservas aceptadas y solicitudes pendientes.
            Reserva reserva = (operaciones ?? new List<Reserva>()).FirstOrDefault(r =>
                RigeEnFecha(nueva, r.FechaSolicitada) &&
                SeSuperponen(nueva.MinutoDesde, nueva.MinutoHasta, r.MinutoDesde.Value, r.MinutoHasta.Value) &&
                !(original != null && RigeEnFecha(original, r.FechaSolicitada) &&
                  SeSuperponen(original.MinutoDesde, original.MinutoHasta, r.MinutoDesde.Value, r.MinutoHasta.Value)));
            if (reserva != null)
            {
                return ResultadoOperacion.Error(
                    "El horario no puede configurarse porque afectaría una operación ya registrada: " + DescribirOperacion(reserva) +
                    ". Cambiá el día o la franja horaria.", "A5");
            }

            return ResultadoOperacion.Ok();
        }

        // Reservas y solicitudes que hoy se pueden atender y dejarían de
        // estar dentro de la disponibilidad con el cambio.
        private static Reserva PrimeraAfectada(List<FranjaEspacio> antes, List<FranjaEspacio> despues, List<Reserva> operaciones)
        {
            return (operaciones ?? new List<Reserva>()).FirstOrDefault(r =>
                HorarioCubierto(antes, r.FechaSolicitada, r.MinutoDesde.Value, r.MinutoHasta.Value) &&
                !HorarioCubierto(despues, r.FechaSolicitada, r.MinutoDesde.Value, r.MinutoHasta.Value));
        }

        private static List<Reserva> ReservasAsociadas(FranjaEspacio franja, List<Reserva> operaciones)
        {
            return (operaciones ?? new List<Reserva>()).Where(r =>
                r.MinutoDesde.HasValue && r.MinutoHasta.HasValue &&
                RigeEnFecha(franja, r.FechaSolicitada) &&
                SeSuperponen(franja.MinutoDesde, franja.MinutoHasta, r.MinutoDesde.Value, r.MinutoHasta.Value))
                .ToList();
        }

        private static List<FranjaEspacio> ActividadesAsociadas(EspacioArtistico espacio, FranjaEspacio franja)
        {
            return Franjas(espacio).Where(f =>
                EsBloqueoDeActividad(f) && !YaPaso(f) && CompartenDias(f, franja) &&
                SeSuperponen(f.MinutoDesde, f.MinutoHasta, franja.MinutoDesde, franja.MinutoHasta))
                .ToList();
        }

        // ¿La franja (por su patrón, sin mirar excepciones) cae en esa fecha?
        private static bool RigeEnFecha(FranjaEspacio franja, DateTime fecha)
        {
            return EsDeFechaConcreta(franja)
                ? franja.Fecha == fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture)
                : franja.DiaSemana == DiaSemanaDe(fecha);
        }

        // ¿Hay algún día en que rijan las dos franjas?
        internal static bool CompartenDias(FranjaEspacio a, FranjaEspacio b)
        {
            bool aFecha = EsDeFechaConcreta(a);
            bool bFecha = EsDeFechaConcreta(b);
            if (aFecha && bFecha)
            {
                return a.Fecha == b.Fecha;
            }

            if (!aFecha && !bFecha)
            {
                return a.DiaSemana == b.DiaSemana;
            }

            FranjaEspacio deFecha = aFecha ? a : b;
            FranjaEspacio semanal = aFecha ? b : a;
            DateTime fecha;
            return DateTime.TryParseExact(deFecha.Fecha, FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha) &&
                DiaSemanaDe(fecha) == semanal.DiaSemana;
        }

        // Un bloqueo de una sola fecha no impide cargar el horario semanal de
        // ese día (es una excepción); uno semanal sí.
        private static bool BloqueoCubreTodosLosDias(FranjaEspacio bloqueo, FranjaEspacio franja)
        {
            if (!EsDeFechaConcreta(franja))
            {
                return !EsDeFechaConcreta(bloqueo) && bloqueo.DiaSemana == franja.DiaSemana;
            }

            return CompartenDias(bloqueo, franja);
        }

        private static bool SeSuperponen(int desde1, int hasta1, int desde2, int hasta2)
        {
            return desde1 < hasta2 && desde2 < hasta1;
        }

        internal static bool YaPaso(FranjaEspacio franja)
        {
            DateTime fecha;
            return EsDeFechaConcreta(franja) &&
                DateTime.TryParseExact(franja.Fecha, FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha) &&
                fecha.Date < DateTime.Now.Date;
        }

        private static List<FranjaEspacio> Franjas(EspacioArtistico espacio)
        {
            return espacio == null || espacio.Ficha == null || espacio.Ficha.Disponibilidad == null
                ? new List<FranjaEspacio>()
                : espacio.Ficha.Disponibilidad.Where(f => f != null).ToList();
        }

        private static List<FranjaEspacio> Ordenar(IEnumerable<FranjaEspacio> franjas)
        {
            return franjas
                .OrderBy(f => EsDeFechaConcreta(f) ? 1 : 0)
                .ThenBy(f => EsDeFechaConcreta(f) ? f.Fecha : string.Empty, StringComparer.Ordinal)
                .ThenBy(f => f.DiaSemana ?? 0)
                .ThenBy(f => f.MinutoDesde)
                .ToList();
        }

        private static FranjaEspacio NuevaFranja(int? diaSemana, string fecha, int minutoDesde, int minutoHasta, bool bloqueado, string motivo)
        {
            return new FranjaEspacio
            {
                DiaSemana = diaSemana,
                Fecha = fecha,
                MinutoDesde = minutoDesde,
                MinutoHasta = minutoHasta,
                Bloqueado = bloqueado,
                Origen = "Manual",
                MotivoBloqueo = bloqueado ? motivo : null
            };
        }

        private static string DescribirOperacion(Reserva reserva)
        {
            return (reserva.EstadoReserva == "Aceptada" ? "una reserva aceptada" : "una solicitud de reserva pendiente") +
                " el " + reserva.FechaSolicitada.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) +
                " de " + FormatearHora(reserva.MinutoDesde.Value) + " a " + FormatearHora(reserva.MinutoHasta.Value);
        }

        private static string DescribirActividad(FranjaEspacio actividad)
        {
            return (string.IsNullOrWhiteSpace(actividad.NombreActividad) ? string.Empty : "«" + actividad.NombreActividad + "» ") +
                "(" + Describir(actividad) + ")";
        }
    }
}
