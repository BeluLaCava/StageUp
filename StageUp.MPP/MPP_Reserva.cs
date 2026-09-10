using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Reserva
    {
        public int Insertar(Reserva reserva)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Reserva_Insertar",
                new SqlParameter("@idEspacioArtistico", reserva.IdEspacioArtistico),
                new SqlParameter("@idUsuarioExternoSolicitante", reserva.IdUsuarioExternoSolicitante),
                new SqlParameter("@fechaSolicitada", reserva.FechaSolicitada),
                new SqlParameter("@comentarioSolicitante", (object)reserva.ComentarioSolicitante ?? DBNull.Value),
                new SqlParameter("@minutoDesde", (object)reserva.MinutoDesde ?? DBNull.Value),
                new SqlParameter("@minutoHasta", (object)reserva.MinutoHasta ?? DBNull.Value),
                new SqlParameter("@precioHoraPactado", (object)reserva.PrecioHoraPactado ?? DBNull.Value),
                new SqlParameter("@moneda", (object)reserva.Moneda ?? DBNull.Value),
                new SqlParameter("@importeEstimado", (object)reserva.ImporteEstimado ?? DBNull.Value));

            return Convert.ToInt32(resultado);
        }

        // Ítem 2: si idReservaAExcluir viene null, se usa al solicitar (aviso
        // temprano); con el id de la propia reserva, se usa al revalidar en el
        // momento de aceptar (ver AceptarSiDisponible).
        public bool ExisteSolapamiento(
            int idEspacioArtistico, DateTime fechaSolicitada, int minutoDesde, int minutoHasta, int? idReservaAExcluir = null)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Reserva_ExisteSolapamiento",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico),
                new SqlParameter("@fechaSolicitada", fechaSolicitada.Date),
                new SqlParameter("@minutoDesde", minutoDesde),
                new SqlParameter("@minutoHasta", minutoHasta),
                new SqlParameter("@idReservaAExcluir", (object)idReservaAExcluir ?? DBNull.Value));

            return Convert.ToBoolean(resultado);
        }

        // Devuelve true si la aceptación se aplicó de verdad (seguía Pendiente y,
        // si tenía horario, seguía libre). false significa que había dejado de
        // estar disponible (otra reserva se aceptó primero para ese horario, o ya
        // no estaba Pendiente) — la BLL decide qué mensaje mostrar en ese caso.
        public bool AceptarSiDisponible(int idReserva, string comentarioResolucion)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Reserva_AceptarSiDisponible",
                new SqlParameter("@idReserva", idReserva),
                new SqlParameter("@comentarioResolucion", (object)comentarioResolucion ?? DBNull.Value));

            return Convert.ToBoolean(resultado);
        }

        public List<Reserva> ListarPorSolicitante(int idUsuarioExternoSolicitante)
        {
            return MapearDesdeTabla(Conexion.Instance.Leer(
                "sp_Reserva_ListarPorSolicitante",
                new SqlParameter("@idUsuarioExternoSolicitante", idUsuarioExternoSolicitante)));
        }

        public List<Reserva> ListarPorGestor(int idUsuarioGestor)
        {
            return MapearDesdeTabla(Conexion.Instance.Leer(
                "sp_Reserva_ListarPorGestor",
                new SqlParameter("@idUsuarioGestor", idUsuarioGestor)));
        }

        public Reserva ObtenerPorId(int idReserva)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Reserva_ObtenerPorId",
                new SqlParameter("@idReserva", idReserva));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public void Resolver(int idReserva, string estadoReserva, string comentarioResolucion)
        {
            Conexion.Instance.Guardar(
                "sp_Reserva_Resolver",
                new SqlParameter("@idReserva", idReserva),
                new SqlParameter("@estadoReserva", estadoReserva),
                new SqlParameter("@comentarioResolucion", (object)comentarioResolucion ?? DBNull.Value));
        }

        public void Cancelar(int idReserva, bool comisionAplicada, decimal? importeComision)
        {
            Conexion.Instance.Guardar(
                "sp_Reserva_Cancelar",
                new SqlParameter("@idReserva", idReserva),
                new SqlParameter("@comisionAplicada", comisionAplicada),
                new SqlParameter("@importeComision", (object)importeComision ?? DBNull.Value));
        }

        public void FinalizarVencidas()
        {
            Conexion.Instance.Guardar("sp_Reserva_FinalizarVencidas");
        }

        private static List<Reserva> MapearDesdeTabla(DataTable tabla)
        {
            var lista = new List<Reserva>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        private static Reserva MapearDesdeFila(DataRow fila)
        {
            var reserva = new Reserva
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
