using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{








    public class BLL_ContenidoPromocional
    {
        public const string PermisoGestionar = "GESTIONAR_CONTENIDO_PROMOCIONAL";
        public const string PermisoReactivar = BLL_AlianzaEstrategica.PermisoReactivar;
        public const string EntidadBitacora = "ContenidoPromocional";
        public const int TamanioMaximoArchivo = 3 * 1024 * 1024;

        public static readonly Dictionary<string, string> Estados = new Dictionary<string, string>
        {
            { ContenidoPromocional.EstadoActivo, "Activo" },
            { ContenidoPromocional.EstadoEnRevision, "En revisión" },
            { ContenidoPromocional.EstadoDesactualizado, "Desactualizado" },
            { ContenidoPromocional.EstadoDadoDeBaja, "Dado de baja" }
        };


        private static readonly Dictionary<string, string> TiposPermitidos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" }, { ".jpeg", "image/jpeg" }, { ".png", "image/png" }, { ".gif", "image/gif" },
            { ".pdf", "application/pdf" }, { ".mp4", "video/mp4" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
        };

        private readonly MPP_ContenidoPromocional _mpp = new MPP_ContenidoPromocional();
        private readonly BLL_CatalogoComercial _catalogos = new BLL_CatalogoComercial();

        public static string TextoEstado(string estado)
        {
            string texto;
            return Estados.TryGetValue(estado ?? string.Empty, out texto) ? texto : estado;
        }

        public static string ClaseEstado(string estado)
        {
            switch (estado)
            {
                case ContenidoPromocional.EstadoActivo: return "comercial-badge-activa";
                case ContenidoPromocional.EstadoEnRevision: return "comercial-badge-planificada";
                case ContenidoPromocional.EstadoDesactualizado: return "comercial-badge-cancelada";
                default: return "comercial-badge-baja";
            }
        }

        public static string ExtensionesPermitidas
        {
            get { return "JPG, PNG, GIF, PDF, MP4, DOCX, PPTX o XLSX"; }
        }

        public static bool EsUrlValida(string url)
        {
            Uri uri;
            return !string.IsNullOrWhiteSpace(url) && url.Length <= 1000 &&
                Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrEmpty(uri.Host);
        }



        public static ResultadoOperacion<ArchivoPromocional> ValidarArchivo(string nombreOriginal, byte[] contenido)
        {
            if (contenido == null || contenido.Length == 0)
            {
                return ResultadoOperacion<ArchivoPromocional>.Error("El archivo está vacío o no se pudo leer.", "A12");
            }

            if (contenido.Length > TamanioMaximoArchivo)
            {
                return ResultadoOperacion<ArchivoPromocional>.Error("El archivo supera los 3 MB. Para piezas más grandes cargá un enlace (por ejemplo, a la carpeta compartida o al video publicado).", "A12");
            }

            char[] invalidos = Path.GetInvalidFileNameChars();
            string nombre = new string((nombreOriginal ?? string.Empty).Replace('\\', '/').Split('/').Last()
                .Where(c => !invalidos.Contains(c)).ToArray()).Trim();
            string extension = Path.GetExtension(nombre);
            string mime;
            if (string.IsNullOrEmpty(extension) || !TiposPermitidos.TryGetValue(extension, out mime))
            {
                return ResultadoOperacion<ArchivoPromocional>.Error("El archivo tiene que ser " + ExtensionesPermitidas + ".", "A12");
            }

            if (!FirmaCoincide(extension.ToLowerInvariant(), contenido))
            {
                return ResultadoOperacion<ArchivoPromocional>.Error("El contenido del archivo no corresponde a su extensión.", "A12");
            }




            if (!ValidadorEstructuraArchivo.EsValido(extension.ToLowerInvariant(), contenido))
            {
                return ResultadoOperacion<ArchivoPromocional>.Error("El archivo está dañado, incompleto o no es un " +
                    extension.TrimStart('.').ToUpperInvariant() + " válido. Revisalo y volvé a adjuntarlo.", "A12");
            }

            if (nombre.Length > 200)
            {
                nombre = nombre.Substring(0, 200 - extension.Length) + extension;
            }

            return ResultadoOperacion<ArchivoPromocional>.Ok(new ArchivoPromocional
            {
                NombreOriginal = nombre,
                Extension = extension.ToLowerInvariant() == ".jpeg" ? ".jpg" : extension.ToLowerInvariant(),
                TipoMime = mime,
                Contenido = contenido
            });
        }

        private static bool FirmaCoincide(string extension, byte[] b)
        {
            Func<int, byte[], bool> empieza = (desde, firma) => b.Length >= desde + firma.Length &&
                firma.Select((x, i) => b[desde + i] == x).All(x => x);

            switch (extension)
            {
                case ".jpg":
                case ".jpeg": return empieza(0, new byte[] { 0xFF, 0xD8, 0xFF });
                case ".png": return empieza(0, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
                case ".gif": return empieza(0, new byte[] { 0x47, 0x49, 0x46, 0x38 });
                case ".pdf": return empieza(0, new byte[] { 0x25, 0x50, 0x44, 0x46 });
                case ".mp4": return empieza(4, new byte[] { 0x66, 0x74, 0x79, 0x70 });
                case ".docx":
                case ".pptx":
                case ".xlsx": return empieza(0, new byte[] { 0x50, 0x4B, 0x03, 0x04 });
                default: return false;
            }
        }

        public ResultadoOperacion<ResumenContenidos> ObtenerResumen()
        {
            try
            {
                return ResultadoOperacion<ResumenContenidos>.Ok(_mpp.Contar());
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<ResumenContenidos>.Error("No se pudo cargar el contenido promocional. Probá nuevamente en unos minutos.");
            }
        }

        public ResultadoOperacion<List<ContenidoPromocional>> Listar(FiltroContenidoPromocional filtro)
        {
            filtro = filtro ?? new FiltroContenidoPromocional();
            if (filtro.Vista != FiltroContenidoPromocional.VistaVigentes && filtro.Vista != FiltroContenidoPromocional.VistaBajas &&
                filtro.Vista != FiltroContenidoPromocional.VistaTodos)
            {
                filtro.Vista = FiltroContenidoPromocional.VistaVigentes;
            }

            if (filtro.Estado != null && !Estados.ContainsKey(filtro.Estado)) filtro.Estado = null;
            if (filtro.TipoContenido != null && !_catalogos.EsValido(ItemCatalogoComercial.TipoContenido, filtro.TipoContenido)) filtro.TipoContenido = null;
            if (filtro.CanalPublicacion != null && !_catalogos.EsValido(ItemCatalogoComercial.CanalUso, filtro.CanalPublicacion)) filtro.CanalPublicacion = null;
            filtro.Texto = BLL_CatalogoComercial.Limpiar(filtro.Texto);
            if (filtro.Texto != null && filtro.Texto.Length > 100) filtro.Texto = filtro.Texto.Substring(0, 100);

            try
            {
                return ResultadoOperacion<List<ContenidoPromocional>>.Ok(_mpp.Listar(filtro));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<ContenidoPromocional>>.Error("No se pudo cargar el contenido promocional. Probá nuevamente en unos minutos.");
            }
        }

        public ContenidoPromocional Obtener(int idContenido)
        {
            try
            {
                return _mpp.Obtener(new ContenidoPromocional { IdContenidoPromocional = idContenido });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }





        public ResultadoOperacion<int> Guardar(ContenidoPromocional contenido, int idUsuarioInterno, out string rutaArchivoAnterior)
        {
            rutaArchivoAnterior = null;
            if (contenido == null)
            {
                return ResultadoOperacion<int>.Error("Completá los datos del contenido.");
            }

            bool esAlta = contenido.IdContenidoPromocional <= 0;
            contenido.Titulo = BLL_CatalogoComercial.Limpiar(contenido.Titulo);
            contenido.Descripcion = BLL_CatalogoComercial.LimpiarTextoLargo(contenido.Descripcion);
            contenido.UrlContenido = string.IsNullOrWhiteSpace(contenido.UrlContenido) ? null : contenido.UrlContenido.Trim();


            if (_catalogos.Listar(ItemCatalogoComercial.TipoContenido).Count == 0 || _catalogos.Listar(ItemCatalogoComercial.CanalUso).Count == 0)
            {
                return ResultadoOperacion<int>.Error("No hay tipos de contenido o canales de uso definidos, por eso no se puede guardar. Consultalo con un administrador.");
            }

            ContenidoPromocional anterior = null;
            if (!esAlta)
            {
                anterior = Obtener(contenido.IdContenidoPromocional);
                if (anterior == null)
                {
                    return ResultadoOperacion<int>.Error("El contenido ya no existe.");
                }

                if (!anterior.Activo)
                {
                    return ResultadoOperacion<int>.Error("Un contenido dado de baja solo se puede consultar.", "A9");
                }
            }


            if (anterior != null && contenido.TieneArchivo && contenido.RutaArchivo == anterior.RutaArchivo)
            {
                contenido.RutaArchivo = null;
                contenido.NombreArchivoOriginal = null;
                contenido.TipoMimeArchivo = null;
                contenido.TamanioArchivo = null;
            }

            bool tendraArchivo = contenido.TieneArchivo || (anterior != null && anterior.TieneArchivo && !contenido.QuitarArchivo);


            List<string> faltantes = new List<string>();
            if (contenido.Titulo == null) faltantes.Add("título");
            if (string.IsNullOrEmpty(contenido.TipoContenido)) faltantes.Add("tipo de contenido");
            if (string.IsNullOrEmpty(contenido.CanalPublicacion)) faltantes.Add("canal previsto de uso");
            if (string.IsNullOrEmpty(contenido.EstadoContenido)) faltantes.Add("estado");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion<int>.Error("Para guardar el contenido completá: " + string.Join(", ", faltantes) + ".", "A14");
            }

            if (contenido.Titulo.Length > 200 || (contenido.Descripcion ?? string.Empty).Length > 2000)
            {
                return ResultadoOperacion<int>.Error("El título admite hasta 200 caracteres y la descripción hasta 2000.", "A14");
            }


            if (!_catalogos.EsValido(ItemCatalogoComercial.TipoContenido, contenido.TipoContenido))
            {
                return ResultadoOperacion<int>.Error("Seleccioná el tipo de contenido para clasificar la pieza promocional.", "A15");
            }

            if (!_catalogos.EsValido(ItemCatalogoComercial.CanalUso, contenido.CanalPublicacion))
            {
                return ResultadoOperacion<int>.Error("Seleccioná el canal previsto de uso para organizar el material.", "A16");
            }


            if (!Estados.ContainsKey(contenido.EstadoContenido) || contenido.EstadoContenido == ContenidoPromocional.EstadoDadoDeBaja)
            {
                return ResultadoOperacion<int>.Error("Seleccioná un estado válido: activo, en revisión o desactualizado.", "A17");
            }


            if (contenido.UrlContenido != null && !EsUrlValida(contenido.UrlContenido))
            {
                return ResultadoOperacion<int>.Error("El enlace tiene que ser una dirección web completa (que empiece con http:// o https://).", "A13");
            }

            if (!tendraArchivo && contenido.UrlContenido == null)
            {
                return ResultadoOperacion<int>.Error("Adjuntá el archivo o ingresá el enlace de la pieza promocional.", "A18");
            }


            string errorVinculo = ValidarVinculo(OpcionVinculoComercial.TipoCampania, contenido.IdCampaniaComercial, anterior == null ? null : anterior.IdCampaniaComercial)
                ?? ValidarVinculo(OpcionVinculoComercial.TipoAlianza, contenido.IdAlianzaEstrategica, anterior == null ? null : anterior.IdAlianzaEstrategica);
            if (errorVinculo != null)
            {
                return ResultadoOperacion<int>.Error(errorVinculo, "A19");
            }

            int id;
            try
            {
                id = _mpp.Guardar(contenido, new UsuarioInterno { IdUsuarioInterno = idUsuarioInterno }, out rutaArchivoAnterior);
            }
            catch (ErrorAccesoDatosException ex)
            {
                switch (ex.NumeroErrorSql)
                {
                    case 51330: return ResultadoOperacion<int>.Error("El contenido ya no existe o fue dado de baja.");
                    case 51331: return ResultadoOperacion<int>.Error("La campaña o la alianza elegida ya no está disponible. Elegí otra o dejá el contenido sin asociar.", "A19");
                    case 51332: return ResultadoOperacion<int>.Error("Adjuntá el archivo o ingresá el enlace de la pieza promocional.", "A18");
                    default: return ResultadoOperacion<int>.Error("No fue posible guardar el contenido en este momento. Intentá nuevamente.", "A20");
                }
            }

            string recurso = contenido.TieneArchivo ? "archivo " + contenido.NombreArchivoOriginal
                : tendraArchivo ? "archivo sin cambios"
                : "enlace";
            BLL_CatalogoComercial.Registrar(idUsuarioInterno, esAlta ? "ALTA" : "MODIFICACION", EntidadBitacora, id,
                (esAlta ? "Alta" : "Modificación") + " del contenido promocional \"" + contenido.Titulo + "\" (" +
                _catalogos.Nombre(ItemCatalogoComercial.TipoContenido, contenido.TipoContenido) + ", " + TextoEstado(contenido.EstadoContenido) +
                ", " + recurso + ")." +
                (contenido.IdCampaniaComercial.HasValue ? " Campaña " + contenido.IdCampaniaComercial + "." : string.Empty) +
                (anterior != null && anterior.EstadoContenido != contenido.EstadoContenido
                    ? " Estado anterior: " + TextoEstado(anterior.EstadoContenido) + "."
                    : string.Empty));

            return ResultadoOperacion<int>.Ok(id, esAlta
                ? "El contenido promocional fue guardado correctamente."
                : "El contenido promocional fue actualizado correctamente.");
        }


        public ResultadoOperacion CambiarEstado(int idContenido, string estadoNuevo, int idUsuarioInterno, bool puedeReactivar)
        {
            ContenidoPromocional contenido = Obtener(idContenido);
            if (contenido == null)
            {
                return ResultadoOperacion.Error("El contenido ya no existe.");
            }

            if (string.IsNullOrEmpty(estadoNuevo) || !Estados.ContainsKey(estadoNuevo))
            {
                return ResultadoOperacion.Error("Seleccioná un estado válido.", "A17");
            }

            if (estadoNuevo == contenido.EstadoContenido)
            {
                return ResultadoOperacion.Error("El contenido ya está en ese estado.");
            }

            bool reactivacion = contenido.EstadoContenido == ContenidoPromocional.EstadoDadoDeBaja;
            if (reactivacion && !puedeReactivar)
            {
                return ResultadoOperacion.Error("Un contenido dado de baja solo se puede consultar. Para volver a usarlo hace falta el permiso de reactivación.", "A9");
            }

            try
            {
                if (!_mpp.CambiarEstado(new ContenidoPromocional { IdContenidoPromocional = idContenido, EstadoContenido = estadoNuevo }))
                {
                    return ResultadoOperacion.Error("El contenido ya está en ese estado.");
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No fue posible actualizar el estado en este momento. Intentá nuevamente.", "A20");
            }

            bool baja = estadoNuevo == ContenidoPromocional.EstadoDadoDeBaja;
            BLL_CatalogoComercial.Registrar(idUsuarioInterno, baja ? "BAJA" : reactivacion ? "ACTIVACION" : "MODIFICACION", EntidadBitacora, idContenido,
                (baja ? "Baja lógica" : reactivacion ? "Reactivación" : "Cambio de estado") + " del contenido promocional \"" + contenido.Titulo + "\": " +
                TextoEstado(contenido.EstadoContenido) + " → " + TextoEstado(estadoNuevo) + ".");

            return ResultadoOperacion.Ok(baja
                ? "El contenido promocional fue dado de baja correctamente. Se conserva en el repositorio para consulta histórica."
                : "El estado fue actualizado correctamente: " + TextoEstado(estadoNuevo).ToLowerInvariant() + ".");
        }

        private string ValidarVinculo(string tipo, int? id, int? actual)
        {
            if (!id.HasValue || id == actual)
            {
                return null;
            }

            return _catalogos.ListarParaVincular(tipo).Any(o => o.Id == id.Value)
                ? null
                : (tipo == OpcionVinculoComercial.TipoCampania
                    ? "La campaña elegida no existe o no está disponible. Elegí otra o dejá el contenido sin campaña."
                    : "La alianza elegida no existe o no está disponible. Elegí otra o dejá el contenido sin alianza.");
        }
    }
}
