using System;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Permisos;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_ComponentePermiso
    {
        public GrupoPermisos ListarArbol()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ComponentePermiso_ListarTodos");

            var filas = new List<DataRow>();
            foreach (DataRow fila in tabla.Rows)
            {
                filas.Add(fila);
            }

            var raiz = new GrupoPermisos(0, "Permisos");
            var nodos = new Dictionary<int, PermisoComponente>();

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

                var hoja = nodo as PermisoHoja;
                if (hoja != null)
                {
                    hoja.NombreGrupo = padre.Nombre;
                }
            }

            return raiz;
        }
    }
}
