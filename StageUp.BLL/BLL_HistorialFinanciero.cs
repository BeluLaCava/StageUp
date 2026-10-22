using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{







    public class BLL_HistorialFinanciero
    {
        public const string MensajeDetalleNoDisponible = "No es posible acceder al detalle de la operación seleccionada en este momento.";


        private static readonly Dictionary<string, string> Tipos = new Dictionary<string, string>
        {
            { "ReservaAbonada", "Reserva aceptada y abonada" },
            { "CargoCancelacion", "Cargo por cancelación" },
            { "ReservaSinPago", "Reserva aceptada sin pagar" },
            { "PagoNoConfirmado", "Pago de reserva no confirmado" },
            { "PagoDeuda", "Pago de deuda de cuenta corriente" },
            { "Liquidacion", "Liquidación al gestor" },
            { "NotaCreditoManual", "Nota de crédito manual" },
            { "NotaDebitoManual", "Nota de débito manual" }
        };


        private static readonly Dictionary<string, string> Estados = new Dictionary<string, string>
        {
            { "Pagado", "Pagado" },
            { "Cobrado", "Cobrado" },
            { "Pendiente", "Pago pendiente" },
            { "EnRevision", "En revisión" },
            { "Fallido", "Fallido" },
            { "Vencido", "Vencido" },
            { "Devuelto", "Devuelto al solicitante" },
            { "Anulado", "Anulado" },
            { "Liquidado", "Liquidado al gestor" },
            { "Aplicado", "Aplicado en cuenta corriente" }
        };

        private static readonly string[] Origenes = { "TX", "PG", "RS", "LQ", "CP" };

        private readonly MPP_HistorialFinanciero _mpp = new MPP_HistorialFinanciero();
        private readonly BLL_CuentaCorriente _cuentaCorriente = new BLL_CuentaCorriente();

        public static Dictionary<string, string> ObtenerTipos()
        {
            return new Dictionary<string, string>(Tipos);
        }

        public static Dictionary<string, string> ObtenerEstados()
        {
            return Estados.Keys.ToDictionary(e => e, DescribirEstado);
        }



        private static readonly Dictionary<string, string> EstadosConfigurables = new Dictionary<string, string>
        {
            { "Pagado", BLL_ParametroPlataforma.EstadoOperacionPagado },
            { "Cobrado", BLL_ParametroPlataforma.EstadoOperacionCobrado },
            { "Devuelto", BLL_ParametroPlataforma.EstadoOperacionDevuelto },
            { "Pendiente", BLL_ParametroPlataforma.EstadoOperacionPendiente },
            { "Fallido", BLL_ParametroPlataforma.EstadoPagoRechazado },
            { "Vencido", BLL_ParametroPlataforma.EstadoPagoVencido }
        };

        public static string DescribirTipo(string tipo)
        {
            string texto;
            return Tipos.TryGetValue(tipo ?? string.Empty, out texto) ? texto : tipo;
        }

        public static string DescribirEstado(string estado)
        {
            string clave;
            if (EstadosConfigurables.TryGetValue(estado ?? string.Empty, out clave))
            {
                return BLL_ParametroPlataforma.TextoEstado(clave);
            }

            string texto;
            return Estados.TryGetValue(estado ?? string.Empty, out texto) ? texto : estado;
        }

        public static string TextoFacturaPendiente()
        {
            return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoFacturaPendiente);
        }



        public static bool IntentarLeerCodigo(string codigo, out string origen, out int idOrigen)
        {
            origen = null;
            idOrigen = 0;
            string limpio = (codigo ?? string.Empty).Trim().ToUpperInvariant();
            if (limpio.Length < 4 || limpio[2] != '-')
            {
                return false;
            }

            string prefijo = limpio.Substring(0, 2);
            if (!int.TryParse(limpio.Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out idOrigen) || idOrigen <= 0)
            {
                return false;
            }

            origen = prefijo == "NC" || prefijo == "ND" ? "CP" : prefijo;
            return Origenes.Contains(origen);
        }


        public ResultadoOperacion<List<OperacionFinanciera>> Listar(FiltroHistorialFinanciero filtro)
        {
            filtro = filtro ?? new FiltroHistorialFinanciero();
            if (!string.IsNullOrEmpty(filtro.TipoOperacion) && !Tipos.ContainsKey(filtro.TipoOperacion))
            {
                filtro.TipoOperacion = null;
            }

            if (!string.IsNullOrEmpty(filtro.EstadoPago) && !Estados.ContainsKey(filtro.EstadoPago))
            {
                filtro.EstadoPago = null;
            }

            if (filtro.IdUsuarioGestor.HasValue && filtro.IdUsuarioGestor.Value <= 0)
            {
                filtro.IdUsuarioGestor = null;
            }

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
            {
                return ResultadoOperacion<List<OperacionFinanciera>>.Error("La fecha desde no puede ser posterior a la fecha hasta.");
            }

            if (filtro.Texto != null && filtro.Texto.Trim().Length > 100)
            {
                filtro.Texto = filtro.Texto.Trim().Substring(0, 100);
            }

            try
            {
                return ResultadoOperacion<List<OperacionFinanciera>>.Ok(_mpp.Listar(filtro));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<OperacionFinanciera>>.Error(ex.Message);
            }
        }


        public bool NoHayOperaciones()
        {
            try
            {
                return _mpp.Contar() == 0;
            }
            catch (ErrorAccesoDatosException)
            {
                return false;
            }
        }

        public List<UsuarioExterno> ListarGestores()
        {
            try
            {
                return _mpp.ListarGestores();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<UsuarioExterno>();
            }
        }


        public List<SaldoCuentaCorriente> ObtenerSaldosGestor(int idUsuarioGestor)
        {
            return _cuentaCorriente.ObtenerSaldos(idUsuarioGestor, BLL_CuentaCorriente.RolGestor);
        }



        public static List<ResumenHistorialFinanciero> Resumir(IEnumerable<OperacionFinanciera> operaciones)
        {
            return (operaciones ?? Enumerable.Empty<OperacionFinanciera>())
                .GroupBy(o => o.Moneda ?? "ARS")
                .OrderBy(g => g.Key == "ARS" ? 0 : 1)
                .Select(g =>
                {
                    List<OperacionFinanciera> cobradas = g.Where(o => o.EstadoPago == "Pagado" || o.EstadoPago == "Cobrado").ToList();
                    return new ResumenHistorialFinanciero
                    {
                        Moneda = g.Key,
                        Cantidad = g.Count(),
                        TotalCobrado = cobradas.Where(o => o.TipoOperacion != "PagoDeuda").Sum(o => o.MontoTotal),
                        TotalComision = cobradas.Sum(o => o.ImporteComision ?? 0m),
                        TotalGestor = cobradas.Sum(o => o.ImporteGestor ?? 0m),
                        TotalPendiente = g.Where(o => o.EstadoPago == "Pendiente" || o.EstadoPago == "EnRevision").Sum(o => o.MontoTotal),
                        TotalLiquidado = g.Where(o => o.EstadoPago == "Liquidado").Sum(o => o.MontoTotal)
                    };
                })
                .ToList();
        }


        public ResultadoOperacion<OperacionFinanciera> ObtenerDetalle(string codigo)
        {
            string origen;
            int idOrigen;
            if (!IntentarLeerCodigo(codigo, out origen, out idOrigen))
            {
                return ResultadoOperacion<OperacionFinanciera>.Error(MensajeDetalleNoDisponible, "A7");
            }

            try
            {
                OperacionFinanciera detalle = _mpp.ObtenerDetalle(new OperacionFinanciera { Origen = origen, IdOrigen = idOrigen });
                if (detalle == null)
                {
                    return ResultadoOperacion<OperacionFinanciera>.Error(MensajeDetalleNoDisponible, "A7");
                }

                if (detalle.IdReserva.HasValue || detalle.IdPago.HasValue || detalle.IdComprobante.HasValue)
                {
                    detalle.Comprobantes = _mpp.ListarComprobantes(detalle);
                }

                detalle.Movimientos = _mpp.ListarMovimientos(detalle);
                if (detalle.IdUsuarioGestor.HasValue)
                {
                    detalle.SaldosGestor = ObtenerSaldosGestor(detalle.IdUsuarioGestor.Value);
                }

                return ResultadoOperacion<OperacionFinanciera>.Ok(detalle);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<OperacionFinanciera>.Error(MensajeDetalleNoDisponible, "A7");
            }
        }



        public static List<string> EstadosParticulares(OperacionFinanciera o)
        {
            List<string> avisos = new List<string>();
            if (o == null)
            {
                return avisos;
            }

            bool tieneFactura = !string.IsNullOrEmpty(o.NumeroComprobante) ||
                                (o.Comprobantes != null && o.Comprobantes.Any(c => c.Tipo == "FC" && c.Estado != "Anulado"));

            switch (o.EstadoPago)
            {
                case "Pendiente":
                    avisos.Add(o.TipoOperacion == "ReservaSinPago"
                        ? "Pago pendiente: la reserva fue aceptada y el solicitante todavía no pagó" +
                          (o.FechaLimitePago.HasValue ? " (plazo hasta el " + o.FechaLimitePago.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + ")." : ".")
                        : "Pago pendiente: el medio de pago todavía no confirmó la operación.");
                    break;
                case "EnRevision":
                    avisos.Add(o.EstadoPagoRegistrado == "NoAsociado"
                        ? "En revisión: el aviso del medio de pago no se pudo asociar a una reserva."
                        : "En revisión: el aviso del medio de pago quedó observado.");
                    break;
                case "Fallido":
                    avisos.Add("Pago fallido" + (string.IsNullOrEmpty(o.Observacion) ? "." : ": " + o.Observacion.TrimEnd('.') + "."));
                    break;
                case "Vencido":
                    avisos.Add("Venció el plazo para pagar: la reserva se canceló sin cargo.");
                    break;
                case "Devuelto":
                    avisos.Add("La reserva se canceló: el importe volvió al solicitante como saldo a favor y se anuló la comisión.");
                    break;
                case "Anulado":
                    avisos.Add(o.Origen == "CP" ? "El comprobante fue anulado: su efecto en la cuenta corriente se revirtió." : "El pago fue anulado.");
                    break;
            }

            if (o.TipoOperacion == "CargoCancelacion")
            {
                avisos.Add("Cargo por cancelación aplicado" +
                           (o.PorcentajeCargo.HasValue ? " (" + o.PorcentajeCargo.Value.ToString("0.##", CultureInfo.InvariantCulture) + " % del valor de la reserva)." : "."));
            }

            bool correspondeFactura = (o.TipoOperacion == "ReservaAbonada" || o.TipoOperacion == "PagoNoConfirmado" || o.TipoOperacion == "ReservaSinPago") &&
                                      o.EstadoPago != "Fallido" && o.EstadoPago != "Vencido" && o.EstadoPago != "Anulado";
            if (correspondeFactura && !tieneFactura)
            {
                avisos.Add(o.EstadoPago == "Pagado" || o.EstadoPago == "Devuelto"
                    ? "Factura pendiente: el pago está confirmado pero no tiene factura asociada."
                    : "Factura pendiente: se emite cuando se confirme el pago.");
            }

            if (o.EstadoRegistro == "PendienteControl")
            {
                avisos.Add("Pendiente de control administrativo: la transacción se registró con información incompleta.");
            }

            if (o.IdPago.HasValue && string.IsNullOrEmpty(o.ReferenciaPago) && o.TipoOperacion != "CargoCancelacion" &&
                o.TipoOperacion != "PagoDeuda" && (o.ImporteTarjeta ?? 1m) > 0m)
            {
                avisos.Add("Sin referencia informada por el medio de pago.");
            }

            return avisos;
        }
    }
}
