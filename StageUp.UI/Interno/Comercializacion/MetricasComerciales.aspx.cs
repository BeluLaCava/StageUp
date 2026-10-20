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
            // Con período, el cálculo es por vigencia y estado actual (las
            // campañas no guardan historial de estados): se nombra así.
            bool conPeriodo = filtro.Desde.HasValue || filtro.Hasta.HasValue;
            litCampaniasTitulo.Text = conPeriodo ? "Campañas con vigencia en el período" : "Campañas comerciales activas";
            litCampaniasDetalle.Text = conPeriodo
                ? "Vigencia que se cruza con el período y estado actual Activa o Finalizada. No se cuentan las canceladas ni las planificadas, " +
                  "aunque hayan estado activas en ese período: no se guarda el historial de estados."
                : "Campañas en curso marcadas como activas";
            litConvertidos.Text = r.ContactosConvertidos.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));
            litConvertidosDetalle.Text = "Contactos incorporados que ya tienen una cuenta de StageUp" +
                (r.IncorporadosSinCuenta > 0
                    ? " · " + r.IncorporadosSinCuenta + (r.IncorporadosSinCuenta == 1 ? " incorporado" : " incorporados") + " todavía sin cuenta (no se cuentan)"
                    : string.Empty);
            MostrarFiltrosPorIndicador(filtro);

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

        // Qué filtros usa cada indicador (los que no le corresponden se dicen
        // explícitamente para que no parezca que se aplicaron).
        private void MostrarFiltrosPorIndicador(FiltroMetricasComerciales filtro)
        {
            bool periodo = filtro.Desde.HasValue || filtro.Hasta.HasValue;
            bool campania = filtro.IdCampaniaComercial.HasValue;
            bool canal = filtro.CanalOrigen != null;
            bool tipo = filtro.TipoUsuario != null;

            litFiltrosUsuarios.Text = TextoFiltros(periodo, "fecha de registro", campania, canal, tipo, true, true);
            litFiltrosEspacios.Text = ConNota(TextoFiltros(periodo, "fecha de alta del espacio", campania, canal, tipo, true, true),
                canal || tipo || campania ? "según el gestor que lo cargó" : null);
            litFiltrosCampanias.Text = TextoFiltros(periodo, "vigencia", campania, canal, tipo, false, false);
            litFiltrosConvertidos.Text = ConNota(TextoFiltros(periodo, "fecha de incorporación", campania, canal, tipo, true, true),
                tipo ? "el tipo es el de la cuenta asociada" : null);
        }

        private static string ConNota(string texto, string nota)
        {
            return nota == null || !texto.EndsWith(".", StringComparison.Ordinal)
                ? texto
                : texto.Substring(0, texto.Length - 1) + " (" + nota + ").";
        }

        private static string TextoFiltros(bool periodo, string fechaPeriodo, bool campania, bool canal, bool tipo,
            bool usaCanal, bool usaTipo)
        {
            List<string> aplica = new List<string>();
            List<string> noAplica = new List<string>();
            if (periodo) aplica.Add("período (" + fechaPeriodo + ")");
            if (campania) aplica.Add("campaña");
            if (canal) (usaCanal ? aplica : noAplica).Add("canal de origen");
            if (tipo) (usaTipo ? aplica : noAplica).Add("tipo de usuario");

            if (aplica.Count == 0 && noAplica.Count == 0)
            {
                return "Sin filtros: todo el historial.";
            }

            string texto = aplica.Count > 0 ? "Filtra por " + string.Join(", ", aplica) + "." : "No usa los filtros elegidos.";
            if (noAplica.Count > 0)
            {
                texto += " No depende del " + string.Join(" ni del ", noAplica) + ".";
            }

            return texto;
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
