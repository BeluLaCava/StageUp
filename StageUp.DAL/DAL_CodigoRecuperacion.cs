using System;
using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    public class DAL_CodigoRecuperacion
    {
        public int Insertar(int idUsuarioExterno, string codigo, DateTime fechaVencimiento)
        {
            object resultado = EjecutorStoredProcedure.EjecutarEscalar(
                "sp_CodigoRecuperacion_Insertar",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno),
                new SqlParameter("@codigo", codigo),
                new SqlParameter("@fechaVencimiento", fechaVencimiento));

            return Convert.ToInt32(resultado);
        }

        public DataTable ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            return EjecutorStoredProcedure.EjecutarConsulta(
                "sp_CodigoRecuperacion_ObtenerVigentePorUsuario",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno));
        }

        public void MarcarUtilizado(int idCodigoRecuperacion)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_CodigoRecuperacion_MarcarUtilizado",
                new SqlParameter("@idCodigoRecuperacion", idCodigoRecuperacion));
        }
    }
}
