using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_AreaInterna
    {
        private readonly MPP_AreaInterna _mppArea = new MPP_AreaInterna();

        public List<AreaInterna> ListarActivas()
        {
            try
            {
                return _mppArea.ListarActivas();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<AreaInterna>();
            }
        }

        public AreaInterna ObtenerPorId(int idAreaInterna)
        {
            try
            {
                return _mppArea.ObtenerPorId(idAreaInterna);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }
    }
}
