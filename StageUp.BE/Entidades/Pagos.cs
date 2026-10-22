using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{








    public class DatosTarjeta
    {
        public string Numero { get; set; }
        public string Titular { get; set; }
        public int MesVencimiento { get; set; }
        public int AnioVencimiento { get; set; }
        public string CodigoSeguridad { get; set; }
    }




    public class TarjetaGuardada
    {
        public int IdTarjetaGuardada { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string Alias { get; set; }
        public string Marca { get; set; }
        public string UltimosDigitos { get; set; }
        public int MesVencimiento { get; set; }
        public int AnioVencimiento { get; set; }
        public bool Preferida { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaUltimoUso { get; set; }


        public string Huella { get; set; }
        public string Iv { get; set; }
        public string ClaveCifrada { get; set; }
        public string DatosCifrados { get; set; }
        public int? IdClaveCifrado { get; set; }

        public bool EstaVencida
        {
            get { return new DateTime(AnioVencimiento, MesVencimiento, 1).AddMonths(1) <= DateTime.Today; }
        }
    }




    public class TarjetaParaPagar
    {
        public DatosTarjeta Nueva { get; set; }
        public bool GuardarEnBilletera { get; set; }
        public string AliasParaGuardar { get; set; }
        public int? IdTarjetaGuardada { get; set; }
        public string CodigoSeguridad { get; set; }
    }


    public class ClaveCifrado
    {
        public int IdClaveCifrado { get; set; }
        public int Version { get; set; }
        public string ClavePublica { get; set; }
        public string ClavePrivadaProtegida { get; set; }
        public string Huella { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
        public int? IdUsuarioInternoAlta { get; set; }
        public string NombreUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public int? IdUsuarioInternoRetiro { get; set; }
        public string NombreUsuarioRetiro { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public int TarjetasProtegidas { get; set; }
    }

    public class Pago
    {
        public int IdPago { get; set; }
        public int IdUsuarioExterno { get; set; }
        public int? IdReserva { get; set; }
        public string Concepto { get; set; }
        public string Moneda { get; set; }
        public decimal ImporteTotal { get; set; }
        public decimal ImporteTarjeta { get; set; }
        public decimal ImporteSaldo { get; set; }
        public string Estado { get; set; }
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


        public string Origen { get; set; }
        public string IdentificadorExterno { get; set; }
        public string MedioInformado { get; set; }
        public string EstadoInformado { get; set; }
        public DateTime? FechaOperacion { get; set; }
        public string Observacion { get; set; }
        public int? IdUsuarioInternoRegistro { get; set; }
    }



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


    public class ItemPagoReserva
    {
        public int? IdPago { get; set; }
        public int? IdReserva { get; set; }
        public string Concepto { get; set; }
        public string Origen { get; set; }
        public string Estado { get; set; }
        public string Grupo { get; set; }
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



    public class AvisoMedioPago
    {
        public int? IdReserva { get; set; }
        public string IdentificadorExterno { get; set; }
        public decimal? Importe { get; set; }
        public string Moneda { get; set; }
        public DateTime? FechaOperacion { get; set; }
        public string Medio { get; set; }
        public string EstadoInformado { get; set; }
        public string Motivo { get; set; }
    }


    public enum ModoPago
    {
        Tarjeta,
        Saldo,
        Combinado
    }

    public class EleccionPago
    {
        public ModoPago Modo { get; set; }
        public decimal ImporteSaldo { get; set; }
    }


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


    public class DatosFacturacion
    {
        public string RazonSocial { get; set; }
        public string DatosFiscales { get; set; }
        public string Leyenda { get; set; }


        public string Cuit { get; set; }
        public string CondicionIva { get; set; }
        public string Domicilio { get; set; }
        public int PuntoVenta { get; set; }
    }



    public class TransaccionEconomica
    {
        public int IdTransaccion { get; set; }
        public string Codigo { get; set; }
        public string TipoOperacion { get; set; }
        public int? IdReserva { get; set; }
        public int? IdPago { get; set; }
        public int? IdComprobante { get; set; }
        public string Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal? PorcentajeComision { get; set; }
        public decimal? ImporteComision { get; set; }
        public decimal? ImporteGestor { get; set; }
        public decimal? PorcentajeCargo { get; set; }
        public string EstadoPago { get; set; }
        public string ReferenciaPago { get; set; }
        public string EstadoRegistro { get; set; }
        public string Observacion { get; set; }
        public DateTime FechaGeneracion { get; set; }

        public string NombreSolicitante { get; set; }
        public string CorreoSolicitante { get; set; }
        public string NombreGestor { get; set; }
        public string CorreoGestor { get; set; }
        public string NombreEspacio { get; set; }
        public string NumeroFactura { get; set; }
        public string NumeroComprobante { get; set; }


        public DateTime? FechaSolicitada { get; set; }
        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }
        public decimal? ImporteReserva { get; set; }
        public string EstadoReserva { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public string MedioPago { get; set; }
    }

    public class FiltroTransacciones
    {
        public string TipoOperacion { get; set; }
        public string EstadoPago { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Texto { get; set; }
    }




    public class OperacionFinanciera
    {
        public string Origen { get; set; }
        public int IdOrigen { get; set; }
        public string Codigo { get; set; }
        public string TipoOperacion { get; set; }
        public int? IdReserva { get; set; }
        public int? IdPago { get; set; }
        public int? IdComprobante { get; set; }
        public int? IdUsuarioSolicitante { get; set; }
        public int? IdUsuarioGestor { get; set; }
        public int? IdEspacioArtistico { get; set; }
        public string Moneda { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal? PorcentajeComision { get; set; }
        public decimal? ImporteComision { get; set; }
        public decimal? ImporteGestor { get; set; }
        public decimal? PorcentajeCargo { get; set; }
        public decimal? ImporteCargo { get; set; }
        public string EstadoPago { get; set; }
        public string ReferenciaPago { get; set; }
        public string EstadoRegistro { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Observacion { get; set; }

        public string NombreSolicitante { get; set; }
        public string NombreGestor { get; set; }
        public string NombreEspacio { get; set; }
        public string NumeroComprobante { get; set; }
        public string TipoComprobante { get; set; }


        public string CorreoSolicitante { get; set; }
        public string CorreoGestor { get; set; }
        public DateTime? FechaSolicitada { get; set; }
        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }
        public decimal? ImporteReserva { get; set; }
        public string EstadoReserva { get; set; }
        public string EstadoPagoReserva { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public DateTime? FechaLimitePago { get; set; }
        public string EstadoPagoRegistrado { get; set; }
        public string MedioPago { get; set; }
        public decimal? ImporteTarjeta { get; set; }
        public decimal? ImporteSaldo { get; set; }
        public string CodigoAutorizacion { get; set; }
        public List<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();
        public List<MovimientoCuentaCorriente> Movimientos { get; set; } = new List<MovimientoCuentaCorriente>();
        public List<SaldoCuentaCorriente> SaldosGestor { get; set; } = new List<SaldoCuentaCorriente>();
    }

    public class FiltroHistorialFinanciero
    {
        public string TipoOperacion { get; set; }
        public string EstadoPago { get; set; }
        public int? IdUsuarioGestor { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Texto { get; set; }
    }


    public class ResumenHistorialFinanciero
    {
        public string Moneda { get; set; }
        public int Cantidad { get; set; }
        public decimal TotalCobrado { get; set; }
        public decimal TotalComision { get; set; }
        public decimal TotalGestor { get; set; }
        public decimal TotalPendiente { get; set; }
        public decimal TotalLiquidado { get; set; }
    }


    public class Comprobante
    {
        public int IdComprobante { get; set; }
        public string Tipo { get; set; }
        public string Numero { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string RolCuenta { get; set; }
        public int? IdReserva { get; set; }
        public decimal Importe { get; set; }
        public string Moneda { get; set; }
        public string Origen { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
        public int? IdUsuarioInternoEmisor { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; }


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
        public decimal Importe { get; set; }
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

        public string NombreUsuario { get; set; }
    }


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



    public class ResultadoMovimientoDinero
    {
        public string Resultado { get; set; }
        public int? IdGenerado { get; set; }

        public bool EsOk
        {
            get { return Resultado == "OK"; }
        }
    }


    public class ResultadoCancelacionReserva
    {
        public bool SeCancelo { get; set; }
        public int? IdNotaCredito { get; set; }
        public int? IdNotaDebito { get; set; }
    }


    public class ContextoPagoReserva
    {
        public Reserva Reserva { get; set; }
        public decimal Total { get; set; }
        public decimal SaldoDisponible { get; set; }


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


        public string ValorProgramado { get; set; }
        public DateTime? FechaVigenciaProgramada { get; set; }
    }


    public class CambioParametroPlataforma
    {
        public int IdCambio { get; set; }
        public string Clave { get; set; }
        public string ValorAnterior { get; set; }
        public string ValorNuevo { get; set; }
        public DateTime FechaVigencia { get; set; }
        public string Estado { get; set; }
        public int? IdUsuarioInterno { get; set; }
        public string NombreResponsable { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaAplicacion { get; set; }
    }
}
