using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Calificacion
    {
        public int Insertar(Calificacion calificacion)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Calificacion_Insertar",
                new SqlParameter("@idReserva", calificacion.IdReserva),
                new SqlParameter("@idUsuarioAutor", calificacion.IdUsuarioAutor),
                new SqlParameter("@tipoCalificacion", calificacion.TipoCalificacion),
                new SqlParameter("@puntaje", calificacion.Puntaje),
                new SqlParameter("@comentario", calificacion.Comentario));

            return Convert.ToInt32(resultado);
        }

        public bool Existe(int idReserva, string tipoCalificacion)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Calificacion_Existe",
                new SqlParameter("@idReserva", idReserva),
                new SqlParameter("@tipoCalificacion", tipoCalificacion));

            return Convert.ToBoolean(resultado);
        }

        public List<Calificacion> ListarPorEspacio(int idEspacioArtistico)
        {
            return MapearLista(Conexion.Instance.Leer(
                "sp_Calificacion_ListarPorEspacio",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico)));
        }

        public List<Calificacion> ListarRecibidasPorUsuario(int idUsuarioExterno)
        {
            return MapearLista(Conexion.Instance.Leer(
                "sp_Calificacion_ListarRecibidasPorUsuario",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno)));
        }

        public List<Calificacion> ListarRealizadasPorUsuario(int idUsuarioExterno)
        {
            return MapearLista(Conexion.Instance.Leer(
                "sp_Calificacion_ListarRealizadasPorUsuario",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno)));
        }

        public ResumenReputacion ObtenerResumenEspacio(int idEspacioArtistico)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Calificacion_ResumenPorEspacio",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
            return tabla.Rows.Count == 0
                ? new ResumenReputacion { IdReferencia = idEspacioArtistico }
                : MapearResumen(tabla.Rows[0], "idEspacioArtistico");
        }

        public ResumenReputacion ObtenerResumenUsuario(int idUsuarioExterno)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Calificacion_ResumenPorUsuario",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno));
            return tabla.Rows.Count == 0
                ? new ResumenReputacion { IdReferencia = idUsuarioExterno }
                : MapearResumen(tabla.Rows[0], "idUsuarioExterno");
        }

        public Dictionary<int, ResumenReputacion> ListarResumenesEspacios()
        {
            return MapearResumenes(
                Conexion.Instance.Leer("sp_Calificacion_ListarResumenesEspacios"),
                "idEspacioArtistico");
        }

        public Dictionary<int, ResumenReputacion> ListarResumenesUsuarios()
        {
            return MapearResumenes(
                Conexion.Instance.Leer("sp_Calificacion_ListarResumenesUsuarios"),
                "idUsuarioExterno");
        }

        private static List<Calificacion> MapearLista(DataTable tabla)
        {
            var lista = new List<Calificacion>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new Calificacion
                {
                    IdCalificacion = Convert.ToInt32(fila["idCalificacion"]),
                    IdReserva = Convert.ToInt32(fila["idReserva"]),
                    IdUsuarioAutor = Convert.ToInt32(fila["idUsuarioAutor"]),
                    TipoCalificacion = fila["tipoCalificacion"].ToString(),
                    Puntaje = Convert.ToInt32(fila["puntaje"]),
                    Comentario = fila["comentario"].ToString(),
                    FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                    Activo = Convert.ToBoolean(fila["activo"]),
                    IdEspacioArtistico = Convert.ToInt32(fila["idEspacioArtistico"]),
                    IdUsuarioEvaluado = fila["idUsuarioEvaluado"] == DBNull.Value
                        ? (int?)null : Convert.ToInt32(fila["idUsuarioEvaluado"]),
                    NombreEspacio = fila["nombreEspacio"].ToString(),
                    NombreAutor = fila["nombreAutor"].ToString(),
                    NombreEvaluado = fila["nombreEvaluado"].ToString(),
                    FechaReserva = Convert.ToDateTime(fila["fechaReserva"])
                });
            }
            return lista;
        }

        private static Dictionary<int, ResumenReputacion> MapearResumenes(DataTable tabla, string columnaId)
        {
            var resumenes = new Dictionary<int, ResumenReputacion>();
            foreach (DataRow fila in tabla.Rows)
            {
                ResumenReputacion resumen = MapearResumen(fila, columnaId);
                resumenes[resumen.IdReferencia] = resumen;
            }
            return resumenes;
        }

        private static ResumenReputacion MapearResumen(DataRow fila, string columnaId)
        {
            return new ResumenReputacion
            {
                IdReferencia = Convert.ToInt32(fila[columnaId]),
                Promedio = fila["promedio"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["promedio"]),
                CantidadCalificaciones = Convert.ToInt32(fila["cantidadCalificaciones"])
            };
        }
    }
}
