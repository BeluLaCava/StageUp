using System;

namespace StageUp.BE.Entidades
{
    // Entidades del módulo de pagos, notas de crédito/débito y cuenta
    // corriente (ítems 5B, 5C, 5D y 6B de la segunda entrega, script 49).

    // Datos de la tarjeta tal como los carga el usuario. Solo viven en memoria
    // durante el pago: nunca se guardan ni se registran en la bitácora. De la
    // tarjeta se persisten solo la marca y los últimos 4 dígitos (en Pago).
    public class DatosTarjeta
    {
        public string Numero { get; set; }
        public string Titular { get; set; }
        public int MesVencimiento { get; set; }
        public int AnioVencimiento { get; set; }
        public string CodigoSeguridad { get; set; }
    }

    public class Pago
    {
        public int IdPago { get; set; }
        public int IdUsuarioExterno { get; set; }
        public int? IdReserva { get; set; }
        public string Concepto { get; set; }          // Reserva | Deuda
        public string Moneda { get; set; }
        public decimal ImporteTotal { get; set; }
        public decimal ImporteTarjeta { get; set; }
        public decimal ImporteSaldo { get; set; }
        public string Estado { get; set; }            // Aprobado | Rechazado
        public string MarcaTarjeta { get; set; }
        public string UltimosDigitos { get; set; }
        public string TitularTarjeta { get; set; }
        public string CodigoAutorizacion { get; set; }
        public string MotivoRechazo { get; set; }
        public decimal? PorcentajeComisionPlataforma { get; set; }
        public decimal? ImporteComisionPlataforma { get; set; }
        public DateTime FechaPago { get; set; }

        public string NombreUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string NombreEspacio { get; set; }
    }

    // Nota de crédito (NC) o de débito (ND).
    public class Comprobante
    {
        public int IdComprobante { get; set; }
        public string Tipo { get; set; }              // NC | ND
        public string Numero { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string RolCuenta { get; set; }         // Cliente | Gestor
        public int? IdReserva { get; set; }
        public decimal Importe { get; set; }
        public string Moneda { get; set; }
        public string Origen { get; set; }            // Cancelacion | PenalidadCancelacion | AjusteManual
        public string Motivo { get; set; }
        public string Estado { get; set; }            // Emitido | Anulado
        public int? IdUsuarioInternoEmisor { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; }

        // Solo para anular: quién lo anula (queda en el movimiento inverso).
        public int? IdUsuarioInternoAnulacion { get; set; }

        public string NombreUsuario { get; set; }
        public string CorreoUsuario { get; set; }
    }

    public class MovimientoCuentaCorriente
    {
        public int IdMovimiento { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string RolCuenta { get; set; }
        public string TipoMovimiento { get; set; }
        public decimal Importe { get; set; }          // + a favor del usuario, - en contra
        public string Moneda { get; set; }
        public string Descripcion { get; set; }
        public int? IdReserva { get; set; }
        public int? IdPago { get; set; }
        public int? IdComprobante { get; set; }
        public int? IdUsuarioInternoResponsable { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public decimal SaldoAcumulado { get; set; }

        public string NumeroComprobante { get; set; }
        public string EstadoComprobante { get; set; }
        public string OrigenComprobante { get; set; }
    }

    // Saldo de una cuenta en una moneda.
    public class SaldoCuentaCorriente
    {
        public int IdUsuarioExterno { get; set; }
        public string RolCuenta { get; set; }
        public string Moneda { get; set; }
        public decimal Saldo { get; set; }
        public int CantidadMovimientos { get; set; }
        public DateTime? UltimoMovimiento { get; set; }

        public string NombreUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string PerfilUsuario { get; set; }
    }

    public class FiltroMovimientos
    {
        public int IdUsuarioExterno { get; set; }
        public string RolCuenta { get; set; }
        public string Moneda { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string TipoMovimiento { get; set; }
    }

    public class FiltroPagos
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string Estado { get; set; }
    }

    public class FiltroCuentasCorrientes
    {
        public string Texto { get; set; }
    }

    // Lo que devuelven los SP de pago/liquidación/comprobantes: un código de
    // resultado (OK, SALDO_INSUFICIENTE, ...) y el id generado.
    public class ResultadoMovimientoDinero
    {
        public string Resultado { get; set; }
        public int? IdGenerado { get; set; }

        public bool EsOk
        {
            get { return Resultado == "OK"; }
        }
    }

    // Resultado de cancelar una reserva (con los comprobantes que se emitieron).
    public class ResultadoCancelacionReserva
    {
        public bool SeCancelo { get; set; }
        public int? IdNotaCredito { get; set; }
        public int? IdNotaDebito { get; set; }
    }

    // Lo que necesita la pantalla de pago de una reserva.
    public class ContextoPagoReserva
    {
        public Reserva Reserva { get; set; }
        public decimal Total { get; set; }
        public decimal SaldoDisponible { get; set; }   // saldo a favor en la moneda de la reserva

        // Cuánto se cubre con saldo si el cliente elige usarlo.
        public decimal ImporteConSaldo
        {
            get { return Math.Min(Math.Max(SaldoDisponible, 0m), Total); }
        }
    }

    public class ParametroPlataforma
    {
        public string Clave { get; set; }
        public string Valor { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
    }
}
