using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno
{




    public partial class DetalleEspacioInterno : Page
    {
        public const string PermisoRequerido = "BUSCAR_EN_PLATAFORMA";
        private const string PaginaBusqueda = "~/Interno/BusquedaInterna.aspx";

        private readonly BLL_EspacioArtistico _bllEspacio = new BLL_EspacioArtistico();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno(PermisoRequerido))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            string volver = UrlParaVolver();
            lnkVolver.NavigateUrl = volver;
            lnkVolverNoEncontrado.NavigateUrl = volver;

            if (IsPostBack)
            {
                return;
            }

            int idEspacio;
            if (!int.TryParse(Request.QueryString["id"], NumberStyles.Integer, CultureInfo.InvariantCulture, out idEspacio))
            {
                MostrarNoEncontrado("El espacio indicado no es válido.");
                return;
            }

            ResultadoOperacion<EspacioArtistico> resultado = _bllEspacio.ObtenerDetalleInterno(idEspacio);
            if (!resultado.Exitoso)
            {
                MostrarNoEncontrado(resultado.Mensaje);
                return;
            }

            Mostrar(resultado.Valor);
        }

        private void Mostrar(EspacioArtistico espacio)
        {
            FichaEspacio ficha = espacio.Ficha ?? new FichaEspacio();
            string estado = BLL_EspacioArtistico.EtiquetaEstado(espacio);
            bool esPublico = espacio.Activo && espacio.Publicado && estado == "Publicado";

            Title = espacio.NombreEspacio + " · Consulta interna | StageUp";
            litTipoEspacio.Text = Valor(espacio.TipoEspacio);
            litNombreEspacio.Text = espacio.NombreEspacio;
            lblEstado.Text = HttpUtility.HtmlEncode(estado);
            lblEstado.CssClass = "espacio-int-estado " + ClaseEstado(estado);

            litAvisoEstado.Text = esPublico
                ? "El espacio está publicado: también podés abrir la ficha que ven los usuarios."
                : estado == "Dado de baja"
                    ? "El espacio está dado de baja: no aparece en la plataforma y no recibe reservas."
                    : "El espacio no está publicado: los usuarios externos no lo ven. Esta consulta no lo publica.";

            lnkFichaPublica.Visible = esPublico;
            lnkFichaPublica.NavigateUrl = "~/Explorar/DetalleEspacio.aspx?id=" + espacio.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture);

            litGestor.Text = string.IsNullOrWhiteSpace(espacio.NombreCompletoGestor)
                ? "Gestor #" + espacio.IdUsuarioGestor.ToString(CultureInfo.InvariantCulture)
                : espacio.NombreCompletoGestor;
            litUbicacion.Text = Ubicacion(ficha);
            litDireccion.Text = Valor(ficha.Direccion);
            litCapacidad.Text = ficha.CapacidadMaxima.HasValue ? ficha.CapacidadMaxima.Value + " personas" : "—";
            litPrecio.Text = ficha.PrecioHora.HasValue
                ? (ficha.Moneda ?? "ARS") + " " + ficha.PrecioHora.Value.ToString("N2", CultureInfo.CurrentCulture)
                : "—";
            litPiso.Text = Valor(ficha.TipoPiso);
            litSuperficie.Text = ficha.SuperficieM2.HasValue ? ficha.SuperficieM2.Value.ToString("0.##", CultureInfo.CurrentCulture) + " m²" : "—";
            litAltura.Text = ficha.AlturaM.HasValue ? ficha.AlturaM.Value.ToString("0.##", CultureInfo.CurrentCulture) + " m" : "—";

            int franjas = (ficha.Disponibilidad ?? new List<FranjaEspacio>()).Count(f => f != null && !f.Bloqueado);
            litFranjas.Text = franjas == 0 ? "Sin franjas cargadas" : franjas == 1 ? "1 franja" : franjas + " franjas";

            List<string> equipamiento = (ficha.Equipamiento ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (!string.IsNullOrWhiteSpace(ficha.DetalleEquipamiento))
            {
                equipamiento.Add(ficha.DetalleEquipamiento.Trim());
            }
            litEquipamiento.Text = equipamiento.Count == 0 ? "—" : string.Join(" · ", equipamiento);

            litDescripcion.Text = Valor(espacio.Descripcion);
            litCondiciones.Text = Valor(ficha.CondicionesUso);
            litReglas.Text = Valor(ficha.ReglasUso);

            litFechaAlta.Text = Fecha(espacio.FechaAlta);
            litFechaPublicacion.Text = Fecha(espacio.FechaPublicacion);
            litFechaModificacion.Text = Fecha(espacio.FechaUltimaModificacion);
            litFechaBaja.Text = Fecha(espacio.FechaBaja);
        }

        private void MostrarNoEncontrado(string mensaje)
        {
            pnlDetalle.Visible = false;
            pnlNoEncontrado.Visible = true;
            litNoEncontrado.Text = mensaje;
        }



        private string UrlParaVolver()
        {
            string volver = Request.QueryString["volver"];
            if (string.IsNullOrWhiteSpace(volver) || volver.Length > 600)
            {
                return PaginaBusqueda;
            }

            FiltroBusquedaGlobal filtro = ConsultaBusqueda.Leer(HttpUtility.ParseQueryString(volver.TrimStart('?')));
            return ConsultaBusqueda.Armar(PaginaBusqueda, filtro);
        }

        private static string ClaseEstado(string estado)
        {
            switch (estado)
            {
                case "Publicado": return "espacio-int-estado-publicado";
                case "Pausado": return "espacio-int-estado-pausado";
                case "Borrador": return "espacio-int-estado-borrador";
                case "Dado de baja": return "espacio-int-estado-baja";
                default: return "espacio-int-estado-sinpublicar";
            }
        }

        private static string Ubicacion(FichaEspacio ficha)
        {
            List<string> partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(ficha.Ciudad)) partes.Add(ficha.Ciudad.Trim());
            if (!string.IsNullOrWhiteSpace(ficha.Provincia)) partes.Add(ficha.Provincia.Trim());
            return partes.Count == 0 ? "—" : string.Join(", ", partes);
        }

        private static string Valor(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? "—" : texto.Trim();
        }

        private static string Fecha(DateTime? fecha)
        {
            return fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "—";
        }
    }
}
