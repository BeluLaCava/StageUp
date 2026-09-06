using System;
using System.Collections.Generic;
using System.Web.Services;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Servicios
{
    /// <summary>
    /// Web Service SOAP (ASMX, la tecnología clásica de ASP.NET Web Forms
    /// sobre .NET Framework) que expone el catálogo público de espacios
    /// artísticos (CU-001-006) para que un sistema externo pueda consultar
    /// espacios publicados de StageUp sin pasar por la interfaz web.
    ///
    /// Reutiliza exactamente la misma lógica de negocio que ya usan
    /// Explorar/ResultadosBusqueda.aspx y Explorar/DetalleEspacio.aspx
    /// (BLL_EspacioArtistico), así que no duplica reglas ni introduce
    /// comportamiento nuevo: es la misma búsqueda simple y el mismo
    /// detalle público, expuestos además como Web Service.
    /// </summary>
    [WebService(Namespace = "http://stageup.local/servicios/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class ServicioEspacios : WebService
    {
        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

        [WebMethod(Description =
            "Busca espacios artísticos publicados y activos por un término de texto libre " +
            "(contra nombre, tipo y descripción). Dejar vacío o nulo para listar todos los " +
            "espacios publicados.")]
        public EspacioPublicadoInfo[] BuscarEspaciosPublicados(string textoBusqueda)
        {
            List<EspacioArtistico> espacios = _bllEspacio.ListarPublicados(textoBusqueda);
            return espacios.ConvertAll(MapearAInfo).ToArray();
        }

        [WebMethod(Description =
            "Obtiene el detalle de un espacio publicado a partir de su Id. Devuelve null si " +
            "el espacio no existe, no está publicado o no está activo (mismo criterio que " +
            "DetalleEspacio.aspx).")]
        public EspacioPublicadoInfo ObtenerDetalleEspacioPublicado(int idEspacioArtistico)
        {
            EspacioArtistico espacio = _bllEspacio.ObtenerDetallePublicado(idEspacioArtistico);
            return espacio == null ? null : MapearAInfo(espacio);
        }

        private static EspacioPublicadoInfo MapearAInfo(EspacioArtistico espacio)
        {
            return new EspacioPublicadoInfo
            {
                IdEspacioArtistico = espacio.IdEspacioArtistico,
                NombreEspacio = espacio.NombreEspacio,
                TipoEspacio = espacio.TipoEspacio,
                Descripcion = espacio.Descripcion,
                FechaPublicacion = espacio.FechaPublicacion
            };
        }
    }

    /// <summary>
    /// DTO público y serializable por SOAP para los métodos de
    /// ServicioEspacios. Expone deliberadamente solo los datos que ya son
    /// públicos en el catálogo (ResultadosBusqueda.aspx / DetalleEspacio.aspx);
    /// no expone columnas internas de EspacioArtistico como IdUsuarioGestor,
    /// EstadoEspacio, Activo, Publicado, FechaAlta, FechaBaja o
    /// FechaUltimaModificacion.
    /// </summary>
    public class EspacioPublicadoInfo
    {
        public int IdEspacioArtistico { get; set; }
        public string NombreEspacio { get; set; }
        public string TipoEspacio { get; set; }
        public string Descripcion { get; set; }
        public DateTime? FechaPublicacion { get; set; }
    }
}
