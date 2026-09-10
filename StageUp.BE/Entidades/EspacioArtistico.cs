using System;
using System.Collections.Generic;

namespace StageUp.BE.Entidades
{
    public class EspacioArtistico
    {
        public FichaEspacio Ficha { get; set; } = new FichaEspacio();
        public int IdEspacioArtistico { get; set; }
        public int IdUsuarioGestor { get; set; }
        public string NombreEspacio { get; set; }
        public string Descripcion { get; set; }
        public string TipoEspacio { get; set; }
        public string EstadoEspacio { get; set; }
        public bool Publicado { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public DateTime? FechaBaja { get; set; }
        public DateTime? FechaUltimaModificacion { get; set; }

        // Datos del gestor para mostrar en el catálogo público y en el detalle del
        // espacio (tanda 4). Se completan solo cuando el espacio viene de una consulta
        // que hace JOIN con UsuarioExterno (catálogo/detalle público); en el resto de
        // los casos (por ejemplo "Mis espacios", donde el gestor ya es uno mismo)
        // quedan en su valor por defecto y no se muestran.
        public string NombreGestor { get; set; }
        public string ApellidoGestor { get; set; }
        public DateTime? GestorDesde { get; set; }
        public int CantidadEspaciosPublicadosGestor { get; set; }
        public decimal PromedioCalificacion { get; set; }
        public int CantidadCalificaciones { get; set; }

        public string NombreCompletoGestor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NombreGestor) && string.IsNullOrWhiteSpace(ApellidoGestor))
                    return null;
                return (NombreGestor + " " + ApellidoGestor).Trim();
            }
        }
    }

    public class FichaEspacio
    {
        // FotoRuta se mantiene por compatibilidad con espacios cargados antes de la
        // tanda de "varias fotografías" (ítem 3): si Fotos viene vacía, la UI cae a
        // esta única foto. Los espacios guardados con el formulario nuevo ya no la
        // usan como fuente de verdad — GuardarFicha la fija a Fotos[0] si hay alguna.
        public string FotoRuta { get; set; }
        public string Provincia { get; set; }
        public string Ciudad { get; set; }
        public string Direccion { get; set; }
        public int? CapacidadMaxima { get; set; }
        public decimal? PrecioHora { get; set; }
        public string Moneda { get; set; } = "ARS";
        public string TipoPiso { get; set; }
        public string DetalleEquipamiento { get; set; }
        public List<string> Equipamiento { get; set; } = new List<string>();
        public List<FranjaEspacio> Disponibilidad { get; set; } = new List<FranjaEspacio>();

        // Varias fotografías (ítem 3, tanda 10/09): rutas en el mismo formato que
        // FotoRuta (~/Content/Uploads/Espacios/<hash>.jpg), en el orden en que se
        // muestran. La primera de la lista es la portada/principal.
        public List<string> Fotos { get; set; } = new List<string>();
    }

    public class FranjaEspacio
    {
        public int? DiaSemana { get; set; }
        public string Fecha { get; set; }
        public int MinutoDesde { get; set; }
        public int MinutoHasta { get; set; }
        public bool Bloqueado { get; set; }
    }

    // Ítem 5 (filtros completos del catálogo): agrupa todos los criterios que
    // puede combinar una búsqueda en ResultadosBusqueda.aspx. Los campos vacíos
    // o en null significan "sin filtrar por esto" — BLL_EspacioArtistico.Buscar
    // sanitiza y valida cada uno antes de pasarlo a la MPP.
    public class FiltroBusquedaEspacios
    {
        public string TextoBusqueda { get; set; }
        public string TipoEspacio { get; set; }

        // Un solo campo de ubicación (la pantalla solo ofrece "Ciudad o zona"
        // en un único cuadro de texto): se busca tanto en Ciudad como en
        // Provincia de FichaEspacio.
        public string Ubicacion { get; set; }
        public decimal? PrecioMaximo { get; set; }
        public int? CapacidadMinima { get; set; }
        public string TipoPiso { get; set; }

        // Mismo formato "yyyy-MM-dd" que FranjaEspacio.Fecha. MinutoDesde/
        // MinutoHasta son opcionales incluso con fecha cargada: si no vienen,
        // solo se exige que el espacio tenga algún horario abierto ese día
        // (la pantalla hoy solo ofrece elegir una fecha, sin rango horario).
        public string FechaDisponibilidad { get; set; }
        public int? MinutoDesde { get; set; }
        public int? MinutoHasta { get; set; }

        // Códigos de FichaEspacioEquipamiento (ver permitidos en
        // BLL_EspacioArtistico.ValidarFicha). Selección múltiple: el espacio
        // tiene que tener TODOS los códigos pedidos, no cualquiera.
        public List<string> Equipamiento { get; set; } = new List<string>();
    }
}
