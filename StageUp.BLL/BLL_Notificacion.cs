using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Notificaciones reales de usuarios externos, generadas por eventos que
    // ya existen en el sistema (solicitud de reserva recibida, reserva
    // aceptada o rechazada). Reemplaza los 3 ítems hardcodeados que había en
    // Site.Master a modo de mockup.
    public class BLL_Notificacion
    {
        private readonly MPP_Notificacion _mppNotificacion = new MPP_Notificacion();

        private const int CantidadPorDefecto = 20;

        // Pensado para llamarse desde otras BLL (ej. BLL_Reserva) justo
        // después de que la operación principal ya se realizó con éxito. Una
        // notificación que falla no debe hacer fallar la operación que la
        // originó, por eso no propaga la excepción.
        public void Notificar(int idUsuarioExterno, TipoNotificacion tipo, string mensaje, string urlDestino)
        {
            try
            {
                _mppNotificacion.Insertar(new Notificacion
                {
                    IdUsuarioExterno = idUsuarioExterno,
                    Tipo = tipo.ToString(),
                    Mensaje = mensaje,
                    UrlDestino = urlDestino
                });
            }
            catch (ErrorAccesoDatosException)
            {
                // No se propaga: la operación que originó la notificación ya
                // se completó correctamente, no tiene que fallar por esto.
            }
        }

        public List<Notificacion> ListarPorUsuario(int idUsuarioExterno, int cantidad = CantidadPorDefecto)
        {
            try
            {
                return _mppNotificacion.ListarPorUsuario(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno }, cantidad);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Notificacion>();
            }
        }

        public int ContarNoLeidas(int idUsuarioExterno)
        {
            try
            {
                return _mppNotificacion.ContarNoLeidas(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return 0;
            }
        }

        // Ítem 36 del checklist de correcciones: antes se traían TODAS las
        // notificaciones del usuario (cantidad = int.MaxValue) solo para
        // validar en memoria que la notificación pedida le pertenece. Ahora
        // esa validación va directo en el WHERE del UPDATE (ver
        // MPP_Notificacion.MarcarLeida / sp_Notificacion_MarcarLeida): si no
        // es del usuario, no se actualiza nada, mismo comportamiento visible
        // que antes.
        public void MarcarLeida(int idNotificacion, int idUsuarioExterno)
        {
            try
            {
                _mppNotificacion.MarcarLeida(new Notificacion
                {
                    IdNotificacion = idNotificacion,
                    IdUsuarioExterno = idUsuarioExterno
                });
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }

        public void MarcarTodasLeidas(int idUsuarioExterno)
        {
            try
            {
                _mppNotificacion.MarcarTodasLeidas(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }
    }
}
