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
                TipoEspacio = Texto(consulta["tipoEspacio"]),
                Provincia = Texto(consulta["provincia"]),
                Ciudad = Texto(consulta["ciudad"]),
                Zona = Texto(consulta["zona"]),
                FechaDisponibilidad = Fecha(consulta["fechaDisponible"]),
                Equipamiento = string.IsNullOrWhiteSpace(consulta["equipamiento"])
                    ? new List<string>()
                    : consulta["equipamiento"].Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList(),
                Caracteristicas = Texto(consulta["caracteristicas"]),
                CategoriaNovedad = Texto(consulta["categoria"]),
                Estado = Texto(consulta["estado"]),
                Perfil = Texto(consulta["perfil"]),
                Rol = Texto(consulta["rol"]),
                Responsable = Texto(consulta["responsable"]),
                Nombre = Texto(consulta["nombre"]),
                Correo = Texto(consulta["correo"]),
                Operacion = Texto(consulta["operacion"]),
                EntidadAfectada = Texto(consulta["entidad"]),
                Orden = Texto(consulta["orden"]) ?? FiltroBusquedaGlobal.OrdenRelevancia
            };

            int entero;
            if (int.TryParse(consulta["capacidad"], NumberStyles.Integer, CultureInfo.InvariantCulture, out entero))
            {
                filtro.CapacidadMinima = entero;
            }

            decimal precio;
            if (decimal.TryParse((consulta["precioMin"] ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                filtro.PrecioMinimo = precio;
            }

            if (decimal.TryParse((consulta["precio"] ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                filtro.PrecioMaximo = precio;
            }

            if (decimal.TryParse((consulta["calificacion"] ?? string.Empty).Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
            {
                filtro.CalificacionMinima = precio;
            }

            filtro.MinutoDesde = Minutos(consulta["horaDesde"]);
            filtro.MinutoHasta = Minutos(consulta["horaHasta"]);

            if (int.TryParse(consulta["idRegistro"], NumberStyles.Integer, CultureInfo.InvariantCulture, out entero))
            {
                filtro.IdRegistro = entero;
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
            Agregar(partes, "tipoEspacio", filtro.TipoEspacio);
            Agregar(partes, "provincia", filtro.Provincia);
            Agregar(partes, "ciudad", filtro.Ciudad);
            Agregar(partes, "zona", filtro.Zona);
            Agregar(partes, "precioMin", filtro.PrecioMinimo.HasValue ? filtro.PrecioMinimo.Value.ToString("0.##", CultureInfo.InvariantCulture) : null);
            Agregar(partes, "capacidad", filtro.CapacidadMinima.HasValue ? filtro.CapacidadMinima.Value.ToString(CultureInfo.InvariantCulture) : null);
            Agregar(partes, "precio", filtro.PrecioMaximo.HasValue ? filtro.PrecioMaximo.Value.ToString("0.##", CultureInfo.InvariantCulture) : null);
            Agregar(partes, "fechaDisponible", filtro.FechaDisponibilidad.HasValue ? filtro.FechaDisponibilidad.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture) : null);
            Agregar(partes, "horaDesde", Hora(filtro.MinutoDesde));
            Agregar(partes, "horaHasta", Hora(filtro.MinutoHasta));
            Agregar(partes, "equipamiento", filtro.Equipamiento.Count == 0 ? null : string.Join(",", filtro.Equipamiento));
            Agregar(partes, "caracteristicas", filtro.Caracteristicas);
            Agregar(partes, "calificacion", filtro.CalificacionMinima.HasValue ? filtro.CalificacionMinima.Value.ToString("0.##", CultureInfo.InvariantCulture) : null);
            Agregar(partes, "categoria", filtro.CategoriaNovedad);
            Agregar(partes, "estado", filtro.Estado);
            Agregar(partes, "perfil", filtro.Perfil);
            Agregar(partes, "rol", filtro.Rol);
            Agregar(partes, "responsable", filtro.Responsable);
            Agregar(partes, "nombre", filtro.Nombre);
            Agregar(partes, "correo", filtro.Correo);
            Agregar(partes, "operacion", filtro.Operacion);
            Agregar(partes, "entidad", filtro.EntidadAfectada);
            Agregar(partes, "idRegistro", filtro.IdRegistro.HasValue ? filtro.IdRegistro.Value.ToString(CultureInfo.InvariantCulture) : null);
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

            if (!string.IsNullOrEmpty(filtro.TipoEspacio)) partes.Add("tipo " + filtro.TipoEspacio);
            if (!string.IsNullOrEmpty(filtro.Provincia)) partes.Add("provincia " + filtro.Provincia);
            if (!string.IsNullOrEmpty(filtro.Ciudad)) partes.Add("ciudad " + filtro.Ciudad);
            if (!string.IsNullOrEmpty(filtro.Zona)) partes.Add("zona " + filtro.Zona);
            if (filtro.PrecioMinimo.HasValue) partes.Add("desde " + filtro.PrecioMinimo.Value.ToString("0.##", CultureInfo.InvariantCulture) + " por hora");

            if (filtro.PrecioMaximo.HasValue)
            {
                partes.Add("hasta " + filtro.PrecioMaximo.Value.ToString("0.##", CultureInfo.InvariantCulture) + " por hora");
            }

            if (filtro.FechaDisponibilidad.HasValue) partes.Add("disponible el " + filtro.FechaDisponibilidad.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
            if (filtro.MinutoDesde.HasValue && filtro.MinutoHasta.HasValue) partes.Add("de " + Hora(filtro.MinutoDesde) + " a " + Hora(filtro.MinutoHasta));
            if (filtro.Equipamiento.Count > 0) partes.Add("equipamiento: " + string.Join(", ", filtro.Equipamiento));
            if (!string.IsNullOrEmpty(filtro.Caracteristicas)) partes.Add("características: " + filtro.Caracteristicas);
            if (filtro.CalificacionMinima.HasValue) partes.Add(filtro.CalificacionMinima.Value.ToString("0.#", CultureInfo.InvariantCulture) + " estrellas o más");

            if (!string.IsNullOrEmpty(filtro.CategoriaNovedad))
            {
                partes.Add("categoría " + (BLL_Busqueda.CategoriasNovedad.ContainsKey(filtro.CategoriaNovedad) ? BLL_Busqueda.CategoriasNovedad[filtro.CategoriaNovedad] : filtro.CategoriaNovedad));
            }

            if (!string.IsNullOrEmpty(filtro.Estado))
            {
                partes.Add(etiquetasEstados != null && etiquetasEstados.ContainsKey(filtro.Estado) ? etiquetasEstados[filtro.Estado] : filtro.Estado);
            }

            if (!string.IsNullOrEmpty(filtro.Perfil)) partes.Add("perfil " + filtro.Perfil);
            if (!string.IsNullOrEmpty(filtro.Rol)) partes.Add("rol " + filtro.Rol);
            if (!string.IsNullOrEmpty(filtro.Responsable)) partes.Add("responsable " + filtro.Responsable);
            if (!string.IsNullOrEmpty(filtro.Nombre)) partes.Add("nombre " + filtro.Nombre);
            if (!string.IsNullOrEmpty(filtro.Correo)) partes.Add("correo " + filtro.Correo);
            if (!string.IsNullOrEmpty(filtro.Operacion)) partes.Add("operación " + filtro.Operacion);
            if (!string.IsNullOrEmpty(filtro.EntidadAfectada)) partes.Add("entidad " + filtro.EntidadAfectada);
            if (filtro.IdRegistro.HasValue) partes.Add("identificador " + filtro.IdRegistro.Value.ToString(CultureInfo.InvariantCulture));

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

        private static int? Minutos(string valor)
        {
            TimeSpan hora;
            return TimeSpan.TryParseExact((valor ?? string.Empty).Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out hora)
                ? (int?)hora.TotalMinutes
                : null;
        }

        private static string Hora(int? minutos)
        {
            return minutos.HasValue ? TimeSpan.FromMinutes(minutos.Value).ToString(@"hh\:mm", CultureInfo.InvariantCulture) : null;
        }
    }
}
