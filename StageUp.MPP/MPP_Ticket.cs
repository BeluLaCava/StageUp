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
    // Hashtable de parámetros).
    public class MPP_Ticket
    {
        public int Crear(Ticket oTicket, string mensajeInicial)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Ticket_Crear",
                new Hashtable
                {
                    { "@idUsuarioExterno", oTicket.IdUsuarioExterno },
                    { "@idReservaAsociada", (object)oTicket.IdReservaAsociada ?? DBNull.Value },
                    { "@categoria", oTicket.Categoria },
                    { "@asunto", oTicket.Asunto },
                    { "@mensajeInicial", mensajeInicial }
                });

            return Convert.ToInt32(resultado);
        }

        public int InsertarMensaje(int idTicket, int? idUsuarioExterno, int? idUsuarioInterno, string mensaje)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_TicketMensaje_Insertar",
                new Hashtable
                {
                    { "@idTicket", idTicket },
                    { "@idUsuarioExterno", (object)idUsuarioExterno ?? DBNull.Value },
                    { "@idUsuarioInterno", (object)idUsuarioInterno ?? DBNull.Value },
                    { "@mensaje", mensaje }
                });

            return Convert.ToInt32(resultado);
        }

        public void CambiarEstado(int idTicket, string estado)
        {
            Conexion.Instance.Guardar(
                "sp_Ticket_CambiarEstado",
                new Hashtable
                {
                    { "@idTicket", idTicket },
                    { "@estado", estado }
                });
        }

        public void Asignar(int idTicket, int idUsuarioInternoAsignado)
        {
            Conexion.Instance.Guardar(
                "sp_Ticket_Asignar",
                new Hashtable
                {
                    { "@idTicket", idTicket },
                    { "@idUsuarioInternoAsignado", idUsuarioInternoAsignado }
                });
        }

        public Ticket ObtenerPorId(int idTicket)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Ticket_ObtenerPorId",
                new Hashtable { { "@idTicket", idTicket } });

            return tabla.Rows.Count == 0 ? null : MapearFilaTicket(tabla.Rows[0]);
        }

        public List<Ticket> ListarPorUsuario(int idUsuarioExterno)
        {
            return MapearTickets(Conexion.Instance.Leer(
                "sp_Ticket_ListarPorUsuario",
                new Hashtable { { "@idUsuarioExterno", idUsuarioExterno } }));
        }

        public List<Ticket> ListarParaInterno(string estado, string categoria)
        {
            return MapearTickets(Conexion.Instance.Leer(
                "sp_Ticket_ListarParaInterno",
                new Hashtable
                {
                    { "@estado", (object)estado ?? DBNull.Value },
                    { "@categoria", (object)categoria ?? DBNull.Value }
                }));
        }

        public List<TicketMensaje> ListarMensajesPorTicket(int idTicket)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TicketMensaje_ListarPorTicket",
                new Hashtable { { "@idTicket", idTicket } });

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
                NombreUsuarioInternoAsignado = fila["nombreUsuarioInternoAsignado"] == DBNull.Value ? null : fila["nombreUsuarioInternoAsignado"].ToString()
            };
        }
    }
}
