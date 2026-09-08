using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_EspacioArtistico
    {
        public int Insertar(EspacioArtistico espacio)
        {
            object resultado = EjecutorStoredProcedure.LeerEscalar(
                "sp_EspacioArtistico_Insertar",
                new SqlParameter("@idUsuarioGestor", espacio.IdUsuarioGestor),
                new SqlParameter("@nombreEspacio", espacio.NombreEspacio),
                new SqlParameter("@descripcion", (object)espacio.Descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", espacio.TipoEspacio));

            return Convert.ToInt32(resultado);
        }

        public void Modificar(EspacioArtistico espacio)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_EspacioArtistico_Modificar",
                new SqlParameter("@idEspacioArtistico", espacio.IdEspacioArtistico),
                new SqlParameter("@nombreEspacio", espacio.NombreEspacio),
                new SqlParameter("@descripcion", (object)espacio.Descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", espacio.TipoEspacio));
        }

        public EspacioArtistico ObtenerPorId(int idEspacioArtistico)
        {
            DataTable tabla = EjecutorStoredProcedure.Leer(
                "sp_EspacioArtistico_ObtenerPorId",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<EspacioArtistico> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            return MapearDesdeTabla(EjecutorStoredProcedure.Leer(
                "sp_EspacioArtistico_ListarPorUsuarioGestor",
                new SqlParameter("@idUsuarioGestor", idUsuarioGestor)));
        }

        public List<EspacioArtistico> ListarPublicados()
        {
            return MapearDesdeTabla(EjecutorStoredProcedure.Leer("sp_EspacioArtistico_ListarPublicados"));
        }

        public void Publicar(int idEspacioArtistico)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_EspacioArtistico_Publicar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void Pausar(int idEspacioArtistico)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_EspacioArtistico_Pausar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void BajaLogica(int idEspacioArtistico)
        {
            EjecutorStoredProcedure.Escribir(
                "sp_EspacioArtistico_BajaLogica",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        private static List<EspacioArtistico> MapearDesdeTabla(DataTable tabla)
        {
            var lista = new List<EspacioArtistico>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        private static EspacioArtistico MapearDesdeFila(DataRow fila)
        {
            return new EspacioArtistico
            {
                IdEspacioArtistico = Convert.ToInt32(fila["idEspacioArtistico"]),
                IdUsuarioGestor = Convert.ToInt32(fila["idUsuarioGestor"]),
                NombreEspacio = fila["nombreEspacio"].ToString(),
                Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                TipoEspacio = fila["tipoEspacio"].ToString(),
                EstadoEspacio = fila["estadoEspacio"].ToString(),
                Publicado = Convert.ToBoolean(fila["publicado"]),
                Activo = Convert.ToBoolean(fila["activo"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaPublicacion = fila["fechaPublicacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaPublicacion"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"])
            };
        }
    }
}
