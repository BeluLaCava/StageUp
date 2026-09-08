using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    public static class EjecutorStoredProcedure
    {
        public static DataTable Leer(string nombreSp, params SqlParameter[] parametros)
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

        public static object LeerEscalar(string nombreSp, params SqlParameter[] parametros)
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

        public static void Escribir(string nombreSp, params SqlParameter[] parametros)
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
