using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_Faq
    {
        private readonly MPP_Faq _mpp = new MPP_Faq();

        public List<Faq> ListarActivas()
        {
            return _mpp.ListarActivas();
        }
    }
}
