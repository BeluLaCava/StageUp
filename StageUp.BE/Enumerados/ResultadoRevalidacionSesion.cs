namespace StageUp.BE.Enumerados
{
    // Resultado de volver a validar una sesión abierta contra la base
    // (por ejemplo, después de restaurar un backup).
    public enum ResultadoRevalidacionSesion
    {
        // La cuenta de la sesión sigue existiendo, es la misma y está activa.
        Valida,
        // La cuenta no está, no está activa o el id es de otra cuenta: la
        // sesión se cierra.
        Invalida,
        // No se pudo consultar la base: la sesión sigue como estaba y se
        // vuelve a intentar en el próximo pedido.
        NoVerificada
    }
}
