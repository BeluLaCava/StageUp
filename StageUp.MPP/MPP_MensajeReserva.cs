using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Mensajería privada por reserva (script 73).
    public class MPP_MensajeReserva
    {
        public int Insertar(MensajeReserva oMensaje)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_MensajeReserva_Insertar",
                new Hashtable
                {
                    { "@idReserva", oMensaje.IdReserva },
                    { "@idUsuarioExternoEmisor", oMensaje.IdUsuarioExternoEmisor },
                    { "@texto", oMensaje.Texto }
                });

            return Convert.ToInt32(resultado);
        }

        public List<MensajeReserva> ListarPorReserva(Reserva oReserva, UsuarioExterno oUsuario)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_MensajeReserva_ListarPorReserva",
                new Hashtable
                {
                    { "@idReserva", oReserva.IdReserva },
                    { "@idUsuarioExterno", oUsuario.IdUsuarioExterno }
                });

            List<MensajeReserva> lista = new List<MensajeReserva>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new MensajeReserva
                {
                    IdMensajeReserva = Convert.ToInt32(fila["idMensajeReserva"]),
                    IdReserva = Convert.ToInt32(fila["idReserva"]),
                    IdUsuarioExternoEmisor = Convert.ToInt32(fila["idUsuarioExternoEmisor"]),
                    Texto = fila["texto"].ToString(),
                    FechaEnvio = Convert.ToDateTime(fila["fechaEnvio"]),
                    FechaLectura = fila["fechaLectura"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaLectura"]),
                    NombreEmisor = fila["nombreEmisor"] == DBNull.Value ? null : fila["nombreEmisor"].ToString(),
                    ApellidoEmisor = fila["apellidoEmisor"] == DBNull.Value ? null : fila["apellidoEmisor"].ToString()
                });
            }
            return lista;
        }

        public int MarcarLeidos(Reserva oReserva, UsuarioExterno oUsuario, List<int> idsMensajes)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_MensajeReserva_MarcarLeidos",
                new Hashtable
                {
                    { "@idReserva", oReserva.IdReserva },
                    { "@idUsuarioExterno", oUsuario.IdUsuarioExterno },
                    { "@idsMensajes", string.Join(",", idsMensajes) }
                });

            return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
        }

        public List<MensajesNoLeidosReserva> NoLeidosPorUsuario(UsuarioExterno oUsuario)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_MensajeReserva_NoLeidosPorUsuario",
                new Hashtable { { "@idUsuarioExterno", oUsuario.IdUsuarioExterno } });

            List<MensajesNoLeidosReserva> lista = new List<MensajesNoLeidosReserva>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new MensajesNoLeidosReserva
                {
                    IdReserva = Convert.ToInt32(fila["idReserva"]),
                    Rol = fila["rol"].ToString(),
                    Cantidad = Convert.ToInt32(fila["cantidad"])
                });
            }
            return lista;
        }
    }
}
