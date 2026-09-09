using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Bitacora
    {
        public void Insertar(RegistroActividad registro)
        {
            Conexion.Instance.Guardar(
                "sp_RegistroActividad_Insertar",
                new SqlParameter("@idUsuarioExternoResponsable", (object)registro.IdUsuarioExternoResponsable ?? DBNull.Value),
                new SqlParameter("@idUsuarioInternoResponsable", (object)registro.IdUsuarioInternoResponsable ?? DBNull.Value),
                new SqlParameter("@tipoOperacion", registro.TipoOperacion),
                new SqlParameter("@tipoEntidadAfectada", registro.TipoEntidadAfectada),
                new SqlParameter("@idEntidadAfectada", (object)registro.IdEntidadAfectada ?? DBNull.Value),
                new SqlParameter("@descripcionOperacion", (object)registro.DescripcionOperacion ?? DBNull.Value),
                new SqlParameter("@origenOperacion", (object)registro.OrigenOperacion ?? DBNull.Value));
        }

        public List<RegistroActividad> Buscar(
            int? idUsuarioExternoResponsable, DateTime? fechaDesde, DateTime? fechaHasta,
            string tipoOperacion, string tipoEntidadAfectada)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RegistroActividad_Buscar",
                new SqlParameter("@idUsuarioExternoResponsable", (object)idUsuarioExternoResponsable ?? DBNull.Value),
                new SqlParameter("@fechaDesde", (object)fechaDesde ?? DBNull.Value),
                new SqlParameter("@fechaHasta", (object)fechaHasta ?? DBNull.Value),
                new SqlParameter("@tipoOperacion", (object)tipoOperacion ?? DBNull.Value),
                new SqlParameter("@tipoEntidadAfectada", (object)tipoEntidadAfectada ?? DBNull.Value));

            var lista = new List<RegistroActividad>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new RegistroActividad
                {
                    IdRegistroActividad = Convert.ToInt32(fila["idRegistroActividad"]),
                    IdUsuarioExternoResponsable = fila["idUsuarioExternoResponsable"] == DBNull.Value
                        ? (int?)null : Convert.ToInt32(fila["idUsuarioExternoResponsable"]),
                    IdUsuarioInternoResponsable = fila["idUsuarioInternoResponsable"] == DBNull.Value
                        ? (int?)null : Convert.ToInt32(fila["idUsuarioInternoResponsable"]),
                    TipoOperacion = fila["tipoOperacion"].ToString(),
                    TipoEntidadAfectada = fila["tipoEntidadAfectada"].ToString(),
                    IdEntidadAfectada = fila["idEntidadAfectada"] == DBNull.Value
                        ? (int?)null : Convert.ToInt32(fila["idEntidadAfectada"]),
                    DescripcionOperacion = fila["descripcionOperacion"] == DBNull.Value
                        ? null : fila["descripcionOperacion"].ToString(),
                    FechaOperacion = Convert.ToDateTime(fila["fechaOperacion"]),
                    OrigenOperacion = fila["origenOperacion"] == DBNull.Value
                        ? null : fila["origenOperacion"].ToString(),
                    NombreResponsable = fila["nombreResponsable"] == DBNull.Value
                        ? null : fila["nombreResponsable"].ToString(),
                    CorreoResponsable = fila["correoResponsable"] == DBNull.Value
                        ? null : fila["correoResponsable"].ToString()
                });
            }
            return lista;
        }
    }
}
