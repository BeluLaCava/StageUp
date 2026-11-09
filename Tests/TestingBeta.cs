using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using StageUp.BLL;
using StageUp.DAL;
using StageUp.Seguridad;

class TestingBeta
{
    static int passed;
    static string connectionString;

    static void Check(bool condition, string test)
    {
        if (!condition) throw new Exception("FAILED: " + test);
        passed++;
        Console.WriteLine("PASS: " + test);
    }

    static string EstadoRegistro()
    {
        using (var connection = new SqlConnection(connectionString))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"SELECT CONCAT(
                (SELECT COUNT_BIG(*) FROM dbo.UsuarioExterno), ':',
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.UsuarioExterno), '/',
                (SELECT COUNT_BIG(*) FROM dbo.CodigoActivacion), ':',
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.CodigoActivacion), '/',
                (SELECT COUNT_BIG(*) FROM dbo.RegistroActividad), ':',
                (SELECT CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM dbo.RegistroActividad));";
            connection.Open();
            return Convert.ToString(command.ExecuteScalar());
        }
    }

    static int Main()
    {
        try
        {
            connectionString = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
            if (!Regex.IsMatch(new SqlConnectionStringBuilder(connectionString).InitialCatalog,
                @"\AStageUp_Beta2_QA_[A-Za-z0-9_]+\z"))
                throw new InvalidOperationException("Se exige una base QA aislada.");

            foreach (var email in new[] { "persona@example.test", "o'connor@example.test", "ana+reservas@example.com", "ana.apellido@sub.example.com" })
                Check(ValidacionContacto.EsCorreoValido(email), "valid email: " + email);
            foreach (var email in new[] { "a@b..com", "a@-dominio.test", "a..b@example.test", "a.@example.test", "no-email", "ana@example.test\r\nBcc:otra@example.test", new string('a',65) + "@example.test", new string('a',301) })
                Check(!ValidacionContacto.EsCorreoValido(email), "invalid email rejected");
            foreach (var phone in new[] { "+54 11 5555-5555", "(011) 4444-5555", "123456" })
                Check(ValidacionContacto.EsTelefonoValido(phone), "valid phone: " + phone);
            foreach (var phone in new[] { "++++++", "------", "()()()", "12345", "abc123", "123456(", ")123456", new string('1',31), "123456\n" })
                Check(!ValidacionContacto.EsTelefonoValido(phone), "invalid phone rejected");

            string antes = EstadoRegistro();
            var bll = new BLL_UsuarioExterno();
            var result = bll.Registrar(new string('a',201), "Apellido", "qa@example.test", "StageUp2026", "StageUp2026", true, true, null);
            Check(!result.Exitoso && result.Mensaje.Contains("200"), "registration checks name length before saving");
            result = bll.Registrar("Nombre", "Apellido", new string('a',301), "StageUp2026", "StageUp2026", true, true, null);
            Check(!result.Exitoso && result.CodigoAlternativo == "A2", "registration checks email length before saving");
            result = bll.Registrar("Nombre", "Apellido", "qa@example.test", "StageUp2026", "StageUp2026", true, true, "++++++");
            Check(!result.Exitoso && result.CodigoAlternativo == "A6", "registration rejects invalid phone before saving");
            result = bll.Registrar("O'Connor", "Apellido", "qa@example.test", "StageUp2026", "different", true, true, null);
            Check(!result.Exitoso && result.CodigoAlternativo == "A4", "legitimate apostrophe reaches normal password validation");
            Check(antes == EstadoRegistro(), "invalid registrations preserve accounts, activation codes and audit records");
            string correos = ConfigurationManager.AppSettings["CorreoPruebaDirectorio"];
            Check(Directory.Exists(correos) && Directory.GetFiles(correos).Length == 0, "invalid registrations generate no emails");

            var method = typeof(Conexion).GetMethod("AgregarParametros", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var value in new[] { "O'Connor", "' OR '1'='1", "'; DROP TABLE UsuarioExterno; --" })
            using (var command = new SqlCommand("sp_NotExecuted"))
            {
                command.CommandType = CommandType.StoredProcedure;
                method.Invoke(null, new object[] { command, new Hashtable { { "@texto", value } } });
                Check(command.CommandText == "sp_NotExecuted" && (string)command.Parameters["@texto"].Value == value,
                    "SQL values remain parameters; command unchanged");
            }
            Console.WriteLine(passed + " checks passed. Registration validated against the isolated QA database; no registration data changed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }
}
