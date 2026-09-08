using System;
using System.Security.Cryptography;
using System.Text;

namespace StageUp.Seguridad
{
    public static class GeneradorDeCodigos
    {
        public static string GenerarCodigoNumerico(int longitud = ConfiguracionSeguridad.LongitudCodigo)
        {
            var builder = new StringBuilder(longitud);
            byte[] buffer = new byte[4];

            using (var rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < longitud; i++)
                {
                    rng.GetBytes(buffer);
                    uint valor = BitConverter.ToUInt32(buffer, 0);
                    builder.Append((valor % 10).ToString());
                }
            }

            return builder.ToString();
        }
    }
}
