using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Infraestructura
{
    // Validación y guardado de una imagen subida desde un formulario.
    //   * Las reglas (JPG, PNG o WebP de hasta 3 MB y 20 megapíxeles, con
    //     extensión y tipo MIME que coincidan con el contenido real) están en
    //     BLL.ValidadorImagen.
    //   * JPG y PNG se vuelven a dibujar en un JPG nuevo de 1600 px como
    //     máximo: así se descarta cualquier contenido extra del archivo
    //     original (metadatos o datos ocultos).
    //   * WebP no se puede volver a generar en .NET Framework: se guarda tal
    //     cual, rearmado sin metadatos por el validador.
    //   * El nombre lo genera el sistema (GUID).
    public static class ImagenSubida
    {
        public const int MaximoBytes = ValidadorImagen.MaximoBytes;
        private const int LadoMaximo = 1600;

        // Devuelve la ruta virtual del archivo guardado ("~/carpeta/guid.jpg"
        // o ".webp") o null con el motivo en error.
        public static string Guardar(HttpPostedFile archivo, string carpetaVirtual, HttpServerUtility servidor, out string error)
        {
            if (archivo == null)
            {
                error = "Elegí una imagen.";
                return null;
            }

            return Guardar(archivo.InputStream, archivo.FileName, archivo.ContentLength, archivo.ContentType, carpetaVirtual, servidor.MapPath, out error);
        }

        // mapPath convierte la ruta virtual en física (Server.MapPath).
        public static string Guardar(Stream contenido, string nombreArchivo, int bytes, string tipoMime, string carpetaVirtual, Func<string, string> mapPath, out string error)
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

            byte[] datos;
            using (MemoryStream copia = new MemoryStream())
            {
                contenido.CopyTo(copia);
                datos = copia.ToArray();
            }

            ResultadoOperacion<ImagenValidada> validacion = ValidadorImagen.Validar(datos, nombreArchivo, tipoMime);
            if (!validacion.Exitoso)
            {
                error = validacion.Mensaje;
                return null;
            }

            string carpeta = carpetaVirtual.EndsWith("/", StringComparison.Ordinal) ? carpetaVirtual : carpetaVirtual + "/";
            try
            {
                if (validacion.Valor.EsWebP)
                {
                    string rutaWebP = carpeta + Guid.NewGuid().ToString("N") + ".webp";
                    Directory.CreateDirectory(mapPath(carpeta));
                    File.WriteAllBytes(mapPath(rutaWebP), validacion.Valor.ContenidoLimpio);
                    return rutaWebP;
                }

                using (MemoryStream lectura = new MemoryStream(datos))
                using (Image original = Image.FromStream(lectura, true, true))
                {
                    if (original.RawFormat.Guid != ImageFormat.Jpeg.Guid && original.RawFormat.Guid != ImageFormat.Png.Guid)
                    {
                        error = "El archivo no es una imagen JPG o PNG válida.";
                        return null;
                    }

                    if ((long)original.Width * original.Height > ValidadorImagen.PixelesMaximos)
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
