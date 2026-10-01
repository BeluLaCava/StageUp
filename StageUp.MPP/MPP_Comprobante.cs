using System;
using System.Collections;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Notas de crédito y débito (ítem 5C, script 49).
    public class MPP_Comprobante
    {
        public ResultadoMovimientoDinero EmitirManual(Comprobante oComprobante)
        {
            return MPP_Pago.MapearResultado(Conexion.Instance.Leer(
                "sp_Comprobante_EmitirManual",
                new Hashtable
                {
                    { "@tipo", oComprobante.Tipo },
                    { "@idUsuarioExterno", oComprobante.IdUsuarioExterno },
                    { "@rolCuenta", oComprobante.RolCuenta },
                    { "@importe", oComprobante.Importe },
                    { "@moneda", oComprobante.Moneda },
                    { "@motivo", oComprobante.Motivo },
                    { "@idReserva", oComprobante.IdReserva },
                    { "@idUsuarioInterno", oComprobante.IdUsuarioInternoEmisor }
                }), "idComprobante");
        }

        public ResultadoMovimientoDinero Anular(Comprobante oComprobante)
        {
            return MPP_Pago.MapearResultado(Conexion.Instance.Leer(
                "sp_Comprobante_Anular",
                new Hashtable
                {
                    { "@idComprobante", oComprobante.IdComprobante },
                    { "@motivo", oComprobante.MotivoAnulacion },
                    { "@idUsuarioInterno", oComprobante.IdUsuarioInternoAnulacion }
                }), null);
        }

        public Comprobante ObtenerPorId(Comprobante oComprobante)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Comprobante_ObtenerPorId",
                new Hashtable { { "@idComprobante", oComprobante.IdComprobante } });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            return new Comprobante
            {
                IdComprobante = Convert.ToInt32(fila["idComprobante"]),
                Tipo = fila["tipo"].ToString(),
                Numero = fila["numero"].ToString(),
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                RolCuenta = fila["rolCuenta"].ToString(),
                IdReserva = fila["idReserva"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idReserva"]),
                Importe = Convert.ToDecimal(fila["importe"]),
                Moneda = fila["moneda"].ToString(),
                Origen = fila["origen"].ToString(),
                Motivo = fila["motivo"].ToString(),
                Estado = fila["estado"].ToString(),
                IdUsuarioInternoEmisor = fila["idUsuarioInternoEmisor"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idUsuarioInternoEmisor"]),
                FechaEmision = Convert.ToDateTime(fila["fechaEmision"]),
                FechaAnulacion = fila["fechaAnulacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaAnulacion"]),
                MotivoAnulacion = fila["motivoAnulacion"] == DBNull.Value ? null : fila["motivoAnulacion"].ToString(),
                NombreUsuario = fila["nombreUsuario"] == DBNull.Value ? null : fila["nombreUsuario"].ToString(),
                CorreoUsuario = fila["correoUsuario"] == DBNull.Value ? null : fila["correoUsuario"].ToString()
            };
        }
    }
}
