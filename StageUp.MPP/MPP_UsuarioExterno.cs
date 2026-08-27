using System;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    /// <summary>
    /// Mapeo entre la entidad de negocio UsuarioExterno y su persistencia
    /// (a través de DAL_UsuarioExterno). La BLL solo conoce esta clase,
    /// nunca a DAL_UsuarioExterno directamente.
    /// </summary>
    public class MPP_UsuarioExterno
    {
        private readonly DAL_UsuarioExterno _dal = new DAL_UsuarioExterno();

        public int Insertar(UsuarioExterno usuario)
        {
            return _dal.Insertar(
                usuario.Nombre,
                usuario.Apellido,
                usuario.CorreoElectronico,
                usuario.PasswordHash,
                usuario.Telefono,
                usuario.EstadoCuenta,
                usuario.PerfilUsuario,
                usuario.AceptaTerminos,
                usuario.AceptaPoliticaPrivacidad,
                usuario.FechaAceptacionTerminos);
        }

        public UsuarioExterno ObtenerPorCorreo(string correoElectronico)
        {
            DataTable tabla = _dal.ObtenerPorCorreo(correoElectronico);
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public UsuarioExterno ObtenerPorId(int idUsuarioExterno)
        {
            DataTable tabla = _dal.ObtenerPorId(idUsuarioExterno);
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void ActivarCuenta(int idUsuarioExterno, string estadoCuenta)
        {
            _dal.ActivarCuenta(idUsuarioExterno, estadoCuenta);
        }

        public void ActualizarPassword(int idUsuarioExterno, string passwordHash)
        {
            _dal.ActualizarPassword(idUsuarioExterno, passwordHash);
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
