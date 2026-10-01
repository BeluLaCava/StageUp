using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Cuenta corriente del cliente y del gestor (ítem 6B, script 49).
    public class MPP_CuentaCorriente
    {
        public List<SaldoCuentaCorriente> ObtenerSaldos(SaldoCuentaCorriente oCuenta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CuentaCorriente_ObtenerSaldos",
                new Hashtable
                {
                    { "@idUsuarioExterno", oCuenta.IdUsuarioExterno },
                    { "@rolCuenta", oCuenta.RolCuenta }
                });

            List<SaldoCuentaCorriente> saldos = new List<SaldoCuentaCorriente>();
            foreach (DataRow fila in tabla.Rows)
            {
                saldos.Add(new SaldoCuentaCorriente
                {
                    IdUsuarioExterno = oCuenta.IdUsuarioExterno,
                    RolCuenta = oCuenta.RolCuenta,
                    Moneda = fila["moneda"].ToString(),
                    Saldo = Convert.ToDecimal(fila["saldo"]),
                    CantidadMovimientos = Convert.ToInt32(fila["cantidadMovimientos"]),
                    UltimoMovimiento = fila["ultimoMovimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["ultimoMovimiento"])
                });
            }
            return saldos;
        }

        public List<MovimientoCuentaCorriente> ListarMovimientos(FiltroMovimientos oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CuentaCorriente_ListarMovimientos",
                new Hashtable
                {
                    { "@idUsuarioExterno", oFiltro.IdUsuarioExterno },
                    { "@rolCuenta", oFiltro.RolCuenta },
                    { "@moneda", string.IsNullOrEmpty(oFiltro.Moneda) ? null : oFiltro.Moneda },
                    { "@desde", oFiltro.Desde.HasValue ? (object)oFiltro.Desde.Value.Date : null },
                    { "@hasta", oFiltro.Hasta.HasValue ? (object)oFiltro.Hasta.Value.Date : null },
                    { "@tipoMovimiento", string.IsNullOrEmpty(oFiltro.TipoMovimiento) ? null : oFiltro.TipoMovimiento }
                });

            List<MovimientoCuentaCorriente> movimientos = new List<MovimientoCuentaCorriente>();
            foreach (DataRow fila in tabla.Rows)
            {
                movimientos.Add(new MovimientoCuentaCorriente
                {
                    IdMovimiento = Convert.ToInt32(fila["idMovimiento"]),
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    RolCuenta = fila["rolCuenta"].ToString(),
                    TipoMovimiento = fila["tipoMovimiento"].ToString(),
                    Importe = Convert.ToDecimal(fila["importe"]),
                    Moneda = fila["moneda"].ToString(),
                    Descripcion = fila["descripcion"].ToString(),
                    IdReserva = Entero(fila, "idReserva"),
                    IdPago = Entero(fila, "idPago"),
                    IdComprobante = Entero(fila, "idComprobante"),
                    FechaMovimiento = Convert.ToDateTime(fila["fechaMovimiento"]),
                    SaldoAcumulado = Convert.ToDecimal(fila["saldoAcumulado"]),
                    NumeroComprobante = Texto(fila, "numeroComprobante"),
                    EstadoComprobante = Texto(fila, "estadoComprobante"),
                    OrigenComprobante = Texto(fila, "origenComprobante")
                });
            }
            return movimientos;
        }

        // Una fila por usuario, rol y moneda. Los usuarios encontrados por
        // texto que todavía no tienen movimientos vienen con rol y moneda nulos.
        public List<SaldoCuentaCorriente> ListarCuentas(FiltroCuentasCorrientes oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CuentaCorriente_ListarCuentas",
                new Hashtable { { "@texto", string.IsNullOrWhiteSpace(oFiltro.Texto) ? null : oFiltro.Texto.Trim() } });

            List<SaldoCuentaCorriente> cuentas = new List<SaldoCuentaCorriente>();
            foreach (DataRow fila in tabla.Rows)
            {
                cuentas.Add(new SaldoCuentaCorriente
                {
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    NombreUsuario = Texto(fila, "nombreUsuario"),
                    CorreoUsuario = Texto(fila, "correoElectronico"),
                    PerfilUsuario = Texto(fila, "perfilUsuario"),
                    RolCuenta = Texto(fila, "rolCuenta"),
                    Moneda = Texto(fila, "moneda"),
                    Saldo = fila["saldo"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["saldo"]),
                    UltimoMovimiento = fila["ultimoMovimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["ultimoMovimiento"])
                });
            }
            return cuentas;
        }

        // oMovimiento.Importe es el importe a liquidar (positivo); el SP lo
        // registra en negativo en la cuenta del gestor.
        public ResultadoMovimientoDinero RegistrarLiquidacion(MovimientoCuentaCorriente oMovimiento)
        {
            return MPP_Pago.MapearResultado(Conexion.Instance.Leer(
                "sp_CuentaCorriente_RegistrarLiquidacion",
                new Hashtable
                {
                    { "@idUsuarioGestor", oMovimiento.IdUsuarioExterno },
                    { "@moneda", oMovimiento.Moneda },
                    { "@importe", oMovimiento.Importe },
                    { "@detalle", oMovimiento.Descripcion },
                    { "@idUsuarioInterno", oMovimiento.IdUsuarioInternoResponsable }
                }), "idMovimiento");
        }

        private static int? Entero(DataRow fila, string columna)
        {
            return fila[columna] == DBNull.Value ? (int?)null : Convert.ToInt32(fila[columna]);
        }

        private static string Texto(DataRow fila, string columna)
        {
            return fila[columna] == DBNull.Value ? null : fila[columna].ToString();
        }
    }
}
