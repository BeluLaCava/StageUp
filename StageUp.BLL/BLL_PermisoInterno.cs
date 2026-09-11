using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Menu;
using StageUp.BE.Permisos;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_PermisoInterno
    {
        private static readonly object CacheLock = new object();
        private static readonly Dictionary<int, List<PermisoHoja>> CachePermisosPorRol =
            new Dictionary<int, List<PermisoHoja>>();

        private readonly MPP_ComponentePermiso _mppComponente = new MPP_ComponentePermiso();
        private readonly MPP_RolInternoComponentePermiso _mppRolComponente = new MPP_RolInternoComponentePermiso();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<string> ListarCodigosPermisosDeRol(int idRolInterno)
        {
            var codigos = new List<string>();
            foreach (PermisoHoja hoja in ObtenerHojasAsignadas(idRolInterno))
            {
                codigos.Add(hoja.CodigoPermiso);
            }
            return codigos;
        }

        public GrupoMenu ConstruirMenuParaRol(int idRolInterno)
        {
            var raiz = new GrupoMenu("Menú");
            GrupoMenu grupoActual = null;
            string nombreGrupoActual = null;

            foreach (PermisoHoja hoja in ObtenerHojasAsignadas(idRolInterno))
            {
                string nombreGrupo = hoja.NombreGrupo ?? "General";
                if (grupoActual == null || nombreGrupo != nombreGrupoActual)
                {
                    grupoActual = new GrupoMenu(nombreGrupo);
                    raiz.Agregar(grupoActual);
                    nombreGrupoActual = nombreGrupo;
                }

                grupoActual.Agregar(new ItemMenu(hoja.Nombre, hoja.UrlAsociada, hoja.Descripcion));
            }

            return raiz;
        }

        public GrupoPermisos ListarComponentesRaizConAsignacion(int idRolInterno)
        {
            GrupoPermisos raiz;
            List<int> idsAsignados;
            RolInterno rol = new RolInterno { IdRolInterno = idRolInterno };
            try
            {
                raiz = _mppComponente.ListarArbol();
                idsAsignados = _mppRolComponente.ListarIdsPorRol(rol);
            }
            catch (ErrorAccesoDatosException)
            {
                return new GrupoPermisos(0, "Permisos");
            }

            foreach (PermisoHoja hoja in raiz.Listar())
            {
                hoja.Asignado = idsAsignados.Contains(hoja.IdComponentePermiso);
            }

            return raiz;
        }

        public ResultadoOperacion AsignarComponentesARol(int idRolInterno, List<int> idsComponentesSeleccionados, int idUsuarioInternoResponsable)
        {
            InvalidarCacheRol(idRolInterno);
            RolInterno rol = new RolInterno { IdRolInterno = idRolInterno };

            try
            {
                _mppRolComponente.EliminarPorRol(rol);

                if (idsComponentesSeleccionados != null)
                {
                    foreach (int idComponentePermiso in idsComponentesSeleccionados)
                    {
                        PermisoHoja permiso = new PermisoHoja(idComponentePermiso, null, null, null, null);
                        _mppRolComponente.Insertar(rol, permiso);
                    }
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ASIGNACION_PERMISOS", "RolInterno", idRolInterno,
                    "Actualización de permisos asignados al rol.");

                return ResultadoOperacion.Ok("Los permisos del rol se actualizaron correctamente.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        private List<PermisoHoja> ObtenerHojasAsignadas(int idRolInterno)
        {
            lock (CacheLock)
            {
                List<PermisoHoja> hojasCacheadas;
                if (CachePermisosPorRol.TryGetValue(idRolInterno, out hojasCacheadas))
                {
                    return new List<PermisoHoja>(hojasCacheadas);
                }
            }

            var hojasAsignadas = new List<PermisoHoja>();

            GrupoPermisos raiz;
            List<int> idsAsignados;
            RolInterno rol = new RolInterno { IdRolInterno = idRolInterno };
            try
            {
                raiz = _mppComponente.ListarArbol();
                idsAsignados = _mppRolComponente.ListarIdsPorRol(rol);
            }
            catch (ErrorAccesoDatosException)
            {
                return hojasAsignadas;
            }

            foreach (PermisoHoja hoja in raiz.Listar())
            {
                if (idsAsignados.Contains(hoja.IdComponentePermiso))
                {
                    hojasAsignadas.Add(hoja);
                }
            }

            lock (CacheLock)
            {
                CachePermisosPorRol[idRolInterno] = new List<PermisoHoja>(hojasAsignadas);
            }

            return new List<PermisoHoja>(hojasAsignadas);
        }

        private static void InvalidarCacheRol(int idRolInterno)
        {
            lock (CacheLock)
            {
                CachePermisosPorRol.Remove(idRolInterno);
            }
        }
    }
}
