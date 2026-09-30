using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Backup y restore (ítem 17, script 46).
    public class MPP_Backup
    {
        public BackupBaseDatos Generar(BackupBaseDatos oBackup)
        {
            DataTable tabla = Conexion.Instance.LeerOperacionLarga(
                "sp_Backup_Generar",
                new Hashtable { { "@descripcion", (object)oBackup.Descripcion ?? DBNull.Value } });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            return new BackupBaseDatos
            {
                Ruta = tabla.Rows[0]["rutaBackup"].ToString(),
                Nombre = tabla.Rows[0]["nombreBackup"].ToString(),
                Descripcion = oBackup.Descripcion,
                Fecha = DateTime.Now
            };
        }

        public List<BackupBaseDatos> ListarHistorial()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_Backup_ListarHistorial");
            List<BackupBaseDatos> lista = new List<BackupBaseDatos>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new BackupBaseDatos
                {
                    IdBackup = Convert.ToInt32(fila["idBackup"]),
                    Nombre = fila["nombre"] == DBNull.Value ? null : fila["nombre"].ToString(),
                    Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                    Fecha = fila["fecha"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fecha"]),
                    TamanioBytes = fila["tamanioBytes"] == DBNull.Value ? 0L : Convert.ToInt64(fila["tamanioBytes"]),
                    Ruta = fila["ruta"].ToString()
                });
            }
            return lista;
        }

        // Se ejecuta conectado a master (una base no se puede restaurar a sí
        // misma). Después se descartan las conexiones del pool.
        public void Restaurar(BackupBaseDatos oBackup)
        {
            Conexion.Instance.EjecutarEnMaster(
                "sp_StageUp_RestaurarBackup",
                new Hashtable
                {
                    { "@nombreBase", Conexion.Instance.ObtenerNombreBaseDeDatos() },
                    { "@rutaBackup", oBackup.Ruta }
                });

            Conexion.Instance.LimpiarConexionesAbiertas();
        }
    }
}
