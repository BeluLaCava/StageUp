using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{



    public class Actividad
    {
        public int IdActividad { get; set; }
        public int IdEspacioArtistico { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }


        public string ModoRecurrencia { get; set; }



        public string Fecha { get; set; }



        public int? SemanaDelMes { get; set; }

        public int? DiaSemanaMensual { get; set; }



        public List<int> DiasSemana { get; set; } = new List<int>();

        public int MinutoDesde { get; set; }
        public int MinutoHasta { get; set; }
        public int CupoMaximo { get; set; }
        public int? ParticipantesEstimados { get; set; }
        public string Notas { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }


        public string NombreEspacio { get; set; }


        public bool EspacioActivo { get; set; } = true;

        public int DuracionMinutos
        {
            get { return MinutoHasta - MinutoDesde; }
        }


        public List<Participante> Participantes { get; set; } = new List<Participante>();
    }
}
