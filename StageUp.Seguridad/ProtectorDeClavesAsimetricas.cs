using System;

namespace StageUp.Seguridad
{
    /// <summary>
    /// Ítems 9 y 10: las claves privadas RSA que usa StageUp se guardan en la
    /// base (tabla ClaveCifrado) cifradas con la clave simétrica de la
    /// configuración privada (AES-256), nunca en claro. Nadie las ve: no se
    /// muestran en pantalla ni se exportan; solo se descifran en memoria en
    /// el momento de usarlas.
    /// </summary>
    public static class ProtectorDeClavesAsimetricas
    {
        public sealed class ParDeClavesProtegido
        {
            public string ClavePublica { get; set; }
            public string ClavePrivadaProtegida { get; set; }
            public string Huella { get; set; }
        }

        public static ParDeClavesProtegido GenerarParProtegido()
        {
            string publica;
            string privada;
            EncriptadorAsimetrico.GenerarParDeClaves(out publica, out privada);
            return new ParDeClavesProtegido
            {
                ClavePublica = publica,
                ClavePrivadaProtegida = EncriptadorSimetrico.Encriptar(privada),
                Huella = EncriptadorAsimetrico.CalcularHuella(publica)
            };
        }

        public static string RecuperarClavePrivada(string clavePrivadaProtegida)
        {
            if (string.IsNullOrEmpty(clavePrivadaProtegida))
            {
                throw new InvalidOperationException("La versión de clave no tiene clave privada guardada.");
            }

            return EncriptadorSimetrico.Desencriptar(clavePrivadaProtegida);
        }
    }
}
