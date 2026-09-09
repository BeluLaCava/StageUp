using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Seguridad;

namespace StageUp.BLL
{
    public class BLL_UsuarioInterno
    {
        private readonly MPP_UsuarioInterno _mppUsuarioInterno = new MPP_UsuarioInterno();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        public ResultadoOperacion<UsuarioInterno> IniciarSesion(string correoElectronico, string password)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password))
                {
                    return ResultadoOperacion<UsuarioInterno>.Error("Ingresá tu correo electrónico y tu contraseña.");
                }

                UsuarioInterno usuario = _mppUsuarioInterno.ObtenerPorCorreo(correoElectronico.Trim().ToLowerInvariant());

                if (usuario == null || !HashDeContrasenas.Verificar(password, usuario.PasswordHash))
                {
                    return ResultadoOperacion<UsuarioInterno>.Error("El correo electrónico o la contraseña son incorrectos.");
                }

                if (usuario.EstadoCuenta != EstadoCuentaInterno.Activa.ToString())
                {
                    return ResultadoOperacion<UsuarioInterno>.Error("Esta cuenta interna no se encuentra habilitada.");
                }

                List<string> codigosPermisos = _bllPermiso.ListarCodigosPermisosDeRol(usuario.IdRolInterno);
                GestorDeSesion.IniciarSesionInterna(usuario, codigosPermisos);

                _bitacora.RegistrarInterno(
                    usuario.IdUsuarioInterno, "LOGIN", "UsuarioInterno", usuario.IdUsuarioInterno,
                    "Inicio de sesión de usuario interno.");

                return ResultadoOperacion<UsuarioInterno>.Ok(usuario);
            });
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
