using System;

namespace StageUp.BE.Entidades
{


    public class PreguntaEspacio
    {
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoRespondida = "Respondida";
        public const string EstadoOculta = "Oculta";
        public const string EstadoEliminada = "Eliminada";

        public int IdPreguntaEspacio { get; set; }
        public int IdEspacioArtistico { get; set; }
        public string NombreEspacio { get; set; }
        public int IdUsuarioGestorEspacio { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string NombreAutor { get; set; }
        public string ApellidoAutor { get; set; }
        public string Pregunta { get; set; }
        public DateTime FechaPregunta { get; set; }
        public string Respuesta { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string Estado { get; set; }
        public string MotivoOcultamiento { get; set; }
        public DateTime? FechaBaja { get; set; }

        public bool EstaRespondida
        {
            get { return Estado == EstadoRespondida; }
        }

        public bool EstaPendiente
        {
            get { return Estado == EstadoPendiente; }
        }


        public string NombreAutorPublico
        {
            get
            {
                string nombre = (NombreAutor ?? string.Empty).Trim();
                string apellido = (ApellidoAutor ?? string.Empty).Trim();
                return apellido.Length == 0 ? nombre : nombre + " " + apellido.Substring(0, 1).ToUpperInvariant() + ".";
            }
        }
    }


    public class PreguntasDeEspacio
    {
        public System.Collections.Generic.List<PreguntaEspacio> Preguntas { get; set; } = new System.Collections.Generic.List<PreguntaEspacio>();
        public int Total { get; set; }
    }
}
