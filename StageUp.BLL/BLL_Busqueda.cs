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
    public class BLL_Busqueda
    {
        public const int LongitudMinimaTexto = 2;
        public const int LongitudMaximaTexto = 100;

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

        public ResultadoOperacion<List<GrupoResultadosBusqueda>> BuscarPublico(string texto, IEnumerable<string> tipos)
        {
            string textoNormalizado;
            ResultadoOperacion validacion = ValidarTexto(texto, out textoNormalizado);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error(validacion.Mensaje);
            }

            FiltroBusquedaGlobal filtro = new FiltroBusquedaGlobal
            {
                Texto = textoNormalizado,
                Tipos = FiltrarTipos(tipos, TiposPublicos.Select(t => t.Codigo).ToList())
            };

            try
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Ok(
                    Agrupar(_mpp.BuscarPublico(filtro), TiposPublicos, false));
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error(ex.Message);
            }
        }

        public ResultadoOperacion<List<GrupoResultadosBusqueda>> BuscarInterno(
            string texto, IEnumerable<string> tipos, int idRolInterno)
        {
            string textoNormalizado;
            ResultadoOperacion validacion = ValidarTexto(texto, out textoNormalizado);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error(validacion.Mensaje);
            }

            List<string> permitidos = ObtenerTiposInternosPermitidos(idRolInterno).Keys.ToList();
            if (permitidos.Count == 0)
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Error("Tu rol no tiene permisos para buscar contenido.");
            }

            FiltroBusquedaGlobal filtro = new FiltroBusquedaGlobal
            {
                Texto = textoNormalizado,
                Tipos = FiltrarTipos(tipos, permitidos)
            };

            try
            {
                return ResultadoOperacion<List<GrupoResultadosBusqueda>>.Ok(
                    Agrupar(_mpp.BuscarInterno(filtro), TiposInternos, true));
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
            textoNormalizado = textoNormalizado.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            return ResultadoOperacion.Ok();
        }

        private static List<GrupoResultadosBusqueda> Agrupar(
            List<ResultadoBusqueda> resultados, List<TipoBusqueda> tipos, bool esInterna)
        {
            List<GrupoResultadosBusqueda> grupos = new List<GrupoResultadosBusqueda>();
            foreach (TipoBusqueda tipo in tipos)
            {
                List<ResultadoBusqueda> delTipo = resultados.Where(r => r.Tipo == tipo.Codigo).ToList();
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
                case "FAQ": return "~/Ayuda/CentroAyuda.aspx";
                default: return null;
            }
        }

        private static string ObtenerUrlInterna(ResultadoBusqueda resultado)
        {
            switch (resultado.Tipo)
            {
                case "UsuarioExterno": return "~/Interno/AprobacionGestores.aspx";
                case "UsuarioInterno": return "~/Interno/GestionUsuariosInternos.aspx";
                case "Espacio": return "~/Explorar/DetalleEspacio.aspx?id=" + resultado.IdReferencia;
                case "Ticket": return "~/Interno/GestionSoporte.aspx?ver=" + resultado.IdReferencia;
                case "Novedad": return "~/Interno/GestionNovedades.aspx";
                case "FAQ": return "~/Interno/GestionFaq.aspx";
                case "Encuesta": return "~/Interno/GestionEncuestas.aspx";
                default: return null; // Reservas: no hay pantalla interna de detalle; el resultado ya muestra los datos.
            }
        }
    }
}
