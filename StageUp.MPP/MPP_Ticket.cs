using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Módulo de soporte/helpdesk (ítems 6C y 16 de la segunda entrega).
    // Mismo patrón que MPP_Faq.cs / MPP_Encuesta.cs (Conexion.Instance +
    // Hashtable de parámetros). Todos los métodos públicos reciben objetos
    // (Ticket, TicketMensaje, UsuarioExterno), no ids sueltos.
    public class MPP_Ticket
    {
        public int Crear(Ticket oTicket, TicketMensaje oMensajeInicial)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Ticket_Crear",
                new Hashtable
                {
                    { "@idUsuarioExterno", oTicket.IdUsuarioExterno },
                    { "@idReservaAsociada", (object)oTicket.IdReservaAsociada ?? DBNull.Value },
                    { "@categoria", oTicket.Categoria },
                    { "@asunto", oTicket.Asunto },
                    { "@mensajeInicial", oMensajeInicial.Mensaje }
                });

            return Convert.ToInt32(resultado);
        }

        public int InsertarMensaje(TicketMensaje oMensaje)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_TicketMensaje_Insertar",
                new Hashtable
                {
                    { "@idTicket", oMensaje.IdTicket },
                    { "@idUsuarioExterno", (object)oMensaje.IdUsuarioExterno ?? DBNull.Value },
                    { "@idUsuarioInterno", (object)oMensaje.IdUsuarioInterno ?? DBNull.Value },
                    { "@mensaje", oMensaje.Mensaje }
                });

            return Convert.ToInt32(resultado);
        }

        public void CambiarEstado(Ticket oTicket)
        {
            Conexion.Instance.Guardar(
                "sp_Ticket_CambiarEstado",
                new Hashtable
                {
                    { "@idTicket", oTicket.IdTicket },
                    { "@estado", oTicket.Estado }
                });
        }

        public void Asignar(Ticket oTicket)
        {
            Conexion.Instance.Guardar(
                "sp_Ticket_Asignar",
                new Hashtable
                {
                    { "@idTicket", oTicket.IdTicket },
                    { "@idUsuarioInternoAsignado", oTicket.IdUsuarioInternoAsignado.Value }
                });
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

        // El filtro viaja como un Ticket "de ejemplo": Estado y Categoria en
        // null significan "todos".
        public List<Ticket> ListarParaInterno(Ticket oFiltro)
        {
            return MapearTickets(Conexion.Instance.Leer(
                "sp_Ticket_ListarParaInterno",
                new Hashtable
                {
                    { "@estado", (object)oFiltro.Estado ?? DBNull.Value },
                    { "@categoria", (object)oFiltro.Categoria ?? DBNull.Value }
                }));
        }

        public int ContarAbiertos()
        {
            object resultado = Conexion.Instance.LeerEscalar("sp_Ticket_ContarAbiertos");
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
                IdReservaAsociada = fila["idReservaAsociada"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idReservaAsociada"]),
                Categoria = fila["categoria"].ToString(),
                Asunto = fila["asunto"].ToString(),
                Estado = fila["estado"].ToString(),
                IdUsuarioInternoAsignado = fila["idUsuarioInternoAsignado"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idUsuarioInternoAsignado"]),
                FechaCreacion = Convert.ToDateTime(fila["fechaCreacion"]),
                FechaUltimaActividad = Convert.ToDateTime(fila["fechaUltimaActividad"]),
                FechaCierre = fila["fechaCierre"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaCierre"]),
                NombreUsuarioExterno = fila["nombreUsuarioExterno"].ToString(),
                CorreoUsuarioExterno = fila["correoUsuarioExterno"].ToString(),
                NombreUsuarioInternoAsignado = fila["nombreUsuarioInternoAsignado"] == DBNull.Value ? null : fila["nombreUsuarioInternoAsignado"].ToString(),
                NombreEspacioReserva = LeerTexto(fila, "nombreEspacioReserva"),
                FechaReserva = LeerFecha(fila, "fechaReserva"),
                MinutoDesdeReserva = LeerEntero(fila, "minutoDesdeReserva"),
                MinutoHastaReserva = LeerEntero(fila, "minutoHastaReserva"),
                EstadoReserva = LeerTexto(fila, "estadoReserva")
            };
        }

        // Las columnas de la reserva asociada las agrega el script 40; se leen
        // de forma tolerante para no romper si la columna no viene.
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
