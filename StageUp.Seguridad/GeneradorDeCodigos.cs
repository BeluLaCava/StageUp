using System;
using System.Security.Cryptography;
using System.Text;

namespace StageUp.Seguridad
{
    /// <summary>
    /// Generación de códigos de activación / recuperación. Se usan
    /// códigos numéricos de 6 dígitos (fáciles de transcribir desde un
    /// correo electrónico), generados con un generador criptográficamente
    /// seguro en vez de System.Random.
    /// </summary>
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
