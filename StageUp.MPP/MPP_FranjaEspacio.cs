using System;
using System.Collections;
using System.Globalization;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{



    public class MPP_FranjaEspacio
    {
        public int InsertarManual(int idEspacioArtistico, FranjaEspacio oFranja)
        {
            Hashtable parametros = CrearParametros(idEspacioArtistico, oFranja);
            parametros.Add("@bloqueado", oFranja.Bloqueado);
            return Convert.ToInt32(Conexion.Instance.LeerEscalar("sp_FranjaEspacio_InsertarManual", parametros), CultureInfo.InvariantCulture);
        }


        public bool ModificarManual(int idEspacioArtistico, FranjaEspacio oFranja)
        {
            Hashtable parametros = CrearParametros(idEspacioArtistico, oFranja);
            parametros.Add("@idFranjaEspacio", oFranja.IdFranjaEspacio.Value);
            return Convert.ToInt32(Conexion.Instance.LeerEscalar("sp_FranjaEspacio_ModificarManual", parametros), CultureInfo.InvariantCulture) > 0;
        }

        public bool EliminarManual(int idEspacioArtistico, int idFranjaEspacio)
        {
            object filas = Conexion.Instance.LeerEscalar(
                "sp_FranjaEspacio_EliminarManual",
                new Hashtable
                {
                    { "@idFranjaEspacio", idFranjaEspacio },
                    { "@idEspacioArtistico", idEspacioArtistico }
                });
            return Convert.ToInt32(filas, CultureInfo.InvariantCulture) > 0;
        }

        private static Hashtable CrearParametros(int idEspacioArtistico, FranjaEspacio oFranja)
        {
            bool fechaConcreta = !string.IsNullOrEmpty(oFranja.Fecha);
            return new Hashtable
            {
                { "@idEspacioArtistico", idEspacioArtistico },
                { "@diaSemana", !fechaConcreta && oFranja.DiaSemana.HasValue ? (object)oFranja.DiaSemana.Value : DBNull.Value },
                { "@fecha", fechaConcreta
                    ? (object)DateTime.ParseExact(oFranja.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : DBNull.Value },
                { "@minutoDesde", oFranja.MinutoDesde },
                { "@minutoHasta", oFranja.MinutoHasta },
                { "@motivoBloqueo", string.IsNullOrWhiteSpace(oFranja.MotivoBloqueo) ? (object)DBNull.Value : oFranja.MotivoBloqueo }
            };
        }
    }
}
