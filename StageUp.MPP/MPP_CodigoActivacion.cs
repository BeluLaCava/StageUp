using System;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_CodigoActivacion
    {
        private readonly DAL_CodigoActivacion _dal = new DAL_CodigoActivacion();

        public int Insertar(CodigoActivacion codigo)
        {
            return _dal.Insertar(codigo.IdUsuarioExterno, codigo.Codigo, codigo.FechaVencimiento);
        }

        public CodigoActivacion ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            DataTable tabla = _dal.ObtenerVigentePorUsuario(idUsuarioExterno);
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void MarcarUtilizado(int idCodigoActivacion)
        {
            _dal.MarcarUtilizado(idCodigoActivacion);
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
