using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Bitacora
    {
        private readonly MPP_Bitacora _mpp = new MPP_Bitacora();

        public static readonly string[] TiposDeOperacion =
        {
            "ALTA", "MODIFICACION", "BAJA", "ACTIVACION", "LOGIN", "RECUPERACION_SOLICITADA",
            "APROBACION", "RECHAZO", "ASIGNACION_PERMISOS"
        };

        public static readonly string[] TiposDeEntidadAfectada =
        {
            "UsuarioExterno", "UsuarioInterno", "EspacioArtistico", "Reserva", "Calificacion", "RolInterno", "Idioma", "Traduccion"
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

        public void RegistrarInterno(
            int idUsuarioInternoResponsable, string tipoOperacion, string tipoEntidadAfectada,
            int? idEntidadAfectada, string descripcionOperacion, string origenOperacion = "StageUp.UI")
        {
            _mpp.Insertar(new RegistroActividad
            {
                IdUsuarioExternoResponsable = null,
                IdUsuarioInternoResponsable = idUsuarioInternoResponsable,
                TipoOperacion = tipoOperacion,
                TipoEntidadAfectada = tipoEntidadAfectada,
                IdEntidadAfectada = idEntidadAfectada,
                DescripcionOperacion = descripcionOperacion,
                OrigenOperacion = origenOperacion
            });
        }

        public List<RegistroActividad> Buscar(
            int? idUsuarioExternoResponsable = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null,
            string tipoOperacion = null, string tipoEntidadAfectada = null)
        {
            return _mpp.Buscar(new FiltroRegistroActividad
            {
                IdUsuarioExternoResponsable = idUsuarioExternoResponsable,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TipoOperacion = tipoOperacion,
                TipoEntidadAfectada = tipoEntidadAfectada
            });
        }
    }
}
