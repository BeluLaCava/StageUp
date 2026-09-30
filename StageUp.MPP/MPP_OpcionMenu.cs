using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // ABMC del menú dinámico (ítem 19, script 45). Mismo patrón que el resto
    // de los mappers: Conexion.Instance + Hashtable, y todos los métodos
    // reciben objetos.
    public class MPP_OpcionMenu
    {
        public List<OpcionMenu> Listar()
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_OpcionMenu_Listar",
                new Hashtable { { "@soloActivas", false } }));
        }

        public List<OpcionMenu> ListarActivas()
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_OpcionMenu_Listar",
                new Hashtable { { "@soloActivas", true } }));
        }

        public OpcionMenu ObtenerPorId(OpcionMenu oOpcion)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_OpcionMenu_ObtenerPorId",
                new Hashtable { { "@idOpcionMenu", oOpcion.IdOpcionMenu } });

            return tabla.Rows.Count == 0 ? null : MapearFila(tabla.Rows[0]);
        }

        public int Insertar(OpcionMenu oOpcion)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_OpcionMenu_Insertar",
                new Hashtable
                {
                    { "@texto", oOpcion.Texto },
                    { "@descripcion", (object)oOpcion.Descripcion ?? DBNull.Value },
                    { "@url", oOpcion.Url },
                    { "@modulo", oOpcion.Modulo },
                    { "@idComponentePermiso", oOpcion.IdComponentePermiso },
                    { "@activo", oOpcion.Activo }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(OpcionMenu oOpcion)
        {
            Conexion.Instance.Guardar(
                "sp_OpcionMenu_Modificar",
                new Hashtable
                {
                    { "@idOpcionMenu", oOpcion.IdOpcionMenu },
                    { "@texto", oOpcion.Texto },
                    { "@descripcion", (object)oOpcion.Descripcion ?? DBNull.Value },
                    { "@url", oOpcion.Url },
                    { "@modulo", oOpcion.Modulo },
                    { "@idComponentePermiso", oOpcion.IdComponentePermiso },
                    { "@activo", oOpcion.Activo }
                });
        }

        public void CambiarEstado(OpcionMenu oOpcion)
        {
            Conexion.Instance.Guardar(
                "sp_OpcionMenu_CambiarEstado",
                new Hashtable
                {
                    { "@idOpcionMenu", oOpcion.IdOpcionMenu },
                    { "@activo", oOpcion.Activo }
                });
        }

        public void ActualizarOrden(OpcionMenu oOpcion)
        {
            Conexion.Instance.Guardar(
                "sp_OpcionMenu_ActualizarOrden",
                new Hashtable
                {
                    { "@idOpcionMenu", oOpcion.IdOpcionMenu },
                    { "@orden", oOpcion.Orden }
                });
        }

        private static List<OpcionMenu> Mapear(DataTable tabla)
        {
            List<OpcionMenu> lista = new List<OpcionMenu>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearFila(fila));
            }
            return lista;
        }

        private static OpcionMenu MapearFila(DataRow fila)
        {
            return new OpcionMenu
            {
                IdOpcionMenu = Convert.ToInt32(fila["idOpcionMenu"]),
                Texto = fila["texto"].ToString(),
                Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                Url = fila["url"].ToString(),
                Modulo = fila["modulo"].ToString(),
                Orden = Convert.ToInt32(fila["orden"]),
                IdComponentePermiso = Convert.ToInt32(fila["idComponentePermiso"]),
                Activo = Convert.ToBoolean(fila["activo"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                CodigoPermiso = fila["codigoPermiso"] == DBNull.Value ? null : fila["codigoPermiso"].ToString(),
                NombrePermiso = fila["nombrePermiso"] == DBNull.Value ? null : fila["nombrePermiso"].ToString()
            };
        }
    }
}
