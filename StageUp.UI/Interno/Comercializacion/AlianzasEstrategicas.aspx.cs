using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno.Comercializacion
{
    // CU-004-003 Gestionar alianzas estratégicas.
    //   AlianzasEstrategicas.aspx?[filtros]         -> listado (pasos 2 a 8, A2 a A4)
    //   AlianzasEstrategicas.aspx?ver={id}          -> detalle (A5, A12, A13)
    //   AlianzasEstrategicas.aspx?nuevo=1           -> alta (pasos 9 a 30)
    //   AlianzasEstrategicas.aspx?editar={id}       -> edición (A6, A8, A14 a A16)
    //   AlianzasEstrategicas.aspx?seguimiento={id}  -> seguimiento (A7, A8)
    //   AlianzasEstrategicas.aspx?renovar={id}      -> renovación o reactivación (A9)
    //   AlianzasEstrategicas.aspx?finalizar={id}    -> finalización (A10)
    //   AlianzasEstrategicas.aspx?baja={id}         -> baja lógica (A11)
    // Filtros en la query string: vista, estado, tipo, resp, desde, hasta, q.
    public partial class AlianzasEstrategicas : PaginaComercial
    {
        private const string Pagina = "AlianzasEstrategicas.aspx";

        private readonly BLL_AlianzaEstrategica _bll = new BLL_AlianzaEstrategica();

        protected override string PermisoRequerido
        {
            get { return BLL_AlianzaEstrategica.PermisoGestionar; }
        }

        private bool PuedeReactivar
        {
            get { return GestorDeSesion.TienePermisoInterno(BLL_AlianzaEstrategica.PermisoReactivar); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso() || IsPostBack)
            {
                return;
            }

            MostrarMensajePendiente(pnlMensaje, litMensaje);

            if (Request.QueryString["nuevo"] == "1") CargarFormulario(null);
            else if (LeerEntero("editar").HasValue) CargarFormulario(LeerEntero("editar").Value);
            else if (LeerEntero("seguimiento").HasValue) CargarSeguimiento(LeerEntero("seguimiento").Value);
            else if (LeerEntero("renovar").HasValue) CargarRenovacion(LeerEntero("renovar").Value);
            else if (LeerEntero("finalizar").HasValue) CargarCierre(LeerEntero("finalizar").Value, false);
            else if (LeerEntero("baja").HasValue) CargarCierre(LeerEntero("baja").Value, true);
            else if (LeerEntero("ver").HasValue) CargarDetalle(LeerEntero("ver").Value);
            else CargarListado();
        }

        private AlianzaEstrategica ObtenerORedirigir(int id)
        {
            AlianzaEstrategica alianza = _bll.Obtener(id);
            if (alianza == null)
            {
                Redirigir(Pagina, "La alianza que buscás no existe.", true);
            }

            return alianza;
        }

        // ------------------------------------------------------------------
        // Listado
        // ------------------------------------------------------------------

        private void CargarListado()
        {
            pnlListado.Visible = true;
            CargarDiccionario(ddlFiltroEstado, BLL_AlianzaEstrategica.Estados, "Todos");
            CargarCatalogo(ddlFiltroTipo, ItemCatalogoComercial.TipoInstitucion, "Todos");
            CargarResponsables(ddlFiltroResponsable, "Todos", null);

            FiltroAlianzaEstrategica filtro = new FiltroAlianzaEstrategica
            {
                Vista = LeerTexto("vista") ?? FiltroAlianzaEstrategica.VistaEnGestion,
                Estado = LeerTexto("estado"),
                TipoInstitucion = LeerTexto("tipo"),
                IdUsuarioInternoResponsable = LeerEntero("resp"),
                Desde = LeerFechaQuery("desde"),
                Hasta = LeerFechaQuery("hasta"),
                Texto = LeerTexto("q")
            };

            // Si se filtra por un estado histórico, se muestran también las históricas.
            if (filtro.Estado == AlianzaEstrategica.EstadoFinalizada || filtro.Estado == AlianzaEstrategica.EstadoDadaDeBaja)
            {
                filtro.Vista = FiltroAlianzaEstrategica.VistaTodas;
            }

            Seleccionar(ddlFiltroVista, filtro.Vista);
            Seleccionar(ddlFiltroEstado, filtro.Estado);
            Seleccionar(ddlFiltroTipo, filtro.TipoInstitucion);
            Seleccionar(ddlFiltroResponsable, filtro.IdUsuarioInternoResponsable.HasValue ? filtro.IdUsuarioInternoResponsable.Value.ToString(CultureInfo.InvariantCulture) : null);
            txtFiltroDesde.Text = FechaParaCampo(filtro.Desde);
            txtFiltroHasta.Text = FechaParaCampo(filtro.Hasta);
            txtFiltroTexto.Text = filtro.Texto;

            ResultadoOperacion<ResumenAlianzas> resumen = _bll.ObtenerResumen();
            if (!resumen.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resumen.Mensaje, true);
                rptAlianzas.Visible = false;
                return;
            }

            litEnGestion.Text = resumen.Valor.EnGestion.ToString(CultureInfo.InvariantCulture);
            litActivas.Text = resumen.Valor.Activas.ToString(CultureInfo.InvariantCulture);
            litEnEvaluacion.Text = resumen.Valor.EnEvaluacion.ToString(CultureInfo.InvariantCulture);
            litVencidas.Text = resumen.Valor.Vencidas.ToString(CultureInfo.InvariantCulture);
            litHistoricas.Text = resumen.Valor.Historicas.ToString(CultureInfo.InvariantCulture);

            // A2.
            if (resumen.Valor.Total == 0)
            {
                pnlSinAlianzas.Visible = true;
                pnlFiltros.Visible = false;
                rptAlianzas.Visible = false;
                return;
            }

            ResultadoOperacion<List<AlianzaEstrategica>> resultado = _bll.Listar(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                rptAlianzas.Visible = false;
                return;
            }

            // A4.
            pnlSinResultados.Visible = resultado.Valor.Count == 0;
            rptAlianzas.Visible = resultado.Valor.Count > 0;
            litCantidad.Text = resultado.Valor.Count == 0
                ? string.Empty
                : resultado.Valor.Count + (resultado.Valor.Count == 1 ? " alianza" : " alianzas") + (filtro.TieneCriterios ? " para los filtros aplicados." : ".");
            rptAlianzas.DataSource = resultado.Valor;
            rptAlianzas.DataBind();
        }

        protected void btnAplicarFiltros_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            List<string> partes = new List<string>();
            if (ddlFiltroVista.SelectedValue != FiltroAlianzaEstrategica.VistaEnGestion)
            {
                Agregar(partes, "vista", ddlFiltroVista.SelectedValue);
            }

            Agregar(partes, "estado", ddlFiltroEstado.SelectedValue);
            Agregar(partes, "tipo", ddlFiltroTipo.SelectedValue);
            Agregar(partes, "resp", ddlFiltroResponsable.SelectedValue);
            Agregar(partes, "desde", txtFiltroDesde.Text);
            Agregar(partes, "hasta", txtFiltroHasta.Text);
            Agregar(partes, "q", txtFiltroTexto.Text);
            Redirigir(ArmarUrl(Pagina, partes), null);
        }

        // ------------------------------------------------------------------
        // Detalle (A5, A12, A13)
        // ------------------------------------------------------------------

        private void CargarDetalle(int id)
        {
            AlianzaEstrategica a = ObtenerORedirigir(id);
            if (a == null)
            {
                return;
            }

            pnlDetalle.Visible = true;
            litTitulo.Text = "Detalle de alianza estratégica";
            litBajada.Text = "Información completa de la alianza, sus vínculos comerciales y todo el seguimiento registrado.";
            litNombre.Text = a.NombreEntidad;
            lblEstado.Text = BLL_AlianzaEstrategica.TextoEstado(a.EstadoAlianza);
            lblEstado.CssClass = "comercial-badge " + BLL_AlianzaEstrategica.ClaseEstado(a.EstadoAlianza);
            litTipo.Text = Catalogos.Nombre(ItemCatalogoComercial.TipoInstitucion, a.TipoInstitucion);
            litResponsable.Text = TextoOGuion(a.NombreResponsable);
            litVigencia.Text = BLL_AlianzaEstrategica.TextoVigencia(a);
            litDatosContacto.Text = a.DatosContacto;
            litObjetivo.Text = a.ObjetivoComercial;
            litAlcance.Text = a.Alcance;
            litDescripcion.Text = a.DescripcionAcuerdo;
            litBeneficios.Text = a.BeneficiosAsociados;
            litObservaciones.Text = TextoOGuion(a.Observaciones);
            litAlta.Text = a.FechaAlta.ToString("dd/MM/yyyy HH:mm");
            litActualizacion.Text = a.FechaUltimaModificacion.ToString("dd/MM/yyyy HH:mm");

            // A12 y A13.
            if (!a.EnGestion)
            {
                pnlAviso.Visible = true;
                litAviso.Text = "La alianza no se encuentra vigente dentro de la gestión comercial activa (" +
                    BLL_AlianzaEstrategica.TextoEstado(a.EstadoAlianza).ToLowerInvariant() + "). Se conserva para consulta histórica: no admite modificaciones" +
                    (PuedeReactivar ? ", salvo renovarla para volver a ponerla en gestión." : ".");
            }
            else if (a.Vencida)
            {
                pnlAviso.Visible = true;
                litAviso.Text = "La vigencia de esta alianza venció el " + a.FechaFin.Value.ToString("dd/MM/yyyy") +
                    ". Podés renovarla, finalizarla o mantenerla para consulta.";
            }

            ResultadoOperacion<List<VinculoComercial>> cargaVinculos = _bll.ListarVinculos(id);
            if (!cargaVinculos.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, cargaVinculos.Mensaje, true);
            }

            List<VinculoComercial> vinculos = cargaVinculos.Exitoso ? cargaVinculos.Valor : new List<VinculoComercial>();
            pnlSinVinculos.Visible = cargaVinculos.Exitoso && vinculos.Count == 0;
            rptVinculos.Visible = vinculos.Count > 0;
            rptVinculos.DataSource = vinculos;
            rptVinculos.DataBind();

            List<SeguimientoAlianza> seguimientos = _bll.ListarSeguimientos(id);
            pnlSinSeguimiento.Visible = seguimientos.Count == 0;
            rptSeguimientos.DataSource = seguimientos;
            rptSeguimientos.DataBind();

            List<VigenciaAlianza> vigencias = _bll.ListarVigencias(id);
            pnlVigencias.Visible = vigencias.Count > 0;
            rptVigencias.DataSource = vigencias;
            rptVigencias.DataBind();

            lnkEditar.Visible = BLL_AlianzaEstrategica.PuedeEditarse(a);
            lnkEditar.NavigateUrl = Pagina + "?editar=" + id;
            lnkSeguimiento.Visible = a.EnGestion;
            lnkSeguimiento.NavigateUrl = Pagina + "?seguimiento=" + id;
            lnkRenovar.Visible = BLL_AlianzaEstrategica.PuedeRenovarse(a, PuedeReactivar);
            lnkRenovar.Text = a.EnGestion ? "Renovar alianza" : "Reactivar con nueva vigencia";
            lnkRenovar.NavigateUrl = Pagina + "?renovar=" + id;
            lnkFinalizar.Visible = BLL_AlianzaEstrategica.PuedeFinalizarse(a);
            lnkFinalizar.NavigateUrl = Pagina + "?finalizar=" + id;
            lnkBaja.Visible = BLL_AlianzaEstrategica.PuedeDarseDeBaja(a);
            lnkBaja.NavigateUrl = Pagina + "?baja=" + id;
        }

        // ------------------------------------------------------------------
        // Formulario
        // ------------------------------------------------------------------

        private void CargarFormulario(int? id)
        {
            AlianzaEstrategica a = null;
            if (id.HasValue)
            {
                a = ObtenerORedirigir(id.Value);
                if (a == null)
                {
                    return;
                }

                if (!BLL_AlianzaEstrategica.PuedeEditarse(a))
                {
                    Redirigir(Pagina + "?ver=" + id.Value, "Una alianza finalizada o dada de baja solo se puede consultar.", true);
                    return;
                }
            }

            pnlFormulario.Visible = true;
            CargarCatalogo(ddlTipo, ItemCatalogoComercial.TipoInstitucion, "Elegí un tipo");
            CargarResponsables(ddlResponsable, "Elegí un responsable", null);
            ddlEstado.Items.Clear();
            foreach (KeyValuePair<string, string> estado in BLL_AlianzaEstrategica.Estados)
            {
                // En el alta no se ofrecen los estados de cierre; en la edición sí (A8).
                if (a != null || (estado.Key != AlianzaEstrategica.EstadoFinalizada && estado.Key != AlianzaEstrategica.EstadoDadaDeBaja))
                {
                    ddlEstado.Items.Add(new ListItem(estado.Value, estado.Key));
                }
            }

            pnlSinCatalogos.Visible = ddlTipo.Items.Count <= 1 || ddlResponsable.Items.Count <= 1;
            btnGuardar.Enabled = !pnlSinCatalogos.Visible;
            CargarOpcionesVinculo(cblContactos, litSinContactos, OpcionVinculoComercial.TipoContacto);
            CargarOpcionesVinculo(cblCampanias, litSinCampanias, OpcionVinculoComercial.TipoCampania);
            CargarOpcionesVinculo(cblContenidos, litSinContenidos, OpcionVinculoComercial.TipoContenido);
            CargarOpcionesVinculo(cblOrigenes, litSinOrigenes, OpcionVinculoComercial.TipoOrigen);

            if (a == null)
            {
                litTitulo.Text = "Agregar alianza";
                Seleccionar(ddlEstado, AlianzaEstrategica.EstadoEnEvaluacion);
                Seleccionar(ddlResponsable, IdUsuarioInterno.ToString(CultureInfo.InvariantCulture));
                txtInicio.Text = FechaParaCampo(DateTime.Today);
                return;
            }

            litTitulo.Text = "Editar alianza";
            litFormularioEyebrow.Text = "Edición";
            litFormularioTitulo.Text = a.NombreEntidad;
            litEtiquetaObservaciones.Text = "Observaciones";
            pnlObservacionCambio.Visible = true;
            txtEntidad.Text = a.NombreEntidad;
            Seleccionar(ddlTipo, a.TipoInstitucion);
            txtDatosContacto.Text = a.DatosContacto;
            AsegurarResponsable(ddlResponsable, a.IdUsuarioInternoResponsable, a.NombreResponsable);
            Seleccionar(ddlEstado, a.EstadoAlianza);
            txtInicio.Text = FechaParaCampo(a.FechaInicio);
            txtFin.Text = FechaParaCampo(a.FechaFin);
            txtObjetivo.Text = a.ObjetivoComercial;
            txtAlcance.Text = a.Alcance;
            txtDescripcion.Text = a.DescripcionAcuerdo;
            txtBeneficios.Text = a.BeneficiosAsociados;
            txtObservaciones.Text = a.Observaciones;
            lnkCancelar.NavigateUrl = Pagina + "?ver=" + a.IdAlianzaEstrategica;

            // Si no se pudieron consultar los vínculos, el formulario quedaría
            // sin ellos y al guardar se borrarían: no se permite guardar.
            ResultadoOperacion<List<VinculoComercial>> cargaVinculos = _bll.ListarVinculos(a.IdAlianzaEstrategica);
            if (!cargaVinculos.Exitoso)
            {
                VinculosNoCargados = true;
                btnGuardar.Enabled = false;
                MostrarMensaje(pnlMensaje, litMensaje, cargaVinculos.Mensaje, true);
                return;
            }

            List<VinculoComercial> vinculos = cargaVinculos.Valor;
            MarcarVinculos(cblContactos, vinculos, OpcionVinculoComercial.TipoContacto);
            MarcarVinculos(cblCampanias, vinculos, OpcionVinculoComercial.TipoCampania);
            MarcarVinculos(cblContenidos, vinculos, OpcionVinculoComercial.TipoContenido);
            MarcarVinculos(cblOrigenes, vinculos, OpcionVinculoComercial.TipoOrigen);
        }

        // Los vínculos de la edición no se pudieron cargar (ver CargarFormulario).
        private bool VinculosNoCargados
        {
            get { return ViewState["VinculosNoCargados"] as bool? ?? false; }
            set { ViewState["VinculosNoCargados"] = value; }
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (VinculosNoCargados)
            {
                MostrarMensaje(pnlMensaje, litMensaje, BLL_AlianzaEstrategica.MensajeVinculosNoDisponibles, true);
                return;
            }

            int? idEditar = LeerEntero("editar");
            AlianzaEstrategica alianza = new AlianzaEstrategica
            {
                IdAlianzaEstrategica = idEditar ?? 0,
                NombreEntidad = txtEntidad.Text,
                TipoInstitucion = Vacio(ddlTipo.SelectedValue),
                DatosContacto = txtDatosContacto.Text,
                IdUsuarioInternoResponsable = EnteroElegido(ddlResponsable) ?? 0,
                EstadoAlianza = Vacio(ddlEstado.SelectedValue),
                ObjetivoComercial = txtObjetivo.Text,
                Alcance = txtAlcance.Text,
                DescripcionAcuerdo = txtDescripcion.Text,
                BeneficiosAsociados = txtBeneficios.Text,
                Observaciones = txtObservaciones.Text,
                ObservacionCambioEstado = idEditar.HasValue ? txtObservacionCambio.Text : null,
                IdsContactos = IdsElegidos(cblContactos),
                IdsCampanias = IdsElegidos(cblCampanias),
                IdsContenidos = IdsElegidos(cblContenidos),
                IdsOrigenes = IdsElegidos(cblOrigenes)
            };

            ResultadoOperacion<int> resultado = _bll.Guardar(alianza, txtInicio.Text, txtFin.Text, IdUsuarioInterno);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                int idDuplicada;
                string codigo = resultado.CodigoAlternativo ?? string.Empty;
                lnkDuplicada.Visible = codigo.StartsWith("A22:", StringComparison.Ordinal) && int.TryParse(codigo.Substring(4), out idDuplicada);
                if (lnkDuplicada.Visible)
                {
                    lnkDuplicada.NavigateUrl = Pagina + "?ver=" + codigo.Substring(4);
                    lnkDuplicada.Target = "_blank";
                }

                return;
            }

            Redirigir(idEditar.HasValue ? Pagina + "?ver=" + resultado.Valor : Pagina, resultado.Mensaje);
        }

        // ------------------------------------------------------------------
        // Seguimiento (A7, A8)
        // ------------------------------------------------------------------

        private void CargarSeguimiento(int id)
        {
            AlianzaEstrategica a = ObtenerORedirigir(id);
            if (a == null)
            {
                return;
            }

            if (!a.EnGestion)
            {
                Redirigir(Pagina + "?ver=" + id, "Una alianza finalizada o dada de baja solo se puede consultar.", true);
                return;
            }

            pnlSeguimiento.Visible = true;
            litTitulo.Text = "Registrar seguimiento";
            litSeguimientoTitulo.Text = a.NombreEntidad;
            litSeguimientoEstado.Text = "Estado actual: " + BLL_AlianzaEstrategica.TextoEstado(a.EstadoAlianza) + ".";
            txtFechaInteraccion.Text = FechaParaCampo(DateTime.Today);
            CargarCatalogo(ddlTipoInteraccion, ItemCatalogoComercial.TipoInteraccion, "Elegí un tipo");
            CargarResponsables(ddlAsignado, "Sin asignar", null);
            ddlEstadoResultante.Items.Clear();
            ddlEstadoResultante.Items.Add(new ListItem("Sin cambios (" + BLL_AlianzaEstrategica.TextoEstado(a.EstadoAlianza) + ")", string.Empty));
            foreach (KeyValuePair<string, string> estado in BLL_AlianzaEstrategica.Estados.Where(p => p.Key != a.EstadoAlianza))
            {
                ddlEstadoResultante.Items.Add(new ListItem(estado.Value, estado.Key));
            }

            lnkCancelarSeguimiento.NavigateUrl = Pagina + "?ver=" + id;
        }

        protected void btnGuardarSeguimiento_Click(object sender, EventArgs e)
        {
            int? id = LeerEntero("seguimiento");
            if (!TieneAcceso() || !id.HasValue)
            {
                return;
            }

            ResultadoOperacion resultado = _bll.RegistrarSeguimiento(new SeguimientoAlianza
            {
                IdAlianzaEstrategica = id.Value,
                TipoInteraccion = Vacio(ddlTipoInteraccion.SelectedValue),
                Observacion = txtObservacion.Text,
                Avance = txtAvance.Text,
                AccionPendiente = txtAccionPendiente.Text,
                IdUsuarioInternoAsignado = EnteroElegido(ddlAsignado),
                EstadoResultante = Vacio(ddlEstadoResultante.SelectedValue)
            }, txtFechaInteraccion.Text, IdUsuarioInterno);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                return;
            }

            Redirigir(Pagina + "?ver=" + id.Value, resultado.Mensaje);
        }

        // ------------------------------------------------------------------
        // Renovación (A9)
        // ------------------------------------------------------------------

        private void CargarRenovacion(int id)
        {
            AlianzaEstrategica a = ObtenerORedirigir(id);
            if (a == null)
            {
                return;
            }

            if (!BLL_AlianzaEstrategica.PuedeRenovarse(a, PuedeReactivar))
            {
                Redirigir(Pagina + "?ver=" + id, a.EnGestion
                    ? "Solo se puede renovar una alianza activa o pausada."
                    : "Una alianza finalizada o dada de baja solo se puede consultar.", true);
                return;
            }

            pnlRenovacion.Visible = true;
            litTitulo.Text = a.EnGestion ? "Renovar alianza" : "Reactivar alianza";
            litRenovacionEyebrow.Text = a.EnGestion ? "Renovación" : "Reactivación con nueva vigencia";
            litRenovacionTitulo.Text = a.NombreEntidad;
            litVigenciaActual.Text = "Vigencia actual: " + BLL_AlianzaEstrategica.TextoVigencia(a) + ". La vigencia anterior queda guardada en el historial." +
                (a.EnGestion ? string.Empty : " Al guardar, la alianza vuelve a estar activa.");

            // Por defecto, el nuevo período empieza al día siguiente del fin anterior (o hoy).
            DateTime inicio = a.FechaFin.HasValue && a.FechaFin.Value.Date >= DateTime.Today ? a.FechaFin.Value.AddDays(1) : DateTime.Today;
            txtRenovInicio.Text = FechaParaCampo(a.EnGestion && !a.Vencida ? a.FechaInicio : inicio);
            txtRenovAlcance.Text = a.Alcance;
            txtRenovBeneficios.Text = a.BeneficiosAsociados;
            CargarResponsables(ddlRenovResponsable, "Sin cambios", null);
            AsegurarResponsable(ddlRenovResponsable, a.IdUsuarioInternoResponsable, a.NombreResponsable);
            lnkCancelarRenovacion.NavigateUrl = Pagina + "?ver=" + id;
            btnGuardarRenovacion.OnClientClick = "if (!StageUpConfirm.confirmPostback(this, event, '" +
                (a.EnGestion
                    ? "Se va a registrar la nueva vigencia de la alianza y la anterior queda en el historial. ¿Confirmás la renovación?"
                    : "La alianza vuelve a estar activa con la nueva vigencia. ¿Confirmás la reactivación?") +
                "', { title: '" + (a.EnGestion ? "Renovar alianza" : "Reactivar alianza") + "', confirmText: 'Sí, guardar', cancelText: 'Volver' })) return false;";
        }

        protected void btnGuardarRenovacion_Click(object sender, EventArgs e)
        {
            int? id = LeerEntero("renovar");
            if (!TieneAcceso() || !id.HasValue)
            {
                return;
            }

            AlianzaEstrategica actual = _bll.Obtener(id.Value);
            ResultadoOperacion resultado = _bll.Renovar(new RenovacionAlianza
            {
                IdAlianzaEstrategica = id.Value,
                Alcance = actual != null && txtRenovAlcance.Text.Trim() == actual.Alcance ? null : txtRenovAlcance.Text,
                BeneficiosAsociados = actual != null && txtRenovBeneficios.Text.Trim() == actual.BeneficiosAsociados ? null : txtRenovBeneficios.Text,
                IdUsuarioInternoResponsable = EnteroElegido(ddlRenovResponsable),
                Observacion = txtRenovObservacion.Text
            }, txtRenovInicio.Text, txtRenovFin.Text, IdUsuarioInterno, PuedeReactivar);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                return;
            }

            Redirigir(Pagina + "?ver=" + id.Value, resultado.Mensaje);
        }

        // ------------------------------------------------------------------
        // Finalización (A10) y baja lógica (A11)
        // ------------------------------------------------------------------

        private void CargarCierre(int id, bool esBaja)
        {
            AlianzaEstrategica a = ObtenerORedirigir(id);
            if (a == null)
            {
                return;
            }

            bool permitido = esBaja ? BLL_AlianzaEstrategica.PuedeDarseDeBaja(a) : BLL_AlianzaEstrategica.PuedeFinalizarse(a);
            if (!permitido)
            {
                Redirigir(Pagina + "?ver=" + id, esBaja
                    ? "La alianza ya estaba dada de baja."
                    : "Solo se puede finalizar una alianza activa o pausada.", true);
                return;
            }

            pnlCierre.Visible = true;
            litTitulo.Text = esBaja ? "Dar de baja alianza" : "Finalizar alianza";
            litCierreEyebrow.Text = esBaja ? "Baja lógica" : "Finalización";
            litCierreTitulo.Text = a.NombreEntidad;
            litCierreAviso.Text = esBaja
                ? "La alianza se va a retirar de la gestión activa, pero conserva su información histórica para consulta interna y análisis posterior."
                : "La alianza va a dejar de estar vigente, pero conserva su información histórica para consulta y trazabilidad comercial.";
            litEtiquetaMotivo.Text = esBaja ? "Motivo de la baja (opcional)" : "Observación de cierre o motivo de finalización (opcional)";
            btnConfirmarCierre.Text = esBaja ? "Dar de baja" : "Finalizar alianza";
            btnConfirmarCierre.OnClientClick = "if (!StageUpConfirm.confirmPostback(this, event, '" +
                (esBaja ? "¿Confirmás la baja lógica de la alianza?" : "¿Confirmás la finalización de la alianza?") +
                "', { title: '" + (esBaja ? "Dar de baja alianza" : "Finalizar alianza") + "', confirmText: 'Sí, confirmar', cancelText: 'Volver' })) return false;";
            lnkCancelarCierre.NavigateUrl = Pagina + "?ver=" + id;
        }

        protected void btnConfirmarCierre_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int? idBaja = LeerEntero("baja");
            int? idFinalizar = LeerEntero("finalizar");
            int? id = idBaja ?? idFinalizar;
            if (!id.HasValue)
            {
                return;
            }

            ResultadoOperacion resultado = idBaja.HasValue
                ? _bll.DarDeBaja(id.Value, txtMotivo.Text, IdUsuarioInterno)
                : _bll.Finalizar(id.Value, txtMotivo.Text, IdUsuarioInterno);
            Redirigir(Pagina + "?ver=" + id.Value, resultado.Mensaje, !resultado.Exitoso);
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        private void CargarResponsables(DropDownList lista, string textoVacio, int? seleccionado)
        {
            lista.Items.Clear();
            lista.Items.Add(new ListItem(textoVacio, string.Empty));
            foreach (UsuarioInterno u in _bll.ListarResponsables())
            {
                lista.Items.Add(new ListItem((u.Nombre + " " + u.Apellido).Trim(), u.IdUsuarioInterno.ToString(CultureInfo.InvariantCulture)));
            }

            if (seleccionado.HasValue)
            {
                Seleccionar(lista, seleccionado.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AsegurarResponsable(DropDownList lista, int id, string nombre)
        {
            string valor = id.ToString(CultureInfo.InvariantCulture);
            if (lista.Items.FindByValue(valor) == null)
            {
                lista.Items.Add(new ListItem((nombre ?? "#" + valor) + " (ya no está activo)", valor));
            }

            Seleccionar(lista, valor);
        }

        private void CargarOpcionesVinculo(CheckBoxList lista, Literal vacio, string tipo)
        {
            lista.Items.Clear();
            foreach (OpcionVinculoComercial opcion in Catalogos.ListarParaVincular(tipo))
            {
                string texto = opcion.Nombre;
                if (!string.IsNullOrEmpty(opcion.Detalle) && tipo == OpcionVinculoComercial.TipoContacto)
                {
                    texto += " (" + opcion.Detalle + ")";
                }

                lista.Items.Add(new ListItem(texto, opcion.Id.ToString(CultureInfo.InvariantCulture)));
            }

            vacio.Visible = lista.Items.Count == 0;
        }

        protected static string TextoHasta(object dato)
        {
            AlianzaEstrategica a = (AlianzaEstrategica)dato;
            string texto = a.FechaFin.HasValue ? "Hasta el " + a.FechaFin.Value.ToString("dd/MM/yyyy") : "Sin fecha de fin";
            return a.Vencida ? texto + " · vencida" : texto;
        }

        protected string TituloSeguimiento(object dato)
        {
            SeguimientoAlianza s = (SeguimientoAlianza)dato;
            string cambio = s.EstadoResultante != null
                ? BLL_AlianzaEstrategica.TextoEstado(s.EstadoAnterior) + " → " + BLL_AlianzaEstrategica.TextoEstado(s.EstadoResultante)
                : null;

            switch (s.TipoRegistro)
            {
                case SeguimientoAlianza.TipoCambioEstado: return "Cambio de estado: " + cambio;
                case SeguimientoAlianza.TipoRenovacion: return "Renovación" + (cambio != null ? " · " + cambio : string.Empty);
                case SeguimientoAlianza.TipoReactivacion: return "Reactivación · " + cambio;
                case SeguimientoAlianza.TipoFinalizacion: return "Finalización · " + cambio;
                case SeguimientoAlianza.TipoBaja: return "Baja lógica · " + cambio;
                default:
                    string titulo = Catalogos.Nombre(ItemCatalogoComercial.TipoInteraccion, s.TipoInteraccion) + " del " + s.FechaInteraccion.ToString("dd/MM/yyyy");
                    return cambio != null ? titulo + " · " + cambio : titulo;
            }
        }

        protected static string MetaSeguimiento(object dato)
        {
            SeguimientoAlianza s = (SeguimientoAlianza)dato;
            return "Registrado el " + s.FechaRegistro.ToString("dd/MM/yyyy HH:mm") + (s.NombreRegistro != null ? " por " + s.NombreRegistro : string.Empty);
        }

        protected static string DetalleSeguimiento(object dato)
        {
            SeguimientoAlianza s = (SeguimientoAlianza)dato;
            List<string> partes = new List<string>();
            if (s.Avance != null) partes.Add("<dt>Avance o compromisos</dt><dd>" + HttpUtility.HtmlEncode(s.Avance) + "</dd>");
            if (s.AccionPendiente != null) partes.Add("<dt>Acción pendiente</dt><dd>" + HttpUtility.HtmlEncode(s.AccionPendiente) + "</dd>");
            if (s.NombreAsignado != null) partes.Add("<dt>Responsable asignado</dt><dd>" + HttpUtility.HtmlEncode(s.NombreAsignado) + "</dd>");
            return partes.Count == 0
                ? string.Empty
                : "<dl class=\"comercial-datos\">" + string.Join(string.Empty, partes.Select(p => "<div>" + p + "</div>")) + "</dl>";
        }
    }
}
