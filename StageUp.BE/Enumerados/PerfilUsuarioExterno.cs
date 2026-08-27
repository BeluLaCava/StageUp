namespace StageUp.BE.Enumerados
{
    /// <summary>
    /// Perfil con el que queda habilitado un usuario externo.
    /// Según CU-001-001, todo usuario nuevo queda inicialmente como
    /// ExternoSolicitante; la habilitación como GestorEspacios
    /// corresponde a un proceso posterior, fuera del alcance de este avance.
    /// </summary>
    public enum PerfilUsuarioExterno
    {
        ExternoSolicitante,
        GestorEspacios
    }
}
