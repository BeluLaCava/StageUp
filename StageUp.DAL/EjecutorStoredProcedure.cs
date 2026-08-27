using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    /// <summary>
    /// Helper común para ejecutar stored procedures por ADO.NET. Las
    /// clases DAL_* de cada entidad arman los parámetros y usan este
    /// helper en vez de repetir el manejo de SqlConnection/SqlCommand.
    /// </summary>
    internal static class EjecutorStoredProcedure
    {
        public static DataTable EjecutarConsulta(string nombreSp, params SqlParameter[] parametros)
        {
            var tabla = new DataTable();

            using (SqlConnection conexion = ConexionSql.ObtenerConexion())
            using (var comando = new SqlCommand(nombreSp, conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                if (parametros != null)
                {
                    comando.Parameters.AddRange(parametros);
                }

                using (var adaptador = new SqlDataAdapter(comando))
                {
                    adaptador.Fill(tabla);
                }
            }

            return tabla;
        }

        public static object EjecutarEscalar(string nombreSp, params SqlParameter[] parametros)
        {
            using (SqlConnection conexion = ConexionSql.ObtenerConexion())
            using (var comando = new SqlCommand(nombreSp, conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                if (parametros != null)
                {
                    comando.Parameters.AddRange(parametros);
                }

                conexion.Open();
                return comando.ExecuteScalar();
            }
        }

        public static void EjecutarNonQuery(string nombreSp, params SqlParameter[] parametros)
        {
            using (SqlConnection conexion = ConexionSql.ObtenerConexion())
            using (var comando = new SqlCommand(nombreSp, conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                if (parametros != null)
                {
                    comando.Parameters.AddRange(parametros);
                }

                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }
    }
}
