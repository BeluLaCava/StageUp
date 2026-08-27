using System;
using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    public class DAL_CodigoActivacion
    {
        public int Insertar(int idUsuarioExterno, string codigo, DateTime fechaVencimiento)
        {
            object resultado = EjecutorStoredProcedure.EjecutarEscalar(
                "sp_CodigoActivacion_Insertar",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno),
                new SqlParameter("@codigo", codigo),
                new SqlParameter("@fechaVencimiento", fechaVencimiento));

            return Convert.ToInt32(resultado);
        }

        public DataTable ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            return EjecutorStoredProcedure.EjecutarConsulta(
                "sp_CodigoActivacion_ObtenerVigentePorUsuario",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno));
        }

        public void MarcarUtilizado(int idCodigoActivacion)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_CodigoActivacion_MarcarUtilizado",
                new SqlParameter("@idCodigoActivacion", idCodigoActivacion));
        }
    }
}
