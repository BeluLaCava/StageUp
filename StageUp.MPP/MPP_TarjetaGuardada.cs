using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{


    public class MPP_TarjetaGuardada
    {
        public List<TarjetaGuardada> ListarPorUsuario(TarjetaGuardada oTarjeta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TarjetaGuardada_ListarPorUsuario",
                new Hashtable { { "@idUsuarioExterno", oTarjeta.IdUsuarioExterno } });

            List<TarjetaGuardada> lista = new List<TarjetaGuardada>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public TarjetaGuardada ObtenerParaUsar(TarjetaGuardada oTarjeta)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TarjetaGuardada_ObtenerParaUsar",
                new Hashtable
                {
                    { "@idTarjetaGuardada", oTarjeta.IdTarjetaGuardada },
                    { "@idUsuarioExterno", oTarjeta.IdUsuarioExterno }
                });
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            TarjetaGuardada tarjeta = Mapear(fila);
            tarjeta.Iv = Texto(fila, "iv");
            tarjeta.ClaveCifrada = Texto(fila, "claveCifrada");
            tarjeta.DatosCifrados = Texto(fila, "datosCifrados");
            tarjeta.IdClaveCifrado = fila["idClaveCifrado"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idClaveCifrado"]);
            return tarjeta;
        }


        public string Insertar(TarjetaGuardada oTarjeta, int maximo, out int idTarjetaGuardada, out bool quedoPreferida)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TarjetaGuardada_Insertar",
                new Hashtable
                {
                    { "@idUsuarioExterno", oTarjeta.IdUsuarioExterno },
                    { "@alias", oTarjeta.Alias },
                    { "@marca", oTarjeta.Marca },
                    { "@ultimosDigitos", oTarjeta.UltimosDigitos },
                    { "@mesVencimiento", oTarjeta.MesVencimiento },
                    { "@anioVencimiento", oTarjeta.AnioVencimiento },
                    { "@preferida", oTarjeta.Preferida },
                    { "@huella", oTarjeta.Huella },
                    { "@iv", oTarjeta.Iv },
                    { "@claveCifrada", oTarjeta.ClaveCifrada },
                    { "@datosCifrados", oTarjeta.DatosCifrados },
                    { "@idClaveCifrado", oTarjeta.IdClaveCifrado },
                    { "@maximo", maximo }
                });

            DataRow fila = tabla.Rows[0];
            idTarjetaGuardada = fila["idTarjetaGuardada"] == DBNull.Value ? 0 : Convert.ToInt32(fila["idTarjetaGuardada"]);
            quedoPreferida = fila["preferida"] != DBNull.Value && Convert.ToBoolean(fila["preferida"]);
            return fila["resultado"].ToString();
        }

        public string CambiarAlias(TarjetaGuardada oTarjeta)
        {
            return Resultado(Conexion.Instance.Leer(
                "sp_TarjetaGuardada_CambiarAlias",
                new Hashtable
                {
                    { "@idTarjetaGuardada", oTarjeta.IdTarjetaGuardada },
                    { "@idUsuarioExterno", oTarjeta.IdUsuarioExterno },
                    { "@alias", oTarjeta.Alias }
                }));
        }

        public string MarcarPreferida(TarjetaGuardada oTarjeta)
        {
            return Resultado(Conexion.Instance.Leer(
                "sp_TarjetaGuardada_MarcarPreferida",
                new Hashtable
                {
                    { "@idTarjetaGuardada", oTarjeta.IdTarjetaGuardada },
                    { "@idUsuarioExterno", oTarjeta.IdUsuarioExterno }
                }));
        }

        public string Eliminar(TarjetaGuardada oTarjeta)
        {
            return Resultado(Conexion.Instance.Leer(
                "sp_TarjetaGuardada_Eliminar",
                new Hashtable
                {
                    { "@idTarjetaGuardada", oTarjeta.IdTarjetaGuardada },
                    { "@idUsuarioExterno", oTarjeta.IdUsuarioExterno }
                }));
        }

        public void RegistrarUso(TarjetaGuardada oTarjeta)
        {
            Conexion.Instance.Leer(
                "sp_TarjetaGuardada_RegistrarUso",
                new Hashtable { { "@idTarjetaGuardada", oTarjeta.IdTarjetaGuardada } });
        }


        public List<TarjetaGuardada> ListarParaReenvolver(ClaveCifrado oClaveActiva)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_TarjetaGuardada_ListarParaReenvolver",
                new Hashtable { { "@idClaveActiva", oClaveActiva.IdClaveCifrado } });

            List<TarjetaGuardada> lista = new List<TarjetaGuardada>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new TarjetaGuardada
                {
                    IdTarjetaGuardada = Convert.ToInt32(fila["idTarjetaGuardada"]),
                    ClaveCifrada = Texto(fila, "claveCifrada"),
                    IdClaveCifrado = Convert.ToInt32(fila["idClaveCifrado"])
                });
            }
            return lista;
        }

        public bool Reenvolver(TarjetaGuardada oTarjeta, int idClaveAnterior)
        {
            object filas = Conexion.Instance.LeerEscalar(
                "sp_TarjetaGuardada_Reenvolver",
                new Hashtable
                {
                    { "@idTarjetaGuardada", oTarjeta.IdTarjetaGuardada },
                    { "@idClaveAnterior", idClaveAnterior },
                    { "@idClaveNueva", oTarjeta.IdClaveCifrado },
                    { "@claveCifrada", oTarjeta.ClaveCifrada }
                });
            return filas != null && filas != DBNull.Value && Convert.ToInt32(filas) > 0;
        }

        private static string Resultado(DataTable tabla)
        {
            return tabla.Rows.Count == 0 ? "NO_ENCONTRADA" : tabla.Rows[0]["resultado"].ToString();
        }

        private static TarjetaGuardada Mapear(DataRow fila)
        {
            return new TarjetaGuardada
            {
                IdTarjetaGuardada = Convert.ToInt32(fila["idTarjetaGuardada"]),
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                Alias = Texto(fila, "alias"),
                Marca = Texto(fila, "marca"),
                UltimosDigitos = Texto(fila, "ultimosDigitos"),
                MesVencimiento = Convert.ToInt32(fila["mesVencimiento"]),
                AnioVencimiento = Convert.ToInt32(fila["anioVencimiento"]),
                Preferida = Convert.ToBoolean(fila["preferida"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimoUso = fila["fechaUltimoUso"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimoUso"])
            };
        }

        private static string Texto(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? null : fila[columna].ToString();
        }
    }
}
