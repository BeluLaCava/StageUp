using System;
using System.Collections.Generic;
using System.Globalization;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // Pago de reservas (ítems 5B y 5D de la segunda entrega, script 49).
    //
    // El cliente paga cuando el gestor acepta la reserva, antes de
    // FechaLimitePago. Puede pagar con tarjeta, con su saldo a favor (notas
    // de crédito) o combinando los dos. La tarjeta se valida acá
    // (ValidadorTarjeta) y la autoriza la pasarela simulada; recién después se
    // registra todo en una sola transacción (sp_Pago_RegistrarReserva).
    public class BLL_Pago
    {
        public const string EstadoNoRequerido = "NoRequerido";
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoPagado = "Pagado";
        public const string EstadoDevuelto = "Devuelto";
        public const string EstadoVencido = "Vencido";

        private readonly MPP_Pago _mpp = new MPP_Pago();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly MPP_UsuarioExterno _mppUsuario = new MPP_UsuarioExterno();
        private readonly BLL_CuentaCorriente _bllCuenta = new BLL_CuentaCorriente();
        private readonly BLL_ParametroPlataforma _bllParametros = new BLL_ParametroPlataforma();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_Notificacion _notificacion = new BLL_Notificacion();
        private readonly ServicioPasarelaPago _pasarela = new ServicioPasarelaPago();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();

        public static bool EsperaPago(Reserva reserva)
        {
            return reserva != null
                && reserva.EstadoReserva == EstadoReserva.Aceptada.ToString()
                && reserva.EstadoPago == EstadoPendiente;
        }

        // Texto corto del estado del pago, para Mis reservas y Solicitudes
        // recibidas. Null si no hay nada que mostrar.
        public static string DescribirEstadoPago(Reserva reserva, bool paraGestor)
        {
            if (reserva == null)
            {
                return null;
            }

            switch (reserva.EstadoPago)
            {
                case EstadoPendiente:
                    if (reserva.EstadoReserva != EstadoReserva.Aceptada.ToString())
                    {
                        return null;
                    }

                    string limite = reserva.FechaLimitePago.HasValue
                        ? " antes del " + reserva.FechaLimitePago.Value.ToString("dd/MM/yyyy 'a las' HH:mm", CultureInfo.InvariantCulture) + " hs"
                        : string.Empty;
                    return paraGestor
                        ? "Esperando el pago del solicitante" + limite + "."
                        : "Pendiente de pago: pagala" + limite + " para confirmarla.";
                case EstadoPagado:
                    return "Pagada" + (reserva.FechaPago.HasValue
                        ? " el " + reserva.FechaPago.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                        : string.Empty) + ".";
                case EstadoDevuelto:
                    return paraGestor
                        ? "Estaba pagada: se le devolvió el importe al solicitante como saldo a favor."
                        : "Lo que pagaste quedó como saldo a favor en tu cuenta corriente.";
                case EstadoVencido:
                    return "Se canceló porque no se pagó a tiempo.";
                default:
                    return null;
            }
        }

        public ResultadoOperacion<ContextoPagoReserva> ObtenerContextoPago(int idReserva, int idUsuarioExterno)
        {
            try
            {
                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReserva });
                ResultadoOperacion validacion = ValidarReservaParaPagar(reserva, idUsuarioExterno);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<ContextoPagoReserva>.Error(validacion.Mensaje);
                }

                return ResultadoOperacion<ContextoPagoReserva>.Ok(new ContextoPagoReserva
                {
                    Reserva = reserva,
                    Total = reserva.ImporteEstimado.Value,
                    SaldoDisponible = _bllCuenta.ObtenerSaldoDisponible(idUsuarioExterno, MonedaDe(reserva))
                });
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<ContextoPagoReserva>.Error(ex.Message);
            }
        }

        // tarjeta puede ser null solo si el saldo a favor cubre todo el importe.
        public ResultadoOperacion<Pago> PagarReserva(int idReserva, int idUsuarioExterno, DatosTarjeta tarjeta, bool usarSaldo)
        {
            try
            {
                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReserva });
                ResultadoOperacion validacion = ValidarReservaParaPagar(reserva, idUsuarioExterno);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<Pago>.Error(validacion.Mensaje);
                }

                string moneda = MonedaDe(reserva);
                decimal total = reserva.ImporteEstimado.Value;
                decimal importeSaldo = usarSaldo
                    ? Math.Min(_bllCuenta.ObtenerSaldoDisponible(idUsuarioExterno, moneda), total)
                    : 0m;
                decimal importeTarjeta = total - importeSaldo;

                Pago pago = new Pago
                {
                    IdUsuarioExterno = idUsuarioExterno,
                    IdReserva = idReserva,
                    Concepto = "Reserva",
                    Moneda = moneda,
                    ImporteTotal = total,
                    ImporteTarjeta = importeTarjeta,
                    ImporteSaldo = importeSaldo,
                    PorcentajeComisionPlataforma = _bllParametros.ObtenerDecimal(BLL_ParametroPlataforma.ComisionPlataformaPorcentaje)
                };

                if (importeTarjeta > 0)
                {
                    ResultadoOperacion autorizacion = AutorizarTarjeta(tarjeta, pago);
                    if (!autorizacion.Exitoso)
                    {
                        return ResultadoOperacion<Pago>.Error(autorizacion.Mensaje);
                    }
                }

                ResultadoMovimientoDinero resultado = _mpp.RegistrarPagoReserva(pago);
                if (!resultado.EsOk || !resultado.IdGenerado.HasValue)
                {
                    // La tarjeta ya estaba autorizada: se anula la autorización
                    // (en la pasarela simulada, no hay nada que revertir) y el
                    // intento queda registrado como no cobrado.
                    RegistrarAutorizacionAnulada(pago, MensajeDeResultado(resultado.Resultado));
                    return ResultadoOperacion<Pago>.Error(MensajeDeResultado(resultado.Resultado) +
                        (importeTarjeta > 0 ? " No se realizó ningún cobro en tu tarjeta." : string.Empty));
                }

                pago.IdPago = resultado.IdGenerado.Value;
                pago.Estado = "Aprobado";
                pago.FechaPago = DateTime.Now;
                pago.ImporteComisionPlataforma = decimal.Round(total * pago.PorcentajeComisionPlataforma.Value / 100m, 2);

                _bitacora.Registrar(
                    idUsuarioExterno, "PAGO", "Pago", pago.IdPago,
                    "Pago de la reserva N° " + idReserva + " (\"" + reserva.NombreEspacio + "\") por " +
                    BLL_CuentaCorriente.FormatearImporte(total, moneda) + DescribirMedios(pago) + ".");

                AvisarPagoAprobado(reserva, pago);

                return ResultadoOperacion<Pago>.Ok(pago, "¡Listo! Recibimos el pago y tu reserva quedó confirmada.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<Pago>.Error(ex.Message);
            }
        }

        // Pago con tarjeta de todo el saldo deudor de la cuenta de cliente en
        // una moneda (por ejemplo, después de una nota de débito).
        public ResultadoOperacion<Pago> PagarDeuda(int idUsuarioExterno, string moneda, DatosTarjeta tarjeta)
        {
            try
            {
                SaldoCuentaCorriente saldo = null;
                foreach (SaldoCuentaCorriente s in _bllCuenta.ObtenerSaldos(idUsuarioExterno, BLL_CuentaCorriente.RolCliente))
                {
                    if (s.Moneda == moneda)
                    {
                        saldo = s;
                    }
                }

                if (saldo == null || saldo.Saldo >= 0)
                {
                    return ResultadoOperacion<Pago>.Error("No tenés saldo deudor en esa moneda.");
                }

                Pago pago = new Pago
                {
                    IdUsuarioExterno = idUsuarioExterno,
                    Concepto = "Deuda",
                    Moneda = moneda,
                    ImporteTotal = -saldo.Saldo,
                    ImporteTarjeta = -saldo.Saldo
                };

                ResultadoOperacion autorizacion = AutorizarTarjeta(tarjeta, pago);
                if (!autorizacion.Exitoso)
                {
                    return ResultadoOperacion<Pago>.Error(autorizacion.Mensaje);
                }

                ResultadoMovimientoDinero resultado = _mpp.PagarDeuda(pago);
                if (!resultado.EsOk || !resultado.IdGenerado.HasValue)
                {
                    RegistrarAutorizacionAnulada(pago, MensajeDeResultado(resultado.Resultado));
                    return ResultadoOperacion<Pago>.Error(MensajeDeResultado(resultado.Resultado) + " No se realizó ningún cobro en tu tarjeta.");
                }

                pago.IdPago = resultado.IdGenerado.Value;
                pago.Estado = "Aprobado";

                _bitacora.Registrar(
                    idUsuarioExterno, "PAGO", "Pago", pago.IdPago,
                    "Pago de saldo deudor por " + BLL_CuentaCorriente.FormatearImporte(pago.ImporteTotal, moneda) + DescribirMedios(pago) + ".");

                return ResultadoOperacion<Pago>.Ok(pago, "Recibimos el pago. Tu cuenta quedó sin deuda en " + moneda + ".");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<Pago>.Error(ex.Message);
            }
        }

        public ResultadoOperacion<List<Pago>> Listar(FiltroPagos filtro)
        {
            if (filtro == null || filtro.Desde.Date > filtro.Hasta.Date)
            {
                return ResultadoOperacion<List<Pago>>.Error("La fecha desde no puede ser posterior a la fecha hasta.");
            }

            if (filtro.Estado != "Aprobado" && filtro.Estado != "Rechazado")
            {
                filtro.Estado = null;
            }

            try
            {
                return ResultadoOperacion<List<Pago>>.Ok(_mpp.Listar(filtro));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<Pago>>.Error(ex.Message);
            }
        }

        public static string DescribirMedios(Pago pago)
        {
            List<string> medios = new List<string>();
            if (pago.ImporteTarjeta > 0)
            {
                medios.Add("tarjeta " + pago.MarcaTarjeta + " terminada en " + pago.UltimosDigitos);
            }

            if (pago.ImporteSaldo > 0)
            {
                medios.Add(BLL_CuentaCorriente.FormatearImporte(pago.ImporteSaldo, pago.Moneda) + " de saldo a favor");
            }

            return medios.Count == 0 ? string.Empty : " con " + string.Join(" y ", medios);
        }

        // Valida la tarjeta y pide la autorización a la pasarela. Si la
        // pasarela la rechaza, el intento queda registrado (sin movimientos).
        // Completa en el pago la marca, los últimos 4 dígitos, el titular y el
        // código de autorización: nunca el número completo ni el código de
        // seguridad.
        private ResultadoOperacion AutorizarTarjeta(DatosTarjeta tarjeta, Pago pago)
        {
            ResultadoOperacion<string> validacion = ValidadorTarjeta.Validar(tarjeta);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion.Error(validacion.Mensaje);
            }

            pago.MarcaTarjeta = validacion.Valor;
            pago.UltimosDigitos = ValidadorTarjeta.ObtenerUltimosDigitos(tarjeta.Numero);
            pago.TitularTarjeta = ValidadorTarjeta.NormalizarTitular(tarjeta.Titular);

            ResultadoAutorizacionPago autorizacion = _pasarela.Autorizar(tarjeta, pago.ImporteTarjeta, pago.Moneda);
            if (!autorizacion.Aprobado)
            {
                pago.MotivoRechazo = autorizacion.MotivoRechazo;
                try
                {
                    _mpp.RegistrarRechazado(pago);
                    _bitacora.Registrar(
                        pago.IdUsuarioExterno, "PAGO", "Pago", null,
                        "Pago rechazado (" + pago.MarcaTarjeta + " terminada en " + pago.UltimosDigitos + "): " + autorizacion.MotivoRechazo);
                }
                catch (ErrorAccesoDatosException)
                {
                }

                return ResultadoOperacion.Error(autorizacion.MotivoRechazo);
            }

            pago.CodigoAutorizacion = autorizacion.CodigoAutorizacion;
            return ResultadoOperacion.Ok();
        }

        private void RegistrarAutorizacionAnulada(Pago pago, string motivo)
        {
            if (pago.ImporteTarjeta <= 0 || string.IsNullOrEmpty(pago.CodigoAutorizacion))
            {
                return;
            }

            try
            {
                pago.MotivoRechazo = "Autorización " + pago.CodigoAutorizacion + " anulada sin cobrar: " + motivo;
                _mpp.RegistrarRechazado(pago);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }

        private void AvisarPagoAprobado(Reserva reserva, Pago pago)
        {
            reserva.EstadoPago = EstadoPagado;
            reserva.FechaPago = pago.FechaPago;

            _notificacion.Notificar(
                reserva.IdUsuarioExternoSolicitante, TipoNotificacion.PagoAprobado,
                "Recibimos el pago de tu reserva para \"" + reserva.NombreEspacio + "\". ¡Está confirmada!",
                "~/MisReservas.aspx");

            decimal neto = pago.ImporteTotal - (pago.ImporteComisionPlataforma ?? 0m);
            _notificacion.Notificar(
                reserva.IdUsuarioGestor, TipoNotificacion.PagoRecibido,
                "Se pagó la reserva N° " + reserva.IdReserva + " de \"" + reserva.NombreEspacio + "\". Te acreditamos " +
                BLL_CuentaCorriente.FormatearImporte(neto, pago.Moneda) + ".",
                "~/MiCuentaCorriente.aspx?cuenta=Gestor");

            try
            {
                UsuarioExterno cliente = _mppUsuario.ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = reserva.IdUsuarioExternoSolicitante });
                if (cliente != null && !string.IsNullOrWhiteSpace(cliente.CorreoElectronico))
                {
                    _servicioCorreo.EnviarPagoAprobado(cliente.CorreoElectronico, cliente.Nombre, reserva, pago);
                }

                UsuarioExterno gestor = _mppUsuario.ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = reserva.IdUsuarioGestor });
                if (gestor != null && !string.IsNullOrWhiteSpace(gestor.CorreoElectronico))
                {
                    _servicioCorreo.EnviarPagoRecibidoGestor(gestor.CorreoElectronico, gestor.Nombre, reserva, neto);
                }
            }
            catch (Exception)
            {
                // El pago ya quedó registrado: un mail que no sale no lo deshace.
            }
        }

        private static ResultadoOperacion ValidarReservaParaPagar(Reserva reserva, int idUsuarioExterno)
        {
            if (reserva == null || reserva.IdUsuarioExternoSolicitante != idUsuarioExterno)
            {
                return ResultadoOperacion.Error("No se encontró la reserva indicada.");
            }

            if (reserva.EstadoPago == EstadoPagado)
            {
                return ResultadoOperacion.Error("Esta reserva ya está pagada.");
            }

            if (!EsperaPago(reserva) || !reserva.ImporteEstimado.HasValue || reserva.ImporteEstimado.Value <= 0)
            {
                return ResultadoOperacion.Error("Esta reserva no tiene un pago pendiente.");
            }

            if (reserva.FechaLimitePago.HasValue && reserva.FechaLimitePago.Value <= DateTime.Now)
            {
                return ResultadoOperacion.Error("Venció el plazo para pagar esta reserva.");
            }

            return ResultadoOperacion.Ok();
        }

        private static string MonedaDe(Reserva reserva)
        {
            return string.IsNullOrWhiteSpace(reserva.Moneda) ? "ARS" : reserva.Moneda;
        }

        private static string MensajeDeResultado(string resultado)
        {
            switch (resultado)
            {
                case "NO_ENCONTRADA":
                    return "No se encontró la reserva indicada.";
                case "NO_PENDIENTE":
                    return "Esta reserva ya no tiene un pago pendiente.";
                case "VENCIDA":
                    return "Venció el plazo para pagar esta reserva.";
                case "SALDO_INSUFICIENTE":
                    return "Tu saldo a favor cambió mientras pagabas. Volvé a intentarlo.";
                case "SIN_DEUDA":
                    return "No tenés saldo deudor en esa moneda.";
                case "IMPORTE_INVALIDO":
                    return "El importe del pago no coincide con el de la reserva.";
                default:
                    return "No se pudo registrar el pago.";
            }
        }
    }
}
