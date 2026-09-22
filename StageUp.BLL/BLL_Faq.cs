using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // ABM real de las preguntas frecuentes del centro de ayuda (ítem 37 del
    // checklist de correcciones). ListarActivas() es la que ya usaba el
    // centro de ayuda público; el resto de los métodos son nuevos, para el
    // panel de administración.
    public class BLL_Faq
    {
        private const int LongitudMaximaPregunta = 300;
        private const int OrdenMinimo = 1;
        private const int OrdenMaximo = 9999;

        private readonly MPP_Faq _mpp = new MPP_Faq();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const string TipoEntidadBitacora = "Faq";

        public List<Faq> ListarActivas()
        {
            try
            {
                return _mpp.ListarActivas();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Faq>();
            }
        }

        public List<Faq> Listar()
        {
            try
            {
                return _mpp.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Faq>();
            }
        }

        public Faq ObtenerPorId(int idFaq)
        {
            try
            {
                return _mpp.ObtenerPorId(new Faq { IdFaq = idFaq });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion<int> Registrar(
            string pregunta, string respuesta, int orden, bool activo, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                string preguntaNormalizada;
                string respuestaNormalizada;
                ResultadoOperacion validacion = ValidarDatos(
                    pregunta, respuesta, orden, out preguntaNormalizada, out respuestaNormalizada);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                int idFaq = _mpp.Insertar(new Faq
                {
                    Pregunta = preguntaNormalizada,
                    Respuesta = respuestaNormalizada,
                    Orden = orden,
                    Activo = activo
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", TipoEntidadBitacora, idFaq,
                    "Alta de la pregunta frecuente \"" + preguntaNormalizada + "\".");

                return ResultadoOperacion<int>.Ok(idFaq, "La pregunta frecuente se creó correctamente.");
            });
        }

        public ResultadoOperacion Modificar(
            int idFaq, string pregunta, string respuesta, int orden, bool activo, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Faq faqActual = _mpp.ObtenerPorId(new Faq { IdFaq = idFaq });
                if (faqActual == null)
                {
                    return ResultadoOperacion.Error("No se encontró la pregunta frecuente seleccionada.");
                }

                string preguntaNormalizada;
                string respuestaNormalizada;
                ResultadoOperacion validacion = ValidarDatos(
                    pregunta, respuesta, orden, out preguntaNormalizada, out respuestaNormalizada);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                _mpp.Modificar(new Faq
                {
                    IdFaq = idFaq,
                    Pregunta = preguntaNormalizada,
                    Respuesta = respuestaNormalizada,
                    Orden = orden,
                    Activo = activo
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idFaq,
                    "Modificación de la pregunta frecuente \"" + preguntaNormalizada + "\".");

                return ResultadoOperacion.Ok("La pregunta frecuente se actualizó correctamente.");
            });
        }

        public ResultadoOperacion DarDeBaja(int idFaq, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Faq faq = _mpp.ObtenerPorId(new Faq { IdFaq = idFaq });
                if (faq == null || !faq.Activo)
                {
                    return ResultadoOperacion.Error("No se encontró la pregunta frecuente seleccionada.");
                }

                _mpp.DarDeBaja(faq);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", TipoEntidadBitacora, idFaq,
                    "Baja de la pregunta frecuente \"" + faq.Pregunta + "\" (deja de mostrarse en el centro de ayuda).");

                return ResultadoOperacion.Ok("La pregunta frecuente se dio de baja y dejó de mostrarse en el centro de ayuda.");
            });
        }

        public ResultadoOperacion Reactivar(int idFaq, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                Faq faq = _mpp.ObtenerPorId(new Faq { IdFaq = idFaq });
                if (faq == null || faq.Activo)
                {
                    return ResultadoOperacion.Error("No se encontró la pregunta frecuente seleccionada.");
                }

                _mpp.Reactivar(faq);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idFaq,
                    "Reactivación de la pregunta frecuente \"" + faq.Pregunta + "\" (vuelve a mostrarse en el centro de ayuda).");

                return ResultadoOperacion.Ok("La pregunta frecuente se activó y ya se muestra en el centro de ayuda.");
            });
        }

        private static ResultadoOperacion ValidarDatos(
            string pregunta, string respuesta, int orden, out string preguntaNormalizada, out string respuestaNormalizada)
        {
            preguntaNormalizada = string.IsNullOrWhiteSpace(pregunta) ? null : pregunta.Trim();
            respuestaNormalizada = string.IsNullOrWhiteSpace(respuesta) ? null : respuesta.Trim();

            if (preguntaNormalizada == null)
            {
                return ResultadoOperacion.Error("Ingresá la pregunta.");
            }

            if (preguntaNormalizada.Length > LongitudMaximaPregunta)
            {
                return ResultadoOperacion.Error(
                    "La pregunta no puede superar los " + LongitudMaximaPregunta + " caracteres.");
            }

            if (respuestaNormalizada == null)
            {
                return ResultadoOperacion.Error("Ingresá la respuesta.");
            }

            if (orden < OrdenMinimo || orden > OrdenMaximo)
            {
                return ResultadoOperacion.Error(
                    "Ingresá un orden entre " + OrdenMinimo + " y " + OrdenMaximo + ".");
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
