using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_PermisoInterno
    {
        public List<PermisoInterno> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_PermisoInterno_Listar");
            return MapearLista(tabla);
        }

        public List<PermisoInterno> ListarPorRol(int idRolInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_PermisoInterno_ListarPorRol",
                new SqlParameter("@idRolInterno", idRolInterno));
            return MapearLista(tabla);
        }

        private static List<PermisoInterno> MapearLista(DataTable tabla)
        {
            var lista = new List<PermisoInterno>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new PermisoInterno
                {
                    IdPermisoInterno = Convert.ToInt32(fila["idPermisoInterno"]),
                    CodigoPermiso = fila["codigoPermiso"].ToString(),
                    NombrePermiso = fila["nombrePermiso"].ToString(),
                    Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                    Modulo = fila["modulo"].ToString(),
                    Accion = fila["accion"].ToString(),
                    EstadoPermiso = fila["estadoPermiso"].ToString(),
                    UrlAsociada = fila["urlAsociada"] == DBNull.Value ? null : fila["urlAsociada"].ToString(),
                    FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                    FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                        ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                    Activo = Convert.ToBoolean(fila["activo"])
                });
            }
            return lista;
        }
    }
}
