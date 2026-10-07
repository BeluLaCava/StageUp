using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-001-012 (corrección de María): no se puede dejar el sistema sin
    // ningún administrador activo. Se considera administrador a un usuario
    // interno activo cuyo rol le da los dos permisos de este caso de uso
    // (gestionar usuarios internos y gestionar roles): es quien puede volver
    // a dar accesos si hiciera falta.
    //
    // Antes de guardar un cambio que podría afectarlo (baja o desactivación de
    // un usuario, cambio de su rol, cambio de los permisos de un rol) se
    // simula cómo quedaría y se cuenta si sigue habiendo al menos uno.
    internal class BLL_ControlAdministradores
    {
        internal const string PermisoUsuarios = "GESTIONAR_USUARIOS_INTERNOS";
        internal const string PermisoRoles = "GESTIONAR_ROLES";

        internal const string MensajeSinAdministradores =
            "dejaría al sistema sin ningún administrador activo (un usuario interno activo con permisos para gestionar usuarios internos y roles). Asigná primero esos permisos a otra persona.";

        private readonly MPP_UsuarioInterno _mppUsuario = new MPP_UsuarioInterno();
        private readonly MPP_ComponentePermiso _mppComponente = new MPP_ComponentePermiso();
        private readonly MPP_RolInternoComponentePermiso _mppRolComponente = new MPP_RolInternoComponentePermiso();

        // Cambio a simular (todo opcional).
        internal class Cambio
        {
            public int? IdUsuario { get; set; }
            public bool UsuarioQuedaInactivo { get; set; }
            public int? NuevoRolUsuario { get; set; }
            public int? IdRol { get; set; }
            public List<int> NuevosComponentesRol { get; set; }
        }

        internal static bool EsRolAdministrador(IEnumerable<string> codigos)
        {
            List<string> lista = codigos.ToList();
            return lista.Contains(PermisoUsuarios) && lista.Contains(PermisoRoles);
        }

        // true si el cambio deja sin administradores a un sistema que hoy sí
        // tiene al menos uno (si hoy no hay ninguno, el cambio no lo empeora y
        // no se bloquea, para no trabar la configuración inicial).
        internal bool CambioDejaSinAdministradores(Cambio cambio)
        {
            return !QuedaAlgunAdministrador(cambio) && QuedaAlgunAdministrador(new Cambio());
        }

        // true si después del cambio sigue habiendo al menos un administrador.
        internal bool QuedaAlgunAdministrador(Cambio cambio)
        {
            GrupoPermisos arbol = _mppComponente.ListarArbol();
            Dictionary<int, bool> rolEsAdministrador = new Dictionary<int, bool>();

            foreach (UsuarioInterno usuario in _mppUsuario.Listar())
            {
                bool activo = usuario.Activo && usuario.EstadoCuenta == "Activa";
                int idRol = usuario.IdRolInterno;

                if (cambio.IdUsuario.HasValue && usuario.IdUsuarioInterno == cambio.IdUsuario.Value)
                {
                    if (cambio.UsuarioQuedaInactivo)
                    {
                        activo = false;
                    }

                    if (cambio.NuevoRolUsuario.HasValue)
                    {
                        idRol = cambio.NuevoRolUsuario.Value;
                    }
                }

                if (!activo)
                {
                    continue;
                }

                bool esAdministrador;
                if (!rolEsAdministrador.TryGetValue(idRol, out esAdministrador))
                {
                    List<int> componentes = cambio.IdRol.HasValue && cambio.IdRol.Value == idRol && cambio.NuevosComponentesRol != null
                        ? cambio.NuevosComponentesRol
                        : _mppRolComponente.ListarIdsPorRol(new RolInterno { IdRolInterno = idRol });

                    esAdministrador = EsRolAdministrador(
                        BLL_PermisoInterno.HojasDeSeleccion(arbol, componentes).Select(h => h.CodigoPermiso));
                    rolEsAdministrador[idRol] = esAdministrador;
                }

                if (esAdministrador)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
