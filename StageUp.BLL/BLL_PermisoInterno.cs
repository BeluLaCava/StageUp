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
            List<string> codigos = new List<string>();
            foreach (PermisoHoja hoja in ObtenerHojasAsignadas(idRolInterno))
            {
                codigos.Add(hoja.CodigoPermiso);
            }
            return codigos;
        }

        public GrupoMenu ConstruirMenuParaRol(int idRolInterno)
        {
            GrupoMenu raiz = new GrupoMenu("Menú");
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

            // Se marca cada nodo (hoja o grupo) según si está asignado DIRECTAMENTE al rol.
            // Si un grupo entero está asignado, sus hijos heredan el acceso en tiempo de
            // lectura (ver ObtenerHojasAsignadas), pero acá el tilde refleja el dato real
            // guardado, no una asignación heredada, para no confundir al que administra.
            foreach (PermisoComponente nodo in RecorrerTodos(raiz))
            {
                nodo.Asignado = idsAsignados.Contains(nodo.IdComponentePermiso);
            }

            return raiz;
        }

        public GrupoPermisos ListarArbolCompleto()
        {
            try
            {
                return _mppComponente.ListarArbol();
            }
            catch (ErrorAccesoDatosException)
            {
                return new GrupoPermisos(0, "Permisos");
            }
        }

        public ResultadoOperacion<int> CrearGrupo(int? idGrupoPadre, string nombre, int idUsuarioInternoResponsable)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return ResultadoOperacion<int>.Error("Ingresá un nombre para el grupo.");
            }

            nombre = nombre.Trim();
            if (nombre.Length > 200)
            {
                return ResultadoOperacion<int>.Error("El nombre del grupo no puede superar los 200 caracteres.");
            }

            GrupoPermisos raiz = ListarArbolCompleto();
            PermisoComponente padre = null;
            if (idGrupoPadre.HasValue)
            {
                padre = BuscarComponente(raiz, idGrupoPadre.Value);
                if (!(padre is GrupoPermisos))
                {
                    return ResultadoOperacion<int>.Error("El grupo padre indicado no existe.");
                }
            }

            try
            {
                GrupoPermisos nuevoGrupo = new GrupoPermisos(0, nombre);
                int idNuevoGrupo = _mppComponente.InsertarGrupo(nuevoGrupo, padre);
                InvalidarCacheCompleta();

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", "ComponentePermiso", idNuevoGrupo,
                    "Alta de grupo de permisos \"" + nombre + "\".");

                return ResultadoOperacion<int>.Ok(idNuevoGrupo, "El grupo se creó correctamente.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<int>.Error(ex.Message);
            }
        }

        public ResultadoOperacion MoverComponente(int idComponente, int? idNuevoGrupoPadre, int idUsuarioInternoResponsable)
        {
            GrupoPermisos raiz = ListarArbolCompleto();
            PermisoComponente componente = BuscarComponente(raiz, idComponente);
            if (componente == null || componente.IdComponentePermiso == 0)
            {
                return ResultadoOperacion.Error("El elemento indicado no existe.");
            }

            PermisoComponente nuevoPadre = null;
            if (idNuevoGrupoPadre.HasValue)
            {
                nuevoPadre = BuscarComponente(raiz, idNuevoGrupoPadre.Value);
                if (!(nuevoPadre is GrupoPermisos))
                {
                    return ResultadoOperacion.Error("El grupo destino indicado no existe.");
                }

                if (idNuevoGrupoPadre.Value == idComponente ||
                    EsDescendiente(componente as GrupoPermisos, idNuevoGrupoPadre.Value))
                {
                    return ResultadoOperacion.Error("No se puede mover un grupo dentro de sí mismo o de uno de sus subgrupos.");
                }
            }

            try
            {
                _mppComponente.MoverComponente(componente, nuevoPadre);
                InvalidarCacheCompleta();

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", "ComponentePermiso", idComponente,
                    "Se movió \"" + componente.Nombre + "\" a " +
                    (nuevoPadre == null ? "la raíz" : "\"" + nuevoPadre.Nombre + "\"") + ".");

                return ResultadoOperacion.Ok("Se movió correctamente.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        public ResultadoOperacion EliminarGrupo(int idGrupo, int idUsuarioInternoResponsable)
        {
            GrupoPermisos raiz = ListarArbolCompleto();
            GrupoPermisos grupo = BuscarComponente(raiz, idGrupo) as GrupoPermisos;
            if (grupo == null || grupo.IdComponentePermiso == 0)
            {
                return ResultadoOperacion.Error("El grupo indicado no existe.");
            }

            if (grupo.Hijos.Count > 0)
            {
                return ResultadoOperacion.Error("El grupo tiene elementos adentro: movelos o eliminalos antes de borrar el grupo.");
            }

            try
            {
                _mppComponente.EliminarGrupo(grupo);
                InvalidarCacheCompleta();

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", "ComponentePermiso", idGrupo,
                    "Baja del grupo de permisos \"" + grupo.Nombre + "\".");

                return ResultadoOperacion.Ok("El grupo se eliminó correctamente.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
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

            List<PermisoHoja> hojasAsignadas = new List<PermisoHoja>();

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

            // Un rol puede tener asignada una hoja suelta O un grupo entero (idsAsignados
            // guarda cualquiera de los dos). Acá es donde se ve el Composite funcionando de
            // verdad: da lo mismo si el componente asignado es una hoja o un grupo, en los
            // dos casos alcanza con llamar a Listar() para obtener las hojas reales que
            // ese componente representa, sin distinguir casos a mano.
            HashSet<int> idsYaAgregados = new HashSet<int>();
            foreach (PermisoComponente nodo in RecorrerTodos(raiz))
            {
                if (!idsAsignados.Contains(nodo.IdComponentePermiso))
                {
                    continue;
                }

                foreach (PermisoHoja hoja in nodo.Listar())
                {
                    if (idsYaAgregados.Add(hoja.IdComponentePermiso))
                    {
                        hojasAsignadas.Add(hoja);
                    }
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

        // Crear, mover o eliminar un grupo cambia la forma del árbol (y con ella el
        // NombreGrupo/la pertenencia de las hojas), así que invalida lo cacheado para
        // todos los roles en vez de solo uno.
        private static void InvalidarCacheCompleta()
        {
            lock (CacheLock)
            {
                CachePermisosPorRol.Clear();
            }
        }

        // Recorre el árbol completo (grupos y hojas), no solo las hojas como Listar().
        // Se usa para ubicar un componente puntual por id y para resolver asignaciones
        // por grupo, sin necesidad de tocar las clases del BE.
        private static IEnumerable<PermisoComponente> RecorrerTodos(PermisoComponente nodo)
        {
            yield return nodo;

            GrupoPermisos grupo = nodo as GrupoPermisos;
            if (grupo != null)
            {
                foreach (PermisoComponente hijo in grupo.Hijos)
                {
                    foreach (PermisoComponente descendiente in RecorrerTodos(hijo))
                    {
                        yield return descendiente;
                    }
                }
            }
        }

        private static PermisoComponente BuscarComponente(PermisoComponente raiz, int idComponentePermiso)
        {
            foreach (PermisoComponente candidato in RecorrerTodos(raiz))
            {
                if (candidato.IdComponentePermiso == idComponentePermiso)
                {
                    return candidato;
                }
            }
            return null;
        }

        private static bool EsDescendiente(GrupoPermisos posibleAncestro, int idCandidatoDescendiente)
        {
            if (posibleAncestro == null)
            {
                return false;
            }

            foreach (PermisoComponente nodo in RecorrerTodos(posibleAncestro))
            {
                if (nodo.IdComponentePermiso == idCandidatoDescendiente)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
