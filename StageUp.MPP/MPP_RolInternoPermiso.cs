using System.Collections;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInternoPermiso
    {
        public void EliminarPorRol(RolInterno oRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoPermiso_EliminarPorRol",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });
        }

        public void Insertar(RolInterno oRolInterno, PermisoInterno oPermisoInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoPermiso_Insertar",
                new Hashtable
                {
                    { "@idRolInterno", oRolInterno.IdRolInterno },
                    { "@idPermisoInterno", oPermisoInterno.IdPermisoInterno }
                });
        }
    }
}
