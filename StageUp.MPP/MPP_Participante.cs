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
                    { "@correo", string.IsNullOrEmpty(participante.Correo) ? (object)DBNull.Value : participante.Correo },
                    { "@telefono", string.IsNullOrEmpty(participante.Telefono) ? (object)DBNull.Value : participante.Telefono },
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
                    { "@correo", string.IsNullOrEmpty(participante.Correo) ? (object)DBNull.Value : participante.Correo },
                    { "@telefono", string.IsNullOrEmpty(participante.Telefono) ? (object)DBNull.Value : participante.Telefono },
                    { "@notas", (object)participante.Notas ?? DBNull.Value }
                });
        }

        public void DarDeBaja(Participante participante)
        {
            Conexion.Instance.Guardar(
                "sp_Participante_DarDeBaja",
                new Hashtable { { "@idParticipante", participante.IdParticipante } });
        }

        public Participante ObtenerPorId(Participante participante)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Participante_ObtenerPorId",
                new Hashtable { { "@idParticipante", participante.IdParticipante } });

            return tabla.Rows.Count == 0 ? null : MapearFila(tabla.Rows[0]);
        }

        public List<Participante> ListarPorUsuarioGestor(UsuarioExterno usuarioGestor)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Participante_ListarPorUsuarioGestor",
                new Hashtable { { "@idUsuarioGestor", usuarioGestor.IdUsuarioExterno } });

            List<Participante> participantes = new List<Participante>();
            foreach (DataRow fila in tabla.Rows)
            {
                participantes.Add(MapearFila(fila));
            }

            return participantes;
        }

        // CU-001-010 (script 58): listado con filtro de estado y búsqueda.
        // estado: "Activos", "Inactivos" o "Todos".
        public List<Participante> Buscar(UsuarioExterno usuarioGestor, string estado, string texto)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Participante_Buscar",
                new Hashtable
                {
                    { "@idUsuarioGestor", usuarioGestor.IdUsuarioExterno },
                    { "@estado", string.IsNullOrEmpty(estado) ? "Activos" : estado },
                    { "@texto", string.IsNullOrEmpty(texto) ? (object)DBNull.Value : texto }
                });

            List<Participante> participantes = new List<Participante>();
            foreach (DataRow fila in tabla.Rows)
            {
                participantes.Add(MapearFila(fila));
            }

            return participantes;
        }

        // Otro participante activo del gestor con el mismo DNI o correo.
        // coincidencia: "Dni" o "Correo".
        public Participante BuscarDuplicado(Participante participante, out string coincidencia)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Participante_BuscarDuplicado",
                new Hashtable
                {
                    { "@idUsuarioGestor", participante.IdUsuarioGestor },
                    { "@dni", participante.Dni },
                    { "@correo", string.IsNullOrEmpty(participante.Correo) ? (object)DBNull.Value : participante.Correo },
                    { "@idParticipanteExcluido", participante.IdParticipante > 0 ? (object)participante.IdParticipante : DBNull.Value }
                });

            coincidencia = null;
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            coincidencia = tabla.Rows[0]["coincidencia"].ToString();
            return MapearFila(tabla.Rows[0]);
        }

        public List<ParticipacionActividad> ListarActividades(Participante participante)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ActividadParticipante_ListarPorParticipante",
                new Hashtable { { "@idParticipante", participante.IdParticipante } });

            List<ParticipacionActividad> actividades = new List<ParticipacionActividad>();
            foreach (DataRow fila in tabla.Rows)
            {
                actividades.Add(new ParticipacionActividad
                {
                    IdActividad = Convert.ToInt32(fila["idActividad"]),
                    NombreActividad = fila["nombre"].ToString(),
                    IdEspacioArtistico = Convert.ToInt32(fila["idEspacioArtistico"]),
                    NombreEspacio = fila.Table.Columns.Contains("nombreEspacio") ? Convert.ToString(fila["nombreEspacio"]) : null,
                    ActividadActiva = !fila.Table.Columns.Contains("actividadActiva") || Convert.ToBoolean(fila["actividadActiva"]),
                    AsociacionActiva = !fila.Table.Columns.Contains("asociacionActiva") || Convert.ToBoolean(fila["asociacionActiva"]),
                    FechaAsociacion = Convert.ToDateTime(fila["fechaAsociacion"]),
                    FechaDesvinculacion = fila.Table.Columns.Contains("fechaDesvinculacion") && fila["fechaDesvinculacion"] != DBNull.Value
                        ? Convert.ToDateTime(fila["fechaDesvinculacion"])
                        : (DateTime?)null
                });
            }

            return actividades;
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
                Correo = LeerTexto(fila, "correo"),
                Telefono = LeerTexto(fila, "telefono"),
                Notas = fila["notas"] == DBNull.Value ? null : fila["notas"].ToString(),
                Activo = Convert.ToBoolean(fila["activo"]),
                FechaCreacion = Convert.ToDateTime(fila["fechaCreacion"]),
                FechaUltimaModificacion = LeerFecha(fila, "fechaUltimaModificacion"),
                FechaBaja = LeerFecha(fila, "fechaBaja")
            };
        }

        private static string LeerTexto(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value ? fila[columna].ToString() : null;
        }

        private static DateTime? LeerFecha(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value ? Convert.ToDateTime(fila[columna]) : (DateTime?)null;
        }
    }
}
