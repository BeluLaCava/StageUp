using System;
using System.Configuration;
using System.Security.Cryptography;

namespace StageUp.Seguridad
{
         public static class EncriptadorAsimetrico
    {
        private const int TamanioClaveBits = 2048;

        public static void GenerarParDeClaves(out string claveBase64Publica, out string claveBase64Privada)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(TamanioClaveBits))
            {
                claveBase64Publica = Convert.ToBase64String(rsa.ExportCspBlob(false));
                claveBase64Privada = Convert.ToBase64String(rsa.ExportCspBlob(true));
            }
        }

        public static bool HayClavesConfiguradas()
        {
            return !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ClaveAsimetricaPublica"])
                && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ClaveAsimetricaPrivada"]);
        }



        public static byte[] Encriptar(byte[] datos, string clavePublicaBase64)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(Convert.FromBase64String(clavePublicaBase64));
                return rsa.Encrypt(datos, true);
            }
        }

        public static byte[] Desencriptar(byte[] datosCifrados, string clavePrivadaBase64)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(Convert.FromBase64String(clavePrivadaBase64));
                return rsa.Decrypt(datosCifrados, true);
            }
        }



        public static string CalcularHuella(string clavePublicaBase64)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Convert.FromBase64String(clavePublicaBase64));
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty);
                return string.Join(" ", new[] { hex.Substring(0, 4), hex.Substring(4, 4), hex.Substring(8, 4), hex.Substring(12, 4),
                                                hex.Substring(16, 4), hex.Substring(20, 4), hex.Substring(24, 4), hex.Substring(28, 4) });
            }
        }

        public static byte[] Encriptar(byte[] datos)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(ObtenerClave("ClaveAsimetricaPublica"));
                return rsa.Encrypt(datos, true);
            }
        }

        public static byte[] Desencriptar(byte[] datosCifrados)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(ObtenerClave("ClaveAsimetricaPrivada"));
                return rsa.Decrypt(datosCifrados, true);
            }
        }

        private static byte[] ObtenerClave(string nombreAppSetting)
        {
            string claveBase64 = ConfigurationManager.AppSettings[nombreAppSetting];
            if (string.IsNullOrEmpty(claveBase64))
            {
                throw new InvalidOperationException(
                    "No se configuró el par de claves asimétricas (" + nombreAppSetting + "). Generalo desde Herramientas de seguridad.");
            }

            try
            {
                return Convert.FromBase64String(claveBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "El valor configurado para " + nombreAppSetting + " no es un Base64 válido.", ex);
            }
        }
    }
}
