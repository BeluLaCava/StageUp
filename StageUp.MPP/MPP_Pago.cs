using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Pagos con tarjeta y saldo a favor (ítems 5B y 5D, script 49).
    public class MPP_Pago
    {
        public ResultadoMovimientoDinero RegistrarPagoReserva(Pago oPago)
        {
            return MapearResultado(Conexion.Instance.Leer(
                "sp_Pago_RegistrarReserva",
                new Hashtable
                {
                    { "@idReserva", oPago.IdReserva },
                    { "@idUsuarioExterno", oPago.IdUsuarioExterno },
                    { "@importeTarjeta", oPago.ImporteTarjeta },
                    { "@importeSaldo", oPago.ImporteSaldo },
                    { "@marcaTarjeta", oPago.MarcaTarjeta },
                    { "@ultimosDigitos", oPago.UltimosDigitos },
                    { "@titularTarjeta", oPago.TitularTarjeta },
                    { "@codigoAutorizacion", oPago.CodigoAutorizacion },
                    { "@porcentajeComisionPlataforma", oPago.PorcentajeComisionPlataforma ?? 0m }
                }), "idPago");
        }

        public ResultadoMovimientoDinero PagarDeuda(Pago oPago)
        {
            return MapearResultado(Conexion.Instance.Leer(
                "sp_CuentaCorriente_PagarDeuda",
                new Hashtable
                {
                    { "@idUsuarioExterno", oPago.IdUsuarioExterno },
                    { "@moneda", oPago.Moneda },
                    { "@importe", oPago.ImporteTotal },
                    { "@marcaTarjeta", oPago.MarcaTarjeta },
                    { "@ultimosDigitos", oPago.UltimosDigitos },
                    { "@titularTarjeta", oPago.TitularTarjeta },
                    { "@codigoAutorizacion", oPago.CodigoAutorizacion }
                }), "idPago");
        }

        public void RegistrarRechazado(Pago oPago)
        {
            Conexion.Instance.Leer(
                "sp_Pago_RegistrarRechazado",
                new Hashtable
                {
                    { "@idUsuarioExterno", oPago.IdUsuarioExterno },
                    { "@idReserva", oPago.IdReserva },
                    { "@concepto", oPago.Concepto },
                    { "@moneda", oPago.Moneda },
                    { "@importeTotal", oPago.ImporteTotal },
                    { "@importeTarjeta", oPago.ImporteTarjeta },
                    { "@marcaTarjeta", oPago.MarcaTarjeta },
                    { "@ultimosDigitos", oPago.UltimosDigitos },
                    { "@titularTarjeta", oPago.TitularTarjeta },
                    { "@motivoRechazo", oPago.MotivoRechazo }
                });
        }

        public List<Pago> Listar(FiltroPagos oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Pago_Listar",
                new Hashtable
                {
                    { "@desde", oFiltro.Desde.Date },
                    { "@hasta", oFiltro.Hasta.Date },
                    { "@estado", string.IsNullOrEmpty(oFiltro.Estado) ? null : oFiltro.Estado }
                });

            List<Pago> pagos = new List<Pago>();
            foreach (DataRow fila in tabla.Rows)
            {
                pagos.Add(new Pago
                {
                    IdPago = Convert.ToInt32(fila["idPago"]),
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    IdReserva = fila["idReserva"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idReserva"]),
                    Concepto = fila["concepto"].ToString(),
                    Moneda = fila["moneda"].ToString(),
                    ImporteTotal = Convert.ToDecimal(fila["importeTotal"]),
                    ImporteTarjeta = Convert.ToDecimal(fila["importeTarjeta"]),
                    ImporteSaldo = Convert.ToDecimal(fila["importeSaldo"]),
                    Estado = fila["estado"].ToString(),
                    MarcaTarjeta = Texto(fila, "marcaTarjeta"),
                    UltimosDigitos = Texto(fila, "ultimosDigitos"),
                    TitularTarjeta = Texto(fila, "titularTarjeta"),
                    CodigoAutorizacion = Texto(fila, "codigoAutorizacion"),
                    MotivoRechazo = Texto(fila, "motivoRechazo"),
                    PorcentajeComisionPlataforma = fila["porcentajeComisionPlataforma"] == DBNull.Value
                        ? (decimal?)null : Convert.ToDecimal(fila["porcentajeComisionPlataforma"]),
                    ImporteComisionPlataforma = fila["importeComisionPlataforma"] == DBNull.Value
                        ? (decimal?)null : Convert.ToDecimal(fila["importeComisionPlataforma"]),
                    FechaPago = Convert.ToDateTime(fila["fechaPago"]),
                    NombreUsuario = Texto(fila, "nombreUsuario"),
                    CorreoUsuario = Texto(fila, "correoUsuario"),
                    NombreEspacio = Texto(fila, "nombreEspacio")
                });
            }
            return pagos;
        }

        internal static ResultadoMovimientoDinero MapearResultado(DataTable tabla, string columnaId)
        {
            if (tabla.Rows.Count == 0)
            {
                return new ResultadoMovimientoDinero { Resultado = "SIN_RESPUESTA" };
            }

            DataRow fila = tabla.Rows[0];
            return new ResultadoMovimientoDinero
            {
                Resultado = fila["resultado"].ToString(),
                IdGenerado = columnaId != null && tabla.Columns.Contains(columnaId) && fila[columnaId] != DBNull.Value
                    ? Convert.ToInt32(fila[columnaId])
                    : (int?)null
            };
        }

        private static string Texto(DataRow fila, string columna)
        {
            return fila[columna] == DBNull.Value ? null : fila[columna].ToString();
        }
    }
}
