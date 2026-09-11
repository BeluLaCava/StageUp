using StageUp.BE.Entidades;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_RolInternoPermiso
    {
        private readonly MPP_RolInternoPermiso _mppRolPermiso = new MPP_RolInternoPermiso();

        public void EliminarPorRol(int idRolInterno)
        {
            _mppRolPermiso.EliminarPorRol(new RolInterno { IdRolInterno = idRolInterno });
        }

        public void Insertar(int idRolInterno, int idPermisoInterno)
        {
            _mppRolPermiso.Insertar(
                new RolInterno { IdRolInterno = idRolInterno },
                new PermisoInterno { IdPermisoInterno = idPermisoInterno });
        }
    }
}
