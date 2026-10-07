using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RolInterno
    {
        public int Insertar(RolInterno oRolInterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RolInterno_Insertar",
                new Hashtable
                {
                    { "@idAreaInterna", (object)oRolInterno.IdAreaInterna ?? DBNull.Value },
                    { "@nombreRol", oRolInterno.NombreRol },
                    { "@descripcion", (object)oRolInterno.Descripcion ?? DBNull.Value },
                    { "@estadoRol", oRolInterno.EstadoRol }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(RolInterno oRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInterno_Modificar",
                new Hashtable
                {
                    { "@idRolInterno", oRolInterno.IdRolInterno },
                    { "@idAreaInterna", (object)oRolInterno.IdAreaInterna ?? DBNull.Value },
                    { "@nombreRol", oRolInterno.NombreRol },
                    { "@descripcion", (object)oRolInterno.Descripcion ?? DBNull.Value }
                });
        }

        public RolInterno ObtenerPorId(RolInterno oRolInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RolInterno_ObtenerPorId",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<RolInterno> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_RolInterno_Listar");
            List<RolInterno> lista = new List<RolInterno>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        // CU-001-012 A11 paso 4: listado con área, estado y cantidades.
        public List<RolInterno> ListarResumen(string estado)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RolInterno_ListarResumen",
                new Hashtable { { "@estado", estado } });
            List<RolInterno> lista = new List<RolInterno>();
            foreach (DataRow fila in tabla.Rows)
            {
                RolInterno rol = MapearDesdeFila(fila);
                rol.CantidadUsuariosActivos = Convert.ToInt32(fila["cantidadUsuariosActivos"]);
                rol.CantidadComponentes = Convert.ToInt32(fila["cantidadComponentes"]);
                lista.Add(rol);
            }
            return lista;
        }

        // A14: otro rol activo con el mismo nombre.
        public bool ExisteNombreActivo(RolInterno oRolInterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RolInterno_ExisteNombreActivo",
                new Hashtable
                {
                    { "@nombreRol", oRolInterno.NombreRol },
                    { "@idRolInternoExcluido", oRolInterno.IdRolInterno == 0 ? (object)DBNull.Value : oRolInterno.IdRolInterno }
                });
            return resultado != null && resultado != DBNull.Value && Convert.ToInt32(resultado) > 0;
        }

        // A11 pasos 11 y 12 / A12 pasos 8 y 9: rol y permisos en una sola
        // transacción. Devuelve el id del rol (nuevo o modificado).
        public int GuardarConPermisos(RolInterno oRolInterno, IEnumerable<int> idsComponentes)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RolInterno_GuardarConPermisos",
                new Hashtable
                {
                    { "@idRolInterno", oRolInterno.IdRolInterno == 0 ? (object)DBNull.Value : oRolInterno.IdRolInterno },
                    { "@idAreaInterna", (object)oRolInterno.IdAreaInterna ?? DBNull.Value },
                    { "@nombreRol", oRolInterno.NombreRol },
                    { "@descripcion", (object)oRolInterno.Descripcion ?? DBNull.Value },
                    { "@idsComponentes", string.Join(",", idsComponentes) }
                });
            return Convert.ToInt32(resultado);
        }

        public void Baja(RolInterno oRolInterno)
        {
            Conexion.Instance.Guardar(
                "sp_RolInterno_Baja",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });
        }

        private static RolInterno MapearDesdeFila(DataRow fila)
        {
            return new RolInterno
            {
                IdRolInterno = Convert.ToInt32(fila["idRolInterno"]),
                IdAreaInterna = fila["idAreaInterna"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idAreaInterna"]),
                NombreArea = fila.Table.Columns.Contains("nombreArea") && fila["nombreArea"] != DBNull.Value
                    ? fila["nombreArea"].ToString() : null,
                NombreRol = fila["nombreRol"].ToString(),
                Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                EstadoRol = fila["estadoRol"].ToString(),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"])
            };
        }
    }
}
