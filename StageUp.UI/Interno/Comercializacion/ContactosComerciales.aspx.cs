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
    // CU-004-002 Gestionar contactos comerciales.
    //   ContactosComerciales.aspx?[filtros]        -> listado (pasos 2 a 8, A2 a A4)
    //   ContactosComerciales.aspx?ver={id}         -> detalle con el seguimiento (A5, A9)
    //   ContactosComerciales.aspx?nuevo=1          -> alta (pasos 9 a 30)
    //   ContactosComerciales.aspx?editar={id}      -> edición (A6, A8 a A12)
    //   ContactosComerciales.aspx?seguimiento={id} -> nueva observación de seguimiento (A7, A8)
    // Filtros en la query string: estado, tipo, origen, campania, q.
    public partial class ContactosComerciales : PaginaComercial
    {
        private const string Pagina = "ContactosComerciales.aspx";

        private readonly BLL_ContactoComercial _bll = new BLL_ContactoComercial();
        private readonly BLL_OrigenUsuario _bllOrigen = new BLL_OrigenUsuario();

        protected override string PermisoRequerido
        {
            get { return BLL_ContactoComercial.PermisoGestionar; }
        }

        // A9: cuenta elegida en el formulario.
        private int? IdCuentaElegida
        {
            get { return ViewState["IdCuentaElegida"] as int?; }
            set { ViewState["IdCuentaElegida"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso() || IsPostBack)
            {
                return;
            }

            MostrarMensajePendiente(pnlMensaje, litMensaje);

            if (Request.QueryString["nuevo"] == "1")
            {
                CargarFormulario(null);
            }
            else if (LeerEntero("editar").HasValue)
            {
                CargarFormulario(LeerEntero("editar").Value);
            }
            else if (LeerEntero("seguimiento").HasValue)
            {
                CargarFormularioSeguimiento(LeerEntero("seguimiento").Value);
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
        // Listado
        // ------------------------------------------------------------------

        private void CargarListado()
        {
            pnlListado.Visible = true;
            CargarDiccionario(ddlFiltroEstado, BLL_ContactoComercial.Estados, "Todos");
            CargarCatalogo(ddlFiltroTipo, ItemCatalogoComercial.TipoEntidad, "Todos");
            CargarOrigenes(ddlFiltroOrigen, "Todos", null);
            CargarVinculoUnico(ddlFiltroCampania, OpcionVinculoComercial.TipoCampania, "Todas");

            FiltroContactoComercial filtro = new FiltroContactoComercial
            {
                Estado = LeerTexto("estado"),
                TipoEntidad = LeerTexto("tipo"),
                IdOrigenUsuario = LeerEntero("origen"),
                IdCampaniaComercial = LeerEntero("campania"),
                Texto = LeerTexto("q")
            };

            Seleccionar(ddlFiltroEstado, filtro.Estado);
            Seleccionar(ddlFiltroTipo, filtro.TipoEntidad);
            Seleccionar(ddlFiltroOrigen, filtro.IdOrigenUsuario.HasValue ? filtro.IdOrigenUsuario.Value.ToString(CultureInfo.InvariantCulture) : null);
            Seleccionar(ddlFiltroCampania, filtro.IdCampaniaComercial.HasValue ? filtro.IdCampaniaComercial.Value.ToString(CultureInfo.InvariantCulture) : null);
            txtFiltroTexto.Text = filtro.Texto;

            ResultadoOperacion<ResumenContactos> resumen = _bll.ObtenerResumen();
            if (!resumen.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resumen.Mensaje, true);
                rptContactos.Visible = false;
                return;
            }

            litTotal.Text = resumen.Valor.Total.ToString(CultureInfo.InvariantCulture);
            litPendientes.Text = resumen.Valor.Pendientes.ToString(CultureInfo.InvariantCulture);
            litContactados.Text = resumen.Valor.Contactados.ToString(CultureInfo.InvariantCulture);
            litInteresados.Text = resumen.Valor.Interesados.ToString(CultureInfo.InvariantCulture);
            litRechazados.Text = resumen.Valor.Rechazados.ToString(CultureInfo.InvariantCulture);
            litIncorporados.Text = resumen.Valor.Incorporados.ToString(CultureInfo.InvariantCulture);

            // A2.
            if (resumen.Valor.Total == 0)
            {
                pnlSinContactos.Visible = true;
                pnlFiltros.Visible = false;
                rptContactos.Visible = false;
                return;
            }

            ResultadoOperacion<List<ContactoComercial>> resultado = _bll.Listar(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                rptContactos.Visible = false;
                return;
            }

            // A4.
            pnlSinResultados.Visible = resultado.Valor.Count == 0;
            rptContactos.Visible = resultado.Valor.Count > 0;
            litCantidad.Text = resultado.Valor.Count == 0
                ? string.Empty
                : resultado.Valor.Count + (resultado.Valor.Count == 1 ? " contacto" : " contactos") + (filtro.TieneCriterios ? " para los filtros aplicados." : ".");
            rptContactos.DataSource = resultado.Valor;
            rptContactos.DataBind();
        }

        protected void btnAplicarFiltros_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            List<string> partes = new List<string>();
            Agregar(partes, "estado", ddlFiltroEstado.SelectedValue);
            Agregar(partes, "tipo", ddlFiltroTipo.SelectedValue);
            Agregar(partes, "origen", ddlFiltroOrigen.SelectedValue);
            Agregar(partes, "campania", ddlFiltroCampania.SelectedValue);
            Agregar(partes, "q", txtFiltroTexto.Text);
            Redirigir(ArmarUrl(Pagina, partes), null);
        }

        // ------------------------------------------------------------------
        // Detalle (A5, A9)
        // ------------------------------------------------------------------

        private void CargarDetalle(int idContacto)
        {
            ContactoComercial contacto = _bll.Obtener(idContacto);
            if (contacto == null)
            {
                Redirigir(Pagina, "El contacto que buscás no existe.", true);
                return;
            }

            pnlDetalle.Visible = true;
            litTitulo.Text = "Detalle de contacto comercial";
            litBajada.Text = "Información del contacto, sus vinculaciones y todo el seguimiento registrado.";
            litNombre.Text = contacto.NombreCompleto;
            lblEstado.Text = BLL_ContactoComercial.TextoEstado(contacto.EstadoContacto);
            lblEstado.CssClass = "comercial-badge " + BLL_ContactoComercial.ClaseEstado(contacto.EstadoContacto);
            litTipo.Text = Catalogos.Nombre(ItemCatalogoComercial.TipoEntidad, contacto.TipoEntidad);
            litEntidad.Text = TextoOGuion(contacto.EntidadRepresentada);
            litMedio.Text = Catalogos.Nombre(ItemCatalogoComercial.MedioContacto, contacto.MedioContacto);
            litCorreo.Text = TextoOGuion(contacto.CorreoElectronico);
            litTelefono.Text = TextoOGuion(contacto.Telefono);
            litOtroDato.Text = TextoOGuion(contacto.OtroDatoContacto);
            litOrigen.Text = TextoOGuion(contacto.NombreOrigen);
            litAlianza.Text = TextoOGuion(contacto.NombreAlianza);
            litAlta.Text = contacto.FechaAlta.ToString("dd/MM/yyyy HH:mm");
            litActualizacion.Text = contacto.FechaUltimaModificacion.ToString("dd/MM/yyyy HH:mm");
            litObservaciones.Text = contacto.Observaciones;

            bool tieneCampania = contacto.IdCampaniaComercial.HasValue;
            lnkCampania.Visible = tieneCampania;
            litSinCampania.Visible = !tieneCampania;
            if (tieneCampania)
            {
                lnkCampania.Text = contacto.NombreCampania;
                lnkCampania.NavigateUrl = "CampaniasComerciales.aspx?ver=" + contacto.IdCampaniaComercial.Value;
            }

            litCuenta.Text = contacto.IdUsuarioExterno.HasValue
                ? contacto.NombreUsuarioExterno + " (" + contacto.CorreoUsuarioExterno + ")"
                : (contacto.EstadoContacto == ContactoComercial.EstadoIncorporado ? "Sin asociar todavía" : "—");

            // A9.
            if (contacto.EstadoContacto == ContactoComercial.EstadoIncorporado)
            {
                pnlAvisoIncorporado.Visible = true;
                litAvisoIncorporado.Text = "Contacto incorporado a la plataforma" +
                    (contacto.FechaIncorporacion.HasValue ? " el " + contacto.FechaIncorporacion.Value.ToString("dd/MM/yyyy") : string.Empty) +
                    ". Se conserva como antecedente comercial de origen y seguimiento; no reemplaza la cuenta del usuario." +
                    (contacto.IdUsuarioExterno.HasValue ? string.Empty : " Podés asociarle la cuenta desde \"Editar contacto\".");
            }

            List<SeguimientoContacto> seguimientos = _bll.ListarSeguimientos(idContacto);
            pnlSinSeguimiento.Visible = seguimientos.Count == 0;
            rptSeguimientos.DataSource = seguimientos;
            rptSeguimientos.DataBind();

            lnkEditar.NavigateUrl = Pagina + "?editar=" + idContacto;
            lnkSeguimiento.NavigateUrl = Pagina + "?seguimiento=" + idContacto;
        }

        // ------------------------------------------------------------------
        // Formulario de contacto
        // ------------------------------------------------------------------

        private void CargarFormulario(int? idContacto)
        {
            pnlFormulario.Visible = true;
            CargarCatalogo(ddlTipo, ItemCatalogoComercial.TipoEntidad, "Elegí un tipo");
            CargarCatalogo(ddlMedio, ItemCatalogoComercial.MedioContacto, "Elegí un medio");
            CargarDiccionario(ddlEstado, BLL_ContactoComercial.Estados, null);
            CargarVinculoUnico(ddlCampania, OpcionVinculoComercial.TipoCampania, "Ninguna");
            CargarVinculoUnico(ddlAlianza, OpcionVinculoComercial.TipoAlianza, "Ninguna");

            ContactoComercial contacto = null;
            if (idContacto.HasValue)
            {
                contacto = _bll.Obtener(idContacto.Value);
                if (contacto == null)
                {
                    Redirigir(Pagina, "El contacto que buscás no existe.", true);
                    return;
                }
            }

            CargarOrigenes(ddlOrigen, "Elegí un origen", contacto);
            pnlSinCatalogos.Visible = ddlTipo.Items.Count <= 1 || ddlMedio.Items.Count <= 1 || ddlOrigen.Items.Count <= 1;
            btnGuardar.Enabled = !pnlSinCatalogos.Visible;

            if (contacto == null)
            {
                litTitulo.Text = "Agregar contacto";
                ddlEstado.SelectedValue = ContactoComercial.EstadoPendiente;
                MostrarCuenta(null, null);
                return;
            }

            litTitulo.Text = "Editar contacto";
            litFormularioEyebrow.Text = "Edición";
            litFormularioTitulo.Text = contacto.NombreCompleto;
            litEtiquetaObservaciones.Text = "Observaciones *";
            pnlObservacionCambio.Visible = true;
            txtNombre.Text = contacto.NombreContacto;
            txtApellido.Text = contacto.ApellidoContacto;
            Seleccionar(ddlTipo, contacto.TipoEntidad);
            txtEntidad.Text = contacto.EntidadRepresentada;
            Seleccionar(ddlMedio, contacto.MedioContacto);
            txtCorreo.Text = contacto.CorreoElectronico;
            txtTelefono.Text = contacto.Telefono;
            txtOtroDato.Text = contacto.OtroDatoContacto;
            Seleccionar(ddlOrigen, contacto.IdOrigenUsuario.ToString(CultureInfo.InvariantCulture));
            Seleccionar(ddlEstado, contacto.EstadoContacto);
            txtObservaciones.Text = contacto.Observaciones;
            AsegurarOpcion(ddlCampania, contacto.IdCampaniaComercial, contacto.NombreCampania);
            AsegurarOpcion(ddlAlianza, contacto.IdAlianzaEstrategica, contacto.NombreAlianza);
            MostrarCuenta(contacto.IdUsuarioExterno,
                contacto.IdUsuarioExterno.HasValue ? contacto.NombreUsuarioExterno + " (" + contacto.CorreoUsuarioExterno + ")" : null);
            lnkCancelar.NavigateUrl = Pagina + "?ver=" + contacto.IdContactoComercial;
        }

        protected void btnBuscarCuenta_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            ResultadoOperacion<CuentaParaVincular> resultado = _bll.BuscarCuenta(txtCorreoCuenta.Text);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                return;
            }

            CuentaParaVincular cuenta = resultado.Valor;
            MostrarCuenta(cuenta.IdUsuarioExterno, cuenta.Nombre + " " + cuenta.Apellido + " (" + cuenta.CorreoElectronico + ")");
            if (ddlEstado.SelectedValue != ContactoComercial.EstadoIncorporado)
            {
                ddlEstado.SelectedValue = ContactoComercial.EstadoIncorporado;
                MostrarMensaje(pnlMensaje, litMensaje, "Encontramos la cuenta. El estado comercial pasó a \"incorporado a la plataforma\": guardá para confirmar.", false);
            }
        }

        protected void lnkQuitarCuenta_Click(object sender, EventArgs e)
        {
            if (TieneAcceso())
            {
                MostrarCuenta(null, null);
            }
        }

        private void MostrarCuenta(int? idUsuario, string descripcion)
        {
            IdCuentaElegida = idUsuario;
            pnlCuentaElegida.Visible = idUsuario.HasValue;
            pnlBuscarCuenta.Visible = !idUsuario.HasValue;
            litCuentaElegida.Text = idUsuario.HasValue ? "Cuenta asociada: " + descripcion + ". " : string.Empty;
            txtCorreoCuenta.Text = string.Empty;
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int? idEditar = LeerEntero("editar");
            ContactoComercial contacto = new ContactoComercial
            {
                IdContactoComercial = idEditar ?? 0,
                NombreContacto = txtNombre.Text,
                ApellidoContacto = txtApellido.Text,
                TipoEntidad = Vacio(ddlTipo.SelectedValue),
                EntidadRepresentada = txtEntidad.Text,
                MedioContacto = Vacio(ddlMedio.SelectedValue),
                CorreoElectronico = txtCorreo.Text,
                Telefono = txtTelefono.Text,
                OtroDatoContacto = txtOtroDato.Text,
                IdOrigenUsuario = EnteroElegido(ddlOrigen) ?? 0,
                EstadoContacto = Vacio(ddlEstado.SelectedValue),
                Observaciones = txtObservaciones.Text,
                ObservacionCambioEstado = idEditar.HasValue ? txtObservacionCambio.Text : null,
                IdCampaniaComercial = EnteroElegido(ddlCampania),
                IdAlianzaEstrategica = EnteroElegido(ddlAlianza),
                IdUsuarioExterno = IdCuentaElegida
            };

            ResultadoOperacion<int> resultado = _bll.Guardar(contacto, IdUsuarioInterno);
            if (!resultado.Exitoso)
            {
                // A13 a A19: se queda en el formulario.
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                int? idDuplicado = BLL_ContactoComercial.IdDuplicado(resultado);
                lnkDuplicado.Visible = idDuplicado.HasValue;
                if (idDuplicado.HasValue)
                {
                    lnkDuplicado.NavigateUrl = Pagina + "?ver=" + idDuplicado.Value;
                    lnkDuplicado.Target = "_blank";
                }

                return;
            }

            // Paso 29 (alta: listado actualizado) o A6 paso 9 (edición: detalle).
            Redirigir(idEditar.HasValue ? Pagina + "?ver=" + resultado.Valor : Pagina, resultado.Mensaje);
        }

        // ------------------------------------------------------------------
        // Seguimiento (A7, A8)
        // ------------------------------------------------------------------

        private void CargarFormularioSeguimiento(int idContacto)
        {
            ContactoComercial contacto = _bll.Obtener(idContacto);
            if (contacto == null)
            {
                Redirigir(Pagina, "El contacto que buscás no existe.", true);
                return;
            }

            pnlSeguimiento.Visible = true;
            litTitulo.Text = "Registrar seguimiento";
            litSeguimientoTitulo.Text = contacto.NombreCompleto;
            litSeguimientoEstado.Text = "Estado comercial actual: " + BLL_ContactoComercial.TextoEstado(contacto.EstadoContacto) + ".";
            txtFechaInteraccion.Text = FechaParaCampo(DateTime.Today);
            CargarCatalogo(ddlTipoInteraccion, ItemCatalogoComercial.TipoInteraccion, "Elegí un tipo");
            ddlEstadoResultante.Items.Clear();
            ddlEstadoResultante.Items.Add(new ListItem("Sin cambios (" + BLL_ContactoComercial.TextoEstado(contacto.EstadoContacto) + ")", string.Empty));
            foreach (KeyValuePair<string, string> estado in BLL_ContactoComercial.Estados.Where(p => p.Key != contacto.EstadoContacto))
            {
                ddlEstadoResultante.Items.Add(new ListItem(estado.Value, estado.Key));
            }

            lnkCancelarSeguimiento.NavigateUrl = Pagina + "?ver=" + idContacto;
        }

        protected void btnGuardarSeguimiento_Click(object sender, EventArgs e)
        {
            int? idContacto = LeerEntero("seguimiento");
            if (!TieneAcceso() || !idContacto.HasValue)
            {
                return;
            }

            SeguimientoContacto seguimiento = new SeguimientoContacto
            {
                IdContactoComercial = idContacto.Value,
                TipoSeguimiento = Vacio(ddlTipoInteraccion.SelectedValue),
                Descripcion = txtObservacion.Text,
                Resultado = txtRespuesta.Text,
                InteresManifestado = txtInteres.Text,
                ProximaAccion = txtAccionPendiente.Text,
                EstadoResultante = Vacio(ddlEstadoResultante.SelectedValue)
            };

            ResultadoOperacion resultado = _bll.RegistrarSeguimiento(seguimiento, txtFechaInteraccion.Text, txtFechaAccion.Text, IdUsuarioInterno);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                return;
            }

            Redirigir(Pagina + "?ver=" + idContacto.Value, resultado.Mensaje);
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        private void CargarOrigenes(DropDownList lista, string textoVacio, ContactoComercial contacto)
        {
            lista.Items.Clear();
            lista.Items.Add(new ListItem(textoVacio, string.Empty));
            ResultadoOperacion<List<OrigenUsuario>> origenes = _bllOrigen.Listar(contacto == null && textoVacio != "Todos");
            if (!origenes.Exitoso)
            {
                return;
            }

            foreach (OrigenUsuario origen in origenes.Valor)
            {
                bool disponible = origen.Activo || (contacto != null && contacto.IdOrigenUsuario == origen.IdOrigenUsuario) || textoVacio == "Todos";
                if (disponible)
                {
                    lista.Items.Add(new ListItem(origen.NombreOrigen + (origen.Activo ? string.Empty : " (dado de baja)"),
                        origen.IdOrigenUsuario.ToString(CultureInfo.InvariantCulture)));
                }
            }
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
                lista.Items.Add(new ListItem((nombre ?? "#" + valor) + " (no disponible para nuevas asociaciones)", valor));
            }

            Seleccionar(lista, valor);
        }

        protected static string DatoPrincipal(object dato)
        {
            ContactoComercial c = (ContactoComercial)dato;
            switch (c.MedioContacto)
            {
                case "Correo": return c.CorreoElectronico ?? string.Empty;
                case "Telefono":
                case "WhatsApp": return c.Telefono ?? string.Empty;
                default: return c.OtroDatoContacto ?? c.CorreoElectronico ?? c.Telefono ?? string.Empty;
            }
        }

        protected string TituloSeguimiento(object dato)
        {
            SeguimientoContacto s = (SeguimientoContacto)dato;
            if (s.TipoSeguimiento == SeguimientoContacto.TipoCambioEstado)
            {
                return "Cambio de estado: " + BLL_ContactoComercial.TextoEstado(s.EstadoAnterior) + " → " + BLL_ContactoComercial.TextoEstado(s.EstadoResultante);
            }

            string titulo = Catalogos.Nombre(ItemCatalogoComercial.TipoInteraccion, s.TipoSeguimiento) + " del " + s.FechaSeguimiento.ToString("dd/MM/yyyy");
            return s.EstadoResultante != null
                ? titulo + " · " + BLL_ContactoComercial.TextoEstado(s.EstadoAnterior) + " → " + BLL_ContactoComercial.TextoEstado(s.EstadoResultante)
                : titulo;
        }

        protected static string MetaSeguimiento(object dato)
        {
            SeguimientoContacto s = (SeguimientoContacto)dato;
            return "Registrado el " + s.FechaRegistro.ToString("dd/MM/yyyy HH:mm") + (s.NombreResponsable != null ? " por " + s.NombreResponsable : string.Empty);
        }

        protected static string DetalleSeguimiento(object dato)
        {
            SeguimientoContacto s = (SeguimientoContacto)dato;
            List<string> partes = new List<string>();
            if (s.Resultado != null) partes.Add("<dt>Respuesta recibida</dt><dd>" + HttpUtility.HtmlEncode(s.Resultado) + "</dd>");
            if (s.InteresManifestado != null) partes.Add("<dt>Interés manifestado</dt><dd>" + HttpUtility.HtmlEncode(s.InteresManifestado) + "</dd>");
            if (s.ProximaAccion != null)
            {
                partes.Add("<dt>Acción pendiente</dt><dd>" + HttpUtility.HtmlEncode(s.ProximaAccion) +
                    (s.FechaProximaAccion.HasValue ? " (para el " + s.FechaProximaAccion.Value.ToString("dd/MM/yyyy") + ")" : string.Empty) + "</dd>");
            }

            return partes.Count == 0
                ? string.Empty
                : "<dl class=\"comercial-datos\">" + string.Join(string.Empty, partes.Select(p => "<div>" + p + "</div>")) + "</dl>";
        }
    }
}
