using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Security;
using System.Text;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

    public class MPP_ParametroPlataforma
    {
        public List<ParametroPlataforma> Listar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ParametroPlataforma_Listar");
            List<ParametroPlataforma> parametros = new List<ParametroPlataforma>();
            foreach (DataRow fila in tabla.Rows)
            {
                parametros.Add(new ParametroPlataforma
                {
                    Clave = fila["clave"].ToString(),
                    Valor = fila["valor"].ToString(),
                    Descripcion = Texto(fila, "descripcion"),
                    FechaUltimaModificacion = Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                    ValorProgramado = Texto(fila, "valorProgramado"),
                    FechaVigenciaProgramada = Fecha(fila, "fechaVigenciaProgramada")
                });
            }
            return parametros;
        }



        public List<CambioParametroPlataforma> GuardarCambios(IDictionary<string, string> valores, DateTime? fechaVigencia, int idUsuarioInterno)
        {
            StringBuilder cambios = new StringBuilder("<c>");
            foreach (KeyValuePair<string, string> valor in valores)
            {
                cambios.Append("<p clave=\"").Append(SecurityElement.Escape(valor.Key))
                       .Append("\" valor=\"").Append(SecurityElement.Escape(valor.Value ?? string.Empty)).Append("\"/>");
            }
            cambios.Append("</c>");

            DataTable tabla = Conexion.Instance.Leer(
                "sp_ParametroPlataforma_GuardarCambios",
                new Hashtable
                {
                    { "@cambios", cambios.ToString() },
                    { "@fechaVigencia", fechaVigencia.HasValue ? (object)fechaVigencia.Value : DBNull.Value },
                    { "@idUsuarioInterno", idUsuarioInterno }
                });

            List<CambioParametroPlataforma> registrados = new List<CambioParametroPlataforma>();
            foreach (DataRow fila in tabla.Rows)
            {
                if (Texto(fila, "resultado") != "OK")
                {
                    return null;
                }

                registrados.Add(MapearCambio(fila));
            }
            return registrados;
        }

        public List<CambioParametroPlataforma> ListarCambios(string clave)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ParametroPlataforma_ListarCambios",
                new Hashtable { { "@clave", string.IsNullOrEmpty(clave) ? (object)DBNull.Value : clave } });

            List<CambioParametroPlataforma> cambios = new List<CambioParametroPlataforma>();
            foreach (DataRow fila in tabla.Rows)
            {
                cambios.Add(MapearCambio(fila));
            }
            return cambios;
        }



        public Dictionary<string, string> ObtenerValoresAl(DateTime fecha)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ParametroPlataforma_ObtenerValoresAl",
                new Hashtable { { "@fecha", fecha } });

            Dictionary<string, string> valores = new Dictionary<string, string>();
            foreach (DataRow fila in tabla.Rows)
            {
                valores[fila["clave"].ToString()] = fila["valor"].ToString();
            }
            return valores;
        }

        private static CambioParametroPlataforma MapearCambio(DataRow fila)
        {
            return new CambioParametroPlataforma
            {
                IdCambio = Convert.ToInt32(fila["idCambio"]),
                Clave = Texto(fila, "clave"),
                ValorAnterior = Texto(fila, "valorAnterior"),
                ValorNuevo = Texto(fila, "valorNuevo") ?? string.Empty,
                FechaVigencia = Convert.ToDateTime(fila["fechaVigencia"]),
                Estado = Texto(fila, "estado"),
                IdUsuarioInterno = fila.Table.Columns.Contains("idUsuarioInterno") && fila["idUsuarioInterno"] != DBNull.Value
                    ? Convert.ToInt32(fila["idUsuarioInterno"]) : (int?)null,
                NombreResponsable = Texto(fila, "nombreResponsable"),
                FechaRegistro = Fecha(fila, "fechaRegistro") ?? DateTime.Now,
                FechaAplicacion = Fecha(fila, "fechaAplicacion")
            };
        }

        private static string Texto(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? null : fila[columna].ToString();
        }

        private static DateTime? Fecha(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila[columna]);
        }
    }
}
