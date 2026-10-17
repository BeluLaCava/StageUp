using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-004-005 Contenido promocional (script 81).
    public class MPP_ContenidoPromocional
    {
        public List<ContenidoPromocional> Listar(FiltroContenidoPromocional oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ContenidoPromocional_Listar",
                new Hashtable
                {
                    { "@vista", oFiltro.Vista ?? FiltroContenidoPromocional.VistaVigentes },
                    { "@estado", MPP_CatalogoComercial.Nulo(oFiltro.Estado) },
                    { "@tipoContenido", MPP_CatalogoComercial.Nulo(oFiltro.TipoContenido) },
                    { "@canalPublicacion", MPP_CatalogoComercial.Nulo(oFiltro.CanalPublicacion) },
                    { "@idCampaniaComercial", MPP_CatalogoComercial.Nulo(oFiltro.IdCampaniaComercial) },
                    { "@texto", MPP_CatalogoComercial.Nulo(oFiltro.Texto) }
                });

            List<ContenidoPromocional> lista = new List<ContenidoPromocional>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public ResumenContenidos Contar()
        {
            DataRow fila = Conexion.Instance.Leer("sp_ContenidoPromocional_Contar").Rows[0];
            return new ResumenContenidos
            {
                Total = MPP_CatalogoComercial.Entero(fila["total"]) ?? 0,
                Activos = MPP_CatalogoComercial.Entero(fila["activos"]) ?? 0,
                EnRevision = MPP_CatalogoComercial.Entero(fila["enRevision"]) ?? 0,
                Desactualizados = MPP_CatalogoComercial.Entero(fila["desactualizados"]) ?? 0,
                Bajas = MPP_CatalogoComercial.Entero(fila["bajas"]) ?? 0
            };
        }

        public ContenidoPromocional Obtener(ContenidoPromocional oContenido)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ContenidoPromocional_Obtener",
                new Hashtable { { "@idContenidoPromocional", oContenido.IdContenidoPromocional } });

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Devuelve el id y, si el archivo se reemplazó o se quitó, la ruta del anterior.
        public int Guardar(ContenidoPromocional oContenido, UsuarioInterno oUsuario, out string rutaArchivoAnterior)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ContenidoPromocional_Guardar",
                new Hashtable
                {
                    { "@idContenidoPromocional", oContenido.IdContenidoPromocional > 0 ? (object)oContenido.IdContenidoPromocional : DBNull.Value },
                    { "@titulo", oContenido.Titulo },
                    { "@descripcion", MPP_CatalogoComercial.Nulo(oContenido.Descripcion) },
                    { "@tipoContenido", oContenido.TipoContenido },
                    { "@canalPublicacion", oContenido.CanalPublicacion },
                    { "@idCampaniaComercial", MPP_CatalogoComercial.Nulo(oContenido.IdCampaniaComercial) },
                    { "@idAlianzaEstrategica", MPP_CatalogoComercial.Nulo(oContenido.IdAlianzaEstrategica) },
                    { "@estadoContenido", oContenido.EstadoContenido },
                    { "@urlContenido", MPP_CatalogoComercial.Nulo(oContenido.UrlContenido) },
                    { "@rutaArchivo", MPP_CatalogoComercial.Nulo(oContenido.RutaArchivo) },
                    { "@nombreArchivoOriginal", MPP_CatalogoComercial.Nulo(oContenido.NombreArchivoOriginal) },
                    { "@tipoMimeArchivo", MPP_CatalogoComercial.Nulo(oContenido.TipoMimeArchivo) },
                    { "@tamanioArchivo", MPP_CatalogoComercial.Nulo(oContenido.TamanioArchivo) },
                    { "@quitarArchivo", oContenido.QuitarArchivo },
                    { "@idUsuarioInterno", oUsuario.IdUsuarioInterno }
                });

            rutaArchivoAnterior = MPP_CatalogoComercial.Texto(tabla.Rows[0]["rutaArchivoAnterior"]);
            return Convert.ToInt32(tabla.Rows[0]["idContenidoPromocional"]);
        }

        public bool CambiarEstado(ContenidoPromocional oContenido)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_ContenidoPromocional_CambiarEstado",
                new Hashtable
                {
                    { "@idContenidoPromocional", oContenido.IdContenidoPromocional },
                    { "@estadoContenido", oContenido.EstadoContenido }
                });

            return Convert.ToInt32(resultado) > 0;
        }

        private static ContenidoPromocional Mapear(DataRow fila)
        {
            return new ContenidoPromocional
            {
                IdContenidoPromocional = Convert.ToInt32(fila["idContenidoPromocional"]),
                Titulo = fila["titulo"].ToString(),
                Descripcion = MPP_CatalogoComercial.Texto(fila["descripcion"]),
                TipoContenido = fila["tipoContenido"].ToString(),
                CanalPublicacion = fila["canalPublicacion"].ToString(),
                IdCampaniaComercial = MPP_CatalogoComercial.Entero(fila["idCampaniaComercial"]),
                NombreCampania = MPP_CatalogoComercial.Texto(fila["nombreCampania"]),
                IdAlianzaEstrategica = MPP_CatalogoComercial.Entero(fila["idAlianzaEstrategica"]),
                NombreAlianza = MPP_CatalogoComercial.Texto(fila["nombreAlianza"]),
                EstadoContenido = fila["estadoContenido"].ToString(),
                UrlContenido = MPP_CatalogoComercial.Texto(fila["urlContenido"]),
                RutaArchivo = MPP_CatalogoComercial.Texto(fila["rutaArchivo"]),
                NombreArchivoOriginal = MPP_CatalogoComercial.Texto(fila["nombreArchivoOriginal"]),
                TipoMimeArchivo = MPP_CatalogoComercial.Texto(fila["tipoMimeArchivo"]),
                TamanioArchivo = MPP_CatalogoComercial.Entero(fila["tamanioArchivo"]),
                IdUsuarioInternoResponsable = MPP_CatalogoComercial.Entero(fila["idUsuarioInternoResponsable"]),
                NombreResponsable = MPP_CatalogoComercial.Texto(fila["nombreResponsable"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimaModificacion = Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                FechaBaja = MPP_CatalogoComercial.Fecha(fila["fechaBaja"]),
                Activo = Convert.ToBoolean(fila["activo"])
            };
        }
    }
}
