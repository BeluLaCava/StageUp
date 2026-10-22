using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace StageUp.BLL
{







    internal static class ValidadorEstructuraArchivo
    {
        public static bool EsValido(string extension, byte[] b)
        {
            if (b == null || b.Length == 0)
            {
                return false;
            }

            try
            {
                switch (extension)
                {
                    case ".png": return EsPngDecodificable(b) && SeDibuja(b, ImageFormat.Png.Guid);
                    case ".jpg":
                    case ".jpeg": return EsJpeg(b) && SeDibuja(b, ImageFormat.Jpeg.Guid);
                    case ".gif": return EsGif(b) && SeDibuja(b, ImageFormat.Gif.Guid);
                    case ".pdf": return EsPdfValido(b);
                    case ".mp4": return EsMp4Valido(b);
                    case ".docx": return EsOfficeValido(b, "word/document.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml", "document", "wordprocessingml");
                    case ".pptx": return EsOfficeValido(b, "ppt/presentation.xml", "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml", "presentation", "presentationml");
                    case ".xlsx": return EsOfficeValido(b, "xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", "workbook", "spreadsheetml");
                    default: return false;
                }
            }
            catch (Exception)
            {

                return false;
            }
        }



        private static bool EsPng(byte[] b)
        {
            byte[] firma = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (b.Length < 8 + 25 + 12 || !Empieza(b, 0, firma))
            {
                return false;
            }

            int pos = 8;
            bool primero = true;
            bool hayDatos = false;
            while (pos + 12 <= b.Length)
            {
                long largo = Entero32(b, pos);
                string tipo = Encoding.ASCII.GetString(b, pos + 4, 4);
                if (largo < 0 || pos + 12 + largo > b.Length)
                {
                    return false;
                }

                uint crc = (uint)Entero32(b, pos + 8 + (int)largo);
                if (Crc32(b, pos + 4, 4 + (int)largo) != crc)
                {
                    return false;
                }

                if (primero)
                {
                    if (tipo != "IHDR" || largo != 13 || Entero32(b, pos + 8) <= 0 || Entero32(b, pos + 12) <= 0)
                    {
                        return false;
                    }
                    primero = false;
                }

                if (tipo == "IDAT")
                {
                    hayDatos = true;
                }

                pos += 12 + (int)largo;
                if (tipo == "IEND")
                {
                    return hayDatos && largo == 0;
                }
            }

            return false;
        }



        private static bool EsJpeg(byte[] b)
        {
            if (b.Length < 128 || b[0] != 0xFF || b[1] != 0xD8)
            {
                return false;
            }

            int fin = b.Length;
            while (fin > 2 && b[fin - 1] == 0x00)
            {
                fin--;
            }

            if (b[fin - 2] != 0xFF || b[fin - 1] != 0xD9)
            {
                return false;
            }

            int pos = 2;
            bool haySof = false;
            while (pos + 4 <= fin)
            {
                if (b[pos] != 0xFF)
                {
                    return false;
                }

                byte marcador = b[pos + 1];
                if (marcador == 0xFF)
                {
                    pos++;
                    continue;
                }

                int largo = (b[pos + 2] << 8) | b[pos + 3];
                if (largo < 2 || pos + 2 + largo > fin)
                {
                    return false;
                }

                bool esSof = marcador >= 0xC0 && marcador <= 0xCF && marcador != 0xC4 && marcador != 0xC8 && marcador != 0xCC;
                if (esSof)
                {
                    if (largo < 8 || ((b[pos + 5] << 8) | b[pos + 6]) == 0 || ((b[pos + 7] << 8) | b[pos + 8]) == 0)
                    {
                        return false;
                    }
                    haySof = true;
                }

                if (marcador == 0xDA)
                {
                    return haySof && pos + 2 + largo < fin - 2;
                }

                pos += 2 + largo;
            }

            return false;
        }



        private static bool EsGif(byte[] b)
        {
            if (b.Length < 26)
            {
                return false;
            }

            string encabezado = Encoding.ASCII.GetString(b, 0, 6);
            if (encabezado != "GIF87a" && encabezado != "GIF89a")
            {
                return false;
            }

            if ((b[6] | (b[7] << 8)) == 0 || (b[8] | (b[9] << 8)) == 0)
            {
                return false;
            }

            int pos = 13;
            if ((b[10] & 0x80) != 0)
            {
                pos += 3 * (1 << ((b[10] & 0x07) + 1));
            }

            int imagenes = 0;
            while (pos < b.Length)
            {
                byte bloque = b[pos];
                if (bloque == 0x3B)
                {
                    return imagenes > 0;
                }

                if (bloque == 0x21)
                {
                    pos = SaltarSubbloques(b, pos + 2);
                }
                else if (bloque == 0x2C)
                {
                    if (pos + 10 > b.Length)
                    {
                        return false;
                    }

                    byte banderas = b[pos + 9];
                    pos += 10;
                    if ((banderas & 0x80) != 0)
                    {
                        pos += 3 * (1 << ((banderas & 0x07) + 1));
                    }

                    pos = SaltarSubbloques(b, pos + 1);
                    imagenes++;
                }
                else
                {
                    return false;
                }

                if (pos < 0)
                {
                    return false;
                }
            }

            return false;
        }

        private static int SaltarSubbloques(byte[] b, int pos)
        {
            while (pos < b.Length)
            {
                int largo = b[pos];
                pos += 1 + largo;
                if (largo == 0)
                {
                    return pos <= b.Length ? pos : -1;
                }
            }

            return -1;
        }



        private static bool EsPdf(byte[] b)
        {
            if (b.Length < 64)
            {
                return false;
            }

            string inicio = Encoding.ASCII.GetString(b, 0, 8);
            if (!inicio.StartsWith("%PDF-1.", StringComparison.Ordinal) && !inicio.StartsWith("%PDF-2.", StringComparison.Ordinal))
            {
                return false;
            }

            int cola = Math.Min(2048, b.Length);
            string final = Encoding.ASCII.GetString(b, b.Length - cola, cola);
            if (final.LastIndexOf("%%EOF", StringComparison.Ordinal) < 0 || final.LastIndexOf("startxref", StringComparison.Ordinal) < 0)
            {
                return false;
            }

            string todo = Encoding.ASCII.GetString(b);
            return todo.IndexOf(" obj", StringComparison.Ordinal) > 0 &&
                (todo.IndexOf("xref", StringComparison.Ordinal) > 0 || todo.IndexOf("/XRef", StringComparison.Ordinal) > 0);
        }



        private static bool EsMp4(byte[] b)
        {
            long pos = 0;
            bool primera = true;
            bool hayMoov = false;
            bool hayMdat = false;
            while (pos < b.Length)
            {
                if (pos + 8 > b.Length)
                {
                    return false;
                }

                long largo = (uint)Entero32(b, (int)pos);
                string tipo = Encoding.ASCII.GetString(b, (int)pos + 4, 4);
                int cabecera = 8;
                if (largo == 1)
                {
                    if (pos + 16 > b.Length)
                    {
                        return false;
                    }

                    largo = ((long)(uint)Entero32(b, (int)pos + 8) << 32) | (uint)Entero32(b, (int)pos + 12);
                    cabecera = 16;
                }
                else if (largo == 0)
                {
                    largo = b.Length - pos;
                }

                if (largo < cabecera || pos + largo > b.Length)
                {
                    return false;
                }

                if (primera && (tipo != "ftyp" || largo < 16))
                {
                    return false;
                }

                primera = false;
                hayMoov |= tipo == "moov";
                hayMdat |= tipo == "mdat";
                pos += largo;
            }

            return pos == b.Length && hayMoov && hayMdat;
        }




        private static bool EsOffice(byte[] b, string partePrincipal, string tipoContenido)
        {
            if (b.Length < 200 || b[0] != 0x50 || b[1] != 0x4B || b[2] != 0x03 || b[3] != 0x04)
            {
                return false;
            }

            using (MemoryStream flujo = new MemoryStream(b, false))
            using (ZipArchive zip = new ZipArchive(flujo, ZipArchiveMode.Read))
            {
                ZipArchiveEntry tipos = zip.GetEntry("[Content_Types].xml");
                ZipArchiveEntry principal = zip.GetEntry(partePrincipal);
                if (tipos == null || principal == null || tipos.Length > 1024 * 1024 || principal.Length == 0)
                {
                    return false;
                }

                string contenidoTipos;
                using (StreamReader lector = new StreamReader(tipos.Open(), Encoding.UTF8))
                {
                    contenidoTipos = lector.ReadToEnd();
                }

                if (contenidoTipos.IndexOf(tipoContenido, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }


                using (Stream datos = principal.Open())
                {
                    byte[] buffer = new byte[4096];
                    long leidos = 0;
                    int n;
                    while ((n = datos.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        leidos += n;
                        if (leidos > 50L * 1024 * 1024)
                        {
                            return false;
                        }
                    }

                    return leidos > 0;
                }
            }
        }







        private const long PixelesMaximos = 40000000;

        private static bool EsPngDecodificable(byte[] b)
        {
            if (!EsPng(b))
            {
                return false;
            }

            int ancho = Entero32(b, 16);
            int alto = Entero32(b, 20);
            byte profundidad = b[24];
            byte tipoColor = b[25];
            byte entrelazado = b[28];
            if ((long)ancho * alto > PixelesMaximos || b[26] != 0 || b[27] != 0 || entrelazado > 1)
            {
                return false;
            }

            int canales;
            switch (tipoColor)
            {
                case 0: canales = 1; if (profundidad != 1 && profundidad != 2 && profundidad != 4 && profundidad != 8 && profundidad != 16) return false; break;
                case 2: canales = 3; if (profundidad != 8 && profundidad != 16) return false; break;
                case 3: canales = 1; if (profundidad != 1 && profundidad != 2 && profundidad != 4 && profundidad != 8) return false; break;
                case 4: canales = 2; if (profundidad != 8 && profundidad != 16) return false; break;
                case 6: canales = 4; if (profundidad != 8 && profundidad != 16) return false; break;
                default: return false;
            }


            MemoryStream datos = new MemoryStream();
            bool hayPaleta = false;
            int pos = 8;
            while (pos + 12 <= b.Length)
            {
                int largo = Entero32(b, pos);
                string tipo = Encoding.ASCII.GetString(b, pos + 4, 4);
                if (tipo == "PLTE")
                {
                    hayPaleta = largo > 0 && largo % 3 == 0;
                }
                else if (tipo == "IDAT")
                {
                    datos.Write(b, pos + 8, largo);
                }

                pos += 12 + largo;
            }

            if (tipoColor == 3 && !hayPaleta)
            {
                return false;
            }



            int bitsPorPixel = canales * profundidad;
            List<long[]> pasadas = new List<long[]>();
            if (entrelazado == 0)
            {
                pasadas.Add(new long[] { ancho, alto });
            }
            else
            {
                int[,] adam7 = { { 0, 0, 8, 8 }, { 4, 0, 8, 8 }, { 0, 4, 4, 8 }, { 2, 0, 4, 4 }, { 0, 2, 2, 4 }, { 1, 0, 2, 2 }, { 0, 1, 1, 2 } };
                for (int p = 0; p < 7; p++)
                {
                    long w = (ancho - adam7[p, 0] + adam7[p, 2] - 1) / adam7[p, 2];
                    long h = (alto - adam7[p, 1] + adam7[p, 3] - 1) / adam7[p, 3];
                    if (w > 0 && h > 0)
                    {
                        pasadas.Add(new long[] { w, h });
                    }
                }
            }

            datos.Position = 0;
            if (datos.Length < 3)
            {
                return false;
            }


            int cmf = datos.ReadByte();
            int flg = datos.ReadByte();
            if ((cmf & 0x0F) != 8 || ((cmf << 8) + flg) % 31 != 0 || (flg & 0x20) != 0)
            {
                return false;
            }

            using (DeflateStream descomprimido = new DeflateStream(datos, CompressionMode.Decompress))
            {
                foreach (long[] pasada in pasadas)
                {
                    long bytesFila = (pasada[0] * bitsPorPixel + 7) / 8;
                    byte[] fila = new byte[bytesFila + 1];
                    for (long y = 0; y < pasada[1]; y++)
                    {
                        if (!LeerCompleto(descomprimido, fila) || fila[0] > 4)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool LeerCompleto(Stream flujo, byte[] buffer)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int n = flujo.Read(buffer, total, buffer.Length - total);
                if (n <= 0)
                {
                    return false;
                }
                total += n;
            }
            return true;
        }



        private static bool SeDibuja(byte[] b, Guid formatoEsperado)
        {
            using (MemoryStream flujo = new MemoryStream(b, false))
            using (Image imagen = Image.FromStream(flujo, false, true))
            {
                if (imagen.RawFormat.Guid != formatoEsperado || imagen.Width <= 0 || imagen.Height <= 0 ||
                    (long)imagen.Width * imagen.Height > PixelesMaximos)
                {
                    return false;
                }

                using (Bitmap miniatura = new Bitmap(32, 32))
                using (Graphics dibujo = Graphics.FromImage(miniatura))
                {
                    dibujo.DrawImage(imagen, 0, 0, 32, 32);
                }
            }

            return true;
        }








        private static bool EsPdfValido(byte[] b)
        {
            if (!EsPdf(b))
            {
                return false;
            }

            string texto = Encoding.GetEncoding("ISO-8859-1").GetString(b);
            int posStartxref = texto.LastIndexOf("startxref", StringComparison.Ordinal);
            Match numero = Regex.Match(texto.Substring(posStartxref + 9), @"^\s*(\d+)");
            long desplazamiento;
            if (!numero.Success || !long.TryParse(numero.Groups[1].Value, out desplazamiento) || desplazamiento <= 0 || desplazamiento >= b.Length)
            {
                return false;
            }

            string enXref = texto.Substring((int)desplazamiento, (int)Math.Min(4096, b.Length - desplazamiento));
            string diccionarioTrailer;
            if (Regex.IsMatch(enXref, @"^\s*xref\b"))
            {
                if (!ValidarXrefTradicional(texto, (int)desplazamiento))
                {
                    return false;
                }

                int posTrailer = texto.IndexOf("trailer", (int)desplazamiento, StringComparison.Ordinal);
                if (posTrailer < 0)
                {
                    return false;
                }

                diccionarioTrailer = texto.Substring(posTrailer, Math.Min(4096, texto.Length - posTrailer));
            }
            else if (Regex.IsMatch(enXref, @"^\s*\d+\s+\d+\s+obj\b") && Regex.IsMatch(enXref, @"/Type\s*/XRef\b"))
            {
                diccionarioTrailer = enXref;
            }
            else
            {
                return false;
            }

            Match raiz = Regex.Match(diccionarioTrailer, @"/Root\s+(\d+)\s+(\d+)\s+R");
            if (!raiz.Success)
            {
                return false;
            }

            string catalogo = BuscarObjeto(texto, raiz.Groups[1].Value, raiz.Groups[2].Value);
            if (catalogo != null)
            {
                if (!Regex.IsMatch(catalogo, @"/Type\s*/Catalog\b"))
                {
                    return false;
                }

                Match paginas = Regex.Match(catalogo, @"/Pages\s+(\d+)\s+(\d+)\s+R");
                if (!paginas.Success)
                {
                    return false;
                }

                int paginasReales;
                if (ValidarArbolPaginas(texto, paginas.Groups[1].Value, paginas.Groups[2].Value, new HashSet<string>(), out paginasReales))
                {
                    return paginasReales > 0;
                }

                return HayArbolPaginasComprimido(texto, b);
            }

            return HayEnFlujoDeObjetos(texto, b, @"/Type\s*/Catalog\b") && HayArbolPaginasComprimido(texto, b);
        }

        private static bool ValidarXrefTradicional(string texto, int inicio)
        {
            int fin = texto.IndexOf("trailer", inicio, StringComparison.Ordinal);
            if (fin < 0)
            {
                return false;
            }

            string seccion = texto.Substring(inicio, fin - inicio);
            using (StringReader lector = new StringReader(seccion))
            {
                string linea = lector.ReadLine();
                if (linea == null || linea.Trim() != "xref")
                {
                    return false;
                }

                bool hayEntrada = false;
                while ((linea = lector.ReadLine()) != null)
                {
                    linea = linea.Trim();
                    if (linea.Length == 0)
                    {
                        continue;
                    }

                    Match cabecera = Regex.Match(linea, @"^(\d+)\s+(\d+)$");
                    int primero;
                    int cantidad;
                    if (!cabecera.Success || !int.TryParse(cabecera.Groups[1].Value, out primero) ||
                        !int.TryParse(cabecera.Groups[2].Value, out cantidad) || cantidad < 1 || cantidad > 1000000)
                    {
                        return false;
                    }

                    for (int i = 0; i < cantidad; i++)
                    {
                        string entrada = lector.ReadLine();
                        Match dato = Regex.Match(entrada ?? string.Empty, @"^\s*(\d{10})\s+(\d{5})\s+([nf])\b");
                        int offset;
                        int generacion;
                        if (!dato.Success || !int.TryParse(dato.Groups[1].Value, out offset) ||
                            !int.TryParse(dato.Groups[2].Value, out generacion))
                        {
                            return false;
                        }

                        if (dato.Groups[3].Value == "n")
                        {
                            if (!ObjetoEnOffset(texto, offset, primero + i, generacion))
                            {
                                return false;
                            }
                            hayEntrada = true;
                        }
                    }
                }

                return hayEntrada;
            }
        }

        private static bool ObjetoEnOffset(string texto, int offset, int numero, int generacion)
        {
            if (offset < 0 || offset >= texto.Length)
            {
                return false;
            }

            int largo = Math.Min(80, texto.Length - offset);
            return Regex.IsMatch(texto.Substring(offset, largo), @"^\s*" + numero + @"\s+" + generacion + @"\s+obj\b");
        }

        private static bool ValidarArbolPaginas(string texto, string numero, string generacion, HashSet<string> visitados, out int paginas)
        {
            paginas = 0;
            string clave = numero + ":" + generacion;
            if (!visitados.Add(clave) || visitados.Count > 10000)
            {
                return false;
            }

            string objeto = BuscarObjeto(texto, numero, generacion);
            if (objeto == null)
            {
                return false;
            }

            if (Regex.IsMatch(objeto, @"/Type\s*/Page\b(?!s)"))
            {
                if (!Regex.IsMatch(objeto, @"/Parent\s+\d+\s+\d+\s+R"))
                {
                    return false;
                }

                paginas = 1;
                return true;
            }

            if (!Regex.IsMatch(objeto, @"/Type\s*/Pages\b"))
            {
                return false;
            }

            Match cantidadDeclarada = Regex.Match(objeto, @"/Count\s+(\d+)");
            Match hijos = Regex.Match(objeto, @"/Kids\s*\[(?<hijos>[^\]]+)\]", RegexOptions.Singleline);
            int esperadas;
            if (!cantidadDeclarada.Success || !int.TryParse(cantidadDeclarada.Groups[1].Value, out esperadas) ||
                esperadas < 1 || esperadas > 1000000 || !hijos.Success)
            {
                return false;
            }

            MatchCollection referencias = Regex.Matches(hijos.Groups["hijos"].Value, @"(\d+)\s+(\d+)\s+R");
            if (referencias.Count == 0)
            {
                return false;
            }

            int total = 0;
            foreach (Match referencia in referencias)
            {
                int encontradas;
                if (!ValidarArbolPaginas(texto, referencia.Groups[1].Value, referencia.Groups[2].Value, visitados, out encontradas))
                {
                    return false;
                }
                total += encontradas;
                if (total > 1000000)
                {
                    return false;
                }
            }

            paginas = total;
            return total == esperadas;
        }

        private static bool HayArbolPaginasComprimido(string texto, byte[] b)
        {
            return HayEnFlujoDeObjetos(texto, b, @"/Type\s*/Pages\b") &&
                HayEnFlujoDeObjetos(texto, b, @"/Count\s+[1-9]\d*") &&
                HayEnFlujoDeObjetos(texto, b, @"/Kids\s*\[[^\]]*\d+\s+\d+\s+R") &&
                HayEnFlujoDeObjetos(texto, b, @"/Type\s*/Page\b(?!s)");
        }



        private static string BuscarObjeto(string texto, string numero, string generacion)
        {
            Match objeto = Regex.Match(texto, @"(?<![0-9])" + numero + @"\s+" + generacion + @"\s+obj\b");
            if (!objeto.Success)
            {
                return null;
            }

            int fin = texto.IndexOf("endobj", objeto.Index, StringComparison.Ordinal);
            if (fin < 0)
            {
                return null;
            }

            return texto.Substring(objeto.Index, Math.Min(fin - objeto.Index, 65536));
        }

        private static bool HayEnFlujoDeObjetos(string texto, byte[] b, string patron)
        {
            foreach (Match flujo in Regex.Matches(texto, @"/Type\s*/ObjStm[^>]*>>\s*stream\r?\n"))
            {
                int inicio = flujo.Index + flujo.Length;
                int fin = texto.IndexOf("endstream", inicio, StringComparison.Ordinal);
                if (fin < 0 || fin - inicio < 3)
                {
                    continue;
                }

                try
                {
                    using (MemoryStream comprimido = new MemoryStream(b, inicio + 2, fin - inicio - 2, false))
                    using (DeflateStream descomprimido = new DeflateStream(comprimido, CompressionMode.Decompress))
                    using (StreamReader lector = new StreamReader(descomprimido, Encoding.GetEncoding("ISO-8859-1")))
                    {
                        char[] buffer = new char[1024 * 1024];
                        int n = lector.ReadBlock(buffer, 0, buffer.Length);
                        if (Regex.IsMatch(new string(buffer, 0, n), patron))
                        {
                            return true;
                        }
                    }
                }
                catch (InvalidDataException)
                {
                }
            }

            return false;
        }







        private static bool EsMp4Valido(byte[] b)
        {
            if (!EsMp4(b))
            {
                return false;
            }

            List<Caja> raiz = LeerCajas(b, 0, b.Length);
            Caja moov = raiz == null ? null : raiz.Find(c => c.Tipo == "moov");
            List<Caja> mdats = raiz == null ? null : raiz.FindAll(c => c.Tipo == "mdat" && c.LargoDatos > 0);
            if (moov == null || mdats == null || mdats.Count == 0)
            {
                return false;
            }

            List<Caja> enMoov = LeerCajas(b, moov.InicioDatos, moov.FinDatos);
            if (enMoov == null)
            {
                return false;
            }

            Caja mvhd = enMoov.Find(c => c.Tipo == "mvhd");
            if (mvhd == null || mvhd.LargoDatos < 24 || !TieneDuracion(b, mvhd))
            {
                return false;
            }

            foreach (Caja trak in enMoov.FindAll(c => c.Tipo == "trak"))
            {
                if (EsPistaValida(b, trak, mdats))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EsPistaValida(byte[] b, Caja trak, List<Caja> mdats)
        {
            List<Caja> enTrak = LeerCajas(b, trak.InicioDatos, trak.FinDatos);
            Caja tkhd = enTrak == null ? null : enTrak.Find(c => c.Tipo == "tkhd");
            Caja mdia = enTrak == null ? null : enTrak.Find(c => c.Tipo == "mdia");
            if (tkhd == null || tkhd.LargoDatos < 84 || mdia == null)
            {
                return false;
            }

            List<Caja> enMdia = LeerCajas(b, mdia.InicioDatos, mdia.FinDatos);
            Caja mdhd = enMdia == null ? null : enMdia.Find(c => c.Tipo == "mdhd");
            Caja hdlr = enMdia == null ? null : enMdia.Find(c => c.Tipo == "hdlr");
            Caja minf = enMdia == null ? null : enMdia.Find(c => c.Tipo == "minf");
            if (mdhd == null || mdhd.LargoDatos < 24 || !TieneDuracion(b, mdhd) ||
                hdlr == null || hdlr.LargoDatos < 12 || minf == null)
            {
                return false;
            }

            string manejador = Encoding.ASCII.GetString(b, hdlr.InicioDatos + 8, 4);
            if (manejador != "vide" && manejador != "soun")
            {
                return false;
            }

            List<Caja> enMinf = LeerCajas(b, minf.InicioDatos, minf.FinDatos);
            Caja stbl = enMinf == null ? null : enMinf.Find(c => c.Tipo == "stbl");
            List<Caja> enStbl = stbl == null ? null : LeerCajas(b, stbl.InicioDatos, stbl.FinDatos);
            if (enStbl == null)
            {
                return false;
            }

            Caja stsd = enStbl.Find(c => c.Tipo == "stsd");
            Caja stsz = enStbl.Find(c => c.Tipo == "stsz" || c.Tipo == "stz2");
            Caja stts = enStbl.Find(c => c.Tipo == "stts");
            Caja stsc = enStbl.Find(c => c.Tipo == "stsc");
            Caja offsets = enStbl.Find(c => c.Tipo == "stco" || c.Tipo == "co64");
            Muestras muestras = LeerMuestras(b, stsz);
            List<long> desplazamientos = LeerDesplazamientos(b, offsets);
            if (!DescripcionValida(b, stsd, manejador) || muestras == null || desplazamientos == null ||
                !TablaTiempoValida(b, stts, muestras.Cantidad) || !TablaMuestrasPorChunkValida(b, stsc))
            {
                return false;
            }

            foreach (long desplazamiento in desplazamientos)
            {
                Caja datos = mdats.Find(c => desplazamiento >= c.InicioDatos && desplazamiento < c.FinDatos);
                if (datos != null && muestras.Primera > 0 && desplazamiento + muestras.Primera <= datos.FinDatos &&
                    HayDatosNoNulos(b, (int)desplazamiento, (int)muestras.Primera))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TieneDuracion(byte[] b, Caja caja)
        {
            int inicio = caja.InicioDatos;
            if (caja.LargoDatos < 20)
            {
                return false;
            }

            if (b[inicio] == 0)
            {
                return caja.LargoDatos >= 20 && (uint)Entero32(b, inicio + 12) > 0 && (uint)Entero32(b, inicio + 16) > 0;
            }

            return b[inicio] == 1 && caja.LargoDatos >= 32 && (uint)Entero32(b, inicio + 20) > 0 && LeerEntero64(b, inicio + 24) > 0;
        }

        private static bool DescripcionValida(byte[] b, Caja stsd, string manejador)
        {
            if (stsd == null || stsd.LargoDatos < 16 || (uint)Entero32(b, stsd.InicioDatos + 4) == 0)
            {
                return false;
            }

            int entrada = stsd.InicioDatos + 8;
            long largo = (uint)Entero32(b, entrada);
            if (largo < 16 || entrada + largo > stsd.FinDatos)
            {
                return false;
            }

            string tipo = Encoding.ASCII.GetString(b, entrada + 4, 4);
            if (manejador == "vide")
            {
                return tipo == "avc1" || tipo == "avc3" || tipo == "hvc1" || tipo == "hev1" ||
                    tipo == "vp08" || tipo == "vp09" || tipo == "av01" || tipo == "mp4v";
            }

            return tipo == "mp4a" || tipo == "ac-3" || tipo == "ec-3" || tipo == "Opus" ||
                tipo == "fLaC" || tipo == "alac";
        }

        private sealed class Muestras
        {
            public int Cantidad;
            public long Primera;
        }

        private static Muestras LeerMuestras(byte[] b, Caja caja)
        {
            if (caja == null || caja.LargoDatos < 12)
            {
                return null;
            }

            uint cantidad = (uint)Entero32(b, caja.InicioDatos + 8);
            if (cantidad == 0 || cantidad > 10000000)
            {
                return null;
            }

            if (caja.Tipo == "stsz")
            {
                uint fijo = (uint)Entero32(b, caja.InicioDatos + 4);
                if (fijo > 0)
                {
                    return new Muestras { Cantidad = (int)cantidad, Primera = fijo };
                }

                long requerido = 12L + cantidad * 4L;
                if (requerido > caja.LargoDatos)
                {
                    return null;
                }

                uint primerTamano = (uint)Entero32(b, caja.InicioDatos + 12);
                return primerTamano == 0 ? null : new Muestras { Cantidad = (int)cantidad, Primera = primerTamano };
            }

            int bits = b[caja.InicioDatos + 7];
            if (bits != 4 && bits != 8 && bits != 16)
            {
                return null;
            }

            long bytesNecesarios = (cantidad * bits + 7L) / 8L;
            if (12L + bytesNecesarios > caja.LargoDatos)
            {
                return null;
            }

            int pos = caja.InicioDatos + 12;
            int primera = bits == 4 ? (b[pos] >> 4) : bits == 8 ? b[pos] : (b[pos] << 8) | b[pos + 1];
            return primera == 0 ? null : new Muestras { Cantidad = (int)cantidad, Primera = primera };
        }

        private static List<long> LeerDesplazamientos(byte[] b, Caja caja)
        {
            if (caja == null || caja.LargoDatos < 12)
            {
                return null;
            }

            uint cantidad = (uint)Entero32(b, caja.InicioDatos + 4);
            int ancho = caja.Tipo == "co64" ? 8 : 4;
            if (cantidad == 0 || cantidad > 1000000 || 8L + cantidad * ancho > caja.LargoDatos)
            {
                return null;
            }

            List<long> resultado = new List<long>((int)cantidad);
            for (int i = 0; i < cantidad; i++)
            {
                int pos = caja.InicioDatos + 8 + i * ancho;
                long valor = ancho == 8 ? LeerEntero64(b, pos) : (uint)Entero32(b, pos);
                if (valor <= 0 || valor >= b.Length)
                {
                    return null;
                }
                resultado.Add(valor);
            }
            return resultado;
        }

        private static bool TablaTiempoValida(byte[] b, Caja caja, int muestras)
        {
            if (caja == null || caja.LargoDatos < 16)
            {
                return false;
            }

            uint entradas = (uint)Entero32(b, caja.InicioDatos + 4);
            if (entradas == 0 || entradas > 1000000 || 8L + entradas * 8L > caja.LargoDatos)
            {
                return false;
            }

            long total = 0;
            for (int i = 0; i < entradas; i++)
            {
                int pos = caja.InicioDatos + 8 + i * 8;
                uint cantidad = (uint)Entero32(b, pos);
                uint duracion = (uint)Entero32(b, pos + 4);
                if (cantidad == 0 || duracion == 0)
                {
                    return false;
                }
                total += cantidad;
                if (total > int.MaxValue)
                {
                    return false;
                }
            }
            return total == muestras;
        }

        private static bool TablaMuestrasPorChunkValida(byte[] b, Caja caja)
        {
            if (caja == null || caja.LargoDatos < 20)
            {
                return false;
            }

            uint entradas = (uint)Entero32(b, caja.InicioDatos + 4);
            if (entradas == 0 || entradas > 1000000 || 8L + entradas * 12L > caja.LargoDatos)
            {
                return false;
            }

            uint anterior = 0;
            for (int i = 0; i < entradas; i++)
            {
                int pos = caja.InicioDatos + 8 + i * 12;
                uint primerChunk = (uint)Entero32(b, pos);
                uint muestras = (uint)Entero32(b, pos + 4);
                uint descripcion = (uint)Entero32(b, pos + 8);
                if ((i == 0 && primerChunk != 1) || primerChunk <= anterior || muestras == 0 || descripcion == 0)
                {
                    return false;
                }
                anterior = primerChunk;
            }
            return true;
        }

        private static bool HayDatosNoNulos(byte[] b, int inicio, int largo)
        {
            if (inicio < 0 || largo < 1 || inicio + (long)largo > b.Length)
            {
                return false;
            }

            for (int i = inicio; i < inicio + largo; i++)
            {
                if (b[i] != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private class Caja
        {
            public string Tipo;
            public int InicioCaja;
            public int InicioDatos;
            public int FinDatos;
            public int LargoDatos { get { return FinDatos - InicioDatos; } }
        }


        private static List<Caja> LeerCajas(byte[] b, int desde, int hasta)
        {
            List<Caja> cajas = new List<Caja>();
            long pos = desde;
            while (pos < hasta)
            {
                if (pos + 8 > hasta)
                {
                    return null;
                }

                long largo = (uint)Entero32(b, (int)pos);
                int cabecera = 8;
                if (largo == 1)
                {
                    if (pos + 16 > hasta)
                    {
                        return null;
                    }

                    largo = ((long)(uint)Entero32(b, (int)pos + 8) << 32) | (uint)Entero32(b, (int)pos + 12);
                    cabecera = 16;
                }
                else if (largo == 0)
                {
                    largo = hasta - pos;
                }

                if (largo < cabecera || pos + largo > hasta)
                {
                    return null;
                }

                cajas.Add(new Caja
                {
                    Tipo = Encoding.ASCII.GetString(b, (int)pos + 4, 4),
                    InicioCaja = (int)pos,
                    InicioDatos = (int)pos + cabecera,
                    FinDatos = (int)(pos + largo)
                });
                pos += largo;
            }

            return cajas;
        }







        private static bool EsOfficeValido(byte[] b, string partePrincipal, string tipoContenido, string raizEsperada, string espacioNombres)
        {
            if (!EsOffice(b, partePrincipal, tipoContenido))
            {
                return false;
            }

            using (MemoryStream flujo = new MemoryStream(b, false))
            using (ZipArchive zip = new ZipArchive(flujo, ZipArchiveMode.Read))
            {
                XDocument tipos = LeerXml(zip.GetEntry("[Content_Types].xml"));
                XDocument relaciones = LeerXml(zip.GetEntry("_rels/.rels"));
                if (tipos == null || relaciones == null || tipos.Root == null || tipos.Root.Name.LocalName != "Types" ||
                    relaciones.Root == null || relaciones.Root.Name.LocalName != "Relationships")
                {
                    return false;
                }

                XElement documento = relaciones.Root.Elements()
                    .FirstOrDefault(r => r.Name.LocalName == "Relationship" &&
                                         ((string)r.Attribute("Type") ?? string.Empty).EndsWith("/officeDocument", StringComparison.Ordinal));
                string destino = documento == null ? null : ((string)documento.Attribute("Target") ?? string.Empty).TrimStart('/');
                if (!string.Equals(destino, partePrincipal, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                bool declarada = tipos.Root.Elements().Any(o => o.Name.LocalName == "Override" &&
                    string.Equals(((string)o.Attribute("PartName") ?? string.Empty).TrimStart('/'), partePrincipal, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string)o.Attribute("ContentType"), tipoContenido, StringComparison.OrdinalIgnoreCase));
                if (!declarada)
                {
                    return false;
                }

                XDocument principal = LeerXml(zip.GetEntry(partePrincipal));
                return principal != null && principal.Root != null &&
                    principal.Root.Name.LocalName == raizEsperada &&
                    principal.Root.Name.NamespaceName.IndexOf(espacioNombres, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }


        private static XDocument LeerXml(ZipArchiveEntry entrada)
        {
            if (entrada == null || entrada.Length == 0 || entrada.Length > 50L * 1024 * 1024)
            {
                return null;
            }

            XmlReaderSettings opciones = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 100L * 1024 * 1024
            };

            try
            {
                using (Stream contenido = entrada.Open())
                using (XmlReader lector = XmlReader.Create(contenido, opciones))
                {
                    return XDocument.Load(lector);
                }
            }
            catch (XmlException)
            {
                return null;
            }
        }

        private static bool Empieza(byte[] b, int desde, byte[] firma)
        {
            if (b.Length < desde + firma.Length)
            {
                return false;
            }

            for (int i = 0; i < firma.Length; i++)
            {
                if (b[desde + i] != firma[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static int Entero32(byte[] b, int pos)
        {
            return (b[pos] << 24) | (b[pos + 1] << 16) | (b[pos + 2] << 8) | b[pos + 3];
        }

        private static long LeerEntero64(byte[] b, int pos)
        {
            ulong alto = (uint)Entero32(b, pos);
            ulong bajo = (uint)Entero32(b, pos + 4);
            ulong valor = (alto << 32) | bajo;
            return valor > long.MaxValue ? -1 : (long)valor;
        }

        private static uint[] _tablaCrc;

        private static uint Crc32(byte[] b, int desde, int largo)
        {
            if (_tablaCrc == null)
            {
                uint[] tabla = new uint[256];
                for (uint n = 0; n < 256; n++)
                {
                    uint c = n;
                    for (int k = 0; k < 8; k++)
                    {
                        c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                    }
                    tabla[n] = c;
                }
                _tablaCrc = tabla;
            }

            uint crc = 0xFFFFFFFF;
            for (int i = desde; i < desde + largo; i++)
            {
                crc = _tablaCrc[(crc ^ b[i]) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFF;
        }
    }
}
