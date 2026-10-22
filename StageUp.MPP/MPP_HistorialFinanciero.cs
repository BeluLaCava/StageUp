using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

    public class MPP_HistorialFinanciero
    {
        public List<OperacionFinanciera> Listar(FiltroHistorialFinanciero oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_HistorialFinanciero_Listar",
                new Hashtable
                {
                    { "@tipoOperacion", string.IsNullOrEmpty(oFiltro.TipoOperacion) ? (object)DBNull.Value : oFiltro.TipoOperacion },
                    { "@estadoPago", string.IsNullOrEmpty(oFiltro.EstadoPago) ? (object)DBNull.Value : oFiltro.EstadoPago },
                    { "@idUsuarioGestor", oFiltro.IdUsuarioGestor.HasValue ? (object)oFiltro.IdUsuarioGestor.Value : DBNull.Value },
                    { "@desde", oFiltro.Desde.HasValue ? (object)oFiltro.Desde.Value.Date : DBNull.Value },
                    { "@hasta", oFiltro.Hasta.HasValue ? (object)oFiltro.Hasta.Value.Date : DBNull.Value },
                    { "@texto", string.IsNullOrWhiteSpace(oFiltro.Texto) ? (object)DBNull.Value : oFiltro.Texto.Trim() }
                });

            List<OperacionFinanciera> lista = new List<OperacionFinanciera>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public int Contar()
        {
            object total = Conexion.Instance.LeerEscalar("sp_HistorialFinanciero_Contar");
            return total == null || total == DBNull.Value ? 0 : Convert.ToInt32(total);
        }

        public List<UsuarioExterno> ListarGestores()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_HistorialFinanciero_ListarGestores");
            List<UsuarioExterno> lista = new List<UsuarioExterno>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new UsuarioExterno
                {
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    Nombre = Texto(fila, "nombre"),
                    CorreoElectronico = Texto(fila, "correoElectronico")
                });
            }
            return lista;
        }

        public OperacionFinanciera ObtenerDetalle(OperacionFinanciera oOperacion)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_HistorialFinanciero_ObtenerDetalle",
                new Hashtable
                {
                    { "@origen", oOperacion.Origen },
                    { "@idOrigen", oOperacion.IdOrigen }
                });
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            OperacionFinanciera o = Mapear(fila);
            o.CorreoSolicitante = Texto(fila, "correoSolicitante");
            o.CorreoGestor = Texto(fila, "correoGestor");
            o.FechaSolicitada = Fecha(fila, "fechaSolicitada");
            o.MinutoDesde = Entero(fila, "minutoDesde");
            o.MinutoHasta = Entero(fila, "minutoHasta");
            o.ImporteReserva = Decimal(fila, "importeEstimado");
            o.EstadoReserva = Texto(fila, "estadoReserva");
            o.EstadoPagoReserva = Texto(fila, "estadoPagoReserva");
            o.FechaCancelacion = Fecha(fila, "fechaCancelacion");
            o.FechaLimitePago = Fecha(fila, "fechaLimitePago");
            o.EstadoPagoRegistrado = Texto(fila, "estadoPagoRegistrado");
            o.ImporteTarjeta = Decimal(fila, "importeTarjeta");
            o.ImporteSaldo = Decimal(fila, "importeSaldo");
            o.CodigoAutorizacion = Texto(fila, "codigoAutorizacion");

            string medio = Texto(fila, "medioInformado");
            if (medio == null && Texto(fila, "marcaTarjeta") != null)
            {
                medio = "Tarjeta " + Texto(fila, "marcaTarjeta") + " terminada en " + Texto(fila, "ultimosDigitos");
            }
            else if (medio == null && o.IdPago.HasValue && (o.ImporteSaldo ?? 0m) > 0m && (o.ImporteTarjeta ?? 0m) == 0m)
            {
                medio = "Saldo a favor de la cuenta corriente";
            }

            o.MedioPago = medio;
            return o;
        }

        public List<Comprobante> ListarComprobantes(OperacionFinanciera oOperacion)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_HistorialFinanciero_ListarComprobantes",
                new Hashtable
                {
                    { "@idReserva", oOperacion.IdReserva.HasValue ? (object)oOperacion.IdReserva.Value : DBNull.Value },
                    { "@idPago", oOperacion.IdPago.HasValue ? (object)oOperacion.IdPago.Value : DBNull.Value },
                    { "@idComprobante", oOperacion.IdComprobante.HasValue ? (object)oOperacion.IdComprobante.Value : DBNull.Value }
                });

            List<Comprobante> lista = new List<Comprobante>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new Comprobante
                {
                    IdComprobante = Convert.ToInt32(fila["idComprobante"]),
                    Tipo = Texto(fila, "tipo"),
                    Numero = Texto(fila, "numero"),
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    RolCuenta = Texto(fila, "rolCuenta"),
                    IdReserva = Entero(fila, "idReserva"),
                    Importe = Convert.ToDecimal(fila["importe"]),
                    Moneda = Texto(fila, "moneda"),
                    Origen = Texto(fila, "origen"),
                    Motivo = Texto(fila, "motivo"),
                    Estado = Texto(fila, "estado"),
                    FechaEmision = Convert.ToDateTime(fila["fechaEmision"]),
                    FechaAnulacion = Fecha(fila, "fechaAnulacion"),
                    MotivoAnulacion = Texto(fila, "motivoAnulacion"),
                    NombreUsuario = Texto(fila, "nombreUsuario")
                });
            }
            return lista;
        }


        public List<MovimientoCuentaCorriente> ListarMovimientos(OperacionFinanciera oOperacion)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_HistorialFinanciero_ListarMovimientos",
                new Hashtable
                {
                    { "@idReserva", oOperacion.IdReserva.HasValue ? (object)oOperacion.IdReserva.Value : DBNull.Value },
                    { "@idPago", oOperacion.IdPago.HasValue ? (object)oOperacion.IdPago.Value : DBNull.Value },
                    { "@idComprobante", oOperacion.IdComprobante.HasValue ? (object)oOperacion.IdComprobante.Value : DBNull.Value },
                    { "@idMovimiento", oOperacion.Origen == "LQ" ? (object)oOperacion.IdOrigen : DBNull.Value }
                });

            List<MovimientoCuentaCorriente> lista = new List<MovimientoCuentaCorriente>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new MovimientoCuentaCorriente
                {
                    IdMovimiento = Convert.ToInt32(fila["idMovimiento"]),
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    RolCuenta = Texto(fila, "rolCuenta"),
                    TipoMovimiento = Texto(fila, "tipoMovimiento"),
                    Importe = Convert.ToDecimal(fila["importe"]),
                    Moneda = Texto(fila, "moneda"),
                    Descripcion = Texto(fila, "descripcion"),
                    IdReserva = Entero(fila, "idReserva"),
                    IdPago = Entero(fila, "idPago"),
                    IdComprobante = Entero(fila, "idComprobante"),
                    FechaMovimiento = Convert.ToDateTime(fila["fechaMovimiento"]),
                    NombreUsuario = Texto(fila, "nombreUsuario")
                });
            }
            return lista;
        }

        private static OperacionFinanciera Mapear(DataRow fila)
        {
            return new OperacionFinanciera
            {
                Origen = Texto(fila, "origen"),
                IdOrigen = Convert.ToInt32(fila["idOrigen"]),
                Codigo = Texto(fila, "codigo"),
                TipoOperacion = Texto(fila, "tipoOperacion"),
                IdReserva = Entero(fila, "idReserva"),
                IdPago = Entero(fila, "idPago"),
                IdComprobante = Entero(fila, "idComprobante"),
                IdUsuarioSolicitante = Entero(fila, "idUsuarioSolicitante"),
                IdUsuarioGestor = Entero(fila, "idUsuarioGestor"),
                IdEspacioArtistico = Entero(fila, "idEspacioArtistico"),
                Moneda = Texto(fila, "moneda"),
                MontoTotal = Convert.ToDecimal(fila["montoTotal"]),
                PorcentajeComision = Decimal(fila, "porcentajeComision"),
                ImporteComision = Decimal(fila, "importeComision"),
                ImporteGestor = Decimal(fila, "importeGestor"),
                PorcentajeCargo = Decimal(fila, "porcentajeCargo"),
                ImporteCargo = Decimal(fila, "importeCargo"),
                EstadoPago = Texto(fila, "estadoPago"),
                ReferenciaPago = Texto(fila, "referenciaPago"),
                EstadoRegistro = Texto(fila, "estadoRegistro"),
                FechaOperacion = Convert.ToDateTime(fila["fechaOperacion"]),
                Observacion = Texto(fila, "observacion"),
                NombreSolicitante = Texto(fila, "nombreSolicitante"),
                NombreGestor = Texto(fila, "nombreGestor"),
                NombreEspacio = Texto(fila, "nombreEspacio"),
                NumeroComprobante = Texto(fila, "numeroComprobante"),
                TipoComprobante = Texto(fila, "tipoComprobante")
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
