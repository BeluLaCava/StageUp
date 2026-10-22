using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno.Comercializacion
{






    public partial class ContenidoPromocionalPagina : PaginaComercial
    {
        private const string Pagina = "ContenidoPromocional.aspx";

        private readonly BLL_ContenidoPromocional _bll = new BLL_ContenidoPromocional();

        protected override string PermisoRequerido
        {
            get { return BLL_ContenidoPromocional.PermisoGestionar; }
        }

        private bool PuedeReactivar
        {
            get { return GestorDeSesion.TienePermisoInterno(BLL_ContenidoPromocional.PermisoReactivar); }
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
            else if (LeerEntero("ver").HasValue) CargarDetalle(LeerEntero("ver").Value);
            else CargarListado();
        }





        private void CargarListado()
        {
            pnlListado.Visible = true;
            CargarDiccionario(ddlFiltroEstado, BLL_ContenidoPromocional.Estados, "Todos");
            CargarCatalogo(ddlFiltroTipo, ItemCatalogoComercial.TipoContenido, "Todos");
            CargarCatalogo(ddlFiltroCanal, ItemCatalogoComercial.CanalUso, "Todos");
            CargarVinculoUnico(ddlFiltroCampania, OpcionVinculoComercial.TipoCampania, "Todas");

            FiltroContenidoPromocional filtro = new FiltroContenidoPromocional
            {
                Vista = LeerTexto("vista") ?? FiltroContenidoPromocional.VistaVigentes,
                Estado = LeerTexto("estado"),
                TipoContenido = LeerTexto("tipo"),
                CanalPublicacion = LeerTexto("canal"),
                IdCampaniaComercial = LeerEntero("campania"),
                Texto = LeerTexto("q")
            };

            if (filtro.Estado == ContenidoPromocional.EstadoDadoDeBaja)
            {
                filtro.Vista = FiltroContenidoPromocional.VistaTodos;
            }

            Seleccionar(ddlFiltroVista, filtro.Vista);
            Seleccionar(ddlFiltroEstado, filtro.Estado);
            Seleccionar(ddlFiltroTipo, filtro.TipoContenido);
            Seleccionar(ddlFiltroCanal, filtro.CanalPublicacion);
            Seleccionar(ddlFiltroCampania, filtro.IdCampaniaComercial.HasValue ? filtro.IdCampaniaComercial.Value.ToString(CultureInfo.InvariantCulture) : null);
            txtFiltroTexto.Text = filtro.Texto;

            ResultadoOperacion<ResumenContenidos> resumen = _bll.ObtenerResumen();
            if (!resumen.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resumen.Mensaje, true);
                rptContenidos.Visible = false;
                return;
            }

            litActivos.Text = resumen.Valor.Activos.ToString(CultureInfo.InvariantCulture);
            litEnRevision.Text = resumen.Valor.EnRevision.ToString(CultureInfo.InvariantCulture);
            litDesactualizados.Text = resumen.Valor.Desactualizados.ToString(CultureInfo.InvariantCulture);
            litBajas.Text = resumen.Valor.Bajas.ToString(CultureInfo.InvariantCulture);


            if (resumen.Valor.Total == 0)
            {
                pnlSinContenidos.Visible = true;
                pnlFiltros.Visible = false;
                rptContenidos.Visible = false;
                return;
            }

            ResultadoOperacion<List<ContenidoPromocional>> resultado = _bll.Listar(filtro);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje, true);
                rptContenidos.Visible = false;
                return;
            }


            pnlSinResultados.Visible = resultado.Valor.Count == 0;
            rptContenidos.Visible = resultado.Valor.Count > 0;
            litCantidad.Text = resultado.Valor.Count == 0
                ? string.Empty
                : resultado.Valor.Count + (resultado.Valor.Count == 1 ? " pieza" : " piezas") + (filtro.TieneCriterios ? " para los filtros aplicados." : ".");
            rptContenidos.DataSource = resultado.Valor;
            rptContenidos.DataBind();
        }

        protected void btnAplicarFiltros_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            List<string> partes = new List<string>();
            if (ddlFiltroVista.SelectedValue != FiltroContenidoPromocional.VistaVigentes)
            {
                Agregar(partes, "vista", ddlFiltroVista.SelectedValue);
            }

            Agregar(partes, "estado", ddlFiltroEstado.SelectedValue);
            Agregar(partes, "tipo", ddlFiltroTipo.SelectedValue);
            Agregar(partes, "canal", ddlFiltroCanal.SelectedValue);
            Agregar(partes, "campania", ddlFiltroCampania.SelectedValue);
            Agregar(partes, "q", txtFiltroTexto.Text);
            Redirigir(ArmarUrl(Pagina, partes), null);
        }





        private void CargarDetalle(int id)
        {
            ContenidoPromocional c = _bll.Obtener(id);
            if (c == null)
            {
                Redirigir(Pagina, "El contenido que buscás no existe.", true);
                return;
            }

            pnlDetalle.Visible = true;
            litTitulo.Text = "Detalle de contenido promocional";
            litBajada.Text = "Información completa de la pieza, su archivo o enlace y su estado.";
            litNombre.Text = c.Titulo;
            lblEstado.Text = BLL_ContenidoPromocional.TextoEstado(c.EstadoContenido);
            lblEstado.CssClass = "comercial-badge " + BLL_ContenidoPromocional.ClaseEstado(c.EstadoContenido);
            litDescripcion.Text = TextoOGuion(c.Descripcion);
            litTipo.Text = Catalogos.Nombre(ItemCatalogoComercial.TipoContenido, c.TipoContenido);
            litCanal.Text = Catalogos.Nombre(ItemCatalogoComercial.CanalUso, c.CanalPublicacion);
            litResponsable.Text = TextoOGuion(c.NombreResponsable);
            litAlta.Text = c.FechaAlta.ToString("dd/MM/yyyy HH:mm");
            litActualizacion.Text = c.FechaUltimaModificacion.ToString("dd/MM/yyyy HH:mm");

            MostrarVinculo(lnkCampania, litSinCampania, c.IdCampaniaComercial, c.NombreCampania,
                "CampaniasComerciales.aspx?ver=", BLL_CampaniaComercial.PermisoGestionar);
            MostrarVinculo(lnkAlianza, litSinAlianza, c.IdAlianzaEstrategica, c.NombreAlianza,
                "AlianzasEstrategicas.aspx?ver=", BLL_AlianzaEstrategica.PermisoGestionar);

            lnkArchivo.Visible = c.TieneArchivo;
            litSinArchivo.Visible = !c.TieneArchivo;
            if (c.TieneArchivo)
            {
                lnkArchivo.Text = "Descargar " + c.NombreArchivoOriginal + (c.TamanioArchivo.HasValue ? " (" + TextoTamanio(c.TamanioArchivo.Value) + ")" : string.Empty);
                lnkArchivo.NavigateUrl = "DescargarContenido.ashx?id=" + c.IdContenidoPromocional;
            }

            imgVista.Visible = c.EsImagen;
            imgVista.ImageUrl = "DescargarContenido.ashx?id=" + c.IdContenidoPromocional + "&ver=1";

            lnkEnlace.Visible = c.UrlContenido != null;
            litSinEnlace.Visible = c.UrlContenido == null;
            if (c.UrlContenido != null)
            {
                lnkEnlace.Text = Resumir(c.UrlContenido, 70);
                lnkEnlace.NavigateUrl = BLL_ContenidoPromocional.EsUrlValida(c.UrlContenido) ? c.UrlContenido : null;
            }


            if (c.EstadoContenido == ContenidoPromocional.EstadoDadoDeBaja)
            {
                pnlAviso.Visible = true;
                litAviso.Text = "Este contenido fue dado de baja" +
                    (c.FechaBaja.HasValue ? " el " + c.FechaBaja.Value.ToString("dd/MM/yyyy") : string.Empty) +
                    ": no está vigente para uso comercial. Se conserva para consulta histórica.";
            }
            else if (c.EstadoContenido == ContenidoPromocional.EstadoDesactualizado)
            {
                pnlAviso.Visible = true;
                litAviso.Text = "Este contenido está desactualizado: requiere revisión antes de usarse en nuevas acciones comerciales. Podés editarlo, cambiar su estado o darlo de baja.";
            }
            else if (c.EstadoContenido == ContenidoPromocional.EstadoEnRevision)
            {
                pnlAviso.Visible = true;
                litAviso.Text = "Este contenido está en revisión: todavía no se considera vigente para uso comercial.";
            }



            bool dadoDeBaja = !c.Activo;
            pnlCambioEstado.Visible = !dadoDeBaja || PuedeReactivar;
            litEtiquetaEstado.Text = dadoDeBaja ? "Reactivar como" : "Cambiar estado";
            btnCambiarEstado.Text = dadoDeBaja ? "Reactivar" : "Actualizar estado";
            ddlNuevoEstado.Items.Clear();
            foreach (KeyValuePair<string, string> estado in BLL_ContenidoPromocional.Estados)
            {
                if (estado.Key != c.EstadoContenido && estado.Key != ContenidoPromocional.EstadoDadoDeBaja)
                {
                    ddlNuevoEstado.Items.Add(new ListItem(estado.Value, estado.Key));
                }
            }

            lnkEditar.Visible = c.Activo;
            lnkEditar.NavigateUrl = Pagina + "?editar=" + id;
            btnDarDeBaja.Visible = c.Activo;
        }

        protected void btnCambiarEstado_Click(object sender, EventArgs e)
        {
            int? id = LeerEntero("ver");
            if (!TieneAcceso() || !id.HasValue)
            {
                return;
            }

            ResultadoOperacion resultado = _bll.CambiarEstado(id.Value, Vacio(ddlNuevoEstado.SelectedValue), IdUsuarioInterno, PuedeReactivar);
            Redirigir(Pagina + "?ver=" + id.Value, resultado.Mensaje, !resultado.Exitoso);
        }

        protected void btnDarDeBaja_Click(object sender, EventArgs e)
        {
            int? id = LeerEntero("ver");
            if (!TieneAcceso() || !id.HasValue)
            {
                return;
            }

            ResultadoOperacion resultado = _bll.CambiarEstado(id.Value, ContenidoPromocional.EstadoDadoDeBaja, IdUsuarioInterno, PuedeReactivar);
            Redirigir(Pagina + "?ver=" + id.Value, resultado.Mensaje, !resultado.Exitoso);
        }





        private void CargarFormulario(int? id)
        {
            ContenidoPromocional c = null;
            if (id.HasValue)
            {
                c = _bll.Obtener(id.Value);
                if (c == null)
                {
                    Redirigir(Pagina, "El contenido que buscás no existe.", true);
                    return;
                }

                if (!c.Activo)
                {
                    Redirigir(Pagina + "?ver=" + id.Value, "Un contenido dado de baja solo se puede consultar.", true);
                    return;
                }
            }

            pnlFormulario.Visible = true;
            CargarCatalogo(ddlTipo, ItemCatalogoComercial.TipoContenido, "Elegí un tipo");
            CargarCatalogo(ddlCanal, ItemCatalogoComercial.CanalUso, "Elegí un canal");
            CargarVinculoUnico(ddlCampania, OpcionVinculoComercial.TipoCampania, "Sin campaña");
            CargarVinculoUnico(ddlAlianza, OpcionVinculoComercial.TipoAlianza, "Sin alianza");
            ddlEstado.Items.Clear();
            foreach (KeyValuePair<string, string> estado in BLL_ContenidoPromocional.Estados)
            {
                if (estado.Key != ContenidoPromocional.EstadoDadoDeBaja)
                {
                    ddlEstado.Items.Add(new ListItem(estado.Value, estado.Key));
                }
            }

            pnlSinCatalogos.Visible = ddlTipo.Items.Count <= 1 || ddlCanal.Items.Count <= 1;
            btnGuardar.Enabled = !pnlSinCatalogos.Visible;

            if (c == null)
            {
                litTitulo.Text = "Agregar contenido";
                Seleccionar(ddlEstado, ContenidoPromocional.EstadoEnRevision);
                int? campania = LeerEntero("campania");
                if (campania.HasValue)
                {
                    Seleccionar(ddlCampania, campania.Value.ToString(CultureInfo.InvariantCulture));
                }

                return;
            }

            litTitulo.Text = "Editar contenido";
            litFormularioEyebrow.Text = "Edición";
            litFormularioTitulo.Text = c.Titulo;
            txtTituloPieza.Text = c.Titulo;
            txtDescripcion.Text = c.Descripcion;
            Seleccionar(ddlTipo, c.TipoContenido);
            Seleccionar(ddlCanal, c.CanalPublicacion);
            AsegurarOpcion(ddlCampania, c.IdCampaniaComercial, c.NombreCampania);
            AsegurarOpcion(ddlAlianza, c.IdAlianzaEstrategica, c.NombreAlianza);
            Seleccionar(ddlEstado, c.EstadoContenido);
            txtEnlace.Text = c.UrlContenido;
            pnlArchivoActual.Visible = c.TieneArchivo;
            chkQuitarArchivo.Text = "Quitar el archivo actual (" + c.NombreArchivoOriginal + ")";
            lnkCancelar.NavigateUrl = Pagina + "?ver=" + c.IdContenidoPromocional;
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int? idEditar = LeerEntero("editar");
            ContenidoPromocional contenido = new ContenidoPromocional
            {
                IdContenidoPromocional = idEditar ?? 0,
                Titulo = txtTituloPieza.Text,
                Descripcion = txtDescripcion.Text,
                TipoContenido = Vacio(ddlTipo.SelectedValue),
                CanalPublicacion = Vacio(ddlCanal.SelectedValue),
                IdCampaniaComercial = EnteroElegido(ddlCampania),
                IdAlianzaEstrategica = EnteroElegido(ddlAlianza),
                EstadoContenido = Vacio(ddlEstado.SelectedValue),
                UrlContenido = txtEnlace.Text,
                QuitarArchivo = idEditar.HasValue && pnlArchivoActual.Visible && chkQuitarArchivo.Checked
            };


            string archivoGuardado = null;
            if (fuArchivo.HasFile)
            {
                byte[] bytes;
                using (MemoryStream memoria = new MemoryStream())
                {
                    fuArchivo.PostedFile.InputStream.CopyTo(memoria);
                    bytes = memoria.ToArray();
                }

                ResultadoOperacion<ArchivoPromocional> archivo = BLL_ContenidoPromocional.ValidarArchivo(fuArchivo.FileName, bytes);
                if (!archivo.Exitoso)
                {
                    MostrarMensaje(pnlMensaje, litMensaje, archivo.Mensaje, true);
                    return;
                }

                try
                {
                    archivoGuardado = ArchivoContenidoPromocional.Guardar(archivo.Valor);
                }
                catch (Exception)
                {
                    MostrarMensaje(pnlMensaje, litMensaje, "No fue posible guardar el archivo en este momento. Intentá nuevamente.", true);
                    return;
                }

                contenido.RutaArchivo = archivoGuardado;
                contenido.NombreArchivoOriginal = archivo.Valor.NombreOriginal;
                contenido.TipoMimeArchivo = archivo.Valor.TipoMime;
                contenido.TamanioArchivo = archivo.Valor.Contenido.Length;
                contenido.QuitarArchivo = false;
            }

            string archivoAnterior;
            ResultadoOperacion<int> resultado = _bll.Guardar(contenido, IdUsuarioInterno, out archivoAnterior);
            if (!resultado.Exitoso)
            {
                if (archivoGuardado != null)
                {
                    ArchivoContenidoPromocional.Borrar(archivoGuardado);
                }

                MostrarMensaje(pnlMensaje, litMensaje, resultado.Mensaje +
                    (archivoGuardado != null ? " Volvé a elegir el archivo antes de guardar." : string.Empty), true);
                return;
            }

            if (archivoAnterior != null)
            {
                ArchivoContenidoPromocional.Borrar(archivoAnterior);
            }

            Redirigir(idEditar.HasValue ? Pagina + "?ver=" + resultado.Valor : Pagina, resultado.Mensaje);
        }





        private static void MostrarVinculo(HyperLink enlace, Literal vacio, int? id, string nombre, string url, string permiso)
        {
            enlace.Visible = id.HasValue;
            vacio.Visible = !id.HasValue;
            if (id.HasValue)
            {
                enlace.Text = nombre;
                enlace.NavigateUrl = GestorDeSesion.TienePermisoInterno(permiso) ? url + id.Value : null;
            }
        }

        private static string TextoTamanio(int bytes)
        {
            return bytes >= 1024 * 1024
                ? (bytes / 1024m / 1024m).ToString("0.0", CultureInfo.InvariantCulture) + " MB"
                : Math.Max(1, bytes / 1024).ToString(CultureInfo.InvariantCulture) + " KB";
        }
    }
}
