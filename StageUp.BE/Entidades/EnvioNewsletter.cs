using System;

namespace StageUp.BE.Entidades
{

    public class EnvioNewsletter
    {
        public const string EstadoEnCurso = "EnCurso";
        public const string EstadoCompleto = "Completo";
        public const string EstadoParcial = "Parcial";
        public const string EstadoSinDestinatarios = "SinDestinatarios";
        public const string EstadoFallido = "Fallido";


        public const string EstadoIncierto = "Incierto";


        public const int MinutosEnvioTrabado = 15;

        public int IdEnvioNewsletter { get; set; }
        public int IdNovedad { get; set; }
        public string TituloNovedad { get; set; }
        public string Criterio { get; set; }
        public string Categoria { get; set; }
        public string Estado { get; set; }
        public int CantidadDestinatarios { get; set; }
        public int CantidadEnviados { get; set; }
        public int CantidadFallidos { get; set; }
        public int CantidadOmitidos { get; set; }
        public int CantidadInciertos { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaUltimaActividad { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Responsable { get; set; }


        public bool PuedeReintentar
        {
            get
            {
                return Estado == EstadoParcial || Estado == EstadoFallido || EstaTrabado;
            }
        }

        public bool EstaTrabado
        {
            get { return Estado == EstadoEnCurso && FechaUltimaActividad < DateTime.Now.AddMinutes(-MinutosEnvioTrabado); }
        }



        public bool PuedeReenviarInciertos
        {
            get { return CantidadInciertos > 0 && (Estado != EstadoEnCurso || EstaTrabado); }
        }


        public bool PermiteNuevoEnvio
        {
            get { return Estado == EstadoSinDestinatarios; }
        }
    }


    public class EnvioNewsletterDestinatario
    {
        public const string EstadoPendiente = "Pendiente";

        public const string EstadoEnviando = "Enviando";
        public const string EstadoEnviado = "Enviado";
        public const string EstadoFallido = "Fallido";
        public const string EstadoOmitido = "Omitido";

        public int IdSuscriptorNewsletter { get; set; }
        public string CorreoElectronico { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public int Intentos { get; set; }
        public DateTime? FechaUltimoIntento { get; set; }
        public DateTime? FechaEnvio { get; set; }
    }


    public class ResultadoEnvioNewsletter
    {
        public string Estado { get; set; }
        public int CantidadDestinatarios { get; set; }
        public int CantidadEnviados { get; set; }
        public int CantidadFallidos { get; set; }
        public int CantidadOmitidos { get; set; }
        public int CantidadInciertos { get; set; }


        public int EnviadosEnEsteIntento { get; set; }
    }
}
