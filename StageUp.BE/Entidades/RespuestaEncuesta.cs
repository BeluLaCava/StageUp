using System;

namespace StageUp.BE.Entidades
{
    // Cabecera de la respuesta a una encuesta (una sola por respondente y
    // encuesta). La responde un usuario externo con sesión
    // (IdUsuarioExterno) o un visitante sin sesión, identificado por el hash
    // de la cookie de su navegador (IdentificadorAnonimo). Se usa para
    // pasarle al mapper un objeto en vez de ids sueltos.
    public class RespuestaEncuesta
    {
        public int IdRespuestaEncuesta { get; set; }
        public int IdEncuesta { get; set; }
        public int? IdUsuarioExterno { get; set; }
        public string IdentificadorAnonimo { get; set; }
        public DateTime FechaRespuesta { get; set; }
    }

    // Quién está respondiendo o consultando encuestas: un usuario con sesión
    // (con su perfil) o un visitante sin sesión. IdentificadorAnonimo es el
    // hash de la cookie del navegador, si existe (también para un usuario
    // con sesión que antes respondió sin iniciarla).
    public class Encuestado
    {
        public int? IdUsuarioExterno { get; set; }
        public string PerfilUsuario { get; set; }
        public string IdentificadorAnonimo { get; set; }

        public bool EsAnonimo
        {
            get { return !IdUsuarioExterno.HasValue; }
        }
    }
}
