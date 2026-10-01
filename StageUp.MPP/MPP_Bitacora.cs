using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Bitacora
    {
        public void Insertar(RegistroActividad oRegistroActividad)
        {
            Conexion.Instance.Guardar(
                "sp_RegistroActividad_Insertar",
                new Hashtable
                {
                    { "@idUsuarioExternoResponsable", (object)oRegistroActividad.IdUsuarioExternoResponsable ?? DBNull.Value },
                    { "@idUsuarioInternoResponsable", (object)oRegistroActividad.IdUsuarioInternoResponsable ?? DBNull.Value },
                    { "@tipoOperacion", oRegistroActividad.TipoOperacion },
                    { "@tipoEntidadAfectada", oRegistroActividad.TipoEntidadAfectada },
                    { "@idEntidadAfectada", (object)oRegistroActividad.IdEntidadAfectada ?? DBNull.Value },
                    { "@descripcionOperacion", (object)oRegistroActividad.DescripcionOperacion ?? DBNull.Value },
                    { "@origenOperacion", (object)oRegistroActividad.OrigenOperacion ?? DBNull.Value }
                });
        }

        public List<RegistroActividad> Buscar(FiltroRegistroActividad oFiltroRegistroActividad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RegistroActividad_Buscar",
                new Hashtable
                {
                    { "@idUsuarioExternoResponsable", (object)oFiltroRegistroActividad.IdUsuarioExternoResponsable ?? DBNull.Value },
                    { "@fechaDesde", (object)oFiltroRegistroActividad.FechaDesde ?? DBNull.Value },
                    { "@fechaHasta", (object)oFiltroRegistroActividad.FechaHasta ?? DBNull.Value },
                    { "@tipoOperacion", (object)oFiltroRegistroActividad.TipoOperacion ?? DBNull.Value },
                    { "@tipoEntidadAfectada", (object)oFiltroRegistroActividad.TipoEntidadAfectada ?? DBNull.Value },
                    { "@idUsuarioInternoResponsable", (object)oFiltroRegistroActividad.IdUsuarioInternoResponsable ?? DBNull.Value },
                    { "@textoResponsable", string.IsNullOrWhiteSpace(oFiltroRegistroActividad.TextoResponsable) ? (object)DBNull.Value : oFiltroRegistroActividad.TextoResponsable.Trim() },
                    { "@tipoResponsable", string.IsNullOrEmpty(oFiltroRegistroActividad.TipoResponsable) ? (object)DBNull.Value : oFiltroRegistroActividad.TipoResponsable },
                    { "@maximo", oFiltroRegistroActividad.Maximo > 0 ? oFiltroRegistroActividad.Maximo : 500 }
                });

            return Mapear(tabla);
        }

        public RegistroActividad ObtenerPorId(RegistroActividad oRegistro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_RegistroActividad_ObtenerPorId",
                new Hashtable { { "@idRegistroActividad", oRegistro.IdRegistroActividad } });
            List<RegistroActividad> lista = Mapear(tabla);
            return lista.Count == 0 ? null : lista[0];
        }

        private static List<RegistroActividad> Mapear(DataTable tabla)
        {
            List<RegistroActividad> lista = new List<RegistroActividad>();
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
