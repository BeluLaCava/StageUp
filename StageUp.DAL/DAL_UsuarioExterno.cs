using System;
using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    /// <summary>
    /// Acceso a datos exclusivo de UsuarioExterno. Solo ejecuta los
    /// stored procedures correspondientes; no valida ni decide nada
    /// (eso es responsabilidad de StageUp.BLL).
    /// </summary>
    public class DAL_UsuarioExterno
    {
        public int Insertar(
            string nombre, string apellido, string correoElectronico, string passwordHash,
            string telefono, string estadoCuenta, string perfilUsuario,
            bool aceptaTerminos, bool aceptaPoliticaPrivacidad, DateTime? fechaAceptacionTerminos)
        {
            object resultado = EjecutorStoredProcedure.EjecutarEscalar(
                "sp_UsuarioExterno_Insertar",
                new SqlParameter("@nombre", nombre),
                new SqlParameter("@apellido", apellido),
                new SqlParameter("@correoElectronico", correoElectronico),
                new SqlParameter("@passwordHash", passwordHash),
                new SqlParameter("@telefono", (object)telefono ?? DBNull.Value),
                new SqlParameter("@estadoCuenta", estadoCuenta),
                new SqlParameter("@perfilUsuario", perfilUsuario),
                new SqlParameter("@aceptaTerminos", aceptaTerminos),
                new SqlParameter("@aceptaPoliticaPrivacidad", aceptaPoliticaPrivacidad),
                new SqlParameter("@fechaAceptacionTerminos", (object)fechaAceptacionTerminos ?? DBNull.Value));

            return Convert.ToInt32(resultado);
        }

        public DataTable ObtenerPorCorreo(string correoElectronico)
        {
            return EjecutorStoredProcedure.EjecutarConsulta(
                "sp_UsuarioExterno_ObtenerPorCorreo",
                new SqlParameter("@correoElectronico", correoElectronico));
        }

        public DataTable ObtenerPorId(int idUsuarioExterno)
        {
            return EjecutorStoredProcedure.EjecutarConsulta(
                "sp_UsuarioExterno_ObtenerPorId",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno));
        }

        public void ActivarCuenta(int idUsuarioExterno, string estadoCuenta)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_UsuarioExterno_ActivarCuenta",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno),
                new SqlParameter("@estadoCuenta", estadoCuenta));
        }

        public void ActualizarPassword(int idUsuarioExterno, string passwordHash)
        {
            EjecutorStoredProcedure.EjecutarNonQuery(
                "sp_UsuarioExterno_ActualizarPassword",
                new SqlParameter("@idUsuarioExterno", idUsuarioExterno),
                new SqlParameter("@passwordHash", passwordHash));
        }
    }
}
