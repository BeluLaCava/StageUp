using System;
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
            object resultado = EjecutorStoredProcedure.LeerEscalar(
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
            DataTable tabla = EjecutorStoredProcedure.Leer(
                "sp_UsuarioInterno_ObtenerPorCorreo",
                new SqlParameter("@correoElectronico", correoElectronico));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public UsuarioInterno ObtenerPorId(int idUsuarioInterno)
        {
            DataTable tabla = EjecutorStoredProcedure.Leer(
                "sp_UsuarioInterno_ObtenerPorId",
                new SqlParameter("@idUsuarioInterno", idUsuarioInterno));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        private static UsuarioInterno MapearDesdeFila(DataRow fila)
        {
            return new UsuarioInterno
            {
                IdUsuarioInterno = Convert.ToInt32(fila["idUsuarioInterno"]),
                IdAreaInterna = Convert.ToInt32(fila["idAreaInterna"]),
                IdRolInterno = Convert.ToInt32(fila["idRolInterno"]),
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
