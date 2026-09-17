using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // Actividad interna de un espacio (clase, taller, ensayo). Al guardarse,
    // bloquea automáticamente la disponibilidad del espacio para reservas
    // externas en los horarios que le correspondan según su recurrencia.
    public class Actividad
    {
        public int IdActividad { get; set; }
        public int IdEspacioArtistico { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }

        // "Semanal" | "Mensual" | "Fecha" (ver StageUp.BE.Enumerados.ModoRecurrenciaActividad).
        public string ModoRecurrencia { get; set; }

        // Usado solo cuando ModoRecurrencia = "Fecha". Formato yyyy-MM-dd,
        // mismo criterio que FranjaEspacio.Fecha.
        public string Fecha { get; set; }

        // Usados solo cuando ModoRecurrencia = "Mensual".
        // SemanaDelMes: 1 a 4 = primera..cuarta semana, 5 = última.
        public int? SemanaDelMes { get; set; }
        // DiaSemanaMensual: 1 = lunes ... 7 = domingo (mismo criterio que FranjaEspacio.DiaSemana).
        public int? DiaSemanaMensual { get; set; }

        // Usado solo cuando ModoRecurrencia = "Semanal". Una actividad puede
        // repetirse en más de un día (ej: lunes y miércoles).
        public List<int> DiasSemana { get; set; } = new List<int>();

        public int MinutoDesde { get; set; }
        public int MinutoHasta { get; set; }
        public int CupoMaximo { get; set; }
        public int? ParticipantesEstimados { get; set; }
        public string Notas { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }

        // Solo lectura, completado por los listados por gestor.
        public string NombreEspacio { get; set; }

        // Participantes asociados; se completa aparte, no viaja en el alta/modificación básica.
        public List<Participante> Participantes { get; set; } = new List<Participante>();
    }
}
