using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-001-007 Gestionar espacios artísticos, caminos A1 a A6: solicitud de
    // habilitación como gestor desde "Ofrecer espacio" (OfrecerEspacio.aspx) y
    // su revisión desde el panel interno (Interno/AprobacionGestores.aspx).
    public class BLL_SolicitudHabilitacionGestor
    {
        public const string CondicionNoInformada = "NoInformado";

        // Condiciones fiscales que se pueden elegir en el formulario.
        public static readonly Dictionary<string, string> CondicionesFiscales = new Dictionary<string, string>
        {
            { "ConsumidorFinal", "Consumidor final" },
            { "Monotributista", "Monotributista" },
            { "ResponsableInscripto", "Responsable inscripto" },
            { "Exento", "Exento" }
        };

        private static readonly Regex PatronCorreo = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex PatronTelefono = new Regex(@"^[0-9+()\-\s]{6,30}$", RegexOptions.Compiled);

        private const int LongitudMinimaDescripcion = 30;
        private const int LongitudMinimaMotivo = 10;
        private const string TipoEntidadBitacora = "SolicitudHabilitacionGestor";

        private readonly MPP_SolicitudHabilitacionGestor _mpp = new MPP_SolicitudHabilitacionGestor();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_Notificacion _notificacion = new BLL_Notificacion();

        public static string NombreEstado(string estado)
        {
            switch (estado)
            {
                case SolicitudHabilitacionGestor.EstadoPendienteRevision: return "Pendiente de revisión";
                case SolicitudHabilitacionGestor.EstadoAprobada: return "Aprobada";
                case SolicitudHabilitacionGestor.EstadoRechazada: return "Rechazada";
                default: return estado;
            }
        }

        public static string NombreCondicionFiscal(string condicion)
        {
            string nombre;
            if (condicion == CondicionNoInformada)
            {
                return "No informada";
            }

            return CondicionesFiscales.TryGetValue(condicion ?? string.Empty, out nombre) ? nombre : condicion;
        }

        // A3, A4 y A6: última solicitud del usuario (null si nunca pidió).
        public SolicitudHabilitacionGestor ObtenerUltima(int idUsuarioExterno)
        {
            try
            {
                return _mpp.ObtenerUltimaPorUsuario(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public List<SolicitudHabilitacionGestor> Listar(string estado)
        {
            try
            {
                return _mpp.Listar(new SolicitudHabilitacionGestor { Estado = string.IsNullOrWhiteSpace(estado) ? null : estado });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<SolicitudHabilitacionGestor>();
            }
        }

        // A1 (pasos 6 a 10) y A5: valida todo el formulario y devuelve todos los
        // errores juntos, para que el usuario sepa qué corregir.
        public ResultadoOperacion<int> Registrar(int idUsuarioExterno, SolicitudHabilitacionGestor datos)
        {
            if (datos == null)
            {
                return ResultadoOperacion<int>.Error("Completá el formulario de habilitación.");
            }

            List<string> errores = new List<string>();
            SolicitudHabilitacionGestor solicitud = new SolicitudHabilitacionGestor { IdUsuarioExterno = idUsuarioExterno };

            solicitud.NombreResponsable = Obligatorio(datos.NombreResponsable, "el nombre y apellido del responsable", 150, errores);

            string documento = SoloDigitos(datos.DocumentoResponsable);
            if (string.IsNullOrEmpty(documento))
            {
                errores.Add("Ingresá el DNI o CUIT del responsable.");
            }
            else if (documento.Length < 7 || documento.Length > 11 || documento.Length == 9 || documento.Length == 10)
            {
                errores.Add("El documento del responsable tiene que ser un DNI (7 u 8 números) o un CUIT (11 números).");
            }

            solicitud.DocumentoResponsable = documento;

            string telefono = Limpiar(datos.TelefonoContacto);
            if (telefono == null)
            {
                errores.Add("Ingresá un teléfono de contacto.");
            }
            else if (!PatronTelefono.IsMatch(telefono))
            {
                errores.Add("El teléfono de contacto no es válido. Usá solo números, espacios y los símbolos + - ( ).");
            }

            solicitud.TelefonoContacto = telefono;

            string correo = Limpiar(datos.CorreoContacto);
            if (correo == null)
            {
                errores.Add("Ingresá un correo electrónico de contacto.");
            }
            else if (correo.Length > 300 || !PatronCorreo.IsMatch(correo))
            {
                errores.Add("El correo electrónico de contacto no tiene un formato válido.");
            }

            solicitud.CorreoContacto = correo == null ? null : correo.ToLowerInvariant();

            string condicion = Limpiar(datos.CondicionFiscal);
            if (condicion == null || !CondicionesFiscales.ContainsKey(condicion))
            {
                errores.Add("Elegí tu condición fiscal.");
            }

            solicitud.CondicionFiscal = condicion;

            solicitud.RazonSocial = Limpiar(datos.RazonSocial);
            if (solicitud.RazonSocial != null && solicitud.RazonSocial.Length > 150)
            {
                errores.Add("La razón social no puede superar los 150 caracteres.");
            }

            string cuit = SoloDigitos(datos.Cuit);
            bool requiereCuit = condicion == "Monotributista" || condicion == "ResponsableInscripto" || condicion == "Exento";
            if (string.IsNullOrEmpty(cuit))
            {
                if (requiereCuit)
                {
                    errores.Add("Con la condición fiscal elegida tenés que ingresar el CUIT.");
                }

                solicitud.Cuit = null;
            }
            else if (!EsCuitValido(cuit))
            {
                errores.Add("El CUIT no es válido: tiene que tener 11 números y un dígito verificador correcto.");
            }
            else
            {
                solicitud.Cuit = cuit.Substring(0, 2) + "-" + cuit.Substring(2, 8) + "-" + cuit.Substring(10, 1);
            }

            solicitud.NombreEspacio = Obligatorio(datos.NombreEspacio, "el nombre del espacio o entidad", 150, errores);
            solicitud.TipoEspacio = Obligatorio(datos.TipoEspacio, "el tipo de espacio", 100, errores);
            solicitud.Provincia = Obligatorio(datos.Provincia, "la provincia", 100, errores);
            solicitud.Ciudad = Obligatorio(datos.Ciudad, "la ciudad", 150, errores);

            string descripcion = Limpiar(datos.DescripcionPropuesta);
            if (descripcion == null || descripcion.Length < LongitudMinimaDescripcion)
            {
                errores.Add("Contanos un poco más sobre el espacio o la entidad que querés ofrecer (al menos " + LongitudMinimaDescripcion + " caracteres).");
            }
            else if (descripcion.Length > 1000)
            {
                errores.Add("La descripción no puede superar los 1000 caracteres.");
            }

            solicitud.DescripcionPropuesta = descripcion;

            if (errores.Count > 0)
            {
                return ResultadoOperacion<int>.Error("Revisá la solicitud: " + string.Join(" ", errores));
            }

            try
            {
                string resultado = _mpp.Registrar(solicitud);
                switch (resultado)
                {
                    case "OK":
                        break;
                    case "YA_PENDIENTE":
                        return ResultadoOperacion<int>.Error("Ya tenés una solicitud pendiente de revisión. No podés enviar otra hasta que se resuelva.");
                    case "YA_ES_GESTOR":
                        return ResultadoOperacion<int>.Error("Tu cuenta ya está habilitada como gestor de espacios.");
                    case "CUENTA_NO_ACTIVA":
                        return ResultadoOperacion<int>.Error("Tu cuenta no está activa, por eso no podés enviar la solicitud.");
                    default:
                        return ResultadoOperacion<int>.Error("No se encontró la cuenta indicada.");
                }

                _bitacora.Registrar(
                    idUsuarioExterno, "ALTA", TipoEntidadBitacora, solicitud.IdSolicitud,
                    "Solicitud de habilitación como gestor para \"" + solicitud.NombreEspacio + "\" (" + solicitud.TipoEspacio +
                    ", " + solicitud.Ciudad + "). Queda pendiente de revisión.");

                return ResultadoOperacion<int>.Ok(solicitud.IdSolicitud,
                    "Enviamos tu solicitud. Queda pendiente de revisión: te avisamos cuando un administrador la resuelva. Mientras tanto podés seguir usando StageUp como siempre.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<int>.Error(ex.Message);
            }
        }

        public ResultadoOperacion Aprobar(int idSolicitud, int idUsuarioInternoRevisor)
        {
            SolicitudHabilitacionGestor solicitud = new SolicitudHabilitacionGestor
            {
                IdSolicitud = idSolicitud,
                IdUsuarioInternoRevisor = idUsuarioInternoRevisor
            };

            try
            {
                string resultado = _mpp.Resolver(solicitud, true);
                if (resultado != "OK")
                {
                    return ResultadoOperacion.Error(MensajeNoResuelta(resultado));
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoRevisor, "APROBACION", TipoEntidadBitacora, idSolicitud,
                    "Aprobación de la solicitud de habilitación como gestor del usuario externo N° " + solicitud.IdUsuarioExterno + ".");

                _notificacion.Notificar(
                    solicitud.IdUsuarioExterno, TipoNotificacion.HabilitacionGestorAprobada,
                    "Tu cuenta fue habilitada como gestor de espacios. ¡Ya podés publicar tus espacios artísticos!",
                    "~/MisEspacios.aspx");

                return ResultadoOperacion.Ok("La solicitud fue aprobada y la cuenta quedó habilitada como gestora de espacios.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        public ResultadoOperacion Rechazar(int idSolicitud, string motivo, int idUsuarioInternoRevisor)
        {
            string motivoLimpio = Limpiar(motivo);
            if (motivoLimpio == null || motivoLimpio.Length < LongitudMinimaMotivo)
            {
                return ResultadoOperacion.Error("Escribí el motivo del rechazo (al menos " + LongitudMinimaMotivo + " caracteres): el usuario lo va a ver.");
            }

            if (motivoLimpio.Length > 500)
            {
                return ResultadoOperacion.Error("El motivo no puede superar los 500 caracteres.");
            }

            SolicitudHabilitacionGestor solicitud = new SolicitudHabilitacionGestor
            {
                IdSolicitud = idSolicitud,
                IdUsuarioInternoRevisor = idUsuarioInternoRevisor,
                MotivoRechazo = motivoLimpio
            };

            try
            {
                string resultado = _mpp.Resolver(solicitud, false);
                if (resultado != "OK")
                {
                    return ResultadoOperacion.Error(MensajeNoResuelta(resultado));
                }

                _bitacora.RegistrarInterno(
                    idUsuarioInternoRevisor, "RECHAZO", TipoEntidadBitacora, idSolicitud,
                    "Rechazo de la solicitud de habilitación como gestor del usuario externo N° " + solicitud.IdUsuarioExterno +
                    ". Motivo: " + motivoLimpio);

                _notificacion.Notificar(
                    solicitud.IdUsuarioExterno, TipoNotificacion.HabilitacionGestorRechazada,
                    "Tu solicitud para ser gestor de espacios fue rechazada. Podés ver el motivo y volver a enviarla.",
                    "~/OfrecerEspacio.aspx");

                return ResultadoOperacion.Ok("La solicitud fue rechazada. El usuario va a ver el motivo.");
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }

        private static string MensajeNoResuelta(string resultado)
        {
            return resultado == "YA_RESUELTA"
                ? "Esta solicitud ya fue resuelta anteriormente. No se hicieron cambios."
                : "No se encontró la solicitud indicada.";
        }

        // CUIT: 11 números con dígito verificador módulo 11.
        public static bool EsCuitValido(string cuit)
        {
            string digitos = SoloDigitos(cuit);
            if (digitos == null || digitos.Length != 11)
            {
                return false;
            }

            int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int suma = 0;
            for (int i = 0; i < 10; i++)
            {
                suma += (digitos[i] - '0') * pesos[i];
            }

            int resto = 11 - (suma % 11);
            int verificador = resto == 11 ? 0 : resto == 10 ? 9 : resto;
            return verificador == digitos[10] - '0';
        }

        private static string Obligatorio(string valor, string nombreCampo, int maximo, List<string> errores)
        {
            string limpio = Limpiar(valor);
            if (limpio == null)
            {
                errores.Add("Ingresá " + nombreCampo + ".");
            }
            else if (limpio.Length > maximo)
            {
                errores.Add("El campo " + nombreCampo + " no puede superar los " + maximo + " caracteres.");
            }

            return limpio;
        }

        private static string Limpiar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static string SoloDigitos(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return new string(valor.Where(char.IsDigit).ToArray());
        }
    }
}
