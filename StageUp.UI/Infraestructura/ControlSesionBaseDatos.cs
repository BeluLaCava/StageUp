using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Infraestructura
{



















    public static class ControlSesionBaseDatos
    {
        public const string MotivoCierre = "base_restaurada";
        public const string PaginaVerificacion = "~/SesionNoVerificada.aspx";



        public static ResultadoRevalidacionSesion VerificarSinExcepciones()
        {
            try
            {
                return Verificar();
            }
            catch (Exception)
            {
                GestorDeSesion.SuspenderSesionEnEstePedido();
                return ResultadoRevalidacionSesion.NoVerificada;
            }
        }

        public static ResultadoRevalidacionSesion Verificar()
        {
            int? idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual();
            int? idUsuarioInterno = GestorDeSesion.ObtenerIdUsuarioInternoActual();
            if (!idUsuarioExterno.HasValue && !idUsuarioInterno.HasValue)
            {
                return ResultadoRevalidacionSesion.Valida;
            }

            BLL_SesionBaseDatos bll = new BLL_SesionBaseDatos();
            bool marcaConfiable;
            string marcaActual = bll.ObtenerMarcaActual(out marcaConfiable);
            string marcaSesion = GestorDeSesion.ObtenerMarcaBaseDatos();
            if (marcaConfiable && marcaActual == marcaSesion)
            {
                return ResultadoRevalidacionSesion.Valida;
            }




            bool huboRestauracion = marcaConfiable && marcaSesion != null;

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

            if (usuarioInterno != null && !GestorDeSesion.ActualizarSesionInterna(usuarioInterno, permisos))
            {
                return Cerrar(ResultadoRevalidacionSesion.Invalida);
            }

            if (!marcaConfiable)
            {


                return ResultadoRevalidacionSesion.Valida;
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


            if (resultado == ResultadoRevalidacionSesion.Invalida)
            {
                GestorDeSesion.CerrarSesion();
                GestorDeSesion.CerrarSesionInterna();
            }
            else
            {
                GestorDeSesion.SuspenderSesionEnEstePedido();
            }

            return resultado;
        }
    }
}
