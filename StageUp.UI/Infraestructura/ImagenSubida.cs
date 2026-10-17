using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;

namespace StageUp.UI.Infraestructura
{
    // Validación y guardado de una imagen subida desde un formulario.
    //   * Solo JPG o PNG de hasta 3 MB (extensión y contenido real: el
    //     archivo tiene que poder abrirse como imagen de ese formato).
    //   * Se rechazan imágenes de más de 20 megapíxeles.
    //   * Se vuelve a dibujar en un JPG nuevo de 1600 px como máximo: así se
    //     descarta cualquier contenido extra del archivo original (metadatos
    //     o datos ocultos) y el nombre lo genera el sistema (GUID).
    public static class ImagenSubida
    {
        public const int MaximoBytes = 3 * 1024 * 1024;
        private const int LadoMaximo = 1600;
        private const long PixelesMaximos = 20000000;
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png" };

        // Devuelve la ruta virtual del archivo guardado ("~/carpeta/guid.jpg")
        // o null con el motivo en error.
        public static string GuardarComoJpeg(HttpPostedFile archivo, string carpetaVirtual, HttpServerUtility servidor, out string error)
        {
            if (archivo == null)
            {
                error = "Elegí una imagen.";
                return null;
            }

            return GuardarComoJpeg(archivo.InputStream, archivo.FileName, archivo.ContentLength, carpetaVirtual, servidor.MapPath, out error);
        }

        // mapPath convierte la ruta virtual en física (Server.MapPath).
        public static string GuardarComoJpeg(Stream contenido, string nombreArchivo, int bytes, string carpetaVirtual, Func<string, string> mapPath, out string error)
        {
            error = null;
            if (contenido == null || bytes <= 0)
            {
                error = "Elegí una imagen.";
                return null;
            }

            if (bytes > MaximoBytes)
            {
                error = "La imagen no puede superar los 3 MB.";
                return null;
            }

            string extension = (Path.GetExtension(nombreArchivo) ?? string.Empty).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
            {
                error = "La imagen tiene que ser JPG o PNG.";
                return null;
            }

            try
            {
                using (Image original = Image.FromStream(contenido, true, true))
                {
                    if (original.RawFormat.Guid != ImageFormat.Jpeg.Guid && original.RawFormat.Guid != ImageFormat.Png.Guid)
                    {
                        error = "El archivo no es una imagen JPG o PNG válida.";
                        return null;
                    }

                    if ((long)original.Width * original.Height > PixelesMaximos)
                    {
                        error = "La imagen es demasiado grande (más de 20 megapíxeles).";
                        return null;
                    }

                    double escala = Math.Min(1.0, (double)LadoMaximo / Math.Max(original.Width, original.Height));
                    int ancho = Math.Max(1, (int)(original.Width * escala));
                    int alto = Math.Max(1, (int)(original.Height * escala));

                    using (Bitmap imagen = new Bitmap(ancho, alto))
                    {
                        using (Graphics dibujo = Graphics.FromImage(imagen))
                        {
                            dibujo.Clear(Color.White);
                            dibujo.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            dibujo.DrawImage(original, 0, 0, ancho, alto);
                        }

                        string carpeta = carpetaVirtual.EndsWith("/", StringComparison.Ordinal) ? carpetaVirtual : carpetaVirtual + "/";
                        string ruta = carpeta + Guid.NewGuid().ToString("N") + ".jpg";
                        Directory.CreateDirectory(mapPath(carpeta));
                        GuardarJpeg(imagen, mapPath(ruta));
                        return ruta;
                    }
                }
            }
            catch (ArgumentException)
            {
                error = "El archivo no es una imagen JPG o PNG válida.";
                return null;
            }
            catch (OutOfMemoryException)
            {
                // GDI+ informa así los archivos que no puede interpretar.
                error = "El archivo no es una imagen JPG o PNG válida.";
                return null;
            }
            catch (ExternalException)
            {
                error = "No pudimos guardar la imagen. Intentá nuevamente.";
                return null;
            }
            catch (IOException)
            {
                error = "No pudimos guardar la imagen. Intentá nuevamente.";
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                error = "No pudimos guardar la imagen: la carpeta de imágenes no tiene permiso de escritura.";
                return null;
            }
        }

        private static void GuardarJpeg(Bitmap imagen, string rutaFisica)
        {
            ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
            if (codec == null)
            {
                imagen.Save(rutaFisica, ImageFormat.Jpeg);
                return;
            }

            using (EncoderParameters parametros = new EncoderParameters(1))
            {
                parametros.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                imagen.Save(rutaFisica, codec, parametros);
            }
        }
    }
}
