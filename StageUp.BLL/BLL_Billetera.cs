using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Seguridad;

namespace StageUp.BLL
{







    public class BLL_Billetera
    {
        public const int MaximoTarjetas = 5;
        private const int LongitudMaximaAlias = 40;
        private static readonly Regex PatronAlias = new Regex(@"^[\p{L}\p{N}][\p{L}\p{N} .,'\-]*$", RegexOptions.Compiled);
        private const string MensajeNoDisponible = "No pudimos usar la tarjeta guardada en este momento. Elegí \"Usar otra tarjeta\" y cargá sus datos.";

        private readonly MPP_TarjetaGuardada _mpp = new MPP_TarjetaGuardada();
        private readonly BLL_ClaveCifrado _claves = new BLL_ClaveCifrado();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<TarjetaGuardada> Listar(int idUsuarioExterno)
        {
            try
            {
                return _mpp.ListarPorUsuario(new TarjetaGuardada { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<TarjetaGuardada>();
            }
        }

        public static string Describir(TarjetaGuardada tarjeta)
        {
            return tarjeta.Marca + " terminada en " + tarjeta.UltimosDigitos + " · vence " +
                   tarjeta.MesVencimiento.ToString("00", CultureInfo.InvariantCulture) + "/" +
                   (tarjeta.AnioVencimiento % 100).ToString("00", CultureInfo.InvariantCulture);
        }

        public ResultadoOperacion<TarjetaGuardada> Guardar(int idUsuarioExterno, DatosTarjeta datos, string alias, bool preferida)
        {
            ResultadoOperacion<string> validacion = ValidadorTarjeta.ValidarParaGuardar(datos);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<TarjetaGuardada>.Error(validacion.Mensaje);
            }

            string numero = ValidadorTarjeta.NormalizarNumero(datos.Numero);
            string ultimos = ValidadorTarjeta.ObtenerUltimosDigitos(numero);
            string aliasFinal;
            string errorAlias = NormalizarAlias(alias, validacion.Valor + " " + ultimos, out aliasFinal);
            if (errorAlias != null)
            {
                return ResultadoOperacion<TarjetaGuardada>.Error(errorAlias);
            }

            if (!BLL_ClaveCifrado.HayClaveMaestra())
            {
                return ResultadoOperacion<TarjetaGuardada>.Error(
                    "La billetera no está disponible en este momento: falta configurar el cifrado. Podés pagar cargando los datos de la tarjeta.");
            }

            int anio = datos.AnioVencimiento < 100 ? datos.AnioVencimiento + 2000 : datos.AnioVencimiento;
            TarjetaGuardada tarjeta = new TarjetaGuardada
            {
                IdUsuarioExterno = idUsuarioExterno,
                Alias = aliasFinal,
                Marca = validacion.Valor,
                UltimosDigitos = ultimos,
                MesVencimiento = datos.MesVencimiento,
                AnioVencimiento = anio,
                Preferida = preferida
            };

            try
            {
                ClaveCifrado clave = _claves.ObtenerActiva("Creada automáticamente al guardar la primera tarjeta en una billetera.");
                string iv;
                string claveCifrada;
                string datosCifrados;
                EncriptadorHibrido.Encriptar(numero + "\n" + ValidadorTarjeta.NormalizarTitular(datos.Titular), clave.ClavePublica,
                    out iv, out claveCifrada, out datosCifrados);

                tarjeta.Iv = iv;
                tarjeta.ClaveCifrada = claveCifrada;
                tarjeta.DatosCifrados = datosCifrados;
                tarjeta.IdClaveCifrado = clave.IdClaveCifrado;
                tarjeta.Huella = EncriptadorSimetrico.CalcularHuella(numero, "TarjetaGuardada");

                int id;
                bool quedoPreferida;
                string resultado = _mpp.Insertar(tarjeta, MaximoTarjetas, out id, out quedoPreferida);
                switch (resultado)
                {
                    case "REPETIDA":
                        return ResultadoOperacion<TarjetaGuardada>.Error("Esa tarjeta ya está en tu billetera.");
                    case "ALIAS_REPETIDO":
                        return ResultadoOperacion<TarjetaGuardada>.Error("Ya tenés una tarjeta con el alias \"" + aliasFinal + "\". Elegí otro.");
                    case "LIMITE":
                        return ResultadoOperacion<TarjetaGuardada>.Error(
                            "Podés guardar hasta " + MaximoTarjetas + " tarjetas. Eliminá alguna para agregar otra.");
                }

                tarjeta.IdTarjetaGuardada = id;
                tarjeta.Preferida = quedoPreferida;
                _bitacora.Registrar(idUsuarioExterno, "ALTA", "TarjetaGuardada", id,
                    "Guardó en su billetera la tarjeta " + tarjeta.Marca + " terminada en " + tarjeta.UltimosDigitos +
                    " (alias \"" + aliasFinal + "\"" + (quedoPreferida ? ", preferida" : string.Empty) + "), cifrada con la clave versión " + clave.Version + ".");


                tarjeta.Iv = null;
                tarjeta.ClaveCifrada = null;
                tarjeta.DatosCifrados = null;
                tarjeta.Huella = null;
                return ResultadoOperacion<TarjetaGuardada>.Ok(tarjeta,
                    "Guardamos la tarjeta " + tarjeta.Marca + " terminada en " + tarjeta.UltimosDigitos + " en tu billetera.");
            }
            catch (Exception ex) when (ex is ErrorAccesoDatosException || ex is InvalidOperationException || ex is CryptographicException)
            {
                return ResultadoOperacion<TarjetaGuardada>.Error("No se pudo guardar la tarjeta en este momento. Probá de nuevo.");
            }
        }

        public ResultadoOperacion CambiarAlias(int idUsuarioExterno, int idTarjetaGuardada, string alias)
        {
            string aliasFinal;
            string error = NormalizarAlias(alias, null, out aliasFinal);
            if (error != null)
            {
                return ResultadoOperacion.Error(error);
            }

            try
            {
                string resultado = _mpp.CambiarAlias(new TarjetaGuardada
                {
                    IdTarjetaGuardada = idTarjetaGuardada,
                    IdUsuarioExterno = idUsuarioExterno,
                    Alias = aliasFinal
                });
                if (resultado == "ALIAS_REPETIDO")
                {
                    return ResultadoOperacion.Error("Ya tenés una tarjeta con el alias \"" + aliasFinal + "\". Elegí otro.");
                }

                if (resultado != "OK")
                {
                    return ResultadoOperacion.Error("La tarjeta ya no está en tu billetera.");
                }

                _bitacora.Registrar(idUsuarioExterno, "MODIFICACION", "TarjetaGuardada", idTarjetaGuardada,
                    "Cambió el alias de una tarjeta de su billetera a \"" + aliasFinal + "\".");
                return ResultadoOperacion.Ok("Se cambió el alias.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No se pudo cambiar el alias. Probá de nuevo.");
            }
        }

        public ResultadoOperacion MarcarPreferida(int idUsuarioExterno, int idTarjetaGuardada)
        {
            try
            {
                if (_mpp.MarcarPreferida(new TarjetaGuardada { IdTarjetaGuardada = idTarjetaGuardada, IdUsuarioExterno = idUsuarioExterno }) != "OK")
                {
                    return ResultadoOperacion.Error("La tarjeta ya no está en tu billetera.");
                }

                _bitacora.Registrar(idUsuarioExterno, "MODIFICACION", "TarjetaGuardada", idTarjetaGuardada,
                    "Marcó una tarjeta de su billetera como preferida.");
                return ResultadoOperacion.Ok("Listo: es tu tarjeta preferida. La vas a ver primero al pagar.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No se pudo marcar la tarjeta como preferida. Probá de nuevo.");
            }
        }

        public ResultadoOperacion Eliminar(int idUsuarioExterno, int idTarjetaGuardada)
        {
            try
            {
                if (_mpp.Eliminar(new TarjetaGuardada { IdTarjetaGuardada = idTarjetaGuardada, IdUsuarioExterno = idUsuarioExterno }) != "OK")
                {
                    return ResultadoOperacion.Error("La tarjeta ya no está en tu billetera.");
                }

                _bitacora.Registrar(idUsuarioExterno, "BAJA", "TarjetaGuardada", idTarjetaGuardada,
                    "Eliminó una tarjeta de su billetera (se borraron sus datos cifrados).");
                return ResultadoOperacion.Ok("Eliminamos la tarjeta de tu billetera.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No se pudo eliminar la tarjeta. Probá de nuevo.");
            }
        }



        public ResultadoOperacion<DatosTarjeta> ObtenerDatosParaPagar(int idUsuarioExterno, int idTarjetaGuardada, string codigoSeguridad)
        {
            TarjetaGuardada tarjeta;
            try
            {
                tarjeta = _mpp.ObtenerParaUsar(new TarjetaGuardada { IdTarjetaGuardada = idTarjetaGuardada, IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<DatosTarjeta>.Error(MensajeNoDisponible);
            }

            if (tarjeta == null)
            {
                return ResultadoOperacion<DatosTarjeta>.Error("La tarjeta elegida ya no está en tu billetera. Elegí otra.");
            }

            if (tarjeta.EstaVencida)
            {
                return ResultadoOperacion<DatosTarjeta>.Error("La tarjeta \"" + tarjeta.Alias + "\" está vencida. Elegí otra o cargá una nueva.");
            }

            if (string.IsNullOrWhiteSpace(codigoSeguridad))
            {
                return ResultadoOperacion<DatosTarjeta>.Error("Escribí el código de seguridad de la tarjeta \"" + tarjeta.Alias + "\".");
            }

            try
            {
                string privada = _claves.ObtenerClavePrivada(tarjeta.IdClaveCifrado.Value);
                string contenido = EncriptadorHibrido.Desencriptar(tarjeta.Iv, tarjeta.ClaveCifrada, tarjeta.DatosCifrados, privada);
                string[] partes = contenido.Split('\n');
                if (partes.Length != 2)
                {
                    return ResultadoOperacion<DatosTarjeta>.Error(MensajeNoDisponible);
                }

                return ResultadoOperacion<DatosTarjeta>.Ok(new DatosTarjeta
                {
                    Numero = partes[0],
                    Titular = partes[1],
                    MesVencimiento = tarjeta.MesVencimiento,
                    AnioVencimiento = tarjeta.AnioVencimiento,
                    CodigoSeguridad = codigoSeguridad
                });
            }
            catch (Exception ex) when (ex is CryptographicException || ex is ArgumentException || ex is InvalidOperationException ||
                                       ex is FormatException || ex is ErrorAccesoDatosException)
            {
                return ResultadoOperacion<DatosTarjeta>.Error(MensajeNoDisponible);
            }
        }

        public void RegistrarUso(int idTarjetaGuardada)
        {
            try
            {
                _mpp.RegistrarUso(new TarjetaGuardada { IdTarjetaGuardada = idTarjetaGuardada });
            }
            catch (ErrorAccesoDatosException)
            {

            }
        }

        private static string NormalizarAlias(string alias, string porDefecto, out string aliasFinal)
        {
            aliasFinal = Regex.Replace((alias ?? string.Empty).Trim(), @"\s+", " ");
            if (aliasFinal.Length == 0 && porDefecto != null)
            {
                aliasFinal = porDefecto;
            }

            if (aliasFinal.Length < 2 || aliasFinal.Length > LongitudMaximaAlias)
            {
                return "El alias tiene que tener entre 2 y " + LongitudMaximaAlias + " caracteres.";
            }

            if (!PatronAlias.IsMatch(aliasFinal))
            {
                return "El alias puede tener letras, números, espacios y . , ' -";
            }

            return null;
        }
    }
}
