using System;

namespace StageUp.BE.Entidades
{
    // Entidades del módulo de pagos, notas de crédito/débito y cuenta
    // corriente (ítems 5B, 5C, 5D y 6B de la segunda entrega, script 49).

    // Datos de la tarjeta tal como los carga el usuario. Solo viven en memoria
    // durante el pago (validación y autorización): nunca se guardan ni se
    // registran en la bitácora. De la tarjeta se persisten solo la marca, los
    // últimos 4 dígitos y el código de autorización (en Pago). El titular
    // tampoco se guarda.
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
        public string CodigoAutorizacion { get; set; }
        public string MotivoRechazo { get; set; }
        public decimal? PorcentajeComisionPlataforma { get; set; }
        public decimal? ImporteComisionPlataforma { get; set; }
        public DateTime FechaPago { get; set; }

        public string NombreUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string NombreEspacio { get; set; }

        // CU-002-001: lo que informa el medio de pago.
        public string Origen { get; set; }                // StageUp | MedioExterno
        public string IdentificadorExterno { get; set; }
        public string MedioInformado { get; set; }
        public string EstadoInformado { get; set; }
        public DateTime? FechaOperacion { get; set; }
        public string Observacion { get; set; }
        public int? IdUsuarioInternoRegistro { get; set; }
    }

    // CU-002-001 A6 paso 10: detalle completo de un pago (o de la reserva que
    // espera el pago, si todavía no hay un pago registrado).
    public class DetallePagoReserva : Pago
    {
        public int? IdUsuarioGestor { get; set; }
        public string NombreGestor { get; set; }
        public string CorreoGestor { get; set; }
        public string EstadoReserva { get; set; }
        public string EstadoPagoReserva { get; set; }
        public DateTime? FechaSolicitada { get; set; }
        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }
        public DateTime? FechaLimitePago { get; set; }
        public int? IdComprobanteFactura { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime? FechaFactura { get; set; }
        public string EstadoFactura { get; set; }
        public string NombreInternoRegistro { get; set; }
    }

    // CU-002-001 A6: fila del listado "Pagos de reservas".
    public class ItemPagoReserva
    {
        public int? IdPago { get; set; }
        public int? IdReserva { get; set; }
        public string Concepto { get; set; }
        public string Origen { get; set; }
        public string Estado { get; set; }
        public string Grupo { get; set; }                 // Pendiente | Confirmado | Fallido | Cancelado | EnRevision
        public string Moneda { get; set; }
        public decimal ImporteTotal { get; set; }
        public string Medio { get; set; }
        public string IdentificadorExterno { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string NombreEspacio { get; set; }
        public DateTime? FechaLimitePago { get; set; }
    }

    public class FiltroPagosReserva
    {
        public string Grupo { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Texto { get; set; }
    }

    // CU-002-001 pasos 1 y 2: resultado de una operación que informa el medio
    // de pago externo (en StageUp se carga desde la simulación del panel).
    public class AvisoMedioPago
    {
        public int? IdReserva { get; set; }
        public string IdentificadorExterno { get; set; }
        public decimal? Importe { get; set; }
        public string Moneda { get; set; }
        public DateTime? FechaOperacion { get; set; }
        public string Medio { get; set; }
        public string EstadoInformado { get; set; }       // Aprobado | Pendiente | Rechazado | Cancelado
        public string Motivo { get; set; }
    }

    // Ítem 7 (lista del profesor): cómo quiere pagar el cliente.
    public enum ModoPago
    {
        Tarjeta,
        Saldo,
        Combinado
    }

    public class EleccionPago
    {
        public ModoPago Modo { get; set; }
        public decimal ImporteSaldo { get; set; }         // solo para Combinado
    }

    // Lo que se muestra antes de confirmar el pago.
    public class ResumenPago
    {
        public string Moneda { get; set; }
        public decimal Total { get; set; }
        public decimal SaldoDisponible { get; set; }
        public decimal SaldoAUsar { get; set; }
        public decimal ImporteTarjeta { get; set; }
        public decimal SaldoRestante { get; set; }
        public string MedioDescripcion { get; set; }
    }

    // Datos del emisor que se muestran en la factura simulada.
    public class DatosFacturacion
    {
        public string RazonSocial { get; set; }
        public string DatosFiscales { get; set; }
        public string Leyenda { get; set; }
    }

    // Nota de crédito (NC) o de débito (ND).
    public class Comprobante
    {
        public int IdComprobante { get; set; }
        public string Tipo { get; set; }              // NC | ND | FC (factura simulada del pago)
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
