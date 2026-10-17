using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;

namespace StageUp.UI.Interno.Comercializacion
{
    // CU-004-001 Gestionar campañas comerciales, más los orígenes de usuario.
    //   CampaniasComerciales.aspx?[filtros]           -> listado (pasos 2 a 7, A2 a A4)
    //   CampaniasComerciales.aspx?ver={id}            -> detalle (A5, A7, A8, A9)
    //   CampaniasComerciales.aspx?nuevo=1             -> alta (pasos 8 a 26)
    //   CampaniasComerciales.aspx?editar={id}         -> edición (A6)
    //   CampaniasComerciales.aspx?vista=origenes[&origen={id|nuevo}] -> orígenes de usuario
    // Los filtros viajan en la query string: vista, estado, canal, publico,
    // desde, hasta, q.
    public partial class CampaniasComerciales : PaginaComercial
    {
        private const string Pagina = "CampaniasComerciales.aspx";

        private readonly BLL_CampaniaComercial _bll = new BLL_CampaniaComercial();
        private readonly BLL_OrigenUsuario _bllOrigen = new BLL_OrigenUsuario();

        protected override string PermisoRequerido
        {
            get { return BLL_CampaniaComercial.PermisoGestionar; }
        }

        private bool VistaOrigenes
        {
            get { return string.Equals(Request.QueryString["vista"], "origenes", StringComparison.OrdinalIgnoreCase); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (IsPostBack)
            {
                return;
            }

            MostrarMensajePendiente(pnlMensaje, litMensaje);

            if (VistaOrigenes)
            {
                string origen = Request.QueryString["origen"];
                if (origen == "nuevo" || LeerEntero("origen").HasValue)
                {
                    CargarFormularioOrigen(LeerEntero("origen"));
                }
                else
                {
                    CargarOrigenes();
                }
            }
            else if (Request.QueryString["nuevo"] == "1")
            {
                CargarFormulario(null);
            }
            else if (LeerEntero("editar").HasValue)
            {
                CargarFormulario(LeerEntero("editar").Value);
            }
            else if (LeerEntero("ver").HasValue)
            {
                CargarDetalle(LeerEntero("ver").Value);
            }
            else
            {
                CargarListado();
            }
        }

        // ------------------------------------------------------------------
        // Listado (pasos 3 a 7, A2 a A4)
        // ------------------------------------------------------------------

        private void CargarListado()
        {
            pnlListado.Visible = true;
            CargarDiccionario(ddlFiltroEstado, BLL_CampaniaComercial.Estados, "Todos");
            CargarCatalogo(ddlFiltroCanal, ItemCatalogoComercial.CanalDifusion, "Todos");
            CargarCatalogo(ddlFiltroPublico, ItemCatalogoComercial.PublicoObjetivo, "Todos");

            FiltroCampaniaComercial filtro = new FiltroCampaniaComercial
            {
                Vista = LeerTexto("vista") ?? FiltroCampaniaComercial.VistaActivas,
                Estado = LeerTexto("estado"),
                CanalDifusion = LeerTexto("canal"),
                Segmento = LeerTexto("publico"),
                Desde = LeerFechaQuery("desde"),
                Hasta = LeerFechaQuery("hasta"),
                Texto = LeerTexto("q")
            };

            Seleccionar(ddlFiltroVista, filtro.Vista);
            Seleccionar(ddlFiltroEstado, filtro.Estado);
            Seleccionar(ddlFiltroCanal, filtro.CanalDifusion);
            Seleccionar(ddlFiltroPublico, filtro.Segmento);
            txtFiltroDesde.Text = FechaParaCampo(filtro.Desde);
            txtFiltroHasta.Text = FechaParaCampo(filtro.Hasta);
            txtFiltroTexto.Text = filtro.Texto;

            ResultadoOperacion<ResumenCampanias> resumen = _bll.ObtenerResumen();
            if (!resumen.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resumen.Mensaje, true);
                rptCampanias.Visible = false;
                return;
            }

            litActivas.Text = resumen.Valor.Activas.ToString(CultureInfo.InvariantCulture);
            litBajas.Text = resumen.Valor.Bajas.ToString(CultureInfo.InvariantCulture);

            // A2: no hay ninguna campaña.
            if (resumen.Valor.Total == 0)
            {
                litEnCurso.Text = "0";
                litPlanificadas.Text = "0";
                pnlSinCampanias.Visible = true;
                pnlFiltros.Visible = false;
                rptCampanias.Visible = false;
                return;
            }

            ResultadoOperacion<List<CampaniaComercial>> enGestion = _bll.Listar(new FiltroCampaniaComercial());
            if (enGestion.Exitoso)
            {
                litEnCurso.Text = enGestion.Valor.Count(c => c.EstadoCampania == CampaniaComercial.EstadoActiva).ToString(CultureInfo.InvariantCulture);
                litPlanificadas.Text = enGestion.Valor.Count(c => c.EstadoCampania == CampaniaComercial.EstadoPlanificada).ToString(CultureInfo.InvariantCulture);
            }

            ResultadoOperacion<List<CampaniaComercial>> resultado = _bll.Listar(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                rptCampanias.Visible = false;
                return;
            }

            // A4: hay campañas, pero ninguna cumple los filtros.
            pnlSinResultados.Visible = resultado.Valor.Count == 0;
            rptCampanias.Visible = resultado.Valor.Count > 0;
            litCantidad.Text = resultado.Valor.Count == 0
                ? string.Empty
                : resultado.Valor.Count + (resultado.Valor.Count == 1 ? " campaña" : " campañas") + (filtro.TieneCriterios ? " para los filtros aplicados." : ".");
            rptCampanias.DataSource = resultado.Valor;
            rptCampanias.DataBind();
        }

