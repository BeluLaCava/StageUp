using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_UsuarioInterno
    {
        public int Insertar(UsuarioInterno oUsuarioInterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioInterno_Insertar",
                new Hashtable
                {
                    { "@idAreaInterna", oUsuarioInterno.IdAreaInterna },
                    { "@idRolInterno", oUsuarioInterno.IdRolInterno },
                    { "@nombre", oUsuarioInterno.Nombre },
                    { "@apellido", oUsuarioInterno.Apellido },
                    { "@correoElectronico", oUsuarioInterno.CorreoElectronico },
                    { "@passwordHash", oUsuarioInterno.PasswordHash },
                    { "@estadoCuenta", oUsuarioInterno.EstadoCuenta }
                });

            return Convert.ToInt32(resultado);
        }

        public UsuarioInterno ObtenerPorCorreo(UsuarioInterno oUsuarioInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioInterno_ObtenerPorCorreo",
                new Hashtable { { "@correoElectronico", oUsuarioInterno.CorreoElectronico } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public UsuarioInterno ObtenerPorId(UsuarioInterno oUsuarioInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioInterno_ObtenerPorId",
                new Hashtable { { "@idUsuarioInterno", oUsuarioInterno.IdUsuarioInterno } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<UsuarioInterno> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_UsuarioInterno_Listar");
            List<UsuarioInterno> usuarios = new List<UsuarioInterno>();

            foreach (DataRow fila in tabla.Rows)
            {
                usuarios.Add(MapearDesdeFila(fila));
            }

            return usuarios;
        }

        public bool ExisteCorreo(UsuarioInterno oUsuarioInterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioInterno_ExisteCorreo",
                new Hashtable
                {
                    { "@correoElectronico", oUsuarioInterno.CorreoElectronico },
                    { "@idUsuarioInternoExcluido", oUsuarioInterno.IdUsuarioInterno > 0 ? (object)oUsuarioInterno.IdUsuarioInterno : DBNull.Value }
                });

            return Convert.ToInt32(resultado) > 0;
        }

        public void Modificar(UsuarioInterno oUsuarioInterno)
        {
            Conexion.Instance.Guardar(
                "sp_UsuarioInterno_Modificar",
                new Hashtable
                {
                    { "@idUsuarioInterno", oUsuarioInterno.IdUsuarioInterno },
                    { "@idAreaInterna", oUsuarioInterno.IdAreaInterna },
                    { "@idRolInterno", oUsuarioInterno.IdRolInterno },
                    { "@nombre", oUsuarioInterno.Nombre },
                    { "@apellido", oUsuarioInterno.Apellido },
                    { "@correoElectronico", oUsuarioInterno.CorreoElectronico },
                    { "@passwordHash", (object)oUsuarioInterno.PasswordHash ?? DBNull.Value },
                    { "@estadoCuenta", oUsuarioInterno.EstadoCuenta }
                });
        }

        public void DarDeBaja(UsuarioInterno oUsuarioInterno)
        {
            Conexion.Instance.Guardar(
                "sp_UsuarioInterno_Baja",
                new Hashtable { { "@idUsuarioInterno", oUsuarioInterno.IdUsuarioInterno } });
        }

        public int ContarActivosPorRol(RolInterno oRolInterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioInterno_ContarActivosPorRol",
                new Hashtable { { "@idRolInterno", oRolInterno.IdRolInterno } });

            return Convert.ToInt32(resultado);
        }

        private static UsuarioInterno MapearDesdeFila(DataRow fila)
        {
            return new UsuarioInterno
            {
                IdUsuarioInterno = Convert.ToInt32(fila["idUsuarioInterno"]),
                IdAreaInterna = Convert.ToInt32(fila["idAreaInterna"]),
                IdRolInterno = Convert.ToInt32(fila["idRolInterno"]),
                NombreArea = fila.Table.Columns.Contains("nombreArea") && fila["nombreArea"] != DBNull.Value
                    ? fila["nombreArea"].ToString() : null,
                NombreRol = fila.Table.Columns.Contains("nombreRol") && fila["nombreRol"] != DBNull.Value
                    ? fila["nombreRol"].ToString() : null,
                Nombre = fila["nombre"].ToString(),
                Apellido = fila["apellido"].ToString(),
                CorreoElectronico = fila["correoElectronico"].ToString(),
                PasswordHash = fila["passwordHash"].ToString(),
                EstadoCuenta = fila["estadoCuenta"].ToString(),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"])
            };
        }
    }
}
