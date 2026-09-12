using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Permisos;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_ComponentePermiso
    {
        public int InsertarGrupo(GrupoPermisos oGrupo, PermisoComponente oPadre)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_ComponentePermiso_InsertarGrupo",
                new Hashtable
                {
                    { "@idComponentePadre", oPadre == null ? (object)DBNull.Value : oPadre.IdComponentePermiso },
                    { "@nombre", oGrupo.Nombre }
                });
            return Convert.ToInt32(resultado);
        }

        public void MoverComponente(PermisoComponente oComponente, PermisoComponente oNuevoPadre)
        {
            Conexion.Instance.Guardar(
                "sp_ComponentePermiso_MoverComponente",
                new Hashtable
                {
                    { "@idComponentePermiso", oComponente.IdComponentePermiso },
                    { "@idNuevoComponentePadre", oNuevoPadre == null ? (object)DBNull.Value : oNuevoPadre.IdComponentePermiso }
                });
        }

        public void EliminarGrupo(PermisoComponente oGrupo)
        {
            Conexion.Instance.Guardar(
                "sp_ComponentePermiso_EliminarGrupo",
                new Hashtable { { "@idComponentePermiso", oGrupo.IdComponentePermiso } });
        }

        public GrupoPermisos ListarArbol()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ComponentePermiso_ListarTodos");

            List<DataRow> filas = new List<DataRow>();
            foreach (DataRow fila in tabla.Rows)
            {
                filas.Add(fila);
            }

            GrupoPermisos raiz = new GrupoPermisos(0, "Permisos");
            Dictionary<int, PermisoComponente> nodos = new Dictionary<int, PermisoComponente>();

            foreach (DataRow fila in filas)
            {
                int idComponentePermiso = Convert.ToInt32(fila["idComponentePermiso"]);
                string tipoComponente = fila["tipoComponente"].ToString();
                string nombre = fila["nombre"].ToString();

                if (tipoComponente == "Grupo")
                {
                    nodos[idComponentePermiso] = new GrupoPermisos(idComponentePermiso, nombre);
                }
                else
                {
                    string codigoPermiso = fila["codigoPermiso"] == DBNull.Value ? null : fila["codigoPermiso"].ToString();
                    string descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString();
                    string urlAsociada = fila["urlAsociada"] == DBNull.Value ? null : fila["urlAsociada"].ToString();
                    nodos[idComponentePermiso] = new PermisoHoja(idComponentePermiso, codigoPermiso, nombre, descripcion, urlAsociada);
                }
            }

            foreach (DataRow fila in filas)
            {
                int idComponentePermiso = Convert.ToInt32(fila["idComponentePermiso"]);
                object valorPadre = fila["idComponentePadre"];
                PermisoComponente nodo = nodos[idComponentePermiso];

                GrupoPermisos padre = null;
                if (valorPadre != DBNull.Value)
                {
                    int idComponentePadre = Convert.ToInt32(valorPadre);
                    PermisoComponente candidatoPadre;
                    if (nodos.TryGetValue(idComponentePadre, out candidatoPadre))
                    {
                        padre = candidatoPadre as GrupoPermisos;
                    }
                }

                if (padre == null)
                {
                    padre = raiz;
                }

                padre.AgregarHijo(nodo);

                PermisoHoja hoja = nodo as PermisoHoja;
                if (hoja != null)
                {
                    hoja.NombreGrupo = padre.Nombre;
                }
            }

            return raiz;
        }
    }
}
