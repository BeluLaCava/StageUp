using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Catálogos de la gestión comercial y opciones para vincular (script 80).
    public class MPP_CatalogoComercial
    {
        public List<ItemCatalogoComercial> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_CatalogoComercial_Listar");
            List<ItemCatalogoComercial> lista = new List<ItemCatalogoComercial>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new ItemCatalogoComercial
                {
                    TipoCatalogo = fila["tipoCatalogo"].ToString(),
                    Codigo = fila["codigo"].ToString(),
                    Nombre = fila["nombre"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"])
                });
            }
            return lista;
        }

        public List<OpcionVinculoComercial> ListarParaVincular(OpcionVinculoComercial oTipo)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Comercial_ListarParaVincular",
                new Hashtable { { "@tipo", oTipo.Tipo } });

            List<OpcionVinculoComercial> lista = new List<OpcionVinculoComercial>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new OpcionVinculoComercial
                {
                    Tipo = oTipo.Tipo,
                    Id = Convert.ToInt32(fila["id"]),
                    Nombre = fila["nombre"].ToString(),
                    Estado = Texto(fila["estado"]),
                    Detalle = Texto(fila["detalle"]),
                    IdCampaniaActual = fila["idCampaniaActual"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idCampaniaActual"])
                });
            }
            return lista;
        }

        internal static string Texto(object valor)
        {
            return valor == null || valor == DBNull.Value ? null : valor.ToString();
        }

        internal static int? Entero(object valor)
        {
            return valor == null || valor == DBNull.Value ? (int?)null : Convert.ToInt32(valor);
        }

        internal static DateTime? Fecha(object valor)
        {
            return valor == null || valor == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(valor);
        }

        internal static object Nulo(object valor)
        {
            if (valor == null)
            {
                return DBNull.Value;
            }

            string texto = valor as string;
            return texto != null && texto.Length == 0 ? DBNull.Value : valor;
        }

        internal static string Ids(List<int> ids)
        {
            return ids == null || ids.Count == 0 ? string.Empty : string.Join(",", ids);
        }
    }
}
