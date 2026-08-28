using System;
using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    /// <summary>
    /// Acceso a datos exclusivo de EspacioArtistico. Solo ejecuta los
    /// stored procedures correspondientes; no valida ni decide nada
    /// (eso es responsabilidad de StageUp.BLL).
    /// </summary>
    public class DAL_EspacioArtistico
    {
        public int Insertar(int idUsuarioGestor, string nombreEspacio, string descripcion, string tipoEspacio)
        {
            object resultado = EjecutorStoredProcedure.EjecutarEscalar(
                "sp_EspacioArtistico_Insertar",
                new SqlParameter("@idUsuarioGestor", idUsuarioGestor),
                new SqlParameter("@nombreEspacio", nombreEspacio),
                new SqlParameter("@descripcion", (object)descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", tipoEspacio));

            return Convert.ToInt32(resultado);
        }

        public void Modificar(int idEspacioArtistico, string nombreEspacio, string descripcion, string tipoEspacio)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_EspacioArtistico_Modificar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico),
                new SqlParameter("@nombreEspacio", nombreEspacio),
                new SqlParameter("@descripcion", (object)descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", tipoEspacio));
        }

        public DataTable ObtenerPorId(int idEspacioArtistico)
        {
            return EjecutorStoredProcedure.EjecutarConsulta(
                "sp_EspacioArtistico_ObtenerPorId",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public DataTable ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            return EjecutorStoredProcedure.EjecutarConsulta(
                "sp_EspacioArtistico_ListarPorUsuarioGestor",
                new SqlParameter("@idUsuarioGestor", idUsuarioGestor));
        }

        public DataTable ListarPublicados()
        {
            return EjecutorStoredProcedure.EjecutarConsulta("sp_EspacioArtistico_ListarPublicados");
        }

        public void Publicar(int idEspacioArtistico)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_EspacioArtistico_Publicar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void Pausar(int idEspacioArtistico)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_EspacioArtistico_Pausar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void BajaLogica(int idEspacioArtistico)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_EspacioArtistico_BajaLogica",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }
    }
}
