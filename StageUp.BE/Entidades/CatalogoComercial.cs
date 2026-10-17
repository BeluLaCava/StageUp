namespace StageUp.BE.Entidades
{
    // Valor de un catálogo de la gestión comercial (público objetivo, canal
    // de difusión, tipo de entidad, etc.). Ver Database/80_Comercializacion.sql.
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

    // Elemento que se puede asociar desde un formulario comercial (campaña,
    // alianza, contacto, contenido u origen).
    public class OpcionVinculoComercial
    {
        public const string TipoCampania = "Campania";
        public const string TipoAlianza = "Alianza";
        public const string TipoContacto = "Contacto";
        public const string TipoContenido = "Contenido";
        public const string TipoOrigen = "Origen";

        // Campania, Alianza, Contacto, Contenido u Origen.
        public string Tipo { get; set; }
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public string Detalle { get; set; }
        public int? IdCampaniaActual { get; set; }
    }

    // Vínculo registrado de una campaña o alianza (para su detalle).
    public class VinculoComercial
    {
        public string Tipo { get; set; }
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public int? Cantidad { get; set; }
    }
}
