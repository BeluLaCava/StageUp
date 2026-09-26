using System;
using System.Configuration;
using System.Security.Cryptography;

namespace StageUp.Seguridad
{
    /// <summary>
    /// Encripta y desencripta bloques cortos de datos (por ejemplo, una clave
    /// simétrica efímera, ver EncriptadorHibrido) usando RSA de 2048 bits con
    /// relleno OAEP. El par de claves se genera una única vez con
    /// GenerarParDeClaves y se guarda en AppSettings.private.config (nunca en
    /// el Web.config versionado), igual que ClaveEncriptacionSimetrica.
    /// RSA solo puede cifrar bloques pequeños (unos 200 bytes con esta
    /// configuración) — no está pensado para cifrar directamente archivos
    /// completos, sino para envolver una clave simétrica que sí cifra el
    /// contenido real.
    /// </summary>
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
