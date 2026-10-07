using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-001-011 Consultar notificaciones y acceder a elementos relacionados.
    //
    // Notificaciones de usuarios externos, generadas por eventos reales del
    // sistema (reservas, pagos, soporte, habilitación como gestor). Cada una
    // tiene título, descripción, fecha, estado de lectura, tipo y la página
    // del elemento relacionado. Solo las ve el usuario al que pertenecen.
    public class BLL_Notificacion
    {
        private readonly MPP_Notificacion _mppNotificacion = new MPP_Notificacion();

        private const int CantidadPorDefecto = 20;
        public const int CantidadCentro = 200;

        public const string FiltroTodas = "Todas";
        public const string FiltroNoLeidas = "NoLeidas";
        public const string FiltroLeidas = "Leidas";

        private static readonly Dictionary<string, string> TitulosPorTipo = new Dictionary<string, string>
        {
            { TipoNotificacion.SolicitudReserva.ToString(), "Nueva solicitud de reserva" },
            { TipoNotificacion.ReservaAceptada.ToString(), "Reserva aceptada" },
            { TipoNotificacion.ReservaRechazada.ToString(), "Reserva rechazada" },
            { TipoNotificacion.ReservaCancelada.ToString(), "Reserva cancelada" },
            { TipoNotificacion.RecordatorioReserva.ToString(), "Recordatorio de reserva" },
            { TipoNotificacion.HabilitacionGestorAprobada.ToString(), "Habilitación como gestor aprobada" },
            { TipoNotificacion.HabilitacionGestorRechazada.ToString(), "Habilitación como gestor rechazada" },
            { TipoNotificacion.RespuestaTicket.ToString(), "Respuesta de soporte" },
            { TipoNotificacion.PagoAprobado.ToString(), "Pago aprobado" },
            { TipoNotificacion.PagoRecibido.ToString(), "Pago recibido" },
            { TipoNotificacion.PagoVencido.ToString(), "Plazo de pago vencido" },
            { TipoNotificacion.ComprobanteEmitido.ToString(), "Comprobante emitido" },
            { TipoNotificacion.LiquidacionRegistrada.ToString(), "Liquidación registrada" }
        };

        // Categoría que se muestra junto a cada notificación.
        private static readonly Dictionary<string, string> CategoriasPorTipo = new Dictionary<string, string>
        {
            { TipoNotificacion.SolicitudReserva.ToString(), "Reservas" },
            { TipoNotificacion.ReservaAceptada.ToString(), "Reservas" },
            { TipoNotificacion.ReservaRechazada.ToString(), "Reservas" },
            { TipoNotificacion.ReservaCancelada.ToString(), "Reservas" },
            { TipoNotificacion.RecordatorioReserva.ToString(), "Recordatorio" },
            { TipoNotificacion.HabilitacionGestorAprobada.ToString(), "Gestor" },
            { TipoNotificacion.HabilitacionGestorRechazada.ToString(), "Gestor" },
            { TipoNotificacion.RespuestaTicket.ToString(), "Soporte" },
            { TipoNotificacion.PagoAprobado.ToString(), "Pagos" },
            { TipoNotificacion.PagoRecibido.ToString(), "Pagos" },
            { TipoNotificacion.PagoVencido.ToString(), "Pagos" },
            { TipoNotificacion.ComprobanteEmitido.ToString(), "Comprobantes" },
            { TipoNotificacion.LiquidacionRegistrada.ToString(), "Cuenta corriente" }
        };

        // Página interna: "~/Pagina.aspx" o "~/Carpeta/Pagina.aspx", con
        // parámetros opcionales. Nada de esquemas, "//", ".." ni "\".
        private static readonly Regex FormatoUrlInterna = new Regex(
            @"^~/([A-Za-z0-9_\-]+/)*[A-Za-z0-9_\-]+\.aspx(\?[A-Za-z0-9_\-=&%\.]*)?$", RegexOptions.Compiled);

        public static string TituloPorTipo(string tipo)
        {
            string titulo;
            return tipo != null && TitulosPorTipo.TryGetValue(tipo, out titulo) ? titulo : "Aviso de StageUp";
        }

        public static string CategoriaPorTipo(string tipo)
        {
            string categoria;
            return tipo != null && CategoriasPorTipo.TryGetValue(tipo, out categoria) ? categoria : "General";
        }

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
                    Titulo = TituloPorTipo(tipo.ToString()),
                    Mensaje = mensaje != null && mensaje.Length > 300 ? mensaje.Substring(0, 297) + "..." : mensaje,
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
            return Listar(idUsuarioExterno, FiltroTodas, cantidad);
        }

        // Centro de notificaciones (pasos 2 a 4, A1 a A3).
        public List<Notificacion> Listar(int idUsuarioExterno, string estado, int cantidad)
        {
            try
            {
                string filtro = estado == FiltroNoLeidas || estado == FiltroLeidas ? estado : FiltroTodas;
                List<Notificacion> notificaciones = _mppNotificacion.ListarPorUsuario(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno },
                    cantidad <= 0 || cantidad > CantidadCentro ? CantidadPorDefecto : cantidad,
                    filtro);
                foreach (Notificacion notificacion in notificaciones)
                {
                    if (string.IsNullOrWhiteSpace(notificacion.Titulo))
                    {
                        notificacion.Titulo = TituloPorTipo(notificacion.Tipo);
                    }
                }

                return notificaciones;
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

        // Ítem 36: la pertenencia se valida en el WHERE del UPDATE: si la
        // notificación no es del usuario no se actualiza nada.
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

        // Solo las del usuario actual.
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

        // Pasos 7 a 9 y A4 a A9: identifica el elemento de la notificación,
        // la marca como leída (aunque el elemento ya no esté disponible) y
        // devuelve la página a la que hay que ir. Si el elemento ya no se
        // puede consultar, devuelve el motivo y no la página.
        public ResultadoOperacion<string> ResolverDestino(int idNotificacion, int idUsuarioExterno)
        {
            Notificacion notificacion;
            try
            {
                notificacion = _mppNotificacion.ObtenerPorId(new Notificacion
                {
                    IdNotificacion = idNotificacion,
                    IdUsuarioExterno = idUsuarioExterno
                });
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<string>.Error(ex.Message);
            }

            if (notificacion == null)
            {
                return ResultadoOperacion<string>.Error("La notificación no existe o no es tuya.", "A9");
            }

            MarcarLeida(idNotificacion, idUsuarioExterno);

            string url = (notificacion.UrlDestino ?? string.Empty).Trim();
            if (url.Length == 0)
            {
                return ResultadoOperacion<string>.Error("Esta notificación no tiene un elemento relacionado para consultar.", "A9");
            }

            if (!EsUrlInterna(url))
            {
                return ResultadoOperacion<string>.Error(
                    "No es posible acceder al detalle relacionado: el enlace de la notificación no es válido.", "A9");
            }

            ResultadoOperacion disponible = VerificarElemento(url, idUsuarioExterno);
            if (!disponible.Exitoso)
            {
                return ResultadoOperacion<string>.Error(
                    "No es posible acceder al detalle relacionado: " + disponible.Mensaje, "A9");
            }

            return ResultadoOperacion<string>.Ok(url);
        }

        public static bool EsUrlInterna(string url)
        {
            return !string.IsNullOrEmpty(url) && url.Length <= 300 && FormatoUrlInterna.IsMatch(url) && url.IndexOf("..", StringComparison.Ordinal) < 0;
        }

        // A4 a A7: el elemento sigue existiendo y el usuario lo puede ver.
        private static ResultadoOperacion VerificarElemento(string url, int idUsuarioExterno)
        {
            string pagina = url;
            string consulta = string.Empty;
            int signo = url.IndexOf('?');
            if (signo >= 0)
            {
                pagina = url.Substring(0, signo);
                consulta = url.Substring(signo + 1);
            }

            try
            {
                int id;
                if (Igual(pagina, "~/DetalleReserva.aspx") && LeerParametro(consulta, "id", out id))
                {
                    ResultadoOperacion<Reserva> reserva = new BLL_Reserva().ObtenerDetalle(id, idUsuarioExterno);
                    return reserva.Exitoso ? ResultadoOperacion.Ok() : ResultadoOperacion.Error("la reserva ya no está disponible para tu cuenta.");
                }

                if (Igual(pagina, "~/Soporte.aspx") && LeerParametro(consulta, "ver", out id))
                {
                    ResultadoOperacion<TicketCompleto> ticket = new BLL_Ticket().ObtenerDetalleParaUsuario(id, idUsuarioExterno);
                    return ticket.Exitoso ? ResultadoOperacion.Ok() : ResultadoOperacion.Error("la solicitud de soporte ya no está disponible.");
                }

                if (Igual(pagina, "~/Explorar/DetalleEspacio.aspx") && LeerParametro(consulta, "id", out id))
                {
                    BLL_EspacioArtistico bllEspacio = new BLL_EspacioArtistico();
                    bool visible = bllEspacio.ObtenerDetallePublicado(id) != null ||
                        bllEspacio.ObtenerDetalleParaGestor(id, idUsuarioExterno) != null;
                    return visible ? ResultadoOperacion.Ok() : ResultadoOperacion.Error("el espacio ya no está publicado o fue dado de baja.");
                }

                if (Igual(pagina, "~/MisActividades.aspx") && LeerParametro(consulta, "ver", out id))
                {
                    return new BLL_Actividad().ObtenerDetalle(id, idUsuarioExterno) != null
                        ? ResultadoOperacion.Ok()
                        : ResultadoOperacion.Error("la actividad interna ya no está disponible.");
                }
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }

            return ResultadoOperacion.Ok();
        }

        private static bool Igual(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool LeerParametro(string consulta, string nombre, out int valor)
        {
            valor = 0;
            foreach (string parte in (consulta ?? string.Empty).Split('&'))
            {
                int igual = parte.IndexOf('=');
                if (igual > 0 && Igual(parte.Substring(0, igual), nombre))
                {
                    return int.TryParse(parte.Substring(igual + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out valor) && valor > 0;
                }
            }

            return false;
        }
    }
}
