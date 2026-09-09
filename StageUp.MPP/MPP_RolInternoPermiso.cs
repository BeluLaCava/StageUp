using System.Data.SqlClient;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInternoPermiso
    {
        public void EliminarPorRol(int idRolInterno)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_RolInternoPermiso_EliminarPorRol",
                new SqlParameter("@idRolInterno", idRolInterno));
        }

        public void Insertar(int idRolInterno, int idPermisoInterno)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_RolInternoPermiso_Insertar",
                new SqlParameter("@idRolInterno", idRolInterno),
                new SqlParameter("@idPermisoInterno", idPermisoInterno));
        }
    }
}
