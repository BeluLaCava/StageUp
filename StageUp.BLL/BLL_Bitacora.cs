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
            "APROBACION", "RECHAZO", "ASIGNACION_PERMISOS", "ASOCIACION", "DESVINCULACION", "ERROR",
            "RESPUESTA", "CIERRE", "BACKUP", "RESTAURACION", "PAGO", "ANULACION", "LIQUIDACION", "BLOQUEO"
        };

        public static readonly string[] TiposDeEntidadAfectada =
        {
            "UsuarioExterno", "UsuarioInterno", "EspacioArtistico", "Reserva", "Calificacion", "RolInterno", "Idioma", "Traduccion", "ComponentePermiso",
            "Actividad", "Participante", "Encuesta", "Sistema", "Ticket", "OpcionMenu", "BaseDeDatos",
            "Pago", "Comprobante", "CuentaCorriente", "ParametroPlataforma", "SolicitudHabilitacionGestor", "Disponibilidad"
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

        // CU-001-013 / ítem 36: búsqueda con todos los filtros (responsable
        // externo o interno por nombre o correo) y un tope de filas.
        public const int MaximoRegistrosPorBusqueda = 500;

        public List<RegistroActividad> Buscar(FiltroRegistroActividad filtro)
        {
            filtro = filtro ?? new FiltroRegistroActividad();
            if (filtro.Maximo <= 0 || filtro.Maximo > MaximoRegistrosPorBusqueda)
            {
                filtro.Maximo = MaximoRegistrosPorBusqueda;
            }

            if (filtro.TipoResponsable != "Externo" && filtro.TipoResponsable != "Interno" && filtro.TipoResponsable != "Sistema")
            {
                filtro.TipoResponsable = null;
            }

            if (!string.IsNullOrWhiteSpace(filtro.TextoResponsable) && filtro.TextoResponsable.Trim().Length > 150)
            {
                filtro.TextoResponsable = filtro.TextoResponsable.Trim().Substring(0, 150);
            }

            return _mpp.Buscar(filtro);
        }

        public RegistroActividad ObtenerPorId(int idRegistroActividad)
        {
            return _mpp.ObtenerPorId(new RegistroActividad { IdRegistroActividad = idRegistroActividad });
        }
    }
}
