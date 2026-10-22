using System;

namespace StageUp.BE.Entidades
{





    public class RespuestaEncuesta
    {
        public int IdRespuestaEncuesta { get; set; }
        public int IdEncuesta { get; set; }
        public int? IdUsuarioExterno { get; set; }
        public string IdentificadorAnonimo { get; set; }
        public DateTime FechaRespuesta { get; set; }
    }





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
