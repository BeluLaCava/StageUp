namespace StageUp.BE.Enumerados
{
    // Tipos de notificación soportados hoy. Cada uno corresponde a un evento
    // real que ya existe en el sistema (no se inventó ningún flujo nuevo
    // para generarlos). Se puede sumar otro tipo más adelante si hace falta.
    public enum TipoNotificacion
    {
        SolicitudReserva,
        ReservaAceptada,
        ReservaRechazada
    }
}
