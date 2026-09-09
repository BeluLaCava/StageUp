using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_RolInterno
    {
        private readonly MPP_RolInterno _mppRol = new MPP_RolInterno();
        private readonly MPP_UsuarioInterno _mppUsuarioInterno = new MPP_UsuarioInterno();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<RolInterno> Listar()
        {
            try
            {
                return _mppRol.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<RolInterno>();
            }
        }

        public RolInterno ObtenerPorId(int idRolInterno)
        {
            try
            {
                return _mppRol.ObtenerPorId(idRolInterno);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion<int> Registrar(string nombreRol, string descripcion, int? idAreaInterna, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(nombreRol))
                {
                    return ResultadoOperacion<int>.Error("Ingresá el nombre del rol.");
                }

                if (nombreRol.Trim().Length > 200)
                {
                    return ResultadoOperacion<int>.Error("El nombre del rol no puede superar los 200 caracteres.");
                }

                if (!string.IsNullOrEmpty(descripcion) && descripcion.Length > 510)
                {
                    return ResultadoOperacion<int>.Error("La descripción no puede superar los 510 caracteres.");
                }

                int idRolInterno = _mppRol.Insertar(new RolInterno
                {
                    NombreRol = nombreRol.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                    IdAreaInterna = idAreaInterna,
                    EstadoRol = "Activo"
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "ALTA", "RolInterno", idRolInterno,
                    "Alta de rol interno: " + nombreRol.Trim() + ".");

                return ResultadoOperacion<int>.Ok(idRolInterno, "El rol se creó correctamente.");
            });
        }

        public ResultadoOperacion Modificar(int idRolInterno, string nombreRol, string descripcion, int? idAreaInterna, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                if (string.IsNullOrWhiteSpace(nombreRol))
                {
                    return ResultadoOperacion.Error("Ingresá el nombre del rol.");
                }

                if (nombreRol.Trim().Length > 200)
                {
                    return ResultadoOperacion.Error("El nombre del rol no puede superar los 200 caracteres.");
                }

                if (!string.IsNullOrEmpty(descripcion) && descripcion.Length > 510)
                {
                    return ResultadoOperacion.Error("La descripción no puede superar los 510 caracteres.");
                }

                _mppRol.Modificar(new RolInterno
                {
                    IdRolInterno = idRolInterno,
                    NombreRol = nombreRol.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                    IdAreaInterna = idAreaInterna
                });

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", "RolInterno", idRolInterno,
                    "Modificación de rol interno: " + nombreRol.Trim() + ".");

                return ResultadoOperacion.Ok("El rol se actualizó correctamente.");
            });
        }

        public ResultadoOperacion DarDeBaja(int idRolInterno, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                int cantidadUsuarios = _mppUsuarioInterno.ContarActivosPorRol(idRolInterno);
                if (cantidadUsuarios > 0)
                {
                    return ResultadoOperacion.Error(
                        "No se puede dar de baja este rol porque tiene " + cantidadUsuarios +
                        " usuario(s) interno(s) asignado(s). Reasigná esos usuarios a otro rol primero.");
                }

                _mppRol.Baja(idRolInterno);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", "RolInterno", idRolInterno,
                    "Baja de rol interno.");

                return ResultadoOperacion.Ok("El rol se dio de baja.");
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
