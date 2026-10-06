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
            return ListarPorUsuarioGestor(usuarioGestor, "Activas", null);
        }

        // CU-001-009 / ítem 36 (script 57): actividades del gestor con sus días
        // y participantes en tres consultas en total, sin importar cuántas
        // actividades haya (antes eran dos consultas extra por actividad).
        // estado: "Activas", "Inactivas" o "Todas".
        public List<Actividad> ListarPorUsuarioGestor(UsuarioExterno usuarioGestor, string estado, int? idEspacioArtistico)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Actividad_ListarPorUsuarioGestorV2",
                new Hashtable
                {
                    { "@idUsuarioGestor", usuarioGestor.IdUsuarioExterno },
                    { "@estado", string.IsNullOrEmpty(estado) ? "Activas" : estado },
                    { "@idEspacioArtistico", idEspacioArtistico.HasValue ? (object)idEspacioArtistico.Value : DBNull.Value }
                });

            List<Actividad> actividades = new List<Actividad>();
            Dictionary<int, Actividad> porId = new Dictionary<int, Actividad>();
            foreach (DataRow fila in tabla.Rows)
            {
                Actividad actividad = MapearFila(fila);
                actividades.Add(actividad);
                porId[actividad.IdActividad] = actividad;
            }

            if (actividades.Count == 0)
            {
                return actividades;
            }

            Hashtable parametrosGestor = new Hashtable { { "@idUsuarioGestor", usuarioGestor.IdUsuarioExterno } };
            foreach (DataRow fila in Conexion.Instance.Leer("sp_ActividadDiaSemana_ListarPorUsuarioGestor", parametrosGestor).Rows)
            {
                Actividad actividad;
                if (porId.TryGetValue(Convert.ToInt32(fila["idActividad"]), out actividad))
                {
                    actividad.DiasSemana.Add(Convert.ToInt32(fila["diaSemana"]));
                }
            }

            parametrosGestor = new Hashtable { { "@idUsuarioGestor", usuarioGestor.IdUsuarioExterno } };
            foreach (DataRow fila in Conexion.Instance.Leer("sp_ActividadParticipante_ListarPorUsuarioGestor", parametrosGestor).Rows)
            {
                Actividad actividad;
                if (porId.TryGetValue(Convert.ToInt32(fila["idActividad"]), out actividad))
                {
                    actividad.Participantes.Add(MapearParticipante(fila));
                }
            }

            return actividades;
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
                    : null,
                EspacioActivo = !fila.Table.Columns.Contains("espacioActivo") || fila["espacioActivo"] == DBNull.Value ||
                    Convert.ToBoolean(fila["espacioActivo"])
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
                participantes.Add(MapearParticipante(fila));
            }

            return participantes;
        }

        private static Participante MapearParticipante(DataRow fila)
        {
            return new Participante
            {
                IdParticipante = Convert.ToInt32(fila["idParticipante"]),
                Nombre = fila["nombre"].ToString(),
                Apellido = fila["apellido"].ToString(),
                Dni = fila["dni"].ToString(),
                Notas = fila["notas"] == DBNull.Value ? null : fila["notas"].ToString(),
                Activo = Convert.ToBoolean(fila["activo"])
            };
        }
    }
}
