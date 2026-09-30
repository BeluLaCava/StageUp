using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // ABMC del menú dinámico (ítem 19 de la segunda entrega). Cada opción del
    // menú del panel interno es una entidad administrable: texto, descripción,
    // URL, módulo (agrupa opciones en el menú), orden, estado y el permiso que
    // hace falta para verla. BLL_PermisoInterno.ConstruirMenuParaRol arma el
    // menú desde acá.
    public class BLL_OpcionMenu
    {
        private const int LongitudMaximaTexto = 100;
        private const int LongitudMaximaDescripcion = 300;
        private const int LongitudMaximaUrl = 300;
        private const int LongitudMaximaModulo = 100;
        private const string TipoEntidadBitacora = "OpcionMenu";

        // La opción de esta misma pantalla no se puede dar de baja: si se
        // desactivara, nadie podría volver a entrar desde el menú a
        // reactivarla.
        private const string CodigoPermisoGestionMenu = "GESTIONAR_MENU";

        private readonly MPP_OpcionMenu _mpp = new MPP_OpcionMenu();
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<OpcionMenu> Listar()
        {
            try
            {
                return _mpp.Listar();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<OpcionMenu>();
            }
        }

        public OpcionMenu ObtenerPorId(int idOpcionMenu)
        {
            try
            {
                return _mpp.ObtenerPorId(new OpcionMenu { IdOpcionMenu = idOpcionMenu });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // Permisos (hojas del árbol) que se pueden exigir para ver una opción.
        public List<PermisoHoja> ListarPermisosDisponibles()
        {
            List<PermisoHoja> hojas = new List<PermisoHoja>();
            foreach (PermisoHoja hoja in _bllPermiso.ListarArbolCompleto().Listar())
            {
                hojas.Add(hoja);
            }
            return hojas.OrderBy(h => h.Nombre).ToList();
        }

        public ResultadoOperacion<int> Registrar(OpcionMenu opcion, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                ResultadoOperacion validacion = Validar(opcion);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje);
                }

                int idOpcion = _mpp.Insertar(opcion);
                BLL_PermisoInterno.InvalidarCacheMenu();

                _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "ALTA", TipoEntidadBitacora, idOpcion,
                    "Alta de la opción de menú \"" + opcion.Texto + "\" (" + opcion.Url + ").");

                return ResultadoOperacion<int>.Ok(idOpcion, "La opción se agregó al final del menú.");
            });
        }

        public ResultadoOperacion Modificar(OpcionMenu opcion, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                OpcionMenu actual = _mpp.ObtenerPorId(opcion);
                if (actual == null)
                {
                    return ResultadoOperacion.Error("No se encontró la opción de menú seleccionada.");
                }

                ResultadoOperacion validacion = Validar(opcion);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                if (!opcion.Activo && actual.CodigoPermiso == CodigoPermisoGestionMenu)
                {
                    return ResultadoOperacion.Error("La opción de Gestión del menú no se puede desactivar.");
                }

                _mpp.Modificar(opcion);
                BLL_PermisoInterno.InvalidarCacheMenu();

                _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, opcion.IdOpcionMenu,
                    "Modificación de la opción de menú \"" + opcion.Texto + "\".");

                return ResultadoOperacion.Ok("La opción de menú se actualizó correctamente.");
            });
        }

        public ResultadoOperacion CambiarEstado(int idOpcionMenu, bool activa, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                OpcionMenu opcion = _mpp.ObtenerPorId(new OpcionMenu { IdOpcionMenu = idOpcionMenu });
                if (opcion == null)
                {
                    return ResultadoOperacion.Error("No se encontró la opción de menú seleccionada.");
                }

                if (!activa && opcion.CodigoPermiso == CodigoPermisoGestionMenu)
                {
                    return ResultadoOperacion.Error("La opción de Gestión del menú no se puede dar de baja.");
                }

                opcion.Activo = activa;
                _mpp.CambiarEstado(opcion);
                BLL_PermisoInterno.InvalidarCacheMenu();

                _bitacora.RegistrarInterno(idUsuarioInternoResponsable, activa ? "ACTIVACION" : "BAJA", TipoEntidadBitacora, idOpcionMenu,
                    (activa ? "Reactivación" : "Baja lógica") + " de la opción de menú \"" + opcion.Texto + "\".");

                return ResultadoOperacion.Ok(activa
                    ? "La opción volvió a mostrarse en el menú."
                    : "La opción se dio de baja y ya no se muestra en el menú.");
            });
        }

        // Sube o baja una opción un lugar, intercambiando su orden con la
        // opción vecina.
        public ResultadoOperacion Mover(int idOpcionMenu, bool haciaArriba, int idUsuarioInternoResponsable)
        {
            return EjecutarProtegido(() =>
            {
                List<OpcionMenu> opciones = _mpp.Listar();
                int indice = opciones.FindIndex(o => o.IdOpcionMenu == idOpcionMenu);
                if (indice < 0)
                {
                    return ResultadoOperacion.Error("No se encontró la opción de menú seleccionada.");
                }

                int indiceVecino = haciaArriba ? indice - 1 : indice + 1;
                if (indiceVecino < 0 || indiceVecino >= opciones.Count)
                {
                    return ResultadoOperacion.Error(haciaArriba
                        ? "La opción ya está primera en el menú."
                        : "La opción ya está última en el menú.");
                }

                OpcionMenu opcion = opciones[indice];
                OpcionMenu vecina = opciones[indiceVecino];

                int ordenOpcion = opcion.Orden;
                int ordenVecina = vecina.Orden;
                if (ordenOpcion == ordenVecina)
                {
                    // Si dos opciones quedaron con el mismo orden, se separan.
                    ordenVecina = haciaArriba ? ordenOpcion - 1 : ordenOpcion + 1;
                }

                opcion.Orden = ordenVecina;
                vecina.Orden = ordenOpcion;
                _mpp.ActualizarOrden(opcion);
                _mpp.ActualizarOrden(vecina);
                BLL_PermisoInterno.InvalidarCacheMenu();

                _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, idOpcionMenu,
                    "Cambio de orden de la opción de menú \"" + opcion.Texto + "\".");

                return ResultadoOperacion.Ok("Se actualizó el orden del menú.");
            });
        }

        private ResultadoOperacion Validar(OpcionMenu opcion)
        {
            opcion.Texto = Normalizar(opcion.Texto);
            opcion.Descripcion = Normalizar(opcion.Descripcion);
            opcion.Url = Normalizar(opcion.Url);
            opcion.Modulo = Normalizar(opcion.Modulo);

            if (opcion.Texto == null)
            {
                return ResultadoOperacion.Error("Ingresá el texto que se va a ver en el menú.");
            }

            if (opcion.Texto.Length > LongitudMaximaTexto)
            {
                return ResultadoOperacion.Error("El texto no puede superar los " + LongitudMaximaTexto + " caracteres.");
            }

            if (opcion.Descripcion != null && opcion.Descripcion.Length > LongitudMaximaDescripcion)
            {
                return ResultadoOperacion.Error("La descripción no puede superar los " + LongitudMaximaDescripcion + " caracteres.");
            }

            if (opcion.Modulo == null)
            {
                return ResultadoOperacion.Error("Ingresá el módulo en el que se agrupa la opción.");
            }

            if (opcion.Modulo.Length > LongitudMaximaModulo)
            {
                return ResultadoOperacion.Error("El módulo no puede superar los " + LongitudMaximaModulo + " caracteres.");
            }

            // Solo páginas propias de StageUp: ruta relativa a la aplicación
            // ("~/..."), sin sitios externos ni scripts.
            if (opcion.Url == null || !opcion.Url.StartsWith("~/", StringComparison.Ordinal)
                || opcion.Url.Contains("://") || opcion.Url.Contains(" ")
                || opcion.Url.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) >= 0
                || opcion.Url.IndexOf(".aspx", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return ResultadoOperacion.Error("La URL tiene que ser una página de StageUp, por ejemplo ~/Interno/GestionFaq.aspx.");
            }

            if (opcion.Url.Length > LongitudMaximaUrl)
            {
                return ResultadoOperacion.Error("La URL no puede superar los " + LongitudMaximaUrl + " caracteres.");
            }

            bool permisoValido = ListarPermisosDisponibles().Any(h => h.IdComponentePermiso == opcion.IdComponentePermiso);
            if (!permisoValido)
            {
                return ResultadoOperacion.Error("Elegí el permiso que hace falta para ver la opción.");
            }

            return ResultadoOperacion.Ok();
        }

        private static string Normalizar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
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
