using System;
using StageUp.BE.Entidades;

namespace StageUp.Seguridad
{
    // Correccion del profesor: la encriptacion de la contrasena no debe quedar
    // a cargo de la BLL. Antes, BLL_UsuarioExterno/BLL_UsuarioInterno llamaban
    // directo a HashDeContrasenas pasandole el string de la contrasena suelto,
    // y eran ellas mismas las que armaban "usuario.PasswordHash = ...".
    //
    // Con esta clase, la BLL le pasa el objeto usuario completo (mas la
    // contrasena en claro) a Seguridad, y es Seguridad quien decide como
    // protegerla y quien completa la propiedad PasswordHash del objeto. La
    // BLL deja de tener cualquier referencia a HashDeContrasenas: solo conoce
    // "proteger" y "verificar".
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
