using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Solicitudes de soporte (CU-003-002, CU-003-003 e ítem 12). Mismo patrón
    // que el resto de los MPP: Conexion.Instance + Hashtable de parámetros, y
    // los métodos públicos reciben objetos (Ticket, TicketMensaje, filtro).
    public class MPP_Ticket
    {
        public int Registrar(Ticket oTicket)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Ticket_Registrar",
                new Hashtable
                {
                    { "@idUsuarioExterno", oTicket.IdUsuarioExterno },
                    { "@idReservaAsociada", (object)oTicket.IdReservaAsociada ?? DBNull.Value },
                    { "@funcionalidad", oTicket.FuncionalidadVinculada },
                    { "@motivo", oTicket.Motivo },
                    { "@descripcion", oTicket.Descripcion },
                    { "@informacionAdicional", (object)oTicket.InformacionAdicional ?? DBNull.Value },
                    { "@origen", oTicket.Origen }
                });

            return Convert.ToInt32(resultado);
        }

        public Ticket ObtenerPorId(Ticket oTicket)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Ticket_ObtenerPorId",
                new Hashtable { { "@idTicket", oTicket.IdTicket } });

            return tabla.Rows.Count == 0 ? null : MapearFilaTicket(tabla.Rows[0]);
        }

        public List<Ticket> ListarPorUsuario(UsuarioExterno oUsuario)
        {
            return MapearTickets(Conexion.Instance.Leer(
                "sp_Ticket_ListarPorUsuario",
                new Hashtable { { "@idUsuarioExterno", oUsuario.IdUsuarioExterno } }));
        }

        public List<Ticket> ListarParaInterno(FiltroTicket oFiltro)
        {
            Hashtable parametros = ParametrosFiltro(oFiltro);
            parametros.Add("@pagina", oFiltro.Pagina);
            parametros.Add("@tamanioPagina", oFiltro.TamanioPagina);
            return MapearTickets(Conexion.Instance.Leer("sp_Ticket_ListarParaInterno", parametros));
        }

        public int ContarParaInterno(FiltroTicket oFiltro)
        {
            object resultado = Conexion.Instance.LeerEscalar("sp_Ticket_ContarParaInterno", ParametrosFiltro(oFiltro));
            return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
        }

        public ResumenSoporte ObtenerResumen()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Ticket_ResumenInterno", new Hashtable());
            if (tabla.Rows.Count == 0)
            {
                return new ResumenSoporte();
            }

            DataRow fila = tabla.Rows[0];
            return new ResumenSoporte
            {
                Total = Convert.ToInt32(fila["total"]),
                Pendientes = Convert.ToInt32(fila["pendientes"]),
                EnProceso = Convert.ToInt32(fila["enProceso"]),
                Resueltas = Convert.ToInt32(fila["resueltas"]),
                Cerradas = Convert.ToInt32(fila["cerradas"]),
                SinLeer = Convert.ToInt32(fila["sinLeer"]),
                SinAsignar = Convert.ToInt32(fila["sinAsignar"])
            };
        }

        // Devuelve el estado en que quedó la solicitud.
        public string RegistrarRespuesta(TicketMensaje oMensaje, string estadoAnterior, bool marcarResuelta)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Ticket_RegistrarRespuesta",
                new Hashtable
                {
                    { "@idTicket", oMensaje.IdTicket },
                    { "@idUsuarioInterno", oMensaje.IdUsuarioInterno.Value },
                    { "@mensaje", oMensaje.Mensaje },
                    { "@estadoAnterior", estadoAnterior },
                    { "@marcarResuelta", marcarResuelta }
                });

            return Convert.ToString(resultado);
        }

        public void CambiarEstado(TicketActualizacion oActualizacion)
        {
            Conexion.Instance.Guardar(
                "sp_Ticket_CambiarEstado",
                new Hashtable
                {
                    { "@idTicket", oActualizacion.IdTicket },
                    { "@estadoAnterior", oActualizacion.EstadoAnterior },
                    { "@estadoNuevo", oActualizacion.EstadoNuevo },
                    { "@idUsuarioInterno", (object)oActualizacion.IdUsuarioInterno ?? DBNull.Value },
                    { "@idUsuarioExterno", (object)oActualizacion.IdUsuarioExterno ?? DBNull.Value },
                    { "@detalle", (object)oActualizacion.Detalle ?? DBNull.Value }
                });
        }

        // Devuelve el estado en que quedó la solicitud.
        public string Asignar(Ticket oTicket, int idUsuarioInternoResponsable, bool tomar)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Ticket_Asignar",
                new Hashtable
                {
                    { "@idTicket", oTicket.IdTicket },
                    { "@idUsuarioInternoAsignado", (object)oTicket.IdUsuarioInternoAsignado ?? DBNull.Value },
                    { "@idUsuarioInternoResponsable", idUsuarioInternoResponsable },
                    { "@tomar", tomar }
                });

            return Convert.ToString(resultado);
        }

        // Devuelve el estado en que quedó la solicitud.
        public string InsertarMensajeUsuario(TicketMensaje oMensaje)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_TicketMensaje_InsertarUsuario",
                new Hashtable
                {
                    { "@idTicket", oMensaje.IdTicket },
                    { "@idUsuarioExterno", oMensaje.IdUsuarioExterno.Value },
                    { "@mensaje", oMensaje.Mensaje }
                });

            return Convert.ToString(resultado);
        }

        public void MarcarLeido(Ticket oTicket, string lado)
        {
            Conexion.Instance.LeerEscalar(
                "sp_Ticket_MarcarLeido",
                new Hashtable
                {
                    { "@idTicket", oTicket.IdTicket },
                    { "@lado", lado }
                });
        }

        // Ítem 12 (actualización periódica): estado y última actividad.
        public Ticket ObtenerMarca(Ticket oTicket)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Ticket_ObtenerMarca",
                new Hashtable { { "@idTicket", oTicket.IdTicket } });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            return new Ticket
            {
                IdTicket = oTicket.IdTicket,
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                Estado = fila["estado"].ToString(),
                FechaUltimaActividad = Convert.ToDateTime(fila["fechaUltimaActividad"])
            };
        }

        // Cantidad de solicitudes y última actividad (todas, o las de un usuario).
        public KeyValuePair<int, DateTime?> ObtenerMarcaListado(UsuarioExterno oUsuario)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Ticket_ObtenerMarcaListado",
                new Hashtable { { "@idUsuarioExterno", oUsuario == null ? (object)DBNull.Value : oUsuario.IdUsuarioExterno } });

            if (tabla.Rows.Count == 0)
            {
                return new KeyValuePair<int, DateTime?>(0, null);
            }

            DataRow fila = tabla.Rows[0];
            return new KeyValuePair<int, DateTime?>(
                Convert.ToInt32(fila["cantidad"]),
                fila["ultimaActividad"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["ultimaActividad"]));
        }

        public int ContarAbiertos()
        {
            object resultado = Conexion.Instance.LeerEscalar("sp_Ticket_ContarAbiertos");
            return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
        }

        public int ContarNovedadesUsuario(UsuarioExterno oUsuario)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Ticket_ContarNovedadesUsuario",
                new Hashtable { { "@idUsuarioExterno", oUsuario.IdUsuarioExterno } });
            return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
        }

        public List<TicketMensaje> ListarMensajesPorTicket(Ticket oTicket)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TicketMensaje_ListarPorTicket",
                new Hashtable { { "@idTicket", oTicket.IdTicket } });

            List<TicketMensaje> lista = new List<TicketMensaje>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new TicketMensaje
                {
                    IdTicketMensaje = Convert.ToInt32(fila["idTicketMensaje"]),
                    IdTicket = Convert.ToInt32(fila["idTicket"]),
                    IdUsuarioExterno = fila["idUsuarioExterno"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idUsuarioExterno"]),
                    IdUsuarioInterno = fila["idUsuarioInterno"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idUsuarioInterno"]),
                    Mensaje = fila["mensaje"].ToString(),
                    FechaEnvio = Convert.ToDateTime(fila["fechaEnvio"]),
                    NombreAutor = fila["nombreAutor"].ToString(),
                    EsInterno = Convert.ToBoolean(fila["esInterno"])
                });
            }
            return lista;
        }

        public List<TicketActualizacion> ListarActualizacionesPorTicket(Ticket oTicket)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TicketActualizacion_ListarPorTicket",
                new Hashtable { { "@idTicket", oTicket.IdTicket } });

            List<TicketActualizacion> lista = new List<TicketActualizacion>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new TicketActualizacion
                {
                    IdTicketActualizacion = Convert.ToInt32(fila["idTicketActualizacion"]),
                    IdTicket = Convert.ToInt32(fila["idTicket"]),
                    TipoActualizacion = fila["tipoActualizacion"].ToString(),
                    EstadoAnterior = LeerTexto(fila, "estadoAnterior"),
                    EstadoNuevo = LeerTexto(fila, "estadoNuevo"),
                    IdUsuarioInterno = LeerEntero(fila, "idUsuarioInterno"),
                    IdUsuarioExterno = LeerEntero(fila, "idUsuarioExterno"),
                    Detalle = LeerTexto(fila, "detalle"),
                    Fecha = Convert.ToDateTime(fila["fecha"]),
                    NombreResponsable = LeerTexto(fila, "nombreResponsable"),
                    EsInterno = Convert.ToBoolean(fila["esInterno"])
                });
            }
            return lista;
        }

        private static Hashtable ParametrosFiltro(FiltroTicket oFiltro)
        {
            return new Hashtable
            {
                { "@estado", (object)oFiltro.Estado ?? DBNull.Value },
                { "@funcionalidad", (object)oFiltro.FuncionalidadVinculada ?? DBNull.Value },
                { "@desde", oFiltro.Desde.HasValue ? (object)oFiltro.Desde.Value.Date : DBNull.Value },
                { "@hasta", oFiltro.Hasta.HasValue ? (object)oFiltro.Hasta.Value.Date : DBNull.Value },
                { "@usuario", (object)oFiltro.Usuario ?? DBNull.Value },
                { "@idTicket", (object)oFiltro.IdTicket ?? DBNull.Value },
                { "@asignacion", (object)oFiltro.Asignacion ?? DBNull.Value },
                { "@idUsuarioInternoAsignado", (object)oFiltro.IdUsuarioInternoAsignado ?? DBNull.Value },
                { "@soloNoLeidas", oFiltro.SoloNoLeidas }
            };
        }

        private static List<Ticket> MapearTickets(DataTable tabla)
        {
            List<Ticket> lista = new List<Ticket>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearFilaTicket(fila));
            }
            return lista;
        }

        private static Ticket MapearFilaTicket(DataRow fila)
        {
            return new Ticket
            {
                IdTicket = Convert.ToInt32(fila["idTicket"]),
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                IdReservaAsociada = LeerEntero(fila, "idReservaAsociada"),
                FuncionalidadVinculada = fila["funcionalidadVinculada"].ToString(),
                Motivo = fila["motivo"].ToString(),
                Descripcion = LeerTexto(fila, "descripcion"),
                InformacionAdicional = LeerTexto(fila, "informacionAdicional"),
                Origen = LeerTexto(fila, "origen"),
                Estado = fila["estado"].ToString(),
                IdUsuarioInternoAsignado = LeerEntero(fila, "idUsuarioInternoAsignado"),
                FechaCreacion = Convert.ToDateTime(fila["fechaCreacion"]),
                FechaUltimaActividad = Convert.ToDateTime(fila["fechaUltimaActividad"]),
                FechaResolucion = LeerFecha(fila, "fechaResolucion"),
                FechaCierre = LeerFecha(fila, "fechaCierre"),
                FechaUltimaLecturaUsuario = LeerFecha(fila, "fechaUltimaLecturaUsuario"),
                FechaUltimaLecturaSoporte = LeerFecha(fila, "fechaUltimaLecturaSoporte"),
                NombreUsuarioExterno = fila["nombreUsuarioExterno"].ToString(),
                CorreoUsuarioExterno = fila["correoUsuarioExterno"].ToString(),
                NombreUsuarioInternoAsignado = LeerTexto(fila, "nombreUsuarioInternoAsignado"),
                CantidadRespuestas = LeerEntero(fila, "cantidadRespuestas") ?? 0,
                NovedadesUsuario = LeerEntero(fila, "novedadesUsuario") ?? 0,
                NovedadesSoporte = LeerEntero(fila, "novedadesSoporte") ?? 0,
                NombreEspacioReserva = LeerTexto(fila, "nombreEspacioReserva"),
                FechaReserva = LeerFecha(fila, "fechaReserva"),
                MinutoDesdeReserva = LeerEntero(fila, "minutoDesdeReserva"),
                MinutoHastaReserva = LeerEntero(fila, "minutoHastaReserva"),
                EstadoReserva = LeerTexto(fila, "estadoReserva")
            };
        }

        private static string LeerTexto(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value
                ? fila[columna].ToString()
                : null;
        }

        private static DateTime? LeerFecha(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value
                ? Convert.ToDateTime(fila[columna])
                : (DateTime?)null;
        }

        private static int? LeerEntero(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value
                ? Convert.ToInt32(fila[columna])
                : (int?)null;
        }
    }
}
