using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-002-002 Gestionar transacciones económicas y comisiones (script 63).
    public class MPP_TransaccionEconomica
    {
        // Devuelve el id de la transacción y si es nueva (false si ya estaba
        // registrada para el mismo pago o cargo).
        public int Registrar(TransaccionEconomica oTransaccion, out bool esNueva)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TransaccionEconomica_Registrar",
                new Hashtable
                {
                    { "@tipoOperacion", oTransaccion.TipoOperacion },
                    { "@idReserva", (object)oTransaccion.IdReserva ?? DBNull.Value },
                    { "@idPago", (object)oTransaccion.IdPago ?? DBNull.Value },
                    { "@idComprobante", (object)oTransaccion.IdComprobante ?? DBNull.Value },
                    { "@montoTotal", oTransaccion.MontoTotal },
                    { "@porcentajeComision", (object)oTransaccion.PorcentajeComision ?? DBNull.Value },
                    { "@importeComision", (object)oTransaccion.ImporteComision ?? DBNull.Value },
                    { "@importeGestor", (object)oTransaccion.ImporteGestor ?? DBNull.Value },
                    { "@porcentajeCargo", (object)oTransaccion.PorcentajeCargo ?? DBNull.Value },
                    { "@estadoPago", oTransaccion.EstadoPago },
                    { "@referenciaPago", (object)oTransaccion.ReferenciaPago ?? DBNull.Value },
                    { "@estadoRegistro", oTransaccion.EstadoRegistro ?? "Registrada" },
                    { "@observacion", (object)oTransaccion.Observacion ?? DBNull.Value }
                });

            DataRow fila = tabla.Rows[0];
            esNueva = Convert.ToBoolean(fila["esNueva"]);
            return Convert.ToInt32(fila["idTransaccion"]);
        }

        public void ActualizarEstadoPorReserva(TransaccionEconomica oTransaccion)
        {
            Conexion.Instance.Leer(
                "sp_TransaccionEconomica_ActualizarEstadoPorReserva",
                new Hashtable
                {
                    { "@idReserva", oTransaccion.IdReserva },
                    { "@estadoPago", oTransaccion.EstadoPago },
                    { "@observacion", (object)oTransaccion.Observacion ?? DBNull.Value }
                });
        }

        public List<TransaccionEconomica> Listar(FiltroTransacciones oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TransaccionEconomica_Listar",
                new Hashtable
                {
                    { "@tipoOperacion", string.IsNullOrEmpty(oFiltro.TipoOperacion) ? (object)DBNull.Value : oFiltro.TipoOperacion },
                    { "@estadoPago", string.IsNullOrEmpty(oFiltro.EstadoPago) ? (object)DBNull.Value : oFiltro.EstadoPago },
                    { "@desde", oFiltro.Desde.HasValue ? (object)oFiltro.Desde.Value.Date : DBNull.Value },
                    { "@hasta", oFiltro.Hasta.HasValue ? (object)oFiltro.Hasta.Value.Date : DBNull.Value },
                    { "@texto", string.IsNullOrWhiteSpace(oFiltro.Texto) ? (object)DBNull.Value : oFiltro.Texto.Trim() }
                });

            List<TransaccionEconomica> lista = new List<TransaccionEconomica>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public int Contar()
        {
            object total = Conexion.Instance.LeerEscalar("sp_TransaccionEconomica_Contar");
            return total == null || total == DBNull.Value ? 0 : Convert.ToInt32(total);
        }

        public TransaccionEconomica ObtenerDetalle(TransaccionEconomica oTransaccion)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TransaccionEconomica_ObtenerDetalle",
                new Hashtable { { "@idTransaccion", oTransaccion.IdTransaccion } });
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            TransaccionEconomica t = Mapear(fila);
            t.CorreoSolicitante = Texto(fila, "correoSolicitante");
            t.CorreoGestor = Texto(fila, "correoGestor");
            t.FechaSolicitada = Fecha(fila, "fechaSolicitada");
            t.MinutoDesde = Entero(fila, "minutoDesde");
            t.MinutoHasta = Entero(fila, "minutoHasta");
            t.ImporteReserva = Decimal(fila, "importeEstimado");
            t.EstadoReserva = Texto(fila, "estadoReserva");
            t.FechaCancelacion = Fecha(fila, "fechaCancelacion");

            string medio = Texto(fila, "medioInformado");
            if (medio == null && Texto(fila, "marcaTarjeta") != null)
            {
                medio = "Tarjeta " + Texto(fila, "marcaTarjeta") + " terminada en " + Texto(fila, "ultimosDigitos");
            }

            t.MedioPago = medio;
            return t;
        }

        private static TransaccionEconomica Mapear(DataRow fila)
        {
            return new TransaccionEconomica
            {
                IdTransaccion = Convert.ToInt32(fila["idTransaccion"]),
                Codigo = Texto(fila, "codigo"),
                TipoOperacion = Texto(fila, "tipoOperacion"),
                IdReserva = Entero(fila, "idReserva"),
                IdPago = Entero(fila, "idPago"),
                IdComprobante = Entero(fila, "idComprobante"),
                Moneda = Texto(fila, "moneda"),
                MontoTotal = Convert.ToDecimal(fila["montoTotal"]),
                PorcentajeComision = Decimal(fila, "porcentajeComision"),
                ImporteComision = Decimal(fila, "importeComision"),
                ImporteGestor = Decimal(fila, "importeGestor"),
                PorcentajeCargo = Decimal(fila, "porcentajeCargo"),
                EstadoPago = Texto(fila, "estadoPago"),
                ReferenciaPago = Texto(fila, "referenciaPago"),
                EstadoRegistro = Texto(fila, "estadoRegistro"),
                Observacion = Texto(fila, "observacion"),
                FechaGeneracion = Convert.ToDateTime(fila["fechaGeneracion"]),
                NombreSolicitante = Texto(fila, "nombreSolicitante"),
                NombreGestor = Texto(fila, "nombreGestor"),
                NombreEspacio = Texto(fila, "nombreEspacio"),
                NumeroFactura = Texto(fila, "numeroFactura"),
                NumeroComprobante = Texto(fila, "numeroComprobante")
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

        private static decimal? Decimal(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(fila[columna]);
        }

        private static DateTime? Fecha(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila[columna]);
        }
    }
}
