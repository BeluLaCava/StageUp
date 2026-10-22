using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using StageUp.BE.Entidades;

namespace StageUp.UI.Infraestructura
{




    public static class ArchivoContenidoPromocional
    {
        private const string Carpeta = "~/App_Data/ContenidoPromocional";
        private static readonly Regex PatronNombre = new Regex(@"^[0-9a-f]{32}\.(jpg|png|gif|pdf|mp4|docx|pptx|xlsx)$");


        public static string Guardar(ArchivoPromocional archivo)
        {
            string carpeta = HostingEnvironment.MapPath(Carpeta);
            Directory.CreateDirectory(carpeta);
            string nombre = Guid.NewGuid().ToString("N") + archivo.Extension;
            File.WriteAllBytes(Path.Combine(carpeta, nombre), archivo.Contenido);
            return nombre;
        }



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


            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
