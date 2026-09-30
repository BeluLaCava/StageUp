using System;
using System.Collections.Generic;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // Backup y restore de la base (ítem 17 de la segunda entrega). Solo lo
    // usa Interno/BackupRestore.aspx (permiso GESTIONAR_BACKUP).
    //
    // Protecciones contra pisar datos por accidente al restaurar:
    //   1. Hay que escribir la palabra de confirmación (RESTAURAR).
    //   2. Solo se puede elegir un backup del historial de esta base (el SP
    //      además rechaza cualquier archivo que SQL Server no tenga
    //      registrado como backup de ella y lo verifica antes de restaurar).
    //   3. Antes de restaurar se genera automáticamente un backup del estado
    //      actual; si ese backup falla, no se restaura nada.
    //   4. Todo queda en la bitácora. El registro de la restauración se
    //      escribe DESPUÉS de restaurar, así queda en la base restaurada (lo
    //      anterior queda en el backup de seguridad).
    public class BLL_Backup
    {
        public const string PalabraConfirmacion = "RESTAURAR";
        private const int LongitudMaximaDescripcion = 200;
        private const string TipoEntidadBitacora = "BaseDeDatos";

        private readonly MPP_Backup _mpp = new MPP_Backup();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        public List<BackupBaseDatos> ListarHistorial()
        {
            try
            {
                return _mpp.ListarHistorial();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<BackupBaseDatos>();
            }
        }

        public ResultadoOperacion<BackupBaseDatos> GenerarBackup(string descripcion, int idUsuarioInternoResponsable)
        {
            string descripcionNormalizada = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
            if (descripcionNormalizada != null && descripcionNormalizada.Length > LongitudMaximaDescripcion)
            {
                return ResultadoOperacion<BackupBaseDatos>.Error(
                    "La descripción no puede superar los " + LongitudMaximaDescripcion + " caracteres.");
            }

            try
            {
                BackupBaseDatos backup = _mpp.Generar(new BackupBaseDatos { Descripcion = descripcionNormalizada ?? "Backup manual" });
                if (backup == null)
                {
                    return ResultadoOperacion<BackupBaseDatos>.Error("No se pudo generar el backup.");
                }

                _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "BACKUP", TipoEntidadBitacora, null,
                    "Backup de la base generado en " + backup.Ruta + ".");

                return ResultadoOperacion<BackupBaseDatos>.Ok(backup, "Backup generado: " + backup.NombreArchivo);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<BackupBaseDatos>.Error("No se pudo generar el backup. " + ex.Message);
            }
        }

        public ResultadoOperacion Restaurar(int idBackup, string confirmacion, int idUsuarioInternoResponsable)
        {
            if (!string.Equals((confirmacion ?? string.Empty).Trim(), PalabraConfirmacion, StringComparison.Ordinal))
            {
                return ResultadoOperacion.Error("Para restaurar tenés que escribir " + PalabraConfirmacion + " en mayúsculas.");
            }

            BackupBaseDatos elegido;
            try
            {
                elegido = _mpp.ListarHistorial().FirstOrDefault(b => b.IdBackup == idBackup);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error("No se pudo leer el historial de backups. " + ex.Message);
            }

            if (elegido == null)
            {
                return ResultadoOperacion.Error("El backup seleccionado ya no figura en el historial.");
            }

            BackupBaseDatos seguridad;
            try
            {
                seguridad = _mpp.Generar(new BackupBaseDatos
                {
                    Descripcion = "Automático antes de restaurar " + elegido.NombreArchivo
                });
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(
                    "No se restauró nada: falló el backup de seguridad del estado actual. " + ex.Message);
            }

            try
            {
                _mpp.Restaurar(elegido);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(
                    "No se pudo restaurar la base. El estado actual quedó guardado en " +
                    (seguridad != null ? seguridad.NombreArchivo : "un backup de seguridad") + ". " + ex.Message);
            }

            try
            {
                _bitacora.RegistrarInterno(idUsuarioInternoResponsable, "RESTAURACION", TipoEntidadBitacora, null,
                    "Base restaurada desde " + elegido.Ruta +
                    (elegido.Fecha.HasValue ? " (backup del " + elegido.Fecha.Value.ToString("dd/MM/yyyy HH:mm") + ")" : string.Empty) +
                    ". Backup de seguridad previo: " + (seguridad != null ? seguridad.Ruta : "-") + ".");
            }
            catch (Exception)
            {
                // El usuario interno puede no existir en un backup viejo; la
                // restauración igual se hizo.
            }

            return ResultadoOperacion.Ok(
                "La base se restauró desde " + elegido.NombreArchivo + ". El estado anterior quedó guardado en " +
                (seguridad != null ? seguridad.NombreArchivo : "un backup de seguridad") + ".");
        }
    }
}
