using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Mensajería privada por reserva: un canal entre el solicitante y el
    // gestor del espacio de cada reserva.
    //   * Solo esos dos usuarios ven y escriben mensajes (se valida acá y
    //     otra vez en los SP).
    //   * Cada mensaje tiene emisor, texto, fecha y hora, y leído/no leído;
    //     se marca leído cuando el otro participante abre la conversación.
    //   * Cada mensaje nuevo genera una notificación para el otro
    //     participante.
    //   * Con la reserva rechazada o cancelada la conversación queda como
    //     registro, pero ya no se pueden enviar mensajes.
    //   * Es independiente de las preguntas públicas de los espacios, de las
    //     solicitudes de soporte, del asistente y de las calificaciones.
    public class BLL_MensajeReserva
    {
        public const int LongitudMaxima = 1000;

        private const string TipoEntidadBitacora = "MensajeReserva";
        private const string MensajeError = "No pudimos enviar el mensaje en este momento. Intentá nuevamente.";
        private static readonly TimeSpan VentanaDuplicado = TimeSpan.FromMinutes(2);

        private readonly MPP_MensajeReserva _mpp = new MPP_MensajeReserva();
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();
        private readonly BLL_Notificacion _bllNotificacion = new BLL_Notificacion();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public static bool EsParticipante(Reserva reserva, int idUsuarioExterno)
        {
            return reserva != null &&
                (reserva.IdUsuarioExternoSolicitante == idUsuarioExterno || reserva.IdUsuarioGestor == idUsuarioExterno);
        }

        // Con la reserva rechazada o cancelada ya no se pueden enviar mensajes.
        public static bool AdmiteMensajes(Reserva reserva)
        {
            return reserva != null &&
                reserva.EstadoReserva != EstadoReserva.Rechazada.ToString() &&
                reserva.EstadoReserva != EstadoReserva.Cancelada.ToString();
        }

        // Conversación de una reserva que el usuario ya puede ver (la página
        // la obtiene con BLL_Reserva.ObtenerDetalle, que valida el acceso).
        public ResultadoOperacion<List<MensajeReserva>> Listar(Reserva reserva, int idUsuarioExterno)
        {
            if (!EsParticipante(reserva, idUsuarioExterno))
            {
                return ResultadoOperacion<List<MensajeReserva>>.Error("No encontramos esa reserva entre las tuyas.");
            }

            try
            {
                return ResultadoOperacion<List<MensajeReserva>>.Ok(
                    _mpp.ListarPorReserva(reserva, new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno }));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<MensajeReserva>>.Error("No pudimos cargar los mensajes de la reserva en este momento.");
            }
        }

        // Marca como leídos solo los mensajes indicados (los que se le
        // mostraron al usuario), y solo si los recibió él en esa reserva.
        public int MarcarLeidos(Reserva reserva, int idUsuarioExterno, IEnumerable<int> idsMostrados)
        {
            List<int> ids = idsMostrados == null ? new List<int>() : idsMostrados.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0 || !EsParticipante(reserva, idUsuarioExterno))
            {
                return 0;
            }

            try
            {
                return _mpp.MarcarLeidos(reserva, new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno }, ids);
            }
            catch (ErrorAccesoDatosException)
            {
                return 0;
            }
        }

        public ResultadoOperacion<int> Enviar(int idReserva, int idUsuarioExterno, string texto)
        {
            string mensaje = Limpiar(texto);
            if (string.IsNullOrEmpty(mensaje))
            {
                return ResultadoOperacion<int>.Error("Escribí el mensaje antes de enviarlo.");
            }

            if (mensaje.Length > LongitudMaxima)
            {
                return ResultadoOperacion<int>.Error("El mensaje no puede superar los " + LongitudMaxima + " caracteres.");
            }

            ResultadoOperacion<Reserva> detalle = _bllReserva.ObtenerDetalle(idReserva, idUsuarioExterno);
            if (!detalle.Exitoso)
            {
                return ResultadoOperacion<int>.Error(detalle.Mensaje);
            }

            Reserva reserva = detalle.Valor;
            if (!AdmiteMensajes(reserva))
            {
                return ResultadoOperacion<int>.Error("La reserva está " + (reserva.EstadoReserva ?? string.Empty).ToLowerInvariant() +
                    ": la conversación queda como registro, pero ya no se pueden enviar mensajes.");
            }

            try
            {
                // Evita el mismo mensaje dos veces seguidas (doble clic o reenvío).
                UsuarioExterno usuario = new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno };
                MensajeReserva ultimo = _mpp.ListarPorReserva(reserva, usuario).LastOrDefault();
                if (ultimo != null && ultimo.IdUsuarioExternoEmisor == idUsuarioExterno && ultimo.Texto == mensaje &&
                    DateTime.Now - ultimo.FechaEnvio < VentanaDuplicado)
                {
                    return ResultadoOperacion<int>.Error("Ya enviaste este mismo mensaje.");
                }

                int idMensaje = _mpp.Insertar(new MensajeReserva
                {
                    IdReserva = idReserva,
                    IdUsuarioExternoEmisor = idUsuarioExterno,
                    Texto = mensaje
                });

                bool esSolicitante = reserva.IdUsuarioExternoSolicitante == idUsuarioExterno;
                int idDestinatario = esSolicitante ? reserva.IdUsuarioGestor : reserva.IdUsuarioExternoSolicitante;
                string emisor = esSolicitante ? reserva.NombreSolicitante : reserva.NombreGestor;

                // No se guarda el texto en la bitácora: es una conversación privada.
                Bitacora(idUsuarioExterno, idMensaje,
                    "Mensaje privado en la reserva N° " + idReserva + " (" + (esSolicitante ? "del solicitante al gestor" : "del gestor al solicitante") + ").");

                _bllNotificacion.Notificar(idDestinatario, TipoNotificacion.MensajeReserva,
                    (string.IsNullOrWhiteSpace(emisor) ? "Tenés un mensaje nuevo" : emisor.Trim() + " te escribió") +
                    " sobre la reserva N° " + idReserva + " en \"" + reserva.NombreEspacio + "\": \"" + Recortar(mensaje, 100) + "\"",
                    "~/DetalleReserva.aspx?id=" + idReserva + "&ver=mensajes");

                return ResultadoOperacion<int>.Ok(idMensaje, "Mensaje enviado.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                if (ex.NumeroErrorSql == 51051)
                {
                    return ResultadoOperacion<int>.Error("La reserva ya no admite mensajes.");
                }

                if (ex.NumeroErrorSql == 51050)
                {
                    return ResultadoOperacion<int>.Error("No encontramos esa reserva entre las tuyas.");
                }

                return ResultadoOperacion<int>.Error(MensajeError);
            }
        }

        // Mensajes sin leer del usuario por reserva (vacío si falla la base:
        // los contadores no deben romper la página).
        public List<MensajesNoLeidosReserva> NoLeidos(int idUsuarioExterno)
        {
            try
            {
                return _mpp.NoLeidosPorUsuario(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<MensajesNoLeidosReserva>();
            }
        }

        public Dictionary<int, int> NoLeidosPorReserva(int idUsuarioExterno, string rol)
        {
            return NoLeidos(idUsuarioExterno)
                .Where(n => rol == null || n.Rol == rol)
                .GroupBy(n => n.IdReserva)
                .ToDictionary(g => g.Key, g => g.Sum(n => n.Cantidad));
        }

        private void Bitacora(int idUsuarioExterno, int idMensaje, string descripcion)
        {
            try
            {
                _bitacora.Registrar(idUsuarioExterno, "ALTA", TipoEntidadBitacora, idMensaje, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }

        // Respeta los saltos de línea; recorta espacios y líneas vacías de más.
        private static string Limpiar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            string limpio = texto.Replace("\r\n", "\n").Trim();
            limpio = Regex.Replace(limpio, @"[ \t]+", " ");
            limpio = Regex.Replace(limpio, @"\n{3,}", "\n\n");
            return limpio;
        }

        private static string Recortar(string texto, int largo)
        {
            string unaLinea = Regex.Replace(texto, @"\s+", " ");
            return unaLinea.Length <= largo ? unaLinea : unaLinea.Substring(0, largo - 3) + "...";
        }
    }
}
