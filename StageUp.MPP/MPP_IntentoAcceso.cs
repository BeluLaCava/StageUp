using System;
using System.Collections;
using StageUp.DAL;
namespace StageUp.MPP
{
    public class MPP_IntentoAcceso
    {
        public int Consultar(string cuenta)
        {
            return Convert.ToInt32(Conexion.Instance.LeerEscalar("sp_IntentoAcceso_Consultar", new Hashtable { { "@cuenta", cuenta } }));
        }
        public int Registrar(string cuenta, bool exitoso)
        {
            return Convert.ToInt32(Conexion.Instance.LeerEscalar("sp_IntentoAcceso_Registrar", new Hashtable { { "@cuenta", cuenta }, { "@exitoso", exitoso } }));
        }
    }
}
