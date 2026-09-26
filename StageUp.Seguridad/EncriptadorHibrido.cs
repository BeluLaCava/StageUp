using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StageUp.Seguridad
{
    /// <summary>
    /// Cifrado híbrido: genera una clave AES efímera (nueva en cada llamada,
    /// nunca se reutiliza ni se guarda), cifra el texto con esa clave, y
    /// envuelve la clave AES con EncriptadorAsimetrico (RSA). Es el mismo
    /// esquema que usan TLS o PGP para poder cifrar datos de cualquier
    /// tamaño con un par de claves asimétricas, que por su naturaleza solo
    /// pueden cifrar bloques muy chicos.
    /// </summary>
    public static class EncriptadorHibrido
    {
        public static void Encriptar(
            string textoPlano, out string ivBase64, out string claveCifradaBase64, out string datosCifradosBase64)
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] datosCifrados;
                using (ICryptoTransform encriptador = aes.CreateEncryptor())
                using (MemoryStream flujoMemoria = new MemoryStream())
                {
                    using (CryptoStream flujoCripto = new CryptoStream(flujoMemoria, encriptador, CryptoStreamMode.Write))
                    {
                        byte[] datos = Encoding.UTF8.GetBytes(textoPlano);
                        flujoCripto.Write(datos, 0, datos.Length);
                        flujoCripto.FlushFinalBlock();
                        datosCifrados = flujoMemoria.ToArray();
                    }
                }

                byte[] claveCifrada = EncriptadorAsimetrico.Encriptar(aes.Key);

                ivBase64 = Convert.ToBase64String(aes.IV);
                claveCifradaBase64 = Convert.ToBase64String(claveCifrada);
                datosCifradosBase64 = Convert.ToBase64String(datosCifrados);
            }
        }

        public static string Desencriptar(string ivBase64, string claveCifradaBase64, string datosCifradosBase64)
        {
            byte[] claveAes = EncriptadorAsimetrico.Desencriptar(Convert.FromBase64String(claveCifradaBase64));
            byte[] iv = Convert.FromBase64String(ivBase64);
            byte[] datosCifrados = Convert.FromBase64String(datosCifradosBase64);

            using (Aes aes = Aes.Create())
            {
                aes.Key = claveAes;
                aes.IV = iv;

                using (ICryptoTransform desencriptador = aes.CreateDecryptor())
                using (MemoryStream flujoMemoria = new MemoryStream(datosCifrados))
                using (CryptoStream flujoCripto = new CryptoStream(flujoMemoria, desencriptador, CryptoStreamMode.Read))
                using (MemoryStream flujoResultado = new MemoryStream())
                {
                    flujoCripto.CopyTo(flujoResultado);
                    return Encoding.UTF8.GetString(flujoResultado.ToArray());
                }
            }
        }
    }
}
