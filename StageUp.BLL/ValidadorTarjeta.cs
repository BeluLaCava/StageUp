using System;
using System.Globalization;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;

namespace StageUp.BLL
{
    // Validación de tarjeta de crédito (ítem 5B de la segunda entrega).
    // Todo se valida antes de llamar a la pasarela: número (algoritmo de
    // Luhn), marca, vencimiento, código de seguridad y titular. El número
    // completo y el código nunca se guardan: de la tarjeta solo se persisten
    // la marca y los últimos 4 dígitos.
    public static class ValidadorTarjeta
    {
        public const string MarcaVisa = "Visa";
        public const string MarcaMastercard = "Mastercard";
        public const string MarcaAmex = "American Express";

        private const int MaximoAniosVigencia = 20;
        private static readonly Regex PatronTitular =
            new Regex(@"^[\p{L}][\p{L}'\.\- ]{1,98}[\p{L}\.]$", RegexOptions.Compiled);

        // Devuelve la marca si la tarjeta es válida.
        public static ResultadoOperacion<string> Validar(DatosTarjeta tarjeta)
        {
            if (tarjeta == null)
            {
                return ResultadoOperacion<string>.Error("Completá los datos de la tarjeta.");
            }

            string numero = NormalizarNumero(tarjeta.Numero);
            if (numero == null || numero.Length < 13 || numero.Length > 19)
            {
                return ResultadoOperacion<string>.Error("El número de tarjeta tiene que tener entre 13 y 19 dígitos.");
            }

            if (!CumpleLuhn(numero))
            {
                return ResultadoOperacion<string>.Error("El número de tarjeta no es válido. Revisá que esté bien escrito.");
            }

            string marca = DetectarMarca(numero);
            if (marca == null)
            {
                return ResultadoOperacion<string>.Error("Aceptamos tarjetas Visa, Mastercard y American Express.");
            }

            ResultadoOperacion vencimiento = ValidarVencimiento(tarjeta.MesVencimiento, tarjeta.AnioVencimiento);
            if (!vencimiento.Exitoso)
            {
                return ResultadoOperacion<string>.Error(vencimiento.Mensaje);
            }

            int largoCodigo = marca == MarcaAmex ? 4 : 3;
            string codigo = (tarjeta.CodigoSeguridad ?? string.Empty).Trim();
            if (codigo.Length != largoCodigo || !EsSoloDigitos(codigo))
            {
                return ResultadoOperacion<string>.Error(
                    "El código de seguridad tiene que tener " + largoCodigo + " dígitos" +
                    (marca == MarcaAmex ? " (en American Express está en el frente de la tarjeta)." : "."));
            }

            string titular = NormalizarTitular(tarjeta.Titular);
            if (titular.Length < 3 || !PatronTitular.IsMatch(titular) || titular.IndexOf(' ') < 0)
            {
                return ResultadoOperacion<string>.Error("Escribí el nombre y apellido del titular tal como figura en la tarjeta.");
            }

            return ResultadoOperacion<string>.Ok(marca);
        }

        public static string NormalizarNumero(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
            {
                return null;
            }

            string sinSeparadores = numero.Replace(" ", string.Empty).Replace("-", string.Empty);
            return EsSoloDigitos(sinSeparadores) ? sinSeparadores : null;
        }

        public static string NormalizarTitular(string titular)
        {
            if (string.IsNullOrWhiteSpace(titular))
            {
                return string.Empty;
            }

            return Regex.Replace(titular.Trim(), @"\s+", " ").ToUpper(CultureInfo.InvariantCulture);
        }

        public static string ObtenerUltimosDigitos(string numero)
        {
            string normalizado = NormalizarNumero(numero) ?? string.Empty;
            return normalizado.Length >= 4 ? normalizado.Substring(normalizado.Length - 4) : normalizado;
        }

        public static bool CumpleLuhn(string numero)
        {
            int suma = 0;
            bool duplicar = false;
            for (int i = numero.Length - 1; i >= 0; i--)
            {
                int digito = numero[i] - '0';
                if (duplicar)
                {
                    digito *= 2;
                    if (digito > 9)
                    {
                        digito -= 9;
                    }
                }

                suma += digito;
                duplicar = !duplicar;
            }

            return suma % 10 == 0;
        }

        public static string DetectarMarca(string numero)
        {
            if (numero.StartsWith("4", StringComparison.Ordinal) &&
                (numero.Length == 13 || numero.Length == 16 || numero.Length == 19))
            {
                return MarcaVisa;
            }

            if (numero.Length == 16)
            {
                int dos = int.Parse(numero.Substring(0, 2), CultureInfo.InvariantCulture);
                int cuatro = int.Parse(numero.Substring(0, 4), CultureInfo.InvariantCulture);
                if ((dos >= 51 && dos <= 55) || (cuatro >= 2221 && cuatro <= 2720))
                {
                    return MarcaMastercard;
                }
            }

            if (numero.Length == 15 && (numero.StartsWith("34", StringComparison.Ordinal) || numero.StartsWith("37", StringComparison.Ordinal)))
            {
                return MarcaAmex;
            }

            return null;
        }

        private static ResultadoOperacion ValidarVencimiento(int mes, int anio)
        {
            if (anio >= 0 && anio < 100)
            {
                anio += 2000;
            }

            if (mes < 1 || mes > 12 || anio < 2000)
            {
                return ResultadoOperacion.Error("Indicá el vencimiento de la tarjeta (mes y año).");
            }

            // La tarjeta vale hasta el último día del mes de vencimiento.
            DateTime hoy = DateTime.Today;
            DateTime finDeVigencia = new DateTime(anio, mes, 1).AddMonths(1);
            if (finDeVigencia <= hoy)
            {
                return ResultadoOperacion.Error("La tarjeta está vencida.");
            }

            if (anio > hoy.Year + MaximoAniosVigencia)
            {
                return ResultadoOperacion.Error("Revisá el año de vencimiento de la tarjeta.");
            }

            return ResultadoOperacion.Ok();
        }

        private static bool EsSoloDigitos(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return false;
            }

            foreach (char c in texto)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
