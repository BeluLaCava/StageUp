using System;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_EspacioArtistico
    {
        public static bool FichaCompletaHabilitada
        {
            get { return string.Equals(ConfigurationManager.AppSettings["EspaciosFichaCompletaHabilitada"], "true", StringComparison.OrdinalIgnoreCase); }
        }

        public int GuardarFicha(EspacioArtistico espacio)
        {
            if (!FichaCompletaHabilitada)
                throw new InvalidOperationException("La ficha completa todavía no está habilitada.");

            return Convert.ToInt32(Conexion.Instance.LeerEscalar(
                "sp_EspacioArtistico_GuardarFichaV2",
                new SqlParameter("@idEspacioArtistico", espacio.IdEspacioArtistico == 0 ? (object)DBNull.Value : espacio.IdEspacioArtistico),
                new SqlParameter("@idUsuarioGestor", espacio.IdUsuarioGestor),
                new SqlParameter("@nombreEspacio", espacio.NombreEspacio),
                new SqlParameter("@tipoEspacio", espacio.TipoEspacio),
                new SqlParameter("@descripcion", (object)espacio.Descripcion ?? DBNull.Value),
                new SqlParameter("@fichaJson", SqlDbType.NVarChar, -1) { Value = new JavaScriptSerializer().Serialize(espacio.Ficha) }));
        }

        public int Insertar(EspacioArtistico espacio)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_EspacioArtistico_Insertar",
                new SqlParameter("@idUsuarioGestor", espacio.IdUsuarioGestor),
                new SqlParameter("@nombreEspacio", espacio.NombreEspacio),
                new SqlParameter("@descripcion", (object)espacio.Descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", espacio.TipoEspacio));

            return Convert.ToInt32(resultado);
        }

        public void Modificar(EspacioArtistico espacio)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Modificar",
                new SqlParameter("@idEspacioArtistico", espacio.IdEspacioArtistico),
                new SqlParameter("@nombreEspacio", espacio.NombreEspacio),
                new SqlParameter("@descripcion", (object)espacio.Descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", espacio.TipoEspacio));
        }

        public EspacioArtistico ObtenerPorId(int idEspacioArtistico)
        {
            DataTable tabla = Conexion.Instance.Leer(
                FichaCompletaHabilitada ? "sp_EspacioArtistico_ObtenerPorIdV2" : "sp_EspacioArtistico_ObtenerPorId",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
            return tabla.Rows.Count == 0 ? null : MapearDesdeFila(tabla.Rows[0]);
        }

        public List<EspacioArtistico> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            return MapearDesdeTabla(Conexion.Instance.Leer(
                FichaCompletaHabilitada ? "sp_EspacioArtistico_ListarPorUsuarioGestorV2" : "sp_EspacioArtistico_ListarPorUsuarioGestor",
                new SqlParameter("@idUsuarioGestor", idUsuarioGestor)));
        }

        public List<EspacioArtistico> ListarPublicados()
        {
            return MapearDesdeTabla(Conexion.Instance.Leer(FichaCompletaHabilitada ? "sp_EspacioArtistico_ListarPublicadosV2" : "sp_EspacioArtistico_ListarPublicados"));
        }

        public void Publicar(int idEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Publicar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void Pausar(int idEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Pausar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void BajaLogica(int idEspacioArtistico)
        {
            Conexion.Instance.Guardar(
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
                Ficha = LeerFicha(fila),
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

        private static FichaEspacio LeerFicha(DataRow fila)
        {
            if (!FichaCompletaHabilitada)
                return new FichaEspacio();
            if (!fila.Table.Columns.Contains("fichaJson"))
                throw new InvalidOperationException("El procedimiento V2 debe devolver fichaJson.");
            if (fila["fichaJson"] == DBNull.Value || string.IsNullOrWhiteSpace(fila["fichaJson"].ToString()))
                return new FichaEspacio();
            return new JavaScriptSerializer().Deserialize<FichaEspacio>(fila["fichaJson"].ToString()) ?? new FichaEspacio();
        }
    }
}
