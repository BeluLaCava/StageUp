using System;
using StageUp.BE.Entidades;

namespace StageUp.Seguridad
{










    public static class ProtectorDeCredenciales
    {
        public static void ProtegerPassword(UsuarioExterno oUsuarioExterno, string passwordEnClaro)
        {
            if (oUsuarioExterno == null)
            {
                throw new ArgumentNullException("oUsuarioExterno");
            }

            oUsuarioExterno.PasswordHash = HashDeContrasenas.CrearHash(passwordEnClaro);
        }

        public static bool VerificarPassword(UsuarioExterno oUsuarioExterno, string passwordEnClaro)
        {
            return oUsuarioExterno != null && HashDeContrasenas.Verificar(passwordEnClaro, oUsuarioExterno.PasswordHash);
        }

        public static void ProtegerPassword(UsuarioInterno oUsuarioInterno, string passwordEnClaro)
        {
            if (oUsuarioInterno == null)
            {
                throw new ArgumentNullException("oUsuarioInterno");
            }

            oUsuarioInterno.PasswordHash = HashDeContrasenas.CrearHash(passwordEnClaro);
        }

        public static bool VerificarPassword(UsuarioInterno oUsuarioInterno, string passwordEnClaro)
        {
            return oUsuarioInterno != null && HashDeContrasenas.Verificar(passwordEnClaro, oUsuarioInterno.PasswordHash);
        }
    }
}
