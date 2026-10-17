using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-004-004 Consultar métricas comerciales y origen de usuarios. Es solo
    // de consulta: no modifica nada.
    //   * Indicadores: nuevos usuarios, espacios incorporados, campañas
    //     activas, contactos convertidos en usuarios y distribución por origen.
    //   * Filtros: período, campaña, canal de origen y tipo de usuario.
    //   * Los usuarios sin origen se agrupan en "Origen no informado" (A8).
    public class BLL_MetricasComerciales
    {
        public const string PermisoConsultar = "CONSULTAR_METRICAS_COMERCIALES";
        public const string TextoNoInformado = "Origen no informado";

        public static readonly Dictionary<string, string> TiposUsuario = new Dictionary<string, string>
        {
            { FiltroMetricasComerciales.TipoSolicitante, "Usuarios solicitantes" },
            { FiltroMetricasComerciales.TipoGestor, "Gestores de espacios" }
        };

        private readonly MPP_MetricasComerciales _mpp = new MPP_MetricasComerciales();
        private readonly BLL_CatalogoComercial _catalogos = new BLL_CatalogoComercial();

        public string NombreCanal(string canal)
        {
            return canal == FiltroMetricasComerciales.CanalNoInformado
                ? TextoNoInformado
                : _catalogos.Nombre(ItemCatalogoComercial.CanalOrigen, canal);
        }

        public ResultadoOperacion<ResumenMetricasComerciales> ObtenerResumen(FiltroMetricasComerciales filtro)
        {
            string error = Normalizar(filtro);
            if (error != null)
            {
                return ResultadoOperacion<ResumenMetricasComerciales>.Error(error);
            }

            try
            {
                return ResultadoOperacion<ResumenMetricasComerciales>.Ok(_mpp.ObtenerResumen(filtro));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<ResumenMetricasComerciales>.Error("No se pudieron calcular las métricas comerciales. Probá nuevamente en unos minutos.");
            }
        }

        // Pasos 13 a 15: usuarios por canal de origen, con el detalle de cada
        // origen y el porcentaje sobre el total del filtro.
        public ResultadoOperacion<List<MetricaCanalOrigen>> ListarPorCanal(FiltroMetricasComerciales filtro)
        {
            string error = Normalizar(filtro);
            if (error != null)
            {
                return ResultadoOperacion<List<MetricaCanalOrigen>>.Error(error);
            }

            List<MetricaOrigenUsuario> origenes;
            try
            {
                origenes = _mpp.ListarPorOrigen(filtro);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<MetricaCanalOrigen>>.Error("No se pudo calcular el origen de los usuarios. Probá nuevamente en unos minutos.");
            }

            int total = origenes.Sum(o => o.CantidadUsuarios);
            List<MetricaCanalOrigen> canales = origenes
                .GroupBy(o => o.CanalOrigen)
                .Select(g => new MetricaCanalOrigen
                {
                    CanalOrigen = g.Key,
                    NombreCanal = NombreCanal(g.Key),
                    CantidadUsuarios = g.Sum(o => o.CantidadUsuarios),
                    Porcentaje = total == 0 ? 0m : Math.Round(g.Sum(o => o.CantidadUsuarios) * 100m / total, 1),
                    Origenes = g.OrderByDescending(o => o.CantidadUsuarios).ToList()
                })
                // El origen no informado va al final, para no confundirlo con un canal.
                .OrderBy(c => c.CanalOrigen == FiltroMetricasComerciales.CanalNoInformado ? 1 : 0)
                .ThenByDescending(c => c.CantidadUsuarios)
                .ToList();

            return ResultadoOperacion<List<MetricaCanalOrigen>>.Ok(canales);
        }

        private string Normalizar(FiltroMetricasComerciales filtro)
        {
            if (filtro == null)
            {
                return "Filtro inválido.";
            }

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                return "La fecha \"desde\" del período tiene que ser anterior o igual a la fecha \"hasta\".";
            }

            if (filtro.CanalOrigen != null && filtro.CanalOrigen != FiltroMetricasComerciales.CanalNoInformado &&
                !_catalogos.EsValido(ItemCatalogoComercial.CanalOrigen, filtro.CanalOrigen))
            {
                filtro.CanalOrigen = null;
            }

            if (filtro.TipoUsuario != null && !TiposUsuario.ContainsKey(filtro.TipoUsuario))
            {
                filtro.TipoUsuario = null;
            }

            return null;
        }
    }
}
