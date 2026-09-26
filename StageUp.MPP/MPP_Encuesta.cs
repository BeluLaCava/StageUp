using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Encuestas dinámicas con fecha de vencimiento y gráfico de resultados
    // al instante. Mismo patrón que MPP_Faq.cs (Conexion.Instance +
    // Hashtable de parámetros).
    public class MPP_Encuesta
    {
        public List<Encuesta> Listar()
        {
            return MapearEncuestas(Conexion.Instance.Leer("sp_Encuesta_Listar"));
        }

        public Encuesta ObtenerPorId(int idEncuesta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Encuesta_ObtenerPorId",
                new Hashtable { { "@idEncuesta", idEncuesta } });

            return tabla.Rows.Count == 0 ? null : MapearFilaEncuesta(tabla.Rows[0]);
        }

        public int Insertar(Encuesta oEncuesta)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Encuesta_Insertar",
                new Hashtable
                {
                    { "@titulo", oEncuesta.Titulo },
                    { "@descripcion", oEncuesta.Descripcion },
                    { "@fechaInicio", oEncuesta.FechaInicio },
                    { "@fechaVencimiento", oEncuesta.FechaVencimiento },
                    { "@publicoObjetivo", oEncuesta.PublicoObjetivo }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(Encuesta oEncuesta)
        {
            Conexion.Instance.Guardar(
                "sp_Encuesta_Modificar",
                new Hashtable
                {
                    { "@idEncuesta", oEncuesta.IdEncuesta },
                    { "@titulo", oEncuesta.Titulo },
                    { "@descripcion", oEncuesta.Descripcion },
                    { "@fechaInicio", oEncuesta.FechaInicio },
                    { "@fechaVencimiento", oEncuesta.FechaVencimiento },
                    { "@publicoObjetivo", oEncuesta.PublicoObjetivo }
                });
        }

        public void CambiarEstado(int idEncuesta, string estado)
        {
            Conexion.Instance.Guardar(
                "sp_Encuesta_CambiarEstado",
                new Hashtable
                {
                    { "@idEncuesta", idEncuesta },
                    { "@estado", estado }
                });
        }

        public List<Encuesta> ListarPendientesParaUsuario(int idUsuarioExterno, string perfilUsuario)
        {
            return MapearEncuestas(Conexion.Instance.Leer(
                "sp_Encuesta_ListarPendientesParaUsuario",
                new Hashtable
                {
                    { "@idUsuarioExterno", idUsuarioExterno },
                    { "@perfilUsuario", perfilUsuario }
                }));
        }

        public List<Encuesta> ListarConResultadosParaUsuario(int idUsuarioExterno, string perfilUsuario)
        {
            return MapearEncuestas(Conexion.Instance.Leer(
                "sp_Encuesta_ListarConResultadosParaUsuario",
                new Hashtable
                {
                    { "@idUsuarioExterno", idUsuarioExterno },
                    { "@perfilUsuario", perfilUsuario }
                }));
        }

        public int InsertarPregunta(PreguntaEncuesta oPregunta)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_PreguntaEncuesta_Insertar",
                new Hashtable
                {
                    { "@idEncuesta", oPregunta.IdEncuesta },
                    { "@texto", oPregunta.Texto },
                    { "@orden", oPregunta.Orden }
                });

            return Convert.ToInt32(resultado);
        }

        public List<PreguntaEncuesta> ListarPreguntasPorEncuesta(int idEncuesta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_PreguntaEncuesta_ListarPorEncuesta",
                new Hashtable { { "@idEncuesta", idEncuesta } });

            List<PreguntaEncuesta> lista = new List<PreguntaEncuesta>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new PreguntaEncuesta
                {
                    IdPreguntaEncuesta = Convert.ToInt32(fila["idPreguntaEncuesta"]),
                    IdEncuesta = Convert.ToInt32(fila["idEncuesta"]),
                    Texto = fila["texto"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"])
                });
            }
            return lista;
        }

        public void EliminarPregunta(int idPreguntaEncuesta)
        {
            Conexion.Instance.Guardar(
                "sp_PreguntaEncuesta_Eliminar",
                new Hashtable { { "@idPreguntaEncuesta", idPreguntaEncuesta } });
        }

        public int InsertarOpcion(OpcionPregunta oOpcion)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_OpcionPregunta_Insertar",
                new Hashtable
                {
                    { "@idPreguntaEncuesta", oOpcion.IdPreguntaEncuesta },
                    { "@texto", oOpcion.Texto },
                    { "@orden", oOpcion.Orden }
                });

            return Convert.ToInt32(resultado);
        }

        public List<OpcionPregunta> ListarOpcionesPorPregunta(int idPreguntaEncuesta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_OpcionPregunta_ListarPorPregunta",
                new Hashtable { { "@idPreguntaEncuesta", idPreguntaEncuesta } });

            List<OpcionPregunta> lista = new List<OpcionPregunta>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new OpcionPregunta
                {
                    IdOpcionPregunta = Convert.ToInt32(fila["idOpcionPregunta"]),
                    IdPreguntaEncuesta = Convert.ToInt32(fila["idPreguntaEncuesta"]),
                    Texto = fila["texto"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"])
                });
            }
            return lista;
        }

        public bool ExisteRespuestaDeUsuario(int idEncuesta, int idUsuarioExterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RespuestaEncuesta_ExisteDeUsuario",
                new Hashtable
                {
                    { "@idEncuesta", idEncuesta },
                    { "@idUsuarioExterno", idUsuarioExterno }
                });

            return resultado != null && Convert.ToBoolean(resultado);
        }

        public int InsertarRespuesta(int idEncuesta, int idUsuarioExterno)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RespuestaEncuesta_Insertar",
                new Hashtable
                {
                    { "@idEncuesta", idEncuesta },
                    { "@idUsuarioExterno", idUsuarioExterno }
                });

            return Convert.ToInt32(resultado);
        }

        public void InsertarRespuestaDetalle(int idRespuestaEncuesta, int idPreguntaEncuesta, int idOpcionPregunta)
        {
            Conexion.Instance.Guardar(
                "sp_RespuestaEncuestaDetalle_Insertar",
                new Hashtable
                {
                    { "@idRespuestaEncuesta", idRespuestaEncuesta },
                    { "@idPreguntaEncuesta", idPreguntaEncuesta },
                    { "@idOpcionPregunta", idOpcionPregunta }
                });
        }

        // Filas planas pregunta+opción con los conteos calculados en el
        // momento. La BLL las agrupa en ResultadoPreguntaEncuesta.
        public List<FilaResultadoEncuesta> ConsultarResultadosFilas(int idEncuesta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Encuesta_ConsultarResultados",
                new Hashtable { { "@idEncuesta", idEncuesta } });

            List<FilaResultadoEncuesta> lista = new List<FilaResultadoEncuesta>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new FilaResultadoEncuesta
                {
                    IdPreguntaEncuesta = Convert.ToInt32(fila["idPreguntaEncuesta"]),
                    TextoPregunta = fila["textoPregunta"].ToString(),
                    OrdenPregunta = Convert.ToInt32(fila["ordenPregunta"]),
                    IdOpcionPregunta = Convert.ToInt32(fila["idOpcionPregunta"]),
                    TextoOpcion = fila["textoOpcion"].ToString(),
                    OrdenOpcion = Convert.ToInt32(fila["ordenOpcion"]),
                    CantidadRespuestas = Convert.ToInt32(fila["cantidadRespuestas"]),
                    TotalRespuestasPregunta = Convert.ToInt32(fila["totalRespuestasPregunta"])
                });
            }
            return lista;
        }

        private static List<Encuesta> MapearEncuestas(DataTable tabla)
        {
            List<Encuesta> lista = new List<Encuesta>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearFilaEncuesta(fila));
            }
            return lista;
        }

        private static Encuesta MapearFilaEncuesta(DataRow fila)
        {
            return new Encuesta
            {
                IdEncuesta = Convert.ToInt32(fila["idEncuesta"]),
                Titulo = fila["titulo"].ToString(),
                Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                FechaInicio = Convert.ToDateTime(fila["fechaInicio"]),
                FechaVencimiento = Convert.ToDateTime(fila["fechaVencimiento"]),
                PublicoObjetivo = fila["publicoObjetivo"].ToString(),
                Estado = fila["estado"].ToString(),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(fila["fechaUltimaModificacion"])
            };
        }
    }
}
