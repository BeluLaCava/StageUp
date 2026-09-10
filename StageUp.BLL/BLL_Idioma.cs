using System;
using System.Collections.Generic;
using System.Globalization;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Idioma
    {
        private const int LongitudMaximaNombre = 100;
        private const int LongitudMaximaCodigo = 20;

        private readonly MPP_Idioma _mppIdioma = new MPP_Idioma();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<Idioma> Listar()
        {
            try
            {
                return _mppIdioma.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Idioma>();
            }
        }

        public Idioma ObtenerPorId(int idIdioma)
        {
            try
            {
                return _mppIdioma.ObtenerPorId(idIdioma);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion<int> Registrar(
            string nombreIdioma,
            string codigoIdioma,
            bool esPredeterminado,
            int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                string nombreNormalizado;
                string codigoNormalizado;
                ResultadoOperacion validacion = ValidarDatos(
                    nombreIdioma, codigoIdioma, out nombreNormalizado, out codigoNormalizado);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                if (ExisteCodigo(codigoNormalizado, null))
                {
                    return ResultadoOperacion<int>.Error("Ya existe un idioma con ese código.");
                }

                int idIdioma = _mppIdioma.Insertar(new Idioma
                {
                    NombreIdioma = nombreNormalizado,
                    CodigoIdioma = codigoNormalizado,
                    EsPredeterminado = esPredeterminado
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable,
                    "ALTA",
                    "Idioma",
                    idIdioma,
                    "Alta del idioma " + nombreNormalizado + " (" + codigoNormalizado + ").");

                return ResultadoOperacion<int>.Ok(idIdioma, "El idioma se creó correctamente.");
            });
        }

        public ResultadoOperacion Modificar(
            int idIdioma,
            string nombreIdioma,
            string codigoIdioma,
            bool esPredeterminado,
            int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Idioma idiomaActual = _mppIdioma.ObtenerPorId(idIdioma);
                if (idiomaActual == null || !idiomaActual.Activo)
                {
                    return ResultadoOperacion.Error("No se encontró el idioma seleccionado.");
                }

                string nombreNormalizado;
                string codigoNormalizado;
                ResultadoOperacion validacion = ValidarDatos(
                    nombreIdioma, codigoIdioma, out nombreNormalizado, out codigoNormalizado);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                if (idiomaActual.EsPredeterminado && !esPredeterminado)
                {
                    return ResultadoOperacion.Error(
                        "Para cambiar el idioma predeterminado, editá otro idioma y marcá esa opción.");
                }

                if (ExisteCodigo(codigoNormalizado, idIdioma))
                {
                    return ResultadoOperacion.Error("Ya existe otro idioma con ese código.");
                }

                _mppIdioma.Modificar(new Idioma
                {
                    IdIdioma = idIdioma,
                    NombreIdioma = nombreNormalizado,
                    CodigoIdioma = codigoNormalizado,
                    EsPredeterminado = esPredeterminado
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable,
                    "MODIFICACION",
                    "Idioma",
                    idIdioma,
                    "Modificación del idioma " + nombreNormalizado + " (" + codigoNormalizado + ").");

                return ResultadoOperacion.Ok("El idioma se actualizó correctamente.");
            });
        }

        public ResultadoOperacion DarDeBaja(int idIdioma, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Idioma idioma = _mppIdioma.ObtenerPorId(idIdioma);
                if (idioma == null || !idioma.Activo)
                {
                    return ResultadoOperacion.Error("No se encontró el idioma seleccionado.");
                }

                if (idioma.EsPredeterminado)
                {
                    return ResultadoOperacion.Error(
                        "No podés dar de baja el idioma predeterminado. Marcá otro idioma como predeterminado primero.");
                }

                _mppIdioma.DarDeBaja(idIdioma);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable,
                    "BAJA",
                    "Idioma",
                    idIdioma,
                    "Baja del idioma " + idioma.NombreIdioma + " (" + idioma.CodigoIdioma + ").");

                return ResultadoOperacion.Ok("El idioma se dio de baja correctamente.");
            });
        }

        private bool ExisteCodigo(string codigoIdioma, int? idIdiomaExcluido)
        {
            foreach (Idioma idioma in _mppIdioma.Listar())
            {
                if ((!idIdiomaExcluido.HasValue || idioma.IdIdioma != idIdiomaExcluido.Value) &&
                    string.Equals(idioma.CodigoIdioma, codigoIdioma, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ResultadoOperacion ValidarDatos(
            string nombreIdioma,
            string codigoIdioma,
            out string nombreNormalizado,
            out string codigoNormalizado)
        {
            nombreNormalizado = string.IsNullOrWhiteSpace(nombreIdioma) ? null : nombreIdioma.Trim();
            codigoNormalizado = string.IsNullOrWhiteSpace(codigoIdioma) ? null : codigoIdioma.Trim();

            if (nombreNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá el nombre del idioma.");
            }

            if (nombreNormalizado.Length > LongitudMaximaNombre)
            {
                return ResultadoOperacion.Error(
                    "El nombre del idioma no puede superar los " + LongitudMaximaNombre + " caracteres.");
            }

            if (codigoNormalizado == null)
            {
                return ResultadoOperacion.Error("Ingresá el código de cultura del idioma.");
            }

            if (codigoNormalizado.Length > LongitudMaximaCodigo)
            {
                return ResultadoOperacion.Error(
                    "El código del idioma no puede superar los " + LongitudMaximaCodigo + " caracteres.");
            }

            try
            {
                codigoNormalizado = CultureInfo.GetCultureInfo(codigoNormalizado).Name;
            }
            catch (CultureNotFoundException)
            {
                return ResultadoOperacion.Error("Ingresá un código de cultura válido, por ejemplo es-AR, en-US o pt-BR.");
            }

            return ResultadoOperacion.Ok();
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
