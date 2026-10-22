using System;
using System.Security.Cryptography;
using StageUp.BE.Entidades;

namespace StageUp.Servicios
{
    public class ResultadoAutorizacionPago
    {
        public bool Aprobado { get; set; }
        public string CodigoAutorizacion { get; set; }
        public string MotivoRechazo { get; set; }
    }












    public class ServicioPasarelaPago
    {
        public ResultadoAutorizacionPago Autorizar(DatosTarjeta tarjeta, decimal importe, string moneda)
        {
            string numero = SoloDigitos(tarjeta == null ? null : tarjeta.Numero);
            if (numero.Length < 4 || importe <= 0)
            {
                return Rechazo("Los datos del pago no son válidos.");
            }

            if (numero.EndsWith("0002", StringComparison.Ordinal))
            {
                return Rechazo("La tarjeta fue rechazada por el banco emisor. Probá con otra tarjeta.");
            }

            if (numero.EndsWith("9995", StringComparison.Ordinal))
            {
                return Rechazo("La tarjeta no tiene fondos suficientes para este pago.");
            }

            if (numero.EndsWith("0119", StringComparison.Ordinal))
            {
                return Rechazo("No pudimos comunicarnos con el procesador de pagos. Probá de nuevo en unos minutos.");
            }

            return new ResultadoAutorizacionPago
            {
                Aprobado = true,
                CodigoAutorizacion = GenerarCodigoAutorizacion()
            };
        }

        private static ResultadoAutorizacionPago Rechazo(string motivo)
        {
            return new ResultadoAutorizacionPago { Aprobado = false, MotivoRechazo = motivo };
        }

        private static string GenerarCodigoAutorizacion()
        {
            byte[] bytes = new byte[4];
            using (RandomNumberGenerator generador = RandomNumberGenerator.Create())
            {
                generador.GetBytes(bytes);
            }

            uint valor = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return valor.ToString("000000");
        }

        private static string SoloDigitos(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }

            char[] digitos = Array.FindAll(texto.ToCharArray(), c => c >= '0' && c <= '9');
            return new string(digitos);
        }
    }
}
