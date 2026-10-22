using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

    public class MPP_ClaveCifrado
    {
        public ClaveCifrado ObtenerActiva()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ClaveCifrado_ObtenerActiva");
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public ClaveCifrado ObtenerPorId(ClaveCifrado oClave)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ClaveCifrado_ObtenerPorId",
                new Hashtable
                {
                    { "@idClaveCifrado", oClave.IdClaveCifrado > 0 ? (object)oClave.IdClaveCifrado : DBNull.Value },
                    { "@version", oClave.Version > 0 ? (object)oClave.Version : DBNull.Value }
                });
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public List<ClaveCifrado> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ClaveCifrado_Listar");
            List<ClaveCifrado> lista = new List<ClaveCifrado>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }



        public string Registrar(ClaveCifrado oClave, bool soloSiNoHayActiva, out int idClaveCifrado, out int? idClaveAnterior, out int version)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ClaveCifrado_Registrar",
                new Hashtable
                {
                    { "@clavePublica", oClave.ClavePublica },
                    { "@clavePrivadaProtegida", oClave.ClavePrivadaProtegida },
                    { "@huella", oClave.Huella },
                    { "@motivo", (object)oClave.Motivo ?? DBNull.Value },
                    { "@idUsuarioInterno", oClave.IdUsuarioInternoAlta.HasValue ? (object)oClave.IdUsuarioInternoAlta.Value : DBNull.Value },
                    { "@soloSiNoHayActiva", soloSiNoHayActiva }
                });

            DataRow fila = tabla.Rows[0];
            idClaveCifrado = Convert.ToInt32(fila["idClaveCifrado"]);
            idClaveAnterior = fila["idClaveAnterior"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idClaveAnterior"]);
            version = fila["version"] == DBNull.Value ? 0 : Convert.ToInt32(fila["version"]);
            return fila["resultado"].ToString();
        }

        private static ClaveCifrado Mapear(DataRow fila)
        {
            return new ClaveCifrado
            {
                IdClaveCifrado = Convert.ToInt32(fila["idClaveCifrado"]),
                Version = Convert.ToInt32(fila["version"]),
                ClavePublica = Texto(fila, "clavePublica"),
                ClavePrivadaProtegida = Texto(fila, "clavePrivadaProtegida"),
                Huella = Texto(fila, "huella"),
                Estado = Texto(fila, "estado"),
                Motivo = Texto(fila, "motivo"),
                IdUsuarioInternoAlta = Entero(fila, "idUsuarioInternoAlta"),
                NombreUsuarioAlta = Texto(fila, "nombreUsuarioAlta"),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                IdUsuarioInternoRetiro = Entero(fila, "idUsuarioInternoRetiro"),
                NombreUsuarioRetiro = Texto(fila, "nombreUsuarioRetiro"),
                FechaRetiro = fila.Table.Columns.Contains("fechaRetiro") && fila["fechaRetiro"] != DBNull.Value ? Convert.ToDateTime(fila["fechaRetiro"]) : (DateTime?)null,
                TarjetasProtegidas = Entero(fila, "tarjetasProtegidas") ?? 0
            };
        }

        private static string Texto(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? null : fila[columna].ToString();
        }

        private static int? Entero(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? (int?)null : Convert.ToInt32(fila[columna]);
        }
    }
}
