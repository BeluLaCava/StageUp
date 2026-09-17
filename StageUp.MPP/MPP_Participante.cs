using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Participante
    {
        public int Insertar(Participante participante)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Participante_Insertar",
                new Hashtable
                {
                    { "@idUsuarioGestor", participante.IdUsuarioGestor },
                    { "@nombre", participante.Nombre },
                    { "@apellido", participante.Apellido },
                    { "@dni", participante.Dni },
                    { "@notas", (object)participante.Notas ?? DBNull.Value }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(Participante participante)
        {
            Conexion.Instance.Guardar(
                "sp_Participante_Modificar",
                new Hashtable
                {
                    { "@idParticipante", participante.IdParticipante },
                    { "@nombre", participante.Nombre },
                    { "@apellido", participante.Apellido },
                    { "@dni", participante.Dni },
                    { "@notas", (object)participante.Notas ?? DBNull.Value }
                });
        }

        public void DarDeBaja(int idParticipante)
        {
            Conexion.Instance.Guardar(
                "sp_Participante_DarDeBaja",
                new Hashtable { { "@idParticipante", idParticipante } });
        }

        public Participante ObtenerPorId(int idParticipante)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Participante_ObtenerPorId",
                new Hashtable { { "@idParticipante", idParticipante } });

            return tabla.Rows.Count == 0 ? null : MapearFila(tabla.Rows[0]);
        }

        public List<Participante> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Participante_ListarPorUsuarioGestor",
                new Hashtable { { "@idUsuarioGestor", idUsuarioGestor } });

            List<Participante> participantes = new List<Participante>();
            foreach (DataRow fila in tabla.Rows)
            {
                participantes.Add(MapearFila(fila));
            }

            return participantes;
        }

        private static Participante MapearFila(DataRow fila)
        {
            return new Participante
            {
                IdParticipante = Convert.ToInt32(fila["idParticipante"]),
                IdUsuarioGestor = Convert.ToInt32(fila["idUsuarioGestor"]),
                Nombre = fila["nombre"].ToString(),
                Apellido = fila["apellido"].ToString(),
                Dni = fila["dni"].ToString(),
                Notas = fila["notas"] == DBNull.Value ? null : fila["notas"].ToString(),
                Activo = Convert.ToBoolean(fila["activo"]),
                FechaCreacion = Convert.ToDateTime(fila["fechaCreacion"])
            };
        }
    }
}
