using System;

namespace StageUp.BE.Entidades
{


    public class BackupBaseDatos
    {
        public int IdBackup { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime? Fecha { get; set; }
        public long TamanioBytes { get; set; }
        public string Ruta { get; set; }

        public string NombreArchivo
        {
            get
            {
                if (string.IsNullOrEmpty(Ruta))
                {
                    return string.Empty;
                }

                int barra = Ruta.LastIndexOf('\\');
                return barra >= 0 ? Ruta.Substring(barra + 1) : Ruta;
            }
        }
    }
}
