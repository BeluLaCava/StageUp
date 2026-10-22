using System;

namespace StageUp.Seguridad
{
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
