namespace StageUp.BE.Enumerados
{
    // CU-003-002 / CU-003-003: estados de una solicitud de soporte.
    // Las transiciones permitidas están en BLL_Ticket.
    public enum EstadoTicket
    {
        Pendiente,
        EnProceso,
        Resuelta,
        Cerrada
    }
}
