using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.Seguridad;

namespace StageUp.BLL
{






    public class BLL_ExportacionSegura
    {
        private readonly BLL_Bitacora _bllBitacora = new BLL_Bitacora();
        private readonly BLL_ClaveCifrado _claves = new BLL_ClaveCifrado();

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



        public ResultadoOperacion<string> ExportarBitacoraCifrada(FiltroRegistroActividad filtro)
        {
            try
            {
                List<RegistroActividad> registros = _bllBitacora.Buscar(filtro);

                string xmlRegistros = SerializarRegistros(registros);

                if (!BLL_ClaveCifrado.HayClaveMaestra())
                {
                    return ResultadoOperacion<string>.Error(
                        "Falta la clave simétrica (ClaveEncriptacionSimetrica) en la configuración privada: sin ella no se puede cifrar la exportación.");
                }

                ClaveCifrado clave = _claves.ObtenerActiva("Creada automáticamente al exportar la bitácora por primera vez.");
                string iv, claveCifrada, datosCifrados;
                EncriptadorHibrido.Encriptar(xmlRegistros, clave.ClavePublica, out iv, out claveCifrada, out datosCifrados);

                XElement envoltorio = new XElement("ExportacionBitacoraCifrada",
                    new XAttribute("fechaGeneracion", DateTime.Now.ToString("s")),
                    new XAttribute("cantidadRegistros", registros.Count),
                    new XAttribute("versionClave", clave.Version),
                    new XAttribute("huellaClave", clave.Huella),
                    new XElement("VectorInicializacion", iv),
                    new XElement("ClaveSimetricaCifrada", claveCifrada),
                    new XElement("DatosCifrados", datosCifrados));

                return ResultadoOperacion<string>.Ok(envoltorio.ToString());
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion<string>.Error(ex.Message);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<string>.Error("No se pudo generar la exportación en este momento.");
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
                string xmlOriginal;
                int version;
                if (int.TryParse((string)envoltorio.Attribute("versionClave"), out version))
                {
                    ClaveCifrado clave = _claves.ObtenerPorVersion(version);
                    if (clave == null)
                    {
                        return ResultadoOperacion<string>.Error("El archivo se cifró con la versión " + version + " de las claves, que no existe en esta base.");
                    }

                    xmlOriginal = EncriptadorHibrido.Desencriptar(iv, claveCifrada, datosCifrados, _claves.ObtenerClavePrivada(clave.IdClaveCifrado));
                }
                else if (EncriptadorAsimetrico.HayClavesConfiguradas())
                {

                    xmlOriginal = EncriptadorHibrido.Desencriptar(iv, claveCifrada, datosCifrados);
                }
                else
                {
                    return ResultadoOperacion<string>.Error(
                        "El archivo es de una versión anterior (sin versión de clave) y ya no están configuradas las claves con que se generó.");
                }

                return ResultadoOperacion<string>.Ok(xmlOriginal);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<string>.Error("No se pudo leer la clave en este momento. Probá de nuevo.");
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoOperacion<string>.Error(ex.Message);
            }
            catch (FormatException)
            {
                return ResultadoOperacion<string>.Error("El archivo está corrupto o no es una exportación válida.");
            }
            catch (Exception ex) when (ex is CryptographicException || ex is ArgumentException)
            {
                return ResultadoOperacion<string>.Error(
                    "No se pudo descifrar el archivo: está alterado o no corresponde a las claves de esta instalación.");
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
