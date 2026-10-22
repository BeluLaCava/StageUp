namespace StageUp.BE.Entidades
{

    public class ImagenValidada
    {
        public const string FormatoJpeg = "JPEG";
        public const string FormatoPng = "PNG";
        public const string FormatoWebP = "WEBP";


        public string Formato { get; set; }



        public int Ancho { get; set; }
        public int Alto { get; set; }



        public byte[] ContenidoLimpio { get; set; }

        public bool EsWebP
        {
            get { return Formato == FormatoWebP; }
        }
    }
}
