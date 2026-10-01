using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using StageUp.BE.Entidades;
using StageUp.Seguridad;

namespace StageUp.BLL
{
    public class BLL_ExportacionSegura
    {
        private readonly BLL_Bitacora _bllBitacora = new BLL_Bitacora();

        public void GenerarNuevoParDeClaves(out string claveBase64Publica, out string claveBase64Privada)
        {
            EncriptadorAsimetrico.GenerarParDeClaves(out claveBase64Publica, out claveBase64Privada);
        }

        public bool HayClavesConfiguradas()
        {
            return EncriptadorAsimetrico.HayClavesConfiguradas();
        }

        public ResultadoOperacion<string> ExportarBitacoraCifrada(
            int? idUsuarioExternoResponsable, DateTime? fechaDesde, DateTime? fechaHasta,
            string tipoOperacion, string tipoEntidadAfectada)
        {
            return ExportarBitacoraCifrada(new FiltroRegistroActividad
            {
                IdUsuarioExternoResponsable = idUsuarioExternoResponsable,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TipoOperacion = tipoOperacion,
                TipoEntidadAfectada = tipoEntidadAfectada
            });
        }

        // Exporta los mismos registros que muestra la pantalla con los
        // filtros aplicados (hasta el máximo por búsqueda).
        public ResultadoOperacion<string> ExportarBitacoraCifrada(FiltroRegistroActividad filtro)
        {
            try
            {
                List<RegistroActividad> registros = _bllBitacora.Buscar(filtro);

                string xmlRegistros = SerializarRegistros(registros);

                string iv, claveCifrada, datosCifrados;
                EncriptadorHibrido.Encriptar(xmlRegistros, out iv, out claveCifrada, out datosCifrados);

                XElement envoltorio = new XElement("ExportacionBitacoraCifrada",
                    new XAttribute("fechaGeneracion", DateTime.Now.ToString("s")),
                    new XAttribute("cantidadRegistros", registros.Count),
                    new XElement("VectorInicializacion", iv),
                    new XElement("ClaveSimetricaCifrada", claveCifrada),
                    new XElement("DatosCifrados", datosCifrados));

                return ResultadoOperacion<string>.Ok(envoltorio.ToString());
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion<string>.Error(ex.Message);
            }
        }

        public ResultadoOperacion<string> ImportarYDescifrar(string envoltorioXml)
        {
            XElement envoltorio;
            try
            {
                envoltorio = XElement.Parse(envoltorioXml);
            }
            catch (XmlException)
            {
                return ResultadoOperacion<string>.Error("El archivo no es un XML válido.");
            }

            string iv = (string)envoltorio.Element("VectorInicializacion");
            string claveCifrada = (string)envoltorio.Element("ClaveSimetricaCifrada");
            string datosCifrados = (string)envoltorio.Element("DatosCifrados");

            if (iv == null || claveCifrada == null || datosCifrados == null)
            {
                return ResultadoOperacion<string>.Error("El archivo no tiene el formato esperado de una exportación de StageUp.");
            }

            try
            {
                string xmlOriginal = EncriptadorHibrido.Desencriptar(iv, claveCifrada, datosCifrados);
                return ResultadoOperacion<string>.Ok(xmlOriginal);
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion<string>.Error(ex.Message);
            }
            catch (FormatException)
            {
                return ResultadoOperacion<string>.Error("El archivo está corrupto o no es una exportación válida.");
            }
            catch (CryptographicException)
            {
                return ResultadoOperacion<string>.Error(
                    "No se pudo descifrar el archivo. Verificá que la clave privada configurada sea la misma que se usó para generarlo.");
            }
        }

        private static string SerializarRegistros(List<RegistroActividad> registros)
        {
            XmlSerializer serializador = new XmlSerializer(typeof(List<RegistroActividad>), new XmlRootAttribute("RegistrosActividad"));
            using (StringWriter escritor = new StringWriter())
            {
                serializador.Serialize(escritor, registros);
                return escritor.ToString();
            }
        }
    }
}
