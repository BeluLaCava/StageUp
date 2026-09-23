using System;
using System.Collections.Generic;
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
    public class BLL_Novedad
    {
        private const int LongitudMaximaTitulo = 180;
        private const int LongitudMaximaResumen = 300;

        // Mismos valores que ofrece el desplegable "Categoría" en
        // Interno/GestionNovedades.aspx.
        public static readonly string[] Categorias =
        {
            "Institucional", "Catalogo", "Consejos", "Comunidad"
        };

        // Mismos valores que ofrece el desplegable "Destinatarios" en
        // Interno/GestionNovedades.aspx.
        public static readonly string[] DestinatariosNewsletter =
        {
            "Activos", "Gestores", "Solicitantes", "Todos"
        };

        private readonly MPP_Novedad _mpp = new MPP_Novedad();
        private readonly BLL_UsuarioExterno _bllUsuarioExterno = new BLL_UsuarioExterno();
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

        public List<Novedad> Listar()
        {
            try
            {
                return _mpp.Listar();
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
            DateTime? fechaPublicacion, bool publicado, int idUsuarioInternoResponsable)
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

                int idNovedad = _mpp.Insertar(new Novedad
                {
                    Titulo = tituloNormalizado,
                    Resumen = resumenNormalizado,
                    Contenido = contenidoNormalizado,
                    Categoria = categoriaNormalizada,
                    UrlImagen = urlImagenNormalizada,
                    Publicado = publicado,
                    FechaPublicacion = publicado ? (fechaPublicacion ?? DateTime.Now) : fechaPublicacion
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", TipoEntidadBitacora, idNovedad,
                    "Alta de la novedad \"" + tituloNormalizado + "\"" + (publicado ? " (publicada)." : " (como borrador)."));

                return ResultadoOperacion<int>.Ok(idNovedad, publicado
                    ? "La novedad se creó y publicó correctamente."
                    : "La novedad se guardó como borrador.");
            });
        }

        public ResultadoOperacion Modificar(
            int idNovedad, string titulo, string resumen, string contenido, string categoria, string urlImagen,
            DateTime? fechaPublicacion, bool publicado, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Novedad actual = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (actual == null)
                {
                    return ResultadoOperacion.Error("No se encontró la novedad seleccionada.");
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

                DateTime? fechaPublicacionFinal = publicado
                    ? (fechaPublicacion ?? actual.FechaPublicacion ?? DateTime.Now)
                    : fechaPublicacion;

                _mpp.Modificar(new Novedad
                {
                    IdNovedad = idNovedad,
                    Titulo = tituloNormalizado,
                    Resumen = resumenNormalizado,
                    Contenido = contenidoNormalizado,
                    Categoria = categoriaNormalizada,
                    UrlImagen = urlImagenNormalizada,
                    Publicado = publicado,
                    FechaPublicacion = fechaPublicacionFinal
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    "Modificación de la novedad \"" + tituloNormalizado + "\".");

                return ResultadoOperacion.Ok("La novedad se actualizó correctamente.");
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

                if (novedad.Publicado)
                {
                    return ResultadoOperacion.Error("La novedad ya está publicada.");
                }

                _mpp.Publicar(novedad);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    "Publicación de la novedad \"" + novedad.Titulo + "\" (queda visible en la página pública de novedades).");

                return ResultadoOperacion.Ok("La novedad se publicó y ya se muestra en la página pública.");
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

        // Envía el newsletter de una novedad ya publicada a los usuarios
        // externos activos de la categoría elegida, usando la plantilla de
        // StageUp.Servicios.ServicioCorreo. Solo se puede enviar una vez por
        // novedad (se marca con enviadaPorCorreo) para evitar duplicar
        // envíos si la novedad se vuelve a editar más adelante.
        public ResultadoOperacion<int> EnviarNewsletter(
            int idNovedad, string destinatarioCodigo, string urlNovedades, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido<int>(() =>
            {
                Novedad novedad = _mpp.ObtenerPorId(new Novedad { IdNovedad = idNovedad });
                if (novedad == null)
                {
                    return ResultadoOperacion<int>.Error("No se encontró la novedad seleccionada.");
                }

                if (!novedad.Publicado)
                {
                    return ResultadoOperacion<int>.Error("Primero publicá la novedad para poder enviarla por correo.");
                }

                if (novedad.EnviadaPorCorreo)
                {
                    return ResultadoOperacion<int>.Error("Esta novedad ya se envió por correo anteriormente.");
                }

                string criterio = EsDestinatarioValido(destinatarioCodigo) ? destinatarioCodigo : "Activos";
                List<UsuarioExterno> destinatarios = _bllUsuarioExterno.ListarParaNewsletter(criterio);

                int enviados = 0;
                foreach (UsuarioExterno destinatario in destinatarios)
                {
                    bool enviado = _servicioCorreo.EnviarNewsletter(
                        destinatario.CorreoElectronico,
                        destinatario.Nombre,
                        novedad.Titulo,
                        novedad.Resumen,
                        "Ver novedades",
                        urlNovedades);

                    if (enviado)
                    {
                        enviados++;
                    }
                }

                _mpp.MarcarEnviadaPorCorreo(new Novedad { IdNovedad = idNovedad, DestinatarioNewsletter = criterio });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idNovedad,
                    "Envío del newsletter de la novedad \"" + novedad.Titulo + "\" a " + enviados + " de " +
                    destinatarios.Count + " destinatario(s) (categoría: " + criterio + ").");

                return destinatarios.Count == 0
                    ? ResultadoOperacion<int>.Ok(0, "No hay destinatarios activos en esa categoría; no se envió ningún correo.")
                    : ResultadoOperacion<int>.Ok(enviados, "Se envió el newsletter a " + enviados + " de " + destinatarios.Count + " destinatario(s).");
            });
        }

        // Envío de prueba: manda el contenido tal como está cargado en el
        // formulario (sin necesidad de haberlo guardado antes) únicamente al
        // correo del usuario interno que está probando la pantalla.
        public ResultadoOperacion EnviarPrueba(
            string correoDestino, string nombreDestino, string titulo, string resumen, string urlNovedades)
        {
            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                return ResultadoOperacion.Error("Tu usuario interno no tiene un correo electrónico configurado para recibir la prueba.");
            }

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(resumen))
            {
                return ResultadoOperacion.Error("Completá el título y el resumen antes de enviar una prueba.");
            }

            bool enviado = _servicioCorreo.EnviarNewsletter(
                correoDestino, nombreDestino, titulo.Trim(), resumen.Trim(), "Ver novedades", urlNovedades);

            return enviado
                ? ResultadoOperacion.Ok("Se envió el correo de prueba a " + correoDestino + ".")
                : ResultadoOperacion.Error("No se pudo enviar el correo de prueba. Revisá la configuración de correo saliente.");
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
