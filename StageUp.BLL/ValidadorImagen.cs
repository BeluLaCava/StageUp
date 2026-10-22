using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StageUp.BE.Entidades;

namespace StageUp.BLL
{












    public static class ValidadorImagen
    {
        public const int MaximoBytes = 3 * 1024 * 1024;
        public const long PixelesMaximos = 20000000;
        private const int LadoMaximoWebP = 16383;

        public const string MensajeFormatos = "La imagen tiene que ser JPG, PNG o WebP.";

        private static readonly Dictionary<string, string[]> Extensiones = new Dictionary<string, string[]>
        {
            { ImagenValidada.FormatoJpeg, new[] { ".jpg", ".jpeg" } },
            { ImagenValidada.FormatoPng, new[] { ".png" } },
            { ImagenValidada.FormatoWebP, new[] { ".webp" } }
        };

        private static readonly Dictionary<string, string[]> TiposMime = new Dictionary<string, string[]>
        {
            { ImagenValidada.FormatoJpeg, new[] { "image/jpeg", "image/pjpeg", "image/jpg" } },
            { ImagenValidada.FormatoPng, new[] { "image/png", "image/x-png" } },
            { ImagenValidada.FormatoWebP, new[] { "image/webp" } }
        };

        public static bool EsExtensionPermitida(string nombreArchivo)
        {
            string extension = (Path.GetExtension(nombreArchivo ?? string.Empty) ?? string.Empty).ToLowerInvariant();
            return Extensiones.Values.Any(lista => lista.Contains(extension));
        }

        public static ResultadoOperacion<ImagenValidada> Validar(byte[] contenido, string nombreArchivo, string tipoMime)
        {
            if (contenido == null || contenido.Length == 0)
            {
                return ResultadoOperacion<ImagenValidada>.Error("Elegí una imagen.");
            }

            if (contenido.Length > MaximoBytes)
            {
                return ResultadoOperacion<ImagenValidada>.Error("La imagen no puede superar los 3 MB.");
            }

            string extension = (Path.GetExtension(nombreArchivo ?? string.Empty) ?? string.Empty).ToLowerInvariant();
            if (!EsExtensionPermitida(nombreArchivo))
            {
                return ResultadoOperacion<ImagenValidada>.Error(MensajeFormatos);
            }

            string formato = DetectarFormato(contenido);
            if (formato == null)
            {
                return ResultadoOperacion<ImagenValidada>.Error("El archivo no es una imagen JPG, PNG o WebP válida.");
            }

            if (!Extensiones[formato].Contains(extension))
            {
                return ResultadoOperacion<ImagenValidada>.Error(
                    "La extensión del archivo (" + extension + ") no coincide con su contenido, que es una imagen " + Nombre(formato) + ".");
            }

            string mime = NormalizarMime(tipoMime);
            if (!TiposMime[formato].Contains(mime))
            {
                return ResultadoOperacion<ImagenValidada>.Error(
                    "El tipo de archivo informado (" + (mime.Length == 0 ? "sin tipo" : mime) + ") no coincide con su contenido, que es una imagen " +
                    Nombre(formato) + ". Volvé a elegir el archivo original.");
            }

            ImagenValidada imagen = new ImagenValidada { Formato = formato };
            if (formato != ImagenValidada.FormatoWebP)
            {
                return ResultadoOperacion<ImagenValidada>.Ok(imagen);
            }

            string error = AnalizarWebP(contenido, imagen);
            if (error != null)
            {
                return ResultadoOperacion<ImagenValidada>.Error(error);
            }

            if ((long)imagen.Ancho * imagen.Alto > PixelesMaximos)
            {
                return ResultadoOperacion<ImagenValidada>.Error("La imagen es demasiado grande (más de 20 megapíxeles).");
            }

            return ResultadoOperacion<ImagenValidada>.Ok(imagen);
        }


