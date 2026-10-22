using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{


    public class SuscriptorNewsletter
    {
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoActivo = "Activo";
        public const string EstadoBaja = "Baja";
        public const string OrigenFormulario = "Formulario";
        public const string OrigenCuenta = "Cuenta";

        public int IdSuscriptorNewsletter { get; set; }
        public string CorreoElectronico { get; set; }
        public string Nombre { get; set; }
        public int? IdUsuarioExterno { get; set; }
        public string Estado { get; set; }
        public string Origen { get; set; }
        public DateTime? VencimientoConfirmacion { get; set; }
        public DateTime? FechaUltimoEnvioConfirmacion { get; set; }
        public string TokenPreferencias { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaConfirmacion { get; set; }
        public DateTime? FechaBaja { get; set; }



        public int? IdUsuarioExternoVinculoPendiente { get; set; }
        public DateTime? VencimientoVinculo { get; set; }


        public List<string> Categorias { get; set; } = new List<string>();


        public int? IdCuenta { get; set; }
        public string PerfilCuenta { get; set; }

        public bool TieneCuenta
        {
            get { return IdCuenta.HasValue; }
        }


        public string CorreoEnmascarado
        {
            get
            {
                string correo = CorreoElectronico ?? string.Empty;
                int arroba = correo.IndexOf('@');
                if (arroba <= 0)
                {
                    return "***";
                }

                string usuario = correo.Substring(0, arroba);
                return usuario.Substring(0, Math.Min(2, usuario.Length)) + "***" + correo.Substring(arroba);
            }
        }
    }


    public class DestinatarioNewsletter
    {
        public int IdSuscriptorNewsletter { get; set; }
        public string CorreoElectronico { get; set; }
        public string Nombre { get; set; }
        public string TokenPreferencias { get; set; }
    }


    public class SuscripcionDeCuenta
    {
        public string CorreoElectronico { get; set; }


        public SuscriptorNewsletter Suscripcion { get; set; }



        public bool Vinculada { get; set; }

        public string Estado
        {
            get { return Suscripcion == null ? null : Suscripcion.Estado; }
        }
    }


    public class FiltroSuscriptoresNewsletter
    {
        public string Texto { get; set; }
        public string Estado { get; set; }
        public bool? ConCuenta { get; set; }
        public int Maximo { get; set; } = 200;
    }

    public class ResumenSuscriptoresNewsletter
    {
        public int Activos { get; set; }
        public int Pendientes { get; set; }
        public int Bajas { get; set; }
        public int ActivosSinCuenta { get; set; }
    }
}
