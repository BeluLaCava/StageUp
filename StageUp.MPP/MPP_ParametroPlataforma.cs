using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Parámetros configurables de la plataforma (script 49).
    public class MPP_ParametroPlataforma
    {
        public List<ParametroPlataforma> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ParametroPlataforma_Listar");
            List<ParametroPlataforma> parametros = new List<ParametroPlataforma>();
            foreach (DataRow fila in tabla.Rows)
            {
                parametros.Add(new ParametroPlataforma
                {
                    Clave = fila["clave"].ToString(),
                    Valor = fila["valor"].ToString(),
                    Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                    FechaUltimaModificacion = Convert.ToDateTime(fila["fechaUltimaModificacion"])
                });
            }
            return parametros;
        }

        public bool Actualizar(ParametroPlataforma oParametro)
        {
            object filas = Conexion.Instance.LeerEscalar(
                "sp_ParametroPlataforma_Actualizar",
                new Hashtable
                {
                    { "@clave", oParametro.Clave },
                    { "@valor", oParametro.Valor }
                });
            return filas != null && filas != DBNull.Value && Convert.ToInt32(filas) > 0;
        }
    }
}
