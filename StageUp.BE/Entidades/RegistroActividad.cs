using System;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Entrada de la bitácora del sistema (CU-001-013 Consultar registros
    /// de actividad). Toda acción que impacte en la persistencia de datos
    /// sensibles (registro, activación, login, cambio de contraseña, etc.)
    /// debe quedar registrada acá.
    /// </summary>
    public class RegistroActividad
    {
        public int IdRegistroActividad { get; set; }
        public int? IdUsuarioExternoResponsable { get; set; }
        public int? IdUsuarioInternoResponsable { get; set; }
        public string TipoOperacion { get; set; }
        public string TipoEntidadAfectada { get; set; }
        public int? IdEntidadAfectada { get; set; }
        public string DescripcionOperacion { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string OrigenOperacion { get; set; }

        /// <summary>
        /// Nombre completo y correo del responsable (externo o interno,
        /// según cuál de los dos Id esté cargado). Solo se completan al
        /// consultar/buscar registros (CU-001-013); no se usan al insertar.
        /// </summary>
        public string NombreResponsable { get; set; }
        public string CorreoResponsable { get; set; }
    }
}
