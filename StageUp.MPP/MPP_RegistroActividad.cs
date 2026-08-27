using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_RegistroActividad
    {
        private readonly DAL_RegistroActividad _dal = new DAL_RegistroActividad();

        public void Insertar(RegistroActividad registro)
        {
            _dal.Insertar(
                registro.IdUsuarioExternoResponsable,
                registro.IdUsuarioInternoResponsable,
                registro.TipoOperacion,
                registro.TipoEntidadAfectada,
                registro.IdEntidadAfectada,
                registro.DescripcionOperacion,
                registro.OrigenOperacion);
        }
    }
}
