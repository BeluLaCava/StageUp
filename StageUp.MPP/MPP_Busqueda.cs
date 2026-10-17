using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Búsqueda global (ítems 4 y 13, script 47) y avanzada (script 76): el
    // filtro completo viaja como parámetros del SP.
    public class MPP_Busqueda
    {
        public List<ResultadoBusqueda> BuscarPublico(FiltroBusquedaGlobal oFiltro)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_Busqueda_Publica",
                new Hashtable
                {
                    { "@texto", Valor(oFiltro.Texto) },
                    { "@incluirEspacios", oFiltro.Incluye("Espacio") },
                    { "@incluirNovedades", oFiltro.Incluye("Novedad") },
                    { "@incluirFaq", oFiltro.Incluye("FAQ") },
                    { "@fechaDesde", Valor(oFiltro.FechaDesde) },
                    { "@fechaHasta", Valor(oFiltro.FechaHasta) },
                    { "@ubicacion", Valor(oFiltro.Ubicacion) },
                    { "@capacidadMinima", oFiltro.CapacidadMinima.HasValue ? (object)oFiltro.CapacidadMinima.Value : DBNull.Value },
                    { "@precioMaximo", oFiltro.PrecioMaximo.HasValue ? (object)oFiltro.PrecioMaximo.Value : DBNull.Value },
                    { "@categoriaNovedad", Valor(oFiltro.CategoriaNovedad) },
                    { "@orden", oFiltro.Orden ?? FiltroBusquedaGlobal.OrdenRelevancia },
                    { "@maximo", oFiltro.Maximo }
                }));
        }

        public List<ResultadoBusqueda> BuscarInterno(FiltroBusquedaGlobal oFiltro)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_Busqueda_Interna",
                new Hashtable
                {
                    { "@texto", Valor(oFiltro.Texto) },
                    { "@incluirUsuariosExternos", oFiltro.Incluye("UsuarioExterno") },
                    { "@incluirUsuariosInternos", oFiltro.Incluye("UsuarioInterno") },
                    { "@incluirEspacios", oFiltro.Incluye("Espacio") },
                    { "@incluirReservas", oFiltro.Incluye("Reserva") },
                    { "@incluirTickets", oFiltro.Incluye("Ticket") },
                    { "@incluirNovedades", oFiltro.Incluye("Novedad") },
                    { "@incluirFaq", oFiltro.Incluye("FAQ") },
                    { "@incluirEncuestas", oFiltro.Incluye("Encuesta") },
                    { "@fechaDesde", Valor(oFiltro.FechaDesde) },
                    { "@fechaHasta", Valor(oFiltro.FechaHasta) },
                    { "@estado", Valor(oFiltro.Estado) },
                    { "@ubicacion", Valor(oFiltro.Ubicacion) },
                    { "@categoriaNovedad", Valor(oFiltro.CategoriaNovedad) },
                    { "@orden", oFiltro.Orden ?? FiltroBusquedaGlobal.OrdenRelevancia },
                    { "@maximo", oFiltro.Maximo }
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
                    Fecha = fila["fecha"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fecha"]),
                    Posicion = tabla.Columns.Contains("posicion") && fila["posicion"] != DBNull.Value ? Convert.ToInt32(fila["posicion"]) : 0
                });
            }
            return lista;
        }

        private static object Valor(string valor)
        {
            return string.IsNullOrEmpty(valor) ? (object)DBNull.Value : valor;
        }

        private static object Valor(DateTime? valor)
        {
            return valor.HasValue ? (object)valor.Value : DBNull.Value;
        }
    }
}
