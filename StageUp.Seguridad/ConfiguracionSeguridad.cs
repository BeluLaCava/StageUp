namespace StageUp.Seguridad
{
    /// <summary>
    /// Parámetros de seguridad de la plataforma. Varios de estos valores
    /// (vigencia del código de activación, criterios de contraseña) no
    /// están definidos de forma explícita en la documentación de StageUp
    /// para este avance; se dejaron acá, centralizados y documentados,
    /// como una decisión técnica razonable a validar con la cátedra/cliente
    /// más adelante si hiciera falta ajustarlos.
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
        /// Longitud mínima de contraseña (criterio de seguridad no definido
        /// explícitamente en la documentación; se asume un mínimo razonable).
        /// </summary>
        public const int LongitudMinimaPassword = 8;
    }
}
