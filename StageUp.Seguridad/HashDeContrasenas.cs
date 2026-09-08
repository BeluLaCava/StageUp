using System;
using System.Security.Cryptography;

namespace StageUp.Seguridad
{
    public static class HashDeContrasenas
    {
        private const int TamanioSalBytes = 16;
        private const int TamanioHashBytes = 32;
        private const int Iteraciones = 100_000;

        public static string CrearHash(string passwordEnClaro)
        {
            if (passwordEnClaro == null)
            {
                throw new ArgumentNullException("passwordEnClaro");
            }

            byte[] sal = new byte[TamanioSalBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(sal);
            }

            byte[] hash = CalcularHash(passwordEnClaro, sal, Iteraciones, TamanioHashBytes);

            return string.Format(
                "{0}.{1}.{2}",
                Iteraciones,
                Convert.ToBase64String(sal),
                Convert.ToBase64String(hash));
        }

        public static bool Verificar(string passwordEnClaro, string hashAlmacenado)
        {
            if (string.IsNullOrEmpty(hashAlmacenado))
            {
                return false;
            }

            string[] partes = hashAlmacenado.Split('.');
            if (partes.Length != 3)
            {
                return false;
            }

            int iteraciones = int.Parse(partes[0]);
            byte[] sal = Convert.FromBase64String(partes[1]);
            byte[] hashEsperado = Convert.FromBase64String(partes[2]);

            byte[] hashCalculado = CalcularHash(passwordEnClaro, sal, iteraciones, hashEsperado.Length);

            return SonIguales(hashCalculado, hashEsperado);
        }

        private static byte[] CalcularHash(string passwordEnClaro, byte[] sal, int iteraciones, int tamanioSalida)
        {
            using (var derivador = new Rfc2898DeriveBytes(passwordEnClaro, sal, iteraciones))
            {
                return derivador.GetBytes(tamanioSalida);
            }
        }

        private static bool SonIguales(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int diferencia = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diferencia |= a[i] ^ b[i];
            }

            return diferencia == 0;
        }
    }
}
