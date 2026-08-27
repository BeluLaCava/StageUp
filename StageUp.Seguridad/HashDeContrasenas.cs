using System;
using System.Security.Cryptography;

namespace StageUp.Seguridad
{
    /// <summary>
    /// Hashing de contraseñas con sal aleatoria por usuario (PBKDF2 /
    /// Rfc2898DeriveBytes, disponible en .NET Framework sin paquetes
    /// externos). Reemplaza el esquema de SHA-256 sin sal que usaba el
    /// proyecto de referencia Ingenieria-Software, que hoy se considera
    /// débil para el manejo de credenciales.
    /// El resultado se guarda como un único string con el formato:
    ///   {iteraciones}.{saltBase64}.{hashBase64}
    /// para no necesitar columnas adicionales en UsuarioExterno.passwordHash.
    /// </summary>
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

        /// <summary>
        /// Comparación en tiempo constante para evitar timing attacks.
        /// </summary>
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
