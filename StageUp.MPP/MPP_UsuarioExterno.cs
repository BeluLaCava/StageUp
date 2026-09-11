using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_UsuarioExterno
    {
        public static bool PerfilCompletoHabilitado
        {
            get { return string.Equals(ConfigurationManager.AppSettings["PerfilCompletoHabilitado"], "true", StringComparison.OrdinalIgnoreCase); }
        }

        public int Insertar(UsuarioExterno oUsuarioExterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioExterno_Insertar",
                new Hashtable
                {
                    { "@nombre", oUsuarioExterno.Nombre },
                    { "@apellido", oUsuarioExterno.Apellido },
                    { "@correoElectronico", oUsuarioExterno.CorreoElectronico },
                    { "@passwordHash", oUsuarioExterno.PasswordHash },
                    { "@telefono", (object)oUsuarioExterno.Telefono ?? DBNull.Value },
                    { "@estadoCuenta", oUsuarioExterno.EstadoCuenta },
                    { "@perfilUsuario", oUsuarioExterno.PerfilUsuario },
                    { "@aceptaTerminos", oUsuarioExterno.AceptaTerminos },
                    { "@aceptaPoliticaPrivacidad", oUsuarioExterno.AceptaPoliticaPrivacidad },
                    { "@fechaAceptacionTerminos", (object)oUsuarioExterno.FechaAceptacionTerminos ?? DBNull.Value }
                });

            return Convert.ToInt32(resultado);
        }

        public UsuarioExterno ObtenerPorCorreo(UsuarioExterno oUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioExterno_ObtenerPorCorreo",
                new Hashtable { { "@correoElectronico", oUsuarioExterno.CorreoElectronico } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public UsuarioExterno ObtenerPorId(UsuarioExterno oUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioExterno_ObtenerPorId",
                new Hashtable { { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public UsuarioExterno ObtenerPerfilPorId(UsuarioExterno oUsuarioExterno)
        {
            if (!PerfilCompletoHabilitado)
            {
                return ObtenerPorId(oUsuarioExterno);
            }

            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioExterno_ObtenerPerfilPorIdV2",
                new Hashtable { { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void ActivarCuenta(UsuarioExterno oUsuarioExterno)
        {
            Conexion.Instance.Guardar(
                "sp_UsuarioExterno_ActivarCuenta",
                new Hashtable
                {
                    { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno },
                    { "@estadoCuenta", oUsuarioExterno.EstadoCuenta }
                });
        }

        public void ActualizarPassword(UsuarioExterno oUsuarioExterno)
        {
            Conexion.Instance.Guardar(
                "sp_UsuarioExterno_ActualizarPassword",
                new Hashtable
                {
                    { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno },
                    { "@passwordHash", oUsuarioExterno.PasswordHash }
                });
        }

        public void ActualizarPerfil(UsuarioExterno oUsuarioExterno)
        {
            Conexion.Instance.Guardar(
                "sp_UsuarioExterno_ActualizarPerfil",
                new Hashtable
                {
                    { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno },
                    { "@perfilUsuario", oUsuarioExterno.PerfilUsuario }
                });
        }

        public void ActualizarDatosPersonales(UsuarioExterno oUsuarioExterno)
        {
            if (!PerfilCompletoHabilitado)
            {
                throw new InvalidOperationException("La edición completa del perfil todavía no está habilitada.");
            }

            Conexion.Instance.Guardar(
                "sp_UsuarioExterno_ActualizarDatosPerfilV2",
                new Hashtable
                {
                    { "@idUsuarioExterno", oUsuarioExterno.IdUsuarioExterno },
                    { "@nombre", oUsuarioExterno.Nombre },
                    { "@apellido", oUsuarioExterno.Apellido },
                    { "@correoElectronico", oUsuarioExterno.CorreoElectronico },
                    { "@fotoPerfilRuta", (object)oUsuarioExterno.FotoPerfilRuta ?? DBNull.Value },
                    { "@descripcionPerfil", (object)oUsuarioExterno.DescripcionPerfil ?? DBNull.Value }
                });
        }

        public List<UsuarioExterno> ListarPorPerfil(UsuarioExterno oUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioExterno_ListarPorPerfil",
                new Hashtable { { "@perfilUsuario", oUsuarioExterno.PerfilUsuario } });

            List<UsuarioExterno> lista = new List<UsuarioExterno>();
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
                FotoPerfilRuta = LeerTextoOpcional(fila, "fotoPerfilRuta"),
                DescripcionPerfil = LeerTextoOpcional(fila, "descripcionPerfil"),
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

        private static string LeerTextoOpcional(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value
                ? fila[columna].ToString()
                : null;
        }
    }
}
