using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_UsuarioExterno
    {
        public int Insertar(UsuarioExterno usuario)
        {
            object resultado = EjecutorStoredProcedure.LeerEscalar(
                "sp_UsuarioExterno_Insertar",
                new SqlParameter("@nombre", usuario.Nombre),
                new SqlParameter("@apellido", usuario.Apellido),
                new SqlParameter("@correoElectronico", usuario.CorreoElectronico),
                new SqlParameter("@passwordHash", usuario.PasswordHash),
                new SqlParameter("@telefono", (object)usuario.Telefono ?? DBNull.Value),
                new SqlParameter("@estadoCuenta", usuario.EstadoCuenta),
                new SqlParameter("@perfilUsuario", usuario.PerfilUsuario),
                new SqlParameter("@aceptaTerminos", usuario.AceptaTerminos),
                new SqlParameter("@aceptaPoliticaPrivacidad", usuario.AceptaPoliticaPrivacidad),
                new SqlParameter("@fechaAceptacionTerminos", (object)usuario.FechaAceptacionTerminos ?? DBNull.Value));

            return Convert.ToInt32(resultado);
        }

        public UsuarioExterno ObtenerPorCorreo(string correoElectronico)
        {
            DataTable tabla = EjecutorStoredProcedure.Leer(
                "sp_UsuarioExterno_ObtenerPorCorreo",
                new SqlParameter("@correoElectronico", correoElectronico));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public UsuarioExterno ObtenerPorId(int idUsuarioExterno)
        {
            DataTable tabla = EjecutorStoredProcedure.Leer(
                "sp_UsuarioExterno_ObtenerPorId",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void ActivarCuenta(int idUsuarioExterno, string estadoCuenta)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_UsuarioExterno_ActivarCuenta",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno),
                new SqlParameter("@estadoCuenta", estadoCuenta));
        }

        public void ActualizarPassword(int idUsuarioExterno, string passwordHash)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_UsuarioExterno_ActualizarPassword",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno),
                new SqlParameter("@passwordHash", passwordHash));
        }

        public void ActualizarPerfil(int idUsuarioExterno, string perfilUsuario)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_UsuarioExterno_ActualizarPerfil",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno),
                new SqlParameter("@perfilUsuario", perfilUsuario));
        }

        public List<UsuarioExterno> ListarPorPerfil(string perfilUsuario)
        {
            DataTable tabla = EjecutorStoredProcedure.Leer(
                "sp_UsuarioExterno_ListarPorPerfil",
                new SqlParameter("@perfilUsuario", perfilUsuario));

            var lista = new List<UsuarioExterno>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        private static UsuarioExterno MapearDesdeFila(DataRow fila)
        {
            return new UsuarioExterno
            {
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                Nombre = fila["nombre"].ToString(),
                Apellido = fila["apellido"].ToString(),
                CorreoElectronico = fila["correoElectronico"].ToString(),
                PasswordHash = fila["passwordHash"].ToString(),
                Telefono = fila["telefono"] == DBNull.Value ? null : fila["telefono"].ToString(),
                EstadoCuenta = fila["estadoCuenta"].ToString(),
                PerfilUsuario = fila["perfilUsuario"].ToString(),
                AceptaTerminos = Convert.ToBoolean(fila["aceptaTerminos"]),
                AceptaPoliticaPrivacidad = Convert.ToBoolean(fila["aceptaPoliticaPrivacidad"]),
                FechaAceptacionTerminos = fila["fechaAceptacionTerminos"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaAceptacionTerminos"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaActivacion = fila["fechaActivacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaActivacion"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"])
            };
        }
    }
}
