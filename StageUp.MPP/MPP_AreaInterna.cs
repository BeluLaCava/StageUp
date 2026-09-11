using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_AreaInterna
    {
        public List<AreaInterna> ListarActivas()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_AreaInterna_ListarActivas");
            List<AreaInterna> areas = new List<AreaInterna>();

            foreach (DataRow fila in tabla.Rows)
            {
                areas.Add(MapearDesdeFila(fila));
            }

            return areas;
        }

        public AreaInterna ObtenerPorId(AreaInterna oAreaInterna)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_AreaInterna_ObtenerPorId",
                new Hashtable
                {
                    { "@idAreaInterna", oAreaInterna.IdAreaInterna }
                });

            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        private static AreaInterna MapearDesdeFila(DataRow fila)
        {
            return new AreaInterna
            {
                IdAreaInterna = Convert.ToInt32(fila["idAreaInterna"]),
                NombreArea = fila["nombreArea"].ToString(),
                Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                EstadoArea = fila["estadoArea"].ToString(),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"])
            };
        }
    }
}
