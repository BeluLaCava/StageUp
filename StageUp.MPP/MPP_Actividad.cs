using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Actividad
    {
        public int Insertar(Actividad actividad)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Actividad_Insertar",
                new Hashtable
                {
                    { "@idEspacioArtistico", actividad.IdEspacioArtistico },
                    { "@nombre", actividad.Nombre },
                    { "@tipo", (object)actividad.Tipo ?? DBNull.Value },
                    { "@modoRecurrencia", actividad.ModoRecurrencia },
                    { "@fecha", ParsearFecha(actividad.Fecha) },
                    { "@semanaDelMes", (object)actividad.SemanaDelMes ?? DBNull.Value },
                    { "@diaSemanaMensual", (object)actividad.DiaSemanaMensual ?? DBNull.Value },
                    { "@minutoDesde", actividad.MinutoDesde },
                    { "@minutoHasta", actividad.MinutoHasta },
                    { "@cupoMaximo", actividad.CupoMaximo },
                    { "@participantesEstimados", (object)actividad.ParticipantesEstimados ?? DBNull.Value },
                    { "@notas", (object)actividad.Notas ?? DBNull.Value }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(Actividad actividad)
        {
            Conexion.Instance.Guardar(
                "sp_Actividad_Modificar",
                new Hashtable
                {
                    { "@idActividad", actividad.IdActividad },
                    { "@nombre", actividad.Nombre },
                    { "@tipo", (object)actividad.Tipo ?? DBNull.Value },
                    { "@modoRecurrencia", actividad.ModoRecurrencia },
                    { "@fecha", ParsearFecha(actividad.Fecha) },
                    { "@semanaDelMes", (object)actividad.SemanaDelMes ?? DBNull.Value },
                    { "@diaSemanaMensual", (object)actividad.DiaSemanaMensual ?? DBNull.Value },
                    { "@minutoDesde", actividad.MinutoDesde },
                    { "@minutoHasta", actividad.MinutoHasta },
                    { "@cupoMaximo", actividad.CupoMaximo },
                    { "@participantesEstimados", (object)actividad.ParticipantesEstimados ?? DBNull.Value },
                    { "@notas", (object)actividad.Notas ?? DBNull.Value }
                });
        }

        public void DarDeBaja(Actividad actividad)
        {
            Conexion.Instance.Guardar(
                "sp_Actividad_DarDeBaja",
                new Hashtable { { "@idActividad", actividad.IdActividad } });
        }

        public Actividad ObtenerPorId(Actividad actividad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Actividad_ObtenerPorId",
                new Hashtable { { "@idActividad", actividad.IdActividad } });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            Actividad encontrada = MapearFila(tabla.Rows[0]);
            encontrada.DiasSemana = ListarDiasSemana(encontrada);
            return encontrada;
        }

        public List<Actividad> ListarPorEspacio(EspacioArtistico espacio)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Actividad_ListarPorEspacio",
                new Hashtable { { "@idEspacioArtistico", espacio.IdEspacioArtistico } });

            return MapearFilas(tabla);
        }

        public List<Actividad> ListarPorUsuarioGestor(UsuarioExterno usuarioGestor)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Actividad_ListarPorUsuarioGestor",
                new Hashtable { { "@idUsuarioGestor", usuarioGestor.IdUsuarioExterno } });

            return MapearFilas(tabla);
        }

        private List<Actividad> MapearFilas(DataTable tabla)
        {
            List<Actividad> actividades = new List<Actividad>();
            foreach (DataRow fila in tabla.Rows)
            {
                Actividad actividad = MapearFila(fila);
                actividad.DiasSemana = ListarDiasSemana(actividad);
                actividades.Add(actividad);
            }

            return actividades;
        }

        private static Actividad MapearFila(DataRow fila)
        {
            return new Actividad
            {
                IdActividad = Convert.ToInt32(fila["idActividad"]),
                IdEspacioArtistico = Convert.ToInt32(fila["idEspacioArtistico"]),
                Nombre = fila["nombre"].ToString(),
                Tipo = fila["tipo"] == DBNull.Value ? null : fila["tipo"].ToString(),
                ModoRecurrencia = fila["modoRecurrencia"].ToString(),
                Fecha = fila["fecha"] == DBNull.Value ? null : Convert.ToDateTime(fila["fecha"]).ToString("yyyy-MM-dd"),
                SemanaDelMes = fila["semanaDelMes"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["semanaDelMes"]),
                DiaSemanaMensual = fila["diaSemanaMensual"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["diaSemanaMensual"]),
                MinutoDesde = Convert.ToInt32(fila["minutoDesde"]),
                MinutoHasta = Convert.ToInt32(fila["minutoHasta"]),
                CupoMaximo = Convert.ToInt32(fila["cupoMaximo"]),
                ParticipantesEstimados = fila["participantesEstimados"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["participantesEstimados"]),
                Notas = fila["notas"] == DBNull.Value ? null : fila["notas"].ToString(),
                Activa = Convert.ToBoolean(fila["activa"]),
                FechaCreacion = Convert.ToDateTime(fila["fechaCreacion"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                NombreEspacio = fila.Table.Columns.Contains("nombreEspacio") && fila["nombreEspacio"] != DBNull.Value
                    ? fila["nombreEspacio"].ToString()
                    : null
            };
        }

        private static object ParsearFecha(string fecha)
        {
            return string.IsNullOrEmpty(fecha)
                ? (object)DBNull.Value
                : DateTime.ParseExact(fecha, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---- Días de semana (recurrencia "Semanal") ----

        public void InsertarDiaSemana(Actividad actividad, int diaSemana)
        {
            Conexion.Instance.Guardar(
                "sp_ActividadDiaSemana_Insertar",
                new Hashtable { { "@idActividad", actividad.IdActividad }, { "@diaSemana", diaSemana } });
        }

        public void EliminarDiasSemana(Actividad actividad)
        {
            Conexion.Instance.Guardar(
                "sp_ActividadDiaSemana_EliminarPorActividad",
                new Hashtable { { "@idActividad", actividad.IdActividad } });
        }

        public List<int> ListarDiasSemana(Actividad actividad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ActividadDiaSemana_ListarPorActividad",
                new Hashtable { { "@idActividad", actividad.IdActividad } });

            List<int> dias = new List<int>();
            foreach (DataRow fila in tabla.Rows)
            {
                dias.Add(Convert.ToInt32(fila["diaSemana"]));
            }

            return dias;
        }

        // ---- Franjas bloqueadas generadas por la actividad ----

        public void InsertarFranjaDesdeActividad(Actividad actividad, FranjaEspacio franja)
        {
            Conexion.Instance.Guardar(
                "sp_FranjaEspacio_InsertarDesdeActividad",
                new Hashtable
                {
                    { "@idEspacioArtistico", actividad.IdEspacioArtistico },
                    { "@idActividad", actividad.IdActividad },
                    { "@diaSemana", (object)franja.DiaSemana ?? DBNull.Value },
                    { "@fecha", ParsearFecha(franja.Fecha) },
                    { "@minutoDesde", franja.MinutoDesde },
                    { "@minutoHasta", franja.MinutoHasta }
                });
        }

        public void EliminarFranjasPorActividad(Actividad actividad)
        {
            Conexion.Instance.Guardar(
                "sp_FranjaEspacio_EliminarPorActividad",
                new Hashtable { { "@idActividad", actividad.IdActividad } });
        }

        // ---- Participantes asociados a la actividad ----

        public void AsociarParticipante(Actividad actividad, Participante participante)
        {
            Conexion.Instance.Guardar(
                "sp_ActividadParticipante_Insertar",
                new Hashtable { { "@idActividad", actividad.IdActividad }, { "@idParticipante", participante.IdParticipante } });
        }

        public void DesasociarParticipante(Actividad actividad, Participante participante)
        {
            Conexion.Instance.Guardar(
                "sp_ActividadParticipante_Eliminar",
                new Hashtable { { "@idActividad", actividad.IdActividad }, { "@idParticipante", participante.IdParticipante } });
        }

        public List<Participante> ListarParticipantesDeActividad(Actividad actividad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ActividadParticipante_ListarPorActividad",
                new Hashtable { { "@idActividad", actividad.IdActividad } });

            List<Participante> participantes = new List<Participante>();
            foreach (DataRow fila in tabla.Rows)
            {
                participantes.Add(new Participante
                {
                    IdParticipante = Convert.ToInt32(fila["idParticipante"]),
                    Nombre = fila["nombre"].ToString(),
                    Apellido = fila["apellido"].ToString(),
                    Dni = fila["dni"].ToString(),
                    Notas = fila["notas"] == DBNull.Value ? null : fila["notas"].ToString(),
                    Activo = Convert.ToBoolean(fila["activo"])
                });
            }

            return participantes;
        }
    }
}
