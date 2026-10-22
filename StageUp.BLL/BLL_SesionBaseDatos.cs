using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{





















    public class BLL_SesionBaseDatos
    {
        private const int SegundosEntreConsultas = 10;
        private const string SinDatosDeRestauraciones = "s/d";

        private static readonly object Bloqueo = new object();
        private static string _marcaSql;
        private static DateTime _ultimaConsultaUtc = DateTime.MinValue;
        private static int _restauracionesDesdeLaAplicacion;
        private static bool _ultimaConsultaFallo = true;

        private readonly MPP_Backup _mppBackup = new MPP_Backup();
        private readonly MPP_UsuarioExterno _mppUsuarioExterno = new MPP_UsuarioExterno();
        private readonly BLL_UsuarioInterno _bllUsuarioInterno = new BLL_UsuarioInterno();


        public string ObtenerMarcaActual()
        {
            bool confiable;
            return ObtenerMarcaActual(out confiable);
        }




        public string ObtenerMarcaActual(out bool confiable)
        {
            bool consultar;
            lock (Bloqueo)
            {
                DateTime ahora = DateTime.UtcNow;
                consultar = _ultimaConsultaUtc > ahora || (ahora - _ultimaConsultaUtc).TotalSeconds >= SegundosEntreConsultas;
                if (consultar)
                {


                    _ultimaConsultaUtc = ahora;
                }
            }

            if (consultar)
            {
                string marca;
                try
                {
                    marca = _mppBackup.ObtenerMarcaUltimaRestauracion() ?? "0";
                }
                catch (Exception)
                {





                    marca = null;
                }

                lock (Bloqueo)
                {
                    _ultimaConsultaFallo = marca == null;
                    if (marca != null)
                    {
                        _marcaSql = marca;
                    }
                    else if (_marcaSql == null)
                    {
                        _marcaSql = SinDatosDeRestauraciones;
                    }
                }
            }

            lock (Bloqueo)
            {
                confiable = !_ultimaConsultaFallo && _marcaSql != null && _marcaSql != SinDatosDeRestauraciones;
                return _marcaSql == null ? null : _marcaSql + "-" + _restauracionesDesdeLaAplicacion;
            }
        }



        internal static void RegistrarRestauracion()
        {
            lock (Bloqueo)
            {
                _restauracionesDesdeLaAplicacion++;
                _ultimaConsultaUtc = DateTime.MinValue;
            }
        }

        public ResultadoRevalidacionSesion RevalidarUsuarioExterno(int idUsuarioExterno, DateTime? fechaAltaEnSesion, out UsuarioExterno usuario)
        {
            try
            {
                usuario = _mppUsuarioExterno.ObtenerPorId(new UsuarioExterno { IdUsuarioExterno = idUsuarioExterno });
            }
            catch (ErrorAccesoDatosException)
            {
                usuario = null;
                return ResultadoRevalidacionSesion.NoVerificada;
            }

            if (usuario == null ||
                usuario.EstadoCuenta != EstadoCuentaExterno.Activa.ToString() ||
                (!fechaAltaEnSesion.HasValue || usuario.FechaAlta != fechaAltaEnSesion.Value))
            {
                usuario = null;
                RegistrarCierre("UsuarioExterno", idUsuarioExterno);
                return ResultadoRevalidacionSesion.Invalida;
            }

            return ResultadoRevalidacionSesion.Valida;
        }

        public ResultadoRevalidacionSesion RevalidarUsuarioInterno(int idUsuarioInterno, DateTime? fechaAltaEnSesion,
            out UsuarioInterno usuario, out List<string> codigosPermisos)
        {
            try
            {


                usuario = _bllUsuarioInterno.ObtenerParaSesion(idUsuarioInterno, out codigosPermisos);
            }
            catch (ErrorAccesoDatosException)
            {
                usuario = null;
                codigosPermisos = null;
                return ResultadoRevalidacionSesion.NoVerificada;
            }

            if (usuario == null || (!fechaAltaEnSesion.HasValue || usuario.FechaAlta != fechaAltaEnSesion.Value))
            {
                usuario = null;
                codigosPermisos = null;
                RegistrarCierre("UsuarioInterno", idUsuarioInterno);
                return ResultadoRevalidacionSesion.Invalida;
            }

            return ResultadoRevalidacionSesion.Valida;
        }

        private static void RegistrarCierre(string tipoEntidad, int idCuenta)
        {
            try
            {

                new BLL_Bitacora().Registrar(null, "CIERRE", tipoEntidad, idCuenta,
                    "Se cerró la sesión abierta de la cuenta " + idCuenta +
                    " porque cambió la base de datos (restauración de un backup) y la cuenta no está, no está activa o el id es de otra cuenta.",
                    "Control de sesión");
            }
            catch (Exception)
            {

            }
        }
    }
}
