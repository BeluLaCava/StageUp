using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInterno
    {
        public int Insertar(RolInterno oRolInterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RolInterno_Insertar",
                new Hashtable
                {
                    { "@idAreaInterna", (object)oRolInterno.IdAreaInterna ?? DBNull.Value },
                    { "@nombreRol", oRolInterno.NombreRol },
                    { "@descripcion", (object)oRolInterno.Descripcion ?? DBNull.Value },
                    { "@estadoRol", oRolInterno.EstadoRol }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(RolInterno oRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInterno_Modificar",
                new Hashtable
                {
                    { "@idRolInterno", oRolInterno.IdRolInterno },
                    { "@idAreaInterna", (object)oRolInterno.IdAreaInterna ?? DBNull.Value },
                    { "@nombreRol", oRolInterno.NombreRol },
                    { "@descripcion", (object)oRolInterno.Descripcion ?? DBNull.Value }
                });
        }

        public RolInterno ObtenerPorId(RolInterno oRolInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RolInterno_ObtenerPorId",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<RolInterno> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_RolInterno_Listar");
            List<RolInterno> lista = new List<RolInterno>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        public void Baja(RolInterno oRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInterno_Baja",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });
        }

        private static RolInterno MapearDesdeFila(DataRow fila)
        {
            return new RolInterno
            {
                IdRolInterno = Convert.ToInt32(fila["idRolInterno"]),
                IdAreaInterna = fila["idAreaInterna"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idAreaInterna"]),
                NombreArea = fila.Table.Columns.Contains("nombreArea") && fila["nombreArea"] != DBNull.Value
                    ? fila["nombreArea"].ToString() : null,
                NombreRol = fila["nombreRol"].ToString(),
                Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                EstadoRol = fila["estadoRol"].ToString(),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"])
            };
        }
    }
}
