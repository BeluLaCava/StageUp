using System;
using System.Collections;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_CodigoActivacion
    {
        public int Insertar(CodigoActivacion oCodigoActivacion)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_CodigoActivacion_Insertar",
                new Hashtable
                {
                    { "@idUsuarioExterno", oCodigoActivacion.IdUsuarioExterno },
                    { "@codigo", oCodigoActivacion.Codigo },
                    { "@fechaVencimiento", oCodigoActivacion.FechaVencimiento }
                });

            return Convert.ToInt32(resultado);
        }

        public CodigoActivacion ObtenerVigentePorUsuario(UsuarioExterno oUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CodigoActivacion_ObtenerVigentePorUsuario",
                new Hashtable { { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void MarcarUtilizado(CodigoActivacion oCodigoActivacion)
        {
            Conexion.Instance.Guardar(
                "sp_CodigoActivacion_MarcarUtilizado",
                new Hashtable { { "@idCodigoActivacion", oCodigoActivacion.IdCodigoActivacion } });
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
