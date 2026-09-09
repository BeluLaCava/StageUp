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
                new SqlParameter("@comentarioSolicitante", (object)reserva.ComentarioSolicitante ?? DBNull.Value));

            return Convert.ToInt32(resultado);
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

        public void Cancelar(int idReserva)
        {
            Conexion.Instance.Guardar(
                "sp_Reserva_Cancelar",
                new SqlParameter("@idReserva", idReserva));
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
                NombreEspacio = fila.Table.Columns.Contains("nombreEspacio") && fila["nombreEspacio"] != DBNull.Value ? fila["nombreEspacio"].ToString() : null,
                IdUsuarioGestor = fila.Table.Columns.Contains("idUsuarioGestor") ? Convert.ToInt32(fila["idUsuarioGestor"]) : 0
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
