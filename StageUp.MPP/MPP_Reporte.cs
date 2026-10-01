using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Reportes con gráficos (ítem 15, script 48). Todos los métodos reciben
    // el FiltroReporte, no parámetros sueltos.
    public class MPP_Reporte
    {
        public IndicadoresTablero ObtenerIndicadores(FiltroReporte oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Reporte_Indicadores",
                new Hashtable
                {
                    { "@desde", oFiltro.Desde.Date },
                    { "@hasta", oFiltro.Hasta.Date },
                    { "@moneda", oFiltro.Moneda }
                });

            if (tabla.Rows.Count == 0)
            {
                return new IndicadoresTablero();
            }

            DataRow fila = tabla.Rows[0];
            return new IndicadoresTablero
            {
                UsuariosActivos = LeerEntero(fila, "usuariosActivos"),
                UsuariosNuevos = LeerEntero(fila, "usuariosNuevos"),
                EspaciosPublicados = LeerEntero(fila, "espaciosPublicados"),
                ReservasSolicitadas = LeerEntero(fila, "reservasSolicitadas"),
                ReservasConfirmadas = LeerEntero(fila, "reservasConfirmadas"),
                ImporteReservas = LeerDecimal(fila, "importeReservas"),
                ImporteComisiones = LeerDecimal(fila, "importeComisiones"),
                PromedioCalificacion = fila["promedioCalificacion"] == DBNull.Value
                    ? (decimal?)null
                    : Convert.ToDecimal(fila["promedioCalificacion"]),
                TicketsPendientes = LeerEntero(fila, "ticketsPendientes")
            };
        }

        public List<FilaReportePeriodo> ListarIngresos(FiltroReporte oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Reporte_Ingresos",
                new Hashtable
                {
                    { "@desde", oFiltro.Desde.Date },
                    { "@hasta", oFiltro.Hasta.Date },
                    { "@agrupacion", oFiltro.Agrupacion },
                    { "@moneda", oFiltro.Moneda },
                    { "@provincia", oFiltro.Provincia },
                    { "@tipoEspacio", oFiltro.TipoEspacio }
                });

            List<FilaReportePeriodo> lista = new List<FilaReportePeriodo>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new FilaReportePeriodo
                {
                    InicioPeriodo = Convert.ToDateTime(fila["inicioPeriodo"]),
                    CantidadReservas = LeerEntero(fila, "cantidadReservas"),
                    ImporteReservas = LeerDecimal(fila, "importeReservas"),
                    ImporteComisiones = LeerDecimal(fila, "importeComisiones")
                });
            }
            return lista;
        }

        public List<FilaReporteZona> ListarIngresosPorZona(FiltroReporte oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Reporte_IngresosPorZona",
                new Hashtable
                {
                    { "@desde", oFiltro.Desde.Date },
                    { "@hasta", oFiltro.Hasta.Date },
                    { "@moneda", oFiltro.Moneda },
                    { "@tipoEspacio", oFiltro.TipoEspacio }
                });

            List<FilaReporteZona> lista = new List<FilaReporteZona>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new FilaReporteZona
                {
                    Provincia = fila["provincia"].ToString(),
                    Ciudad = fila["ciudad"].ToString(),
                    CantidadReservas = LeerEntero(fila, "cantidadReservas"),
                    ImporteReservas = LeerDecimal(fila, "importeReservas")
                });
            }
            return lista;
        }

        public List<FilaReporteEstado> ListarReservasPorEstado(FiltroReporte oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Reporte_ReservasPorEstado",
                new Hashtable
                {
                    { "@desde", oFiltro.Desde.Date },
                    { "@hasta", oFiltro.Hasta.Date },
                    { "@provincia", oFiltro.Provincia },
                    { "@tipoEspacio", oFiltro.TipoEspacio }
                });

            List<FilaReporteEstado> lista = new List<FilaReporteEstado>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new FilaReporteEstado
                {
                    Estado = fila["estado"].ToString(),
                    Cantidad = LeerEntero(fila, "cantidad")
                });
            }
            return lista;
        }

        public List<FilaParticipacionEncuesta> ListarParticipacionEncuestas()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Reporte_ParticipacionEncuestas");

            List<FilaParticipacionEncuesta> lista = new List<FilaParticipacionEncuesta>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new FilaParticipacionEncuesta
                {
                    IdEncuesta = Convert.ToInt32(fila["idEncuesta"]),
                    Titulo = fila["titulo"].ToString(),
                    Estado = fila["estado"].ToString(),
                    PublicoObjetivo = fila["publicoObjetivo"].ToString(),
                    FechaVencimiento = Convert.ToDateTime(fila["fechaVencimiento"]),
                    CantidadRespuestas = LeerEntero(fila, "cantidadRespuestas"),
                    CantidadDestinatarios = LeerEntero(fila, "cantidadDestinatarios")
                });
            }
            return lista;
        }

        // Valores posibles de cada filtro: clave "Provincia" o "TipoEspacio".
        public Dictionary<string, List<string>> ListarValoresFiltros()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Reporte_ListarFiltros");

            Dictionary<string, List<string>> valores = new Dictionary<string, List<string>>
            {
                { "Provincia", new List<string>() },
                { "TipoEspacio", new List<string>() }
            };

            foreach (DataRow fila in tabla.Rows)
            {
                string filtro = fila["filtro"].ToString();
                if (valores.ContainsKey(filtro) && fila["valor"] != DBNull.Value)
                {
                    valores[filtro].Add(fila["valor"].ToString());
                }
            }
            return valores;
        }

        private static int LeerEntero(DataRow fila, string columna)
        {
            return fila[columna] == DBNull.Value ? 0 : Convert.ToInt32(fila[columna]);
        }

        private static decimal LeerDecimal(DataRow fila, string columna)
        {
            return fila[columna] == DBNull.Value ? 0m : Convert.ToDecimal(fila[columna]);
        }
    }
}
