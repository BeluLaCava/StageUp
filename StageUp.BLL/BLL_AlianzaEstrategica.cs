using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-004-003 Gestionar alianzas estratégicas.
    //   * Alta, consulta y modificación (A6), seguimiento (A7), cambio de
    //     estado (A8), renovación con historial de vigencias (A9),
    //     finalización (A10) y baja lógica (A11).
    //   * Una alianza finalizada o dada de baja solo se consulta (A13); con el
    //     permiso REACTIVAR_REGISTROS_COMERCIALES se la puede renovar, y así
    //     vuelve a estar activa.
    //   * Vínculos con contactos, campañas, contenido promocional y orígenes
    //     (A14 a A16, A23) y detección de duplicados (A22).
    public class BLL_AlianzaEstrategica
    {
        public const string PermisoGestionar = "GESTIONAR_ALIANZAS_ESTRATEGICAS";
        public const string PermisoReactivar = "REACTIVAR_REGISTROS_COMERCIALES";
        public const string EntidadBitacora = "AlianzaEstrategica";

        public static readonly Dictionary<string, string> Estados = new Dictionary<string, string>
        {
            { AlianzaEstrategica.EstadoEnEvaluacion, "En evaluación" },
            { AlianzaEstrategica.EstadoActiva, "Activa" },
            { AlianzaEstrategica.EstadoPausada, "Pausada" },
            { AlianzaEstrategica.EstadoFinalizada, "Finalizada" },
            { AlianzaEstrategica.EstadoDadaDeBaja, "Dada de baja" }
        };

        private readonly MPP_AlianzaEstrategica _mpp = new MPP_AlianzaEstrategica();
        private readonly BLL_CatalogoComercial _catalogos = new BLL_CatalogoComercial();

        public static string TextoEstado(string estado)
        {
            string texto;
            return Estados.TryGetValue(estado ?? string.Empty, out texto) ? texto : estado;
        }

        public static string ClaseEstado(string estado)
        {
            switch (estado)
            {
                case AlianzaEstrategica.EstadoActiva: return "comercial-badge-activa";
                case AlianzaEstrategica.EstadoEnEvaluacion: return "comercial-badge-planificada";
                case AlianzaEstrategica.EstadoPausada: return "comercial-badge-contactado";
                case AlianzaEstrategica.EstadoFinalizada: return "comercial-badge-finalizada";
                default: return "comercial-badge-baja";
            }
        }

        public static bool PuedeEditarse(AlianzaEstrategica a)
        {
            return a != null && a.EnGestion;
        }

        // A9: activa, pausada (incluye las vencidas) o, con permiso, una histórica.
        public static bool PuedeRenovarse(AlianzaEstrategica a, bool puedeReactivar)
        {
            if (a == null)
            {
                return false;
            }

            return a.EstadoAlianza == AlianzaEstrategica.EstadoActiva || a.EstadoAlianza == AlianzaEstrategica.EstadoPausada ||
                (puedeReactivar && !a.EnGestion);
        }

        public static bool PuedeFinalizarse(AlianzaEstrategica a)
        {
            return a != null && (a.EstadoAlianza == AlianzaEstrategica.EstadoActiva || a.EstadoAlianza == AlianzaEstrategica.EstadoPausada);
        }

        public static bool PuedeDarseDeBaja(AlianzaEstrategica a)
        {
            return a != null && a.EstadoAlianza != AlianzaEstrategica.EstadoDadaDeBaja;
        }

        public static string TextoVigencia(AlianzaEstrategica a)
        {
            if (a == null)
            {
                return string.Empty;
            }

            string texto = a.FechaFin.HasValue
                ? a.FechaInicio.ToString("dd/MM/yyyy") + " al " + a.FechaFin.Value.ToString("dd/MM/yyyy")
                : "desde el " + a.FechaInicio.ToString("dd/MM/yyyy") + ", sin fecha de fin";
            return a.Vencida ? texto + " (vencida)" : texto;
        }

        public ResultadoOperacion<ResumenAlianzas> ObtenerResumen()
        {
            try
            {
                return ResultadoOperacion<ResumenAlianzas>.Ok(_mpp.Contar());
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<ResumenAlianzas>.Error("No se pudieron cargar las alianzas estratégicas. Probá nuevamente en unos minutos.");
            }
        }

        public ResultadoOperacion<List<AlianzaEstrategica>> Listar(FiltroAlianzaEstrategica filtro)
        {
            filtro = filtro ?? new FiltroAlianzaEstrategica();
            if (filtro.Vista != FiltroAlianzaEstrategica.VistaEnGestion && filtro.Vista != FiltroAlianzaEstrategica.VistaHistoricas &&
                filtro.Vista != FiltroAlianzaEstrategica.VistaTodas)
            {
                filtro.Vista = FiltroAlianzaEstrategica.VistaEnGestion;
            }

            if (filtro.Estado != null && !Estados.ContainsKey(filtro.Estado)) filtro.Estado = null;
            if (filtro.TipoInstitucion != null && !_catalogos.EsValido(ItemCatalogoComercial.TipoInstitucion, filtro.TipoInstitucion)) filtro.TipoInstitucion = null;
            filtro.Texto = BLL_CatalogoComercial.Limpiar(filtro.Texto);
            if (filtro.Texto != null && filtro.Texto.Length > 100) filtro.Texto = filtro.Texto.Substring(0, 100);

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                return ResultadoOperacion<List<AlianzaEstrategica>>.Error("En el filtro de vigencia, la fecha \"desde\" tiene que ser anterior o igual a la fecha \"hasta\".");
            }

            try
            {
                return ResultadoOperacion<List<AlianzaEstrategica>>.Ok(_mpp.Listar(filtro));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<AlianzaEstrategica>>.Error("No se pudieron cargar las alianzas estratégicas. Probá nuevamente en unos minutos.");
            }
        }

        public AlianzaEstrategica Obtener(int idAlianza)
        {
            try
            {
                return _mpp.Obtener(new AlianzaEstrategica { IdAlianzaEstrategica = idAlianza });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public List<VinculoComercial> ListarVinculos(int idAlianza)
        {
            try
            {
                return _mpp.ListarVinculos(new AlianzaEstrategica { IdAlianzaEstrategica = idAlianza });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<VinculoComercial>();
            }
        }

        public List<SeguimientoAlianza> ListarSeguimientos(int idAlianza)
        {
            try
            {
                return _mpp.ListarSeguimientos(new AlianzaEstrategica { IdAlianzaEstrategica = idAlianza });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<SeguimientoAlianza>();
            }
        }

        public List<VigenciaAlianza> ListarVigencias(int idAlianza)
        {
            try
            {
                return _mpp.ListarVigencias(new AlianzaEstrategica { IdAlianzaEstrategica = idAlianza });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<VigenciaAlianza>();
            }
        }

        // Usuarios internos activos que pueden ser responsables o asignados.
        public List<UsuarioInterno> ListarResponsables()
        {
            return new BLL_UsuarioInterno().Listar()
                .Where(u => u.Activo && u.EstadoCuenta == StageUp.BE.Enumerados.EstadoCuentaInterno.Activa.ToString())
                .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
                .ToList();
        }

        // Pasos 9 a 29 (alta) y A6 (edición), con A17 a A24.
        public ResultadoOperacion<int> Guardar(AlianzaEstrategica alianza, string fechaInicioTexto, string fechaFinTexto, int idUsuarioInterno)
        {
            if (alianza == null)
            {
                return ResultadoOperacion<int>.Error("Completá los datos de la alianza.");
            }

            bool esAlta = alianza.IdAlianzaEstrategica <= 0;
            alianza.NombreEntidad = BLL_CatalogoComercial.Limpiar(alianza.NombreEntidad);
            alianza.DatosContacto = BLL_CatalogoComercial.LimpiarTextoLargo(alianza.DatosContacto);
            alianza.ObjetivoComercial = BLL_CatalogoComercial.LimpiarTextoLargo(alianza.ObjetivoComercial);
            alianza.Alcance = BLL_CatalogoComercial.LimpiarTextoLargo(alianza.Alcance);
            alianza.DescripcionAcuerdo = BLL_CatalogoComercial.LimpiarTextoLargo(alianza.DescripcionAcuerdo);
            alianza.BeneficiosAsociados = BLL_CatalogoComercial.LimpiarTextoLargo(alianza.BeneficiosAsociados);
            alianza.Observaciones = BLL_CatalogoComercial.LimpiarTextoLargo(alianza.Observaciones);
            alianza.ObservacionCambioEstado = BLL_CatalogoComercial.LimpiarTextoLargo(alianza.ObservacionCambioEstado);

            // A17.
            List<string> faltantes = new List<string>();
            if (alianza.NombreEntidad == null) faltantes.Add("entidad asociada");
            if (string.IsNullOrEmpty(alianza.TipoInstitucion)) faltantes.Add("tipo de institución");
            if (alianza.DatosContacto == null) faltantes.Add("datos de contacto");
            if (alianza.IdUsuarioInternoResponsable <= 0) faltantes.Add("responsable interno");
            if (alianza.ObjetivoComercial == null) faltantes.Add("objetivo comercial");
            if (alianza.Alcance == null) faltantes.Add("alcance");
            if (alianza.DescripcionAcuerdo == null) faltantes.Add("descripción del acuerdo");
            if (alianza.BeneficiosAsociados == null) faltantes.Add("beneficios o aportes");
            if (string.IsNullOrWhiteSpace(fechaInicioTexto)) faltantes.Add("fecha de inicio");
            if (string.IsNullOrEmpty(alianza.EstadoAlianza)) faltantes.Add("estado");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion<int>.Error("Para guardar la alianza completá: " + string.Join(", ", faltantes) + ".", "A17");
            }

            // A18.
            if (alianza.NombreEntidad.Length < 3 || alianza.NombreEntidad.Length > 200)
            {
                return ResultadoOperacion<int>.Error("Completá el nombre de la entidad asociada (entre 3 y 200 caracteres).", "A18");
            }

            if (alianza.DatosContacto.Length > 500 || alianza.ObjetivoComercial.Length > 1000 || alianza.Alcance.Length > 1000 ||
                alianza.DescripcionAcuerdo.Length > 2000 || alianza.BeneficiosAsociados.Length > 2000 ||
                (alianza.Observaciones ?? string.Empty).Length > 2000 || (alianza.ObservacionCambioEstado ?? string.Empty).Length > 2000)
            {
                return ResultadoOperacion<int>.Error("Algún dato supera el largo permitido (datos de contacto 500, objetivo y alcance 1000, descripción, beneficios y observaciones 2000 caracteres).", "A17");
            }

            if (!_catalogos.EsValido(ItemCatalogoComercial.TipoInstitucion, alianza.TipoInstitucion))
            {
                return ResultadoOperacion<int>.Error("Seleccioná un tipo de institución válido.", "A17");
            }

            // A19 y A20.
            DateTime? inicio, fin;
            if (!BLL_CatalogoComercial.LeerFecha(fechaInicioTexto, out inicio) || !BLL_CatalogoComercial.LeerFecha(fechaFinTexto, out fin))
            {
                return ResultadoOperacion<int>.Error("Las fechas de vigencia no tienen un formato válido. Corregilas.", "A19");
            }

            if (fin.HasValue && inicio.Value > fin.Value)
            {
                return ResultadoOperacion<int>.Error("La fecha de inicio tiene que ser anterior o igual a la fecha de finalización. Corregí el período de vigencia.", "A20");
            }

            alianza.FechaInicio = inicio.Value;
            alianza.FechaFin = fin;

            // A21.
            if (!Estados.ContainsKey(alianza.EstadoAlianza) ||
                (esAlta && alianza.EstadoAlianza != AlianzaEstrategica.EstadoEnEvaluacion && alianza.EstadoAlianza != AlianzaEstrategica.EstadoActiva &&
                 alianza.EstadoAlianza != AlianzaEstrategica.EstadoPausada))
            {
                return ResultadoOperacion<int>.Error(esAlta
                    ? "Una alianza nueva empieza en evaluación, activa o pausada."
                    : "Seleccioná un estado válido para la alianza.", "A21");
            }

            if (!ListarResponsables().Any(u => u.IdUsuarioInterno == alianza.IdUsuarioInternoResponsable))
            {
                return ResultadoOperacion<int>.Error("El responsable interno tiene que ser un usuario interno activo.", "A17");
            }

            AlianzaEstrategica anterior = null;
            if (!esAlta)
            {
                anterior = Obtener(alianza.IdAlianzaEstrategica);
                if (anterior == null)
                {
                    return ResultadoOperacion<int>.Error("La alianza ya no existe.");
                }

                // A13.
                if (!PuedeEditarse(anterior))
                {
                    return ResultadoOperacion<int>.Error("Una alianza finalizada o dada de baja solo se puede consultar.", "A13");
                }
            }

            // A23.
            string errorVinculos = ValidarVinculos(alianza, esAlta ? new List<VinculoComercial>() : ListarVinculos(alianza.IdAlianzaEstrategica));
            if (errorVinculos != null)
            {
                return ResultadoOperacion<int>.Error(errorVinculos, "A23");
            }

            // A22.
            try
            {
                AlianzaEstrategica duplicada = _mpp.BuscarDuplicado(alianza);
                if (duplicada != null)
                {
                    return ResultadoOperacion<int>.Error(
                        "La alianza podría duplicar una relación ya existente: \"" + duplicada.NombreEntidad + "\" ya tiene una alianza " +
                        TextoEstado(duplicada.EstadoAlianza).ToLowerInvariant() + " con un período o un objetivo equivalente. Revisala, corregí los datos o cancelá la carga.",
                        "A22:" + duplicada.IdAlianzaEstrategica);
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<int>.Error("No fue posible guardar la alianza en este momento. Intentá nuevamente.", "A24");
            }

            int id;
            try
            {
                id = _mpp.Guardar(alianza, new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno });
                alianza.IdAlianzaEstrategica = id;
                _mpp.GuardarVinculos(alianza);
            }
            catch (ErrorAccesoDatosException ex)
            {
                switch (ex.NumeroErrorSql)
                {
                    case 51320: return ResultadoOperacion<int>.Error("La alianza ya no existe.");
                    case 51321: return ResultadoOperacion<int>.Error("El responsable interno tiene que ser un usuario interno activo.", "A17");
                    case 51322: return ResultadoOperacion<int>.Error("Un vínculo comercial ya no está disponible. Revisá los vínculos y guardá de nuevo.", "A23");
                    default: return ResultadoOperacion<int>.Error("No fue posible guardar la alianza en este momento. Intentá nuevamente.", "A24");
                }
            }

            string vinculos = alianza.IdsContactos.Count + " contactos, " + alianza.IdsCampanias.Count + " campañas, " +
                alianza.IdsContenidos.Count + " contenidos y " + alianza.IdsOrigenes.Count + " orígenes";
            BLL_CatalogoComercial.Registrar(idUsuarioInterno, esAlta ? "ALTA" : "MODIFICACION", EntidadBitacora, id,
                (esAlta ? "Alta" : "Modificación") + " de la alianza estratégica con \"" + alianza.NombreEntidad + "\" (" +
                TextoEstado(alianza.EstadoAlianza) + ", " + TextoVigencia(alianza) + "). Vínculos: " + vinculos + "." +
                (anterior != null && anterior.EstadoAlianza != alianza.EstadoAlianza
                    ? " Estado anterior: " + TextoEstado(anterior.EstadoAlianza) + "."
                    : string.Empty));

            return ResultadoOperacion<int>.Ok(id, esAlta
                ? "La alianza estratégica fue guardada correctamente."
                : "La alianza estratégica fue actualizada correctamente.");
        }

        // A7 (y A8 si se indica el estado resultante).
        public ResultadoOperacion RegistrarSeguimiento(SeguimientoAlianza seguimiento, string fechaTexto, int idUsuarioInterno)
        {
            if (seguimiento == null)
            {
                return ResultadoOperacion.Error("Completá los datos del seguimiento.");
            }

            AlianzaEstrategica alianza = Obtener(seguimiento.IdAlianzaEstrategica);
            if (alianza == null)
            {
                return ResultadoOperacion.Error("La alianza ya no existe.");
            }

            if (!alianza.EnGestion)
            {
                return ResultadoOperacion.Error("Una alianza finalizada o dada de baja solo se puede consultar.", "A13");
            }

            seguimiento.Observacion = BLL_CatalogoComercial.LimpiarTextoLargo(seguimiento.Observacion);
            seguimiento.Avance = BLL_CatalogoComercial.LimpiarTextoLargo(seguimiento.Avance);
            seguimiento.AccionPendiente = BLL_CatalogoComercial.LimpiarTextoLargo(seguimiento.AccionPendiente);
            if (string.IsNullOrEmpty(seguimiento.EstadoResultante) || seguimiento.EstadoResultante == alianza.EstadoAlianza)
            {
                seguimiento.EstadoResultante = null;
            }

            List<string> faltantes = new List<string>();
            if (string.IsNullOrWhiteSpace(fechaTexto)) faltantes.Add("fecha de interacción");
            if (string.IsNullOrEmpty(seguimiento.TipoInteraccion)) faltantes.Add("tipo de interacción");
            if (seguimiento.Observacion == null) faltantes.Add("observación");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Error("Para guardar el seguimiento completá: " + string.Join(", ", faltantes) + ".", "A17");
            }

            DateTime? fecha;
            if (!BLL_CatalogoComercial.LeerFecha(fechaTexto, out fecha))
            {
                return ResultadoOperacion.Error("La fecha de interacción no tiene un formato válido.", "A17");
            }

            if (fecha.Value.Date > DateTime.Today)
            {
                return ResultadoOperacion.Error("La fecha de interacción no puede ser posterior a hoy.", "A17");
            }

            if (!_catalogos.EsValido(ItemCatalogoComercial.TipoInteraccion, seguimiento.TipoInteraccion))
            {
                return ResultadoOperacion.Error("Seleccioná el tipo de interacción realizada.", "A17");
            }

            if (seguimiento.Observacion.Length < 5 || seguimiento.Observacion.Length > 2000 ||
                (seguimiento.Avance ?? string.Empty).Length > 1000 || (seguimiento.AccionPendiente ?? string.Empty).Length > 1000)
            {
                return ResultadoOperacion.Error("Describí la interacción (entre 5 y 2000 caracteres; avance y acción pendiente hasta 1000).", "A17");
            }

            if (seguimiento.IdUsuarioInternoAsignado.HasValue &&
                !ListarResponsables().Any(u => u.IdUsuarioInterno == seguimiento.IdUsuarioInternoAsignado.Value))
            {
                return ResultadoOperacion.Error("El responsable asignado tiene que ser un usuario interno activo.", "A17");
            }

            if (seguimiento.EstadoResultante != null && !Estados.ContainsKey(seguimiento.EstadoResultante))
            {
                return ResultadoOperacion.Error("Seleccioná un estado válido para la alianza.", "A21");
            }

            seguimiento.FechaInteraccion = fecha.Value;
            seguimiento.IdUsuarioInternoRegistro = idUsuarioInterno;

            try
            {
                _mpp.RegistrarSeguimiento(seguimiento);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No fue posible guardar el seguimiento en este momento. Intentá nuevamente.", "A24");
            }

            BLL_CatalogoComercial.Registrar(idUsuarioInterno, "SEGUIMIENTO", EntidadBitacora, alianza.IdAlianzaEstrategica,
                "Seguimiento de la alianza con \"" + alianza.NombreEntidad + "\" (" +
                _catalogos.Nombre(ItemCatalogoComercial.TipoInteraccion, seguimiento.TipoInteraccion) + " del " +
                seguimiento.FechaInteraccion.ToString("dd/MM/yyyy") + ")." +
                (seguimiento.EstadoResultante != null
                    ? " Estado: " + TextoEstado(alianza.EstadoAlianza) + " → " + TextoEstado(seguimiento.EstadoResultante) + "."
                    : string.Empty));

            return ResultadoOperacion.Ok(seguimiento.EstadoResultante != null
                ? "El seguimiento fue registrado correctamente y la alianza pasó a \"" + TextoEstado(seguimiento.EstadoResultante) + "\"."
                : "El seguimiento fue registrado correctamente.");
        }

        // A9 (y reactivación con permiso).
        public ResultadoOperacion Renovar(RenovacionAlianza renovacion, string fechaInicioTexto, string fechaFinTexto, int idUsuarioInterno, bool puedeReactivar)
        {
            if (renovacion == null)
            {
                return ResultadoOperacion.Error("Completá los datos de la renovación.");
            }

            AlianzaEstrategica alianza = Obtener(renovacion.IdAlianzaEstrategica);
            if (alianza == null)
            {
                return ResultadoOperacion.Error("La alianza ya no existe.");
            }

            if (!PuedeRenovarse(alianza, puedeReactivar))
            {
                return ResultadoOperacion.Error(alianza.EnGestion
                    ? "Solo se puede renovar una alianza activa o pausada."
                    : "Una alianza finalizada o dada de baja solo se puede consultar. Para volver a ponerla en gestión hace falta el permiso de reactivación.", "A13");
            }

            if (string.IsNullOrWhiteSpace(fechaInicioTexto))
            {
                return ResultadoOperacion.Error("Indicá la fecha de inicio del nuevo período.", "A17");
            }

            DateTime? inicio, fin;
            if (!BLL_CatalogoComercial.LeerFecha(fechaInicioTexto, out inicio) || !BLL_CatalogoComercial.LeerFecha(fechaFinTexto, out fin))
            {
                return ResultadoOperacion.Error("Las fechas no tienen un formato válido. Corregilas.", "A19");
            }

            if (fin.HasValue && inicio.Value > fin.Value)
            {
                return ResultadoOperacion.Error("La fecha de inicio tiene que ser anterior o igual a la fecha de finalización.", "A20");
            }

            if (fin.HasValue && fin.Value.Date < DateTime.Today)
            {
                return ResultadoOperacion.Error("La nueva fecha de finalización no puede estar vencida.", "A20");
            }

            if (fin == alianza.FechaFin && inicio.Value == alianza.FechaInicio && alianza.EnGestion)
            {
                return ResultadoOperacion.Error("El nuevo período es igual al actual. Cambiá la fecha de finalización o el período de vigencia.", "A20");
            }

            renovacion.FechaInicio = inicio.Value;
            renovacion.FechaFin = fin;
            renovacion.Alcance = BLL_CatalogoComercial.LimpiarTextoLargo(renovacion.Alcance);
            renovacion.BeneficiosAsociados = BLL_CatalogoComercial.LimpiarTextoLargo(renovacion.BeneficiosAsociados);
            renovacion.Observacion = BLL_CatalogoComercial.LimpiarTextoLargo(renovacion.Observacion);
            renovacion.Reactivar = !alianza.EnGestion;

            if ((renovacion.Alcance ?? string.Empty).Length > 1000 || (renovacion.BeneficiosAsociados ?? string.Empty).Length > 2000 ||
                (renovacion.Observacion ?? string.Empty).Length > 1000)
            {
                return ResultadoOperacion.Error("Algún dato supera el largo permitido (alcance y observación 1000, beneficios 2000 caracteres).", "A17");
            }

            if (renovacion.IdUsuarioInternoResponsable.HasValue &&
                !ListarResponsables().Any(u => u.IdUsuarioInterno == renovacion.IdUsuarioInternoResponsable.Value))
            {
                return ResultadoOperacion.Error("El responsable interno tiene que ser un usuario interno activo.", "A17");
            }

            try
            {
                _mpp.Renovar(renovacion, new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No fue posible guardar la renovación en este momento. Intentá nuevamente.", "A24");
            }

            string periodo = renovacion.FechaFin.HasValue
                ? renovacion.FechaInicio.ToString("dd/MM/yyyy") + " al " + renovacion.FechaFin.Value.ToString("dd/MM/yyyy")
                : "desde el " + renovacion.FechaInicio.ToString("dd/MM/yyyy") + ", sin fecha de fin";
            BLL_CatalogoComercial.Registrar(idUsuarioInterno, renovacion.Reactivar ? "ACTIVACION" : "RENOVACION", EntidadBitacora, alianza.IdAlianzaEstrategica,
                (renovacion.Reactivar ? "Reactivación" : "Renovación") + " de la alianza con \"" + alianza.NombreEntidad + "\". Nueva vigencia: " + periodo +
                " (antes " + TextoVigencia(alianza) + ").");

            return ResultadoOperacion.Ok(renovacion.Reactivar
                ? "La alianza fue reactivada con la nueva vigencia (" + periodo + ")."
                : "La alianza fue renovada correctamente (" + periodo + "). La vigencia anterior quedó en el historial.");
        }

        // A10.
        public ResultadoOperacion Finalizar(int idAlianza, string motivo, int idUsuarioInterno)
        {
            AlianzaEstrategica alianza = Obtener(idAlianza);
            if (alianza == null)
            {
                return ResultadoOperacion.Error("La alianza ya no existe.");
            }

            if (!PuedeFinalizarse(alianza))
            {
                return ResultadoOperacion.Error("Solo se puede finalizar una alianza activa o pausada.");
            }

            return Cerrar(alianza, AlianzaEstrategica.EstadoFinalizada, motivo, idUsuarioInterno,
                "CIERRE", "Finalización", "La alianza fue finalizada correctamente. Se conserva para consulta, trazabilidad comercial y análisis posterior.");
        }

        // A11.
        public ResultadoOperacion DarDeBaja(int idAlianza, string motivo, int idUsuarioInterno)
        {
            AlianzaEstrategica alianza = Obtener(idAlianza);
            if (alianza == null)
            {
                return ResultadoOperacion.Error("La alianza ya no existe.");
            }

            if (!PuedeDarseDeBaja(alianza))
            {
                return ResultadoOperacion.Error("La alianza ya estaba dada de baja.");
            }

            return Cerrar(alianza, AlianzaEstrategica.EstadoDadaDeBaja, motivo, idUsuarioInterno,
                "BAJA", "Baja lógica", "La alianza fue dada de baja correctamente. Se conserva para consulta histórica y análisis posterior.");
        }

        private ResultadoOperacion Cerrar(AlianzaEstrategica alianza, string estado, string motivo, int idUsuarioInterno,
            string operacion, string accion, string mensajeOk)
        {
            motivo = BLL_CatalogoComercial.LimpiarTextoLargo(motivo);
            if ((motivo ?? string.Empty).Length > 2000)
            {
                return ResultadoOperacion.Error("El motivo no puede superar los 2000 caracteres.");
            }

            try
            {
                _mpp.Cerrar(alianza, new SeguimientoAlianza
                {
                    EstadoResultante = estado,
                    Observacion = motivo,
                    IdUsuarioInternoRegistro = idUsuarioInterno
                });
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.NumeroErrorSql == 51324
                    ? "El estado actual de la alianza no lo permite."
                    : "No fue posible guardar el cambio en este momento. Intentá nuevamente.", "A24");
            }

            BLL_CatalogoComercial.Registrar(idUsuarioInterno, operacion, EntidadBitacora, alianza.IdAlianzaEstrategica,
                accion + " de la alianza con \"" + alianza.NombreEntidad + "\" (estado anterior " + TextoEstado(alianza.EstadoAlianza) + ")." +
                (motivo != null ? " Motivo: " + motivo : string.Empty));

            return ResultadoOperacion.Ok(mensajeOk);
        }

        private string ValidarVinculos(AlianzaEstrategica alianza, List<VinculoComercial> actuales)
        {
            var revisar = new[]
            {
                new { Tipo = OpcionVinculoComercial.TipoContacto, Ids = alianza.IdsContactos, Texto = "Uno de los contactos comerciales elegidos" },
                new { Tipo = OpcionVinculoComercial.TipoCampania, Ids = alianza.IdsCampanias, Texto = "Una de las campañas comerciales elegidas" },
                new { Tipo = OpcionVinculoComercial.TipoContenido, Ids = alianza.IdsContenidos, Texto = "Una de las piezas de contenido promocional elegidas" },
                new { Tipo = OpcionVinculoComercial.TipoOrigen, Ids = alianza.IdsOrigenes, Texto = "Uno de los orígenes de usuarios elegidos" }
            };

            foreach (var grupo in revisar)
            {
                if (grupo.Ids == null || grupo.Ids.Count == 0)
                {
                    continue;
                }

                HashSet<int> disponibles = new HashSet<int>(_catalogos.ListarParaVincular(grupo.Tipo).Select(o => o.Id));
                disponibles.UnionWith(actuales.Where(v => v.Tipo == grupo.Tipo).Select(v => v.Id));
                if (grupo.Ids.Any(id => !disponibles.Contains(id)))
                {
                    return grupo.Texto + " ya no existe o no está disponible para asociar. Corregí o quitá ese vínculo.";
                }
            }

            return null;
        }
    }
}
