using System;
namespace StageUp.BE.Entidades
{
    public class CorreoAdministrativo
    {
        public int IdCorreo { get; set; }
        public string Destinatario { get; set; }
        public string Asunto { get; set; }
        public int? IdReserva { get; set; }
        public int? IdPago { get; set; }
        public int? IdComprobante { get; set; }
        public DateTime FechaIntento { get; set; }
        public DateTime? FechaResultado { get; set; }
        public string Estado { get; set; }
        public string Detalle { get; set; }
    }
    // Implemented by the business layer; Servicios does not depend on persistence.
    public interface IHistorialCorreoAdministrativo
    {
        int Iniciar(CorreoAdministrativo correo);
        void Finalizar(int idCorreo, bool enviado);
    }
}
