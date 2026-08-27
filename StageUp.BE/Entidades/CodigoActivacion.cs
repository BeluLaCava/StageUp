using System;

namespace StageUp.BE.Entidades
{
    /// <summary>
    /// Código de activación de cuenta enviado por correo electrónico,
    /// según CU-001-001. Cada código pertenece a un UsuarioExterno,
    /// tiene una vigencia definida y solo puede usarse una vez.
    /// </summary>
    public class CodigoActivacion
    {
        public int IdCodigoActivacion { get; set; }
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
