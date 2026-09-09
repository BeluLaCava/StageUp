using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_CodigoRecuperacion
    {
        private readonly MPP_CodigoRecuperacion _mppCodigoRecuperacion = new MPP_CodigoRecuperacion();

        public int Insertar(CodigoRecuperacion codigo)
        {
            return _mppCodigoRecuperacion.Insertar(codigo);
        }

        public CodigoRecuperacion ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            return _mppCodigoRecuperacion.ObtenerVigentePorUsuario(idUsuarioExterno);
        }

        public void MarcarUtilizado(int idCodigoRecuperacion)
        {
            _mppCodigoRecuperacion.MarcarUtilizado(idCodigoRecuperacion);
        }
    }
}
