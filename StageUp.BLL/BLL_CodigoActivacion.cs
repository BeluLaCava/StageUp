using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_CodigoActivacion
    {
        private readonly MPP_CodigoActivacion _mppCodigoActivacion = new MPP_CodigoActivacion();

        public int Insertar(CodigoActivacion codigo)
        {
            return _mppCodigoActivacion.Insertar(codigo);
        }

        public CodigoActivacion ObtenerVigentePorUsuario(int idUsuarioExterno)
        {
            return _mppCodigoActivacion.ObtenerVigentePorUsuario(idUsuarioExterno);
        }

        public void MarcarUtilizado(int idCodigoActivacion)
        {
            _mppCodigoActivacion.MarcarUtilizado(idCodigoActivacion);
        }
    }
}
