namespace StageUp.BE.Enumerados
{
    /// <summary>
    /// Estados posibles de la cuenta de un UsuarioExterno, según
    /// CU-001-001 Registrar y activar usuario externo.
    /// El valor persistido en UsuarioExterno.estadoCuenta es el nombre
    /// literal de este enumerado (ver MPP_UsuarioExterno).
    /// </summary>
    public enum EstadoCuentaExterno
    {
        PendienteActivacion,
        Activa,
        Inactiva
    }
}
