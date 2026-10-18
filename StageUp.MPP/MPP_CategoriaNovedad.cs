using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Categorías de novedades y del newsletter (script 85).
    public class MPP_CategoriaNovedad
    {
        public List<CategoriaNovedad> Listar(bool soloActivas)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CategoriaNovedad_Listar",
                new Hashtable { { "@soloActivas", soloActivas } });

            List<CategoriaNovedad> lista = new List<CategoriaNovedad>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new CategoriaNovedad
                {
                    Codigo = fila["codigo"].ToString(),
                    Nombre = fila["nombre"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"]),
                    Activa = Convert.ToBoolean(fila["activa"]),
                    FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                    FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                        ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                    CantidadNovedades = Convert.ToInt32(fila["cantidadNovedades"]),
                    CantidadSuscriptores = Convert.ToInt32(fila["cantidadSuscriptores"])
                });
            }
            return lista;
        }

        public void Insertar(CategoriaNovedad oCategoria)
        {
            Conexion.Instance.Guardar(
                "sp_CategoriaNovedad_Insertar",
                new Hashtable
                {
                    { "@codigo", oCategoria.Codigo },
                    { "@nombre", oCategoria.Nombre },
                    { "@orden", oCategoria.Orden }
                });
        }

        public void Modificar(CategoriaNovedad oCategoria)
        {
            Conexion.Instance.Guardar(
                "sp_CategoriaNovedad_Modificar",
                new Hashtable
                {
                    { "@codigo", oCategoria.Codigo },
                    { "@nombre", oCategoria.Nombre },
                    { "@orden", oCategoria.Orden }
                });
        }

        public void CambiarEstado(CategoriaNovedad oCategoria)
        {
            Conexion.Instance.Guardar(
                "sp_CategoriaNovedad_CambiarEstado",
                new Hashtable
                {
                    { "@codigo", oCategoria.Codigo },
                    { "@activa", oCategoria.Activa }
                });
        }
    }
}
