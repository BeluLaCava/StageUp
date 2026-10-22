using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Infraestructura
{





    public static class ConsultaBusqueda
    {
        private const string FormatoFecha = "yyyy-MM-dd";

        public static FiltroBusquedaGlobal Leer(NameValueCollection consulta)
        {
            FiltroBusquedaGlobal filtro = new FiltroBusquedaGlobal
            {
                Texto = Texto(consulta["q"]),
                Tipos = string.IsNullOrWhiteSpace(consulta["tipos"])
                    ? new List<string>()
                    : consulta["tipos"].Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList(),
                FechaDesde = Fecha(consulta["desde"]),
                FechaHasta = Fecha(consulta["hasta"]),
                Ubicacion = Texto(consulta["ubicacion"]),
                CategoriaNovedad = Texto(consulta["categoria"]),
                Estado = Texto(consulta["estado"]),
                Orden = Texto(consulta["orden"]) ?? FiltroBusquedaGlobal.OrdenRelevancia
            };

            int entero;
            if (int.TryParse(consulta["capacidad"], NumberStyles.Integer, CultureInfo.InvariantCulture, out entero))
            {
                filtro.CapacidadMinima = entero;
            }

            decimal precio;
            if (decimal.TryParse((consulta["precio"] ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                filtro.PrecioMaximo = precio;
            }

            if (int.TryParse(consulta["max"], NumberStyles.Integer, CultureInfo.InvariantCulture, out entero))
            {
                filtro.Maximo = entero;
            }

            return filtro;
        }

        public static bool HayBusqueda(FiltroBusquedaGlobal filtro)
        {
            return !string.IsNullOrWhiteSpace(filtro.Texto) || filtro.TieneCriteriosAvanzados;
        }

        public static bool UsaOpcionesAvanzadas(FiltroBusquedaGlobal filtro)
        {
            return filtro.Tipos.Count > 0 || filtro.TieneCriteriosAvanzados ||
                filtro.Orden != FiltroBusquedaGlobal.OrdenRelevancia || filtro.Maximo != 25;
        }

        public static string Armar(string pagina, FiltroBusquedaGlobal filtro)
        {
            List<string> partes = new List<string>();
            Agregar(partes, "q", filtro.Texto);
            Agregar(partes, "tipos", filtro.Tipos.Count == 0 ? null : string.Join(",", filtro.Tipos));
            Agregar(partes, "desde", filtro.FechaDesde.HasValue ? filtro.FechaDesde.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture) : null);
            Agregar(partes, "hasta", filtro.FechaHasta.HasValue ? filtro.FechaHasta.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture) : null);
            Agregar(partes, "ubicacion", filtro.Ubicacion);
            Agregar(partes, "capacidad", filtro.CapacidadMinima.HasValue ? filtro.CapacidadMinima.Value.ToString(CultureInfo.InvariantCulture) : null);
            Agregar(partes, "precio", filtro.PrecioMaximo.HasValue ? filtro.PrecioMaximo.Value.ToString("0.##", CultureInfo.InvariantCulture) : null);
            Agregar(partes, "categoria", filtro.CategoriaNovedad);
            Agregar(partes, "estado", filtro.Estado);
            Agregar(partes, "orden", filtro.Orden == FiltroBusquedaGlobal.OrdenRelevancia ? null : filtro.Orden);
            Agregar(partes, "max", filtro.Maximo == 25 ? null : filtro.Maximo.ToString(CultureInfo.InvariantCulture));
            return partes.Count == 0 ? pagina : pagina + "?" + string.Join("&", partes);
        }


        public static string Describir(FiltroBusquedaGlobal filtro, IDictionary<string, string> etiquetasTipos, IDictionary<string, string> etiquetasEstados)
        {
            List<string> partes = new List<string>();
            if (filtro.Tipos.Count > 0)
            {
                partes.Add(string.Join(", ", filtro.Tipos.Select(t => etiquetasTipos != null && etiquetasTipos.ContainsKey(t) ? etiquetasTipos[t] : t)));
            }

            if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue)
            {
                partes.Add("del " + filtro.FechaDesde.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " al " + filtro.FechaHasta.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
            }
            else if (filtro.FechaDesde.HasValue)
            {
                partes.Add("desde el " + filtro.FechaDesde.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
            }
            else if (filtro.FechaHasta.HasValue)
            {
                partes.Add("hasta el " + filtro.FechaHasta.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(filtro.Ubicacion))
            {
                partes.Add("en " + filtro.Ubicacion);
            }

            if (filtro.CapacidadMinima.HasValue)
            {
                partes.Add("para " + filtro.CapacidadMinima.Value.ToString(CultureInfo.InvariantCulture) + " personas o más");
            }

            if (filtro.PrecioMaximo.HasValue)
            {
                partes.Add("hasta " + filtro.PrecioMaximo.Value.ToString("0.##", CultureInfo.InvariantCulture) + " por hora");
            }

            if (!string.IsNullOrEmpty(filtro.CategoriaNovedad))
            {
                partes.Add("categoría " + (BLL_Busqueda.CategoriasNovedad.ContainsKey(filtro.CategoriaNovedad) ? BLL_Busqueda.CategoriasNovedad[filtro.CategoriaNovedad] : filtro.CategoriaNovedad));
            }

            if (!string.IsNullOrEmpty(filtro.Estado))
            {
                partes.Add(etiquetasEstados != null && etiquetasEstados.ContainsKey(filtro.Estado) ? etiquetasEstados[filtro.Estado] : filtro.Estado);
            }

            if (filtro.Orden != FiltroBusquedaGlobal.OrdenRelevancia && BLL_Busqueda.Ordenes.ContainsKey(filtro.Orden))
            {
                partes.Add("orden: " + BLL_Busqueda.Ordenes[filtro.Orden].ToLowerInvariant());
            }

            return string.Join(" · ", partes);
        }

        private static void Agregar(List<string> partes, string clave, string valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                partes.Add(clave + "=" + HttpUtility.UrlEncode(valor.Trim()));
            }
        }

        private static string Texto(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static DateTime? Fecha(string valor)
        {
            DateTime fecha;
            return DateTime.TryParseExact((valor ?? string.Empty).Trim(), FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                ? fecha
                : (DateTime?)null;
        }
    }
}
