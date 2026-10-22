using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{





    public class BLL_Faq
    {
        public const string EstadoBorrador = "Borrador";
        public const string EstadoActivo = "Activo";
        public const string EstadoInactivo = "Inactivo";

        private const int LongitudMinimaPregunta = 10;
        private const int LongitudMaximaPregunta = 300;
        private const int LongitudMinimaRespuesta = 30;
        private const int LongitudMaximaRespuesta = 4000;
        private const int LongitudMaximaPalabras = 300;
        private const int OrdenMinimo = 1;
        private const int OrdenMaximo = 9999;
        private const string TipoEntidadBitacora = "Faq";
        private const string MensajeErrorGuardado = "No fue posible guardar el contenido de ayuda en este momento. No se aplicó ningún cambio; probá de nuevo.";
        private static readonly Regex PatronHtml = new Regex(@"<\s*/?\s*[a-zA-Z][^>]*>", RegexOptions.Compiled);

        private readonly MPP_Faq _mpp = new MPP_Faq();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();



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


        public ResultadoOperacion<List<Faq>> ObtenerContenidoPublico()
        {
            try
            {
                return ResultadoOperacion<List<Faq>>.Ok(_mpp.ListarActivas());
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<Faq>>.Error("La ayuda no puede consultarse en este momento. Probá de nuevo en unos minutos.", "A1");
            }
        }

        public List<Faq> Listar(FiltroFaq filtro = null)
        {
            try
            {
                filtro = filtro ?? new FiltroFaq();
                if (filtro.Estado != EstadoBorrador && filtro.Estado != EstadoActivo && filtro.Estado != EstadoInactivo)
                {
                    filtro.Estado = null;
                }

                return _mpp.Listar(filtro);
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

        public List<CategoriaAyuda> ListarCategorias()
        {
            try
            {
                return _mpp.ListarCategorias();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<CategoriaAyuda>();
            }
        }

        public List<ConsultaAsistenteSinRespuesta> ListarConsultasSinRespuesta()
        {
            try
            {
                return _mpp.ListarConsultasSinRespuesta();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<ConsultaAsistenteSinRespuesta>();
            }
        }

        public ResultadoOperacion DescartarConsultaSinRespuesta(string textoNormalizado, int idUsuarioInternoResponsable)
        {
            if (string.IsNullOrWhiteSpace(textoNormalizado))
            {
                return ResultadoOperacion.Error("No se indicó la consulta.");
            }

            try
            {
                _mpp.MarcarConsultaAtendida(textoNormalizado);
                _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, null,
                    "Marcó como atendida la consulta del asistente \"" + textoNormalizado + "\".");
                return ResultadoOperacion.Ok("La consulta se quitó de la lista.");
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No se pudo actualizar la consulta. Probá de nuevo.");
            }
        }



        public const int SugerenciaMaximaBasicas = 12;

        private string AvisoListaBasica(Faq faq)
        {
            if (faq == null || !faq.EsBasica || faq.Estado != EstadoActivo)
            {
                return string.Empty;
            }

            int cantidad;
            try
            {
                cantidad = _mpp.ListarActivas().Count(f => f.EsBasica);
            }
            catch (ErrorAccesoDatosException)
            {
                return string.Empty;
            }

            return cantidad > SugerenciaMaximaBasicas
                ? " Atención: hay " + cantidad + " preguntas básicas publicadas. Conviene que la lista sea breve (hasta " + SugerenciaMaximaBasicas +
                  " aprox.) y que lo específico lo resuelva el asistente."
                : string.Empty;
        }




        public ResultadoOperacion<Faq> ObtenerPublicada(int idFaq)
        {
            ResultadoOperacion<List<Faq>> contenido = ObtenerContenidoPublico();
            if (!contenido.Exitoso)
            {
                return ResultadoOperacion<Faq>.Error(contenido.Mensaje, "A1");
            }

            return ResultadoOperacion<Faq>.Ok(contenido.Valor.FirstOrDefault(f => f.IdFaq == idFaq));
        }


        public ResultadoOperacion<List<Faq>> ObtenerPreguntasBasicas()
        {
            ResultadoOperacion<List<Faq>> contenido = ObtenerContenidoPublico();
            if (!contenido.Exitoso)
            {
                return contenido;
            }

            return ResultadoOperacion<List<Faq>>.Ok(contenido.Valor
                .Where(f => f.EsBasica)
                .OrderBy(f => f.Orden)
                .ThenBy(f => f.OrdenCategoria)
                .ThenBy(f => f.IdFaq)
                .ToList());
        }

        public static string DescribirEstado(string estado)
        {
            return estado == EstadoActivo ? "Publicado" : estado == EstadoInactivo ? "Desactivado" : "Borrador";
        }





        public ResultadoOperacion<int> Registrar(Faq datos, int idUsuarioInternoResponsable, string textoConsultaOrigen = null)
        {
            Faq normalizado;
            ResultadoOperacion validacion = Validar(datos, null, out normalizado);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<int>.Error(validacion.Mensaje, validacion.CodigoAlternativo);
            }

            int idFaq;
            try
            {
                idFaq = _mpp.Insertar(normalizado);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<int>.Error(MensajeErrorGuardado, "A14");
            }

            _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "ALTA", TipoEntidadBitacora, idFaq,
                "Alta de contenido de ayuda \"" + normalizado.Pregunta + "\"" +
                (normalizado.IdCategoriaAyuda.HasValue ? " en la categoría " + NombreCategoria(normalizado.IdCategoriaAyuda) : " sin categoría") +
                " (" + DescribirEstado(normalizado.Estado).ToLowerInvariant() + (normalizado.EsBasica ? ", pregunta básica" : string.Empty) + ").");

            if (!string.IsNullOrWhiteSpace(textoConsultaOrigen))
            {
                try
                {
                    _mpp.MarcarConsultaAtendida(textoConsultaOrigen);
                }
                catch (ErrorAccesoDatosException)
                {
                }
            }

            return ResultadoOperacion<int>.Ok(idFaq, (normalizado.Estado == EstadoActivo
                ? (normalizado.EsBasica
                    ? "El contenido de ayuda se guardó y quedó publicado entre las preguntas básicas del centro de ayuda."
                    : "El contenido de ayuda se guardó y quedó publicado. No es una pregunta básica: no aparece en la lista del centro de ayuda, pero sí en la búsqueda.")
                : "El contenido de ayuda se guardó como borrador. Todavía no lo ven los usuarios.") + AvisoListaBasica(normalizado));
        }


        public ResultadoOperacion Modificar(Faq datos, int idUsuarioInternoResponsable)
        {
            Faq actual = ObtenerPorId(datos == null ? 0 : datos.IdFaq);
            if (actual == null)
            {
                return ResultadoOperacion.Error("No se encontró el contenido de ayuda seleccionado.");
            }

            Faq normalizado;
            ResultadoOperacion validacion = Validar(datos, actual.IdFaq, out normalizado);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            normalizado.IdFaq = actual.IdFaq;
            try
            {
                if (!_mpp.Modificar(normalizado))
                {
                    return ResultadoOperacion.Error("No se encontró el contenido de ayuda seleccionado.");
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error(MensajeErrorGuardado, "A14");
            }

            List<string> cambios = new List<string>();
            if (actual.Pregunta != normalizado.Pregunta) cambios.Add("título");
            if (actual.Respuesta != normalizado.Respuesta) cambios.Add("respuesta");
            if (actual.IdCategoriaAyuda != normalizado.IdCategoriaAyuda) cambios.Add("categoría (" + NombreCategoria(normalizado.IdCategoriaAyuda) + ")");
            if (actual.Estado != normalizado.Estado) cambios.Add("estado (" + DescribirEstado(actual.Estado) + " → " + DescribirEstado(normalizado.Estado) + ")");
            if (actual.EsBasica != normalizado.EsBasica) cambios.Add(normalizado.EsBasica ? "pasó a pregunta básica" : "dejó de ser pregunta básica");
            if ((actual.PalabrasClave ?? string.Empty) != (normalizado.PalabrasClave ?? string.Empty)) cambios.Add("palabras clave");
            if (actual.Orden != normalizado.Orden) cambios.Add("orden");

            _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, actual.IdFaq,
                "Modificación del contenido de ayuda \"" + normalizado.Pregunta + "\"" +
                (cambios.Count == 0 ? " (sin cambios)." : ": " + string.Join(", ", cambios) + "."));

            return ResultadoOperacion.Ok((normalizado.Estado == EstadoActivo && normalizado.EsBasica
                ? "El contenido de ayuda se actualizó correctamente y ya se ve así entre las preguntas básicas del centro de ayuda."
                : "El contenido de ayuda se actualizó correctamente.") + AvisoListaBasica(normalizado));
        }


        public ResultadoOperacion Publicar(int idFaq, int idUsuarioInternoResponsable)
        {
            Faq faq = ObtenerPorId(idFaq);
            if (faq == null)
            {
                return ResultadoOperacion.Error("No se encontró el contenido de ayuda seleccionado.");
            }

            if (faq.Estado == EstadoActivo)
            {
                return ResultadoOperacion.Error("El contenido ya está publicado en el centro de ayuda.", "A8");
            }

            Faq normalizado;
            faq.Estado = EstadoActivo;
            ResultadoOperacion validacion = Validar(faq, faq.IdFaq, out normalizado);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion.Error("No se puede publicar todavía: " + validacion.Mensaje + " Editalo y volvé a intentar.", validacion.CodigoAlternativo);
            }

            return CambiarEstado(faq, EstadoActivo, idUsuarioInternoResponsable,
                "Publicación del contenido de ayuda \"" + faq.Pregunta + "\".",
                faq.EsBasica
                    ? "El contenido se publicó entre las preguntas básicas del centro de ayuda."
                    : "El contenido se publicó (se encuentra con la búsqueda; no está en la lista de preguntas básicas).");
        }


        public ResultadoOperacion Desactivar(int idFaq, int idUsuarioInternoResponsable)
        {
            Faq faq = ObtenerPorId(idFaq);
            if (faq == null)
            {
                return ResultadoOperacion.Error("No se encontró el contenido de ayuda seleccionado.");
            }

            if (faq.Estado != EstadoActivo)
            {
                return ResultadoOperacion.Error("Solo se puede desactivar un contenido publicado.");
            }

            return CambiarEstado(faq, EstadoInactivo, idUsuarioInternoResponsable,
                "Desactivación del contenido de ayuda \"" + faq.Pregunta + "\" (deja de verse en el centro de ayuda).",
                "El contenido se desactivó: ya no se ve en el centro de ayuda" + (faq.EsBasica ? " ni entre las preguntas básicas." : ".") + " Se conserva su historial.");
        }

        private ResultadoOperacion CambiarEstado(Faq faq, string estado, int idUsuarioInternoResponsable, string descripcionBitacora, string mensaje)
        {
            try
            {
                if (!_mpp.CambiarEstado(new Faq { IdFaq = faq.IdFaq, Estado = estado }))
                {
                    return ResultadoOperacion.Error("No se encontró el contenido de ayuda seleccionado.");
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error(MensajeErrorGuardado, "A14");
            }

            _bitacora.RegistrarInterno(idUsuarioInternoResponsable, estado == EstadoActivo ? "MODIFICACION" : "BAJA", TipoEntidadBitacora, faq.IdFaq, descripcionBitacora);
            return ResultadoOperacion.Ok(mensaje);
        }



        private ResultadoOperacion Validar(Faq datos, int? idExcluir, out Faq normalizado)
        {
            try
            {
                return ValidarDatos(datos, idExcluir, out normalizado);
            }
            catch (ErrorAccesoDatosException)
            {
                normalizado = null;
                return ResultadoOperacion.Error(MensajeErrorGuardado, "A14");
            }
        }

        private ResultadoOperacion ValidarDatos(Faq datos, int? idExcluir, out Faq normalizado)
        {
            normalizado = null;
            if (datos == null)
            {
                return ResultadoOperacion.Error("Completá los datos del contenido.", "A10");
            }

            string pregunta = Regex.Replace((datos.Pregunta ?? string.Empty).Trim(), @"\s+", " ");
            string respuesta = (datos.Respuesta ?? string.Empty).Trim();
            string palabras = Regex.Replace((datos.PalabrasClave ?? string.Empty).Trim(), @"\s+", " ");
            string estado = datos.Estado == EstadoActivo || datos.Estado == EstadoInactivo ? datos.Estado : EstadoBorrador;

            List<string> faltantes = new List<string>();
            if (pregunta.Length == 0) faltantes.Add("la pregunta o título");
            if (respuesta.Length == 0) faltantes.Add("la respuesta");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Error("Completá " + string.Join(" y ", faltantes) + ".", "A10");
            }

            if (datos.Orden < OrdenMinimo || datos.Orden > OrdenMaximo)
            {
                return ResultadoOperacion.Error("Ingresá un orden entre " + OrdenMinimo + " y " + OrdenMaximo + ".", "A10");
            }

            List<CategoriaAyuda> categorias = _mpp.ListarCategorias().Where(c => c.Activa).ToList();
            CategoriaAyuda categoria = datos.IdCategoriaAyuda.HasValue ? categorias.FirstOrDefault(c => c.IdCategoriaAyuda == datos.IdCategoriaAyuda.Value) : null;
            if (categorias.Count == 0)
            {

                if (estado == EstadoActivo)
                {
                    return ResultadoOperacion.Error("No hay categorías temáticas disponibles: el contenido no puede publicarse. Guardalo como borrador.", "A3");
                }
            }
            else if (categoria == null)
            {
                return ResultadoOperacion.Error("Elegí una categoría temática existente para organizar el contenido.", "A11");
            }

            if (pregunta.Length < LongitudMinimaPregunta || pregunta.Length > LongitudMaximaPregunta)
            {
                return ResultadoOperacion.Error("La pregunta o título tiene que tener entre " + LongitudMinimaPregunta + " y " + LongitudMaximaPregunta + " caracteres.", "A13");
            }

            if (respuesta.Length < LongitudMinimaRespuesta || respuesta.Length > LongitudMaximaRespuesta)
            {
                return ResultadoOperacion.Error("La respuesta tiene que explicar la consulta: escribí entre " + LongitudMinimaRespuesta + " y " + LongitudMaximaRespuesta + " caracteres.", "A13");
            }

            if (PatronHtml.IsMatch(pregunta) || PatronHtml.IsMatch(respuesta) || PatronHtml.IsMatch(palabras))
            {
                return ResultadoOperacion.Error("El contenido no puede tener etiquetas HTML: escribilo como texto.", "A13");
            }

            if (Normalizar(respuesta) == Normalizar(pregunta))
            {
                return ResultadoOperacion.Error("La respuesta no puede repetir la pregunta: explicá cómo resolver la consulta.", "A13");
            }

            if (palabras.Length > LongitudMaximaPalabras)
            {
                return ResultadoOperacion.Error("Las palabras clave pueden tener hasta " + LongitudMaximaPalabras + " caracteres.", "A10");
            }

            string clave = Normalizar(pregunta);
            Faq duplicado = _mpp.ListarPreguntas().FirstOrDefault(f => f.IdFaq != (idExcluir ?? 0) && Normalizar(f.Pregunta) == clave);
            if (duplicado != null)
            {
                return ResultadoOperacion.Error(
                    "Ya existe un contenido de ayuda con esa pregunta (" + DescribirEstado(duplicado.Estado).ToLowerInvariant() +
                    "): \"" + duplicado.Pregunta + "\". Revisalo o cambiá el título.", "A12");
            }

            normalizado = new Faq
            {
                IdFaq = datos.IdFaq,
                Pregunta = pregunta,
                Respuesta = respuesta,
                Orden = datos.Orden,
                Estado = estado,
                IdCategoriaAyuda = categoria == null ? (int?)null : categoria.IdCategoriaAyuda,
                EsBasica = datos.EsBasica,
                PalabrasClave = palabras.Length == 0 ? null : palabras
            };
            return ResultadoOperacion.Ok();
        }

        private string NombreCategoria(int? idCategoria)
        {
            CategoriaAyuda categoria = idCategoria.HasValue ? ListarCategorias().FirstOrDefault(c => c.IdCategoriaAyuda == idCategoria.Value) : null;
            return categoria == null ? "sin categoría" : categoria.Nombre;
        }



        public static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            string descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder resultado = new StringBuilder(descompuesto.Length);
            foreach (char c in descompuesto)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                resultado.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            return Regex.Replace(resultado.ToString(), @"\s+", " ").Trim();
        }
    }
}
