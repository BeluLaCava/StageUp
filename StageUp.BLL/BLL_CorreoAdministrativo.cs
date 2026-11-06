using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;
namespace StageUp.BLL
{
    public class BLL_CorreoAdministrativo : IHistorialCorreoAdministrativo
    {
        public const string PermisoConsultar = "CONSULTAR_TRANSACCIONES";
        private readonly MPP_CorreoAdministrativo _mpp = new MPP_CorreoAdministrativo();
        public static ServicioCorreo CrearServicio()
        {
            return new ServicioCorreo { HistorialAdministrativo = new BLL_CorreoAdministrativo() };
        }
        public int Iniciar(CorreoAdministrativo correo) { return _mpp.Iniciar(correo); }
        public void Finalizar(int idCorreo, bool enviado) { _mpp.Finalizar(idCorreo, enviado); }
        public ResultadoOperacion<List<CorreoAdministrativo>> Listar(string estado, DateTime? desde, DateTime? hasta, int? reserva, string destinatario)
        {
            if (!string.IsNullOrEmpty(estado) && estado != "EnProceso" && estado != "Enviado" && estado != "Fallido")
                return ResultadoOperacion<List<CorreoAdministrativo>>.Error("Elegí un estado válido.");
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                return ResultadoOperacion<List<CorreoAdministrativo>>.Error("La fecha desde no puede ser posterior a la fecha hasta.");
            if (reserva.HasValue && reserva.Value <= 0)
                return ResultadoOperacion<List<CorreoAdministrativo>>.Error("Ingresá un número de reserva válido.");
            if (destinatario != null && destinatario.Length > 300)
                return ResultadoOperacion<List<CorreoAdministrativo>>.Error("El destinatario no puede superar los 300 caracteres.");
            try { return ResultadoOperacion<List<CorreoAdministrativo>>.Ok(_mpp.Listar(string.IsNullOrEmpty(estado) ? null : estado,
                desde, hasta, reserva, string.IsNullOrWhiteSpace(destinatario) ? null : destinatario.Trim())); }
            catch (ErrorAccesoDatosException ex) { return ResultadoOperacion<List<CorreoAdministrativo>>.Error(ex.Message); }
        }
    }
}
