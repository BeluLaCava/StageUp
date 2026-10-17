using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // ABM real de las novedades públicas y su envío por newsletter (ítem 38
    // del checklist de correcciones). Antes de esta clase, GestionNovedades
    // solo mostraba un formulario con datos de ejemplo fijos, sin persistir
    // nada, y StageUp.Servicios.ServicioCorreo.EnviarNewsletter no tenía
    // ningún llamador en todo el proyecto.
    // Vigencia (script 75): cada novedad tiene fecha de publicación y,
    // opcionalmente, de vencimiento. Se muestra en la página pública solo
    // mientras está Vigente; antes de su fecha queda Programada y después,
    // Vencida. La baja es lógica (DadaDeBaja) y se puede restaurar como
    // borrador. La imagen se sube como archivo: solo se aceptan rutas de
    // Content/Uploads/Novedades generadas por el sistema.
    public class BLL_Novedad
    {
        private const int LongitudMaximaTitulo = 180;
        private const int LongitudMaximaResumen = 300;

        public const string CarpetaImagenes = "~/Content/Uploads/Novedades/";
        private static readonly Regex FormatoImagenSubida = new Regex(@"^~/Content/Uploads/Novedades/[a-f0-9]{32}\.jpg$", RegexOptions.Compiled);

        public static string ObtenerEtiquetaEstado(string estado)
        {
            switch (estado)
            {
                case Novedad.EstadoBorrador: return "Borrador";
                case Novedad.EstadoProgramada: return "Programada";
                case Novedad.EstadoVigente: return "Vigente";
                case Novedad.EstadoVencida: return "Vencida";
                case Novedad.EstadoDadaDeBaja: return "Dada de baja";
                default: return estado ?? string.Empty;
            }
        }

        public static bool EsImagenSubida(string urlImagen)
        {
            return !string.IsNullOrEmpty(urlImagen) && FormatoImagenSubida.IsMatch(urlImagen);
        }

        // Mismos valores que ofrece el desplegable "Categoría" en
        // Interno/GestionNovedades.aspx.
        public static readonly string[] Categorias =
        {
            "Institucional", "Catalogo", "Consejos", "Comunidad"
        };

        // Mismos valores que ofrece el desplegable "Destinatarios" en
        // Interno/GestionNovedades.aspx.
        // Activos y Todos incluyen también a los suscriptores sin cuenta;
        // Suscriptores es solo para ellos (script 78).
        public static readonly string[] DestinatariosNewsletter =
        {
            "Activos", "Gestores", "Solicitantes", "Suscriptores", "Todos"
        };

        private readonly MPP_Novedad _mpp = new MPP_Novedad();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly ServicioCorreo _servicioCorreo = new ServicioCorreo();

        private const string TipoEntidadBitacora = "Novedad";

        public List<Novedad> ListarPublicadas()
        {
            try
            {
                return _mpp.ListarPublicadas();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Novedad>();
            }
        }

        public List<Novedad> Listar(string estado = null)
        {
            try
            {
                return _mpp.Listar(estado);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Novedad>();
            }
        }

        public Novedad ObtenerPorId(int idNovedad)
        {
            try
            {
                return _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion<int> Registrar(
            string titulo, string resumen, string contenido, string categoria, string urlImagen,
            DateTime? fechaPublicacion, DateTime? fechaVencimiento, bool publicado, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                string tituloNormalizado, resumenNormalizado, contenidoNormalizado, categoriaNormalizada, urlImagenNormalizada;
                ResultadoOperacion validacion = ValidarDatos(
                    titulo, resumen, contenido, categoria, urlImagen,
                    out tituloNormalizado, out resumenNormalizado, out contenidoNormalizado,
                    out categoriaNormalizada, out urlImagenNormalizada);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                if (urlImagenNormalizada != null && !EsImagenSubida(urlImagenNormalizada))
                {
                    return ResultadoOperacion<int>.Error("La imagen no es válida. Subila de nuevo desde el formulario.");
                }

                DateTime? fechaPublicacionFinal = publicado ? (fechaPublicacion ?? DateTime.Now) : fechaPublicacion;
                ResultadoOperacion vigencia = ValidarVigencia(fechaPublicacionFinal, fechaVencimiento, publicado);
                if (!vigencia.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(vigencia.Mensaje);
                }

                int idNovedad = _mpp.Insertar(new Novedad
                {
                    Titulo = tituloNormalizado,
                    Resumen = resumenNormalizado,
                    Contenido = contenidoNormalizado,
                    Categoria = categoriaNormalizada,
                    UrlImagen = urlImagenNormalizada,
                    Publicado = publicado,
                    FechaPublicacion = fechaPublicacionFinal,
                    FechaVencimiento = fechaVencimiento
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", TipoEntidadBitacora, idNovedad,
                    "Alta de la novedad \"" + tituloNormalizado + "\"" + DescribirVigencia(publicado, fechaPublicacionFinal, fechaVencimiento) + ".");

                return ResultadoOperacion<int>.Ok(idNovedad, !publicado
                    ? "La novedad se guardó como borrador."
                    : fechaPublicacionFinal > DateTime.Now
                        ? "La novedad quedó programada: se va a publicar el " + FormatearFechaHora(fechaPublicacionFinal.Value) + "."
                        : "La novedad se creó y publicó correctamente.");
            });
        }

        public ResultadoOperacion Modificar(
            int idNovedad, string titulo, string resumen, string contenido, string categoria, string urlImagen,
            DateTime? fechaPublicacion, DateTime? fechaVencimiento, bool publicado, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Novedad actual = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (actual == null)
                {
                    return ResultadoOperacion.Error("No se encontró la novedad seleccionada.");
                }

                if (actual.Estado == Novedad.EstadoDadaDeBaja)
                {
                    return ResultadoOperacion.Error("La novedad está dada de baja. Restaurala antes de editarla.");
                }

                string tituloNormalizado, resumenNormalizado, contenidoNormalizado, categoriaNormalizada, urlImagenNormalizada;
                ResultadoOperacion validacion = ValidarDatos(
                    titulo, resumen, contenido, categoria, urlImagen,
                    out tituloNormalizado, out resumenNormalizado, out contenidoNormalizado,
                    out categoriaNormalizada, out urlImagenNormalizada);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                // Se acepta la imagen que ya tenía (aunque sea de antes de la
                // carga por archivo) o una nueva subida desde el formulario.
                if (urlImagenNormalizada != null && urlImagenNormalizada != actual.UrlImagen && !EsImagenSubida(urlImagenNormalizada))
                {
                    return ResultadoOperacion.Error("La imagen no es válida. Subila de nuevo desde el formulario.");
                }

                DateTime? fechaPublicacionFinal = publicado
                    ? (fechaPublicacion ?? actual.FechaPublicacion ?? DateTime.Now)
                    : fechaPublicacion;
                // Una novedad vencida se puede corregir sin cambiarle la fecha
                // de vencimiento (sigue vencida).
                ResultadoOperacion vigencia = ValidarVigencia(fechaPublicacionFinal, fechaVencimiento,
                    publicado && fechaVencimiento != actual.FechaVencimiento);
                if (!vigencia.Exitoso)
                {
                    return vigencia;
                }

                _mpp.Modificar(new Novedad
                {
                    IdNovedad = idNovedad,
                    Titulo = tituloNormalizado,
                    Resumen = resumenNormalizado,
                    Contenido = contenidoNormalizado,
                    Categoria = categoriaNormalizada,
                    UrlImagen = urlImagenNormalizada,
                    Publicado = publicado,
                    FechaPublicacion = fechaPublicacionFinal,
                    FechaVencimiento = fechaVencimiento
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    "Modificación de la novedad \"" + tituloNormalizado + "\"" + DescribirVigencia(publicado, fechaPublicacionFinal, fechaVencimiento) + ".");

                return ResultadoOperacion.Ok(publicado && fechaPublicacionFinal > DateTime.Now
                    ? "La novedad se actualizó y quedó programada para el " + FormatearFechaHora(fechaPublicacionFinal.Value) + "."
                    : "La novedad se actualizó correctamente.");
            });
        }

        public ResultadoOperacion Publicar(int idNovedad, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Novedad novedad = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (novedad == null)
                {
                    return ResultadoOperacion.Error("No se encontró la novedad seleccionada.");
                }

                if (novedad.Estado == Novedad.EstadoDadaDeBaja)
                {
                    return ResultadoOperacion.Error("La novedad está dada de baja. Restaurala antes de publicarla.");
                }

                if (novedad.Publicado)
                {
                    return ResultadoOperacion.Error("La novedad ya está publicada.");
                }

                DateTime fechaPublicacion = novedad.FechaPublicacion ?? DateTime.Now;
                ResultadoOperacion vigencia = ValidarVigencia(fechaPublicacion, novedad.FechaVencimiento, true);
                if (!vigencia.Exitoso)
                {
                    return ResultadoOperacion.Error(vigencia.Mensaje + " Editala para cambiar las fechas.");
                }

                _mpp.Publicar(novedad);

                bool programada = fechaPublicacion > DateTime.Now;
                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    "Publicación de la novedad \"" + novedad.Titulo + "\"" + DescribirVigencia(true, fechaPublicacion, novedad.FechaVencimiento) + ".");

                return ResultadoOperacion.Ok(programada
                    ? "La novedad quedó programada: se va a mostrar en la página pública desde el " + FormatearFechaHora(fechaPublicacion) + "."
                    : "La novedad se publicó y ya se muestra en la página pública.");
            });
        }

        public ResultadoOperacion VolverABorrador(int idNovedad, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Novedad novedad = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (novedad == null)
                {
                    return ResultadoOperacion.Error("No se encontró la novedad seleccionada.");
                }

                if (novedad.Estado == Novedad.EstadoDadaDeBaja)
                {
                    return ResultadoOperacion.Error("La novedad está dada de baja.");
                }

                if (!novedad.Publicado)
                {
                    return ResultadoOperacion.Error("La novedad ya está en borrador.");
                }

                _mpp.VolverABorrador(novedad);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    "La novedad \"" + novedad.Titulo + "\" volvió a borrador (deja de mostrarse en la página pública).");

                return ResultadoOperacion.Ok("La novedad volvió a borrador y dejó de mostrarse en la página pública.");
            });
        }

        // Baja lógica: deja de mostrarse y queda en el historial.
        public ResultadoOperacion DarDeBaja(int idNovedad, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Novedad novedad = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (novedad == null)
                {
                    return ResultadoOperacion.Error("No se encontró la novedad seleccionada.");
                }

                if (!_mpp.DarDeBaja(novedad, new UsuarioInterno { IdUsuarioInterno = idUsuarioInternoResponsable }))
                {
                    return ResultadoOperacion.Error("La novedad ya estaba dada de baja.");
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", TipoEntidadBitacora, idNovedad,
                    "Baja de la novedad \"" + novedad.Titulo + "\" (estaba " + ObtenerEtiquetaEstado(novedad.Estado).ToLowerInvariant() + ").");

                return ResultadoOperacion.Ok("La novedad se dio de baja: ya no se muestra y queda en el historial.");
            });
        }

        public ResultadoOperacion Restaurar(int idNovedad, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Novedad novedad = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (novedad == null)
                {
                    return ResultadoOperacion.Error("No se encontró la novedad seleccionada.");
                }

                if (!_mpp.Restaurar(novedad))
                {
                    return ResultadoOperacion.Error("La novedad no está dada de baja.");
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    "Restauración de la novedad \"" + novedad.Titulo + "\" como borrador.");

                return ResultadoOperacion.Ok("La novedad se restauró como borrador. Revisala y publicala cuando quieras.");
            });
        }

        // Envía el newsletter de una novedad ya publicada a los usuarios
        // externos activos de la categoría elegida, usando la plantilla de
        // StageUp.Servicios.ServicioCorreo. Solo se puede enviar una vez por
        // novedad (se marca con enviadaPorCorreo) para evitar duplicar
        // envíos si la novedad se vuelve a editar más adelante.
        public ResultadoOperacion<int> EnviarNewsletter(
            int idNovedad, string destinatarioCodigo, string urlNovedades, int idUsuarioInternoResponsable,
            string urlBaseAplicacion = null, string rutaBaseAplicacion = null)
        {
            return EjecutarProtegido<int>(() =>
            {
                Novedad novedad = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (novedad == null)
                {
                    return ResultadoOperacion<int>.Error("No se encontró la novedad seleccionada.");
                }

                if (novedad.Estado != Novedad.EstadoVigente)
                {
                    return ResultadoOperacion<int>.Error(novedad.Estado == Novedad.EstadoProgramada
                        ? "La novedad todavía no está vigente: podés enviarla por correo a partir del " + FormatearFechaHora(novedad.FechaPublicacion.Value) + "."
                        : "Solo se puede enviar por correo una novedad vigente (publicada y sin vencer).");
                }

                if (novedad.EnviadaPorCorreo)
                {
                    return ResultadoOperacion<int>.Error("Esta novedad ya se envió por correo anteriormente.");
                }

                string criterio = EsDestinatarioValido(destinatarioCodigo) ? destinatarioCodigo : "Activos";
                // Usuarios del criterio y suscriptores confirmados, sin los que
                // se dieron de baja ni los que eligieron otras categorías.
                List<DestinatarioNewsletter> destinatarios = new BLL_Newsletter().ListarDestinatarios(criterio, novedad);
                string urlImagenNewsletter = ConstruirUrlAbsoluta(novedad.UrlImagen, urlBaseAplicacion);
                string rutaImagenNewsletter = ResolverRutaFisica(novedad.UrlImagen, rutaBaseAplicacion);

                int enviados = 0;
                foreach (DestinatarioNewsletter destinatario in destinatarios)
                {
                    bool enviado = _servicioCorreo.EnviarNewsletter(
                        destinatario.CorreoElectronico,
                        destinatario.Nombre,
                        novedad.Titulo,
                        novedad.Resumen,
                        "Ver novedades",
                        urlNovedades,
                        urlImagenNewsletter,
                        rutaImagenNewsletter,
                        BLL_Newsletter.UrlPreferencias(urlBaseAplicacion, destinatario.TokenPreferencias));

                    if (enviado)
                    {
                        enviados++;
                    }
                }

                // Ítem 38 del checklist de correcciones: antes se marcaba la
                // novedad como enviada (EnviadaPorCorreo) sin importar el
                // resultado real del envío. Como el envío solo se permite
                // una vez (ver el chequeo de EnviadaPorCorreo más arriba), si
                // había destinatarios pero el envío falló para todos (por
                // ejemplo, el servidor de correo saliente estaba caído en
                // ese momento), la novedad quedaba marcada como "ya
                // enviada" para siempre, sin haber llegado a nadie y sin
                // posibilidad de reintentar. Ahora solo se marca como
                // enviada si no había destinatarios (nada que reintentar) o
                // si al menos uno recibió el correo.
                bool huboEnvioONoHabiaNadaQueEnviar = enviados > 0 || destinatarios.Count == 0;
                if (huboEnvioONoHabiaNadaQueEnviar)
                {
                    _mpp.MarcarEnviadaPorCorreo(new Novedad { IdNovedad = idNovedad, DestinatarioNewsletter = criterio });
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    huboEnvioONoHabiaNadaQueEnviar
                        ? "Envío del newsletter de la novedad \"" + novedad.Titulo + "\" a " + enviados + " de " +
                          destinatarios.Count + " destinatario(s) (categoría: " + criterio + ")."
                        : "Intento fallido de envío del newsletter de la novedad \"" + novedad.Titulo + "\": no se " +
                          "pudo entregar a ninguno de los " + destinatarios.Count + " destinatario(s) (categoría: " +
                          criterio + "). No se marcó como enviada; se puede reintentar.");

                if (destinatarios.Count == 0)
                {
                    return ResultadoOperacion<int>.Ok(0, "No hay destinatarios activos en esa categoría; no se envió ningún correo.");
                }

                if (enviados == 0)
                {
                    return ResultadoOperacion<int>.Error(
                        "No se pudo enviar el newsletter a ninguno de los " + destinatarios.Count +
                        " destinatario(s). Revisá la configuración de correo saliente e intentá de nuevo.");
                }

                return ResultadoOperacion<int>.Ok(enviados, "Se envió el newsletter a " + enviados + " de " + destinatarios.Count + " destinatario(s).");
            });
        }

        // Envío de prueba: manda el contenido tal como está cargado en el
        // formulario (sin necesidad de haberlo guardado antes) únicamente al
        // correo del usuario interno que está probando la pantalla.
        public ResultadoOperacion EnviarPrueba(
            string correoDestino, string nombreDestino, string titulo, string resumen, string urlNovedades,
            string urlImagen = null, string urlBaseAplicacion = null, string rutaBaseAplicacion = null)
        {
            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                return ResultadoOperacion.Error("Tu usuario interno no tiene un correo electrónico configurado para recibir la prueba.");
            }

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(resumen))
            {
                return ResultadoOperacion.Error("Completá el título y el resumen antes de enviar una prueba.");
            }

            string urlImagenNewsletter = ConstruirUrlAbsoluta(urlImagen, urlBaseAplicacion);
            string rutaImagenNewsletter = ResolverRutaFisica(urlImagen, rutaBaseAplicacion);

            bool enviado = _servicioCorreo.EnviarNewsletter(
                correoDestino, nombreDestino, titulo.Trim(), resumen.Trim(), "Ver novedades", urlNovedades,
                urlImagenNewsletter, rutaImagenNewsletter);

            return enviado
                ? ResultadoOperacion.Ok("Se envió el correo de prueba a " + correoDestino + ".")
                : ResultadoOperacion.Error("No se pudo enviar el correo de prueba. Revisá la configuración de correo saliente.");
        }

        // exigirFuturo: al publicar, la fecha de vencimiento no puede haber pasado.
        private static ResultadoOperacion ValidarVigencia(DateTime? fechaPublicacion, DateTime? fechaVencimiento, bool exigirFuturo)
        {
            if (!fechaVencimiento.HasValue)
            {
                return ResultadoOperacion.Ok();
            }

            if (fechaPublicacion.HasValue && fechaVencimiento.Value <= fechaPublicacion.Value)
            {
                return ResultadoOperacion.Error("La fecha de vencimiento tiene que ser posterior a la de publicación.");
            }

            if (exigirFuturo && fechaVencimiento.Value <= DateTime.Now)
            {
                return ResultadoOperacion.Error("La fecha de vencimiento ya pasó: elegí una fecha futura o guardala como borrador.");
            }

            return ResultadoOperacion.Ok();
        }

        private static string DescribirVigencia(bool publicado, DateTime? fechaPublicacion, DateTime? fechaVencimiento)
        {
            if (!publicado)
            {
                return " (como borrador)";
            }

            string texto = fechaPublicacion.HasValue && fechaPublicacion.Value > DateTime.Now
                ? " (programada para el " + FormatearFechaHora(fechaPublicacion.Value)
                : " (publicada";
            return texto + (fechaVencimiento.HasValue ? ", vence el " + FormatearFechaHora(fechaVencimiento.Value) : ", sin vencimiento") + ")";
        }

        private static string FormatearFechaHora(DateTime fecha)
        {
            return fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        private static bool EsDestinatarioValido(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return false;
            }

            foreach (string valido in DestinatariosNewsletter)
            {
                if (string.Equals(valido, codigo, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string ConstruirUrlAbsoluta(string url, string urlBaseAplicacion)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            string urlNormalizada = url.Trim();
            Uri uriAbsoluta;
            if (Uri.TryCreate(urlNormalizada, UriKind.Absolute, out uriAbsoluta) &&
                (uriAbsoluta.Scheme == Uri.UriSchemeHttp || uriAbsoluta.Scheme == Uri.UriSchemeHttps))
            {
                return uriAbsoluta.ToString();
            }

            if (string.IsNullOrWhiteSpace(urlBaseAplicacion))
            {
                return null;
            }

            string relativa = NormalizarRutaRelativa(urlNormalizada);
            return new Uri(new Uri(AsegurarBarraFinal(urlBaseAplicacion)), relativa).ToString();
        }

        private static string ResolverRutaFisica(string url, string rutaBaseAplicacion)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(rutaBaseAplicacion))
            {
                return null;
            }

            Uri uriAbsoluta;
            if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out uriAbsoluta))
            {
                return null;
            }

            string baseCompleta = Path.GetFullPath(rutaBaseAplicacion);
            string relativa = NormalizarRutaRelativa(url.Trim()).Replace('/', Path.DirectorySeparatorChar);
            string rutaCompleta = Path.GetFullPath(Path.Combine(baseCompleta, relativa));

            if (!rutaCompleta.StartsWith(baseCompleta, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return File.Exists(rutaCompleta) ? rutaCompleta : null;
        }

        private static string NormalizarRutaRelativa(string url)
        {
            if (url.StartsWith("~/", StringComparison.Ordinal))
            {
                return url.Substring(2);
            }

            return url.TrimStart('/');
        }

        private static string AsegurarBarraFinal(string url)
        {
            return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
        }

        private static ResultadoOperacion ValidarDatos(
            string titulo, string resumen, string contenido, string categoria, string urlImagen,
            out string tituloNormalizado, out string resumenNormalizado, out string contenidoNormalizado,
            out string categoriaNormalizada, out string urlImagenNormalizada)
        {
            tituloNormalizado = string.IsNullOrWhiteSpace(titulo) ? null : titulo.Trim();
            resumenNormalizado = string.IsNullOrWhiteSpace(resumen) ? null : resumen.Trim();
            contenidoNormalizado = string.IsNullOrWhiteSpace(contenido) ? null : contenido.Trim();
            categoriaNormalizada = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
            urlImagenNormalizada = string.IsNullOrWhiteSpace(urlImagen) ? null : urlImagen.Trim();

            if (tituloNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá el título.");
            }

            if (tituloNormalizado.Length > LongitudMaximaTitulo)
            {
                return ResultadoOperacion.Error("El título no puede superar los " + LongitudMaximaTitulo + " caracteres.");
            }

            if (resumenNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá el resumen.");
            }

            if (resumenNormalizado.Length > LongitudMaximaResumen)
            {
                return ResultadoOperacion.Error("El resumen no puede superar los " + LongitudMaximaResumen + " caracteres.");
            }

            if (contenidoNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá el contenido de la novedad.");
            }

            if (categoriaNormalizada == null || !EsCategoriaValida(categoriaNormalizada))
            {
                return ResultadoOperacion.Error("Seleccioná una categoría válida.");
            }

            return ResultadoOperacion.Ok();
        }

        private static bool EsCategoriaValida(string categoria)
        {
            foreach (string valida in Categorias)
            {
                if (string.Equals(valida, categoria, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static ResultadoOperacion EjecutarProtegido(Func<ResultadoOperacion> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        private static ResultadoOperacion<T> EjecutarProtegido<T>(Func<ResultadoOperacion<T>> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<T>.Error(ex.Message);
            }
        }
    }
}
