using System.Configuration;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    public static class ConexionSql
    {
        private const string NombreCadenaConexion = "StageUpConnectionString";

        public static SqlConnection ObtenerConexion()
        {
            string cadena = ConfigurationManager.ConnectionStrings[NombreCadenaConexion].ConnectionString;
            return new SqlConnection(cadena);
        }
    }
}
