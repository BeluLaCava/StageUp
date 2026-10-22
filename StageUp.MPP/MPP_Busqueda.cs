using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{


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
                    { "@tipoEspacio", Valor(oFiltro.TipoEspacio) },
                    { "@provincia", Valor(oFiltro.Provincia) },
                    { "@ciudad", Valor(oFiltro.Ciudad) },
                    { "@zona", Valor(oFiltro.Zona) },
                    { "@precioMinimo", oFiltro.PrecioMinimo.HasValue ? (object)oFiltro.PrecioMinimo.Value : DBNull.Value },
                    { "@capacidadMinima", oFiltro.CapacidadMinima.HasValue ? (object)oFiltro.CapacidadMinima.Value : DBNull.Value },
                    { "@precioMaximo", oFiltro.PrecioMaximo.HasValue ? (object)oFiltro.PrecioMaximo.Value : DBNull.Value },
                    { "@fechaDisponibilidad", Valor(oFiltro.FechaDisponibilidad) },
                    { "@minutoDesde", oFiltro.MinutoDesde.HasValue ? (object)oFiltro.MinutoDesde.Value : DBNull.Value },
                    { "@minutoHasta", oFiltro.MinutoHasta.HasValue ? (object)oFiltro.MinutoHasta.Value : DBNull.Value },
                    { "@equipamiento", Valor(oFiltro.Equipamiento == null || oFiltro.Equipamiento.Count == 0 ? null : string.Join(",", oFiltro.Equipamiento)) },
                    { "@caracteristicas", Valor(oFiltro.Caracteristicas) },
                    { "@calificacionMinima", oFiltro.CalificacionMinima.HasValue ? (object)oFiltro.CalificacionMinima.Value : DBNull.Value },
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
                    { "@incluirRegistrosActividad", oFiltro.Incluye("RegistroActividad") },
                    { "@fechaDesde", Valor(oFiltro.FechaDesde) },
                    { "@fechaHasta", Valor(oFiltro.FechaHasta) },
                    { "@estado", Valor(oFiltro.Estado) },
                    { "@ubicacion", Valor(oFiltro.Ubicacion) },
                    { "@categoriaNovedad", Valor(oFiltro.CategoriaNovedad) },
                    { "@perfil", Valor(oFiltro.Perfil) },
                    { "@rol", Valor(oFiltro.Rol) },
                    { "@responsable", Valor(oFiltro.Responsable) },
                    { "@nombre", Valor(oFiltro.Nombre) },
                    { "@correo", Valor(oFiltro.Correo) },
                    { "@operacion", Valor(oFiltro.Operacion) },
                    { "@entidadAfectada", Valor(oFiltro.EntidadAfectada) },
                    { "@idRegistro", oFiltro.IdRegistro.HasValue ? (object)oFiltro.IdRegistro.Value : DBNull.Value },
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
