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



    public class BLL_CatalogoComercial
    {
        private const int MinutosCache = 5;
        private static readonly object Bloqueo = new object();
        private static List<ItemCatalogoComercial> _cache;
        private static DateTime _cacheHasta = DateTime.MinValue;

        private readonly MPP_CatalogoComercial _mpp = new MPP_CatalogoComercial();

        public List<ItemCatalogoComercial> Listar(string tipoCatalogo)
        {
            return Todos().Where(i => i.TipoCatalogo == tipoCatalogo).OrderBy(i => i.Orden).ToList();
        }

        public bool EsValido(string tipoCatalogo, string codigo)
        {
            return !string.IsNullOrWhiteSpace(codigo) && Listar(tipoCatalogo).Any(i => i.Codigo == codigo);
        }


        public string Nombre(string tipoCatalogo, string codigo)
        {
            if (string.IsNullOrEmpty(codigo))
            {
                return string.Empty;
            }

            ItemCatalogoComercial item = Listar(tipoCatalogo).FirstOrDefault(i => i.Codigo == codigo);
            return item == null ? codigo : item.Nombre;
        }

        public string Nombres(string tipoCatalogo, IEnumerable<string> codigos)
        {
            return codigos == null ? string.Empty : string.Join(", ", codigos.Select(c => Nombre(tipoCatalogo, c)));
        }

        public List<OpcionVinculoComercial> ListarParaVincular(string tipo)
        {
            try
            {
                return _mpp.ListarParaVincular(new OpcionVinculoComercial { Tipo = tipo });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<OpcionVinculoComercial>();
            }
        }

        private List<ItemCatalogoComercial> Todos()
        {
            lock (Bloqueo)
            {
                if (_cache != null && DateTime.Now < _cacheHasta)
                {
                    return _cache;
                }
            }

            List<ItemCatalogoComercial> items;
            try
            {
                items = _mpp.Listar();
            }
            catch (ErrorAccesoDatosException)
            {


                return new List<ItemCatalogoComercial>();
            }

            lock (Bloqueo)
            {
                _cache = items;
                _cacheHasta = DateTime.Now.AddMinutes(MinutosCache);
            }
            return items;
        }





        internal static string Limpiar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return string.Join(" ", valor.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        internal static string LimpiarTextoLargo(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }


        internal static bool LeerFecha(string texto, out DateTime? fecha)
        {
            fecha = null;
            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            DateTime valor;
            if (DateTime.TryParseExact(texto.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor)
                && valor.Year >= 2000 && valor.Year <= 2100)
            {
                fecha = valor;
                return true;
            }

            return false;
        }


        internal static string Recortar(string descripcion)
        {
            return descripcion != null && descripcion.Length > 1900 ? descripcion.Substring(0, 1900) : descripcion;
        }

        internal static void Registrar(int idUsuarioInterno, string operacion, string entidad, int? id, string descripcion)
        {
            try
            {
                if (descripcion != null && descripcion.Length > 1900)
                {
                    descripcion = descripcion.Substring(0, 1900);
                }

                new BLL_Bitacora().RegistrarInterno(idUsuarioInterno, operacion, entidad, id, descripcion);
            }
            catch (Exception)
            {

            }
        }


        public static string GenerarCodigo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            string normalizado = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder resultado = new StringBuilder();
            bool guion = false;
            foreach (char c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    resultado.Append(c);
                    guion = false;
                }
                else if (!guion && resultado.Length > 0)
                {
                    resultado.Append('-');
                    guion = true;
                }
            }

            string codigo = resultado.ToString().Trim('-');
            return codigo.Length > 40 ? codigo.Substring(0, 40).Trim('-') : codigo;
        }
    }
}
