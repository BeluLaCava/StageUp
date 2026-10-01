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
        public const string PenalidadCancelacionPorcentaje = "PenalidadCancelacionPorcentaje";
        public const string HorasCancelacionSinCargo = "HorasCancelacionSinCargo";

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
            { PenalidadCancelacionPorcentaje, new DefinicionParametro { Nombre = "Penalidad por cancelación tardía", Unidad = "%", Minimo = 0, Maximo = 100, PorDefecto = 10 } },
            { HorasCancelacionSinCargo, new DefinicionParametro { Nombre = "Anticipación mínima para cancelar sin cargo", Unidad = "horas", Minimo = 0, Maximo = 168, SoloEnteros = true, PorDefecto = 24 } }
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
            DefinicionParametro definicion;
            if (!Definiciones.TryGetValue(clave ?? string.Empty, out definicion))
            {
                return ResultadoOperacion.Error("El parámetro indicado no existe.");
            }

            string texto = (valorIngresado ?? string.Empty).Trim().Replace(',', '.');
            decimal valor;
            if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor))
            {
                return ResultadoOperacion.Error(definicion.Nombre + ": ingresá un número.");
            }

            if (definicion.SoloEnteros && valor != Math.Truncate(valor))
            {
                return ResultadoOperacion.Error(definicion.Nombre + ": ingresá un número entero.");
            }

            if (valor < definicion.Minimo || valor > definicion.Maximo)
            {
                return ResultadoOperacion.Error(
                    definicion.Nombre + ": tiene que estar entre " + definicion.Minimo.ToString("0.##", CultureInfo.InvariantCulture) +
                    " y " + definicion.Maximo.ToString("0.##", CultureInfo.InvariantCulture) + " " + definicion.Unidad + ".");
            }

            valor = decimal.Round(valor, 2);
            string valorGuardado = valor.ToString("0.##", CultureInfo.InvariantCulture);

            try
            {
                ParametroPlataforma anterior = _mpp.Listar().FirstOrDefault(p => p.Clave == clave);
                if (anterior != null && anterior.Valor == valorGuardado)
                {
                    return ResultadoOperacion.Ok();
                }

                if (!_mpp.Actualizar(new ParametroPlataforma { Clave = clave, Valor = valorGuardado }))
                {
                    return ResultadoOperacion.Error("No se encontró el parámetro " + definicion.Nombre + ".");
                }

                InvalidarCache();

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, null,
                    definicion.Nombre + ": " + (anterior == null ? "-" : anterior.Valor) + " → " + valorGuardado + " " + definicion.Unidad + ".");

                return ResultadoOperacion.Ok("Se actualizó: " + definicion.Nombre + ".");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }
    }
}
