using System;

namespace StageUp.BE.Entidades
{

    public class ContenidoPromocional
    {
        public const string EstadoActivo = "Activo";
        public const string EstadoEnRevision = "EnRevision";
        public const string EstadoDesactualizado = "Desactualizado";
        public const string EstadoDadoDeBaja = "DadoDeBaja";

        public int IdContenidoPromocional { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string TipoContenido { get; set; }
        public string CanalPublicacion { get; set; }
        public int? IdCampaniaComercial { get; set; }
        public string NombreCampania { get; set; }
        public int? IdAlianzaEstrategica { get; set; }
        public string NombreAlianza { get; set; }
        public string EstadoContenido { get; set; }
        public string UrlContenido { get; set; }
        public string RutaArchivo { get; set; }
        public string NombreArchivoOriginal { get; set; }
        public string TipoMimeArchivo { get; set; }
        public int? TamanioArchivo { get; set; }
        public int? IdUsuarioInternoResponsable { get; set; }
        public string NombreResponsable { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public bool Activo { get; set; }


        public bool QuitarArchivo { get; set; }

        public bool TieneArchivo
        {
            get { return !string.IsNullOrEmpty(RutaArchivo); }
        }

        public bool EsImagen
        {
            get { return TieneArchivo && TipoMimeArchivo != null && TipoMimeArchivo.StartsWith("image/", StringComparison.Ordinal); }
        }
    }

    public class FiltroContenidoPromocional
    {
        public const string VistaVigentes = "Vigentes";
        public const string VistaBajas = "Bajas";
        public const string VistaTodos = "Todos";

        public string Vista { get; set; } = VistaVigentes;
        public string Estado { get; set; }
        public string TipoContenido { get; set; }
        public string CanalPublicacion { get; set; }
        public int? IdCampaniaComercial { get; set; }
        public string Texto { get; set; }

        public bool TieneCriterios
        {
            get
            {
                return Vista != VistaVigentes || Estado != null || TipoContenido != null || CanalPublicacion != null ||
                    IdCampaniaComercial.HasValue || Texto != null;
            }
        }
    }

    public class ResumenContenidos
    {
        public int Total { get; set; }
        public int Activos { get; set; }
        public int EnRevision { get; set; }
        public int Desactualizados { get; set; }
        public int Bajas { get; set; }
    }


    public class ArchivoPromocional
    {
        public string NombreOriginal { get; set; }
        public string Extension { get; set; }
        public string TipoMime { get; set; }
        public byte[] Contenido { get; set; }
    }
}
