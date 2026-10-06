using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-001-007 (A1 a A6): solicitudes de habilitación como gestor (script 54).
    public class MPP_SolicitudHabilitacionGestor
    {
        // Devuelve el resultado del SP (OK, YA_PENDIENTE, YA_ES_GESTOR,
        // CUENTA_NO_ACTIVA, NO_EXISTE) y deja el id en oSolicitud.IdSolicitud.
        public string Registrar(SolicitudHabilitacionGestor oSolicitud)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SolicitudHabilitacion_Registrar",
                new Hashtable
                {
                    { "@idUsuarioExterno", oSolicitud.IdUsuarioExterno },
                    { "@nombreResponsable", oSolicitud.NombreResponsable },
                    { "@documentoResponsable", oSolicitud.DocumentoResponsable },
                    { "@telefonoContacto", oSolicitud.TelefonoContacto },
                    { "@correoContacto", oSolicitud.CorreoContacto },
                    { "@condicionFiscal", oSolicitud.CondicionFiscal },
                    { "@razonSocial", (object)oSolicitud.RazonSocial ?? DBNull.Value },
                    { "@cuit", (object)oSolicitud.Cuit ?? DBNull.Value },
                    { "@nombreEspacio", oSolicitud.NombreEspacio },
                    { "@tipoEspacio", oSolicitud.TipoEspacio },
                    { "@provincia", oSolicitud.Provincia },
                    { "@ciudad", oSolicitud.Ciudad },
                    { "@descripcionPropuesta", oSolicitud.DescripcionPropuesta }
                });

            if (tabla.Rows.Count == 0)
            {
                return "NO_EXISTE";
            }

            DataRow fila = tabla.Rows[0];
            if (fila["idSolicitud"] != DBNull.Value)
            {
                oSolicitud.IdSolicitud = Convert.ToInt32(fila["idSolicitud"]);
            }

            return Convert.ToString(fila["resultado"]);
        }

        public SolicitudHabilitacionGestor ObtenerUltimaPorUsuario(UsuarioExterno oUsuario)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SolicitudHabilitacion_ObtenerUltimaPorUsuario",
                new Hashtable { { "@idUsuarioExterno", oUsuario.IdUsuarioExterno } });
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public List<SolicitudHabilitacionGestor> Listar(SolicitudHabilitacionGestor oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SolicitudHabilitacion_Listar",
                new Hashtable { { "@estado", string.IsNullOrEmpty(oFiltro.Estado) ? (object)DBNull.Value : oFiltro.Estado } });

            List<SolicitudHabilitacionGestor> lista = new List<SolicitudHabilitacionGestor>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }

            return lista;
        }

        // Devuelve el resultado del SP (OK, YA_RESUELTA, NO_EXISTE) y deja el
        // usuario de la solicitud en oSolicitud.IdUsuarioExterno.
        public string Resolver(SolicitudHabilitacionGestor oSolicitud, bool aprobar)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SolicitudHabilitacion_Resolver",
                new Hashtable
                {
                    { "@idSolicitud", oSolicitud.IdSolicitud },
                    { "@aprobar", aprobar },
                    { "@motivoRechazo", (object)oSolicitud.MotivoRechazo ?? DBNull.Value },
                    { "@idUsuarioInternoRevisor", oSolicitud.IdUsuarioInternoRevisor.Value }
                });

            if (tabla.Rows.Count == 0)
            {
                return "NO_EXISTE";
            }

            DataRow fila = tabla.Rows[0];
            if (fila["idUsuarioExterno"] != DBNull.Value)
            {
                oSolicitud.IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]);
            }

            return Convert.ToString(fila["resultado"]);
        }

        private static SolicitudHabilitacionGestor Mapear(DataRow fila)
        {
            return new SolicitudHabilitacionGestor
            {
                IdSolicitud = Convert.ToInt32(fila["idSolicitud"]),
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                Estado = Convert.ToString(fila["estado"]),
                NombreResponsable = Texto(fila, "nombreResponsable"),
                DocumentoResponsable = Texto(fila, "documentoResponsable"),
                TelefonoContacto = Texto(fila, "telefonoContacto"),
                CorreoContacto = Texto(fila, "correoContacto"),
                CondicionFiscal = Texto(fila, "condicionFiscal"),
                RazonSocial = Texto(fila, "razonSocial"),
                Cuit = Texto(fila, "cuit"),
                NombreEspacio = Texto(fila, "nombreEspacio"),
                TipoEspacio = Texto(fila, "tipoEspacio"),
                Provincia = Texto(fila, "provincia"),
                Ciudad = Texto(fila, "ciudad"),
                DescripcionPropuesta = Texto(fila, "descripcionPropuesta"),
                FechaSolicitud = Convert.ToDateTime(fila["fechaSolicitud"]),
                FechaRevision = fila["fechaRevision"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaRevision"]),
                IdUsuarioInternoRevisor = fila["idUsuarioInternoRevisor"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idUsuarioInternoRevisor"]),
                MotivoRechazo = Texto(fila, "motivoRechazo"),
                NombreUsuario = Texto(fila, "nombreUsuario"),
                CorreoUsuario = Texto(fila, "correoUsuario"),
                NombreRevisor = Texto(fila, "nombreRevisor")
            };
        }

        private static string Texto(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value ? fila[columna].ToString() : null;
        }
    }
}
