namespace StageUp.BE.Entidades
{
    // Resultado de validar una imagen subida (BLL.ValidadorImagen).
    public class ImagenValidada
    {
        public const string FormatoJpeg = "JPEG";
        public const string FormatoPng = "PNG";
        public const string FormatoWebP = "WEBP";

        // Formato detectado por el contenido real del archivo.
        public string Formato { get; set; }

        // Solo se conocen sin decodificar en WebP (en JPG y PNG las obtiene
        // la UI al volver a dibujar la imagen).
        public int Ancho { get; set; }
        public int Alto { get; set; }

        // WebP: el archivo rearmado sin metadatos (EXIF, XMP, ICC ni bloques
        // desconocidos), listo para guardar tal cual.
        public byte[] ContenidoLimpio { get; set; }

        public bool EsWebP
        {
            get { return Formato == FormatoWebP; }
        }
    }
}
