using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // Cuenta corriente, notas de crédito/débito y liquidaciones (ítems 5C y
    // 6B de la segunda entrega, script 49).
    //
    // El saldo de una cuenta es la suma de sus movimientos: positivo = a favor
    // del usuario, negativo = le debe a StageUp. Cada moneda es una cuenta
    // aparte y un mismo usuario puede tener cuenta como cliente y como gestor.
    public class BLL_CuentaCorriente
    {
        public const string RolCliente = "Cliente";
        public const string RolGestor = "Gestor";
        public static readonly string[] Monedas = { "ARS", "USD" };

        private const int LongitudMinimaMotivo = 5;
        private const int LongitudMaximaMotivo = 500;
        private const decimal ImporteMaximo = 100000000m;

        private static readonly Dictionary<string, string> EtiquetasMovimiento = new Dictionary<string, string>
        {
            { "CargoReserva", "Cargo por reserva" },
            { "PagoTarjeta", "Pago con tarjeta" },
            { "NotaCredito", "Nota de crédito" },
            { "NotaDebito", "Nota de débito" },
            { "AnulacionComprobante", "Anulación de comprobante" },
            { "IngresoReserva", "Ingreso por reserva" },
            { "ComisionPlataforma", "Comisión StageUp" },
            { "AnulacionIngreso", "Anulación de ingreso" },
            { "AnulacionComision", "Devolución de comisión" },
            { "Liquidacion", "Liquidación" }
        };

        private static readonly string[] TiposCliente = { "CargoReserva", "PagoTarjeta", "NotaCredito", "NotaDebito", "AnulacionComprobante" };
        private static readonly string[] TiposGestor =
            { "IngresoReserva", "ComisionPlataforma", "AnulacionIngreso", "AnulacionComision", "Liquidacion", "NotaCredito", "NotaDebito", "AnulacionComprobante" };

        private readonly MPP_CuentaCorriente _mpp = new MPP_CuentaCorriente();
        private readonly MPP_Comprobante _mppComprobante = new MPP_Comprobante();
        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_Notificacion _notificacion = new BLL_Notificacion();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();

        public static string EtiquetaMovimiento(string tipoMovimiento)
        {
            string etiqueta;
            return EtiquetasMovimiento.TryGetValue(tipoMovimiento ?? string.Empty, out etiqueta) ? etiqueta : tipoMovimiento;
        }

        public static Dictionary<string, string> ObtenerTiposMovimiento(string rolCuenta)
        {
            string[] tipos = rolCuenta == RolGestor ? TiposGestor : TiposCliente;
            return tipos.ToDictionary(t => t, EtiquetaMovimiento);
        }

        public static string FormatearImporte(decimal importe, string moneda)
        {
            return (moneda == "USD" ? "US$ " : "$ ") + importe.ToString("N2", CultureInfo.GetCultureInfo("es-AR"));
        }

        // Acepta "1.234,56", "1234,56" o "1234.56".
        public static bool IntentarLeerImporte(string texto, out decimal importe)
        {
            importe = 0m;
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            string limpio = texto.Trim().Replace("$", string.Empty).Replace(" ", string.Empty);
            if (limpio.Contains(","))
            {
                limpio = limpio.Replace(".", string.Empty).Replace(",", ".");
            }

            return decimal.TryParse(limpio, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out importe);
        }

        public List<SaldoCuentaCorriente> ObtenerSaldos(int idUsuarioExterno, string rolCuenta)
        {
            try
            {
                return _mpp.ObtenerSaldos(new SaldoCuentaCorriente { IdUsuarioExterno = idUsuarioExterno, RolCuenta = NormalizarRol(rolCuenta) });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<SaldoCuentaCorriente>();
            }
        }

        // Saldo a favor del cliente en una moneda (0 si no tiene o si debe).
        public decimal ObtenerSaldoDisponible(int idUsuarioExterno, string moneda)
        {
            SaldoCuentaCorriente saldo = ObtenerSaldos(idUsuarioExterno, RolCliente).FirstOrDefault(s => s.Moneda == moneda);
            return saldo == null ? 0m : Math.Max(0m, saldo.Saldo);
        }

        public ResultadoOperacion<List<MovimientoCuentaCorriente>> ListarMovimientos(FiltroMovimientos filtro)
        {
            if (filtro == null)
            {
                return ResultadoOperacion<List<MovimientoCuentaCorriente>>.Error("Indicá la cuenta a consultar.");
            }

            filtro.RolCuenta = NormalizarRol(filtro.RolCuenta);
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
            {
                return ResultadoOperacion<List<MovimientoCuentaCorriente>>.Error("La fecha desde no puede ser posterior a la fecha hasta.");
            }

            if (!string.IsNullOrEmpty(filtro.Moneda) && !Monedas.Contains(filtro.Moneda))
            {
                filtro.Moneda = null;
            }

            if (!string.IsNullOrEmpty(filtro.TipoMovimiento) && !EtiquetasMovimiento.ContainsKey(filtro.TipoMovimiento))
            {
                filtro.TipoMovimiento = null;
            }

            try
            {
                return ResultadoOperacion<List<MovimientoCuentaCorriente>>.Ok(_mpp.ListarMovimientos(filtro));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<MovimientoCuentaCorriente>>.Error(ex.Message);
            }
        }

        // ---- Administración -------------------------------------------------

        public List<SaldoCuentaCorriente> ListarCuentas(string texto)
        {
            try
            {
                string textoLimpio = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
                if (textoLimpio != null && textoLimpio.Length > 100)
                {
                    textoLimpio = textoLimpio.Substring(0, 100);
                }

                return _mpp.ListarCuentas(new FiltroCuentasCorrientes { Texto = textoLimpio });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<SaldoCuentaCorriente>();
            }
        }

        public ResultadoOperacion<int> EmitirComprobanteManual(
            string tipo, int idUsuarioExterno, string rolCuenta, decimal importe, string moneda,
            string motivo, int? idReserva, int idUsuarioInternoResponsable)
        {
            if (tipo != "NC" && tipo != "ND")
            {
                return ResultadoOperacion<int>.Error("Elegí si es una nota de crédito o de débito.");
            }

            if (rolCuenta != RolCliente && rolCuenta != RolGestor)
            {
                return ResultadoOperacion<int>.Error("Elegí la cuenta (cliente o gestor).");
            }

            ResultadoOperacion validacion = ValidarImporteYMoneda(importe, moneda);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<int>.Error(validacion.Mensaje);
            }

            string motivoLimpio = (motivo ?? string.Empty).Trim();
            if (motivoLimpio.Length < LongitudMinimaMotivo || motivoLimpio.Length > LongitudMaximaMotivo)
            {
                return ResultadoOperacion<int>.Error(
                    "Escribí el motivo del comprobante (entre " + LongitudMinimaMotivo + " y " + LongitudMaximaMotivo + " caracteres).");
            }

            try
            {
                ResultadoMovimientoDinero resultado = _mppComprobante.EmitirManual(new Comprobante
                {
                    Tipo = tipo,
                    IdUsuarioExterno = idUsuarioExterno,
                    RolCuenta = rolCuenta,
                    Importe = decimal.Round(importe, 2),
                    Moneda = moneda,
                    Motivo = motivoLimpio,
                    IdReserva = idReserva,
                    IdUsuarioInternoEmisor = idUsuarioInternoResponsable
                });

                if (resultado.Resultado == "RESERVA_INVALIDA")
                {
                    return ResultadoOperacion<int>.Error("La reserva indicada no corresponde a esa cuenta.");
                }

                if (!resultado.EsOk || !resultado.IdGenerado.HasValue)
                {
                    return ResultadoOperacion<int>.Error("No se pudo emitir el comprobante.");
                }

                string nombreTipo = tipo == "NC" ? "Nota de crédito" : "Nota de débito";
                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", "Comprobante", resultado.IdGenerado,
                    nombreTipo + " manual por " + FormatearImporte(importe, moneda) + " a la cuenta de " + rolCuenta.ToLowerInvariant() +
                    " del usuario N° " + idUsuarioExterno + ". Motivo: " + motivoLimpio);

                AvisarComprobanteEmitido(resultado.IdGenerado.Value);
                return ResultadoOperacion<int>.Ok(resultado.IdGenerado.Value, nombreTipo + " emitida.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<int>.Error(ex.Message);
            }
        }

        public ResultadoOperacion AnularComprobante(int idComprobante, string motivo, int idUsuarioInternoResponsable)
        {
            string motivoLimpio = (motivo ?? string.Empty).Trim();
            if (motivoLimpio.Length < LongitudMinimaMotivo || motivoLimpio.Length > LongitudMaximaMotivo)
            {
                return ResultadoOperacion.Error(
                    "Escribí el motivo de la anulación (entre " + LongitudMinimaMotivo + " y " + LongitudMaximaMotivo + " caracteres).");
            }

            try
            {
                Comprobante comprobante = _mppComprobante.ObtenerPorId(new Comprobante { IdComprobante = idComprobante });
                ResultadoMovimientoDinero resultado = _mppComprobante.Anular(new Comprobante
                {
                    IdComprobante = idComprobante,
                    MotivoAnulacion = motivoLimpio,
                    IdUsuarioInternoAnulacion = idUsuarioInternoResponsable
                });

                if (resultado.Resultado == "NO_ENCONTRADO" || comprobante == null)
                {
                    return ResultadoOperacion.Error("No se encontró el comprobante.");
                }

                if (resultado.Resultado == "NO_ANULABLE")
                {
                    return ResultadoOperacion.Error(
                        "Solo se pueden anular comprobantes manuales que sigan vigentes. Los de una cancelación forman parte de la reserva.");
                }

                if (!resultado.EsOk)
                {
                    return ResultadoOperacion.Error("No se pudo anular el comprobante.");
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ANULACION", "Comprobante", idComprobante,
                    "Anulación de " + comprobante.Numero + " (" + FormatearImporte(comprobante.Importe, comprobante.Moneda) + "). Motivo: " + motivoLimpio);

                _notificacion.Notificar(
                    comprobante.IdUsuarioExterno, TipoNotificacion.ComprobanteEmitido,
                    "Se anuló el comprobante " + comprobante.Numero + " de tu cuenta corriente.",
                    "~/MiCuentaCorriente.aspx");

                return ResultadoOperacion.Ok("Se anuló " + comprobante.Numero + ".");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        public ResultadoOperacion RegistrarLiquidacion(
            int idUsuarioGestor, string moneda, decimal importe, string detalle, int idUsuarioInternoResponsable)
        {
            ResultadoOperacion validacion = ValidarImporteYMoneda(importe, moneda);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            string detalleLimpio = string.IsNullOrWhiteSpace(detalle) ? null : detalle.Trim();
            if (detalleLimpio != null && detalleLimpio.Length > 300)
            {
                return ResultadoOperacion.Error("El detalle no puede superar los 300 caracteres.");
            }

            try
            {
                ResultadoMovimientoDinero resultado = _mpp.RegistrarLiquidacion(new MovimientoCuentaCorriente
                {
                    IdUsuarioExterno = idUsuarioGestor,
                    Moneda = moneda,
                    Importe = decimal.Round(importe, 2),
                    Descripcion = detalleLimpio,
                    IdUsuarioInternoResponsable = idUsuarioInternoResponsable
                });

                if (resultado.Resultado == "SALDO_INSUFICIENTE")
                {
                    return ResultadoOperacion.Error("El importe supera el saldo a favor del gestor en esa moneda.");
                }

                if (!resultado.EsOk)
                {
                    return ResultadoOperacion.Error("No se pudo registrar la liquidación.");
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "LIQUIDACION", "CuentaCorriente", resultado.IdGenerado,
                    "Liquidación de " + FormatearImporte(importe, moneda) + " al gestor N° " + idUsuarioGestor + "." +
                    (detalleLimpio == null ? string.Empty : " " + detalleLimpio));

                _notificacion.Notificar(
                    idUsuarioGestor, TipoNotificacion.LiquidacionRegistrada,
                    "StageUp te liquidó " + FormatearImporte(importe, moneda) + ".",
                    "~/MiCuentaCorriente.aspx?cuenta=Gestor");

                return ResultadoOperacion.Ok("Liquidación registrada.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        // Notificación y mail de un comprobante recién emitido (manual o por
        // la cancelación de una reserva). Nunca hace fallar la operación.
        public void AvisarComprobanteEmitido(int idComprobante)
        {
            try
            {
                Comprobante comprobante = _mppComprobante.ObtenerPorId(new Comprobante { IdComprobante = idComprobante });
                if (comprobante == null)
                {
                    return;
                }

                string nombreTipo = comprobante.Tipo == "NC" ? "una nota de crédito" : "una nota de débito";
                _notificacion.Notificar(
                    comprobante.IdUsuarioExterno, TipoNotificacion.ComprobanteEmitido,
                    "Se emitió " + nombreTipo + " (" + comprobante.Numero + ") por " +
                    FormatearImporte(comprobante.Importe, comprobante.Moneda) + " en tu cuenta corriente.",
                    comprobante.RolCuenta == RolGestor ? "~/MiCuentaCorriente.aspx?cuenta=Gestor" : "~/MiCuentaCorriente.aspx");

                UsuarioExterno usuario = _mppUsuario.ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = comprobante.IdUsuarioExterno });
                if (usuario != null && !string.IsNullOrWhiteSpace(usuario.CorreoElectronico))
                {
                    _servicioCorreo.EnviarComprobanteEmitido(usuario.CorreoElectronico, usuario.Nombre, comprobante);
                }
            }
            catch (Exception)
            {
                // El comprobante ya quedó emitido: el aviso es secundario.
            }
        }

        private static ResultadoOperacion ValidarImporteYMoneda(decimal importe, string moneda)
        {
            if (!Monedas.Contains(moneda))
            {
                return ResultadoOperacion.Error("Elegí la moneda.");
            }

            if (importe <= 0 || importe > ImporteMaximo)
            {
                return ResultadoOperacion.Error("Ingresá un importe mayor a cero.");
            }

            if (decimal.Round(importe, 2) != importe)
            {
                return ResultadoOperacion.Error("El importe puede tener como máximo dos decimales.");
            }

            return ResultadoOperacion.Ok();
        }

        private static string NormalizarRol(string rolCuenta)
        {
            return rolCuenta == RolGestor ? RolGestor : RolCliente;
        }
    }
}
