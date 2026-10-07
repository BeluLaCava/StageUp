using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    // Participante de las actividades internas de un gestor. No es un
    // usuario de StageUp: es un contacto propio del gestor (nombre,
    // apellido, DNI) que se asocia a una o más de sus actividades.
    public class Participante
    {
        public int IdParticipante { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }

        // CU-001-010 (script 58): contacto opcional.
        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }
        public DateTime? FechaBaja { get; set; }

        // Solo en el detalle: actividades vigentes e historial.
        public List<ParticipacionActividad> Actividades { get; set; } = new List<ParticipacionActividad>();

        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }
    }

    // Asociación de un participante con una actividad interna. Desvincular no
    // borra la asociación: la marca como inactiva (historial).
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
