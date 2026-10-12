using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Seguridad;

namespace StageUp.BLL
{
    // Ítems 9 y 10: pares de claves RSA de StageUp (script 66).
    //
    // La versión Activa cifra lo nuevo (tarjetas de la billetera y
    // exportaciones de la bitácora); las Retiradas se conservan para leer lo
    // viejo. La clave privada vive en la base cifrada con la clave simétrica
    // de la configuración privada y nunca se muestra. La rotación genera una
    // versión nueva, vuelve a proteger las tarjetas con ella y queda en la
    // bitácora con el responsable y el motivo.
    public class BLL_ClaveCifrado
    {
        public const int LongitudMinimaMotivo = 10;
        public const int LongitudMaximaMotivo = 300;

        private readonly MPP_ClaveCifrado _mpp = new MPP_ClaveCifrado();
        private readonly MPP_TarjetaGuardada _mppTarjeta = new MPP_TarjetaGuardada();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // La clave simétrica (AES-256) de la configuración privada es la que
        // protege las claves privadas: sin ella no se puede cifrar nada.
        public static bool HayClaveMaestra()
        {
            try
            {
                EncriptadorSimetrico.CalcularHuella("StageUp", "Prueba");
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public List<ClaveCifrado> Listar()
        {
            try
            {
                return _mpp.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<ClaveCifrado>();
            }
        }

        // Versión activa. Si todavía no hay ninguna, crea la primera (queda en
        // la bitácora). Tira InvalidOperationException si falta la clave
        // maestra en la configuración privada.
        public ClaveCifrado ObtenerActiva(string motivoSiSeCrea)
        {
            ClaveCifrado activa = _mpp.ObtenerActiva();
            if (activa != null)
            {
                return activa;
            }

            ProtectorDeClavesAsimetricas.ParDeClavesProtegido par = ProtectorDeClavesAsimetricas.GenerarParProtegido();
            int idClave;
            int? idAnterior;
            int version;
            string resultado = _mpp.Registrar(new ClaveCifrado
            {
                ClavePublica = par.ClavePublica,
                ClavePrivadaProtegida = par.ClavePrivadaProtegida,
                Huella = par.Huella,
                Motivo = motivoSiSeCrea
            }, true, out idClave, out idAnterior, out version);

            if (resultado == "OK")
            {
                _bitacora.Registrar(null, "ALTA", "ClaveCifrado", idClave,
                    "Se creó la versión " + version + " del par de claves de cifrado (huella " + par.Huella + "). " + motivoSiSeCrea,
                    "StageUp.Sistema");
            }

            return _mpp.ObtenerActiva();
        }

        public ClaveCifrado ObtenerPorVersion(int version)
        {
            return _mpp.ObtenerPorId(new ClaveCifrado { Version = version });
        }

        // Clave privada en claro, solo en memoria, para descifrar.
        public string ObtenerClavePrivada(int idClaveCifrado)
        {
            ClaveCifrado clave = _mpp.ObtenerPorId(new ClaveCifrado { IdClaveCifrado = idClaveCifrado });
            if (clave == null)
            {
                throw new InvalidOperationException("No se encontró la versión de clave con la que se cifraron los datos.");
            }

            return ProtectorDeClavesAsimetricas.RecuperarClavePrivada(clave.ClavePrivadaProtegida);
        }

        public int ContarPendientesDeReenvolver()
        {
            try
            {
                ClaveCifrado activa = _mpp.ObtenerActiva();
                return activa == null ? 0 : _mppTarjeta.ListarParaReenvolver(activa).Count;
            }
            catch (ErrorAccesoDatosException)
            {
                return 0;
            }
        }

        // Ítem 10: rotación auditada. Genera la versión nueva (la anterior
        // queda Retirada) y vuelve a proteger las tarjetas guardadas.
        public ResultadoOperacion<string> Rotar(int idUsuarioInterno, string motivo, bool confirmado)
        {
            string motivoLimpio = (motivo ?? string.Empty).Trim();
            if (motivoLimpio.Length < LongitudMinimaMotivo || motivoLimpio.Length > LongitudMaximaMotivo)
            {
                return ResultadoOperacion<string>.Error(
                    "Escribí el motivo de la rotación (entre " + LongitudMinimaMotivo + " y " + LongitudMaximaMotivo + " caracteres). Queda en la bitácora.");
            }

            if (!confirmado)
            {
                return ResultadoOperacion<string>.Error("Confirmá que querés generar un par de claves nuevo y retirar el actual.");
            }

            if (!HayClaveMaestra())
            {
                return ResultadoOperacion<string>.Error(
                    "Falta la clave simétrica (ClaveEncriptacionSimetrica) en la configuración privada: sin ella no se pueden proteger las claves.");
            }

            ProtectorDeClavesAsimetricas.ParDeClavesProtegido par = ProtectorDeClavesAsimetricas.GenerarParProtegido();
            int idNueva;
            int? idAnterior;
            int version;
            try
            {
                _mpp.Registrar(new ClaveCifrado
                {
                    ClavePublica = par.ClavePublica,
                    ClavePrivadaProtegida = par.ClavePrivadaProtegida,
                    Huella = par.Huella,
                    Motivo = motivoLimpio,
                    IdUsuarioInternoAlta = idUsuarioInterno
                }, false, out idNueva, out idAnterior, out version);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<string>.Error("No se pudo generar el par de claves nuevo. No se cambió nada.");
            }

            ClaveCifrado anterior = idAnterior.HasValue ? _mpp.ObtenerPorId(new ClaveCifrado { IdClaveCifrado = idAnterior.Value }) : null;
            _bitacora.RegistrarInterno(idUsuarioInterno, "ROTACION_CLAVES", "ClaveCifrado", idNueva,
                "Rotación de claves de cifrado: se generó la versión " + version + " (huella " + par.Huella + ")" +
                (anterior == null ? string.Empty : " y se retiró la versión " + anterior.Version + " (huella " + anterior.Huella + ")") +
                ". Motivo: " + motivoLimpio);

            int reenvueltas;
            int fallidas;
            ReenvolverTarjetas(out reenvueltas, out fallidas);
            _bitacora.RegistrarInterno(idUsuarioInterno, "ROTACION_CLAVES", "ClaveCifrado", idNueva,
                "Tarjetas de la billetera protegidas con la versión " + version + ": " + reenvueltas +
                (fallidas > 0 ? ". No se pudieron proteger: " + fallidas + " (se pueden reintentar)." : "."));

            string mensaje = "Se generó la versión " + version + " del par de claves y se retiró la anterior. " +
                             (reenvueltas == 1 ? "Se volvió a proteger 1 tarjeta" : "Se volvieron a proteger " + reenvueltas + " tarjetas") +
                             " con la clave nueva." +
                             (fallidas > 0 ? " Quedaron " + fallidas + " pendientes: usá \"Reintentar\"." : string.Empty);
            return fallidas > 0
                ? ResultadoOperacion<string>.OkAlternativo(mensaje, mensaje, "REENVOLVER_PENDIENTE")
                : ResultadoOperacion<string>.Ok(mensaje, mensaje);
        }

        // Reintenta las tarjetas que quedaron con una versión anterior.
        public ResultadoOperacion<string> ReintentarPendientes(int idUsuarioInterno)
        {
            int reenvueltas;
            int fallidas;
            try
            {
                ReenvolverTarjetas(out reenvueltas, out fallidas);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<string>.Error("No se pudo completar la operación. Probá de nuevo.");
            }

            _bitacora.RegistrarInterno(idUsuarioInterno, "ROTACION_CLAVES", "ClaveCifrado", null,
                "Reintento de protección de tarjetas con la clave activa: " + reenvueltas + " protegidas" +
                (fallidas > 0 ? ", " + fallidas + " con error." : "."));

            string mensaje = reenvueltas + " tarjeta(s) protegida(s) con la clave activa." +
                             (fallidas > 0 ? " Siguen pendientes " + fallidas + "." : string.Empty);
            return fallidas > 0 ? ResultadoOperacion<string>.Error(mensaje) : ResultadoOperacion<string>.Ok(mensaje, mensaje);
        }

        private void ReenvolverTarjetas(out int reenvueltas, out int fallidas)
        {
            reenvueltas = 0;
            fallidas = 0;
            ClaveCifrado activa = _mpp.ObtenerActiva();
            if (activa == null)
            {
                return;
            }

            Dictionary<int, string> privadas = new Dictionary<int, string>();
            foreach (TarjetaGuardada tarjeta in _mppTarjeta.ListarParaReenvolver(activa))
            {
                try
                {
                    int idAnterior = tarjeta.IdClaveCifrado.Value;
                    string privada;
                    if (!privadas.TryGetValue(idAnterior, out privada))
                    {
                        privada = ObtenerClavePrivada(idAnterior);
                        privadas[idAnterior] = privada;
                    }

                    tarjeta.ClaveCifrada = EncriptadorHibrido.ReenvolverClave(tarjeta.ClaveCifrada, privada, activa.ClavePublica);
                    tarjeta.IdClaveCifrado = activa.IdClaveCifrado;
                    if (_mppTarjeta.Reenvolver(tarjeta, idAnterior))
                    {
                        reenvueltas++;
                    }
                }
                catch (Exception ex) when (ex is CryptographicException || ex is ArgumentException || ex is InvalidOperationException ||
                                           ex is FormatException || ex is ErrorAccesoDatosException)
                {
                    fallidas++;
                }
            }

            privadas.Clear();
        }

        public static string DescribirVersion(ClaveCifrado clave)
        {
            return clave == null ? "Sin claves generadas"
                : "Versión " + clave.Version.ToString(CultureInfo.InvariantCulture) + " · " + clave.Huella;
        }
    }
}
