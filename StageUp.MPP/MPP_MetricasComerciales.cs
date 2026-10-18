using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-004-004 Métricas comerciales y origen de usuarios (script 81).
    public class MPP_MetricasComerciales
    {
        public ResumenMetricasComerciales ObtenerResumen(FiltroMetricasComerciales oFiltro)
        {
            DataRow fila = Conexion.Instance.Leer("sp_MetricasComerciales_Resumen", Parametros(oFiltro)).Rows[0];
            return new ResumenMetricasComerciales
            {
                NuevosUsuarios = Convert.ToInt32(fila["nuevosUsuarios"]),
                UsuariosConOrigen = Convert.ToInt32(fila["usuariosConOrigen"]),
                EspaciosIncorporados = Convert.ToInt32(fila["espaciosIncorporados"]),
                CampaniasActivas = Convert.ToInt32(fila["campaniasActivas"]),
                ContactosConvertidos = Convert.ToInt32(fila["contactosConvertidos"]),
                IncorporadosSinCuenta = fila.Table.Columns.Contains("incorporadosSinCuenta") ? Convert.ToInt32(fila["incorporadosSinCuenta"]) : 0,
                TotalUsuarios = Convert.ToInt32(fila["totalUsuarios"]),
                TotalCampanias = Convert.ToInt32(fila["totalCampanias"]),
                TotalContactos = Convert.ToInt32(fila["totalContactos"])
            };
        }

        public List<MetricaOrigenUsuario> ListarPorOrigen(FiltroMetricasComerciales oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer("sp_MetricasComerciales_PorOrigen", Parametros(oFiltro));
            List<MetricaOrigenUsuario> lista = new List<MetricaOrigenUsuario>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new MetricaOrigenUsuario
                {
                    IdOrigenUsuario = MPP_CatalogoComercial.Entero(fila["idOrigenUsuario"]),
                    CanalOrigen = fila["canalOrigen"].ToString(),
                    NombreOrigen = MPP_CatalogoComercial.Texto(fila["nombreOrigen"]),
                    IdCampaniaComercial = MPP_CatalogoComercial.Entero(fila["idCampaniaComercial"]),
                    NombreCampania = MPP_CatalogoComercial.Texto(fila["nombreCampania"]),
                    IdAlianzaEstrategica = MPP_CatalogoComercial.Entero(fila["idAlianzaEstrategica"]),
                    NombreAlianza = MPP_CatalogoComercial.Texto(fila["nombreAlianza"]),
                    CantidadUsuarios = Convert.ToInt32(fila["cantidadUsuarios"]),
                    CantidadGestores = Convert.ToInt32(fila["cantidadGestores"])
                });
            }
            return lista;
        }

        private static Hashtable Parametros(FiltroMetricasComerciales oFiltro)
        {
            return new Hashtable
            {
                { "@desde", MPP_CatalogoComercial.Nulo(oFiltro.Desde) },
                { "@hasta", MPP_CatalogoComercial.Nulo(oFiltro.Hasta) },
                { "@idCampaniaComercial", MPP_CatalogoComercial.Nulo(oFiltro.IdCampaniaComercial) },
                { "@canalOrigen", MPP_CatalogoComercial.Nulo(oFiltro.CanalOrigen) },
                { "@tipoUsuario", MPP_CatalogoComercial.Nulo(oFiltro.TipoUsuario) }
            };
        }
    }
}
