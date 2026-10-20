using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StageUp.UI.Infraestructura
{
    // Decodifica una WebP completa con el códec de imágenes de Windows (WIC,
    // el mismo que usa el Explorador; viene con Windows 10 y 11). .NET
    // Framework no tiene un decodificador propio de WebP: así se comprueba
    // en el servidor que el archivo es una imagen que realmente se puede
    // abrir, no solo que sus cabeceras sean coherentes.
    //   * Se decodifican todos los píxeles (fila por fila).
    //   * Corre en un hilo STA aparte, con un tiempo máximo.
    //   * Si el servidor no tiene el códec WebP, se informa con faltaCodec
    //     (la imagen se rechaza igual: no se guarda nada sin comprobar).
    internal static class DecodificadorWebP
    {
        private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(20);

        public static bool SePuedeDecodificar(byte[] datos, int anchoEsperado, int altoEsperado, out bool faltaCodec)
        {
            bool resultado = false;
            bool sinCodec = false;

            Thread hilo = new Thread(() =>
            {
                try
                {
                    using (MemoryStream flujo = new MemoryStream(datos, false))
                    {
                        BitmapDecoder decodificador = BitmapDecoder.Create(
                            flujo, BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        if (decodificador.Frames.Count != 1)
                        {
                            return;
                        }

                        BitmapSource cuadro = decodificador.Frames[0];
                        if (cuadro.PixelWidth != anchoEsperado || cuadro.PixelHeight != altoEsperado)
                        {
                            return;
                        }

                        FormatConvertedBitmap convertido = new FormatConvertedBitmap(cuadro, PixelFormats.Bgra32, null, 0);
                        int bytesFila = convertido.PixelWidth * 4;
                        byte[] fila = new byte[bytesFila];
                        for (int y = 0; y < convertido.PixelHeight; y++)
                        {
                            convertido.CopyPixels(new Int32Rect(0, y, convertido.PixelWidth, 1), fila, bytesFila, 0);
                        }

                        resultado = true;
                    }
                }
                catch (NotSupportedException)
                {
                    // "No se encontró un componente de imágenes adecuado".
                    sinCodec = true;
                }
                catch (Exception)
                {
                    // Archivo incompleto o dañado.
                }
            });

            hilo.SetApartmentState(ApartmentState.STA);
            hilo.IsBackground = true;
            hilo.Start();
            if (!hilo.Join(TiempoMaximo))
            {
                faltaCodec = false;
                return false;
            }

            faltaCodec = sinCodec;
            return resultado;
        }
    }
}
