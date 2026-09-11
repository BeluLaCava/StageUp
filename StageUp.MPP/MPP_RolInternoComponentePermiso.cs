using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInternoComponentePermiso
    {
        public List<int> ListarIdsPorRol(RolInterno oRolInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RolInternoComponentePermiso_ListarPorRol",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });

            List<int> ids = new List<int>();
            foreach (DataRow fila in tabla.Rows)
            {
                ids.Add(Convert.ToInt32(fila["idComponentePermiso"]));
            }
            return ids;
        }

        public void EliminarPorRol(RolInterno oRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoComponentePermiso_EliminarPorRol",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });
        }

        public void Insertar(RolInterno oRolInterno, PermisoComponente oComponentePermiso)
        {
            Conexion.Instance.Guardar(
                "sp_RolInternoComponentePermiso_Insertar",
                new Hashtable
                {
                    { "@idRolInterno", oRolInterno.IdRolInterno },
                    { "@idComponentePermiso", oComponentePermiso.IdComponentePermiso }
                });
        }
    }
}
