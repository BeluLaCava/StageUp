using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;
namespace StageUp.MPP
{
    public class MPP_CorreoAdministrativo
    {
        public int Iniciar(CorreoAdministrativo c)
        {
            return Convert.ToInt32(Conexion.Instance.LeerEscalar("sp_CorreoAdministrativo_Iniciar", new Hashtable {
                { "@destinatario", c.Destinatario }, { "@asunto", c.Asunto },
                { "@idReserva", (object)c.IdReserva ?? DBNull.Value }, { "@idPago", (object)c.IdPago ?? DBNull.Value },
                { "@idComprobante", (object)c.IdComprobante ?? DBNull.Value } }));
        }
        public void Finalizar(int id, bool enviado)
        {
            Conexion.Instance.Guardar("sp_CorreoAdministrativo_Finalizar", new Hashtable { { "@idCorreo", id }, { "@enviado", enviado } });
        }
        public List<CorreoAdministrativo> Listar(string estado, DateTime? desde, DateTime? hasta, int? reserva, string destinatario)
        {
            DataTable tabla = Conexion.Instance.Leer("sp_CorreoAdministrativo_Listar", new Hashtable {
                { "@estado", (object)estado ?? DBNull.Value }, { "@desde", (object)desde ?? DBNull.Value },
                { "@hasta", (object)hasta ?? DBNull.Value }, { "@idReserva", (object)reserva ?? DBNull.Value },
                { "@destinatario", (object)destinatario ?? DBNull.Value } });
            List<CorreoAdministrativo> resultado = new List<CorreoAdministrativo>();
            foreach (DataRow f in tabla.Rows) resultado.Add(new CorreoAdministrativo {
                IdCorreo = Convert.ToInt32(f["idCorreo"]), Destinatario = Convert.ToString(f["destinatario"]), Asunto = Convert.ToString(f["asunto"]),
                Estado = Convert.ToString(f["estado"]), Detalle = Convert.ToString(f["detalle"]), FechaIntento = Convert.ToDateTime(f["fechaIntento"]),
                FechaResultado = f["fechaResultado"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(f["fechaResultado"]),
                IdReserva = f["idReserva"] == DBNull.Value ? (int?)null : Convert.ToInt32(f["idReserva"]),
                IdPago = f["idPago"] == DBNull.Value ? (int?)null : Convert.ToInt32(f["idPago"]),
                IdComprobante = f["idComprobante"] == DBNull.Value ? (int?)null : Convert.ToInt32(f["idComprobante"]) });
            return resultado;
        }
    }
}
