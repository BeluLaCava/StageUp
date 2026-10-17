using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-004-001 Gestionar campañas comerciales.
    //   * Alta, consulta, modificación y baja lógica (la baja conserva la
    //     campaña para consulta histórica y la saca del listado activo).
    //   * Una campaña se vincula con contactos, alianzas, contenido
    //     promocional y orígenes de usuario (A16 si alguno no está disponible).
    //   * Todo queda en la bitácora (entidad CampaniaComercial).
    public class BLL_CampaniaComercial
    {
        public const string PermisoGestionar = "GESTIONAR_CAMPANIAS_COMERCIALES";
        public const string EntidadBitacora = "CampaniaComercial";

        public static readonly Dictionary<string, string> Estados = new Dictionary<string, string>
        {
            { CampaniaComercial.EstadoPlanificada, "Planificada" },
            { CampaniaComercial.EstadoActiva, "Activa" },
            { CampaniaComercial.EstadoFinalizada, "Finalizada" },
            { CampaniaComercial.EstadoCancelada, "Cancelada" }
        };

        private readonly MPP_CampaniaComercial _mpp = new MPP_CampaniaComercial();
        private readonly BLL_CatalogoComercial _catalogos = new BLL_CatalogoComercial();

        public static string TextoEstado(CampaniaComercial campania)
        {
            if (campania == null)
            {
                return string.Empty;
            }

            if (!campania.Activa)
            {
                return "Dada de baja";
            }

            string texto;
            return Estados.TryGetValue(campania.EstadoCampania ?? string.Empty, out texto) ? texto : campania.EstadoCampania;
        }

        public static string ClaseEstado(CampaniaComercial campania)
        {
            if (campania == null || !campania.Activa)
            {
                return "comercial-badge-baja";
            }

            switch (campania.EstadoCampania)
            {
                case CampaniaComercial.EstadoActiva: return "comercial-badge-activa";
                case CampaniaComercial.EstadoPlanificada: return "comercial-badge-planificada";
                case CampaniaComercial.EstadoFinalizada: return "comercial-badge-finalizada";
                default: return "comercial-badge-cancelada";
            }
        }

        // A7: la baja se ofrece desde una campaña activa o planificada.
        public static bool PuedeDarseDeBaja(CampaniaComercial campania)
        {
            return campania != null && campania.Activa &&
                (campania.EstadoCampania == CampaniaComercial.EstadoActiva || campania.EstadoCampania == CampaniaComercial.EstadoPlanificada);
        }

        public static bool PuedeEditarse(CampaniaComercial campania)
        {
            return campania != null && campania.Activa;
        }

        public ResultadoOperacion<ResumenCampanias> ObtenerResumen()
        {
            try
            {
                return ResultadoOperacion<ResumenCampanias>.Ok(_mpp.Contar());
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<ResumenCampanias>.Error("No se pudieron cargar las campañas comerciales. Probá nuevamente en unos minutos.");
            }
        }

        public ResultadoOperacion<List<CampaniaComercial>> Listar(FiltroCampaniaComercial filtro)
        {
            filtro = filtro ?? new FiltroCampaniaComercial();
            if (filtro.Vista != FiltroCampaniaComercial.VistaActivas && filtro.Vista != FiltroCampaniaComercial.VistaBajas &&
                filtro.Vista != FiltroCampaniaComercial.VistaTodas)
            {
                filtro.Vista = FiltroCampaniaComercial.VistaActivas;
            }

            if (filtro.Estado != null && !Estados.ContainsKey(filtro.Estado)) filtro.Estado = null;
            if (filtro.CanalDifusion != null && !_catalogos.EsValido(ItemCatalogoComercial.CanalDifusion, filtro.CanalDifusion)) filtro.CanalDifusion = null;
            if (filtro.Segmento != null && !_catalogos.EsValido(ItemCatalogoComercial.PublicoObjetivo, filtro.Segmento)) filtro.Segmento = null;
            filtro.Texto = BLL_CatalogoComercial.Limpiar(filtro.Texto);
            if (filtro.Texto != null && filtro.Texto.Length > 100) filtro.Texto = filtro.Texto.Substring(0, 100);

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                return ResultadoOperacion<List<CampaniaComercial>>.Error("En el filtro de vigencia, la fecha \"desde\" tiene que ser anterior o igual a la fecha \"hasta\".");
            }

            try
            {
                return ResultadoOperacion<List<CampaniaComercial>>.Ok(_mpp.Listar(filtro));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<CampaniaComercial>>.Error("No se pudieron cargar las campañas comerciales. Probá nuevamente en unos minutos.");
            }
        }

        public CampaniaComercial Obtener(int idCampaniaComercial)
        {
            try
            {
                return _mpp.Obtener(new CampaniaComercial { IdCampaniaComercial = idCampaniaComercial });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public List<VinculoComercial> ListarVinculos(int idCampaniaComercial)
        {
            try
            {
                return _mpp.ListarVinculos(new CampaniaComercial { IdCampaniaComercial = idCampaniaComercial });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<VinculoComercial>();
            }
        }

        // Pasos 9 a 25 (alta) y A6 (modificación), con A10 a A17.
        public ResultadoOperacion<int> Guardar(CampaniaComercial campania, string fechaInicioTexto, string fechaFinTexto, int idUsuarioInternoResponsable)
        {
            if (campania == null)
            {
                return ResultadoOperacion<int>.Error("Completá los datos de la campaña.");
            }

            bool esAlta = campania.IdCampaniaComercial <= 0;
            campania.Nombre = BLL_CatalogoComercial.Limpiar(campania.Nombre);
            campania.Descripcion = BLL_CatalogoComercial.LimpiarTextoLargo(campania.Descripcion);
            campania.PublicoObjetivo = (campania.PublicoObjetivo ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            // A10: campos obligatorios.
            List<string> faltantes = new List<string>();
            if (campania.Nombre == null) faltantes.Add("nombre identificatorio");
            if (campania.Descripcion == null) faltantes.Add("descripción");
            if (campania.PublicoObjetivo.Count == 0) faltantes.Add("público objetivo");
            if (string.IsNullOrEmpty(campania.CanalDifusion)) faltantes.Add("canal de difusión");
            if (string.IsNullOrWhiteSpace(fechaInicioTexto)) faltantes.Add("fecha de inicio");
            if (string.IsNullOrWhiteSpace(fechaFinTexto)) faltantes.Add("fecha de finalización");
            if (string.IsNullOrEmpty(campania.EstadoCampania)) faltantes.Add("estado");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion<int>.Error("Para guardar la campaña completá: " + string.Join(", ", faltantes) + ".", "A10");
            }

            if (campania.Nombre.Length > 150)
            {
                return ResultadoOperacion<int>.Error("El nombre no puede superar los 150 caracteres.", "A10");
            }

            if (campania.Descripcion.Length > 2000)
            {
                return ResultadoOperacion<int>.Error("La descripción no puede superar los 2000 caracteres.", "A10");
            }

            // A11 y A12: vigencia.
            DateTime? inicio, fin;
            if (!BLL_CatalogoComercial.LeerFecha(fechaInicioTexto, out inicio) || !BLL_CatalogoComercial.LeerFecha(fechaFinTexto, out fin))
            {
                return ResultadoOperacion<int>.Error("Las fechas de vigencia no tienen un formato válido. Corregilas.", "A11");
            }

            if (inicio.Value > fin.Value)
            {
                return ResultadoOperacion<int>.Error("La fecha de inicio tiene que ser anterior o igual a la fecha de finalización. Corregí el período de vigencia.", "A12");
            }

            campania.FechaInicio = inicio.Value;
            campania.FechaFin = fin.Value;

            // A13: público objetivo.
            if (campania.PublicoObjetivo.Any(s => !_catalogos.EsValido(ItemCatalogoComercial.PublicoObjetivo, s)))
            {
                return ResultadoOperacion<int>.Error("Seleccioná al menos un público objetivo de la lista.", "A13");
            }

            // A14: canal.
            if (!_catalogos.EsValido(ItemCatalogoComercial.CanalDifusion, campania.CanalDifusion))
            {
                return ResultadoOperacion<int>.Error("Seleccioná un canal de difusión válido.", "A14");
            }

            // A15: estado.
            if (!Estados.ContainsKey(campania.EstadoCampania))
            {
                return ResultadoOperacion<int>.Error("Seleccioná un estado válido para la campaña (planificada, activa, finalizada o cancelada).", "A15");
            }

            CampaniaComercial anterior = null;
            if (!esAlta)
            {
                anterior = Obtener(campania.IdCampaniaComercial);
                if (anterior == null || !anterior.Activa)
                {
                    return ResultadoOperacion<int>.Error("La campaña ya no existe o fue dada de baja, por eso no se puede editar.");
                }
            }

            // A16: lo elegido tiene que existir y estar disponible (lo que ya
            // estaba vinculado se puede conservar).
            string errorVinculos = ValidarVinculos(campania, esAlta ? new List<VinculoComercial>() : ListarVinculos(campania.IdCampaniaComercial));
            if (errorVinculos != null)
            {
                return ResultadoOperacion<int>.Error(errorVinculos, "A16");
            }

            int id;
            try
            {
                id = _mpp.Guardar(new CampaniaComercial
                {
                    IdCampaniaComercial = campania.IdCampaniaComercial,
                    Nombre = campania.Nombre,
                    Descripcion = campania.Descripcion,
                    CanalDifusion = campania.CanalDifusion,
                    FechaInicio = campania.FechaInicio,
                    FechaFin = campania.FechaFin,
                    EstadoCampania = campania.EstadoCampania,
                    PublicoObjetivo = campania.PublicoObjetivo,
                    IdUsuarioInternoResponsable = esAlta ? idUsuarioInternoResponsable : (int?)null
                });
            }
            catch (ErrorAccesoDatosException ex)
            {
                if (ex.NumeroErrorSql == 51303)
                {
                    return ResultadoOperacion<int>.Error("La campaña ya no existe o fue dada de baja, por eso no se puede editar.");
                }

                return ResultadoOperacion<int>.Error("No fue posible guardar la campaña en este momento. Intentá guardarla nuevamente.", "A17");
            }

            // Pasos 16 y 22: vínculos comerciales.
            campania.IdCampaniaComercial = id;
            string avisoVinculos = null;
            try
            {
                _mpp.GuardarVinculos(campania);
            }
            catch (ErrorAccesoDatosException ex)
            {
                avisoVinculos = ex.NumeroErrorSql == 51302
                    ? " Pero un vínculo comercial elegido ya no está disponible (A16): revisá los vínculos y guardá de nuevo."
                    : " Pero no se pudieron guardar los vínculos comerciales: editala y guardá de nuevo.";
            }

            string resumenVinculos = campania.IdsContactos.Count + " contactos, " + campania.IdsAlianzas.Count + " alianzas, " +
                campania.IdsContenidos.Count + " contenidos y " + campania.IdsOrigenes.Count + " orígenes";

            BLL_CatalogoComercial.Registrar(idUsuarioInternoResponsable, esAlta ? "ALTA" : "MODIFICACION", EntidadBitacora, id,
                (esAlta ? "Alta" : "Modificación") + " de la campaña comercial \"" + campania.Nombre + "\" (" + TextoEstado(new CampaniaComercial
                {
                    Activa = true,
                    EstadoCampania = campania.EstadoCampania
                }) + ", " + campania.FechaInicio.ToString("dd/MM/yyyy") + " al " + campania.FechaFin.ToString("dd/MM/yyyy") +
                "). Vínculos: " + resumenVinculos + "." +
                (anterior != null && anterior.EstadoCampania != campania.EstadoCampania
                    ? " Estado anterior: " + TextoEstado(anterior) + "."
                    : string.Empty));

            if (avisoVinculos != null)
            {
                return ResultadoOperacion<int>.OkAlternativo(id, (esAlta ? "La campaña se guardó." : "Los datos de la campaña se actualizaron.") + avisoVinculos, "A16");
            }

            return ResultadoOperacion<int>.Ok(id, esAlta
                ? "La campaña comercial fue guardada correctamente."
                : "La campaña comercial fue actualizada correctamente.");
        }

        private string ValidarVinculos(CampaniaComercial campania, List<VinculoComercial> actuales)
        {
            var revisar = new[]
            {
                new { Tipo = OpcionVinculoComercial.TipoContacto, Ids = campania.IdsContactos, Texto = "contacto comercial" },
                new { Tipo = OpcionVinculoComercial.TipoAlianza, Ids = campania.IdsAlianzas, Texto = "alianza estratégica" },
                new { Tipo = OpcionVinculoComercial.TipoContenido, Ids = campania.IdsContenidos, Texto = "contenido promocional" },
                new { Tipo = OpcionVinculoComercial.TipoOrigen, Ids = campania.IdsOrigenes, Texto = "origen de usuario" }
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
                    return "Un " + grupo.Texto + " elegido no existe o ya no está disponible para asociar. Corregí o quitá ese vínculo de la campaña.";
                }
            }

            return null;
        }

        // A7.
        public ResultadoOperacion DarDeBaja(int idCampaniaComercial, int idUsuarioInternoResponsable)
        {
            CampaniaComercial campania = Obtener(idCampaniaComercial);
            if (campania == null)
            {
                return ResultadoOperacion.Error("La campaña ya no existe.");
            }

            if (!PuedeDarseDeBaja(campania))
            {
                return ResultadoOperacion.Error(campania.Activa
                    ? "Solo se puede dar de baja una campaña activa o planificada."
                    : "La campaña ya estaba dada de baja.");
            }

            try
            {
                if (!_mpp.DarDeBaja(campania))
                {
                    return ResultadoOperacion.Error("La campaña ya estaba dada de baja.");
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No fue posible dar de baja la campaña en este momento. Intentá nuevamente.");
            }

            BLL_CatalogoComercial.Registrar(idUsuarioInternoResponsable, "BAJA", EntidadBitacora, idCampaniaComercial,
                "Baja lógica de la campaña comercial \"" + campania.Nombre + "\" (estado " + TextoEstado(campania) + "). Se conserva para consulta histórica.");

            return ResultadoOperacion.Ok("La campaña fue dada de baja correctamente. Se conserva para consulta histórica y análisis posterior.");
        }
    }
}
