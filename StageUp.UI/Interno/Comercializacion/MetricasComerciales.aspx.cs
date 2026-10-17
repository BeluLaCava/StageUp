using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Interno.Comercializacion
{
    // CU-004-004 Consultar métricas comerciales y origen de usuarios.
    //   MetricasComerciales.aspx?[desde, hasta, campania, canal, tipo]
    // Sin filtros muestra los indicadores generales (A3); los filtros viajan
    // en la query string (A4). Solo consulta.
    public partial class MetricasComerciales : PaginaComercial
    {
        private const string Pagina = "MetricasComerciales.aspx";

        private readonly BLL_MetricasComerciales _bll = new BLL_MetricasComerciales();
        private int _maximoBarra;

        protected override string PermisoRequerido
        {
            get { return BLL_MetricasComerciales.PermisoConsultar; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso() || IsPostBack)
            {
                return;
            }

            CargarFiltros();
            FiltroMetricasComerciales filtro = new FiltroMetricasComerciales
            {
                Desde = LeerFechaQuery("desde"),
                Hasta = LeerFechaQuery("hasta"),
                IdCampaniaComercial = LeerEntero("campania"),
                CanalOrigen = LeerTexto("canal"),
                TipoUsuario = LeerTexto("tipo")
            };

            txtDesde.Text = FechaParaCampo(filtro.Desde);
            txtHasta.Text = FechaParaCampo(filtro.Hasta);
            Seleccionar(ddlCampania, filtro.IdCampaniaComercial.HasValue ? filtro.IdCampaniaComercial.Value.ToString(CultureInfo.InvariantCulture) : null);
            Seleccionar(ddlCanal, filtro.CanalOrigen);
            Seleccionar(ddlTipoUsuario, filtro.TipoUsuario);
            CargarTablero(filtro);
        }

        private void CargarFiltros()
        {
            // Todas las campañas, también las finalizadas o dadas de baja (A9).
            ddlCampania.Items.Clear();
            ddlCampania.Items.Add(new ListItem("Todas", string.Empty));
            ResultadoOperacion<List<CampaniaComercial>> campanias = new BLL_CampaniaComercial()
                .Listar(new FiltroCampaniaComercial { Vista = FiltroCampaniaComercial.VistaTodas });
            if (campanias.Exitoso)
            {
                foreach (CampaniaComercial c in campanias.Valor.OrderBy(c => c.Nombre))
                {
                    ddlCampania.Items.Add(new ListItem(c.Nombre + (c.Activa ? string.Empty : " (dada de baja)"),
                        c.IdCampaniaComercial.ToString(CultureInfo.InvariantCulture)));
                }
            }

            CargarCatalogo(ddlCanal, ItemCatalogoComercial.CanalOrigen, "Todos");
            ddlCanal.Items.Add(new ListItem(BLL_MetricasComerciales.TextoNoInformado, FiltroMetricasComerciales.CanalNoInformado));
            CargarDiccionario(ddlTipoUsuario, BLL_MetricasComerciales.TiposUsuario, "Todos");
        }

        private void CargarTablero(FiltroMetricasComerciales filtro)
        {
            ResultadoOperacion<ResumenMetricasComerciales> resumen = _bll.ObtenerResumen(filtro);
            if (!resumen.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resumen.Mensaje, true);
                return;
            }

            ResumenMetricasComerciales r = resumen.Valor;
            litCriterios.Text = DescribirFiltro(filtro);

            // A2: la sección se puede abrir igual, sin indicadores.
            if (r.SinInformacion)
            {
                pnlSinInformacion.Visible = true;
                return;
            }

            // A5: hay información, pero nada coincide con los filtros.
            if (filtro.TieneCriterios && r.SinResultados)
            {
                pnlSinResultados.Visible = true;
                return;
            }

            pnlTablero.Visible = true;
            litNuevosUsuarios.Text = r.NuevosUsuarios.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));
            litConOrigen.Text = r.NuevosUsuarios == 0
                ? "Sin registros en el período"
                : r.UsuariosConOrigen + " con origen informado · " + r.UsuariosSinOrigen + " sin informar";
            litEspacios.Text = r.EspaciosIncorporados.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));
            litCampanias.Text = r.CampaniasActivas.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));
            litCampaniasDetalle.Text = filtro.Desde.HasValue || filtro.Hasta.HasValue
                ? "Activas en algún momento del período"
                : "Campañas en curso marcadas como activas";
            litConvertidos.Text = r.ContactosConvertidos.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));

            ResultadoOperacion<List<MetricaCanalOrigen>> canales = _bll.ListarPorCanal(filtro);
            if (!canales.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, canales.Mensaje, true);
                return;
            }

            // A7: hay usuarios, pero ninguno con origen identificado.
            pnlSinOrigen.Visible = r.NuevosUsuarios > 0 && r.UsuariosConOrigen == 0;
            _maximoBarra = canales.Valor.Count == 0 ? 0 : canales.Valor.Max(c => c.CantidadUsuarios);
            rptCanales.DataSource = canales.Valor;
            rptCanales.DataBind();

            rptOrigenes.DataSource = canales.Valor
                .SelectMany(c => c.Origenes.Select(o => new
                {
                    NombreCanal = c.NombreCanal,
                    Origen = o.NombreOrigen ?? BLL_MetricasComerciales.TextoNoInformado,
                    Vinculo = o.NombreCampania != null ? "Campaña: " + o.NombreCampania
                        : o.NombreAlianza != null ? "Alianza: " + o.NombreAlianza
                        : "—",
                    Usuarios = o.CantidadUsuarios,
                    Gestores = o.CantidadGestores
                }))
                .ToList();
            rptOrigenes.DataBind();
        }

        protected void btnAplicarFiltros_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            List<string> partes = new List<string>();
            Agregar(partes, "desde", txtDesde.Text);
            Agregar(partes, "hasta", txtHasta.Text);
            Agregar(partes, "campania", ddlCampania.SelectedValue);
            Agregar(partes, "canal", ddlCanal.SelectedValue);
            Agregar(partes, "tipo", ddlTipoUsuario.SelectedValue);
            Redirigir(ArmarUrl(Pagina, partes) + "#origen", null);
        }

        // Ancho de la barra relativo al canal con más usuarios (mínimo visible
        // si tiene al menos uno).
        protected string AnchoBarra(object cantidad)
        {
            int valor = Convert.ToInt32(cantidad, CultureInfo.InvariantCulture);
            if (_maximoBarra <= 0 || valor <= 0)
            {
                return "0%";
            }

            decimal porcentaje = Math.Max(2m, valor * 100m / _maximoBarra);
            return porcentaje.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private string DescribirFiltro(FiltroMetricasComerciales filtro)
        {
            if (!filtro.TieneCriterios)
            {
                return "Mostrando los indicadores generales (todo el historial).";
            }

            List<string> partes = new List<string>();
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue)
            {
                partes.Add("del " + filtro.Desde.Value.ToString("dd/MM/yyyy") + " al " + filtro.Hasta.Value.ToString("dd/MM/yyyy"));
            }
            else if (filtro.Desde.HasValue)
            {
                partes.Add("desde el " + filtro.Desde.Value.ToString("dd/MM/yyyy"));
            }
            else if (filtro.Hasta.HasValue)
            {
                partes.Add("hasta el " + filtro.Hasta.Value.ToString("dd/MM/yyyy"));
            }

            if (filtro.IdCampaniaComercial.HasValue && ddlCampania.SelectedItem != null)
            {
                partes.Add("campaña " + ddlCampania.SelectedItem.Text);
            }

            if (filtro.CanalOrigen != null)
            {
                partes.Add("canal " + _bll.NombreCanal(filtro.CanalOrigen).ToLowerInvariant());
            }

            if (filtro.TipoUsuario != null)
            {
                partes.Add(BLL_MetricasComerciales.TiposUsuario[filtro.TipoUsuario].ToLowerInvariant());
            }

            return "Filtrando por: " + string.Join(" · ", partes) + ".";
        }
    }
}
