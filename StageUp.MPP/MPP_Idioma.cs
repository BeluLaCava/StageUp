using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Idioma
    {
        public List<Idioma> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Idioma_Listar");
            var idiomas = new List<Idioma>();
            foreach (DataRow fila in tabla.Rows)
            {
                idiomas.Add(Mapear(fila));
            }

            return idiomas;
        }

        public Idioma ObtenerPorId(int idIdioma)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Idioma_ObtenerPorId",
                new SqlParameter("@idIdioma", SqlDbType.Int) { Value = idIdioma });

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public int Insertar(Idioma idioma)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Idioma_Insertar",
                new SqlParameter("@codigoIdioma", SqlDbType.NVarChar, 20) { Value = idioma.CodigoIdioma },
                new SqlParameter("@nombreIdioma", SqlDbType.NVarChar, 100) { Value = idioma.NombreIdioma },
                new SqlParameter("@esPredeterminado", SqlDbType.Bit) { Value = idioma.EsPredeterminado });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(Idioma idioma)
        {
            Conexion.Instance.Guardar(
                "sp_Idioma_Modificar",
                new SqlParameter("@idIdioma", SqlDbType.Int) { Value = idioma.IdIdioma },
                new SqlParameter("@codigoIdioma", SqlDbType.NVarChar, 20) { Value = idioma.CodigoIdioma },
                new SqlParameter("@nombreIdioma", SqlDbType.NVarChar, 100) { Value = idioma.NombreIdioma },
                new SqlParameter("@esPredeterminado", SqlDbType.Bit) { Value = idioma.EsPredeterminado });
        }

        public void DarDeBaja(int idIdioma)
        {
            Conexion.Instance.Guardar(
                "sp_Idioma_DarDeBaja",
                new SqlParameter("@idIdioma", SqlDbType.Int) { Value = idIdioma });
        }

        private static Idioma Mapear(DataRow fila)
        {
            return new Idioma
            {
                IdIdioma = Convert.ToInt32(fila["idIdioma"]),
                CodigoIdioma = fila["codigoIdioma"].ToString(),
                NombreIdioma = fila["nombreIdioma"].ToString(),
                EsPredeterminado = Convert.ToBoolean(fila["esPredeterminado"]),
                EstadoIdioma = fila["estadoIdioma"].ToString(),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"]),
                CantidadEtiquetas = fila.Table.Columns.Contains("cantidadEtiquetas")
                    ? Convert.ToInt32(fila["cantidadEtiquetas"])
                    : 0,
                CantidadTraducciones = fila.Table.Columns.Contains("cantidadTraducciones")
                    ? Convert.ToInt32(fila["cantidadTraducciones"])
                    : 0
            };
        }
    }
}
