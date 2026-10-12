using System;
using System.Collections.Generic;
using System.Globalization;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-002-002 Gestionar transacciones económicas y comisiones.
    //
    // Cada hecho de negocio con dinero genera una transacción: el pago
    // confirmado de una reserva aceptada (ReservaAbonada) y el cargo por
    // cancelar una reserva aceptada y abonada (CargoCancelacion, 50 % o
    // 100 % según la política). La comisión de StageUp sale del parámetro
    // vigente y se aplica sobre el importe efectivamente cobrado; el resto es
    // del gestor. Una cancelación sin cargo no genera transacción con importe:
    // solo actualiza la de la reserva abonada (A2).
    public class BLL_TransaccionEconomica
    {
        public const string TipoReservaAbonada = "ReservaAbonada";
        public const string TipoCargoCancelacion = "CargoCancelacion";

        public const string EstadoPagado = "Pagado";
        public const string EstadoCobrado = "Cobrado";
        public const string EstadoDevuelto = "Devuelto";
        public const string EstadoPendiente = "Pendiente";

        public const string RegistroCompleto = "Registrada";
        public const string RegistroPendienteControl = "PendienteControl";

        private const string TipoEntidadBitacora = "TransaccionEconomica";

        private readonly MPP_TransaccionEconomica _mpp = new MPP_TransaccionEconomica();
        private readonly MPP_Pago _mppPago = new MPP_Pago();
        private readonly MPP_Reserva _mppReserva = new MPP_Reserva();
        private readonly BLL_ParametroPlataforma _bllParametros = new BLL_ParametroPlataforma();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public static string DescribirTipo(string tipo)
        {
            return tipo == TipoCargoCancelacion ? "Cargo por cancelación"
                : tipo == TipoReservaAbonada ? "Reserva aceptada y abonada"
                : tipo;
        }

        public static string DescribirEstadoPago(string estado)
        {
            switch (estado)
            {
                case EstadoPagado: return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoOperacionPagado);
                case EstadoCobrado: return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoOperacionCobrado);
                case EstadoDevuelto: return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoOperacionDevuelto);
                case EstadoPendiente: return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoOperacionPendiente);
                default: return estado;
            }
        }

        // ------------------------------------------------------------------
        // Escenario principal (pasos 1 a 21): pago confirmado de una reserva.
        // ------------------------------------------------------------------
        public ResultadoOperacion<int> RegistrarPorPagoAprobado(int idPago)
        {
            try
            {
                DetallePagoReserva pago = _mppPago.ObtenerDetalle(new Pago { IdPago = idPago });
                if (pago == null || pago.Estado != "Aprobado")
                {
                    return ResultadoOperacion<int>.Error("No hay un pago confirmado para registrar la transacción.");
                }

                // Paso 3 / A3: información de la operación.
                List<string> faltantes = new List<string>();
                if (!pago.IdReserva.HasValue) faltantes.Add("la reserva");
                if (pago.IdUsuarioExterno <= 0) faltantes.Add("el usuario solicitante");
                if (!pago.IdUsuarioGestor.HasValue) faltantes.Add("el gestor del espacio");
                if (string.IsNullOrEmpty(pago.NombreEspacio)) faltantes.Add("el espacio reservado");
                if (pago.ImporteTotal <= 0) faltantes.Add("el valor de la reserva");

                // Pasos 15 a 18: comisión vigente sobre lo efectivamente cobrado.
                decimal porcentaje = pago.PorcentajeComisionPlataforma ??
                    _bllParametros.ObtenerDecimal(BLL_ParametroPlataforma.ComisionPlataformaPorcentaje);
                decimal comision = decimal.Round(pago.ImporteTotal * porcentaje / 100m, 2);

                List<string> observaciones = new List<string>();
                if (faltantes.Count > 0)
                {
                    observaciones.Add("Información incompleta (falta " + UnirLista(faltantes) + "): pendiente de control administrativo.");
                }

                // A4: referencia al medio de pago.
                if (string.IsNullOrWhiteSpace(pago.IdentificadorExterno))
                {
                    observaciones.Add("Referencia al medio de pago no informada.");
                }

                TransaccionEconomica transaccion = new TransaccionEconomica
                {
                    TipoOperacion = TipoReservaAbonada,
                    IdReserva = pago.IdReserva,
                    IdPago = pago.IdPago,
                    MontoTotal = pago.ImporteTotal,
                    PorcentajeComision = porcentaje,
                    ImporteComision = comision,
                    ImporteGestor = pago.ImporteTotal - comision,
                    EstadoPago = EstadoPagado,
                    ReferenciaPago = string.IsNullOrWhiteSpace(pago.IdentificadorExterno) ? null : pago.IdentificadorExterno,
                    EstadoRegistro = faltantes.Count > 0 ? RegistroPendienteControl : RegistroCompleto,
                    Observacion = observaciones.Count > 0 ? string.Join(" ", observaciones) : null
                };

                bool esNueva;
                int id = _mpp.Registrar(transaccion, out esNueva);
                if (esNueva)
                {
                    _bitacora.Registrar(
                        pago.IdUsuarioExterno > 0 ? pago.IdUsuarioExterno : (int?)null, "ALTA", TipoEntidadBitacora, id,
                        "Transacción económica por la reserva N° " + pago.IdReserva + " abonada: " +
                        BLL_CuentaCorriente.FormatearImporte(pago.ImporteTotal, pago.Moneda) + ", comisión StageUp " +
                        porcentaje.ToString("0.##", CultureInfo.InvariantCulture) + " % (" +
                        BLL_CuentaCorriente.FormatearImporte(comision, pago.Moneda) + "), al gestor " +
                        BLL_CuentaCorriente.FormatearImporte(pago.ImporteTotal - comision, pago.Moneda) + "." +
                        (transaccion.Observacion != null ? " " + transaccion.Observacion : string.Empty),
                        "StageUp.BLL");
                }

                return ResultadoOperacion<int>.Ok(id);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<int>.Error(ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // A1 (cancelación con cargo) y A2 (sin cargo).
        // ------------------------------------------------------------------
        public ResultadoOperacion<int> RegistrarPorCancelacion(int idReserva, int? idNotaDebito)
        {
            try
            {
                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = idReserva });
                if (reserva == null)
                {
                    return ResultadoOperacion<int>.Error("No se encontró la reserva cancelada.");
                }

                // A2: solicitud pendiente o reserva aceptada sin pagar: no hubo
                // importe cobrado, no se genera transacción.
                if (reserva.EstadoPago != BLL_Pago.EstadoDevuelto)
                {
                    return ResultadoOperacion<int>.Ok(0, "La cancelación no genera una transacción económica con importe.");
                }

                decimal total = reserva.ImporteEstimado ?? 0m;
                decimal cargo = reserva.ComisionAplicada ? (reserva.ImporteComision ?? 0m) : 0m;
                string moneda = reserva.Moneda ?? "ARS";
                decimal porcentajeCargo = total > 0 && cargo > 0 ? decimal.Round(cargo * 100m / total, 0) : 0m;

                _mpp.ActualizarEstadoPorReserva(new TransaccionEconomica
                {
                    IdReserva = idReserva,
                    EstadoPago = EstadoDevuelto,
                    Observacion = cargo > 0
                        ? "Reserva cancelada: se devolvió el pago como saldo a favor y se cobró un cargo del " +
                          porcentajeCargo.ToString("0.##", CultureInfo.InvariantCulture) + " %."
                        : "Reserva cancelada sin cargo: se devolvió todo el pago como saldo a favor."
                });

                if (cargo <= 0)
                {
                    return ResultadoOperacion<int>.Ok(0, "La cancelación no tuvo cargo: no se calculó comisión.");
                }

                int? idPago = _mppPago.ObtenerIdAprobadoPorReserva(reserva);
                DetallePagoReserva pago = idPago.HasValue ? _mppPago.ObtenerDetalle(new Pago { IdPago = idPago.Value }) : null;
                decimal porcentaje = pago != null && pago.PorcentajeComisionPlataforma.HasValue
                    ? pago.PorcentajeComisionPlataforma.Value
                    : _bllParametros.ObtenerDecimal(BLL_ParametroPlataforma.ComisionPlataformaPorcentaje);
                decimal comision = decimal.Round(cargo * porcentaje / 100m, 2);

                List<string> observaciones = new List<string>();
                if (pago == null)
                {
                    observaciones.Add("No se encontró el pago original de la reserva: pendiente de control administrativo.");
                }
                else if (string.IsNullOrWhiteSpace(pago.IdentificadorExterno))
                {
                    observaciones.Add("Referencia al medio de pago no informada.");
                }

                TransaccionEconomica transaccion = new TransaccionEconomica
                {
                    TipoOperacion = TipoCargoCancelacion,
                    IdReserva = idReserva,
                    IdPago = idPago,
                    IdComprobante = idNotaDebito,
                    MontoTotal = cargo,
                    PorcentajeComision = porcentaje,
                    ImporteComision = comision,
                    ImporteGestor = cargo - comision,
                    PorcentajeCargo = porcentajeCargo,
                    EstadoPago = EstadoCobrado,
                    ReferenciaPago = pago == null || string.IsNullOrWhiteSpace(pago.IdentificadorExterno) ? null : pago.IdentificadorExterno,
                    EstadoRegistro = pago == null ? RegistroPendienteControl : RegistroCompleto,
                    Observacion = observaciones.Count > 0 ? string.Join(" ", observaciones) : null
                };

                bool esNueva;
                int id = _mpp.Registrar(transaccion, out esNueva);
                if (esNueva)
                {
                    _bitacora.Registrar(
                        reserva.IdUsuarioExternoSolicitante, "ALTA", TipoEntidadBitacora, id,
                        "Transacción económica por el cargo de cancelación de la reserva N° " + idReserva + ": " +
                        porcentajeCargo.ToString("0.##", CultureInfo.InvariantCulture) + " % del valor (" +
                        BLL_CuentaCorriente.FormatearImporte(cargo, moneda) + "), comisión StageUp " +
                        porcentaje.ToString("0.##", CultureInfo.InvariantCulture) + " % (" +
                        BLL_CuentaCorriente.FormatearImporte(comision, moneda) + "), al gestor " +
                        BLL_CuentaCorriente.FormatearImporte(cargo - comision, moneda) + ".",
                        "StageUp.BLL");
                }

                return ResultadoOperacion<int>.Ok(id);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<int>.Error(ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // A5 a A9: consulta administrativa.
        // ------------------------------------------------------------------
        public ResultadoOperacion<List<TransaccionEconomica>> Listar(FiltroTransacciones filtro)
        {
            filtro = filtro ?? new FiltroTransacciones();
            if (filtro.TipoOperacion != TipoReservaAbonada && filtro.TipoOperacion != TipoCargoCancelacion)
            {
                filtro.TipoOperacion = null;
            }

            if (filtro.EstadoPago != EstadoPagado && filtro.EstadoPago != EstadoCobrado &&
                filtro.EstadoPago != EstadoDevuelto && filtro.EstadoPago != EstadoPendiente)
            {
                filtro.EstadoPago = null;
            }

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
            {
                return ResultadoOperacion<List<TransaccionEconomica>>.Error("La fecha desde no puede ser posterior a la fecha hasta.");
            }

            if (filtro.Texto != null && filtro.Texto.Trim().Length > 100)
            {
                filtro.Texto = filtro.Texto.Trim().Substring(0, 100);
            }

            try
            {
                return ResultadoOperacion<List<TransaccionEconomica>>.Ok(_mpp.Listar(filtro));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<TransaccionEconomica>>.Error(ex.Message);
            }
        }

        // A7: true si todavía no hay ninguna transacción.
        public bool NoHayTransacciones()
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

        public ResultadoOperacion<TransaccionEconomica> ObtenerDetalle(int idTransaccion)
        {
            try
            {
                TransaccionEconomica detalle = _mpp.ObtenerDetalle(new TransaccionEconomica { IdTransaccion = idTransaccion });
                return detalle == null
                    ? ResultadoOperacion<TransaccionEconomica>.Error("No es posible acceder al detalle de la transacción seleccionada en este momento.", "A9")
                    : ResultadoOperacion<TransaccionEconomica>.Ok(detalle);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<TransaccionEconomica>.Error("No es posible acceder al detalle de la transacción seleccionada en este momento.", "A9");
            }
        }

        private static string UnirLista(List<string> partes)
        {
            return partes.Count == 1
                ? partes[0]
                : string.Join(", ", partes.GetRange(0, partes.Count - 1)) + " y " + partes[partes.Count - 1];
        }
    }
}
