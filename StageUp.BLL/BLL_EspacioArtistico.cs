using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    /// <summary>
    /// Reglas de negocio del ABMC de EspacioArtistico (CU-001-007
    /// Gestionar espacios artísticos). Cubre las columnas propias de la
    /// entidad (nombre, descripción, tipo, estado, publicación, baja
    /// lógica); las entidades relacionadas que describe el CU completo
    /// (ubicación, características físicas, condiciones de uso, valores,
    /// equipamiento, imágenes) quedan para una etapa posterior.
    ///
    /// Supuesto (no implementado todavía, a validar con la cátedra/cliente):
    /// el CU real exige que el usuario esté "habilitado como gestor de
    /// espacios" mediante una solicitud revisada y aprobada por un usuario
    /// interno, antes de poder acceder a "Mis espacios". Esa revisión de
    /// habilitación aparece además como un requerimiento propio en el
    /// catálogo de RQF, separado de este ABMC. Mientras esa funcionalidad
    /// no se implemente, cualquier usuario externo autenticado puede
    /// administrar sus propios espacios.
    /// </summary>
    public class BLL_EspacioArtistico
    {
        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const int LongitudMaximaNombre = 300;
        private const int LongitudMaximaDescripcion = 2000;
        private const int LongitudMaximaTipoEspacio = 200;
        private const string TipoEntidadBitacora = "EspacioArtistico";

        public ResultadoOperacion<int> Registrar(int idUsuarioGestor, string nombreEspacio, string descripcion, string tipoEspacio)
        {
            ResultadoOperacion validacion = ValidarDatosBasicos(nombreEspacio, descripcion, tipoEspacio);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<int>.Error(validacion.Mensaje, validacion.CodigoAlternativo);
            }

            var espacio = new EspacioArtistico
            {
                IdUsuarioGestor = idUsuarioGestor,
                NombreEspacio = nombreEspacio.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                TipoEspacio = tipoEspacio.Trim()
            };

            int idEspacioArtistico = _mppEspacio.Insertar(espacio);

            _bitacora.Registrar(
                idUsuarioGestor, "ALTA", TipoEntidadBitacora, idEspacioArtistico,
                "Alta de espacio artístico \"" + espacio.NombreEspacio + "\" (borrador).");

            return ResultadoOperacion<int>.Ok(idEspacioArtistico, "El espacio se guardó como borrador. Podés publicarlo cuando quieras.");
        }

        public ResultadoOperacion Modificar(int idEspacioArtistico, int idUsuarioGestorSolicitante, string nombreEspacio, string descripcion, string tipoEspacio)
        {
            ResultadoOperacion validacion = ValidarDatosBasicos(nombreEspacio, descripcion, tipoEspacio);
            if (!validacion.Exitoso)
            {
                return validacion;
            }

            EspacioArtistico espacioExistente = _mppEspacio.ObtenerPorId(idEspacioArtistico);
            ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacioExistente, idUsuarioGestorSolicitante);
            if (!validacionPropiedad.Exitoso)
            {
                return validacionPropiedad;
            }

            var espacio = new EspacioArtistico
            {
                IdEspacioArtistico = idEspacioArtistico,
                NombreEspacio = nombreEspacio.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                TipoEspacio = tipoEspacio.Trim()
            };

            _mppEspacio.Modificar(espacio);

            _bitacora.Registrar(
                idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idEspacioArtistico,
                "Modificación de espacio artístico \"" + espacio.NombreEspacio + "\".");

            return ResultadoOperacion.Ok("Los cambios se guardaron correctamente.");
        }

        public List<EspacioArtistico> ListarMisEspacios(int idUsuarioGestor)
        {
            return _mppEspacio.ListarPorUsuarioGestor(idUsuarioGestor);
        }

        public ResultadoOperacion Publicar(int idEspacioArtistico, int idUsuarioGestorSolicitante)
        {
            EspacioArtistico espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
            ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
            if (!validacionPropiedad.Exitoso)
            {
                return validacionPropiedad;
            }

            _mppEspacio.Publicar(idEspacioArtistico);

            _bitacora.Registrar(
                idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idEspacioArtistico,
                "Publicación del espacio artístico \"" + espacio.NombreEspacio + "\".");

            return ResultadoOperacion.Ok("El espacio ya está publicado y va a poder verse en el catálogo público.");
        }

        public ResultadoOperacion Pausar(int idEspacioArtistico, int idUsuarioGestorSolicitante)
        {
            EspacioArtistico espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
            ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
            if (!validacionPropiedad.Exitoso)
            {
                return validacionPropiedad;
            }

            _mppEspacio.Pausar(idEspacioArtistico);

            _bitacora.Registrar(
                idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idEspacioArtistico,
                "Pausa del espacio artístico \"" + espacio.NombreEspacio + "\" (dejó de estar publicado).");

            return ResultadoOperacion.Ok("El espacio quedó pausado y ya no se muestra en el catálogo público.");
        }

        public ResultadoOperacion DarDeBaja(int idEspacioArtistico, int idUsuarioGestorSolicitante)
        {
            EspacioArtistico espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
            ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
            if (!validacionPropiedad.Exitoso)
            {
                return validacionPropiedad;
            }

            _mppEspacio.BajaLogica(idEspacioArtistico);

            _bitacora.Registrar(
                idUsuarioGestorSolicitante, "BAJA", TipoEntidadBitacora, idEspacioArtistico,
                "Baja lógica del espacio artístico \"" + espacio.NombreEspacio + "\".");

            return ResultadoOperacion.Ok("El espacio se dio de baja. Se mantiene en el historial pero ya no está disponible.");
        }

        // A15 del CU-001-007: valores/campos con formato inválido.
        private static ResultadoOperacion ValidarDatosBasicos(string nombreEspacio, string descripcion, string tipoEspacio)
        {
            if (string.IsNullOrWhiteSpace(nombreEspacio))
            {
                return ResultadoOperacion.Error("Ingresá el nombre del espacio.", "A15");
            }

            if (nombreEspacio.Trim().Length > LongitudMaximaNombre)
            {
                return ResultadoOperacion.Error("El nombre del espacio no puede superar los " + LongitudMaximaNombre + " caracteres.", "A15");
            }

            if (string.IsNullOrWhiteSpace(tipoEspacio))
            {
                return ResultadoOperacion.Error("Ingresá el tipo de espacio.", "A15");
            }

            if (tipoEspacio.Trim().Length > LongitudMaximaTipoEspacio)
            {
                return ResultadoOperacion.Error("El tipo de espacio no puede superar los " + LongitudMaximaTipoEspacio + " caracteres.", "A15");
            }

            if (!string.IsNullOrEmpty(descripcion) && descripcion.Trim().Length > LongitudMaximaDescripcion)
            {
                return ResultadoOperacion.Error("La descripción no puede superar los " + LongitudMaximaDescripcion + " caracteres.", "A15");
            }

            return ResultadoOperacion.Ok();
        }

        // El espacio debe existir, no estar dado de baja, y pertenecer al
        // usuario que está pidiendo la acción (nadie puede administrar el
        // espacio de otro gestor).
        private static ResultadoOperacion ValidarPropiedad(EspacioArtistico espacio, int idUsuarioGestorSolicitante)
        {
            if (espacio == null || !espacio.Activo)
            {
                return ResultadoOperacion.Error("El espacio no existe o ya fue dado de baja.");
            }

            if (espacio.IdUsuarioGestor != idUsuarioGestorSolicitante)
            {
                return ResultadoOperacion.Error("No tenés permiso para administrar este espacio.");
            }

            return ResultadoOperacion.Ok();
        }
    }
}