        protected void btnAplicarFiltros_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            List<string> partes = new List<string>();
            if (ddlFiltroVista.SelectedValue != FiltroCampaniaComercial.VistaActivas)
            {
                Agregar(partes, "vista", ddlFiltroVista.SelectedValue);
            }

            Agregar(partes, "estado", ddlFiltroEstado.SelectedValue);
            Agregar(partes, "canal", ddlFiltroCanal.SelectedValue);
            Agregar(partes, "publico", ddlFiltroPublico.SelectedValue);
            Agregar(partes, "desde", txtFiltroDesde.Text);
            Agregar(partes, "hasta", txtFiltroHasta.Text);
            Agregar(partes, "q", txtFiltroTexto.Text);
            Redirigir(ArmarUrl(Pagina, partes), null);
        }

        // ------------------------------------------------------------------
        // Detalle (A5, A7, A8, A9)
        // ------------------------------------------------------------------

        private void CargarDetalle(int idCampania)
        {
            CampaniaComercial campania = _bll.Obtener(idCampania);
            if (campania == null)
            {
                Redirigir(Pagina, "La campaña que buscás no existe.", true);
                return;
            }

            pnlDetalle.Visible = true;
            litTitulo.Text = "Detalle de campaña comercial";
            litBajada.Text = "Consultá la información completa de la campaña y sus vínculos comerciales.";
            litNombre.Text = campania.Nombre;
            lblEstado.Text = BLL_CampaniaComercial.TextoEstado(campania);
            lblEstado.CssClass = "comercial-badge " + BLL_CampaniaComercial.ClaseEstado(campania);
            litDescripcion.Text = campania.Descripcion;
            litPublico.Text = Catalogos.Nombres(ItemCatalogoComercial.PublicoObjetivo, campania.PublicoObjetivo);
            litCanal.Text = Catalogos.Nombre(ItemCatalogoComercial.CanalDifusion, campania.CanalDifusion);
            litInicio.Text = campania.FechaInicio.ToString("dd/MM/yyyy");
            litFin.Text = campania.FechaFin.ToString("dd/MM/yyyy");
            litPeriodo.Text = DescribirPeriodo(campania);
            litResponsable.Text = TextoOGuion(campania.NombreResponsable);
            litFechas.Text = campania.FechaAlta.ToString("dd/MM/yyyy") + " / " + campania.FechaUltimaModificacion.ToString("dd/MM/yyyy HH:mm");

            // A8 y A9.
            if (!campania.Activa)
            {
                pnlAvisoEstado.Visible = true;
                litAvisoEstado.Text = "Esta campaña fue dada de baja" +
                    (campania.FechaBaja.HasValue ? " el " + campania.FechaBaja.Value.ToString("dd/MM/yyyy") : string.Empty) +
                    " y no forma parte de la gestión comercial activa. Se conserva para consulta histórica y análisis.";
            }
            else if (campania.EstadoCampania == CampaniaComercial.EstadoFinalizada)
            {
                pnlAvisoEstado.Visible = true;
                litAvisoEstado.Text = "La campaña está finalizada: ya no se encuentra vigente. Se conserva para consulta, trazabilidad comercial y análisis.";
            }
            else if (campania.EstadoCampania == CampaniaComercial.EstadoCancelada)
            {
                pnlAvisoEstado.Visible = true;
                litAvisoEstado.Text = "La campaña fue cancelada: no forma parte de las acciones comerciales vigentes.";
            }
            else if (campania.EstadoCampania == CampaniaComercial.EstadoActiva && campania.FechaFin.Date < DateTime.Today)
            {
                pnlAvisoEstado.Visible = true;
                litAvisoEstado.Text = "La campaña sigue marcada como activa, pero su fecha de finalización ya pasó. Si terminó, editala y pasala a finalizada.";
            }

            List<VinculoComercial> vinculos = _bll.ListarVinculos(idCampania);
            pnlSinVinculos.Visible = vinculos.Count == 0;
            rptVinculos.Visible = vinculos.Count > 0;
            rptVinculos.DataSource = vinculos;
            rptVinculos.DataBind();

            lnkEditar.Visible = BLL_CampaniaComercial.PuedeEditarse(campania);
            lnkEditar.NavigateUrl = Pagina + "?editar=" + idCampania;
            btnDarDeBaja.Visible = BLL_CampaniaComercial.PuedeDarseDeBaja(campania);
        }

