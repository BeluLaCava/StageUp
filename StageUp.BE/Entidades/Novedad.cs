using System;

namespace StageUp.BE.Entidades
{


    public class Novedad
    {
        public const string EstadoBorrador = "Borrador";
        public const string EstadoProgramada = "Programada";
        public const string EstadoVigente = "Vigente";
        public const string EstadoVencida = "Vencida";
        public const string EstadoDadaDeBaja = "DadaDeBaja";

        public int IdNovedad { get; set; }
        public string Titulo { get; set; }
        public string Resumen { get; set; }
        public string Contenido { get; set; }
        public string Categoria { get; set; }
        public string UrlImagen { get; set; }
        public bool Publicado { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string Estado { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime? FechaBaja { get; set; }
        public bool EnviadaPorCorreo { get; set; }
        public string DestinatarioNewsletter { get; set; }
        public DateTime? FechaEnvioNewsletter { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
    }
}
