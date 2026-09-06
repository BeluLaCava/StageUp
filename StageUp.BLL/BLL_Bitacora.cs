using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    /// <summary>
    /// Registro y consulta de la bitácora del sistema (CU-001-013
    /// Consultar registros de actividad). <see cref="Registrar"/> se usa
    /// desde el resto de la BLL cada vez que una acción impacta en la
    /// persistencia de información sensible; <see cref="Buscar"/> permite
    /// consultarla, con filtros básicos opcionales.
    ///
    /// Supuesto de alcance (a validar con la cátedra/cliente si hiciera
    /// falta ajustarlo): el CU-001-013 define como actor a un "usuario
    /// interno autorizado" con permisos específicos. Como el login y los
    /// permisos de usuarios internos todavía no están implementados en la
    /// aplicación, la pantalla de consulta (Interno/RegistroActividad.aspx)
    /// se restringe por ahora a cualquier usuario externo autenticado —
    /// mismo criterio ya aplicado en "Mis espacios" para EspacioArtistico.
    /// </summary>
    public class BLL_Bitacora
    {
        private readonly MPP_RegistroActividad _mpp = new MPP_RegistroActividad();

        /// <summary>
        /// Tipos de operación que registra hoy el sistema, para poblar el
        /// filtro de la pantalla de consulta (CU-001-013, filtro "tipo de
        /// operación"). Es un vocabulario fijo controlado por el propio
        /// código (no texto libre del usuario), así que se mantiene acá en
        /// vez de consultarlo con un DISTINCT contra la base.
        /// </summary>
        public static readonly string[] TiposDeOperacion =
        {
            "ALTA", "MODIFICACION", "BAJA", "ACTIVACION", "LOGIN", "RECUPERACION_SOLICITADA"
        };

        /// <summary>
        /// Tipos de entidad afectada que registra hoy el sistema, para el
        /// filtro "entidad afectada" del CU-001-013.
        /// </summary>
        public static readonly string[] TiposDeEntidadAfectada =
        {
            "UsuarioExterno", "EspacioArtistico"
        };

        public void Registrar(
            int? idUsuarioExternoResponsable, string tipoOperacion, string tipoEntidadAfectada,
            int? idEntidadAfectada, string descripcionOperacion, string origenOperacion = "StageUp.UI")
        {
            _mpp.Insertar(new RegistroActividad
            {
                IdUsuarioExternoResponsable = idUsuarioExternoResponsable,
                IdUsuarioInternoResponsable = null,
                TipoOperacion = tipoOperacion,
                TipoEntidadAfectada = tipoEntidadAfectada,
                IdEntidadAfectada = idEntidadAfectada,
                DescripcionOperacion = descripcionOperacion,
                OrigenOperacion = origenOperacion
            });
        }

        /// <summary>
        /// Consulta la bitácora aplicando filtros básicos opcionales
        /// (escenario principal + A3 "Aplicación de filtros de consulta"
        /// del CU-001-013). Cualquier filtro en null/vacío se ignora.
        /// </summary>
        public List<RegistroActividad> Buscar(
            int? idUsuarioExternoResponsable = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null,
            string tipoOperacion = null, string tipoEntidadAfectada = null)
        {
            return _mpp.Buscar(idUsuarioExternoResponsable, fechaDesde, fechaHasta, tipoOperacion, tipoEntidadAfectada);
        }
    }
}
