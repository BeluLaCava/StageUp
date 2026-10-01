using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Reportes con gráficos y tablero de indicadores (ítem 15 de la segunda
    // entrega). Solo lo usa Interno/Reportes.aspx (permiso VER_REPORTES).
    //
    // Criterio de ingresos (script 48): importe estimado de las reservas
    // Aceptadas o Finalizadas, imputado a la fecha del alquiler, más las
    // comisiones cobradas por cancelaciones tardías, imputadas a la fecha de
    // cancelación. Cada moneda se informa por separado.
    public class BLL_Reporte
    {
        public const string AgrupacionDia = "Dia";
        public const string AgrupacionSemana = "Semana";
        public const string AgrupacionMes = "Mes";
        public const string AgrupacionAnio = "Anio";

        public static readonly string[] Monedas = { "ARS", "USD" };

        // Límites para que el gráfico siga siendo legible: si se pide más,
        // se agrupa en la unidad siguiente y se avisa.
        private const int MaximoDiasAgrupandoPorDia = 92;
        private const int MaximoDiasAgrupandoPorSemana = 730;
        private const int MaximoAniosDeRango = 10;
        private const int MaximoZonasEnGrafico = 10;

        private static readonly CultureInfo Cultura = new CultureInfo("es-AR");

        private readonly MPP_Reporte _mpp = new MPP_Reporte();

        public static Dictionary<string, string> ObtenerAgrupaciones()
        {
            return new Dictionary<string, string>
            {
                { AgrupacionDia, "Día" },
                { AgrupacionSemana, "Semana" },
                { AgrupacionMes, "Mes" },
                { AgrupacionAnio, "Año" }
            };
        }

        // Por defecto: los últimos 12 meses completos, incluido el actual,
        // agrupados por mes, en pesos.
        public static FiltroReporte CrearFiltroPorDefecto()
        {
            DateTime hoy = DateTime.Today;
            DateTime inicioMesActual = new DateTime(hoy.Year, hoy.Month, 1);
            return new FiltroReporte
            {
                Desde = inicioMesActual.AddMonths(-11),
                Hasta = inicioMesActual.AddMonths(1).AddDays(-1),
                Agrupacion = AgrupacionMes,
                Moneda = "ARS"
            };
        }

        public Dictionary<string, List<string>> ListarValoresFiltros()
        {
            try
            {
                return _mpp.ListarValoresFiltros();
            }
            catch (ErrorAccesoDatosException)
            {
                return new Dictionary<string, List<string>>
                {
                    { "Provincia", new List<string>() },
                    { "TipoEspacio", new List<string>() }
                };
            }
        }

        public ResultadoOperacion<ReporteCompleto> ObtenerReporte(FiltroReporte filtro)
        {
            if (filtro == null)
            {
                return ResultadoOperacion<ReporteCompleto>.Error("Indicá el período del reporte.");
            }

            filtro.Desde = filtro.Desde.Date;
            filtro.Hasta = filtro.Hasta.Date;
            if (filtro.Desde > filtro.Hasta)
            {
                return ResultadoOperacion<ReporteCompleto>.Error("La fecha desde no puede ser posterior a la fecha hasta.");
            }

            if (filtro.Desde.AddYears(MaximoAniosDeRango) < filtro.Hasta)
            {
                return ResultadoOperacion<ReporteCompleto>.Error(
                    "El período no puede superar los " + MaximoAniosDeRango + " años.");
            }

            if (!Monedas.Contains(filtro.Moneda))
            {
                return ResultadoOperacion<ReporteCompleto>.Error("Moneda no válida.");
            }

            if (!ObtenerAgrupaciones().ContainsKey(filtro.Agrupacion ?? string.Empty))
            {
                filtro.Agrupacion = AgrupacionMes;
            }

            filtro.Provincia = string.IsNullOrWhiteSpace(filtro.Provincia) ? null : filtro.Provincia.Trim();
            filtro.TipoEspacio = string.IsNullOrWhiteSpace(filtro.TipoEspacio) ? null : filtro.TipoEspacio.Trim();

            string aviso = AjustarAgrupacion(filtro);

            try
            {
                ReporteCompleto reporte = new ReporteCompleto
                {
                    Filtro = filtro,
                    Aviso = aviso,
                    Indicadores = _mpp.ObtenerIndicadores(filtro),
                    Ingresos = CompletarPeriodos(_mpp.ListarIngresos(filtro), filtro),
                    Zonas = _mpp.ListarIngresosPorZona(filtro),
                    Estados = _mpp.ListarReservasPorEstado(filtro),
                    Encuestas = _mpp.ListarParticipacionEncuestas()
                };

                reporte.TotalImporteReservas = reporte.Ingresos.Sum(f => f.ImporteReservas);
                reporte.TotalImporteComisiones = reporte.Ingresos.Sum(f => f.ImporteComisiones);
                reporte.TotalReservasPeriodo = reporte.Ingresos.Sum(f => f.CantidadReservas);

                CalcularPorcentajesIngresos(reporte.Ingresos);
                reporte.ZonasDetalle = CalcularPorcentajesZonasDetalle(reporte.Zonas);
                reporte.Zonas = CalcularPorcentajesZonas(reporte.Zonas);
                CalcularPorcentajesEstados(reporte.Estados);
                CalcularParticipacion(reporte.Encuestas);

                return ResultadoOperacion<ReporteCompleto>.Ok(reporte);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<ReporteCompleto>.Error(ex.Message);
            }
        }

        public static string ObtenerEtiquetaPeriodo(DateTime inicio, string agrupacion)
        {
            switch (agrupacion)
            {
                case AgrupacionDia:
                    return inicio.ToString("dd/MM", Cultura);
                case AgrupacionSemana:
                    return "Sem. " + inicio.ToString("dd/MM", Cultura);
                case AgrupacionAnio:
                    return inicio.ToString("yyyy", Cultura);
                default:
                    string mes = Cultura.DateTimeFormat.GetAbbreviatedMonthName(inicio.Month).TrimEnd('.');
                    return Cultura.TextInfo.ToTitleCase(mes) + " " + inicio.ToString("yy", Cultura);
            }
        }

        public static string FormatearImporte(decimal importe, string moneda)
        {
            return (moneda == "USD" ? "US$ " : "$ ") + importe.ToString("N0", Cultura);
        }

        public static string NombreEstadoReserva(string estado)
        {
            switch (estado)
            {
                case "Pendiente": return "Pendiente de revisión";
                case "Aceptada": return "Aceptada";
                case "Rechazada": return "Rechazada";
                case "Cancelada": return "Cancelada";
                case "Finalizada": return "Finalizada";
                default: return estado;
            }
        }

        public static string NombrePublicoEncuesta(string publico)
        {
            switch (publico)
            {
                case "GestorEspacios": return "Gestores de espacios";
                case "ExternoSolicitante": return "Solicitantes";
                default: return "Todos";
            }
        }

        // CSV del reporte completo (ingresos por período, por zona, reservas
        // por estado y participación en encuestas), con punto y coma (lo abre
        // Excel en configuración regional de Argentina), importes sin
        // separador de miles y porcentajes con coma decimal.
        public static string GenerarCsvReporteCompleto(ReporteCompleto reporte)
        {
            System.Text.StringBuilder csv = new System.Text.StringBuilder();
            FiltroReporte filtro = reporte.Filtro;
            csv.AppendLine("Reporte StageUp");
            csv.AppendLine("Desde;" + filtro.Desde.ToString("dd/MM/yyyy", Cultura) +
                           ";Hasta;" + filtro.Hasta.ToString("dd/MM/yyyy", Cultura));
            csv.AppendLine("Moneda;" + filtro.Moneda +
                           ";Zona;" + CampoCsv(filtro.Provincia ?? "Todas") +
                           ";Tipo de espacio;" + CampoCsv(filtro.TipoEspacio ?? "Todos"));

            csv.AppendLine();
            csv.AppendLine("Ingresos por período (" + ObtenerAgrupaciones()[filtro.Agrupacion].ToLowerInvariant() + ")");
            csv.AppendLine("Inicio del período;Período;Reservas confirmadas;Importe reservas;Comisiones por cancelación");
            foreach (FilaReportePeriodo fila in reporte.Ingresos)
            {
                csv.AppendLine(string.Join(";",
                    fila.InicioPeriodo.ToString("dd/MM/yyyy", Cultura),
                    CampoCsv(fila.Etiqueta),
                    fila.CantidadReservas.ToString(Cultura),
                    Decimal2(fila.ImporteReservas),
                    Decimal2(fila.ImporteComisiones)));
            }

            csv.AppendLine(string.Join(";",
                "Total", string.Empty,
                reporte.TotalReservasPeriodo.ToString(Cultura),
                Decimal2(reporte.TotalImporteReservas),
                Decimal2(reporte.TotalImporteComisiones)));

            csv.AppendLine();
            csv.AppendLine("Ingresos por zona");
            csv.AppendLine("Provincia;Ciudad;Cantidad de reservas;Importe;Porcentaje del total");
            foreach (FilaReporteZona zona in reporte.ZonasDetalle)
            {
                csv.AppendLine(string.Join(";",
                    CampoCsv(zona.Provincia),
                    CampoCsv(zona.Ciudad),
                    zona.CantidadReservas.ToString(Cultura),
                    Decimal2(zona.ImporteReservas),
                    Decimal1(zona.PorcentajeDelTotal)));
            }

            csv.AppendLine();
            csv.AppendLine("Reservas por estado");
            csv.AppendLine("Estado;Cantidad;Porcentaje del total");
            foreach (FilaReporteEstado estado in reporte.Estados)
            {
                csv.AppendLine(string.Join(";",
                    CampoCsv(NombreEstadoReserva(estado.Estado)),
                    estado.Cantidad.ToString(Cultura),
                    Decimal1(estado.PorcentajeDelTotal)));
            }

            csv.AppendLine();
            csv.AppendLine("Participación en encuestas");
            csv.AppendLine("Encuesta;Estado;Público objetivo;Respuestas;Destinatarios;Tasa de participación;Vencimiento");
            foreach (FilaParticipacionEncuesta encuesta in reporte.Encuestas)
            {
                csv.AppendLine(string.Join(";",
                    CampoCsv(encuesta.Titulo),
                    CampoCsv(encuesta.Estado),
                    CampoCsv(NombrePublicoEncuesta(encuesta.PublicoObjetivo)),
                    encuesta.CantidadRespuestas.ToString(Cultura),
                    encuesta.CantidadDestinatarios.ToString(Cultura),
                    encuesta.TasaParticipacion.HasValue ? Decimal1(encuesta.TasaParticipacion.Value) : "-",
                    encuesta.FechaVencimiento.ToString("dd/MM/yyyy", Cultura)));
            }

            return csv.ToString();
        }

        private static string Decimal2(decimal valor)
        {
            return valor.ToString("0.00", Cultura);
        }

        private static string Decimal1(decimal valor)
        {
            return valor.ToString("0.0", Cultura);
        }

        private static string CampoCsv(string valor)
        {
            string texto = valor ?? string.Empty;
            // Evita que Excel interprete el contenido como fórmula.
            if (texto.Length > 0 && "=+-@".IndexOf(texto[0]) >= 0)
            {
                texto = "'" + texto;
            }

            if (texto.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
            {
                texto = "\"" + texto.Replace("\"", "\"\"") + "\"";
            }

            return texto;
        }

        private static string AjustarAgrupacion(FiltroReporte filtro)
        {
            int dias = (filtro.Hasta - filtro.Desde).Days + 1;
            if (filtro.Agrupacion == AgrupacionDia && dias > MaximoDiasAgrupandoPorDia)
            {
                filtro.Agrupacion = dias > MaximoDiasAgrupandoPorSemana ? AgrupacionMes : AgrupacionSemana;
                return "El período tiene más de " + MaximoDiasAgrupandoPorDia +
                       " días: para que el gráfico se pueda leer, se agrupó por " +
                       ObtenerAgrupaciones()[filtro.Agrupacion].ToLowerInvariant() + ".";
            }

            if (filtro.Agrupacion == AgrupacionSemana && dias > MaximoDiasAgrupandoPorSemana)
            {
                filtro.Agrupacion = AgrupacionMes;
                return "El período supera los dos años: para que el gráfico se pueda leer, se agrupó por mes.";
            }

            return null;
        }

        // Mismo criterio que el SP: la semana empieza el lunes.
        private static DateTime InicioDePeriodo(DateTime fecha, string agrupacion)
        {
            switch (agrupacion)
            {
                case AgrupacionDia:
                    return fecha.Date;
                case AgrupacionSemana:
                    int desdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
                    return fecha.Date.AddDays(-desdeLunes);
                case AgrupacionAnio:
                    return new DateTime(fecha.Year, 1, 1);
                default:
                    return new DateTime(fecha.Year, fecha.Month, 1);
            }
        }

        private static DateTime SiguientePeriodo(DateTime inicio, string agrupacion)
        {
            switch (agrupacion)
            {
                case AgrupacionDia:
                    return inicio.AddDays(1);
                case AgrupacionSemana:
                    return inicio.AddDays(7);
                case AgrupacionAnio:
                    return inicio.AddYears(1);
                default:
                    return inicio.AddMonths(1);
            }
        }

        // El SP solo devuelve los períodos con movimientos. Para que el gráfico
        // no "salte" períodos sin ingresos, se completan con cero.
        private static List<FilaReportePeriodo> CompletarPeriodos(List<FilaReportePeriodo> filas, FiltroReporte filtro)
        {
            Dictionary<DateTime, FilaReportePeriodo> porInicio = filas
                .GroupBy(f => f.InicioPeriodo.Date)
                .ToDictionary(g => g.Key, g => g.First());

            List<FilaReportePeriodo> completas = new List<FilaReportePeriodo>();
            DateTime ultimo = InicioDePeriodo(filtro.Hasta, filtro.Agrupacion);
            for (DateTime inicio = InicioDePeriodo(filtro.Desde, filtro.Agrupacion);
                 inicio <= ultimo;
                 inicio = SiguientePeriodo(inicio, filtro.Agrupacion))
            {
                FilaReportePeriodo fila;
                if (!porInicio.TryGetValue(inicio, out fila))
                {
                    fila = new FilaReportePeriodo { InicioPeriodo = inicio };
                }

                fila.Etiqueta = ObtenerEtiquetaPeriodo(inicio, filtro.Agrupacion);
                completas.Add(fila);
            }

            return completas;
        }

        private static void CalcularPorcentajesIngresos(List<FilaReportePeriodo> filas)
        {
            decimal maximo = filas.Count == 0 ? 0m : filas.Max(f => f.ImporteReservas);
            foreach (FilaReportePeriodo fila in filas)
            {
                fila.PorcentajeBarra = Porcentaje(fila.ImporteReservas, maximo);
            }
        }

        // Se muestran las 10 zonas con más ingresos; el resto se suma en "Otras".
        // Todas las zonas (copias, sin agrupar), con su porcentaje del total.
        private static List<FilaReporteZona> CalcularPorcentajesZonasDetalle(List<FilaReporteZona> zonas)
        {
            List<FilaReporteZona> detalle = zonas
                .OrderByDescending(z => z.ImporteReservas)
                .Select(z => new FilaReporteZona
                {
                    Provincia = z.Provincia,
                    Ciudad = z.Ciudad,
                    CantidadReservas = z.CantidadReservas,
                    ImporteReservas = z.ImporteReservas
                })
                .ToList();

            decimal total = detalle.Sum(z => z.ImporteReservas);
            foreach (FilaReporteZona zona in detalle)
            {
                zona.PorcentajeDelTotal = Porcentaje(zona.ImporteReservas, total);
            }

            return detalle;
        }

        private static List<FilaReporteZona> CalcularPorcentajesZonas(List<FilaReporteZona> zonas)
        {
            List<FilaReporteZona> ordenadas = zonas.OrderByDescending(z => z.ImporteReservas).ToList();
            if (ordenadas.Count > MaximoZonasEnGrafico)
            {
                List<FilaReporteZona> resto = ordenadas.Skip(MaximoZonasEnGrafico - 1).ToList();
                ordenadas = ordenadas.Take(MaximoZonasEnGrafico - 1).ToList();
                ordenadas.Add(new FilaReporteZona
                {
                    Provincia = "Otras zonas",
                    Ciudad = resto.Count + " ciudades",
                    CantidadReservas = resto.Sum(z => z.CantidadReservas),
                    ImporteReservas = resto.Sum(z => z.ImporteReservas)
                });
            }

            decimal total = ordenadas.Sum(z => z.ImporteReservas);
            decimal maximo = ordenadas.Count == 0 ? 0m : ordenadas.Max(z => z.ImporteReservas);
            foreach (FilaReporteZona zona in ordenadas)
            {
                zona.PorcentajeBarra = Porcentaje(zona.ImporteReservas, maximo);
                zona.PorcentajeDelTotal = Porcentaje(zona.ImporteReservas, total);
            }

            return ordenadas;
        }

        private static void CalcularPorcentajesEstados(List<FilaReporteEstado> estados)
        {
            int total = estados.Sum(e => e.Cantidad);
            int maximo = estados.Count == 0 ? 0 : estados.Max(e => e.Cantidad);
            foreach (FilaReporteEstado estado in estados)
            {
                estado.PorcentajeBarra = Porcentaje(estado.Cantidad, maximo);
                estado.PorcentajeDelTotal = Porcentaje(estado.Cantidad, total);
            }
        }

        private static void CalcularParticipacion(List<FilaParticipacionEncuesta> encuestas)
        {
            foreach (FilaParticipacionEncuesta encuesta in encuestas)
            {
                encuesta.TasaParticipacion = encuesta.CantidadDestinatarios > 0
                    ? Math.Min(100m, Porcentaje(encuesta.CantidadRespuestas, encuesta.CantidadDestinatarios))
                    : (decimal?)null;
                encuesta.PorcentajeBarra = encuesta.TasaParticipacion ?? 0m;
            }
        }

        private static decimal Porcentaje(decimal valor, decimal total)
        {
            return total <= 0m ? 0m : Math.Round(valor * 100m / total, 1);
        }
    }
}
