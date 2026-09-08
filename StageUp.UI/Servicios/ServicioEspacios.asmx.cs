using System;
using System.Collections.Generic;
using System.Web.Services;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Servicios
{
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

    public class EspacioPublicadoInfo
    {
        public int IdEspacioArtistico { get; set; }
        public string NombreEspacio { get; set; }
        public string TipoEspacio { get; set; }
        public string Descripcion { get; set; }
        public DateTime? FechaPublicacion { get; set; }
    }
}
