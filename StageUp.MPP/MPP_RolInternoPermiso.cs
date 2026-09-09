using System.Data.SqlClient;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInternoPermiso
    {
        public void EliminarPorRol(int idRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoPermiso_EliminarPorRol",
                new SqlParameter("@idRolInterno", idRolInterno));
        }

        public void Insertar(int idRolInterno, int idPermisoInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoPermiso_Insertar",
                new SqlParameter("@idRolInterno", idRolInterno),
                new SqlParameter("@idPermisoInterno", idPermisoInterno));
        }
    }
}
