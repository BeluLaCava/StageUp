namespace StageUp.BE.Entidades
{


    public class ItemCatalogoComercial
    {
        public const string PublicoObjetivo = "PublicoObjetivo";
        public const string CanalDifusion = "CanalDifusion";
        public const string TipoEntidad = "TipoEntidad";
        public const string MedioContacto = "MedioContacto";
        public const string TipoInteraccion = "TipoInteraccion";
        public const string TipoInstitucion = "TipoInstitucion";
        public const string TipoContenido = "TipoContenido";
        public const string CanalUso = "CanalUso";
        public const string CanalOrigen = "CanalOrigen";

        public string TipoCatalogo { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int Orden { get; set; }
    }



    public class OpcionVinculoComercial
    {
        public const string TipoCampania = "Campania";
        public const string TipoAlianza = "Alianza";
        public const string TipoContacto = "Contacto";
        public const string TipoContenido = "Contenido";
        public const string TipoOrigen = "Origen";


        public string Tipo { get; set; }
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public string Detalle { get; set; }
        public int? IdCampaniaActual { get; set; }
    }


    public class VinculoComercial
    {
        public string Tipo { get; set; }
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public int? Cantidad { get; set; }
    }
}
