using System;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_CodigoRecuperacion
    {
        private readonly DAL_CodigoRecuperacion _dal = new DAL_CodigoRecuperacion();

        public int Insertar(CodigoRecuperacion codigo)
        {
            return _dal.Insertar(codigo.IdUsuarioExterno, codigo.Codigo, codigo.FechaVencimiento);
        }

        public CodigoRecuperacion ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            DataTable tabla = _dal.ObtenerVigentePorUsuario(idUsuarioExterno);
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void MarcarUtilizado(int idCodigoRecuperacion)
        {
            _dal.MarcarUtilizado(idCodigoRecuperacion);
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
