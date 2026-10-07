using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-001-012 Gestionar usuarios internos y permisos: roles internos (A11 a
    // A15). El rol se guarda junto con sus permisos en una sola operación:
    // nombre obligatorio y sin repetir entre los roles activos (A14), al menos
    // un permiso (A13), permisos existentes y activos que den algún acceso
    // real (A15) y sin dejar el sistema sin administradores activos.
    public class BLL_RolInterno
    {
        public const string FiltroActivos = "Activos";
        public const string FiltroInactivos = "Inactivos";
        public const string FiltroTodos = "Todos";

        public const int LongitudMaximaNombre = 200;
        public const int LongitudMaximaDescripcion = 510;

        private const string TipoEntidadBitacora = "RolInterno";

        private readonly MPP_RolInterno _mppRol = new MPP_RolInterno();
        private readonly MPP_UsuarioInterno _mppUsuarioInterno = new MPP_UsuarioInterno();
        private readonly MPP_AreaInterna _mppArea = new MPP_AreaInterna();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();
        private readonly BLL_ControlAdministradores _control = new BLL_ControlAdministradores();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // Roles activos (para asignar a un usuario interno).
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

        // A11 pasos 3 y 4: listado con área, estado y cantidades.
        public List<RolInterno> ListarResumen(string estado)
        {
            try
            {
                string filtro = estado == FiltroInactivos || estado == FiltroTodos ? estado : FiltroActivos;
                return _mppRol.ListarResumen(filtro);
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
                return _mppRol.ObtenerPorId(new RolInterno { IdRolInterno = idRolInterno });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // A12 paso 2: detalle del rol (datos, lo tildado, los permisos que
        // resultan y los usuarios internos que lo tienen).
        public DetalleRolInterno ObtenerDetalle(int idRolInterno)
        {
            try
            {
                RolInterno rol = _mppRol.ObtenerPorId(new RolInterno { IdRolInterno = idRolInterno });
                if (rol == null)
                {
                    return null;
                }

                GrupoPermisos arbol = _bllPermiso.ListarArbolCompleto();
                List<int> ids = _bllPermiso.ListarIdsAsignados(idRolInterno);

                List<PermisoComponente> componentes = new List<PermisoComponente>();
                foreach (int id in ids)
                {
                    PermisoComponente componente = BLL_PermisoInterno.BuscarEnArbol(arbol, id);
                    if (componente != null)
                    {
                        componentes.Add(componente);
                    }
                }

                return new DetalleRolInterno
                {
                    Rol = rol,
                    ComponentesAsignados = componentes.OrderBy(c => c is GrupoPermisos ? 0 : 1).ThenBy(c => c.Nombre).ToList(),
                    PermisosEfectivos = BLL_PermisoInterno.HojasDeSeleccion(arbol, ids),
                    Usuarios = _mppUsuarioInterno.Listar().Where(u => u.IdRolInterno == idRolInterno).ToList()
                };
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        // A11 pasos 9 a 13 (alta) y A12 pasos 6 a 10 (modificación)
        // ------------------------------------------------------------------
        public ResultadoOperacion<int> Guardar(RolInterno datos, List<int> idsComponentes, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                if (datos == null)
                {
                    return ResultadoOperacion<int>.Error("Completá los datos del rol.", "A11");
                }

                bool esNuevo = datos.IdRolInterno == 0;
                string codigoDatos = esNuevo ? "A11" : "A12";

                RolInterno anterior = null;
                List<int> idsAnteriores = new List<int>();
                if (!esNuevo)
                {
                    anterior = _mppRol.ObtenerPorId(new RolInterno { IdRolInterno = datos.IdRolInterno });
                    if (anterior == null || !anterior.Activo)
                    {
                        return ResultadoOperacion<int>.Error("El rol seleccionado no existe o está dado de baja: no se puede modificar.", "A12");
                    }

                    idsAnteriores = _bllPermiso.ListarIdsAsignados(anterior.IdRolInterno);
                }

                RolInterno rol = new RolInterno
                {
                    IdRolInterno = datos.IdRolInterno,
                    NombreRol = (datos.NombreRol ?? string.Empty).Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : datos.Descripcion.Trim(),
                    IdAreaInterna = datos.IdAreaInterna.HasValue && datos.IdAreaInterna.Value > 0 ? datos.IdAreaInterna : null
                };

                List<int> ids = (idsComponentes ?? new List<int>()).Where(id => id > 0).Distinct().ToList();

                // Datos obligatorios del rol.
                if (rol.NombreRol.Length == 0)
                {
                    return ResultadoOperacion<int>.Error(
                        ids.Count == 0
                            ? "Completá el nombre del rol y elegí al menos un permiso."
                            : "Completá el nombre del rol.",
                        codigoDatos);
                }

                if (rol.NombreRol.Length > LongitudMaximaNombre)
                {
                    return ResultadoOperacion<int>.Error("El nombre del rol puede tener hasta " + LongitudMaximaNombre + " caracteres.", codigoDatos);
                }

                if (rol.Descripcion != null && rol.Descripcion.Length > LongitudMaximaDescripcion)
                {
                    return ResultadoOperacion<int>.Error("La descripción puede tener hasta " + LongitudMaximaDescripcion + " caracteres.", codigoDatos);
                }

                if (rol.IdAreaInterna.HasValue)
                {
                    AreaInterna area = _mppArea.ObtenerPorId(new AreaInterna { IdAreaInterna = rol.IdAreaInterna.Value });
                    if (area == null || !area.Activo)
                    {
                        return ResultadoOperacion<int>.Error("El área interna seleccionada no existe o no está activa. Elegí otra.", codigoDatos);
                    }

                    rol.NombreArea = area.NombreArea;
                }

                // A14: nombre repetido entre los roles activos.
                if (_mppRol.ExisteNombreActivo(rol))
                {
                    return ResultadoOperacion<int>.Error(
                        "Ya existe un rol activo llamado \"" + rol.NombreRol + "\". Elegí otro nombre para este rol.", "A14");
                }

                // A13: al menos un permiso.
                if (ids.Count == 0)
                {
                    return ResultadoOperacion<int>.Error(
                        "El rol debe tener al menos un permiso asignado. Tildá los permisos o grupos que va a tener.", "A13");
                }

                // A15: cada permiso tiene que existir y estar activo, y la
                // selección tiene que dar al menos un acceso real.
                GrupoPermisos arbol = _bllPermiso.ListarArbolCompleto();
                if (ids.Any(id => BLL_PermisoInterno.BuscarEnArbol(arbol, id) == null))
                {
                    return ResultadoOperacion<int>.Error(
                        "La configuración de permisos no es válida: alguno de los permisos elegidos ya no existe o fue desactivado. Revisá la selección.",
                        esNuevo ? "A13" : "A15");
                }

                List<PermisoHoja> hojas = BLL_PermisoInterno.HojasDeSeleccion(arbol, ids);
                if (hojas.Count == 0)
                {
                    return ResultadoOperacion<int>.Error(
                        "La configuración de permisos no es válida: los grupos elegidos todavía no tienen permisos adentro. Tildá al menos un permiso.",
                        esNuevo ? "A13" : "A15");
                }

                // No dejar el sistema sin administradores activos.
                if (!esNuevo && !BLL_ControlAdministradores.EsRolAdministrador(hojas.Select(h => h.CodigoPermiso)) &&
                    _control.CambioDejaSinAdministradores(new BLL_ControlAdministradores.Cambio
                    {
                        IdRol = rol.IdRolInterno,
                        NuevosComponentesRol = ids
                    }))
                {
                    return ResultadoOperacion<int>.Error(
                        "No se puede guardar esta configuración de permisos: " + BLL_ControlAdministradores.MensajeSinAdministradores, "A15");
                }

                int idRolInterno = _mppRol.GuardarConPermisos(rol, ids);
                BLL_PermisoInterno.InvalidarCacheRol(idRolInterno);

                RegistrarEnBitacora(esNuevo, idRolInterno, rol, anterior, ids, idsAnteriores, arbol, idUsuarioInternoResponsable);

                if (esNuevo)
                {
                    return ResultadoOperacion<int>.Ok(idRolInterno, "El rol \"" + rol.NombreRol + "\" fue registrado correctamente.");
                }

                int usuarios = _mppUsuarioInterno.ContarActivosPorRol(new RolInterno { IdRolInterno = idRolInterno });
                string mensaje = "El rol y sus permisos fueron actualizados correctamente.";
                if (usuarios > 0)
                {
                    mensaje += usuarios == 1
                        ? " El usuario interno que tiene este rol ya trabaja con los permisos actualizados."
                        : " Los " + usuarios + " usuarios internos que tienen este rol ya trabajan con los permisos actualizados.";
                }

                return ResultadoOperacion<int>.Ok(idRolInterno, mensaje);
            });
        }

        public ResultadoOperacion DarDeBaja(int idRolInterno, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                RolInterno rol = _mppRol.ObtenerPorId(new RolInterno { IdRolInterno = idRolInterno });
                if (rol == null)
                {
                    return ResultadoOperacion.Error("No se encontró el rol seleccionado.");
                }

                if (!rol.Activo)
                {
                    return ResultadoOperacion.Error("El rol ya estaba dado de baja.");
                }

                int cantidadUsuarios = _mppUsuarioInterno.ContarActivosPorRol(rol);
                if (cantidadUsuarios > 0)
                {
                    return ResultadoOperacion.Error(
                        "No se puede dar de baja este rol porque tiene " + cantidadUsuarios +
                        (cantidadUsuarios == 1 ? " usuario interno activo asignado" : " usuarios internos activos asignados") +
                        ". Asigná otro rol a esos usuarios primero.");
                }

                _mppRol.Baja(rol);
                BLL_PermisoInterno.InvalidarCacheRol(idRolInterno);

                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "BAJA", TipoEntidadBitacora, idRolInterno,
                    "Baja lógica del rol interno \"" + rol.NombreRol + "\".");

                return ResultadoOperacion.Ok("El rol \"" + rol.NombreRol + "\" se dio de baja. Se conserva su historial.");
            });
        }

        // ------------------------------------------------------------------
        // Bitácora: quién, qué y qué cambió (datos y permisos).
        // ------------------------------------------------------------------
        private void RegistrarEnBitacora(
            bool esNuevo, int idRolInterno, RolInterno rol, RolInterno anterior,
            List<int> ids, List<int> idsAnteriores, GrupoPermisos arbol, int idResponsable)
        {
            string area = rol.NombreArea ?? "sin área específica";

            if (esNuevo)
            {
                _bitacora.RegistrarInterno(
                    idResponsable, "ALTA", TipoEntidadBitacora, idRolInterno,
                    Recortar("Alta del rol interno \"" + rol.NombreRol + "\" (" + area + ") con permisos: " +
                             NombresComponentes(arbol, ids) + "."));
                return;
            }

            List<string> cambios = new List<string>();
            if (!string.Equals(anterior.NombreRol, rol.NombreRol, StringComparison.Ordinal))
            {
                cambios.Add("nombre \"" + anterior.NombreRol + "\" → \"" + rol.NombreRol + "\"");
            }

            if (anterior.IdAreaInterna != rol.IdAreaInterna)
            {
                cambios.Add("área " + (anterior.NombreArea ?? "sin área específica") + " → " + area);
            }

            if (!string.Equals(anterior.Descripcion ?? string.Empty, rol.Descripcion ?? string.Empty, StringComparison.Ordinal))
            {
                cambios.Add("descripción actualizada");
            }

            if (cambios.Count > 0)
            {
                _bitacora.RegistrarInterno(
                    idResponsable, "MODIFICACION", TipoEntidadBitacora, idRolInterno,
                    Recortar("Modificación del rol interno \"" + rol.NombreRol + "\": " + string.Join("; ", cambios) + "."));
            }

            List<int> agregados = ids.Except(idsAnteriores).ToList();
            List<int> quitados = idsAnteriores.Except(ids).ToList();
            if (agregados.Count > 0 || quitados.Count > 0)
            {
                List<string> partes = new List<string>();
                if (agregados.Count > 0)
                {
                    partes.Add("se agregaron " + NombresComponentes(arbol, agregados));
                }

                if (quitados.Count > 0)
                {
                    partes.Add("se quitaron " + NombresComponentes(arbol, quitados));
                }

                _bitacora.RegistrarInterno(
                    idResponsable, "ASIGNACION_PERMISOS", TipoEntidadBitacora, idRolInterno,
                    Recortar("Permisos del rol \"" + rol.NombreRol + "\": " + string.Join("; ", partes) + "."));
            }
        }

        private static string NombresComponentes(GrupoPermisos arbol, IEnumerable<int> ids)
        {
            List<string> nombres = new List<string>();
            foreach (int id in ids)
            {
                PermisoComponente componente = BLL_PermisoInterno.BuscarEnArbol(arbol, id);
                if (componente == null)
                {
                    nombres.Add("#" + id);
                }
                else
                {
                    nombres.Add(componente is GrupoPermisos ? "grupo " + componente.Nombre : componente.Nombre);
                }
            }

            return string.Join(", ", nombres);
        }

        private static string Recortar(string texto)
        {
            return texto.Length <= 1900 ? texto : texto.Substring(0, 1897) + "...";
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
