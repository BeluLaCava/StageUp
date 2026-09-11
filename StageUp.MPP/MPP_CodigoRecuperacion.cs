using System;
using System.Collections;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_CodigoRecuperacion
    {
        public int Insertar(CodigoRecuperacion oCodigoRecuperacion)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_CodigoRecuperacion_Insertar",
                new Hashtable
                {
                    { "@idUsuarioExterno", oCodigoRecuperacion.IdUsuarioExterno },
                    { "@codigo", oCodigoRecuperacion.Codigo },
                    { "@fechaVencimiento", oCodigoRecuperacion.FechaVencimiento }
                });

            return Convert.ToInt32(resultado);
        }

        public CodigoRecuperacion ObtenerVigentePorUsuario(UsuarioExterno oUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CodigoRecuperacion_ObtenerVigentePorUsuario",
                new Hashtable { { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void MarcarUtilizado(CodigoRecuperacion oCodigoRecuperacion)
        {
            Conexion.Instance.Guardar(
                "sp_CodigoRecuperacion_MarcarUtilizado",
                new Hashtable { { "@idCodigoRecuperacion", oCodigoRecuperacion.IdCodigoRecuperacion } });
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
