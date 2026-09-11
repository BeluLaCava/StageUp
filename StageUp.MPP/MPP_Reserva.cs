using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Reserva
    {
        public int Insertar(Reserva oReserva)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Reserva_Insertar",
                new Hashtable
                {
                    { "@idEspacioArtistico", oReserva.IdEspacioArtistico },
                    { "@idUsuarioExternoSolicitante", oReserva.IdUsuarioExternoSolicitante },
                    { "@fechaSolicitada", oReserva.FechaSolicitada },
                    { "@comentarioSolicitante", (object)oReserva.ComentarioSolicitante ?? DBNull.Value },
                    { "@minutoDesde", (object)oReserva.MinutoDesde ?? DBNull.Value },
                    { "@minutoHasta", (object)oReserva.MinutoHasta ?? DBNull.Value },
                    { "@precioHoraPactado", (object)oReserva.PrecioHoraPactado ?? DBNull.Value },
                    { "@moneda", (object)oReserva.Moneda ?? DBNull.Value },
                    { "@importeEstimado", (object)oReserva.ImporteEstimado ?? DBNull.Value }
                });

            return Convert.ToInt32(resultado);
        }

        public bool ExisteSolapamiento(Reserva oReserva)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Reserva_ExisteSolapamiento",
                new Hashtable
                {
                    { "@idEspacioArtistico", oReserva.IdEspacioArtistico },
                    { "@fechaSolicitada", oReserva.FechaSolicitada.Date },
                    { "@minutoDesde", (object)oReserva.MinutoDesde ?? DBNull.Value },
                    { "@minutoHasta", (object)oReserva.MinutoHasta ?? DBNull.Value },
                    { "@idReservaAExcluir", oReserva.IdReserva > 0 ? (object)oReserva.IdReserva : DBNull.Value }
                });

            return Convert.ToBoolean(resultado);
        }

        public bool AceptarSiDisponible(Reserva oReserva)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Reserva_AceptarSiDisponible",
                new Hashtable
                {
                    { "@idReserva", oReserva.IdReserva },
                    { "@comentarioResolucion", (object)oReserva.ComentarioResolucion ?? DBNull.Value }
                });

            return Convert.ToBoolean(resultado);
        }

        public List<Reserva> ListarPorSolicitante(UsuarioExterno oUsuarioExterno)
        {
            return MapearDesdeTabla(Conexion.Instance.Leer(
                "sp_Reserva_ListarPorSolicitante",
                new Hashtable { { "@idUsuarioExternoSolicitante", oUsuarioExterno.IdUsuarioExterno } }));
        }

        public List<Reserva> ListarPorGestor(UsuarioExterno oUsuarioExterno)
        {
            return MapearDesdeTabla(Conexion.Instance.Leer(
                "sp_Reserva_ListarPorGestor",
                new Hashtable { { "@idUsuarioGestor", oUsuarioExterno.IdUsuarioExterno } }));
        }

        public Reserva ObtenerPorId(Reserva oReserva)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Reserva_ObtenerPorId",
                new Hashtable { { "@idReserva", oReserva.IdReserva } });
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void Resolver(Reserva oReserva)
        {
            Conexion.Instance.Guardar(
                "sp_Reserva_Resolver",
                new Hashtable
                {
                    { "@idReserva", oReserva.IdReserva },
                    { "@estadoReserva", oReserva.EstadoReserva },
                    { "@comentarioResolucion", (object)oReserva.ComentarioResolucion ?? DBNull.Value }
                });
        }

        public void Cancelar(Reserva oReserva)
        {
            Conexion.Instance.Guardar(
                "sp_Reserva_Cancelar",
                new Hashtable
                {
                    { "@idReserva", oReserva.IdReserva },
                    { "@comisionAplicada", oReserva.ComisionAplicada },
                    { "@importeComision", (object)oReserva.ImporteComision ?? DBNull.Value }
                });
        }

        public void FinalizarVencidas()
        {
            Conexion.Instance.Guardar("sp_Reserva_FinalizarVencidas");
        }

        private static List<Reserva> MapearDesdeTabla(DataTable tabla)
        {
            List<Reserva> lista = new List<Reserva>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        private static Reserva MapearDesdeFila(DataRow fila)
        {
            Reserva reserva = new Reserva
            {
                IdReserva = Convert.ToInt32(fila["idReserva"]),
                IdEspacioArtistico = Convert.ToInt32(fila["idEspacioArtistico"]),
                IdUsuarioExternoSolicitante = Convert.ToInt32(fila["idUsuarioExternoSolicitante"]),
                FechaSolicitada = Convert.ToDateTime(fila["fechaSolicitada"]),
                ComentarioSolicitante = fila["comentarioSolicitante"] == DBNull.Value ? null : fila["comentarioSolicitante"].ToString(),
                EstadoReserva = fila["estadoReserva"].ToString(),
                ComentarioResolucion = fila["comentarioResolucion"] == DBNull.Value ? null : fila["comentarioResolucion"].ToString(),
                FechaCreacion = Convert.ToDateTime(fila["fechaCreacion"]),
                FechaResolucion = fila["fechaResolucion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaResolucion"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                FechaFinalizacion = fila.Table.Columns.Contains("fechaFinalizacion") && fila["fechaFinalizacion"] != DBNull.Value
                    ? Convert.ToDateTime(fila["fechaFinalizacion"]) : (DateTime?)null,
                MinutoDesde = fila.Table.Columns.Contains("minutoDesde") && fila["minutoDesde"] != DBNull.Value ? Convert.ToInt32(fila["minutoDesde"]) : (int?)null,
                MinutoHasta = fila.Table.Columns.Contains("minutoHasta") && fila["minutoHasta"] != DBNull.Value ? Convert.ToInt32(fila["minutoHasta"]) : (int?)null,
                PrecioHoraPactado = fila.Table.Columns.Contains("precioHoraPactado") && fila["precioHoraPactado"] != DBNull.Value ? Convert.ToDecimal(fila["precioHoraPactado"]) : (decimal?)null,
                Moneda = fila.Table.Columns.Contains("moneda") && fila["moneda"] != DBNull.Value ? fila["moneda"].ToString() : null,
                ImporteEstimado = fila.Table.Columns.Contains("importeEstimado") && fila["importeEstimado"] != DBNull.Value ? Convert.ToDecimal(fila["importeEstimado"]) : (decimal?)null,
                ComisionAplicada = fila.Table.Columns.Contains("comisionAplicada") && Convert.ToBoolean(fila["comisionAplicada"]),
                ImporteComision = fila.Table.Columns.Contains("importeComision") && fila["importeComision"] != DBNull.Value ? Convert.ToDecimal(fila["importeComision"]) : (decimal?)null,
                FechaCancelacion = fila.Table.Columns.Contains("fechaCancelacion") && fila["fechaCancelacion"] != DBNull.Value ? Convert.ToDateTime(fila["fechaCancelacion"]) : (DateTime?)null,
                NombreEspacio = fila.Table.Columns.Contains("nombreEspacio") && fila["nombreEspacio"] != DBNull.Value ? fila["nombreEspacio"].ToString() : null,
                IdUsuarioGestor = fila.Table.Columns.Contains("idUsuarioGestor") ? Convert.ToInt32(fila["idUsuarioGestor"]) : 0,
                PromedioCalificacionSolicitante = fila.Table.Columns.Contains("promedioCalificacionSolicitante") && fila["promedioCalificacionSolicitante"] != DBNull.Value
                    ? Convert.ToDecimal(fila["promedioCalificacionSolicitante"]) : 0m,
                CantidadCalificacionesSolicitante = fila.Table.Columns.Contains("cantidadCalificacionesSolicitante")
                    ? Convert.ToInt32(fila["cantidadCalificacionesSolicitante"]) : 0,
                CalificacionEspacioRealizada = fila.Table.Columns.Contains("calificacionEspacioRealizada") && Convert.ToBoolean(fila["calificacionEspacioRealizada"]),
                CalificacionSolicitanteRealizada = fila.Table.Columns.Contains("calificacionSolicitanteRealizada") && Convert.ToBoolean(fila["calificacionSolicitanteRealizada"])
            };

            if (fila.Table.Columns.Contains("nombreSolicitante") && fila["nombreSolicitante"] != DBNull.Value)
            {
                reserva.NombreSolicitante = fila["nombreSolicitante"].ToString();
            }

            if (fila.Table.Columns.Contains("correoSolicitante") && fila["correoSolicitante"] != DBNull.Value)
            {
                reserva.CorreoSolicitante = fila["correoSolicitante"].ToString();
            }

            return reserva;
        }
    }
}
