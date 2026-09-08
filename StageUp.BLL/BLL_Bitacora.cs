using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Bitacora
    {
        private readonly MPP_RegistroActividad _mpp = new MPP_RegistroActividad();

        public static readonly string[] TiposDeOperacion =
        {
            "ALTA", "MODIFICACION", "BAJA", "ACTIVACION", "LOGIN", "RECUPERACION_SOLICITADA"
        };

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

        public List<RegistroActividad> Buscar(
            int? idUsuarioExternoResponsable = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null,
            string tipoOperacion = null, string tipoEntidadAfectada = null)
        {
            return _mpp.Buscar(idUsuarioExternoResponsable, fechaDesde, fechaHasta, tipoOperacion, tipoEntidadAfectada);
        }
    }
}
