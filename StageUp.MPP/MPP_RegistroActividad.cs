using System;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RegistroActividad
    {
        private readonly DAL_RegistroActividad _dal = new DAL_RegistroActividad();

        public void Insertar(RegistroActividad registro)
        {
            _dal.Insertar(
                registro.IdUsuarioExternoResponsable,
                registro.IdUsuarioInternoResponsable,
                registro.TipoOperacion,
                registro.TipoEntidadAfectada,
                registro.IdEntidadAfectada,
                registro.DescripcionOperacion,
                registro.OrigenOperacion);
        }

        public List<RegistroActividad> Buscar(
            int? idUsuarioExternoResponsable, DateTime? fechaDesde, DateTime? fechaHasta,
            string tipoOperacion, string tipoEntidadAfectada)
        {
            DataTable tabla = _dal.Buscar(idUsuarioExternoResponsable, fechaDesde, fechaHasta, tipoOperacion, tipoEntidadAfectada);

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
