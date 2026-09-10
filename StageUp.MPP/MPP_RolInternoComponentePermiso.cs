using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInternoComponentePermiso
    {
        public List<int> ListarIdsPorRol(int idRolInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RolInternoComponentePermiso_ListarPorRol",
                new SqlParameter("@idRolInterno", idRolInterno));

            var ids = new List<int>();
            foreach (DataRow fila in tabla.Rows)
            {
                ids.Add(Convert.ToInt32(fila["idComponentePermiso"]));
            }
            return ids;
        }

        public void EliminarPorRol(int idRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoComponentePermiso_EliminarPorRol",
                new SqlParameter("@idRolInterno", idRolInterno));
        }

        public void Insertar(int idRolInterno, int idComponentePermiso)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoComponentePermiso_Insertar",
                new SqlParameter("@idRolInterno", idRolInterno),
                new SqlParameter("@idComponentePermiso", idComponentePermiso));
        }
    }
}
