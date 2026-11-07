using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{

    public class BLL_Comprobante
    {
        private const string NoDisponible = "No encontramos el comprobante o no tenés acceso para consultarlo.";
        public static bool EsNota(string tipo) { return tipo == "NC" || tipo == "ND"; }

        public ResultadoOperacion<Comprobante> ObtenerParaUsuario(int idComprobante, int idUsuario)
        {
            if (idComprobante <= 0 || idUsuario <= 0) return ResultadoOperacion<Comprobante>.Error(NoDisponible);
            try
            {
                var usuario = new MPP_UsuarioExterno().ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = idUsuario });
                if (usuario == null || usuario.EstadoCuenta != EstadoCuentaExterno.Activa.ToString())
                    return ResultadoOperacion<Comprobante>.Error(NoDisponible);
                var nota = new MPP_Comprobante().ObtenerPorId(new Comprobante { IdComprobante = idComprobante });
                if (nota == null || !EsNota(nota.Tipo) || nota.IdUsuarioExterno != idUsuario)
                    return ResultadoOperacion<Comprobante>.Error(NoDisponible);
                return ResultadoOperacion<Comprobante>.Ok(nota);
            }
            catch (ErrorAccesoDatosException) { return ResultadoOperacion<Comprobante>.Error("No pudimos consultar el comprobante. Intentá nuevamente."); }
        }

        public ResultadoOperacion<Comprobante> ObtenerParaInterno(int idComprobante, int idUsuarioInterno)
        {
            if (idComprobante <= 0 || idUsuarioInterno <= 0) return ResultadoOperacion<Comprobante>.Error(NoDisponible);
            try
            {
                List<string> permisos;
                var usuario = new BLL_UsuarioInterno().ObtenerParaSesion(idUsuarioInterno, out permisos);
                if (usuario == null || (!permisos.Contains("GESTIONAR_PAGOS") && !permisos.Contains("CONSULTAR_HISTORIAL_FINANCIERO")))
                    return ResultadoOperacion<Comprobante>.Error(NoDisponible);
                var nota = new MPP_Comprobante().ObtenerPorId(new Comprobante { IdComprobante = idComprobante });
                if (nota == null || !EsNota(nota.Tipo)) return ResultadoOperacion<Comprobante>.Error(NoDisponible);
                return ResultadoOperacion<Comprobante>.Ok(nota);
            }
            catch (ErrorAccesoDatosException) { return ResultadoOperacion<Comprobante>.Error("No pudimos consultar el comprobante. Intentá nuevamente."); }
        }

        public bool PuedeConsultarReserva(int idReserva, int idUsuario)
        {
            if (idReserva <= 0 || idUsuario <= 0) return false;
            try
            {

                var reserva = new MPP_Reserva().ObtenerDetalle(new Reserva { IdReserva = idReserva });
                return reserva != null && (reserva.IdUsuarioExternoSolicitante == idUsuario || reserva.IdUsuarioGestor == idUsuario);
            }
            catch (ErrorAccesoDatosException) { return false; }
        }

        public ResultadoOperacion<List<Comprobante>> ListarNotasDeReservaParaUsuario(int idReserva, int idUsuario)
        {
            if (idReserva <= 0 || idUsuario <= 0) return ResultadoOperacion<List<Comprobante>>.Error(NoDisponible);
            try
            {
                var usuario = new MPP_UsuarioExterno().ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = idUsuario });
                if (usuario == null || usuario.EstadoCuenta != EstadoCuentaExterno.Activa.ToString() ||
                    !PuedeConsultarReserva(idReserva, idUsuario))
                    return ResultadoOperacion<List<Comprobante>>.Error(NoDisponible);

                var notas = new MPP_HistorialFinanciero().ListarComprobantes(new OperacionFinanciera { IdReserva = idReserva })
                    .Where(c => c.IdUsuarioExterno == idUsuario && EsNota(c.Tipo)).ToList();
                return ResultadoOperacion<List<Comprobante>>.Ok(notas);
            }
            catch (ErrorAccesoDatosException) { return ResultadoOperacion<List<Comprobante>>.Error("No pudimos consultar las notas asociadas. Intentá nuevamente."); }
        }
    }
}
