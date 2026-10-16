using System;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-003-001: pasos y acciones del asistente guiado (script 70). Mismo
    // patrón que el resto de los MPP (Conexion.Instance).
    public class MPP_NodoAsistente
    {
        public List<NodoAsistente> ListarActivos()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_NodoAsistente_ListarActivos");
            List<NodoAsistente> lista = new List<NodoAsistente>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new NodoAsistente
                {
                    IdNodoAsistente = Convert.ToInt32(fila["idNodoAsistente"]),
                    IdNodoPadre = fila["idNodoPadre"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idNodoPadre"]),
                    Codigo = fila["codigo"].ToString(),
                    Tipo = fila["tipo"].ToString(),
                    TextoOpcion = fila["textoOpcion"].ToString(),
                    Mensaje = fila["mensaje"].ToString(),
                    PalabrasClave = fila["palabrasClave"] == DBNull.Value ? null : fila["palabrasClave"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"])
                });
            }
            return lista;
        }

        public List<AccionNodoAsistente> ListarAccionesActivas()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_AccionNodoAsistente_ListarActivas");
            List<AccionNodoAsistente> lista = new List<AccionNodoAsistente>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new AccionNodoAsistente
                {
                    IdAccionNodoAsistente = Convert.ToInt32(fila["idAccionNodoAsistente"]),
                    IdNodoAsistente = Convert.ToInt32(fila["idNodoAsistente"]),
                    Texto = fila["texto"].ToString(),
                    TipoAccion = fila["tipoAccion"].ToString(),
                    Destino = fila["destino"] == DBNull.Value ? null : fila["destino"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"])
                });
            }
            return lista;
        }
    }
}