        public static string DetectarFormato(byte[] b)
        {
            if (b == null)
            {
                return null;
            }

            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            {
                return ImagenValidada.FormatoJpeg;
            }

            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 &&
                b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            {
                return ImagenValidada.FormatoPng;
            }

            if (b.Length >= 12 && Ascii(b, 0, "RIFF") && Ascii(b, 8, "WEBP"))
            {
                return ImagenValidada.FormatoWebP;
            }

            return null;
        }

        private static string NormalizarMime(string tipoMime)
        {
            string mime = (tipoMime ?? string.Empty).Trim().ToLowerInvariant();
            int puntoYComa = mime.IndexOf(';');
            return puntoYComa >= 0 ? mime.Substring(0, puntoYComa).Trim() : mime;
        }

        private static string Nombre(string formato)
        {
            return formato == ImagenValidada.FormatoJpeg ? "JPG" : formato == ImagenValidada.FormatoPng ? "PNG" : "WebP";
        }





        private static string AnalizarWebP(byte[] b, ImagenValidada imagen)
        {
            const string invalida = "El archivo no es una imagen WebP válida (está incompleto o dañado).";

            if (b.Length < 20)
            {
                return invalida;
            }

            long tamanioRiff = LeerUInt32(b, 4);
            if (tamanioRiff + 8 != b.Length)
            {
                return invalida;
            }

            List<Bloque> bloques = new List<Bloque>();
            int pos = 12;
            while (pos < b.Length)
            {
                if (pos + 8 > b.Length)
                {
                    return invalida;
                }

                string tipo = System.Text.Encoding.ASCII.GetString(b, pos, 4);
                long tamanio = LeerUInt32(b, pos + 4);
                long fin = pos + 8 + tamanio;
                if (fin > b.Length)
                {
                    return invalida;
                }

                bloques.Add(new Bloque { Tipo = tipo, Inicio = pos + 8, Tamanio = (int)tamanio });
                pos = (int)(fin + (tamanio % 2));
                if (pos > b.Length)
                {
                    return invalida;
                }
            }

            if (bloques.Count == 0)
            {
                return invalida;
            }

            Bloque primero = bloques[0];
            Bloque vp8x = null;
            Bloque alfa = null;
            Bloque flujo;

            if (primero.Tipo == "VP8X")
            {
                vp8x = primero;
                if (vp8x.Tamanio < 10)
                {
                    return invalida;
                }

                byte banderas = b[vp8x.Inicio];
                if ((banderas & 0x02) != 0 || bloques.Any(x => x.Tipo == "ANIM" || x.Tipo == "ANMF"))
                {
                    return "Las imágenes WebP animadas no se admiten. Elegí una imagen fija.";
                }

                flujo = bloques.FirstOrDefault(x => x.Tipo == "VP8 " || x.Tipo == "VP8L");
                if (flujo == null || bloques.Count(x => x.Tipo == "VP8 " || x.Tipo == "VP8L") != 1)
                {
                    return invalida;
                }

                alfa = flujo.Tipo == "VP8 " ? bloques.FirstOrDefault(x => x.Tipo == "ALPH") : null;
                if (alfa != null && bloques.IndexOf(alfa) > bloques.IndexOf(flujo))
                {
                    return invalida;
                }
            }
            else if (primero.Tipo == "VP8 " || primero.Tipo == "VP8L")
            {
                if (bloques.Count != 1)
                {
                    return invalida;
                }

                flujo = primero;
            }
            else
            {
                return invalida;
            }

            int ancho;
            int alto;
            if (!LeerDimensionesFlujo(b, flujo, out ancho, out alto))
            {
                return invalida;
            }

            if (vp8x != null)
            {
                int anchoLienzo = LeerUInt24(b, vp8x.Inicio + 4) + 1;
                int altoLienzo = LeerUInt24(b, vp8x.Inicio + 7) + 1;
                if (anchoLienzo != ancho || altoLienzo != alto)
                {
                    return invalida;
                }
            }

            if (ancho < 1 || alto < 1 || ancho > LadoMaximoWebP || alto > LadoMaximoWebP)
            {
                return invalida;
            }

            imagen.Ancho = ancho;
            imagen.Alto = alto;
            imagen.ContenidoLimpio = Rearmar(b, vp8x, alfa, flujo);
            return null;
        }

