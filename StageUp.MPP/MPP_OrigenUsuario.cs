using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

    public class MPP_OrigenUsuario
    {
        public List<OrigenUsuario> Listar(OrigenUsuario oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_OrigenUsuario_Listar",
                new Hashtable { { "@soloActivos", oFiltro.Activo } });

            List<OrigenUsuario> lista = new List<OrigenUsuario>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public OrigenUsuario Obtener(OrigenUsuario oOrigen)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_OrigenUsuario_Obtener",
                new Hashtable
                {
                    { "@idOrigenUsuario", oOrigen.IdOrigenUsuario > 0 ? (object)oOrigen.IdOrigenUsuario : DBNull.Value },
                    { "@codigo", MPP_CatalogoComercial.Nulo(oOrigen.Codigo) }
                });

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public int Guardar(OrigenUsuario oOrigen)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_OrigenUsuario_Guardar",
                new Hashtable
                {
                    { "@idOrigenUsuario", oOrigen.IdOrigenUsuario > 0 ? (object)oOrigen.IdOrigenUsuario : DBNull.Value },
                    { "@nombreOrigen", oOrigen.NombreOrigen },
                    { "@codigo", oOrigen.Codigo },
                    { "@canalOrigen", oOrigen.CanalOrigen },
                    { "@descripcion", MPP_CatalogoComercial.Nulo(oOrigen.Descripcion) },
                    { "@idCampaniaComercial", MPP_CatalogoComercial.Nulo(oOrigen.IdCampaniaComercial) },
                    { "@idAlianzaEstrategica", MPP_CatalogoComercial.Nulo(oOrigen.IdAlianzaEstrategica) },
                    { "@visibleEnRegistro", oOrigen.VisibleEnRegistro }
                });

            return Convert.ToInt32(resultado);
        }

        public bool CambiarActivo(OrigenUsuario oOrigen)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_OrigenUsuario_CambiarActivo",
                new Hashtable
                {
                    { "@idOrigenUsuario", oOrigen.IdOrigenUsuario },
                    { "@activo", oOrigen.Activo }
                });

            return Convert.ToInt32(resultado) > 0;
        }

        public bool AsignarAUsuario(UsuarioExterno oUsuario, OrigenUsuario oOrigen, bool soloSiVacio)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_UsuarioExterno_AsignarOrigen",
                new Hashtable
                {
                    { "@idUsuarioExterno", oUsuario.IdUsuarioExterno },
                    { "@idOrigenUsuario", oOrigen.IdOrigenUsuario },
                    { "@soloSiVacio", soloSiVacio }
                });

            return Convert.ToInt32(resultado) > 0;
        }

        private static OrigenUsuario Mapear(DataRow fila)
        {
            return new OrigenUsuario
            {
                IdOrigenUsuario = Convert.ToInt32(fila["idOrigenUsuario"]),
                NombreOrigen = fila["nombreOrigen"].ToString(),
                Codigo = fila["codigo"].ToString(),
                CanalOrigen = fila["canalOrigen"].ToString(),
                Descripcion = MPP_CatalogoComercial.Texto(fila["descripcion"]),
                IdCampaniaComercial = MPP_CatalogoComercial.Entero(fila["idCampaniaComercial"]),
                NombreCampania = MPP_CatalogoComercial.Texto(fila["nombreCampania"]),
                IdAlianzaEstrategica = MPP_CatalogoComercial.Entero(fila["idAlianzaEstrategica"]),
                NombreAlianza = MPP_CatalogoComercial.Texto(fila["nombreAlianza"]),
                VisibleEnRegistro = Convert.ToBoolean(fila["visibleEnRegistro"]),
                FechaRegistro = Convert.ToDateTime(fila["fechaRegistro"]),
                FechaUltimaModificacion = Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"]),
                CantidadUsuarios = Convert.ToInt32(fila["cantidadUsuarios"]),
                CantidadContactos = Convert.ToInt32(fila["cantidadContactos"])
            };
        }
    }
}
