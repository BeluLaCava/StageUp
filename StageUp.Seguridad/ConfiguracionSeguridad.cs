namespace StageUp.Seguridad
{
    /// <summary>
    /// Parámetros de seguridad de la plataforma. 
    /// </summary>
    public static class ConfiguracionSeguridad
    {
        /// <summary>
        /// CU-001-001 no fija un número exacto de minutos para el código de
        /// activación ("vigencia definida por la configuración de la
        /// plataforma"). Se asume 30 minutos.
        /// </summary>
        public const int MinutosVigenciaCodigoActivacion = 30;

        /// <summary>
        /// CU-001-002 sí especifica explícitamente 15 minutos de vigencia.
        /// </summary>
        public const int MinutosVigenciaCodigoRecuperacion = 15;

        /// <summary>
        /// Longitud del código numérico de activación / recuperación.
        /// </summary>
        public const int LongitudCodigo = 6;

        /// <summary>
        /// Longitud mínima de contraseña (mínimo razonable).
        /// </summary>
        public const int LongitudMinimaPassword = 8;
    }
}
