using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StageUp.Seguridad
{
    /// <summary>
    /// Encripta y desencripta datos sensibles (por ejemplo, el teléfono del usuario)
    /// usando AES-256 en modo CBC. La clave se toma primero de la variable de entorno
    /// STAGEUP_CLAVE_ENCRIPTACION y, si no está definida, del AppSetting
    /// "ClaveEncriptacionSimetrica" (que debe vivir únicamente en AppSettings.private.config,
    /// nunca en el Web.config versionado).
    /// </summary>
    public static class EncriptadorSimetrico
    {
        private static byte[] ObtenerClave()
        {
            string claveBase64 = Environment.GetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION");
            if (string.IsNullOrEmpty(claveBase64))
            {
                claveBase64 = ConfigurationManager.AppSettings["ClaveEncriptacionSimetrica"];
            }

            if (string.IsNullOrEmpty(claveBase64))
            {
                throw new InvalidOperationException(
                    "No se configuró la clave de encriptación simétrica (ClaveEncriptacionSimetrica).");
            }

            byte[] clave;
            try
            {
                clave = Convert.FromBase64String(claveBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "La clave de encriptación simétrica (ClaveEncriptacionSimetrica) no es un valor Base64 válido.", ex);
            }

            if (clave.Length != 32)
            {
                throw new InvalidOperationException(
                    "La clave de encriptación simétrica debe tener 32 bytes (AES-256) codificados en Base64.");
            }

            return clave;
        }

        /// <summary>
        /// Encripta un texto plano. Devuelve null si la entrada es null.
        /// El resultado incluye el IV (los primeros 16 bytes) seguido del texto cifrado,
        /// todo codificado en Base64 para poder guardarlo en una columna NVARCHAR.
        /// </summary>
        public static string Encriptar(string textoPlano)
        {
            if (textoPlano == null)
            {
                return null;
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = ObtenerClave();
                aes.GenerateIV();

                using (ICryptoTransform encriptador = aes.CreateEncryptor())
                using (MemoryStream flujoMemoria = new MemoryStream())
                {
                    flujoMemoria.Write(aes.IV, 0, aes.IV.Length);
                    using (CryptoStream flujoCripto = new CryptoStream(flujoMemoria, encriptador, CryptoStreamMode.Write))
                    {
                        byte[] datos = Encoding.UTF8.GetBytes(textoPlano);
                        flujoCripto.Write(datos, 0, datos.Length);
                        flujoCripto.FlushFinalBlock();
                        return Convert.ToBase64String(flujoMemoria.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Desencripta un texto generado por Encriptar. Devuelve null si la entrada es null.
        /// Tira FormatException o CryptographicException si el valor no es un texto cifrado válido
        /// (por ejemplo, si viene un valor viejo en texto plano cargado antes de esta funcionalidad).
        /// </summary>
        public static string Desencriptar(string textoCifrado)
        {
            if (textoCifrado == null)
            {
                return null;
            }

            byte[] datosCompletos = Convert.FromBase64String(textoCifrado);

            using (Aes aes = Aes.Create())
            {
                aes.Key = ObtenerClave();

                if (datosCompletos.Length < aes.IV.Length)
                {
                    throw new CryptographicException("El texto cifrado es demasiado corto para contener un IV válido.");
                }

                byte[] iv = new byte[aes.IV.Length];
                Array.Copy(datosCompletos, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (ICryptoTransform desencriptador = aes.CreateDecryptor())
                using (MemoryStream flujoMemoria = new MemoryStream(datosCompletos, iv.Length, datosCompletos.Length - iv.Length))
                using (CryptoStream flujoCripto = new CryptoStream(flujoMemoria, desencriptador, CryptoStreamMode.Read))
                using (MemoryStream flujoResultado = new MemoryStream())
                {
                    flujoCripto.CopyTo(flujoResultado);
                    return Encoding.UTF8.GetString(flujoResultado.ToArray());
                }
            }
        }

        /// <summary>
        /// Igual que Desencriptar, pero si el valor guardado no es un texto cifrado válido
        /// (por ejemplo, datos cargados antes de habilitar la encriptación), devuelve el valor
        /// original tal cual en vez de tirar una excepción. Pensado para usarse al leer de la base.
        /// </summary>
        public static string DesencriptarOMantener(string valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return valor;
            }

            try
            {
                return Desencriptar(valor);
            }
            catch (FormatException)
            {
                return valor;
            }
            catch (CryptographicException)
            {
                return valor;
            }
        }
    }
}
