using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Faq
    {
        // Usada por el centro de ayuda público (CentroAyuda.aspx.cs). Su
        // stored procedure (sp_Faq_ListarActivas, de 04_FaqDinamica.sql) no
        // trae fechaAlta/fechaUltimaModificacion, por eso Mapear las lee de
        // forma defensiva.
        public List<Faq> ListarActivas()
        {
            return Mapear(Conexion.Instance.Leer("sp_Faq_ListarActivas"));
        }

        // Todas las preguntas, activas e inactivas — para el panel de
        // administración (ítem 37 del checklist de correcciones).
        public List<Faq> Listar()
        {
            return Mapear(Conexion.Instance.Leer("sp_Faq_Listar"));
        }

        public Faq ObtenerPorId(Faq oFaq)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Faq_ObtenerPorId",
                new Hashtable { { "@idFaq", oFaq.IdFaq } });

            return tabla.Rows.Count == 0 ? null : MapearFila(tabla.Rows[0]);
        }

        public int Insertar(Faq oFaq)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_Faq_Insertar",
                new Hashtable
                {
                    { "@pregunta", oFaq.Pregunta },
                    { "@respuesta", oFaq.Respuesta },
                    { "@orden", oFaq.Orden },
                    { "@activo", oFaq.Activo }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(Faq oFaq)
        {
            Conexion.Instance.Guardar(
                "sp_Faq_Modificar",
                new Hashtable
                {
                    { "@idFaq", oFaq.IdFaq },
                    { "@pregunta", oFaq.Pregunta },
                    { "@respuesta", oFaq.Respuesta },
                    { "@orden", oFaq.Orden },
                    { "@activo", oFaq.Activo }
                });
        }

        public void DarDeBaja(Faq oFaq)
        {
            Conexion.Instance.Guardar(
                "sp_Faq_DarDeBaja",
                new Hashtable { { "@idFaq", oFaq.IdFaq } });
        }

        public void Reactivar(Faq oFaq)
        {
            Conexion.Instance.Guardar(
                "sp_Faq_Reactivar",
                new Hashtable { { "@idFaq", oFaq.IdFaq } });
        }

        private static List<Faq> Mapear(DataTable tabla)
        {
            List<Faq> lista = new List<Faq>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearFila(fila));
            }
            return lista;
        }

        private static Faq MapearFila(DataRow fila)
        {
            return new Faq
            {
                IdFaq = Convert.ToInt32(fila["idFaq"]),
                Pregunta = fila["pregunta"].ToString(),
                Respuesta = fila["respuesta"].ToString(),
                Orden = Convert.ToInt32(fila["orden"]),
                Activo = Convert.ToBoolean(fila["activo"]),
                FechaAlta = fila.Table.Columns.Contains("fechaAlta") && fila["fechaAlta"] != DBNull.Value
                    ? Convert.ToDateTime(fila["fechaAlta"])
                    : default(DateTime),
                FechaUltimaModificacion = fila.Table.Columns.Contains("fechaUltimaModificacion") && fila["fechaUltimaModificacion"] != DBNull.Value
                    ? Convert.ToDateTime(fila["fechaUltimaModificacion"])
                    : (DateTime?)null
            };
        }
    }
}
