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

        public List<Notificacion> ListarPorUsuario(UsuarioExterno usuarioExterno, int cantidad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Notificacion_ListarPorUsuario",
                new Hashtable { { "@idUsuarioExterno", usuarioExterno.IdUsuarioExterno }, { "@cantidad", cantidad } });

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

        // Ítem 36 del checklist de correcciones: antes, validar que la
        // notificación fuera del usuario se hacía en la BLL trayendo TODAS
        // sus notificaciones a memoria. Ahora la validación de pertenencia
        // va en el propio WHERE del UPDATE (ver sp_Notificacion_MarcarLeida
        // en Database/34_OptimizarReputacionYNotificaciones.sql): si la
        // notificación no es del usuario, el UPDATE simplemente no afecta
        // filas, igual que antes cuando la validación en memoria fallaba.
        public void MarcarLeida(Notificacion notificacion)
        {
            Conexion.Instance.Guardar(
                "sp_Notificacion_MarcarLeida",
                new Hashtable
                {
                    { "@idNotificacion", notificacion.IdNotificacion },
                    { "@idUsuarioExterno", notificacion.IdUsuarioExterno }
                });
        }

        public void MarcarTodasLeidas(UsuarioExterno usuarioExterno)
        {
            Conexion.Instance.Guardar(
                "sp_Notificacion_MarcarTodasLeidas",
                new Hashtable { { "@idUsuarioExterno", usuarioExterno.IdUsuarioExterno } });
        }

        public int ContarNoLeidas(UsuarioExterno usuarioExterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Notificacion_ContarNoLeidas",
                new Hashtable { { "@idUsuarioExterno", usuarioExterno.IdUsuarioExterno } });

            return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
        }
    }
}
