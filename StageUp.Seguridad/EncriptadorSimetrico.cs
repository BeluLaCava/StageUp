using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StageUp.Seguridad
{
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

                 public static string CalcularHuella(string texto, string proposito)
        {
            byte[] claveDerivada;
            using (HMACSHA256 derivador = new HMACSHA256(ObtenerClave()))
            {
                claveDerivada = derivador.ComputeHash(Encoding.UTF8.GetBytes("StageUp|" + (proposito ?? string.Empty)));
            }

            using (HMACSHA256 hmac = new HMACSHA256(claveDerivada))
            {
                byte[] huella = hmac.ComputeHash(Encoding.UTF8.GetBytes(texto ?? string.Empty));
                return BitConverter.ToString(huella).Replace("-", string.Empty);
            }
        }

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
