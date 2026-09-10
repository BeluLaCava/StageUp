using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_UsuarioInterno
    {
        public int Insertar(UsuarioInterno usuario)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioInterno_Insertar",
                new SqlParameter("@idAreaInterna", usuario.IdAreaInterna),
                new SqlParameter("@idRolInterno", usuario.IdRolInterno),
                new SqlParameter("@nombre", usuario.Nombre),
                new SqlParameter("@apellido", usuario.Apellido),
                new SqlParameter("@correoElectronico", usuario.CorreoElectronico),
                new SqlParameter("@passwordHash", usuario.PasswordHash),
                new SqlParameter("@estadoCuenta", usuario.EstadoCuenta));

            return Convert.ToInt32(resultado);
        }

        public UsuarioInterno ObtenerPorCorreo(string correoElectronico)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioInterno_ObtenerPorCorreo",
                new SqlParameter("@correoElectronico", correoElectronico));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public UsuarioInterno ObtenerPorId(int idUsuarioInterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioInterno_ObtenerPorId",
                new SqlParameter("@idUsuarioInterno", idUsuarioInterno));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<UsuarioInterno> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_UsuarioInterno_Listar");
            var usuarios = new List<UsuarioInterno>();

            foreach (DataRow fila in tabla.Rows)
            {
                usuarios.Add(MapearDesdeFila(fila));
            }

            return usuarios;
        }

        public bool ExisteCorreo(string correoElectronico, int? idUsuarioInternoExcluido)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioInterno_ExisteCorreo",
                new SqlParameter("@correoElectronico", correoElectronico),
                new SqlParameter("@idUsuarioInternoExcluido", (object)idUsuarioInternoExcluido ?? DBNull.Value));

            return Convert.ToInt32(resultado) > 0;
        }

        public void Modificar(UsuarioInterno usuario)
        {
            Conexion.Instance.Guardar(
                "sp_UsuarioInterno_Modificar",
                new SqlParameter("@idUsuarioInterno", usuario.IdUsuarioInterno),
                new SqlParameter("@idAreaInterna", usuario.IdAreaInterna),
                new SqlParameter("@idRolInterno", usuario.IdRolInterno),
                new SqlParameter("@nombre", usuario.Nombre),
                new SqlParameter("@apellido", usuario.Apellido),
                new SqlParameter("@correoElectronico", usuario.CorreoElectronico),
                new SqlParameter("@passwordHash", (object)usuario.PasswordHash ?? DBNull.Value),
                new SqlParameter("@estadoCuenta", usuario.EstadoCuenta));
        }

        public void DarDeBaja(int idUsuarioInterno)
        {
            Conexion.Instance.Guardar(
                "sp_UsuarioInterno_Baja",
                new SqlParameter("@idUsuarioInterno", idUsuarioInterno));
        }

        public int ContarActivosPorRol(int idRolInterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioInterno_ContarActivosPorRol",
                new SqlParameter("@idRolInterno", idRolInterno));

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
