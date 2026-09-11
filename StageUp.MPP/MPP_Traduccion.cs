using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Traduccion
    {
        public List<Traduccion> ListarDiccionario(Idioma oIdioma)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Traduccion_ListarDiccionario",
                new Hashtable { { "@idIdioma", oIdioma.IdIdioma } });

            List<Traduccion> traducciones = new List<Traduccion>();
            foreach (DataRow fila in tabla.Rows)
            {
                traducciones.Add(new Traduccion
                {
                    IdIdioma = oIdioma.IdIdioma,
                    ClaveEtiqueta = fila["claveEtiqueta"].ToString(),
                    TextoPredeterminado = fila["textoPredeterminado"].ToString(),
                    TextoTraducido = fila["textoTraducido"].ToString(),
                    Modulo = fila["modulo"].ToString()
                });
            }

            return traducciones;
        }

        public List<Traduccion> ListarConfiguracion(Idioma oIdioma)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Traduccion_ListarConfiguracion",
                new Hashtable { { "@idIdioma", oIdioma.IdIdioma } });

            List<Traduccion> traducciones = new List<Traduccion>();
            foreach (DataRow fila in tabla.Rows)
            {
                traducciones.Add(new Traduccion
                {
                    IdTraduccion = fila["idTraduccion"] == DBNull.Value
                        ? (int?)null
                        : Convert.ToInt32(fila["idTraduccion"]),
                    IdIdioma = oIdioma.IdIdioma,
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

        public void Guardar(Traduccion oTraduccion)
        {
            Conexion.Instance.Guardar(
                "sp_Traduccion_Guardar",
                new Hashtable
                {
                    { "@idIdioma", oTraduccion.IdIdioma },
                    { "@idEtiquetaTraduccion", oTraduccion.IdEtiquetaTraduccion },
                    { "@textoTraducido", oTraduccion.TextoTraducido }
                });
        }

        public void Eliminar(Traduccion oTraduccion)
        {
            Conexion.Instance.Guardar(
                "sp_Traduccion_Eliminar",
                new Hashtable
                {
                    { "@idIdioma", oTraduccion.IdIdioma },
                    { "@idEtiquetaTraduccion", oTraduccion.IdEtiquetaTraduccion }
                });
        }
    }
}
