using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace StageUp.BLL
{
    // CU-004-005: comprueba que un archivo subido sea de verdad del tipo que
    // dice su extensión, recorriendo su estructura (no solo los primeros
    // bytes). Rechaza archivos truncados, renombrados o armados a mano.
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
                    case ".png": return EsPng(b);
                    case ".jpg":
                    case ".jpeg": return EsJpeg(b);
                    case ".gif": return EsGif(b);
                    case ".pdf": return EsPdf(b);
                    case ".mp4": return EsMp4(b);
                    case ".docx": return EsOffice(b, "word/document.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml");
                    case ".pptx": return EsOffice(b, "ppt/presentation.xml", "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml");
                    case ".xlsx": return EsOffice(b, "xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                    default: return false;
                }
            }
            catch (Exception)
            {
                // Cualquier estructura que no se pueda recorrer es inválida.
                return false;
            }
        }

        // PNG: firma, IHDR primero (13 bytes, ancho y alto > 0), cada chunk
        // completo y con su CRC correcto, y termina en IEND.
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

        // JPEG: SOI, segmentos con su largo hasta el inicio de los datos
        // (SOS), un marcador de tamaño de imagen (SOFn) y fin EOI.
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

        // GIF: encabezado, pantalla lógica, bloques de imagen y extensiones
        // completos, al menos una imagen y terminador 0x3B.
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

        // PDF: versión al comienzo, al menos un objeto, tabla de referencias
        // (xref o flujo XRef), startxref y %%EOF al final.
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

        // MP4: cajas de primer nivel que cubren exactamente el archivo, la
        // primera "ftyp" y con "moov" (descripción) y "mdat" (datos).
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

        // DOCX, PPTX y XLSX: ZIP completo (directorio central legible), con
        // [Content_Types].xml que declara el documento principal del tipo
        // esperado y esa parte presente.
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

                // La parte principal tiene que poder descomprimirse.
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
