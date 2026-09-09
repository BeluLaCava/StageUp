using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInterno
    {
        public int Insertar(RolInterno rol)
        {
            object resultado = EjecutorStoredProcedure.LeerEscalar(
                "sp_RolInterno_Insertar",
                new SqlParameter("@idAreaInterna", (object)rol.IdAreaInterna ?? DBNull.Value),
                new SqlParameter("@nombreRol", rol.NombreRol),
                new SqlParameter("@descripcion", (object)rol.Descripcion ?? DBNull.Value),
                new SqlParameter("@estadoRol", rol.EstadoRol));

            return Convert.ToInt32(resultado);
        }

        public void Modificar(RolInterno rol)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_RolInterno_Modificar",
                new SqlParameter("@idRolInterno", rol.IdRolInterno),
                new SqlParameter("@idAreaInterna", (object)rol.IdAreaInterna ?? DBNull.Value),
                new SqlParameter("@nombreRol", rol.NombreRol),
                new SqlParameter("@descripcion", (object)rol.Descripcion ?? DBNull.Value));
        }

        public RolInterno ObtenerPorId(int idRolInterno)
        {
            DataTable tabla = EjecutorStoredProcedure.Leer(
                "sp_RolInterno_ObtenerPorId",
                new SqlParameter("@idRolInterno", idRolInterno));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<RolInterno> Listar()
        {
            DataTable tabla = EjecutorStoredProcedure.Leer("sp_RolInterno_Listar");
            var lista = new List<RolInterno>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        public void Baja(int idRolInterno)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_RolInterno_Baja",
                new SqlParameter("@idRolInterno", idRolInterno));
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
