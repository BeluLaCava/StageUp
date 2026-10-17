using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Infraestructura
{
    // Sesión abierta después de restaurar un backup (script 79). Global.asax
    // lo llama en cada pedido que usa la sesión, y BackupRestore.aspx justo
    // después de restaurar.
    //   * Si la marca de la base no cambió, no hace nada (la marca se lee
    //     de memoria; la base se consulta como mucho cada 10 segundos).
    //   * Si cambió, vuelve a validar la cuenta externa y la interna de la
    //     sesión. Si alguna ya no corresponde, cierra la sesión. Si las dos
    //     siguen siendo válidas, actualiza nombre, perfil y permisos con los
    //     datos de la base restaurada y deja un aviso para mostrar una vez.
    public static class ControlSesionBaseDatos
    {
        public const string MotivoCierre = "base_restaurada";

        public static ResultadoRevalidacionSesion Verificar()
        {
            int? idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual();
            int? idUsuarioInterno = GestorDeSesion.ObtenerIdUsuarioInternoActual();
            if (!idUsuarioExterno.HasValue && !idUsuarioInterno.HasValue)
            {
                return ResultadoRevalidacionSesion.Valida;
            }

            BLL_SesionBaseDatos bll = new BLL_SesionBaseDatos();
            string marcaActual = bll.ObtenerMarcaActual();
            string marcaSesion = GestorDeSesion.ObtenerMarcaBaseDatos();
            if (marcaActual == null || marcaActual == marcaSesion)
            {
                return ResultadoRevalidacionSesion.Valida;
            }

            // Sin marca guardada es una sesión recién iniciada: se valida
            // igual, pero no se muestra el aviso de restauración.
            bool huboRestauracion = marcaSesion != null;

            UsuarioExterno usuarioExterno = null;
            if (idUsuarioExterno.HasValue)
            {
                ResultadoRevalidacionSesion resultado = bll.RevalidarUsuarioExterno(
                    idUsuarioExterno.Value, GestorDeSesion.ObtenerFechaAltaCuentaActual(), out usuarioExterno);
                if (resultado != ResultadoRevalidacionSesion.Valida)
                {
                    return Cerrar(resultado);
                }
            }

            UsuarioInterno usuarioInterno = null;
            List<string> permisos = null;
            if (idUsuarioInterno.HasValue)
            {
                ResultadoRevalidacionSesion resultado = bll.RevalidarUsuarioInterno(
                    idUsuarioInterno.Value, GestorDeSesion.ObtenerFechaAltaCuentaInternaActual(), out usuarioInterno, out permisos);
                if (resultado != ResultadoRevalidacionSesion.Valida)
                {
                    return Cerrar(resultado);
                }
            }

            if (usuarioExterno != null)
            {
                GestorDeSesion.ActualizarNombreCompletoEnSesion(usuarioExterno.Nombre, usuarioExterno.Apellido);
                GestorDeSesion.ActualizarPerfilEnSesion(usuarioExterno.PerfilUsuario);
                GestorDeSesion.MarcarCuentaVerificada();
            }

            if (usuarioInterno != null)
            {
                GestorDeSesion.IniciarSesionInterna(usuarioInterno, permisos);
            }

            GestorDeSesion.EstablecerMarcaBaseDatos(marcaActual);
            if (huboRestauracion)
            {
                GestorDeSesion.MarcarAvisoRestauracion();
            }

            return ResultadoRevalidacionSesion.Valida;
        }

        private static ResultadoRevalidacionSesion Cerrar(ResultadoRevalidacionSesion resultado)
        {
            // Si no se pudo consultar la base, la sesión sigue como estaba y
            // se vuelve a intentar en el próximo pedido (la marca no se guarda).
            if (resultado == ResultadoRevalidacionSesion.Invalida)
            {
                GestorDeSesion.CerrarSesion();
                GestorDeSesion.CerrarSesionInterna();
            }

            return resultado;
        }
    }
}
