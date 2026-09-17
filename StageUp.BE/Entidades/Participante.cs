using System;

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
        public string Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }
    }
}
