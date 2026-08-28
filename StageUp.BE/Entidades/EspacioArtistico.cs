using System;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Entidad de negocio EspacioArtistico, según el diccionario de datos
    /// de StageUp_Tecnico.docx (10.7.4). Representa el espacio que un
    /// usuario externo habilitado como gestor ofrece dentro de StageUp
    /// (CU-001-007 Gestionar espacios artísticos).
    /// </summary>
    public class EspacioArtistico
    {
        public int IdEspacioArtistico { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string NombreEspacio { get; set; }
        public string Descripcion { get; set; }
        public string TipoEspacio { get; set; }
        public string EstadoEspacio { get; set; }
        public bool Publicado { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
    }
}
