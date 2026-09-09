using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Menu;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_PermisoInterno
    {
        private readonly MPP_PermisoInterno _mppPermiso = new MPP_PermisoInterno();
        private readonly MPP_RolInternoPermiso _mppRolPermiso = new MPP_RolInternoPermiso();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<PermisoInterno> Listar()
        {
            try
            {
                return _mppPermiso.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<PermisoInterno>();
            }
        }

        public List<PermisoInterno> ListarPorRol(int idRolInterno)
        {
            try
            {
                return _mppPermiso.ListarPorRol(idRolInterno);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<PermisoInterno>();
            }
        }

        public List<PermisoInterno> ListarConAsignacion(int idRolInterno)
        {
            List<PermisoInterno> todos = Listar();
            List<PermisoInterno> asignados = ListarPorRol(idRolInterno);

            foreach (PermisoInterno permiso in todos)
            {
                permiso.Asignado = ContieneCodigo(asignados, permiso.CodigoPermiso);
            }

            return todos;
        }

        public List<string> ListarCodigosPermisosDeRol(int idRolInterno)
        {
            var codigos = new List<string>();
            foreach (PermisoInterno permiso in ListarPorRol(idRolInterno))
            {
                codigos.Add(permiso.CodigoPermiso);
            }
            return codigos;
        }

        public GrupoMenu ConstruirMenuParaRol(int idRolInterno)
        {
            var raiz = new GrupoMenu("Menú");
            GrupoMenu grupoActual = null;
            string moduloActual = null;

            foreach (PermisoInterno permiso in ListarPorRol(idRolInterno))
            {
                if (grupoActual == null || permiso.Modulo != moduloActual)
                {
                    grupoActual = new GrupoMenu(permiso.Modulo);
                    raiz.Agregar(grupoActual);
                    moduloActual = permiso.Modulo;
                }

                grupoActual.Agregar(new ItemMenu(permiso.NombrePermiso, permiso.UrlAsociada, permiso.Descripcion));
            }

            return raiz;
        }

        public ResultadoOperacion AsignarPermisosARol(int idRolInterno, List<int> idsPermisosSeleccionados, int idUsuarioInternoResponsable)
        {
            try
            {
                _mppRolPermiso.EliminarPorRol(idRolInterno);

                if (idsPermisosSeleccionados != null)
                {
                    foreach (int idPermisoInterno in idsPermisosSeleccionados)
                    {
                        _mppRolPermiso.Insertar(idRolInterno, idPermisoInterno);
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

        private static bool ContieneCodigo(List<PermisoInterno> permisos, string codigoPermiso)
        {
            foreach (PermisoInterno permiso in permisos)
            {
                if (permiso.CodigoPermiso == codigoPermiso)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
