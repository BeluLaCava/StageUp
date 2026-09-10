using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Traduccion
    {
        private const int LongitudMaximaTraduccion = 2000;

        private readonly MPP_Traduccion _mppTraduccion = new MPP_Traduccion();
        private readonly MPP_Idioma _mppIdioma = new MPP_Idioma();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<Traduccion> ListarConfiguracion(int idIdioma)
        {
            try
            {
                return _mppTraduccion.ListarConfiguracion(idIdioma);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Traduccion>();
            }
        }

        public ResultadoOperacion Guardar(
            int idIdioma,
            int idEtiquetaTraduccion,
            string textoTraducido,
            int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Idioma idioma = _mppIdioma.ObtenerPorId(idIdioma);
                if (idioma == null || !idioma.Activo)
                {
                    return ResultadoOperacion.Error("No se encontró el idioma seleccionado.");
                }

                string textoNormalizado = string.IsNullOrWhiteSpace(textoTraducido)
                    ? null
                    : textoTraducido.Trim();

                if (textoNormalizado == null)
                {
                    return ResultadoOperacion.Error("Ingresá la traducción antes de guardarla.");
                }

                if (textoNormalizado.Length > LongitudMaximaTraduccion)
                {
                    return ResultadoOperacion.Error(
                        "La traducción no puede superar los " + LongitudMaximaTraduccion + " caracteres.");
                }

                _mppTraduccion.Guardar(new Traduccion
                {
                    IdIdioma = idIdioma,
                    IdEtiquetaTraduccion = idEtiquetaTraduccion,
                    TextoTraducido = textoNormalizado
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable,
                    "MODIFICACION",
                    "Traduccion",
                    idEtiquetaTraduccion,
                    "Alta o actualización de una traducción para el idioma " + idioma.CodigoIdioma + ".");

                return ResultadoOperacion.Ok("La traducción se guardó correctamente.");
            });
        }

        public ResultadoOperacion Eliminar(
            int idIdioma,
            int idEtiquetaTraduccion,
            int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Idioma idioma = _mppIdioma.ObtenerPorId(idIdioma);
                if (idioma == null || !idioma.Activo)
                {
                    return ResultadoOperacion.Error("No se encontró el idioma seleccionado.");
                }

                _mppTraduccion.Eliminar(idIdioma, idEtiquetaTraduccion);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable,
                    "BAJA",
                    "Traduccion",
                    idEtiquetaTraduccion,
                    "Eliminación de una traducción para el idioma " + idioma.CodigoIdioma + ".");

                return ResultadoOperacion.Ok("La traducción se eliminó. La interfaz usará el texto predeterminado.");
            });
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
    }
}