        private static bool LeerDimensionesFlujo(byte[] b, Bloque flujo, out int ancho, out int alto)
        {
            ancho = 0;
            alto = 0;
            int i = flujo.Inicio;

            if (flujo.Tipo == "VP8 ")
            {


                if (flujo.Tamanio < 10 || (b[i] & 0x01) != 0 || b[i + 3] != 0x9D || b[i + 4] != 0x01 || b[i + 5] != 0x2A)
                {
                    return false;
                }

                ancho = (b[i + 6] | (b[i + 7] << 8)) & 0x3FFF;
                alto = (b[i + 8] | (b[i + 9] << 8)) & 0x3FFF;
                return true;
            }


            if (flujo.Tamanio < 5 || b[i] != 0x2F)
            {
                return false;
            }

            uint bits = (uint)(b[i + 1] | (b[i + 2] << 8) | (b[i + 3] << 16) | (b[i + 4] << 24));
            if ((bits >> 29) != 0)
            {
                return false;
            }

            ancho = (int)(bits & 0x3FFF) + 1;
            alto = (int)((bits >> 14) & 0x3FFF) + 1;
            return true;
        }



        private static byte[] Rearmar(byte[] b, Bloque vp8x, Bloque alfa, Bloque flujo)
        {
            using (MemoryStream salida = new MemoryStream())
            {
                Escribir(salida, "RIFF");
                EscribirUInt32(salida, 0);
                Escribir(salida, "WEBP");

                if (vp8x != null)
                {
                    byte[] datos = new byte[10];
                    Buffer.BlockCopy(b, vp8x.Inicio, datos, 0, 10);
                    datos[0] = (byte)(datos[0] & 0x10);
                    datos[1] = 0;
                    datos[2] = 0;
                    datos[3] = 0;
                    EscribirBloque(salida, "VP8X", datos, 0, datos.Length);
                }

                if (alfa != null)
                {
                    EscribirBloque(salida, "ALPH", b, alfa.Inicio, alfa.Tamanio);
                }

                EscribirBloque(salida, flujo.Tipo, b, flujo.Inicio, flujo.Tamanio);

                byte[] resultado = salida.ToArray();
                uint tamanioRiff = (uint)(resultado.Length - 8);
                resultado[4] = (byte)tamanioRiff;
                resultado[5] = (byte)(tamanioRiff >> 8);
                resultado[6] = (byte)(tamanioRiff >> 16);
                resultado[7] = (byte)(tamanioRiff >> 24);
                return resultado;
            }
        }

        private static void EscribirBloque(Stream salida, string tipo, byte[] datos, int desde, int largo)
        {
            Escribir(salida, tipo);
            EscribirUInt32(salida, (uint)largo);
            salida.Write(datos, desde, largo);
            if (largo % 2 == 1)
            {
                salida.WriteByte(0);
            }
        }

        private static void Escribir(Stream salida, string ascii)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(ascii);
            salida.Write(bytes, 0, bytes.Length);
        }

        private static void EscribirUInt32(Stream salida, uint valor)
        {
            salida.WriteByte((byte)valor);
            salida.WriteByte((byte)(valor >> 8));
            salida.WriteByte((byte)(valor >> 16));
            salida.WriteByte((byte)(valor >> 24));
        }

        private static long LeerUInt32(byte[] b, int pos)
        {
            return (uint)(b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16) | (b[pos + 3] << 24));
        }

        private static int LeerUInt24(byte[] b, int pos)
        {
            return b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16);
        }

        private static bool Ascii(byte[] b, int pos, string texto)
        {
            for (int i = 0; i < texto.Length; i++)
            {
                if (b[pos + i] != (byte)texto[i])
                {
                    return false;
                }
            }
            return true;
        }

        private class Bloque
        {
            public string Tipo;
            public int Inicio;
            public int Tamanio;
        }
    }
}
