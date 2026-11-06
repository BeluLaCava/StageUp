using System;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace StageUp.Seguridad
{
    public static class ValidacionContacto
    {
        public const int MaximoNombre = 200;
        public const int MaximoCorreo = 300;
        // Compatible con los validadores de Web Forms y con JavaScript.
        public const string PatronTelefono = @"^(?=(?:[^0-9]*[0-9]){6})\+?[0-9() \-]{6,30}$";
        private static readonly Regex Telefono = new Regex(PatronTelefono, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        private static readonly Regex EtiquetaDominio = new Regex(@"\A[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\z", RegexOptions.Compiled);

        public static bool EsCorreoValido(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor.Length > MaximoCorreo || valor.IndexOfAny(new[] { '\r', '\n', '\t', '<', '>' }) >= 0)
                return false;
            string correo = valor.Trim();
            try
            {
                MailAddress direccion = new MailAddress(correo);
                if (!string.Equals(direccion.Address, correo, StringComparison.OrdinalIgnoreCase)) return false;
                string usuario = direccion.User;
                if (usuario.Length > 64 || usuario.StartsWith(".") || usuario.EndsWith(".") || usuario.Contains("..")) return false;
                string dominio = new IdnMapping().GetAscii(direccion.Host);
                string[] partes = dominio.Split('.');
                if (partes.Length < 2 || dominio.Length > 253) return false;
                foreach (string parte in partes)
                    if (!EtiquetaDominio.IsMatch(parte)) return false;
                return true;
            }
            catch (FormatException) { return false; }
            catch (ArgumentException) { return false; }
        }

        public static bool EsTelefonoValido(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor.Length > 30 || valor.IndexOfAny(new[] { '\r', '\n', '\t' }) >= 0 || !Telefono.IsMatch(valor)) return false;
            int parentesis = 0;
            foreach (char caracter in valor)
            {
                if (caracter == '(' && ++parentesis > 1) return false;
                if (caracter == ')' && --parentesis < 0) return false;
            }
            return parentesis == 0;
        }
    }
}
