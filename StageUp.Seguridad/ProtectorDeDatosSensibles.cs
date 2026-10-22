using System;

namespace StageUp.Seguridad
{



















    public static class ProtectorDeDatosSensibles
    {



        public static object PrepararTelefonoParaGuardar(string telefonoEnClaro)
        {
            return string.IsNullOrEmpty(telefonoEnClaro)
                ? (object)DBNull.Value
                : EncriptadorSimetrico.Encriptar(telefonoEnClaro);
        }





        public static string RecuperarTelefono(object valorAlmacenado)
        {
            if (valorAlmacenado == null || valorAlmacenado == DBNull.Value)
            {
                return null;
            }

            return EncriptadorSimetrico.DesencriptarOMantener(valorAlmacenado.ToString());
        }
    }
}
