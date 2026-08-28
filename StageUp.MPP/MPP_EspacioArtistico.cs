using System;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    /// <summary>
    /// Mapeo entre la entidad de negocio EspacioArtistico y su persistencia
    /// (a través de DAL_EspacioArtistico). La BLL solo conoce esta clase,
    /// nunca a DAL_EspacioArtistico directamente.
    /// </summary>
    public class MPP_EspacioArtistico
    {
        private readonly DAL_EspacioArtistico _dal = new DAL_EspacioArtistico();

        public int Insertar(EspacioArtistico espacio)
        {
            return _dal.Insertar(espacio.IdUsuarioGestor, espacio.NombreEspacio, espacio.Descripcion, espacio.TipoEspacio);
        }

        public void Modificar(EspacioArtistico espacio)
        {
            _dal.Modificar(espacio.IdEspacioArtistico, espacio.NombreEspacio, espacio.Descripcion, espacio.TipoEspacio);
        }

        public EspacioArtistico ObtenerPorId(int idEspacioArtistico)
        {
            DataTable tabla = _dal.ObtenerPorId(idEspacioArtistico);
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<EspacioArtistico> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            return MapearDesdeTabla(_dal.ListarPorUsuarioGestor(idUsuarioGestor));
        }

        public List<EspacioArtistico> ListarPublicados()
        {
            return MapearDesdeTabla(_dal.ListarPublicados());
        }

        public void Publicar(int idEspacioArtistico)
        {
            _dal.Publicar(idEspacioArtistico);
        }

        public void Pausar(int idEspacioArtistico)
        {
            _dal.Pausar(idEspacioArtistico);
        }

        public void BajaLogica(int idEspacioArtistico)
        {
            _dal.BajaLogica(idEspacioArtistico);
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
