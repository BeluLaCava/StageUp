using System;
using System.Collections;
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

    public sealed class Conexion
    {
        private static Conexion _instance;
        private static readonly object _lock = new object();

        private Conexion()
        {
        }

        public static Conexion Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Conexion();
                    }

                    return _instance;
                }
            }
        }

        private const string NombreCadenaConexion = "StageUpConnectionString";

        private static SqlConnection ObtenerConexion()
        {
            string cadena = ConfigurationManager.ConnectionStrings[NombreCadenaConexion].ConnectionString;
            return new SqlConnection(cadena);
        }

        public DataTable Leer(string nombreSp, Hashtable parametros = null)
        {
            try
            {
                DataTable tabla = new DataTable();

                using (SqlConnection conexion = ObtenerConexion())
                using (SqlCommand comando = new SqlCommand(nombreSp, conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    AgregarParametros(comando, parametros);

                    using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
                    {
                        adaptador.Fill(tabla);
                    }
                }

                return tabla;
            }
            catch (SqlException ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error SQL al consultar los datos. Probá nuevamente en unos minutos.", ex);
            }
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error al consultar los datos. Probá nuevamente en unos minutos.", ex);
            }
        }

        public object LeerEscalar(string nombreSp, Hashtable parametros = null)
        {
            try
            {
                using (SqlConnection conexion = ObtenerConexion())
                using (SqlCommand comando = new SqlCommand(nombreSp, conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    AgregarParametros(comando, parametros);

                    conexion.Open();
                    return comando.ExecuteScalar();
                }
            }
            catch (SqlException ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error SQL al ejecutar la operación. Probá nuevamente en unos minutos.", ex);
            }
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error al ejecutar la operación. Probá nuevamente en unos minutos.", ex);
            }
        }

        public bool Guardar(string nombreSp, Hashtable parametros = null)
        {
            try
            {
                using (SqlConnection conexion = ObtenerConexion())
                using (SqlCommand comando = new SqlCommand(nombreSp, conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    AgregarParametros(comando, parametros);

                    conexion.Open();
                    comando.ExecuteNonQuery();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error SQL al guardar los datos. Probá nuevamente en unos minutos.", ex);
            }
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error al guardar los datos. Probá nuevamente en unos minutos.", ex);
            }
        }

        // ------------------------------------------------------------------
        // Backup y restore (ítem 17 de la segunda entrega). Son operaciones
        // largas (timeout de 10 minutos en vez de los 30 segundos por
        // defecto) y la restauración se ejecuta conectada a master, porque
        // una base no se puede restaurar a sí misma mientras está en uso.
        // ------------------------------------------------------------------
        private const int SegundosTimeoutOperacionLarga = 600;

        public string ObtenerNombreBaseDeDatos()
        {
            string cadena = ConfigurationManager.ConnectionStrings[NombreCadenaConexion].ConnectionString;
            return new SqlConnectionStringBuilder(cadena).InitialCatalog;
        }

        public DataTable LeerOperacionLarga(string nombreSp, Hashtable parametros = null)
        {
            try
            {
                DataTable tabla = new DataTable();

                using (SqlConnection conexion = ObtenerConexion())
                using (SqlCommand comando = new SqlCommand(nombreSp, conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandTimeout = SegundosTimeoutOperacionLarga;
                    AgregarParametros(comando, parametros);

                    using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
                    {
                        adaptador.Fill(tabla);
                    }
                }

                return tabla;
            }
            catch (SqlException ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error SQL en la operación: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error en la operación. Probá nuevamente en unos minutos.", ex);
            }
        }

        public void EjecutarEnMaster(string nombreSp, Hashtable parametros = null)
        {
            try
            {
                string cadena = ConfigurationManager.ConnectionStrings[NombreCadenaConexion].ConnectionString;
                SqlConnectionStringBuilder constructor = new SqlConnectionStringBuilder(cadena)
                {
                    InitialCatalog = "master",
                    Pooling = false
                };

                using (SqlConnection conexion = new SqlConnection(constructor.ConnectionString))
                using (SqlCommand comando = new SqlCommand(nombreSp, conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandTimeout = SegundosTimeoutOperacionLarga;
                    AgregarParametros(comando, parametros);

                    conexion.Open();
                    comando.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error SQL en la operación: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException(
                    nombreSp, "Ocurrió un error en la operación. Probá nuevamente en unos minutos.", ex);
            }
        }

        // Después de restaurar, las conexiones que quedaron en el pool apuntan
        // a sesiones que SQL Server cerró: se descartan todas.
        public void LimpiarConexionesAbiertas()
        {
            SqlConnection.ClearAllPools();
        }

        private static void AgregarParametros(SqlCommand comando, Hashtable parametros)
        {
            if (parametros == null)
            {
                return;
            }

            foreach (DictionaryEntry parametro in parametros)
            {
                string nombre = Convert.ToString(parametro.Key);
                object valor = parametro.Value ?? DBNull.Value;
                comando.Parameters.AddWithValue(nombre, valor);
            }
        }
    }
}
