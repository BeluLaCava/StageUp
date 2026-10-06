using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Parámetros de la plataforma (script 49), editables desde
    // Interno/ParametrosPlataforma.aspx (permiso CONFIGURAR_PARAMETROS). Si la
    // base no responde se usan los valores por defecto, para que una reserva
    // nunca falle por no poder leer un parámetro.
    public class BLL_ParametroPlataforma
    {
        public const string ComisionPlataformaPorcentaje = "ComisionPlataformaPorcentaje";
        public const string HorasLimitePago = "HorasLimitePago";
        // Política de cancelación del CU-001-005 (A14 a A16), script 53.
        public const string DiasCancelacionSinCargo = "DiasCancelacionSinCargo";
        public const string DiasCancelacionCargoParcial = "DiasCancelacionCargoParcial";
        public const string PorcentajeCargoParcial = "PorcentajeCargoParcial";
        public const string PorcentajeCargoTotal = "PorcentajeCargoTotal";

        private class DefinicionParametro
        {
            public string Nombre;
            public string Unidad;
            public decimal Minimo;
            public decimal Maximo;
            public bool SoloEnteros;
            public decimal PorDefecto;
        }

        private static readonly Dictionary<string, DefinicionParametro> Definiciones = new Dictionary<string, DefinicionParametro>
        {
            { ComisionPlataformaPorcentaje, new DefinicionParametro { Nombre = "Comisión de StageUp por reserva", Unidad = "%", Minimo = 0, Maximo = 50, PorDefecto = 10 } },
            { HorasLimitePago, new DefinicionParametro { Nombre = "Plazo para pagar una reserva aceptada", Unidad = "horas", Minimo = 1, Maximo = 168, SoloEnteros = true, PorDefecto = 48 } },
            { DiasCancelacionSinCargo, new DefinicionParametro { Nombre = "Anticipación para cancelar sin cargo", Unidad = "días", Minimo = 1, Maximo = 365, SoloEnteros = true, PorDefecto = 14 } },
            { DiasCancelacionCargoParcial, new DefinicionParametro { Nombre = "Anticipación mínima para el cargo parcial", Unidad = "días", Minimo = 0, Maximo = 364, SoloEnteros = true, PorDefecto = 7 } },
            { PorcentajeCargoParcial, new DefinicionParametro { Nombre = "Cargo parcial por cancelación", Unidad = "%", Minimo = 0, Maximo = 100, PorDefecto = 50 } },
            { PorcentajeCargoTotal, new DefinicionParametro { Nombre = "Cargo total por cancelación", Unidad = "%", Minimo = 0, Maximo = 100, PorDefecto = 100 } }
        };

        private const string TipoEntidadBitacora = "ParametroPlataforma";

        private readonly MPP_ParametroPlataforma _mpp = new MPP_ParametroPlataforma();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // Ítem 36: los parámetros se leen en cada reserva, cancelación y pago
        // (y en Mis reservas, una vez por fila). Se guardan en memoria unos
        // minutos y se descartan cuando el administrador los modifica.
        private const int MinutosCache = 5;
        private static readonly object _lockCache = new object();
        private static List<ParametroPlataforma> _cache;
        private static DateTime _vencimientoCache = DateTime.MinValue;

        private List<ParametroPlataforma> ListarConCache()
        {
            lock (_lockCache)
            {
                if (_cache != null && DateTime.Now < _vencimientoCache)
                {
                    return _cache;
                }
            }

            List<ParametroPlataforma> parametros = _mpp.Listar();
            lock (_lockCache)
            {
                _cache = parametros;
                _vencimientoCache = DateTime.Now.AddMinutes(MinutosCache);
            }

            return parametros;
        }

        public static void InvalidarCache()
        {
            lock (_lockCache)
            {
                _cache = null;
            }
        }

        public static string ObtenerNombre(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) ? definicion.Nombre : clave;
        }

        public static string ObtenerUnidad(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) ? definicion.Unidad : string.Empty;
        }

        public List<ParametroPlataforma> Listar()
        {
            try
            {
                return _mpp.Listar().Where(p => Definiciones.ContainsKey(p.Clave)).ToList();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<ParametroPlataforma>();
            }
        }

        public decimal ObtenerDecimal(string clave)
        {
            DefinicionParametro definicion = Definiciones[clave];
            try
            {
                ParametroPlataforma parametro = ListarConCache().FirstOrDefault(p => p.Clave == clave);
                decimal valor;
                if (parametro != null &&
                    decimal.TryParse(parametro.Valor, NumberStyles.Number, CultureInfo.InvariantCulture, out valor) &&
                    valor >= definicion.Minimo && valor <= definicion.Maximo)
                {
                    return valor;
                }
            }
            catch (ErrorAccesoDatosException)
            {
            }

            return definicion.PorDefecto;
        }

        public int ObtenerEntero(string clave)
        {
            return (int)Math.Round(ObtenerDecimal(clave));
        }

        // El valor llega como lo escribió el administrador (acepta coma o punto
        // decimal) y se guarda siempre con punto.
        public ResultadoOperacion Actualizar(string clave, string valorIngresado, int idUsuarioInternoResponsable)
        {
            return ActualizarVarios(new Dictionary<string, string> { { clave ?? string.Empty, valorIngresado } }, idUsuarioInternoResponsable);
        }

        // Guarda todos los valores de la pantalla juntos: primero valida cada
        // uno y la consistencia del conjunto (con los valores nuevos, no con
        // los guardados), y solo si todo está bien los guarda.
        public ResultadoOperacion ActualizarVarios(IDictionary<string, string> valoresIngresados, int idUsuarioInternoResponsable)
        {
            Dictionary<string, decimal> nuevos = new Dictionary<string, decimal>();
            List<string> errores = new List<string>();

            foreach (KeyValuePair<string, string> par in valoresIngresados)
            {
                decimal valor;
                string error = ParsearYValidar(par.Key, par.Value, out valor);
                if (error != null)
                {
                    errores.Add(error);
                }
                else
                {
                    nuevos[par.Key] = valor;
                }
            }

            if (errores.Count == 0)
            {
                string errorCruzado = ValidarConsistenciaCancelacion(
                    clave => nuevos.ContainsKey(clave) ? nuevos[clave] : ObtenerDecimal(clave));
                if (errorCruzado != null)
                {
                    errores.Add(errorCruzado);
                }
            }

            if (errores.Count > 0)
            {
                return ResultadoOperacion.Error(string.Join(" ", errores));
            }

            int cambios = 0;
            foreach (KeyValuePair<string, decimal> par in nuevos)
            {
                bool cambio;
                string error = Guardar(par.Key, par.Value, idUsuarioInternoResponsable, out cambio);
                if (error != null)
                {
                    return ResultadoOperacion.Error(error);
                }

                if (cambio)
                {
                    cambios++;
                }
            }

            return ResultadoOperacion.Ok(cambios == 0 ? null : "Se guardaron los cambios.");
        }

        private static string ParsearYValidar(string clave, string valorIngresado, out decimal valor)
        {
            valor = 0m;
            DefinicionParametro definicion;
            if (!Definiciones.TryGetValue(clave ?? string.Empty, out definicion))
            {
                return "El parámetro indicado no existe.";
            }

            string texto = (valorIngresado ?? string.Empty).Trim().Replace(',', '.');
            if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor))
            {
                return definicion.Nombre + ": ingresá un número.";
            }

            if (definicion.SoloEnteros && valor != Math.Truncate(valor))
            {
                return definicion.Nombre + ": ingresá un número entero.";
            }

            if (valor < definicion.Minimo || valor > definicion.Maximo)
            {
                return definicion.Nombre + ": tiene que estar entre " + definicion.Minimo.ToString("0.##", CultureInfo.InvariantCulture) +
                    " y " + definicion.Maximo.ToString("0.##", CultureInfo.InvariantCulture) + " " + definicion.Unidad + ".";
            }

            valor = decimal.Round(valor, 2);
            return null;
        }

        // Política de cancelación (CU-001-005): el plazo del cargo parcial
        // tiene que ser menor que el plazo sin cargo, y el cargo parcial no
        // puede superar al total.
        private static string ValidarConsistenciaCancelacion(Func<string, decimal> valor)
        {
            decimal diasSinCargo = valor(DiasCancelacionSinCargo);
            decimal diasParcial = valor(DiasCancelacionCargoParcial);
            if (diasParcial >= diasSinCargo)
            {
                return ObtenerNombre(DiasCancelacionCargoParcial) + " (" + diasParcial.ToString("0", CultureInfo.InvariantCulture) +
                    " días) tiene que ser menor que " + ObtenerNombre(DiasCancelacionSinCargo).ToLowerInvariant() +
                    " (" + diasSinCargo.ToString("0", CultureInfo.InvariantCulture) + " días).";
            }

            decimal parcial = valor(PorcentajeCargoParcial);
            decimal total = valor(PorcentajeCargoTotal);
            if (parcial > total)
            {
                return ObtenerNombre(PorcentajeCargoParcial) + " (" + parcial.ToString("0.##", CultureInfo.InvariantCulture) +
                    " %) no puede superar el " + ObtenerNombre(PorcentajeCargoTotal).ToLowerInvariant() +
                    " (" + total.ToString("0.##", CultureInfo.InvariantCulture) + " %).";
            }

            return null;
        }

        private string Guardar(string clave, decimal valor, int idUsuarioInternoResponsable, out bool cambio)
        {
            cambio = false;
            DefinicionParametro definicion = Definiciones[clave];
            string valorGuardado = valor.ToString("0.##", CultureInfo.InvariantCulture);

            try
            {
                ParametroPlataforma anterior = _mpp.Listar().FirstOrDefault(p => p.Clave == clave);
                if (anterior != null && anterior.Valor == valorGuardado)
                {
                    return null;
                }

                if (!_mpp.Actualizar(new ParametroPlataforma { Clave = clave, Valor = valorGuardado }))
                {
                    return "No se encontró el parámetro " + definicion.Nombre + ". Revisá que estén aplicados los scripts de la base.";
                }

                InvalidarCache();

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, null,
                    definicion.Nombre + ": " + (anterior == null ? "-" : anterior.Valor) + " → " + valorGuardado + " " + definicion.Unidad + ".");

                cambio = true;
                return null;
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ex.Message;
            }
        }
    }
}
