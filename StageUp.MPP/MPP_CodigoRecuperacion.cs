using System;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_CodigoRecuperacion
    {
        public int Insertar(CodigoRecuperacion codigo)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_CodigoRecuperacion_Insertar",
                new SqlParameter("@idUsuarioExterno", codigo.IdUsuarioExterno),
                new SqlParameter("@codigo", codigo.Codigo),
                new SqlParameter("@fechaVencimiento", codigo.FechaVencimiento));

            return Convert.ToInt32(resultado);
        }

        public CodigoRecuperacion ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CodigoRecuperacion_ObtenerVigentePorUsuario",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void MarcarUtilizado(int idCodigoRecuperacion)
        {
            Conexion.Instance.Guardar(
                "sp_CodigoRecuperacion_MarcarUtilizado",
                new SqlParameter("@idCodigoRecuperacion", idCodigoRecuperacion));
        }

        private static CodigoRecuperacion MapearDesdeFila(DataRow fila)
        {
            return new CodigoRecuperacion
            {
                IdCodigoRecuperacion = Convert.ToInt32(fila["idCodigoRecuperacion"]),
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
