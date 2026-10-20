using System;

namespace StageUp.BE.Entidades
{
    // Envío del newsletter de una novedad, con su estado (script 85).
    public class EnvioNewsletter
    {
        public const string EstadoEnCurso = "EnCurso";
        public const string EstadoCompleto = "Completo";
        public const string EstadoParcial = "Parcial";
        public const string EstadoSinDestinatarios = "SinDestinatarios";
        public const string EstadoFallido = "Fallido";
        // Sin fallidos, pero con destinatarios de resultado incierto (el
        // correo pudo haber salido y no se pudo registrar).
        public const string EstadoIncierto = "Incierto";

        // Un envío que quedó EnCurso sin actividad este tiempo se puede retomar.
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

        // Quedan destinatarios sin recibirlo (o el envío se cortó).
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

        // Hay destinatarios con resultado incierto y el envío no está en curso:
        // la administración puede decidir reenviarles.
        public bool PuedeReenviarInciertos
        {
            get { return CantidadInciertos > 0 && (Estado != EstadoEnCurso || EstaTrabado); }
        }

        // Un intento sin destinatarios no cuenta como envío: se puede volver a enviar.
        public bool PermiteNuevoEnvio
        {
            get { return Estado == EstadoSinDestinatarios; }
        }
    }

    // Estado de un destinatario dentro de un envío.
    public class EnvioNewsletterDestinatario
    {
        public const string EstadoPendiente = "Pendiente";
        // Se estaba enviando y no se registró el resultado: incierto.
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

    // Resultado de un envío o reintento.
    public class ResultadoEnvioNewsletter
    {
        public string Estado { get; set; }
        public int CantidadDestinatarios { get; set; }
        public int CantidadEnviados { get; set; }
        public int CantidadFallidos { get; set; }
        public int CantidadOmitidos { get; set; }
        public int CantidadInciertos { get; set; }

        // Correos que salieron en este intento.
        public int EnviadosEnEsteIntento { get; set; }
    }
}
