using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

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
                    { "@codigoAutorizacion", oPago.CodigoAutorizacion },
                    { "@porcentajeComisionPlataforma", oPago.PorcentajeComisionPlataforma ?? 0m },
                    { "@origen", oPago.Origen ?? "StageUp" },
                    { "@identificadorExterno", (object)oPago.IdentificadorExterno ?? DBNull.Value },
                    { "@medioInformado", (object)oPago.MedioInformado ?? DBNull.Value },
                    { "@fechaOperacion", (object)oPago.FechaOperacion ?? DBNull.Value },
                    { "@idPagoPendiente", oPago.IdPago > 0 ? (object)oPago.IdPago : DBNull.Value },
                    { "@idUsuarioInternoRegistro", (object)oPago.IdUsuarioInternoRegistro ?? DBNull.Value }
                }), "idPago");
        }



        public int RegistrarAviso(Pago oPago, int? idUsuarioInternoRegistro)
        {
            object id = Conexion.Instance.LeerEscalar(
                "sp_Pago_RegistrarAviso",
                new Hashtable
                {
                    { "@idReserva", (object)oPago.IdReserva ?? DBNull.Value },
                    { "@idUsuarioExterno", oPago.IdUsuarioExterno > 0 ? (object)oPago.IdUsuarioExterno : DBNull.Value },
                    { "@moneda", oPago.Moneda ?? "ARS" },
                    { "@importeTotal", oPago.ImporteTotal },
                    { "@estado", oPago.Estado },
                    { "@identificadorExterno", (object)oPago.IdentificadorExterno ?? DBNull.Value },
                    { "@medioInformado", (object)oPago.MedioInformado ?? DBNull.Value },
                    { "@estadoInformado", (object)oPago.EstadoInformado ?? DBNull.Value },
                    { "@fechaOperacion", (object)oPago.FechaOperacion ?? DBNull.Value },
                    { "@motivoRechazo", (object)oPago.MotivoRechazo ?? DBNull.Value },
                    { "@observacion", (object)oPago.Observacion ?? DBNull.Value },
                    { "@idUsuarioInternoRegistro", (object)idUsuarioInternoRegistro ?? DBNull.Value }
                });
            return Convert.ToInt32(id);
        }



        public List<Pago> BuscarPorIdentificador(Pago oPago)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Pago_BuscarPorIdentificador",
                new Hashtable { { "@identificadorExterno", oPago.IdentificadorExterno } });
            List<Pago> pagos = new List<Pago>();
            foreach (DataRow fila in tabla.Rows)
            {
                pagos.Add(new Pago
                {
                    IdPago = Convert.ToInt32(fila["idPago"]),
                    IdReserva = fila["idReserva"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idReserva"]),
                    Estado = fila["estado"].ToString()
                });
            }
            return pagos;
        }

        public int? ObtenerIdAprobadoPorReserva(Reserva oReserva)
        {
            object id = Conexion.Instance.LeerEscalar(
                "sp_Pago_ObtenerAprobadoPorReserva",
                new Hashtable { { "@idReserva", oReserva.IdReserva } });
            return id == null || id == DBNull.Value ? (int?)null : Convert.ToInt32(id);
        }


        public List<ItemPagoReserva> ListarAdministracion(FiltroPagosReserva oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Pago_ListarAdministracion",
                new Hashtable
                {
                    { "@grupo", string.IsNullOrEmpty(oFiltro.Grupo) ? "Todos" : oFiltro.Grupo },
                    { "@desde", oFiltro.Desde.HasValue ? (object)oFiltro.Desde.Value.Date : DBNull.Value },
                    { "@hasta", oFiltro.Hasta.HasValue ? (object)oFiltro.Hasta.Value.Date : DBNull.Value },
                    { "@texto", string.IsNullOrWhiteSpace(oFiltro.Texto) ? (object)DBNull.Value : oFiltro.Texto.Trim() }
                });

            List<ItemPagoReserva> items = new List<ItemPagoReserva>();
            foreach (DataRow fila in tabla.Rows)
            {
                items.Add(new ItemPagoReserva
                {
                    IdPago = Entero(fila, "idPago"),
                    IdReserva = Entero(fila, "idReserva"),
                    Concepto = Texto(fila, "concepto"),
                    Origen = Texto(fila, "origen"),
                    Estado = Texto(fila, "estado"),
                    Grupo = Texto(fila, "grupo"),
                    Moneda = Texto(fila, "moneda"),
                    ImporteTotal = fila["importeTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["importeTotal"]),
                    Medio = Texto(fila, "medio"),
                    IdentificadorExterno = Texto(fila, "identificadorExterno"),
                    Fecha = Convert.ToDateTime(fila["fecha"]),
                    NombreUsuario = Texto(fila, "nombreUsuario"),
                    CorreoUsuario = Texto(fila, "correoUsuario"),
                    NombreEspacio = Texto(fila, "nombreEspacio"),
                    FechaLimitePago = Fecha(fila, "fechaLimitePago")
                });
            }
            return items;
        }

        public int ContarAdministracion()
        {
            object total = Conexion.Instance.LeerEscalar("sp_Pago_ContarAdministracion");
            return total == null || total == DBNull.Value ? 0 : Convert.ToInt32(total);
        }



        public DetallePagoReserva ObtenerDetalle(Pago oPago)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Pago_ObtenerDetalle",
                new Hashtable
                {
                    { "@idPago", oPago.IdPago > 0 ? (object)oPago.IdPago : DBNull.Value },
                    { "@idReserva", oPago.IdPago > 0 || !oPago.IdReserva.HasValue ? DBNull.Value : (object)oPago.IdReserva.Value }
                });
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            return new DetallePagoReserva
            {
                IdPago = Entero(fila, "idPago") ?? 0,
                IdUsuarioExterno = Entero(fila, "idUsuarioExterno") ?? 0,
                IdReserva = Entero(fila, "idReserva"),
                Concepto = Texto(fila, "concepto"),
                Moneda = Texto(fila, "moneda"),
                ImporteTotal = fila["importeTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["importeTotal"]),
                ImporteTarjeta = fila["importeTarjeta"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["importeTarjeta"]),
                ImporteSaldo = fila["importeSaldo"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["importeSaldo"]),
                Estado = Texto(fila, "estado"),
                MarcaTarjeta = Texto(fila, "marcaTarjeta"),
                UltimosDigitos = Texto(fila, "ultimosDigitos"),
                CodigoAutorizacion = Texto(fila, "codigoAutorizacion"),
                MotivoRechazo = Texto(fila, "motivoRechazo"),
                PorcentajeComisionPlataforma = fila["porcentajeComisionPlataforma"] == DBNull.Value
                    ? (decimal?)null : Convert.ToDecimal(fila["porcentajeComisionPlataforma"]),
                ImporteComisionPlataforma = fila["importeComisionPlataforma"] == DBNull.Value
                    ? (decimal?)null : Convert.ToDecimal(fila["importeComisionPlataforma"]),
                FechaPago = Convert.ToDateTime(fila["fechaPago"]),
                Origen = Texto(fila, "origen"),
                IdentificadorExterno = Texto(fila, "identificadorExterno"),
                MedioInformado = Texto(fila, "medioInformado"),
                EstadoInformado = Texto(fila, "estadoInformado"),
                FechaOperacion = Fecha(fila, "fechaOperacion"),
                Observacion = Texto(fila, "observacion"),
                NombreUsuario = Texto(fila, "nombreUsuario"),
                CorreoUsuario = Texto(fila, "correoUsuario"),
                NombreEspacio = Texto(fila, "nombreEspacio"),
                IdUsuarioGestor = Entero(fila, "idUsuarioGestor"),
                NombreGestor = Texto(fila, "nombreGestor"),
                CorreoGestor = Texto(fila, "correoGestor"),
                EstadoReserva = Texto(fila, "estadoReserva"),
                EstadoPagoReserva = Texto(fila, "estadoPagoReserva"),
                FechaSolicitada = Fecha(fila, "fechaSolicitada"),
                MinutoDesde = Entero(fila, "minutoDesde"),
                MinutoHasta = Entero(fila, "minutoHasta"),
                FechaLimitePago = Fecha(fila, "fechaLimitePago"),
                IdComprobanteFactura = Entero(fila, "idComprobante"),
                NumeroFactura = Texto(fila, "numeroFactura"),
                FechaFactura = Fecha(fila, "fechaFactura"),
                EstadoFactura = Texto(fila, "estadoFactura"),
                NombreInternoRegistro = Texto(fila, "nombreInternoRegistro")
            };
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
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? null : fila[columna].ToString();
        }

        private static int? Entero(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? (int?)null : Convert.ToInt32(fila[columna]);
        }

        private static DateTime? Fecha(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila[columna]);
        }
    }
}
