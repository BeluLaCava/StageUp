using System.Configuration;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    /// <summary>
    /// Punto único para obtener conexiones a SQL Server. Lee la cadena de
    /// conexión "StageUpConnectionString" del Web.config del proyecto que
    /// consume esta capa (StageUp.UI). No contiene reglas de negocio: solo
    /// acceso a datos.
    /// </summary>
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
