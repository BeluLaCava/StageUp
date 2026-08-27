using System;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    public class DAL_RegistroActividad
    {
        public void Insertar(
            int? idUsuarioExternoResponsable, int? idUsuarioInternoResponsable,
            string tipoOperacion, string tipoEntidadAfectada, int? idEntidadAfectada,
            string descripcionOperacion, string origenOperacion)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_RegistroActividad_Insertar",
                new SqlParameter("@idUsuarioExternoResponsable", (object)idUsuarioExternoResponsable ?? DBNull.Value),
                new SqlParameter("@idUsuarioInternoResponsable", (object)idUsuarioInternoResponsable ?? DBNull.Value),
                new SqlParameter("@tipoOperacion", tipoOperacion),
                new SqlParameter("@tipoEntidadAfectada", tipoEntidadAfectada),
                new SqlParameter("@idEntidadAfectada", (object)idEntidadAfectada ?? DBNull.Value),
                new SqlParameter("@descripcionOperacion", (object)descripcionOperacion ?? DBNull.Value),
                new SqlParameter("@origenOperacion", (object)origenOperacion ?? DBNull.Value));
        }
    }
}
