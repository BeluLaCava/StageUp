using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-004-001 Campañas comerciales (script 80).
    public class MPP_CampaniaComercial
    {
        public List<CampaniaComercial> Listar(FiltroCampaniaComercial oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CampaniaComercial_Listar",
                new Hashtable
                {
                    { "@vista", oFiltro.Vista ?? FiltroCampaniaComercial.VistaActivas },
                    { "@estado", MPP_CatalogoComercial.Nulo(oFiltro.Estado) },
                    { "@canalDifusion", MPP_CatalogoComercial.Nulo(oFiltro.CanalDifusion) },
                    { "@segmento", MPP_CatalogoComercial.Nulo(oFiltro.Segmento) },
                    { "@desde", MPP_CatalogoComercial.Nulo(oFiltro.Desde) },
                    { "@hasta", MPP_CatalogoComercial.Nulo(oFiltro.Hasta) },
                    { "@texto", MPP_CatalogoComercial.Nulo(oFiltro.Texto) }
                });

            List<CampaniaComercial> lista = new List<CampaniaComercial>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public ResumenCampanias Contar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_CampaniaComercial_Contar");
            DataRow fila = tabla.Rows[0];
            return new ResumenCampanias
            {
                Total = MPP_CatalogoComercial.Entero(fila["total"]) ?? 0,
                Activas = MPP_CatalogoComercial.Entero(fila["activas"]) ?? 0,
                Bajas = MPP_CatalogoComercial.Entero(fila["bajas"]) ?? 0
            };
        }

        public CampaniaComercial Obtener(CampaniaComercial oCampania)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CampaniaComercial_Obtener",
                new Hashtable { { "@idCampaniaComercial", oCampania.IdCampaniaComercial } });

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public List<VinculoComercial> ListarVinculos(CampaniaComercial oCampania)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_CampaniaComercial_ListarVinculos",
                new Hashtable { { "@idCampaniaComercial", oCampania.IdCampaniaComercial } });

            List<VinculoComercial> lista = new List<VinculoComercial>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new VinculoComercial
                {
                    Tipo = fila["tipo"].ToString(),
                    Id = Convert.ToInt32(fila["id"]),
                    Nombre = fila["nombre"].ToString(),
                    Estado = MPP_CatalogoComercial.Texto(fila["estado"]),
                    Cantidad = MPP_CatalogoComercial.Entero(fila["cantidad"])
                });
            }
            return lista;
        }

        public int Guardar(CampaniaComercial oCampania)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_CampaniaComercial_Guardar",
                new Hashtable
                {
                    { "@idCampaniaComercial", oCampania.IdCampaniaComercial > 0 ? (object)oCampania.IdCampaniaComercial : DBNull.Value },
                    { "@nombre", oCampania.Nombre },
                    { "@descripcion", oCampania.Descripcion },
                    { "@canalDifusion", oCampania.CanalDifusion },
                    { "@fechaInicio", oCampania.FechaInicio },
                    { "@fechaFin", oCampania.FechaFin },
                    { "@estadoCampania", oCampania.EstadoCampania },
                    { "@publicoObjetivo", string.Join(",", oCampania.PublicoObjetivo ?? new List<string>()) },
                    { "@idUsuarioInternoResponsable", MPP_CatalogoComercial.Nulo(oCampania.IdUsuarioInternoResponsable) },
                    { "@idsContactos", MPP_CatalogoComercial.Ids(oCampania.IdsContactos) },
                    { "@idsAlianzas", MPP_CatalogoComercial.Ids(oCampania.IdsAlianzas) },
                    { "@idsContenidos", MPP_CatalogoComercial.Ids(oCampania.IdsContenidos) },
                    { "@idsOrigenes", MPP_CatalogoComercial.Ids(oCampania.IdsOrigenes) }
                });

            return Convert.ToInt32(resultado);
        }

        public bool DarDeBaja(CampaniaComercial oCampania)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_CampaniaComercial_DarDeBaja",
                new Hashtable { { "@idCampaniaComercial", oCampania.IdCampaniaComercial } });

            return Convert.ToInt32(resultado) > 0;
        }

        private static CampaniaComercial Mapear(DataRow fila)
        {
            string publico = MPP_CatalogoComercial.Texto(fila["publicoObjetivo"]) ?? string.Empty;
            return new CampaniaComercial
            {
                IdCampaniaComercial = Convert.ToInt32(fila["idCampaniaComercial"]),
                Nombre = fila["nombre"].ToString(),
                Descripcion = fila["descripcion"].ToString(),
                CanalDifusion = fila["canalDifusion"].ToString(),
                FechaInicio = Convert.ToDateTime(fila["fechaInicio"]),
                FechaFin = Convert.ToDateTime(fila["fechaFin"]),
                EstadoCampania = fila["estadoCampania"].ToString(),
                PublicoObjetivo = publico.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                IdUsuarioInternoResponsable = MPP_CatalogoComercial.Entero(fila["idUsuarioInternoResponsable"]),
                NombreResponsable = MPP_CatalogoComercial.Texto(fila["nombreResponsable"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimaModificacion = Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                FechaBaja = MPP_CatalogoComercial.Fecha(fila["fechaBaja"]),
                Activa = Convert.ToBoolean(fila["activa"]),
                CantidadContactos = Convert.ToInt32(fila["cantidadContactos"]),
                CantidadAlianzas = Convert.ToInt32(fila["cantidadAlianzas"]),
                CantidadContenidos = Convert.ToInt32(fila["cantidadContenidos"]),
                CantidadOrigenes = Convert.ToInt32(fila["cantidadOrigenes"])
            };
        }
    }
}
