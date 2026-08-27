using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    /// <summary>
    /// Registro de acciones en la bitácora del sistema (CU-001-013).
    /// Se usa desde el resto de la BLL cada vez que una acción impacta en
    /// la persistencia de información sensible.
    /// </summary>
    public class BLL_Bitacora
    {
        private readonly MPP_RegistroActividad _mpp = new MPP_RegistroActividad();

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
    }
}
