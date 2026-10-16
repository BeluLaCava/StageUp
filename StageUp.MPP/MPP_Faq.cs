using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-003-004 Gestionar contenido de ayuda y CU-003-001 (script 67).
    public class MPP_Faq
    {
        // Centro de ayuda público y asistente: solo contenido activo.
        public List<Faq> ListarActivas()
        {
            return Mapear(Conexion.Instance.Leer("sp_Faq_ListarActivas"));
        }

        public List<Faq> Listar(FiltroFaq oFiltro)
        {
            oFiltro = oFiltro ?? new FiltroFaq();
            return Mapear(Conexion.Instance.Leer(
                "sp_Faq_Listar",
                new Hashtable
                {
                    { "@estado", string.IsNullOrEmpty(oFiltro.Estado) ? (object)DBNull.Value : oFiltro.Estado },
                    { "@idCategoriaAyuda", oFiltro.IdCategoriaAyuda.HasValue ? (object)oFiltro.IdCategoriaAyuda.Value : DBNull.Value },
                    { "@texto", string.IsNullOrWhiteSpace(oFiltro.Texto) ? (object)DBNull.Value : oFiltro.Texto.Trim() }
                }));
        }

        public Faq ObtenerPorId(Faq oFaq)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Faq_ObtenerPorId",
                new Hashtable { { "@idFaq", oFaq.IdFaq } });

            return tabla.Rows.Count == 0 ? null : MapearFila(tabla.Rows[0]);
        }

        public List<Faq> ListarPreguntas()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Faq_ListarPreguntas");
            List<Faq> lista = new List<Faq>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new Faq
                {
                    IdFaq = Convert.ToInt32(fila["idFaq"]),
                    Pregunta = fila["pregunta"].ToString(),
                    Estado = fila["estado"].ToString()
                });
            }
            return lista;
        }

        public int Insertar(Faq oFaq)
        {
            object resultado = Conexion.Instance.LeerEscalar("sp_Faq_Insertar", Parametros(oFaq, false));
            return Convert.ToInt32(resultado);
        }

        public bool Modificar(Faq oFaq)
        {
            object filas = Conexion.Instance.LeerEscalar("sp_Faq_Modificar", Parametros(oFaq, true));
            return filas != null && filas != DBNull.Value && Convert.ToInt32(filas) > 0;
        }

        public bool CambiarEstado(Faq oFaq)
        {
            object filas = Conexion.Instance.LeerEscalar(
                "sp_Faq_CambiarEstado",
                new Hashtable
                {
                    { "@idFaq", oFaq.IdFaq },
                    { "@estado", oFaq.Estado }
                });
            return filas != null && filas != DBNull.Value && Convert.ToInt32(filas) > 0;
        }

        public List<CategoriaAyuda> ListarCategorias()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_CategoriaAyuda_Listar");
            List<CategoriaAyuda> lista = new List<CategoriaAyuda>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new CategoriaAyuda
                {
                    IdCategoriaAyuda = Convert.ToInt32(fila["idCategoriaAyuda"]),
                    Codigo = fila["codigo"].ToString(),
                    Nombre = fila["nombre"].ToString(),
                    Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"]),
                    Activa = Convert.ToBoolean(fila["activa"]),
                    CantidadActivas = Convert.ToInt32(fila["cantidadActivas"]),
                    CantidadAsistente = Convert.ToInt32(fila["cantidadAsistente"])
                });
            }
            return lista;
        }

        // CU-003-001 A4/A5: consulta escrita en el asistente, con el paso del
        // asistente o la pregunta frecuente que la respondió (o ninguno).
        public void RegistrarConsultaAsistente(string texto, string textoNormalizado, int? idNodoAsistente, int? idFaqRespuesta, int? idUsuarioExterno)
        {
            Conexion.Instance.LeerEscalar(
                "sp_ConsultaAsistente_Registrar",
                new Hashtable
                {
                    { "@texto", texto },
                    { "@textoNormalizado", textoNormalizado },
                    { "@idNodoAsistente", idNodoAsistente.HasValue ? (object)idNodoAsistente.Value : DBNull.Value },
                    { "@idFaqRespuesta", idFaqRespuesta.HasValue ? (object)idFaqRespuesta.Value : DBNull.Value },
                    { "@idUsuarioExterno", idUsuarioExterno.HasValue ? (object)idUsuarioExterno.Value : DBNull.Value }
                });
        }

        public List<ConsultaAsistenteSinRespuesta> ListarConsultasSinRespuesta()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ConsultaAsistente_ListarSinRespuesta");
            List<ConsultaAsistenteSinRespuesta> lista = new List<ConsultaAsistenteSinRespuesta>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new ConsultaAsistenteSinRespuesta
                {
                    Texto = fila["texto"].ToString(),
                    TextoNormalizado = fila["textoNormalizado"].ToString(),
                    Cantidad = Convert.ToInt32(fila["cantidad"]),
                    UltimaFecha = Convert.ToDateTime(fila["ultimaFecha"])
                });
            }
            return lista;
        }

        public void MarcarConsultaAtendida(string textoNormalizado)
        {
            Conexion.Instance.LeerEscalar(
                "sp_ConsultaAsistente_MarcarAtendida",
                new Hashtable { { "@textoNormalizado", textoNormalizado } });
        }

        private static Hashtable Parametros(Faq oFaq, bool incluirId)
        {
            Hashtable parametros = new Hashtable
            {
                { "@pregunta", oFaq.Pregunta },
                { "@respuesta", oFaq.Respuesta },
                { "@orden", oFaq.Orden },
                { "@estado", oFaq.Estado },
                { "@idCategoriaAyuda", oFaq.IdCategoriaAyuda.HasValue ? (object)oFaq.IdCategoriaAyuda.Value : DBNull.Value },
                { "@usarEnAsistente", oFaq.UsarEnAsistente },
                { "@palabrasClave", string.IsNullOrWhiteSpace(oFaq.PalabrasClave) ? (object)DBNull.Value : oFaq.PalabrasClave }
            };

            if (incluirId)
            {
                parametros.Add("@idFaq", oFaq.IdFaq);
            }

            return parametros;
        }

        private static List<Faq> Mapear(DataTable tabla)
        {
            List<Faq> lista = new List<Faq>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearFila(fila));
            }
            return lista;
        }

        private static Faq MapearFila(DataRow fila)
        {
            DataColumnCollection columnas = fila.Table.Columns;
            bool activo = Convert.ToBoolean(fila["activo"]);
            return new Faq
            {
                IdFaq = Convert.ToInt32(fila["idFaq"]),
                Pregunta = fila["pregunta"].ToString(),
                Respuesta = fila["respuesta"].ToString(),
                Orden = Convert.ToInt32(fila["orden"]),
                Activo = activo,
                Estado = columnas.Contains("estado") && fila["estado"] != DBNull.Value ? fila["estado"].ToString() : (activo ? "Activo" : "Borrador"),
                IdCategoriaAyuda = columnas.Contains("idCategoriaAyuda") && fila["idCategoriaAyuda"] != DBNull.Value ? Convert.ToInt32(fila["idCategoriaAyuda"]) : (int?)null,
                NombreCategoria = columnas.Contains("nombreCategoria") && fila["nombreCategoria"] != DBNull.Value ? fila["nombreCategoria"].ToString() : null,
                OrdenCategoria = columnas.Contains("ordenCategoria") && fila["ordenCategoria"] != DBNull.Value ? Convert.ToInt32(fila["ordenCategoria"]) : 0,
                CategoriaActiva = !columnas.Contains("categoriaActiva") || (fila["categoriaActiva"] != DBNull.Value && Convert.ToBoolean(fila["categoriaActiva"])),
                UsarEnAsistente = columnas.Contains("usarEnAsistente") && fila["usarEnAsistente"] != DBNull.Value && Convert.ToBoolean(fila["usarEnAsistente"]),
                PalabrasClave = columnas.Contains("palabrasClave") && fila["palabrasClave"] != DBNull.Value ? fila["palabrasClave"].ToString() : null,
                FechaAlta = columnas.Contains("fechaAlta") && fila["fechaAlta"] != DBNull.Value ? Convert.ToDateTime(fila["fechaAlta"]) : DateTime.MinValue,
                FechaUltimaModificacion = columnas.Contains("fechaUltimaModificacion") && fila["fechaUltimaModificacion"] != DBNull.Value
                    ? Convert.ToDateTime(fila["fechaUltimaModificacion"]) : (DateTime?)null
            };
        }
    }
}
