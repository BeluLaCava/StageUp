using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Búsqueda de toda la plataforma (ítems 4 y 13 de la segunda entrega).
    //
    // Pública (Buscar.aspx): espacios, novedades y preguntas frecuentes. La
    // búsqueda simple busca en todo; la avanzada deja elegir los tipos.
    //
    // Interna (Interno/BusquedaInterna.aspx): cada tipo de contenido exige un
    // permiso, y solo se buscan los tipos que el rol del usuario tiene. Un
    // usuario sin GESTIONAR_SOPORTE, por ejemplo, nunca ve tickets ni
    // reservas en los resultados, aunque los pida.
    //
    // Búsqueda avanzada (script 76): además del texto y los tipos, un
    // FiltroBusquedaGlobal con rango de fechas, ubicación, capacidad y precio
    // (espacios), categoría (novedades), estado (interna, un tipo por vez),
    // orden y cantidad de resultados. Se valida todo acá y la MPP lo pasa
    // como parámetros al SP (sin SQL dinámico). Con algún criterio avanzado
    // el texto es opcional.
    public class BLL_Busqueda
    {
        public const int LongitudMinimaTexto = 2;
        public const int LongitudMaximaTexto = 100;
        private const int LongitudMaximaUbicacion = 100;

        public static readonly int[] MaximosPermitidos = { 10, 25, 50, 100 };

        public static readonly Dictionary<string, string> Ordenes = new Dictionary<string, string>
        {
            { FiltroBusquedaGlobal.OrdenRelevancia, "Más relevantes" },
            { FiltroBusquedaGlobal.OrdenRecientes, "Más recientes" },
            { FiltroBusquedaGlobal.OrdenAntiguos, "Más antiguos" },
            { FiltroBusquedaGlobal.OrdenAlfabetico, "Alfabético" }
        };

        // Categorías de novedades (tabla CategoriaNovedad, script 85): código -> nombre.
        public static Dictionary<string, string> CategoriasNovedad
        {
            get { return BLL_CategoriaNovedad.ObtenerEtiquetas(); }
        }

        // Estados que se pueden filtrar en la búsqueda interna, por tipo.
        private static readonly Dictionary<string, Dictionary<string, string>> EstadosPorTipo = new Dictionary<string, Dictionary<string, string>>
        {
            { "UsuarioExterno", new Dictionary<string, string> { { "PendienteActivacion", "Pendiente de activación" }, { "Activa", "Activa" }, { "Inactiva", "Inactiva" } } },
            { "UsuarioInterno", new Dictionary<string, string> { { "Activa", "Activa" }, { "Inactiva", "Inactiva" } } },
            { "Espacio", new Dictionary<string, string> { { "Borrador", "Borrador" }, { "Publicado", "Publicado" }, { "Pausado", "Pausado" }, { "DadoDeBaja", "Dado de baja" } } },
            { "Reserva", new Dictionary<string, string> { { "Pendiente", "Pendiente" }, { "Aceptada", "Aceptada" }, { "Rechazada", "Rechazada" }, { "Cancelada", "Cancelada" }, { "Finalizada", "Finalizada" } } },
            { "Ticket", new Dictionary<string, string> { { "Pendiente", "Pendiente" }, { "EnProceso", "En proceso" }, { "Resuelta", "Resuelta" }, { "Cerrada", "Cerrada" } } },
            { "Novedad", new Dictionary<string, string> { { "Borrador", "Borrador" }, { "Programada", "Programada" }, { "Vigente", "Vigente" }, { "Vencida", "Vencida" }, { "DadaDeBaja", "Dada de baja" } } },
            { "FAQ", new Dictionary<string, string> { { "Borrador", "Borrador" }, { "Activo", "Activa" }, { "Inactivo", "Inactiva" } } },
            { "Encuesta", new Dictionary<string, string> { { "Borrador", "Borrador" }, { "Activa", "Activa" }, { "Cerrada", "Cerrada" } } }
        };

        private class TipoBusqueda
        {
            public string Codigo;
            public string Etiqueta;
            public string PermisoRequerido;
        }

        private static readonly List<TipoBusqueda> TiposPublicos = new List<TipoBusqueda>
        {
            new TipoBusqueda { Codigo = "Espacio", Etiqueta = "Espacios" },
            new TipoBusqueda { Codigo = "Novedad", Etiqueta = "Novedades" },
            new TipoBusqueda { Codigo = "FAQ", Etiqueta = "Ayuda / FAQ" }
        };

        private static readonly List<TipoBusqueda> TiposInternos = new List<TipoBusqueda>
        {
            new TipoBusqueda { Codigo = "UsuarioExterno", Etiqueta = "Usuarios externos", PermisoRequerido = "APROBAR_GESTORES" },
            new TipoBusqueda { Codigo = "UsuarioInterno", Etiqueta = "Usuarios internos", PermisoRequerido = "GESTIONAR_USUARIOS_INTERNOS" },
            new TipoBusqueda { Codigo = "Espacio", Etiqueta = "Espacios", PermisoRequerido = "BUSCAR_EN_PLATAFORMA" },
            new TipoBusqueda { Codigo = "Reserva", Etiqueta = "Reservas", PermisoRequerido = "GESTIONAR_SOPORTE" },
            new TipoBusqueda { Codigo = "Ticket", Etiqueta = "Tickets de soporte", PermisoRequerido = "GESTIONAR_SOPORTE" },
            new TipoBusqueda { Codigo = "Novedad", Etiqueta = "Novedades", PermisoRequerido = "GESTIONAR_NOVEDADES" },
            new TipoBusqueda { Codigo = "FAQ", Etiqueta = "Preguntas frecuentes", PermisoRequerido = "GESTIONAR_FAQ" },
            new TipoBusqueda { Codigo = "Encuesta", Etiqueta = "Encuestas", PermisoRequerido = "GESTIONAR_ENCUESTAS" }
        };

        private readonly MPP_Busqueda _mpp = new MPP_Busqueda();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        // Tipos disponibles (código, etiqueta), para armar los filtros.
        public static Dictionary<string, string> ObtenerTiposPublicos()
        {
            return TiposPublicos.ToDictionary(t => t.Codigo, t => t.Etiqueta);
        }

        public Dictionary<string, string> ObtenerTiposInternosPermitidos(int idRolInterno)
        {
            List<string> permisos = _bllPermiso.ListarCodigosPermisosDeRol(idRolInterno);
            return TiposInternos
                .Where(t => permisos.Contains(t.PermisoRequerido))
                .ToDictionary(t => t.Codigo, t => t.Etiqueta);
        }

        // Estados de la búsqueda interna para los tipos que el rol puede ver.
        // Valor "Tipo|Estado", etiqueta "Reservas · Pendiente".
        public Dictionary<string, string> ObtenerEstadosInternosPermitidos(int idRolInterno)
        {
            Dictionary<string, string> tipos = ObtenerTiposInternosPermitidos(idRolInterno);
            Dictionary<string, string> estados = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> tipo in tipos)
            {
                Dictionary<string, string> delTipo;
                if (!EstadosPorTipo.TryGetValue(tipo.Key, out delTipo))
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> estado in delTipo)
                {
                    estados[tipo.Key + "|" + estado.Key] = tipo.Value + " · " + estado.Value;
                }
            }
            return estados;
        }

        public ResultadoOperacion<List<GrupoResultadosBusqueda>> BuscarPublico(string texto, IEnumerable<string> tipos)
        {
            return BuscarPublico(new FiltroBusquedaGlobal { Texto = texto, Tipos = tipos == null ? new List<string>() : tipos.ToList() });
        }

        public ResultadoOperacion<List<GrupoResultadosBusqueda>> BuscarPublico(FiltroBusquedaGlobal pedido)
        {
            List<string> publicos = TiposPublicos.Select(t => t.Codigo).ToList();
            FiltroBusquedaGlobal filtro;
            ResultadoOperacion validacion = ValidarFiltro(pedido, publicos, false, TiposPublicos, out filtro);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error(validacion.Mensaje);
            }

            try
            {
                // El mensaje (si lo hay) avisa a qué tipos se limitó la búsqueda.
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Ok(
                    Agrupar(_mpp.BuscarPublico(filtro), TiposPublicos, false), validacion.Mensaje);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error(ex.Message);
            }
        }

        public ResultadoOperacion<List<GrupoResultadosBusqueda>> BuscarInterno(
            string texto, IEnumerable<string> tipos, int idRolInterno)
        {
            return BuscarInterno(new FiltroBusquedaGlobal { Texto = texto, Tipos = tipos == null ? new List<string>() : tipos.ToList() }, idRolInterno);
        }

        public ResultadoOperacion<List<GrupoResultadosBusqueda>> BuscarInterno(FiltroBusquedaGlobal pedido, int idRolInterno)
        {
            List<string> permitidos = ObtenerTiposInternosPermitidos(idRolInterno).Keys.ToList();
            if (permitidos.Count == 0)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error("Tu rol no tiene permisos para buscar contenido.");
            }

            FiltroBusquedaGlobal filtro;
            ResultadoOperacion validacion = ValidarFiltro(pedido, permitidos, true, TiposInternos, out filtro);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error(validacion.Mensaje);
            }

            try
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Ok(
                    Agrupar(_mpp.BuscarInterno(filtro), TiposInternos, true), validacion.Mensaje);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error(ex.Message);
            }
        }

        // Si no se eligió ningún tipo (búsqueda simple), se busca en todos los
        // permitidos. Si se eligieron, solo los que además estén permitidos.
        private static List<string> FiltrarTipos(IEnumerable<string> pedidos, List<string> permitidos)
        {
            List<string> listaPedida = pedidos == null ? new List<string>() : pedidos.Distinct().ToList();
            if (listaPedida.Count == 0)
            {
                return new List<string>(permitidos);
            }

            // Un tipo pedido que no está permitido simplemente no se busca.
            return listaPedida.Where(permitidos.Contains).ToList();
        }

        // Valida y normaliza el filtro pedido. Devuelve una copia limpia: los
        // tipos quedan reducidos a los permitidos y los textos, escapados
        // para LIKE.
        private static ResultadoOperacion ValidarFiltro(FiltroBusquedaGlobal pedido, List<string> permitidos, bool esInterna,
            List<TipoBusqueda> catalogoTipos, out FiltroBusquedaGlobal filtro)
        {
            filtro = null;
            if (pedido == null)
            {
                return ResultadoOperacion.Error("Indicá qué querés buscar.");
            }

            FiltroBusquedaGlobal limpio = new FiltroBusquedaGlobal
            {
                FechaDesde = pedido.FechaDesde.HasValue ? pedido.FechaDesde.Value.Date : (DateTime?)null,
                FechaHasta = pedido.FechaHasta.HasValue ? pedido.FechaHasta.Value.Date : (DateTime?)null,
                Ubicacion = string.IsNullOrWhiteSpace(pedido.Ubicacion) ? null : pedido.Ubicacion.Trim(),
                CapacidadMinima = esInterna ? null : pedido.CapacidadMinima,
                PrecioMaximo = esInterna ? null : pedido.PrecioMaximo,
                CategoriaNovedad = string.IsNullOrWhiteSpace(pedido.CategoriaNovedad) ? null : pedido.CategoriaNovedad.Trim(),
                Estado = esInterna && !string.IsNullOrWhiteSpace(pedido.Estado) ? pedido.Estado.Trim() : null,
                Orden = pedido.Orden != null && Ordenes.ContainsKey(pedido.Orden) ? pedido.Orden : FiltroBusquedaGlobal.OrdenRelevancia,
                Maximo = MaximosPermitidos.Contains(pedido.Maximo) ? pedido.Maximo : 25
            };

            if (string.IsNullOrWhiteSpace(pedido.Texto))
            {
                if (!limpio.TieneCriteriosAvanzados)
                {
                    return ResultadoOperacion.Error("Escribí al menos " + LongitudMinimaTexto + " caracteres para buscar, o usá algún filtro de la búsqueda avanzada.");
                }
            }
            else
            {
                string textoNormalizado;
                ResultadoOperacion validacionTexto = ValidarTexto(pedido.Texto, out textoNormalizado);
                if (!validacionTexto.Exitoso)
                {
                    return validacionTexto;
                }

                limpio.Texto = textoNormalizado;
            }

            if (limpio.FechaDesde.HasValue && limpio.FechaHasta.HasValue && limpio.FechaDesde.Value > limpio.FechaHasta.Value)
            {
                return ResultadoOperacion.Error("La fecha \"desde\" no puede ser posterior a la fecha \"hasta\".");
            }

            if ((limpio.FechaDesde.HasValue && limpio.FechaDesde.Value.Year < 2000) || (limpio.FechaHasta.HasValue && limpio.FechaHasta.Value.Year > 2100))
            {
                return ResultadoOperacion.Error("Revisá el rango de fechas.");
            }

            if (limpio.Ubicacion != null)
            {
                if (limpio.Ubicacion.Length < LongitudMinimaTexto || limpio.Ubicacion.Length > LongitudMaximaUbicacion)
                {
                    return ResultadoOperacion.Error("La ubicación tiene que tener entre " + LongitudMinimaTexto + " y " + LongitudMaximaUbicacion + " caracteres.");
                }

                limpio.Ubicacion = EscaparLike(limpio.Ubicacion);
            }

            if (limpio.CapacidadMinima.HasValue && (limpio.CapacidadMinima.Value < 1 || limpio.CapacidadMinima.Value > 100000))
            {
                return ResultadoOperacion.Error("La capacidad mínima tiene que ser un número mayor a cero.");
            }

            if (limpio.PrecioMaximo.HasValue && limpio.PrecioMaximo.Value <= 0)
            {
                return ResultadoOperacion.Error("El precio máximo tiene que ser mayor a cero.");
            }

            if (limpio.CategoriaNovedad != null && !CategoriasNovedad.ContainsKey(limpio.CategoriaNovedad))
            {
                return ResultadoOperacion.Error("Elegí una categoría de novedad válida.");
            }

            limpio.Tipos = FiltrarTipos(pedido.Tipos, permitidos);

            // El estado es de un tipo puntual: se busca solo en ese tipo.
            if (limpio.Estado != null)
            {
                string[] partes = limpio.Estado.Split('|');
                Dictionary<string, string> estadosDelTipo;
                if (partes.Length != 2 || !permitidos.Contains(partes[0]) ||
                    !EstadosPorTipo.TryGetValue(partes[0], out estadosDelTipo) || !estadosDelTipo.ContainsKey(partes[1]))
                {
                    return ResultadoOperacion.Error("Elegí un estado válido.");
                }

                limpio.Tipos = new List<string> { partes[0] };
                limpio.Estado = partes[1];
            }

            if (limpio.Tipos.Count == 0)
            {
                return ResultadoOperacion.Error("Elegí al menos un tipo de contenido que puedas buscar.");
            }

            // Filtros de un tipo de contenido puntual (ítem 6): cada uno se
            // aplica solo a los tipos que tienen ese dato, igual en la búsqueda
            // pública y en la interna. Nunca se ignora uno en silencio:
            //   * si hay filtros de tipos distintos (por ejemplo, ubicación de
            //     espacios y categoría de novedades), no hay contenido al que
            //     se apliquen los dos y se avisa;
            //   * si no, la búsqueda se limita a los tipos donde se aplican y
            //     el resultado lo dice.
            List<KeyValuePair<string, List<string>>> especificos = new List<KeyValuePair<string, List<string>>>();
            if (limpio.CapacidadMinima.HasValue || limpio.PrecioMaximo.HasValue)
            {
                especificos.Add(new KeyValuePair<string, List<string>>(
                    limpio.CapacidadMinima.HasValue && limpio.PrecioMaximo.HasValue ? "la capacidad y el precio" : limpio.CapacidadMinima.HasValue ? "la capacidad" : "el precio",
                    new List<string> { "Espacio" }));
            }

            if (limpio.Ubicacion != null)
            {
                especificos.Add(new KeyValuePair<string, List<string>>("la ubicación",
                    esInterna ? new List<string> { "Espacio", "Reserva" } : new List<string> { "Espacio" }));
            }

            if (limpio.CategoriaNovedad != null)
            {
                especificos.Add(new KeyValuePair<string, List<string>>("la categoría", new List<string> { "Novedad" }));
            }

            string aviso = null;
            if (especificos.Count > 0)
            {
                Func<IEnumerable<string>, string> etiquetas = codigos => UnirConY(codigos
                    .Select(c => catalogoTipos.FirstOrDefault(t => t.Codigo == c))
                    .Where(t => t != null)
                    .Select(t => t.Etiqueta.ToLowerInvariant())
                    .ToList());

                List<string> comunes = permitidos.Where(t => especificos.All(e => e.Value.Contains(t))).ToList();
                if (comunes.Count == 0)
                {
                    return ResultadoOperacion.Error("No se pueden combinar " + UnirConY(especificos
                        .Select(e => e.Key + " (filtro de " + etiquetas(e.Value.Where(permitidos.Contains)) + ")").ToList()) +
                        ": cada uno se aplica a un tipo de contenido distinto, así que juntos no darían ningún resultado. Hacé una búsqueda para cada uno.");
                }

                List<string> restringidos = limpio.Tipos.Where(comunes.Contains).ToList();
                string filtrosTexto = UnirConY(especificos.Select(e => e.Key).ToList());
                if (restringidos.Count == 0)
                {
                    return ResultadoOperacion.Error(Capitalizar(filtrosTexto) + (especificos.Count == 1 ? " se aplica" : " se aplican") +
                        " solo a " + etiquetas(comunes) + ": incluí ese tipo de contenido o quitá el filtro.");
                }

                if (restringidos.Count < limpio.Tipos.Count)
                {
                    aviso = "Se buscó solo en " + etiquetas(restringidos) + " porque " + filtrosTexto +
                        (especificos.Count == 1 ? " se aplica" : " se aplican") + " únicamente a ese contenido.";
                }

                limpio.Tipos = restringidos;
            }

            filtro = limpio;
            return ResultadoOperacion.Ok(aviso);
        }

        private static string UnirConY(List<string> partes)
        {
            if (partes.Count <= 1)
            {
                return partes.Count == 0 ? string.Empty : partes[0];
            }

            return string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[partes.Count - 1];
        }

        private static string Capitalizar(string texto)
        {
            return string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto.Substring(1);
        }

        private static string EscaparLike(string texto)
        {
            return texto.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }

        private static ResultadoOperacion ValidarTexto(string texto, out string textoNormalizado)
        {
            textoNormalizado = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
            if (textoNormalizado == null || textoNormalizado.Length < LongitudMinimaTexto)
            {
                return ResultadoOperacion.Error("Escribí al menos " + LongitudMinimaTexto + " caracteres para buscar.");
            }

            if (textoNormalizado.Length > LongitudMaximaTexto)
            {
                return ResultadoOperacion.Error("La búsqueda no puede superar los " + LongitudMaximaTexto + " caracteres.");
            }

            // Los comodines de LIKE se buscan como texto literal.
            textoNormalizado = EscaparLike(textoNormalizado);
            return ResultadoOperacion.Ok();
        }

        private static List<GrupoResultadosBusqueda> Agrupar(
            List<ResultadoBusqueda> resultados, List<TipoBusqueda> tipos, bool esInterna)
        {
            List<GrupoResultadosBusqueda> grupos = new List<GrupoResultadosBusqueda>();
            foreach (TipoBusqueda tipo in tipos)
            {
                List<ResultadoBusqueda> delTipo = resultados.Where(r => r.Tipo == tipo.Codigo).OrderBy(r => r.Posicion).ToList();
                if (delTipo.Count == 0)
                {
                    continue;
                }

                foreach (ResultadoBusqueda resultado in delTipo)
                {
                    resultado.EtiquetaTipo = tipo.Etiqueta;
                    resultado.Url = esInterna ? ObtenerUrlInterna(resultado) : ObtenerUrlPublica(resultado);
                }

                grupos.Add(new GrupoResultadosBusqueda
                {
                    Tipo = tipo.Codigo,
                    EtiquetaTipo = tipo.Etiqueta,
                    Resultados = delTipo
                });
            }
            return grupos;
        }

        private static string ObtenerUrlPublica(ResultadoBusqueda resultado)
        {
            switch (resultado.Tipo)
            {
                case "Espacio": return "~/Explorar/DetalleEspacio.aspx?id=" + resultado.IdReferencia;
                case "Novedad": return "~/Novedades.aspx";
                // Abre esa pregunta con su respuesta, aunque no sea de las básicas.
                case "FAQ": return "~/Ayuda/CentroAyuda.aspx?faq=" + resultado.IdReferencia + "#faq-title";
                default: return null;
            }
        }

        public const string UrlDetalleEspacioInterno = "~/Interno/DetalleEspacioInterno.aspx?id=";

        private static string ObtenerUrlInterna(ResultadoBusqueda resultado)
        {
            switch (resultado.Tipo)
            {
                case "UsuarioExterno": return "~/Interno/AprobacionGestores.aspx";
                case "UsuarioInterno": return "~/Interno/GestionUsuariosInternos.aspx";
                // Consulta interna de solo lectura: sirve también para espacios
                // sin publicar o dados de baja, sin exponerlos en la ficha pública.
                case "Espacio": return UrlDetalleEspacioInterno + resultado.IdReferencia;
                case "Ticket": return "~/Interno/GestionSoporte.aspx?ver=" + resultado.IdReferencia;
                case "Novedad": return "~/Interno/GestionNovedades.aspx";
                case "FAQ": return "~/Interno/GestionFaq.aspx";
                case "Encuesta": return "~/Interno/GestionEncuestas.aspx";
                default: return null; // Reservas: no hay pantalla interna de detalle; el resultado ya muestra los datos.
            }
        }
    }
}
