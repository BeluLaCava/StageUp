using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Novedad
    {


        public List<Novedad> Listar(string estado = null)
        {
            return Mapear(Conexion.Instance.Leer(
                "sp_Novedad_Listar",
                new Hashtable { { "@estado", (object)estado ?? DBNull.Value } }));
        }



        public List<Novedad> ListarPublicadas()
        {
            return Mapear(Conexion.Instance.Leer("sp_Novedad_ListarPublicadas"));
        }

        public Novedad ObtenerPorId(Novedad oNovedad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Novedad_ObtenerPorId",
                new Hashtable { { "@idNovedad", oNovedad.IdNovedad } });

            return tabla.Rows.Count == 0 ? null : MapearFila(tabla.Rows[0]);
        }

        public int Insertar(Novedad oNovedad)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Novedad_Insertar",
                new Hashtable
                {
                    { "@titulo", oNovedad.Titulo },
                    { "@resumen", oNovedad.Resumen },
                    { "@contenido", oNovedad.Contenido },
                    { "@categoria", oNovedad.Categoria },
                    { "@urlImagen", (object)oNovedad.UrlImagen ?? DBNull.Value },
                    { "@publicado", oNovedad.Publicado },
                    { "@fechaPublicacion", (object)oNovedad.FechaPublicacion ?? DBNull.Value },
                    { "@fechaVencimiento", (object)oNovedad.FechaVencimiento ?? DBNull.Value }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(Novedad oNovedad)
        {
            Conexion.Instance.Guardar(
                "sp_Novedad_Modificar",
                new Hashtable
                {
                    { "@idNovedad", oNovedad.IdNovedad },
                    { "@titulo", oNovedad.Titulo },
                    { "@resumen", oNovedad.Resumen },
                    { "@contenido", oNovedad.Contenido },
                    { "@categoria", oNovedad.Categoria },
                    { "@urlImagen", (object)oNovedad.UrlImagen ?? DBNull.Value },
                    { "@publicado", oNovedad.Publicado },
                    { "@fechaPublicacion", (object)oNovedad.FechaPublicacion ?? DBNull.Value },
                    { "@fechaVencimiento", (object)oNovedad.FechaVencimiento ?? DBNull.Value }
                });
        }

        public void Publicar(Novedad oNovedad)
        {
            Conexion.Instance.Guardar(
                "sp_Novedad_Publicar",
                new Hashtable { { "@idNovedad", oNovedad.IdNovedad } });
        }

        public void VolverABorrador(Novedad oNovedad)
        {
            Conexion.Instance.Guardar(
                "sp_Novedad_VolverABorrador",
                new Hashtable { { "@idNovedad", oNovedad.IdNovedad } });
        }

        public bool DarDeBaja(Novedad oNovedad, UsuarioInterno oResponsable)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Novedad_DarDeBaja",
                new Hashtable
                {
                    { "@idNovedad", oNovedad.IdNovedad },
                    { "@idUsuarioInternoBaja", oResponsable.IdUsuarioInterno }
                });
            return resultado != null && resultado != DBNull.Value && Convert.ToInt32(resultado) > 0;
        }

        public bool Restaurar(Novedad oNovedad)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Novedad_Restaurar",
                new Hashtable { { "@idNovedad", oNovedad.IdNovedad } });
            return resultado != null && resultado != DBNull.Value && Convert.ToInt32(resultado) > 0;
        }

        public void MarcarEnviadaPorCorreo(Novedad oNovedad)
        {
            Conexion.Instance.Guardar(
                "sp_Novedad_MarcarEnviadaPorCorreo",
                new Hashtable
                {
                    { "@idNovedad", oNovedad.IdNovedad },
                    { "@destinatarioNewsletter", (object)oNovedad.DestinatarioNewsletter ?? DBNull.Value }
                });
        }

        private static List<Novedad> Mapear(DataTable tabla)
        {
            List<Novedad> lista = new List<Novedad>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearFila(fila));
            }
            return lista;
        }

        private static Novedad MapearFila(DataRow fila)
        {
            return new Novedad
            {
                IdNovedad = Convert.ToInt32(fila["idNovedad"]),
                Titulo = fila["titulo"].ToString(),
                Resumen = fila["resumen"].ToString(),
                Contenido = fila["contenido"].ToString(),
                Categoria = fila["categoria"].ToString(),
                UrlImagen = fila["urlImagen"] == DBNull.Value ? null : fila["urlImagen"].ToString(),
                Publicado = Convert.ToBoolean(fila["publicado"]),
                FechaPublicacion = fila["fechaPublicacion"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(fila["fechaPublicacion"]),
                EnviadaPorCorreo = Convert.ToBoolean(fila["enviadaPorCorreo"]),
                DestinatarioNewsletter = fila["destinatarioNewsletter"] == DBNull.Value
                    ? null
                    : fila["destinatarioNewsletter"].ToString(),
                FechaEnvioNewsletter = fila["fechaEnvioNewsletter"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(fila["fechaEnvioNewsletter"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                FechaVencimiento = Fecha(fila, "fechaVencimiento"),
                FechaBaja = Fecha(fila, "fechaBaja"),
                Activo = !fila.Table.Columns.Contains("activo") || Convert.ToBoolean(fila["activo"]),
                Estado = fila.Table.Columns.Contains("estado") ? fila["estado"].ToString() : null
            };
        }

        private static DateTime? Fecha(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value
                ? Convert.ToDateTime(fila[columna])
                : (DateTime?)null;
        }
    }
}
