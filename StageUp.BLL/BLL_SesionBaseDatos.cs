using System;
using System.Collections.Generic;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Sesiones abiertas después de restaurar un backup (script 79).
    //
    // Al restaurar, la base vuelve al estado del backup, pero las sesiones
    // abiertas (en memoria del servidor web) siguen con los datos de antes:
    // una cuenta creada después del backup ya no existe, una dada de baja
    // puede volver a estar activa, los permisos pueden ser otros, y un id
    // que ya no existe se le puede asignar a la próxima cuenta que se cree.
    //
    //   * La marca de la base combina el id de la última restauración que
    //     registra SQL Server (msdb, así se detecta también una restauración
    //     hecha desde SSMS) con las restauraciones hechas desde esta ejecución
    //     de la aplicación. Se consulta como mucho cada 10 segundos.
    //   * Cada sesión guarda la marca con la que se validó. Si cambió, la
    //     cuenta se vuelve a leer de la base: la sesión sigue abierta solo si
    //     la cuenta existe, tiene la misma fecha de alta (si no, el id es de
    //     otra cuenta) y está activa. Si no, se cierra y queda en la bitácora.
    public class BLL_SesionBaseDatos
    {
        private const int SegundosEntreConsultas = 10;
        private const string SinDatosDeRestauraciones = "s/d";

        private static readonly object Bloqueo = new object();
        private static string _marcaSql;
        private static DateTime _ultimaConsultaUtc = DateTime.MinValue;
        private static int _restauracionesDesdeLaAplicacion;

        private readonly MPP_Backup _mppBackup = new MPP_Backup();
        private readonly MPP_UsuarioExterno _mppUsuarioExterno = new MPP_UsuarioExterno();
        private readonly BLL_UsuarioInterno _bllUsuarioInterno = new BLL_UsuarioInterno();

        // Null mientras no se sepa (la primera consulta todavía no terminó).
        public string ObtenerMarcaActual()
        {
            bool consultar;
            lock (Bloqueo)
            {
                DateTime ahora = DateTime.UtcNow;
                consultar = _ultimaConsultaUtc > ahora || (ahora - _ultimaConsultaUtc).TotalSeconds >= SegundosEntreConsultas;
                if (consultar)
                {
                    // Se marca antes de consultar: mientras tanto, los demás
                    // pedidos usan la última marca conocida.
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
                    // Sin permiso para leer msdb, servidor sin responder o
                    // configuración incompleta: se mantiene la última marca
                    // conocida. Las restauraciones hechas desde el panel se
                    // siguen detectando igual. Esto corre en cada pedido, por
                    // eso ningún error de acá debe cortar la página.
                    marca = null;
                }

                lock (Bloqueo)
                {
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
                return _marcaSql == null ? null : _marcaSql + "-" + _restauracionesDesdeLaAplicacion;
            }
        }

        // Lo llama BLL_Backup después de restaurar desde el panel: cambia la
        // marca enseguida, sin esperar la próxima consulta.
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
                (fechaAltaEnSesion.HasValue && usuario.FechaAlta != fechaAltaEnSesion.Value))
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
                // Devuelve null si no existe o no está activa, y los permisos
                // del rol según la base restaurada.
                usuario = _bllUsuarioInterno.ObtenerParaSesion(idUsuarioInterno, out codigosPermisos);
            }
            catch (ErrorAccesoDatosException)
            {
                usuario = null;
                codigosPermisos = null;
                return ResultadoRevalidacionSesion.NoVerificada;
            }

            if (usuario == null || (fechaAltaEnSesion.HasValue && usuario.FechaAlta != fechaAltaEnSesion.Value))
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
                // Sin responsable: la cuenta puede no existir en la base actual.
                new BLL_Bitacora().Registrar(null, "CIERRE", tipoEntidad, idCuenta,
                    "Se cerró la sesión abierta de la cuenta " + idCuenta +
                    " porque cambió la base de datos (restauración de un backup) y la cuenta no está, no está activa o el id es de otra cuenta.",
                    "Control de sesión");
            }
            catch (Exception)
            {
                // La bitácora no debe impedir el cierre de la sesión.
            }
        }
    }
}
