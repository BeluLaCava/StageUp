using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Idioma
    {
        public List<Idioma> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Idioma_Listar");
            List<Idioma> idiomas = new List<Idioma>();
            foreach (DataRow fila in tabla.Rows)
            {
                idiomas.Add(Mapear(fila));
            }

            return idiomas;
        }

        public Idioma ObtenerPorId(Idioma oIdioma)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Idioma_ObtenerPorId",
                new Hashtable { { "@idIdioma", oIdioma.IdIdioma } });

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public int Insertar(Idioma oIdioma)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Idioma_Insertar",
                new Hashtable
                {
                    { "@codigoIdioma", oIdioma.CodigoIdioma },
                    { "@nombreIdioma", oIdioma.NombreIdioma },
                    { "@esPredeterminado", oIdioma.EsPredeterminado }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(Idioma oIdioma)
        {
            Conexion.Instance.Guardar(
                "sp_Idioma_Modificar",
                new Hashtable
                {
                    { "@idIdioma", oIdioma.IdIdioma },
                    { "@codigoIdioma", oIdioma.CodigoIdioma },
                    { "@nombreIdioma", oIdioma.NombreIdioma },
                    { "@esPredeterminado", oIdioma.EsPredeterminado }
                });
        }

        public void DarDeBaja(Idioma oIdioma)
        {
            Conexion.Instance.Guardar(
                "sp_Idioma_DarDeBaja",
                new Hashtable { { "@idIdioma", oIdioma.IdIdioma } });
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
