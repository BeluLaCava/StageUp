using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Notificacion
    {
        public void Insertar(Notificacion notificacion)
        {
            Conexion.Instance.Guardar(
                "sp_Notificacion_Insertar",
                new Hashtable
                {
                    { "@idUsuarioExterno", notificacion.IdUsuarioExterno },
                    { "@tipo", notificacion.Tipo },
                    { "@mensaje", notificacion.Mensaje },
                    { "@urlDestino", (object)notificacion.UrlDestino ?? DBNull.Value }
                });
        }

        public List<Notificacion> ListarPorUsuario(int idUsuarioExterno, int cantidad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Notificacion_ListarPorUsuario",
                new Hashtable { { "@idUsuarioExterno", idUsuarioExterno }, { "@cantidad", cantidad } });

            List<Notificacion> notificaciones = new List<Notificacion>();
            foreach (DataRow fila in tabla.Rows)
            {
                notificaciones.Add(new Notificacion
                {
                    IdNotificacion = Convert.ToInt32(fila["idNotificacion"]),
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    Tipo = fila["tipo"].ToString(),
                    Mensaje = fila["mensaje"].ToString(),
                    UrlDestino = fila["urlDestino"] == DBNull.Value ? null : fila["urlDestino"].ToString(),
                    Leida = Convert.ToBoolean(fila["leida"]),
                    FechaCreacion = Convert.ToDateTime(fila["fechaCreacion"])
                });
            }

            return notificaciones;
        }

        public void MarcarLeida(int idNotificacion)
        {
            Conexion.Instance.Guardar(
                "sp_Notificacion_MarcarLeida",
                new Hashtable { { "@idNotificacion", idNotificacion } });
        }

        public void MarcarTodasLeidas(int idUsuarioExterno)
        {
            Conexion.Instance.Guardar(
                "sp_Notificacion_MarcarTodasLeidas",
                new Hashtable { { "@idUsuarioExterno", idUsuarioExterno } });
        }

        public int ContarNoLeidas(int idUsuarioExterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Notificacion_ContarNoLeidas",
                new Hashtable { { "@idUsuarioExterno", idUsuarioExterno } });

            return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
        }
    }
}
