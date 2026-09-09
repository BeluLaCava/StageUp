using System;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_Faq
    {
        public List<Faq> ListarActivas()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Faq_ListarActivas");

            var lista = new List<Faq>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new Faq
                {
                    IdFaq = Convert.ToInt32(fila["idFaq"]),
                    Pregunta = fila["pregunta"].ToString(),
                    Respuesta = fila["respuesta"].ToString(),
                    Orden = Convert.ToInt32(fila["orden"]),
                    Activo = Convert.ToBoolean(fila["activo"])
                });
            }
            return lista;
        }
    }
}
