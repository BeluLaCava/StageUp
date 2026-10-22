using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

    public class MPP_PreguntaEspacio
    {
        public int Insertar(PreguntaEspacio oPregunta)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_PreguntaEspacio_Insertar",
                new Hashtable
                {
                    { "@idEspacioArtistico", oPregunta.IdEspacioArtistico },
                    { "@idUsuarioExterno", oPregunta.IdUsuarioExterno },
                    { "@pregunta", oPregunta.Pregunta }
                });

            return Convert.ToInt32(resultado);
        }

        public PreguntaEspacio ObtenerPorId(PreguntaEspacio oPregunta)
        {
            List<PreguntaEspacio> lista = Mapear(Conexion.Instance.Leer(
                "sp_PreguntaEspacio_ObtenerPorId",
                new Hashtable { { "@idPreguntaEspacio", oPregunta.IdPreguntaEspacio } }));
            return lista.Count == 0 ? null : lista[0];
        }

        public List<PreguntaEspacio> ListarPublicas(EspacioArtistico oEspacio, int? cantidad)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_PreguntaEspacio_ListarPublicas",
                new Hashtable
                {
                    { "@idEspacioArtistico", oEspacio.IdEspacioArtistico },
                    { "@cantidad", cantidad.HasValue ? (object)cantidad.Value : DBNull.Value }
                }));
        }

        public int ContarPublicas(EspacioArtistico oEspacio)
        {
            return Entero(Conexion.Instance.LeerEscalar(
                "sp_PreguntaEspacio_ContarPublicas",
                new Hashtable { { "@idEspacioArtistico", oEspacio.IdEspacioArtistico } }));
        }

        public List<PreguntaEspacio> ListarPendientesDeUsuario(EspacioArtistico oEspacio, UsuarioExterno oUsuario)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_PreguntaEspacio_ListarPendientesDeUsuario",
                new Hashtable
                {
                    { "@idEspacioArtistico", oEspacio.IdEspacioArtistico },
                    { "@idUsuarioExterno", oUsuario.IdUsuarioExterno }
                }));
        }

        public List<PreguntaEspacio> ListarPorGestor(UsuarioExterno oGestor, string estado, int? idEspacioArtistico)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_PreguntaEspacio_ListarPorGestor",
                new Hashtable
                {
                    { "@idUsuarioGestor", oGestor.IdUsuarioExterno },
                    { "@estado", (object)estado ?? DBNull.Value },
                    { "@idEspacioArtistico", idEspacioArtistico.HasValue ? (object)idEspacioArtistico.Value : DBNull.Value }
                }));
        }

        public int ContarPendientesGestor(UsuarioExterno oGestor)
        {
            return Entero(Conexion.Instance.LeerEscalar(
                "sp_PreguntaEspacio_ContarPendientesGestor",
                new Hashtable { { "@idUsuarioGestor", oGestor.IdUsuarioExterno } }));
        }


        public bool Responder(PreguntaEspacio oPregunta, int idUsuarioGestor)
        {
            return Entero(Conexion.Instance.LeerEscalar(
                "sp_PreguntaEspacio_Responder",
                new Hashtable
                {
                    { "@idPreguntaEspacio", oPregunta.IdPreguntaEspacio },
                    { "@idUsuarioGestor", idUsuarioGestor },
                    { "@respuesta", oPregunta.Respuesta }
                })) > 0;
        }


        public bool DarDeBaja(PreguntaEspacio oPregunta, string estadoAnterior, int idUsuarioBaja)
        {
            return Entero(Conexion.Instance.LeerEscalar(
                "sp_PreguntaEspacio_DarDeBaja",
                new Hashtable
                {
                    { "@idPreguntaEspacio", oPregunta.IdPreguntaEspacio },
                    { "@estadoAnterior", estadoAnterior },
                    { "@estadoNuevo", oPregunta.Estado },
                    { "@idUsuarioBaja", idUsuarioBaja },
                    { "@motivo", (object)oPregunta.MotivoOcultamiento ?? DBNull.Value }
                })) > 0;
        }

        private static int Entero(object valor)
        {
            return valor == null || valor == DBNull.Value ? 0 : Convert.ToInt32(valor);
        }

        private static List<PreguntaEspacio> Mapear(DataTable tabla)
        {
            List<PreguntaEspacio> lista = new List<PreguntaEspacio>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new PreguntaEspacio
                {
                    IdPreguntaEspacio = Convert.ToInt32(fila["idPreguntaEspacio"]),
                    IdEspacioArtistico = Convert.ToInt32(fila["idEspacioArtistico"]),
                    NombreEspacio = Convert.ToString(fila["nombreEspacio"]),
                    IdUsuarioGestorEspacio = Convert.ToInt32(fila["idUsuarioGestorEspacio"]),
                    IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                    NombreAutor = Convert.ToString(fila["nombreAutor"]),
                    ApellidoAutor = Convert.ToString(fila["apellidoAutor"]),
                    Pregunta = Convert.ToString(fila["pregunta"]),
                    FechaPregunta = Convert.ToDateTime(fila["fechaPregunta"]),
                    Respuesta = fila["respuesta"] == DBNull.Value ? null : Convert.ToString(fila["respuesta"]),
                    FechaRespuesta = fila["fechaRespuesta"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaRespuesta"]),
                    Estado = Convert.ToString(fila["estado"]),
                    MotivoOcultamiento = fila["motivoOcultamiento"] == DBNull.Value ? null : Convert.ToString(fila["motivoOcultamiento"]),
                    FechaBaja = fila["fechaBaja"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"])
                });
            }
            return lista;
        }
    }
}
