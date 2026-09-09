using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_RolInternoPermiso
    {
        private readonly MPP_RolInternoPermiso _mppRolPermiso = new MPP_RolInternoPermiso();

        public void EliminarPorRol(int idRolInterno)
        {
            _mppRolPermiso.EliminarPorRol(idRolInterno);
        }

        public void Insertar(int idRolInterno, int idPermisoInterno)
        {
            _mppRolPermiso.Insertar(idRolInterno, idPermisoInterno);
        }
    }
}
