using System;
using System.Security.Cryptography;
using System.Text;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Seguridad;
namespace StageUp.BLL
{
    internal static class ControlIntentosAcceso
    {
        private const string MensajeBloqueo = "Se alcanzó el límite de intentos de acceso. Esperá 15 minutos desde el bloqueo antes de volver a intentar.";
        internal static T Ejecutar<T>(string correo, Func<T> autenticar, Func<string, T> error) where T : ResultadoOperacion
        {
            string cuenta;
            using (SHA256 sha = SHA256.Create())
                cuenta = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes((correo ?? "").Trim().ToLowerInvariant()))).Replace("-", "");
            try
            {
                return Conexion.Instance.EjecutarAtomico("StageUp:Acceso:" + cuenta, () =>
                {
                    MPP_IntentoAcceso mpp = new MPP_IntentoAcceso();
                    if (mpp.Consultar(cuenta) > 0) return error(MensajeBloqueo);
                    T resultado = autenticar();
                    bool credencialesValidas = resultado.Exitoso || resultado.CodigoAlternativo == "ACTIVACION_PENDIENTE";
                    int segundos = mpp.Registrar(cuenta, credencialesValidas);
                    if (!credencialesValidas)
                    {

                        new BLL_Bitacora().Registrar(null, segundos > 0 ? "BLOQUEO" : "ERROR", "Sistema", null,
                            segundos > 0 ? "Acceso bloqueado durante 15 minutos tras 5 intentos fallidos consecutivos. Referencia: " + cuenta
                                         : "Intento de autenticación no satisfactorio. Referencia: " + cuenta, "StageUp.BLL");
                    }
                    return segundos > 0 ? error(MensajeBloqueo) : resultado;
                }, r => true);
            }
            catch (ErrorAccesoDatosException)
            {

                GestorDeSesion.CerrarSesion();
                GestorDeSesion.CerrarSesionInterna();
                return error("No pudimos verificar el acceso en este momento. Intentá nuevamente en unos minutos.");
            }
        }
    }
}
