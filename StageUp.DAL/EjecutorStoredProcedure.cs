using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    public class ErrorAccesoDatosException : Exception
    {
        public string NombreStoredProcedure { get; private set; }

        public ErrorAccesoDatosException(string nombreStoredProcedure, string mensaje, Exception innerException)
            : base(mensaje, innerException)
        {
            NombreStoredProcedure = nombreStoredProcedure;
        }
    }

    public static class EjecutorStoredProcedure
    {
        private const string NombreCadenaConexion = "StageUpConnectionString";

        private static SqlConnection ObtenerConexion()
        {
            string cadena = ConfigurationManager.ConnectionStrings[NombreCadenaConexion].ConnectionString;
            return new SqlConnection(cadena);
        }

        public static DataTable Leer(string nombreSp, params SqlParameter[] parametros)
        {
            try
            {
                var tabla = new DataTable();

                using (SqlConnection conexion = ObtenerConexion())
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
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error al consultar los datos. Probá nuevamente en unos minutos.", ex);
            }
        }

        public static object LeerEscalar(string nombreSp, params SqlParameter[] parametros)
        {
            try
            {
                using (SqlConnection conexion = ObtenerConexion())
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
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error al guardar los datos. Probá nuevamente en unos minutos.", ex);
            }
        }

        public static void Escribir(string nombreSp, params SqlParameter[] parametros)
        {
            try
            {
                using (SqlConnection conexion = ObtenerConexion())
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
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error al guardar los datos. Probá nuevamente en unos minutos.", ex);
            }
        }
    }
}
