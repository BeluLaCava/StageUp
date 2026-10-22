using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StageUp.Seguridad
{
         public static class EncriptadorHibrido
    {


        public static void Encriptar(
            string textoPlano, out string ivBase64, out string claveCifradaBase64, out string datosCifradosBase64)
        {
            Encriptar(textoPlano, null, out ivBase64, out claveCifradaBase64, out datosCifradosBase64);
        }


        public static void Encriptar(
            string textoPlano, string clavePublicaBase64, out string ivBase64, out string claveCifradaBase64, out string datosCifradosBase64)
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

                byte[] claveCifrada = clavePublicaBase64 == null
                    ? EncriptadorAsimetrico.Encriptar(aes.Key)
                    : EncriptadorAsimetrico.Encriptar(aes.Key, clavePublicaBase64);

                ivBase64 = Convert.ToBase64String(aes.IV);
                claveCifradaBase64 = Convert.ToBase64String(claveCifrada);
                datosCifradosBase64 = Convert.ToBase64String(datosCifrados);
            }
        }

        public static string Desencriptar(string ivBase64, string claveCifradaBase64, string datosCifradosBase64)
        {
            return Desencriptar(ivBase64, claveCifradaBase64, datosCifradosBase64, null);
        }

        public static string Desencriptar(string ivBase64, string claveCifradaBase64, string datosCifradosBase64, string clavePrivadaBase64)
        {
            byte[] claveCifrada = Convert.FromBase64String(claveCifradaBase64);
            byte[] claveAes = clavePrivadaBase64 == null
                ? EncriptadorAsimetrico.Desencriptar(claveCifrada)
                : EncriptadorAsimetrico.Desencriptar(claveCifrada, clavePrivadaBase64);
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




        public static string ReenvolverClave(string claveCifradaBase64, string clavePrivadaAnteriorBase64, string clavePublicaNuevaBase64)
        {
            byte[] claveAes = EncriptadorAsimetrico.Desencriptar(Convert.FromBase64String(claveCifradaBase64), clavePrivadaAnteriorBase64);
            try
            {
                return Convert.ToBase64String(EncriptadorAsimetrico.Encriptar(claveAes, clavePublicaNuevaBase64));
            }
            finally
            {
                Array.Clear(claveAes, 0, claveAes.Length);
            }
        }
    }
}
