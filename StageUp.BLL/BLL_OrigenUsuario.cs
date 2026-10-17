using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Orígenes de usuario (canales de captación). Se administran desde
    // Campañas comerciales (mismo permiso) y alimentan las métricas de
    // CU-004-004. El origen de cada usuario se registra al crear la cuenta:
    // por el enlace de difusión (?origen=codigo) o por lo que la persona
    // elige en "¿Cómo conociste StageUp?". Un contacto incorporado también
    // le asigna su origen a la cuenta asociada si no tenía uno.
    public class BLL_OrigenUsuario
    {
        public const string EntidadBitacora = "OrigenUsuario";
        private static readonly Regex PatronCodigo = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");

        private readonly MPP_OrigenUsuario _mpp = new MPP_OrigenUsuario();
        private readonly BLL_CatalogoComercial _catalogos = new BLL_CatalogoComercial();

        public ResultadoOperacion<List<OrigenUsuario>> Listar(bool soloActivos)
        {
            try
            {
                return ResultadoOperacion<List<OrigenUsuario>>.Ok(_mpp.Listar(new OrigenUsuario { Activo = soloActivos }));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<OrigenUsuario>>.Error("No se pudieron cargar los orígenes de usuario. Probá nuevamente en unos minutos.");
            }
        }

        public OrigenUsuario Obtener(int idOrigenUsuario)
        {
            try
            {
                return _mpp.Obtener(new OrigenUsuario { IdOrigenUsuario = idOrigenUsuario });
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // Origen activo con ese código (el de un enlace de difusión), o null.
        public OrigenUsuario ObtenerPorCodigo(string codigo)
        {
            string normalizado = (codigo ?? string.Empty).Trim().ToLowerInvariant();
            if (!EsCodigoValido(normalizado))
            {
                return null;
            }

            try
            {
                OrigenUsuario origen = _mpp.Obtener(new OrigenUsuario { Codigo = normalizado });
                return origen != null && origen.Activo ? origen : null;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // Opciones de "¿Cómo conociste StageUp?" en el registro.
        public List<OrigenUsuario> ListarParaRegistro()
        {
            ResultadoOperacion<List<OrigenUsuario>> resultado = Listar(true);
            return resultado.Exitoso
                ? resultado.Valor.Where(o => o.VisibleEnRegistro).ToList()
                : new List<OrigenUsuario>();
        }

        public static bool EsCodigoValido(string codigo)
        {
            return !string.IsNullOrEmpty(codigo) && codigo.Length >= 2 && codigo.Length <= 40 && PatronCodigo.IsMatch(codigo);
        }

        public ResultadoOperacion<int> Guardar(OrigenUsuario origen, int idUsuarioInternoResponsable)
        {
            if (origen == null)
            {
                return ResultadoOperacion<int>.Error("Completá los datos del origen.");
            }

            origen.NombreOrigen = BLL_CatalogoComercial.Limpiar(origen.NombreOrigen);
            origen.Descripcion = BLL_CatalogoComercial.LimpiarTextoLargo(origen.Descripcion);
            bool codigoAutomatico = string.IsNullOrWhiteSpace(origen.Codigo);
            origen.Codigo = codigoAutomatico
                ? BLL_CatalogoComercial.GenerarCodigo(origen.NombreOrigen)
                : origen.Codigo.Trim().ToLowerInvariant();

            List<string> faltantes = new List<string>();
            if (origen.NombreOrigen == null) faltantes.Add("nombre");
            if (string.IsNullOrEmpty(origen.CanalOrigen)) faltantes.Add("canal de origen");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion<int>.Error("Completá: " + string.Join(", ", faltantes) + ".");
            }

            if (origen.NombreOrigen.Length > 150)
            {
                return ResultadoOperacion<int>.Error("El nombre no puede superar los 150 caracteres.");
            }

            if (!_catalogos.EsValido(ItemCatalogoComercial.CanalOrigen, origen.CanalOrigen))
            {
                return ResultadoOperacion<int>.Error("Elegí un canal de origen válido.");
            }

            if (!EsCodigoValido(origen.Codigo))
            {
                return ResultadoOperacion<int>.Error(codigoAutomatico
                    ? "Escribí un código para el enlace: con el nombre no alcanza para armarlo (letras minúsculas sin tildes, números y guiones, entre 2 y 40 caracteres)."
                    : "El código del enlace solo puede tener letras minúsculas sin tildes, números y guiones (entre 2 y 40 caracteres).");
            }

            if (origen.Descripcion != null && origen.Descripcion.Length > 1000)
            {
                return ResultadoOperacion<int>.Error("La descripción no puede superar los 1000 caracteres.");
            }

            if (origen.CanalOrigen == OrigenUsuario.CanalCampania && !origen.IdCampaniaComercial.HasValue)
            {
                return ResultadoOperacion<int>.Error("Un origen de tipo campaña comercial tiene que indicar la campaña.");
            }

            if (origen.CanalOrigen == OrigenUsuario.CanalAlianza && !origen.IdAlianzaEstrategica.HasValue)
            {
                return ResultadoOperacion<int>.Error("Un origen de tipo alianza estratégica tiene que indicar la alianza.");
            }

            bool esAlta = origen.IdOrigenUsuario <= 0;
            int id;
            try
            {
                id = _mpp.Guardar(origen);
            }
            catch (ErrorAccesoDatosException ex)
            {
                if (ex.NumeroErrorSql == 51300 || ex.NumeroErrorSql == 2601 || ex.NumeroErrorSql == 2627)
                {
                    return ResultadoOperacion<int>.Error("El código \"" + origen.Codigo + "\" ya lo usa otro origen. Elegí otro.");
                }

                if (ex.NumeroErrorSql == 51301)
                {
                    return ResultadoOperacion<int>.Error("La campaña o la alianza elegida ya no está disponible.");
                }

                return ResultadoOperacion<int>.Error("No fue posible guardar el origen en este momento. Probá nuevamente.");
            }

            BLL_CatalogoComercial.Registrar(idUsuarioInternoResponsable, esAlta ? "ALTA" : "MODIFICACION", EntidadBitacora, id,
                (esAlta ? "Alta" : "Modificación") + " del origen de usuario \"" + origen.NombreOrigen + "\" (código " + origen.Codigo + ").");

            return ResultadoOperacion<int>.Ok(id, esAlta ? "El origen se registró correctamente." : "El origen se actualizó correctamente.");
        }

        public ResultadoOperacion CambiarActivo(int idOrigenUsuario, bool activo, int idUsuarioInternoResponsable)
        {
            OrigenUsuario origen = Obtener(idOrigenUsuario);
            if (origen == null)
            {
                return ResultadoOperacion.Error("El origen ya no existe.");
            }

            try
            {
                _mpp.CambiarActivo(new OrigenUsuario { IdOrigenUsuario = idOrigenUsuario, Activo = activo });
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No fue posible actualizar el origen en este momento.");
            }

            BLL_CatalogoComercial.Registrar(idUsuarioInternoResponsable, activo ? "ACTIVACION" : "BAJA", EntidadBitacora, idOrigenUsuario,
                (activo ? "Reactivación" : "Baja lógica") + " del origen de usuario \"" + origen.NombreOrigen + "\".");

            return ResultadoOperacion.Ok(activo
                ? "El origen volvió a estar disponible."
                : "El origen se dio de baja: deja de ofrecerse en el registro y en los formularios, y los usuarios que llegaron por él se siguen contando.");
        }

        // Al crear la cuenta: primero el enlace de difusión; si no hay, lo que
        // eligió la persona. No pisa un origen ya registrado.
        public void AsignarAlRegistrarse(int idUsuarioExterno, string codigoEnlace, int? idOrigenElegido)
        {
            OrigenUsuario origen = ObtenerPorCodigo(codigoEnlace);
            if (origen == null && idOrigenElegido.HasValue)
            {
                origen = ListarParaRegistro().FirstOrDefault(o => o.IdOrigenUsuario == idOrigenElegido.Value);
            }

            if (origen == null)
            {
                return;
            }

            try
            {
                _mpp.AsignarAUsuario(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno }, origen, true);
            }
            catch (ErrorAccesoDatosException)
            {
                // El registro de la cuenta no depende del origen.
            }
        }
    }
}