        protected void btnDarDeBaja_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !LeerEntero("ver").HasValue)
            {
                return;
            }

            int id = LeerEntero("ver").Value;
            ResultadoOperacion resultado = _bll.DarDeBaja(id, IdUsuarioInterno);
            Redirigir(Pagina + "?ver=" + id, resultado.Mensaje, !resultado.Exitoso);
        }

        // ------------------------------------------------------------------
        // Formulario (pasos 8 a 26 y A6)
        // ------------------------------------------------------------------

        private void CargarFormulario(int? idCampania)
        {
            pnlFormulario.Visible = true;
            CargarOpcionesFormulario();

            if (!idCampania.HasValue)
            {
                litTitulo.Text = "Agregar campaña";
                ddlEstado.SelectedValue = CampaniaComercial.EstadoPlanificada;
                return;
            }

            CampaniaComercial campania = _bll.Obtener(idCampania.Value);
            if (campania == null || !BLL_CampaniaComercial.PuedeEditarse(campania))
            {
                Redirigir(campania == null ? Pagina : Pagina + "?ver=" + idCampania.Value,
                    campania == null ? "La campaña que buscás no existe." : "Una campaña dada de baja solo se puede consultar.", true);
                return;
            }

            litTitulo.Text = "Editar campaña";
            litFormularioEyebrow.Text = "Edición";
            litFormularioTitulo.Text = campania.Nombre;
            txtNombre.Text = campania.Nombre;
            txtDescripcion.Text = campania.Descripcion;
            MarcarVarios(cblPublico, campania.PublicoObjetivo);
            Seleccionar(ddlCanal, campania.CanalDifusion);
            Seleccionar(ddlEstado, campania.EstadoCampania);
            txtInicio.Text = FechaParaCampo(campania.FechaInicio);
            txtFin.Text = FechaParaCampo(campania.FechaFin);
            lnkCancelar.NavigateUrl = Pagina + "?ver=" + campania.IdCampaniaComercial;

            List<VinculoComercial> vinculos = _bll.ListarVinculos(campania.IdCampaniaComercial);
            MarcarVinculos(cblContactos, vinculos, OpcionVinculoComercial.TipoContacto);
            MarcarVinculos(cblAlianzas, vinculos, OpcionVinculoComercial.TipoAlianza);
            MarcarVinculos(cblContenidos, vinculos, OpcionVinculoComercial.TipoContenido);
            MarcarVinculos(cblOrigenes, vinculos, OpcionVinculoComercial.TipoOrigen);
        }

        private void CargarOpcionesFormulario()
        {
            CargarCatalogo(cblPublico, ItemCatalogoComercial.PublicoObjetivo, null);
            CargarCatalogo(ddlCanal, ItemCatalogoComercial.CanalDifusion, "Elegí un canal");
            CargarDiccionario(ddlEstado, BLL_CampaniaComercial.Estados, null);
            pnlSinCatalogos.Visible = cblPublico.Items.Count == 0 || ddlCanal.Items.Count <= 1;
            btnGuardar.Enabled = !pnlSinCatalogos.Visible;

            int? idActual = LeerEntero("editar");
            CargarOpciones(cblContactos, litSinContactos, OpcionVinculoComercial.TipoContacto, idActual);
            CargarOpciones(cblAlianzas, litSinAlianzas, OpcionVinculoComercial.TipoAlianza, idActual);
            CargarOpciones(cblContenidos, litSinContenidos, OpcionVinculoComercial.TipoContenido, idActual);
            CargarOpciones(cblOrigenes, litSinOrigenes, OpcionVinculoComercial.TipoOrigen, idActual);
        }

        private void CargarOpciones(CheckBoxList lista, Literal vacio, string tipo, int? idCampaniaActual)
        {
            lista.Items.Clear();
            foreach (OpcionVinculoComercial opcion in Catalogos.ListarParaVincular(tipo))
            {
                string texto = opcion.Nombre;
                if (!string.IsNullOrEmpty(opcion.Detalle) && tipo == OpcionVinculoComercial.TipoContacto)
                {
                    texto += " (" + opcion.Detalle + ")";
                }

                if (opcion.IdCampaniaActual.HasValue && opcion.IdCampaniaActual != idCampaniaActual)
                {
                    texto += " · hoy en otra campaña";
                }

                lista.Items.Add(new ListItem(texto, opcion.Id.ToString(CultureInfo.InvariantCulture)));
            }

            vacio.Visible = lista.Items.Count == 0;
        }

        // Lo vinculado que ya no aparece entre las opciones (por ejemplo, una
        // alianza que pasó a finalizada) se agrega para no perderlo al guardar.
        private static void MarcarVinculos(CheckBoxList lista, List<VinculoComercial> vinculos, string tipo)
        {
            foreach (VinculoComercial vinculo in vinculos.Where(v => v.Tipo == tipo))
            {
                string valor = vinculo.Id.ToString(CultureInfo.InvariantCulture);
                ListItem item = lista.Items.FindByValue(valor);
                if (item == null)
                {
                    item = new ListItem(vinculo.Nombre + " (" + vinculo.Estado + ")", valor);
                    lista.Items.Add(item);
                }

                item.Selected = true;
            }
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int? idEditar = LeerEntero("editar");
            CampaniaComercial campania = new CampaniaComercial
            {
                IdCampaniaComercial = idEditar ?? 0,
                Nombre = txtNombre.Text,
                Descripcion = txtDescripcion.Text,
                PublicoObjetivo = Elegidos(cblPublico),
                CanalDifusion = Vacio(ddlCanal.SelectedValue),
                EstadoCampania = Vacio(ddlEstado.SelectedValue),
                IdsContactos = IdsElegidos(cblContactos),
                IdsAlianzas = IdsElegidos(cblAlianzas),
                IdsContenidos = IdsElegidos(cblContenidos),
                IdsOrigenes = IdsElegidos(cblOrigenes)
            };

            ResultadoOperacion<int> resultado = _bll.Guardar(campania, txtInicio.Text, txtFin.Text, IdUsuarioInterno);
            if (!resultado.Exitoso)
            {
                // A10 a A17: se queda en el formulario con lo cargado.
                pnlFormulario.Visible = true;
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                return;
            }

            // Pasos 25 y 26 (alta) o A6 pasos 8 y 9 (edición).
            Redirigir(idEditar.HasValue ? Pagina + "?ver=" + resultado.Valor : Pagina, resultado.Mensaje,
                resultado.CodigoAlternativo != null);
        }

        // ------------------------------------------------------------------
        // Orígenes de usuario
        // ------------------------------------------------------------------

        private void CargarOrigenes()
        {
            pnlOrigenes.Visible = true;
            litTitulo.Text = "Orígenes de usuario";
            litBajada.Text = "Definí los canales por los que llegan las personas a StageUp y obtené el enlace de cada uno para tus acciones de difusión.";

            ResultadoOperacion<List<OrigenUsuario>> resultado = _bllOrigen.Listar(false);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                rptOrigenes.Visible = false;
                return;
            }

            pnlSinOrigenesListado.Visible = resultado.Valor.Count == 0;
            rptOrigenes.DataSource = resultado.Valor;
            rptOrigenes.DataBind();
        }

        protected void rptOrigenes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!TieneAcceso() || e.CommandName != "CambiarActivo")
            {
                return;
            }

            string[] partes = Convert.ToString(e.CommandArgument).Split('|');
            int id;
            if (partes.Length != 2 || !int.TryParse(partes[0], out id))
            {
                return;
            }

            ResultadoOperacion resultado = _bllOrigen.CambiarActivo(id, partes[1] == "1", IdUsuarioInterno);
            Redirigir(Pagina + "?vista=origenes", resultado.Mensaje, !resultado.Exitoso);
        }

        private void CargarFormularioOrigen(int? idOrigen)
        {
            pnlFormOrigen.Visible = true;
            litTitulo.Text = "Orígenes de usuario";
            litBajada.Text = "Definí los canales por los que llegan las personas a StageUp.";
            CargarCatalogo(ddlCanalOrigen, ItemCatalogoComercial.CanalOrigen, "Elegí un canal");
            CargarVinculoUnico(ddlCampaniaOrigen, OpcionVinculoComercial.TipoCampania, "Ninguna");
            CargarVinculoUnico(ddlAlianzaOrigen, OpcionVinculoComercial.TipoAlianza, "Ninguna");
            chkVisibleOrigen.Checked = false;

            if (!idOrigen.HasValue)
            {
                return;
            }

            OrigenUsuario origen = _bllOrigen.Obtener(idOrigen.Value);
            if (origen == null)
            {
                Redirigir(Pagina + "?vista=origenes", "El origen que buscás no existe.", true);
                return;
            }

            litTituloOrigen.Text = origen.NombreOrigen;
            txtNombreOrigen.Text = origen.NombreOrigen;
            txtCodigoOrigen.Text = origen.Codigo;
            Seleccionar(ddlCanalOrigen, origen.CanalOrigen);
            AsegurarOpcion(ddlCampaniaOrigen, origen.IdCampaniaComercial, origen.NombreCampania);
            AsegurarOpcion(ddlAlianzaOrigen, origen.IdAlianzaEstrategica, origen.NombreAlianza);
            chkVisibleOrigen.Checked = origen.VisibleEnRegistro;
            txtDescripcionOrigen.Text = origen.Descripcion;
        }

        private void CargarVinculoUnico(DropDownList lista, string tipo, string textoVacio)
        {
            lista.Items.Clear();
            lista.Items.Add(new ListItem(textoVacio, string.Empty));
            foreach (OpcionVinculoComercial opcion in Catalogos.ListarParaVincular(tipo))
            {
                lista.Items.Add(new ListItem(opcion.Nombre, opcion.Id.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private static void AsegurarOpcion(DropDownList lista, int? id, string nombre)
        {
            if (!id.HasValue)
            {
                return;
            }

            string valor = id.Value.ToString(CultureInfo.InvariantCulture);
            if (lista.Items.FindByValue(valor) == null)
            {
                lista.Items.Add(new ListItem((nombre ?? "#" + valor) + " (no disponible)", valor));
            }

            Seleccionar(lista, valor);
        }

        protected void btnGuardarOrigen_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            OrigenUsuario origen = new OrigenUsuario
            {
                IdOrigenUsuario = LeerEntero("origen") ?? 0,
                NombreOrigen = txtNombreOrigen.Text,
                Codigo = txtCodigoOrigen.Text,
                CanalOrigen = Vacio(ddlCanalOrigen.SelectedValue),
                IdCampaniaComercial = EnteroElegido(ddlCampaniaOrigen),
                IdAlianzaEstrategica = EnteroElegido(ddlAlianzaOrigen),
                VisibleEnRegistro = chkVisibleOrigen.Checked,
                Descripcion = txtDescripcionOrigen.Text
            };

            ResultadoOperacion<int> resultado = _bllOrigen.Guardar(origen, IdUsuarioInterno);
            if (!resultado.Exitoso)
            {
                pnlFormOrigen.Visible = true;
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                return;
            }

            Redirigir(Pagina + "?vista=origenes", resultado.Mensaje);
        }

        // ------------------------------------------------------------------
        // Textos para el listado y el detalle
        // ------------------------------------------------------------------

        protected string TextoPublico(object dato)
        {
            CampaniaComercial campania = (CampaniaComercial)dato;
            return Catalogos.Nombres(ItemCatalogoComercial.PublicoObjetivo, campania.PublicoObjetivo);
        }

        protected static string TextoEstado(object dato)
        {
            return BLL_CampaniaComercial.TextoEstado((CampaniaComercial)dato);
        }

        protected static string ClaseEstado(object dato)
        {
            return BLL_CampaniaComercial.ClaseEstado((CampaniaComercial)dato);
        }

        protected static string TextoVinculos(object dato)
        {
            CampaniaComercial c = (CampaniaComercial)dato;
            if (c.CantidadVinculos == 0)
            {
                return "Sin vínculos";
            }

            List<string> partes = new List<string>();
            if (c.CantidadContactos > 0) partes.Add(c.CantidadContactos + (c.CantidadContactos == 1 ? " contacto" : " contactos"));
            if (c.CantidadAlianzas > 0) partes.Add(c.CantidadAlianzas + (c.CantidadAlianzas == 1 ? " alianza" : " alianzas"));
            if (c.CantidadContenidos > 0) partes.Add(c.CantidadContenidos + (c.CantidadContenidos == 1 ? " pieza" : " piezas"));
            if (c.CantidadOrigenes > 0) partes.Add(c.CantidadOrigenes + (c.CantidadOrigenes == 1 ? " origen" : " orígenes"));
            return string.Join(" · ", partes);
        }

        protected static string Resumir(object valor, int largo)
        {
            string texto = Convert.ToString(valor) ?? string.Empty;
            return texto.Length <= largo ? texto : texto.Substring(0, largo).TrimEnd() + "…";
        }

        protected static string TextoTipoVinculo(object tipo)
        {
            switch (Convert.ToString(tipo))
            {
                case OpcionVinculoComercial.TipoContacto: return "Contacto comercial";
                case OpcionVinculoComercial.TipoAlianza: return "Alianza estratégica";
                case OpcionVinculoComercial.TipoContenido: return "Contenido promocional";
                case OpcionVinculoComercial.TipoOrigen: return "Origen de usuario";
                default: return Convert.ToString(tipo);
            }
        }

        protected static string TextoEstadoVinculo(object dato)
        {
            VinculoComercial vinculo = (VinculoComercial)dato;
            return vinculo.Tipo == OpcionVinculoComercial.TipoContacto
                ? BLL_ContactoComercial.TextoEstado(vinculo.Estado)
                : TextoOGuion(vinculo.Estado);
        }

        // Los contactos llevan a su detalle; el resto se muestra como texto
        // (sus pantallas se suman con el script 81).
        protected string EnlaceVinculo(object dato)
        {
            VinculoComercial vinculo = (VinculoComercial)dato;
            string nombre = HttpUtility.HtmlEncode(vinculo.Nombre);
            if (vinculo.Tipo == OpcionVinculoComercial.TipoContacto)
            {
                return "<a class=\"text-link\" href=\"ContactosComerciales.aspx?ver=" + vinculo.Id + "\">" + nombre + "</a>";
            }

            if (vinculo.Tipo == OpcionVinculoComercial.TipoOrigen)
            {
                return "<a class=\"text-link\" href=\"" + Pagina + "?vista=origenes&amp;origen=" + vinculo.Id + "\">" + nombre + "</a>";
            }

            return nombre;
        }

        protected string EnlaceOrigen(object codigo)
        {
            return Request.Url.GetLeftPart(UriPartial.Authority) + ResolveUrl("~/Registrarse.aspx") + "?origen=" + Convert.ToString(codigo);
        }

        private static string DescribirPeriodo(CampaniaComercial campania)
        {
            int dias = (campania.FechaFin.Date - campania.FechaInicio.Date).Days + 1;
            string duracion = dias + (dias == 1 ? " día" : " días");
            DateTime hoy = DateTime.Today;
            if (hoy < campania.FechaInicio.Date)
            {
                return duracion + " · empieza en " + (campania.FechaInicio.Date - hoy).Days + " días";
            }

            if (hoy > campania.FechaFin.Date)
            {
                return duracion + " · terminó";
            }

            return duracion + " · en curso";
        }
    }
}
