using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Búsqueda global (ítems 4 y 13, script 47).
    public class MPP_Busqueda
    {
        public List<ResultadoBusqueda> BuscarPublico(FiltroBusquedaGlobal oFiltro)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_Busqueda_Publica",
                new Hashtable
                {
                    { "@texto", oFiltro.Texto },
                    { "@incluirEspacios", oFiltro.Incluye("Espacio") },
                    { "@incluirNovedades", oFiltro.Incluye("Novedad") },
                    { "@incluirFaq", oFiltro.Incluye("FAQ") }
                }));
        }

        public List<ResultadoBusqueda> BuscarInterno(FiltroBusquedaGlobal oFiltro)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_Busqueda_Interna",
                new Hashtable
                {
                    { "@texto", oFiltro.Texto },
                    { "@incluirUsuariosExternos", oFiltro.Incluye("UsuarioExterno") },
                    { "@incluirUsuariosInternos", oFiltro.Incluye("UsuarioInterno") },
                    { "@incluirEspacios", oFiltro.Incluye("Espacio") },
                    { "@incluirReservas", oFiltro.Incluye("Reserva") },
                    { "@incluirTickets", oFiltro.Incluye("Ticket") },
                    { "@incluirNovedades", oFiltro.Incluye("Novedad") },
                    { "@incluirFaq", oFiltro.Incluye("FAQ") },
                    { "@incluirEncuestas", oFiltro.Incluye("Encuesta") }
                }));
        }

        private static List<ResultadoBusqueda> Mapear(DataTable tabla)
        {
            List<ResultadoBusqueda> lista = new List<ResultadoBusqueda>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new ResultadoBusqueda
                {
                    Tipo = fila["tipo"].ToString(),
                    IdReferencia = Convert.ToInt32(fila["idReferencia"]),
                    Titulo = fila["titulo"] == DBNull.Value ? string.Empty : fila["titulo"].ToString(),
                    Detalle = fila["detalle"] == DBNull.Value ? null : fila["detalle"].ToString(),
                    Fecha = fila["fecha"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fecha"])
                });
            }
            return lista;
        }
    }
}
