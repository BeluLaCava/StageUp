using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using StageUp.BE.Entidades;

namespace StageUp.UI.Infraestructura
{
    // CU-004-005: archivos de las piezas promocionales. Se guardan en
    // App_Data/ContenidoPromocional (IIS no sirve esa carpeta) con un nombre
    // aleatorio, y solo se descargan desde DescargarContenido.ashx, que pide
    // el permiso. El nombre original queda en la base.
    public static class ArchivoContenidoPromocional
    {
        private const string Carpeta = "~/App_Data/ContenidoPromocional";
        private static readonly Regex PatronNombre = new Regex(@"^[0-9a-f]{32}\.(jpg|png|gif|pdf|mp4|docx|pptx|xlsx)$");

        // Devuelve el nombre con el que quedó guardado.
        public static string Guardar(ArchivoPromocional archivo)
        {
            string carpeta = HostingEnvironment.MapPath(Carpeta);
            Directory.CreateDirectory(carpeta);
            string nombre = Guid.NewGuid().ToString("N") + archivo.Extension;
            File.WriteAllBytes(Path.Combine(carpeta, nombre), archivo.Contenido);
            return nombre;
        }

        // Ruta física de un archivo guardado, o null si el nombre no es uno de
        // los que genera Guardar (evita leer cualquier otro archivo).
        public static string RutaFisica(string nombre)
        {
            if (string.IsNullOrEmpty(nombre) || !PatronNombre.IsMatch(nombre))
            {
                return null;
            }

            string ruta = Path.Combine(HostingEnvironment.MapPath(Carpeta), nombre);
            return File.Exists(ruta) ? ruta : null;
        }

        public static void Borrar(string nombre)
        {
            try
            {
                string ruta = RutaFisica(nombre);
                if (ruta != null)
                {
                    File.Delete(ruta);
                }
            }
            catch (IOException)
            {
                // Si no se puede borrar (por ejemplo, está en uso) queda huérfano;
                // no afecta al contenido.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
