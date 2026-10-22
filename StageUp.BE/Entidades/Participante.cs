using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{



    public class Participante
    {
        public int IdParticipante { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }


        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public DateTime? FechaBaja { get; set; }


        public List<ParticipacionActividad> Actividades { get; set; } = new List<ParticipacionActividad>();

        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }
    }



    public class ParticipacionActividad
    {
        public int IdActividad { get; set; }
        public string NombreActividad { get; set; }
        public int IdEspacioArtistico { get; set; }
        public string NombreEspacio { get; set; }
        public bool ActividadActiva { get; set; }
        public bool AsociacionActiva { get; set; }
        public DateTime FechaAsociacion { get; set; }
        public DateTime? FechaDesvinculacion { get; set; }

        public bool Vigente
        {
            get { return ActividadActiva && AsociacionActiva; }
        }
    }
}
