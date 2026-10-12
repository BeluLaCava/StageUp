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
        private readonly BLL_Billetera _billetera = new BLL_Billetera();

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

        // Ítem 7: el cliente elige cómo pagar (todo con tarjeta, todo con
        // saldo a favor o una parte con saldo y el resto con tarjeta). Aunque
        // el saldo alcance, puede pagar todo con tarjeta.
        public ResultadoOperacion<ResumenPago> CalcularResumen(ContextoPagoReserva contexto, EleccionPago eleccion)
        {
            if (contexto == null || contexto.Reserva == null)
            {
                return ResultadoOperacion<ResumenPago>.Error("No se encontró la reserva indicada.");
            }

            string moneda = MonedaDe(contexto.Reserva);
            decimal total = contexto.Total;
            decimal disponible = Math.Max(contexto.SaldoDisponible, 0m);
            ModoPago modo = eleccion == null ? ModoPago.Tarjeta : eleccion.Modo;
            decimal saldoAUsar;

            switch (modo)
            {
                case ModoPago.Saldo:
                    if (disponible < total)
                    {
                        return ResultadoOperacion<ResumenPago>.Error(
                            "Tu saldo a favor (" + BLL_CuentaCorriente.FormatearImporte(disponible, moneda) +
                            ") no alcanza para pagar toda la reserva. Elegí pagar una parte con saldo y el resto con tarjeta.");
                    }

                    saldoAUsar = total;
                    break;
                case ModoPago.Combinado:
                    saldoAUsar = decimal.Round(eleccion.ImporteSaldo, 2);
                    if (disponible <= 0)
                    {
                        return ResultadoOperacion<ResumenPago>.Error("No tenés saldo a favor para usar en este pago.");
                    }

                    if (saldoAUsar <= 0 || saldoAUsar >= total)
                    {
                        return ResultadoOperacion<ResumenPago>.Error(
                            "Indicá cuánto saldo usar: tiene que ser más de 0 y menos que el total de la reserva (" +
                            BLL_CuentaCorriente.FormatearImporte(total, moneda) + ").");
                    }

                    if (saldoAUsar > disponible)
                    {
                        return ResultadoOperacion<ResumenPago>.Error(
                            "No podés usar más saldo del que tenés disponible (" + BLL_CuentaCorriente.FormatearImporte(disponible, moneda) + ").");
                    }

                    break;
                default:
                    saldoAUsar = 0m;
                    break;
            }

            decimal importeTarjeta = total - saldoAUsar;
            return ResultadoOperacion<ResumenPago>.Ok(new ResumenPago
            {
                Moneda = moneda,
                Total = total,
                SaldoDisponible = disponible,
                SaldoAUsar = saldoAUsar,
                ImporteTarjeta = importeTarjeta,
                SaldoRestante = disponible - saldoAUsar,
                MedioDescripcion = saldoAUsar <= 0 ? "Tarjeta de crédito"
                    : importeTarjeta <= 0 ? "Saldo a favor"
                    : "Saldo a favor y tarjeta de crédito"
            });
        }

        // Ítem 8: pago con una tarjeta de la billetera (pidiendo el código de
        // seguridad) o con una nueva que, si el usuario quiere, se guarda
        // después de un pago aprobado. medio puede ser null si se paga todo
        // con saldo a favor.
        public ResultadoOperacion<Pago> PagarReserva(int idReserva, int idUsuarioExterno, TarjetaParaPagar medio, EleccionPago eleccion)
        {
            DatosTarjeta tarjeta;
            ResultadoOperacion<DatosTarjeta> datos = ResolverTarjeta(idUsuarioExterno, medio, out tarjeta);
            if (!datos.Exitoso)
            {
                return ResultadoOperacion<Pago>.Error(datos.Mensaje);
            }

            return CompletarConBilletera(idUsuarioExterno, medio, PagarReserva(idReserva, idUsuarioExterno, tarjeta, eleccion));
        }

        public ResultadoOperacion<Pago> PagarDeuda(int idUsuarioExterno, string moneda, TarjetaParaPagar medio)
        {
            DatosTarjeta tarjeta;
            ResultadoOperacion<DatosTarjeta> datos = ResolverTarjeta(idUsuarioExterno, medio, out tarjeta);
            if (!datos.Exitoso)
            {
                return ResultadoOperacion<Pago>.Error(datos.Mensaje);
            }

            return CompletarConBilletera(idUsuarioExterno, medio, PagarDeuda(idUsuarioExterno, moneda, tarjeta));
        }

        private ResultadoOperacion<DatosTarjeta> ResolverTarjeta(int idUsuarioExterno, TarjetaParaPagar medio, out DatosTarjeta tarjeta)
        {
            tarjeta = null;
            if (medio == null)
            {
                return ResultadoOperacion<DatosTarjeta>.Ok(null);
            }

            if (!medio.IdTarjetaGuardada.HasValue)
            {
                tarjeta = medio.Nueva;
                return ResultadoOperacion<DatosTarjeta>.Ok(tarjeta);
            }

            ResultadoOperacion<DatosTarjeta> guardada = _billetera.ObtenerDatosParaPagar(idUsuarioExterno, medio.IdTarjetaGuardada.Value, medio.CodigoSeguridad);
            tarjeta = guardada.Exitoso ? guardada.Valor : null;
            return guardada;
        }

        private ResultadoOperacion<Pago> CompletarConBilletera(int idUsuarioExterno, TarjetaParaPagar medio, ResultadoOperacion<Pago> resultado)
        {
            if (!resultado.Exitoso || medio == null || resultado.Valor.ImporteTarjeta <= 0)
            {
                return resultado;
            }

            if (medio.IdTarjetaGuardada.HasValue)
            {
                _billetera.RegistrarUso(medio.IdTarjetaGuardada.Value);
                return resultado;
            }

            if (!medio.GuardarEnBilletera || medio.Nueva == null)
            {
                return resultado;
            }

            ResultadoOperacion<TarjetaGuardada> guardada = _billetera.Guardar(idUsuarioExterno, medio.Nueva, medio.AliasParaGuardar, false);
            return ResultadoOperacion<Pago>.Ok(resultado.Valor, resultado.Mensaje + " " +
                (guardada.Exitoso ? guardada.Mensaje : "El pago se hizo, pero no pudimos guardar la tarjeta: " + guardada.Mensaje));
        }

        // tarjeta puede ser null solo si se paga todo con saldo a favor.
        public ResultadoOperacion<Pago> PagarReserva(int idReserva, int idUsuarioExterno, DatosTarjeta tarjeta, EleccionPago eleccion)
        {
            try
            {
                ResultadoOperacion<ContextoPagoReserva> contexto = ObtenerContextoPago(idReserva, idUsuarioExterno);
                if (!contexto.Exitoso)
                {
                    return ResultadoOperacion<Pago>.Error(contexto.Mensaje);
                }

                ResultadoOperacion<ResumenPago> resumen = CalcularResumen(contexto.Valor, eleccion);
                if (!resumen.Exitoso)
                {
                    return ResultadoOperacion<Pago>.Error(resumen.Mensaje);
                }

                Reserva reserva = contexto.Valor.Reserva;
                Pago pago = new Pago
                {
                    IdUsuarioExterno = idUsuarioExterno,
                    IdReserva = idReserva,
                    Concepto = "Reserva",
                    Moneda = resumen.Valor.Moneda,
                    ImporteTotal = resumen.Valor.Total,
                    ImporteTarjeta = resumen.Valor.ImporteTarjeta,
                    ImporteSaldo = resumen.Valor.SaldoAUsar,
                    Origen = "StageUp",
                    PorcentajeComisionPlataforma = _bllParametros.ObtenerDecimal(BLL_ParametroPlataforma.ComisionPlataformaPorcentaje)
                };

                if (pago.ImporteTarjeta > 0)
                {
                    ResultadoOperacion autorizacion = AutorizarTarjeta(tarjeta, pago);
                    if (!autorizacion.Exitoso)
                    {
                        return ResultadoOperacion<Pago>.Error(autorizacion.Mensaje);
                    }
                }

                // Saldo, tarjeta, movimientos y factura se registran en una sola
                // transacción: si algo falla no se descuenta el saldo.
                ResultadoMovimientoDinero resultado = _mpp.RegistrarPagoReserva(pago);
                if (!resultado.EsOk || !resultado.IdGenerado.HasValue)
                {
                    RegistrarAutorizacionAnulada(pago, MensajeDeResultado(resultado.Resultado));
                    return ResultadoOperacion<Pago>.Error(MensajeDeResultado(resultado.Resultado) +
                        (pago.ImporteTarjeta > 0 ? " No se realizó ningún cobro en tu tarjeta." : string.Empty) +
                        (pago.ImporteSaldo > 0 ? " Tu saldo a favor no se modificó." : string.Empty));
                }

                pago.IdPago = resultado.IdGenerado.Value;
                pago.Estado = "Aprobado";
                pago.FechaPago = DateTime.Now;
                pago.ImporteComisionPlataforma = decimal.Round(pago.ImporteTotal * pago.PorcentajeComisionPlataforma.Value / 100m, 2);

                _bitacora.Registrar(
                    idUsuarioExterno, "PAGO", "Pago", pago.IdPago,
                    "Pago de la reserva N° " + idReserva + " (\"" + reserva.NombreEspacio + "\") por " +
                    BLL_CuentaCorriente.FormatearImporte(pago.ImporteTotal, pago.Moneda) + DescribirMedios(pago) +
                    ". Se emitió la factura simulada.");

                AvisarPagoAprobado(reserva, pago);
                RegistrarTransaccionEconomica(pago.IdPago);

                return ResultadoOperacion<Pago>.Ok(pago, "¡Listo! Recibimos el pago y tu reserva quedó confirmada. Ya tenés la factura del pago.");
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

        // ------------------------------------------------------------------
        // CU-002-001 A6 a A10: "Pagos de reservas" (consulta y control)
        // ------------------------------------------------------------------
        public const string GrupoTodos = "Todos";
        public const string GrupoPendiente = "Pendiente";
        public const string GrupoConfirmado = "Confirmado";
        public const string GrupoFallido = "Fallido";
        public const string GrupoCancelado = "Cancelado";
        public const string GrupoEnRevision = "EnRevision";

        private static readonly string[] Grupos = { GrupoTodos, GrupoPendiente, GrupoConfirmado, GrupoFallido, GrupoCancelado, GrupoEnRevision };

        // Nombre de cada estado como lo describe el documento.
        // Texto de cada estado según los parámetros administrativos (CU-002-004 A5).
        public static string DescribirEstado(string estado)
        {
            switch (estado)
            {
                case "Aprobado": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoAprobado);
                case "Rechazado": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoRechazado);
                case "Anulado": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoAnulado);
                case "Pendiente": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoPendiente);
                case "EsperandoPago": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoEsperando);
                case "Vencido": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoVencido);
                case "Observado": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoObservado);
                case "NoAsociado": return BLL_ParametroPlataforma.TextoEstado(BLL_ParametroPlataforma.EstadoPagoNoAsociado);
                default: return estado;
            }
        }

        public static string DescribirGrupo(string grupo)
        {
            return grupo == GrupoEnRevision ? "En revisión" : grupo;
        }

        public ResultadoOperacion<List<ItemPagoReserva>> ListarAdministracion(FiltroPagosReserva filtro)
        {
            filtro = filtro ?? new FiltroPagosReserva();
            if (Array.IndexOf(Grupos, filtro.Grupo) < 0)
            {
                filtro.Grupo = GrupoTodos;
            }

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
            {
                return ResultadoOperacion<List<ItemPagoReserva>>.Error("La fecha desde no puede ser posterior a la fecha hasta.");
            }

            if (filtro.Texto != null && filtro.Texto.Trim().Length > 100)
            {
                filtro.Texto = filtro.Texto.Trim().Substring(0, 100);
            }

            try
            {
                return ResultadoOperacion<List<ItemPagoReserva>>.Ok(_mpp.ListarAdministracion(filtro));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<ItemPagoReserva>>.Error(ex.Message);
            }
        }

        // A8: true si no hay ningún pago (ni reservas esperando el pago).
        public bool NoHayPagos()
        {
            try
            {
                return _mpp.ContarAdministracion() == 0;
            }
            catch (ErrorAccesoDatosException)
            {
                return false;
            }
        }

        // A6 pasos 9 y 10 (y A10 si no está disponible).
        public ResultadoOperacion<DetallePagoReserva> ObtenerDetalleAdministracion(int? idPago, int? idReserva)
        {
            try
            {
                DetallePagoReserva detalle = _mpp.ObtenerDetalle(new Pago { IdPago = idPago ?? 0, IdReserva = idReserva });
                return detalle == null
                    ? ResultadoOperacion<DetallePagoReserva>.Error("No es posible acceder al detalle del pago seleccionado en este momento.", "A10")
                    : ResultadoOperacion<DetallePagoReserva>.Ok(detalle);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<DetallePagoReserva>.Error("No es posible acceder al detalle del pago seleccionado en este momento.", "A10");
            }
        }

        // Detalle del pago y su factura para el solicitante o el gestor de la
        // reserva (DetallePago.aspx).
        public ResultadoOperacion<DetallePagoReserva> ObtenerDetalleParaUsuario(int? idPago, int? idReserva, int idUsuarioExterno)
        {
            try
            {
                int? id = idPago;
                if (!id.HasValue && idReserva.HasValue)
                {
                    id = _mpp.ObtenerIdAprobadoPorReserva(new Reserva { IdReserva = idReserva.Value });
                }

                DetallePagoReserva detalle = id.HasValue ? _mpp.ObtenerDetalle(new Pago { IdPago = id.Value }) : null;
                if (detalle == null || detalle.Estado != "Aprobado" ||
                    (detalle.IdUsuarioExterno != idUsuarioExterno && detalle.IdUsuarioGestor != idUsuarioExterno))
                {
                    return ResultadoOperacion<DetallePagoReserva>.Error("No se encontró el pago indicado.");
                }

                return ResultadoOperacion<DetallePagoReserva>.Ok(detalle);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<DetallePagoReserva>.Error(ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // CU-002-001 pasos 1 a 9 y A1 a A5: aviso del medio de pago externo.
        // En StageUp el medio externo está simulado: el aviso se carga desde
        // "Pagos de reservas" (usuario interno autorizado).
        // ------------------------------------------------------------------
        public static readonly string[] EstadosInformables = { "Aprobado", "Pendiente", "Rechazado", "Cancelado" };

        public ResultadoOperacion<int> RegistrarAvisoMedioExterno(AvisoMedioPago aviso, int idUsuarioInternoResponsable)
        {
            if (aviso == null)
            {
                return ResultadoOperacion<int>.Error("Completá los datos del aviso.");
            }

            try
            {
                string identificador = string.IsNullOrWhiteSpace(aviso.IdentificadorExterno) ? null : aviso.IdentificadorExterno.Trim();
                string medio = string.IsNullOrWhiteSpace(aviso.Medio) ? null : aviso.Medio.Trim();
                string estadoInformado = Array.IndexOf(EstadosInformables, aviso.EstadoInformado) >= 0 ? aviso.EstadoInformado : null;
                string moneda = aviso.Moneda == "USD" ? "USD" : "ARS";
                string motivo = string.IsNullOrWhiteSpace(aviso.Motivo) ? null : Recortar(aviso.Motivo.Trim(), 300);
                if (identificador != null && identificador.Length > 60)
                {
                    identificador = identificador.Substring(0, 60);
                }

                if (medio != null && medio.Length > 40)
                {
                    medio = medio.Substring(0, 40);
                }

                Pago registro = new Pago
                {
                    IdReserva = aviso.IdReserva,
                    Moneda = moneda,
                    ImporteTotal = aviso.Importe.HasValue && aviso.Importe.Value > 0 ? decimal.Round(aviso.Importe.Value, 2) : 0m,
                    IdentificadorExterno = identificador,
                    MedioInformado = medio,
                    EstadoInformado = aviso.EstadoInformado,
                    FechaOperacion = aviso.FechaOperacion,
                    MotivoRechazo = motivo
                };

                // Paso 3 / A1: datos necesarios para identificar la operación.
                List<string> faltantes = new List<string>();
                if (!aviso.IdReserva.HasValue) faltantes.Add("la reserva asociada");
                if (identificador == null) faltantes.Add("el identificador del pago");
                if (registro.ImporteTotal <= 0) faltantes.Add("el monto abonado");
                if (!aviso.FechaOperacion.HasValue) faltantes.Add("la fecha de la operación");
                if (medio == null) faltantes.Add("el medio utilizado");
                if (estadoInformado == null) faltantes.Add("el estado informado");

                if (faltantes.Count > 0)
                {
                    registro.Estado = "Observado";
                    registro.Observacion = "Información incompleta: falta " + UnirLista(faltantes) + ". Queda para revisión administrativa.";
                    return GuardarAviso(registro, idUsuarioInternoResponsable,
                        "El aviso no tiene todos los datos para identificar la operación (falta " + UnirLista(faltantes) +
                        "). Se registró como pago observado, sin confirmar nada, para revisión administrativa.", "A1");
                }

                if (identificador != null)
                {
                    List<Pago> previos = _mpp.BuscarPorIdentificador(new Pago { IdentificadorExterno = identificador });
                    if (previos.Count > 0 && previos[0].Estado == "Aprobado")
                    {
                        return ResultadoOperacion<int>.Error("Ese pago ya estaba confirmado (pago N° " + previos[0].IdPago + "). No se registró de nuevo.");
                    }

                    Pago pendiente = previos.Find(p => p.Estado == "Pendiente");
                    if (pendiente != null)
                    {
                        registro.IdPago = pendiente.IdPago;
                    }
                }

                // Paso 4 / A2: la transacción económica (reserva aceptada que
                // espera el pago) tiene que existir.
                Reserva reserva = _mppReserva.ObtenerPorId(new Reserva { IdReserva = aviso.IdReserva.Value });
                if (reserva == null || !EsperaPago(reserva) || !reserva.ImporteEstimado.HasValue)
                {
                    registro.Estado = "NoAsociado";
                    registro.IdUsuarioExterno = reserva != null ? reserva.IdUsuarioExternoSolicitante : 0;
                    registro.IdReserva = reserva != null ? (int?)reserva.IdReserva : null;
                    registro.IdPago = 0;
                    registro.Observacion = reserva == null
                        ? "No existe la reserva N° " + aviso.IdReserva.Value + " informada por el medio de pago."
                        : "La reserva N° " + reserva.IdReserva + " no tiene un pago pendiente (estado de pago: " + (reserva.EstadoPago ?? "-") + ").";
                    return GuardarAviso(registro, idUsuarioInternoResponsable,
                        "No se encontró una transacción pendiente de pago para ese aviso. Se registró como pago no asociado, sin modificar ninguna reserva.", "A2");
                }

                registro.IdUsuarioExterno = reserva.IdUsuarioExternoSolicitante;
                registro.Moneda = MonedaDe(reserva);

                switch (estadoInformado)
                {
                    case "Pendiente":
                        registro.Estado = "Pendiente";
                        registro.Observacion = "El medio de pago informó el pago como pendiente. La reserva sigue esperando la confirmación.";
                        return GuardarAviso(registro, idUsuarioInternoResponsable,
                            "Se registró el pago como pendiente. La reserva N° " + reserva.IdReserva + " sigue pendiente de confirmación.", "A3");
                    case "Rechazado":
                        registro.Estado = "Rechazado";
                        registro.MotivoRechazo = registro.MotivoRechazo ?? "El medio de pago informó que el pago no se completó.";
                        ResultadoOperacion<int> fallido = GuardarAviso(registro, idUsuarioInternoResponsable,
                            "Se registró el pago como fallido. La reserva N° " + reserva.IdReserva + " sigue esperando el pago dentro de su plazo.", "A4");
                        if (fallido.Exitoso)
                        {
                            _notificacion.Notificar(
                                reserva.IdUsuarioExternoSolicitante, TipoNotificacion.PagoNoCompletado,
                                "El pago de tu reserva para \"" + reserva.NombreEspacio + "\" no se completó. Podés volver a intentarlo antes del vencimiento.",
                                "~/Pagar.aspx?reserva=" + reserva.IdReserva);
                        }

                        return fallido;
                    case "Cancelado":
                        registro.Estado = "Anulado";
                        registro.Observacion = "El medio de pago informó el pago como cancelado.";
                        return GuardarAviso(registro, idUsuarioInternoResponsable,
                            "Se registró el pago como cancelado. La reserva N° " + reserva.IdReserva + " no se modificó.", "A5");
                }

                // Pasos 5 a 9: pago confirmado.
                if (registro.ImporteTotal != reserva.ImporteEstimado.Value || registro.Moneda != moneda)
                {
                    registro.Estado = "Observado";
                    registro.IdPago = 0;
                    registro.Observacion = "El monto informado (" + BLL_CuentaCorriente.FormatearImporte(registro.ImporteTotal, moneda) +
                        ") no coincide con el de la reserva N° " + reserva.IdReserva + " (" +
                        BLL_CuentaCorriente.FormatearImporte(reserva.ImporteEstimado.Value, registro.Moneda) + ").";
                    return GuardarAviso(registro, idUsuarioInternoResponsable,
                        "El pago no se puede confirmar porque el monto no coincide con el de la reserva. Quedó observado para revisión.", "A1");
                }

                Pago pago = new Pago
                {
                    IdPago = registro.IdPago,
                    IdUsuarioExterno = reserva.IdUsuarioExternoSolicitante,
                    IdReserva = reserva.IdReserva,
                    Concepto = "Reserva",
                    Moneda = registro.Moneda,
                    ImporteTotal = reserva.ImporteEstimado.Value,
                    ImporteTarjeta = reserva.ImporteEstimado.Value,
                    ImporteSaldo = 0m,
                    Origen = "MedioExterno",
                    IdentificadorExterno = identificador,
                    MedioInformado = medio,
                    FechaOperacion = aviso.FechaOperacion,
                    IdUsuarioInternoRegistro = idUsuarioInternoResponsable,
                    PorcentajeComisionPlataforma = _bllParametros.ObtenerDecimal(BLL_ParametroPlataforma.ComisionPlataformaPorcentaje)
                };

                ResultadoMovimientoDinero resultado = _mpp.RegistrarPagoReserva(pago);
                if (!resultado.EsOk || !resultado.IdGenerado.HasValue)
                {
                    return ResultadoOperacion<int>.Error(MensajeDeResultado(resultado.Resultado));
                }

                pago.IdPago = resultado.IdGenerado.Value;
                pago.Estado = "Aprobado";
                pago.FechaPago = DateTime.Now;
                pago.ImporteComisionPlataforma = decimal.Round(pago.ImporteTotal * pago.PorcentajeComisionPlataforma.Value / 100m, 2);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "PAGO", "Pago", pago.IdPago,
                    "Pago confirmado por el medio de pago (" + medio + ", operación " + identificador + ") para la reserva N° " +
                    reserva.IdReserva + " por " + BLL_CuentaCorriente.FormatearImporte(pago.ImporteTotal, pago.Moneda) +
                    ". Comisión " + pago.PorcentajeComisionPlataforma.Value.ToString("0.##", CultureInfo.InvariantCulture) + " %. Se emitió la factura simulada.");

                AvisarPagoAprobado(reserva, pago);
                RegistrarTransaccionEconomica(pago.IdPago);

                return ResultadoOperacion<int>.Ok(pago.IdPago,
                    "Pago confirmado. La reserva N° " + reserva.IdReserva + " quedó pagada, se registraron los movimientos y se emitió la factura simulada.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<int>.Error(ex.Message);
            }
        }

        private ResultadoOperacion<int> GuardarAviso(Pago registro, int idUsuarioInternoResponsable, string mensaje, string codigo)
        {
            int idPago = _mpp.RegistrarAviso(registro, idUsuarioInternoResponsable);
            _bitacora.RegistrarInterno(
                idUsuarioInternoResponsable, "PAGO", "Pago", idPago,
                Recortar("Aviso del medio de pago (" + (registro.IdentificadorExterno ?? "sin identificador") + "): " +
                    DescribirEstado(registro.Estado) +
                    (registro.IdReserva.HasValue ? " para la reserva N° " + registro.IdReserva.Value : string.Empty) +
                    (registro.Observacion != null ? ". " + registro.Observacion : string.Empty) +
                    (registro.MotivoRechazo != null && registro.Estado == "Rechazado" ? ". Motivo: " + registro.MotivoRechazo : string.Empty), 1900));
            return ResultadoOperacion<int>.OkAlternativo(idPago, mensaje, codigo);
        }

        private static string UnirLista(List<string> partes)
        {
            return partes.Count == 1
                ? partes[0]
                : string.Join(", ", partes.GetRange(0, partes.Count - 1)) + " y " + partes[partes.Count - 1];
        }

        private static string Recortar(string texto, int maximo)
        {
            return texto.Length <= maximo ? texto : texto.Substring(0, maximo - 3) + "...";
        }

        // Medio utilizado, como se muestra en el detalle y en la factura.
        public static string DescribirMedioPago(Pago pago)
        {
            if (pago == null)
            {
                return "-";
            }

            if (pago.Origen == "MedioExterno")
            {
                return (pago.MedioInformado ?? "Medio de pago externo") +
                    (string.IsNullOrEmpty(pago.IdentificadorExterno) ? string.Empty : " (operación " + pago.IdentificadorExterno + ")");
            }

            string medios = DescribirMedios(pago).Trim();
            if (medios.StartsWith("con ", StringComparison.Ordinal))
            {
                medios = medios.Substring(4);
            }

            if (medios.Length == 0)
            {
                return pago.MedioInformado ?? "-";
            }

            medios = char.ToUpperInvariant(medios[0]) + medios.Substring(1);
            return pago.ImporteTarjeta > 0 && !string.IsNullOrEmpty(pago.CodigoAutorizacion)
                ? medios + " (autorización " + pago.CodigoAutorizacion + ")"
                : medios;
        }

        public static string DescribirFechaReserva(DetallePagoReserva detalle)
        {
            if (detalle == null || !detalle.FechaSolicitada.HasValue)
            {
                return "-";
            }

            string texto = detalle.FechaSolicitada.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            if (detalle.MinutoDesde.HasValue && detalle.MinutoHasta.HasValue)
            {
                texto += " de " + Hora(detalle.MinutoDesde.Value) + " a " + Hora(detalle.MinutoHasta.Value) + " hs";
            }

            return texto;
        }

        private static string Hora(int minutos)
        {
            return minutos == 1440
                ? "24:00"
                : (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        // Emisor de la factura simulada. Los datos se toman de los
        // parámetros de facturación (CU-002-004) cuando estén configurados.
        // Datos del emisor (CU-002-004 A6). Una factura ya emitida se muestra
        // con los datos que regían en su fecha.
        public static DatosFacturacion ObtenerDatosFacturacion(DateTime? fechaFactura = null)
        {
            return new BLL_ParametroPlataforma().ObtenerDatosFacturacion(fechaFactura);
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
        // Completa en el pago la marca, los últimos 4 dígitos y el código de
        // autorización: nunca el número completo, el código de seguridad ni
        // el titular.
        private ResultadoOperacion AutorizarTarjeta(DatosTarjeta tarjeta, Pago pago)
        {
            ResultadoOperacion<string> validacion = ValidadorTarjeta.Validar(tarjeta);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion.Error(validacion.Mensaje);
            }

            // El titular (igual que el número completo y el código de
            // seguridad) solo se usa en memoria para validar y autorizar: no se
            // guarda. Del medio de pago se persisten la marca y los últimos 4.
            pago.MarcaTarjeta = validacion.Valor;
            pago.UltimosDigitos = ValidadorTarjeta.ObtenerUltimosDigitos(tarjeta.Numero);

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

        // CU-002-002: el pago confirmado genera la transacción económica con
        // la comisión de StageUp y el importe del gestor. Si falla, el pago ya
        // quedó registrado y la transacción se puede revisar después.
        private static void RegistrarTransaccionEconomica(int idPago)
        {
            try
            {
                new BLL_TransaccionEconomica().RegistrarPorPagoAprobado(idPago);
            }
            catch (Exception)
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
                // CU-002-004 A7: configuración vigente de los correos administrativos.
                _servicioCorreo.Administrativo = _bllParametros.ObtenerConfiguracionCorreoAdministrativo();
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
                case "YA_REGISTRADO":
                    return "Ese pago ya estaba registrado.";
                case "PAGO_NO_PENDIENTE":
                    return "El pago pendiente informado ya no está pendiente.";
                default:
                    return "No se pudo registrar el pago.";
            }
        }
    }
}
