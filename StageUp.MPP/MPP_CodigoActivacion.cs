using System;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_CodigoActivacion
    {
        public int Insertar(CodigoActivacion codigo)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_CodigoActivacion_Insertar",
                new SqlParameter("@idUsuarioExterno", codigo.IdUsuarioExterno),
                new SqlParameter("@codigo", codigo.Codigo),
                new SqlParameter("@fechaVencimiento", codigo.FechaVencimiento));

            return Convert.ToInt32(resultado);
        }

        public CodigoActivacion ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CodigoActivacion_ObtenerVigentePorUsuario",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void MarcarUtilizado(int idCodigoActivacion)
        {
            Conexion.Instance.Guardar(
                "sp_CodigoActivacion_MarcarUtilizado",
                new SqlParameter("@idCodigoActivacion", idCodigoActivacion));
        }

        private static CodigoActivacion MapearDesdeFila(DataRow fila)
        {
            return new CodigoActivacion
            {
                IdCodigoActivacion = Convert.ToInt32(fila["idCodigoActivacion"]),
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                Codigo = fila["codigo"].ToString(),
                FechaGeneracion = Convert.ToDateTime(fila["fechaGeneracion"]),
                FechaVencimiento = Convert.ToDateTime(fila["fechaVencimiento"]),
                Utilizado = Convert.ToBoolean(fila["utilizado"]),
                FechaUtilizacion = fila["fechaUtilizacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUtilizacion"])
            };
        }
    }
}
