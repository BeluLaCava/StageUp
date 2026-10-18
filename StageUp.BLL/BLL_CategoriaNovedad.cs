using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Categorías de novedades y del newsletter, administrables (script 85).
    //   * El código se genera a partir del nombre y no cambia: lo guardan las
    //     novedades y las preferencias de los suscriptores.
    //   * Una categoría desactivada deja de ofrecerse para nuevas novedades y
    //     en las preferencias; las novedades que ya la tienen la conservan.
    //   * Tiene que quedar al menos una categoría activa.
    //   * La lista se guarda en memoria un minuto (se usa en varias páginas
    //     públicas) y se vuelve a leer después de cada cambio.
    public class BLL_CategoriaNovedad
    {
        public const int LongitudMaximaNombre = 60;
        private const int LongitudMaximaCodigo = 30;
        private const string TipoEntidadBitacora = "CategoriaNovedad";
        private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(1);

        private static readonly object Bloqueo = new object();
        private static List<CategoriaNovedad> _cache;
        private static DateTime _cacheHasta = DateTime.MinValue;

        private readonly MPP_CategoriaNovedad _mpp = new MPP_CategoriaNovedad();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // Todas (activas e inactivas), en el orden configurado.
        public static List<CategoriaNovedad> ListarTodas()
        {
            lock (Bloqueo)
            {
                if (_cache == null || DateTime.Now >= _cacheHasta)
                {
                    try
                    {
                        _cache = new MPP_CategoriaNovedad().Listar(false);
                        _cacheHasta = DateTime.Now.Add(DuracionCache);
                    }
                    catch (ErrorAccesoDatosException)
                    {
                        // Sin base: se sigue con lo último leído (o nada).
                        return _cache == null ? new List<CategoriaNovedad>() : new List<CategoriaNovedad>(_cache);
                    }
                }

                return new List<CategoriaNovedad>(_cache);
            }
        }

        public static List<CategoriaNovedad> ListarActivas()
        {
            return ListarTodas().Where(c => c.Activa).ToList();
        }

        // Código -> nombre de todas las categorías (para mostrar y filtrar).
        public static Dictionary<string, string> ObtenerEtiquetas()
        {
            Dictionary<string, string> etiquetas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (CategoriaNovedad categoria in ListarTodas())
            {
                etiquetas[categoria.Codigo] = categoria.Nombre;
            }
            return etiquetas;
        }

        public static string ObtenerEtiqueta(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return "Novedades";
            }

            CategoriaNovedad categoria = ListarTodas().FirstOrDefault(c => string.Equals(c.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
            return categoria == null ? codigo : categoria.Nombre;
        }

        public static bool EsActiva(string codigo)
        {
            return !string.IsNullOrWhiteSpace(codigo) &&
                ListarTodas().Any(c => c.Activa && string.Equals(c.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        }

        public static bool Existe(string codigo)
        {
            return !string.IsNullOrWhiteSpace(codigo) &&
                ListarTodas().Any(c => string.Equals(c.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        }

        // Código guardado (con las mayúsculas de la tabla) o null.
        public static string NormalizarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            CategoriaNovedad categoria = ListarTodas().FirstOrDefault(c => string.Equals(c.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));
            return categoria == null ? null : categoria.Codigo;
        }

        // Listado de administración (sin caché: trae las cantidades al día).
        public List<CategoriaNovedad> ListarParaAdministrar()
        {
            try
            {
                return _mpp.Listar(false);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<CategoriaNovedad>();
            }
        }

        public ResultadoOperacion Registrar(string nombre, int orden, int idUsuarioInternoResponsable)
        {
            string nombreNormalizado;
            ResultadoOperacion validacion = Validar(nombre, orden, out nombreNormalizado);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            List<CategoriaNovedad> existentes = ListarParaAdministrar();
            if (existentes.Any(c => string.Equals(c.Nombre, nombreNormalizado, StringComparison.OrdinalIgnoreCase)))
            {
                return ResultadoOperacion.Error("Ya existe una categoría con ese nombre.");
            }

            string codigo = GenerarCodigo(nombreNormalizado, existentes.Select(c => c.Codigo));
            if (codigo == null)
            {
                return ResultadoOperacion.Error("El nombre tiene que incluir al menos una letra o un número.");
            }

            try
            {
                _mpp.Insertar(new CategoriaNovedad { Codigo = codigo, Nombre = nombreNormalizado, Orden = orden });
                InvalidarCache();
                Bitacora(idUsuarioInternoResponsable, "ALTA", "Alta de la categoría de novedades \"" + nombreNormalizado + "\" (código " + codigo + ").");
                return ResultadoOperacion.Ok("Se agregó la categoría \"" + nombreNormalizado + "\".");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(MensajeError(ex, "No se pudo agregar la categoría."));
            }
        }

        public ResultadoOperacion Modificar(string codigo, string nombre, int orden, int idUsuarioInternoResponsable)
        {
            string codigoExistente = BuscarCodigo(codigo);
            if (codigoExistente == null)
            {
                return ResultadoOperacion.Error("La categoría no existe.");
            }

            string nombreNormalizado;
            ResultadoOperacion validacion = Validar(nombre, orden, out nombreNormalizado);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            try
            {
                _mpp.Modificar(new CategoriaNovedad { Codigo = codigoExistente, Nombre = nombreNormalizado, Orden = orden });
                InvalidarCache();
                Bitacora(idUsuarioInternoResponsable, "MODIFICACION", "Modificación de la categoría de novedades " + codigoExistente + ": \"" + nombreNormalizado + "\", orden " + orden + ".");
                return ResultadoOperacion.Ok("Se guardaron los cambios de la categoría.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(MensajeError(ex, "No se pudo guardar la categoría."));
            }
        }

        public ResultadoOperacion CambiarEstado(string codigo, bool activa, int idUsuarioInternoResponsable)
        {
            string codigoExistente = BuscarCodigo(codigo);
            if (codigoExistente == null)
            {
                return ResultadoOperacion.Error("La categoría no existe.");
            }

            try
            {
                _mpp.CambiarEstado(new CategoriaNovedad { Codigo = codigoExistente, Activa = activa });
                InvalidarCache();
                Bitacora(idUsuarioInternoResponsable, activa ? "ACTIVACION" : "BAJA",
                    (activa ? "Activación" : "Desactivación") + " de la categoría de novedades " + codigoExistente + ".");
                return ResultadoOperacion.Ok(activa
                    ? "La categoría volvió a estar disponible."
                    : "La categoría quedó desactivada: ya no se ofrece para nuevas novedades ni en las preferencias del newsletter.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(MensajeError(ex, "No se pudo cambiar el estado de la categoría."));
            }
        }

        public static void InvalidarCache()
        {
            lock (Bloqueo)
            {
                _cache = null;
            }
        }

        // "Guías y consejos" -> "GuiasYConsejos" (sin tildes, máximo 30
        // caracteres; si ya existe se le agrega un número).
        public static string GenerarCodigo(string nombre, IEnumerable<string> existentes)
        {
            string sinTildes = new string((nombre ?? string.Empty).Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());

            StringBuilder codigo = new StringBuilder();
            foreach (string palabra in sinTildes.Split(new[] { ' ', '-', '_', '/', '.', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string limpia = new string(palabra.Where(c => c < 128 && char.IsLetterOrDigit(c)).ToArray());
                if (limpia.Length > 0)
                {
                    codigo.Append(char.ToUpperInvariant(limpia[0])).Append(limpia.Substring(1));
                }
            }

            if (codigo.Length == 0)
            {
                return null;
            }

            string baseCodigo = codigo.ToString();
            if (baseCodigo.Length > LongitudMaximaCodigo)
            {
                baseCodigo = baseCodigo.Substring(0, LongitudMaximaCodigo);
            }

            HashSet<string> usados = new HashSet<string>(existentes ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            string candidato = baseCodigo;
            for (int numero = 2; usados.Contains(candidato); numero++)
            {
                string sufijo = numero.ToString(CultureInfo.InvariantCulture);
                candidato = baseCodigo.Substring(0, Math.Min(baseCodigo.Length, LongitudMaximaCodigo - sufijo.Length)) + sufijo;
            }

            return candidato;
        }

        private string BuscarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            CategoriaNovedad categoria = ListarParaAdministrar().FirstOrDefault(c => string.Equals(c.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));
            return categoria == null ? null : categoria.Codigo;
        }

        private static ResultadoOperacion Validar(string nombre, int orden, out string nombreNormalizado)
        {
            nombreNormalizado = string.IsNullOrWhiteSpace(nombre) ? null : string.Join(" ", nombre.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if (nombreNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá el nombre de la categoría.");
            }

            if (nombreNormalizado.Length > LongitudMaximaNombre)
            {
                return ResultadoOperacion.Error("El nombre no puede superar los " + LongitudMaximaNombre + " caracteres.");
            }

            if (orden < 0 || orden > 9999)
            {
                return ResultadoOperacion.Error("El orden tiene que ser un número entre 0 y 9999.");
            }

            return ResultadoOperacion.Ok();
        }

        private static string MensajeError(ErrorAccesoDatosException ex, string porDefecto)
        {
            switch (ex.NumeroErrorSql)
            {
                case 51070: return "Ya existe una categoría con ese nombre.";
                case 51071: return "Ya existe una categoría con ese código. Probá con otro nombre.";
                case 51072: return "La categoría no existe.";
                case 51073: return "Tiene que quedar al menos una categoría activa.";
                default: return porDefecto + " Intentá nuevamente.";
            }
        }

        private void Bitacora(int idUsuarioInterno, string operacion, string descripcion)
        {
            try
            {
                _bitacora.RegistrarInterno(idUsuarioInterno, operacion, TipoEntidadBitacora, null, descripcion);
            }
            catch (ErrorAccesoDatosException)
            {
            }
        }
    }
}
