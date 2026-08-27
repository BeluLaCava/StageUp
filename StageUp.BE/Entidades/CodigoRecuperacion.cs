using System;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Código de recuperación de contraseña, según CU-001-002.
    /// Vigencia de 15 minutos según la especificación del caso de uso.
    /// </summary>
    public class CodigoRecuperacion
    {
        public int IdCodigoRecuperacion { get; set; }
        public int IdUsuarioExterno { get; set; }
        public string Codigo { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public bool Utilizado { get; set; }
        public DateTime? FechaUtilizacion { get; set; }

        public bool EstaVigente(DateTime momento)
        {
            return !Utilizado && momento <= FechaVencimiento;
        }
    }
}
