using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Traduccion
    {
        public List<Traduccion> ListarConfiguracion(int idIdioma)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Traduccion_ListarConfiguracion",
                new SqlParameter("@idIdioma", SqlDbType.Int) { Value = idIdioma });

            var traducciones = new List<Traduccion>();
            foreach (DataRow fila in tabla.Rows)
            {
                traducciones.Add(new Traduccion
                {
                    IdTraduccion = fila["idTraduccion"] == DBNull.Value
                        ? (int?)null
                        : Convert.ToInt32(fila["idTraduccion"]),
                    IdIdioma = idIdioma,
                    IdEtiquetaTraduccion = Convert.ToInt32(fila["idEtiquetaTraduccion"]),
                    ClaveEtiqueta = fila["claveEtiqueta"].ToString(),
                    TextoPredeterminado = fila["textoPredeterminado"].ToString(),
                    TextoTraducido = fila["textoTraducido"] == DBNull.Value ? null : fila["textoTraducido"].ToString(),
                    Modulo = fila["modulo"].ToString(),
                    FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(fila["fechaUltimaModificacion"])
                });
            }

            return traducciones;
        }

        public void Guardar(Traduccion traduccion)
        {
            Conexion.Instance.Guardar(
                "sp_Traduccion_Guardar",
                new SqlParameter("@idIdioma", SqlDbType.Int) { Value = traduccion.IdIdioma },
                new SqlParameter("@idEtiquetaTraduccion", SqlDbType.Int) { Value = traduccion.IdEtiquetaTraduccion },
                new SqlParameter("@textoTraducido", SqlDbType.NVarChar, 2000) { Value = traduccion.TextoTraducido });
        }

        public void Eliminar(int idIdioma, int idEtiquetaTraduccion)
        {
            Conexion.Instance.Guardar(
                "sp_Traduccion_Eliminar",
                new SqlParameter("@idIdioma", SqlDbType.Int) { Value = idIdioma },
                new SqlParameter("@idEtiquetaTraduccion", SqlDbType.Int) { Value = idEtiquetaTraduccion });
        }
    }
}
