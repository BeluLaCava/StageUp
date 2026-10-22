using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{






    public class MPP_Encuesta
    {
        public List<Encuesta> Listar()
        {
            return MapearEncuestas(Conexion.Instance.Leer("sp_Encuesta_Listar"));
        }

        public Encuesta ObtenerPorId(Encuesta oEncuesta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Encuesta_ObtenerPorId",
                new Hashtable { { "@idEncuesta", oEncuesta.IdEncuesta } });

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

        public void CambiarEstado(Encuesta oEncuesta)
        {
            Conexion.Instance.Guardar(
                "sp_Encuesta_CambiarEstado",
                new Hashtable
                {
                    { "@idEncuesta", oEncuesta.IdEncuesta },
                    { "@estado", oEncuesta.Estado }
                });
        }




        public bool EliminarBorrador(Encuesta oEncuesta)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Encuesta_EliminarBorrador",
                new Hashtable { { "@idEncuesta", oEncuesta.IdEncuesta } });

            return resultado != null && resultado != DBNull.Value && Convert.ToInt32(resultado) > 0;
        }

        public List<Encuesta> ListarPendientesParaUsuario(Encuestado oEncuestado)
        {
            return MapearEncuestas(Conexion.Instance.Leer(
                "sp_Encuesta_ListarPendientesParaUsuario",
                new Hashtable
                {
                    { "@idUsuarioExterno", Valor(oEncuestado.IdUsuarioExterno) },
                    { "@perfilUsuario", Valor(oEncuestado.PerfilUsuario) },
                    { "@identificadorAnonimo", Valor(oEncuestado.IdentificadorAnonimo) }
                }));
        }

        public List<Encuesta> ListarConResultadosParaUsuario(Encuestado oEncuestado)
        {
            return MapearEncuestas(Conexion.Instance.Leer(
                "sp_Encuesta_ListarConResultadosParaUsuario",
                new Hashtable
                {
                    { "@idUsuarioExterno", Valor(oEncuestado.IdUsuarioExterno) },
                    { "@perfilUsuario", Valor(oEncuestado.PerfilUsuario) },
                    { "@identificadorAnonimo", Valor(oEncuestado.IdentificadorAnonimo) }
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

        public List<PreguntaEncuesta> ListarPreguntasPorEncuesta(Encuesta oEncuesta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_PreguntaEncuesta_ListarPorEncuesta",
                new Hashtable { { "@idEncuesta", oEncuesta.IdEncuesta } });

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

        public void EliminarPregunta(PreguntaEncuesta oPregunta)
        {
            Conexion.Instance.Guardar(
                "sp_PreguntaEncuesta_Eliminar",
                new Hashtable { { "@idPreguntaEncuesta", oPregunta.IdPreguntaEncuesta } });
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

        public List<OpcionPregunta> ListarOpcionesPorPregunta(PreguntaEncuesta oPregunta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_OpcionPregunta_ListarPorPregunta",
                new Hashtable { { "@idPreguntaEncuesta", oPregunta.IdPreguntaEncuesta } });

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

        public bool ExisteRespuestaDeUsuario(RespuestaEncuesta oRespuesta)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RespuestaEncuesta_ExisteDeUsuario",
                new Hashtable
                {
                    { "@idEncuesta", oRespuesta.IdEncuesta },
                    { "@idUsuarioExterno", Valor(oRespuesta.IdUsuarioExterno) },
                    { "@identificadorAnonimo", Valor(oRespuesta.IdentificadorAnonimo) }
                });

            return resultado != null && Convert.ToBoolean(resultado);
        }





        public int RegistrarRespuesta(RespuestaEncuesta oRespuesta, List<RespuestaEncuestaDetalle> detalles, string descripcionBitacora)
        {
            List<string> pares = new List<string>();
            foreach (RespuestaEncuestaDetalle detalle in detalles)
            {
                pares.Add(detalle.IdPreguntaEncuesta.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                    detalle.IdOpcionPregunta.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            object resultado = Conexion.Instance.LeerEscalar(
                "sp_RespuestaEncuesta_Registrar",
                new Hashtable
                {
                    { "@idEncuesta", oRespuesta.IdEncuesta },
                    { "@idUsuarioExterno", Valor(oRespuesta.IdUsuarioExterno) },
                    { "@identificadorNavegador", Valor(oRespuesta.IdentificadorAnonimo) },
                    { "@opciones", string.Join(",", pares) },
                    { "@descripcionBitacora", Valor(descripcionBitacora) }
                });

            return Convert.ToInt32(resultado);
        }



        public List<FilaResultadoEncuesta> ConsultarResultadosFilas(Encuesta oEncuesta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Encuesta_ConsultarResultados",
                new Hashtable { { "@idEncuesta", oEncuesta.IdEncuesta } });

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

        private static object Valor(int? valor)
        {
            return valor.HasValue ? (object)valor.Value : DBNull.Value;
        }

        private static object Valor(string valor)
        {
            return string.IsNullOrEmpty(valor) ? (object)DBNull.Value : valor;
        }
    }
}
