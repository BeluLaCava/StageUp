IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/16_ComponentePermiso.sql.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Backup_Generar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Backup_Generar;
GO
CREATE PROCEDURE dbo.sp_Backup_Generar
    @descripcion NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @carpeta NVARCHAR(400) = CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(400));
    IF @carpeta IS NULL OR LEN(@carpeta) = 0
    BEGIN
        THROW 51021, 'No se pudo determinar la carpeta de backups de la instancia de SQL Server.', 1;
    END

    IF RIGHT(@carpeta, 1) <> N'\'
    BEGIN
        SET @carpeta = @carpeta + N'\';
    END

    DECLARE @base SYSNAME = DB_NAME();
    DECLARE @marca NVARCHAR(30) = FORMAT(GETDATE(), 'yyyyMMdd_HHmmssfff');
    DECLARE @ruta NVARCHAR(400) = @carpeta + @base + N'_' + @marca + N'.bak';
    DECLARE @nombre NVARCHAR(128) = @base + N' - backup ' + @marca;

    BACKUP DATABASE @base
        TO DISK = @ruta
        WITH INIT, CHECKSUM, NAME = @nombre, DESCRIPTION = @descripcion;

    SELECT @ruta AS rutaBackup, @nombre AS nombreBackup;
END
GO

IF OBJECT_ID('dbo.sp_Backup_ListarHistorial', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Backup_ListarHistorial;
GO
CREATE PROCEDURE dbo.sp_Backup_ListarHistorial
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 50
        bs.backup_set_id       AS idBackup,
        bs.name                AS nombre,
        bs.description         AS descripcion,
        bs.backup_finish_date  AS fecha,
        bs.backup_size         AS tamanioBytes,
        mf.physical_device_name AS ruta
    FROM msdb.dbo.backupset bs
    INNER JOIN msdb.dbo.backupmediafamily mf ON mf.media_set_id = bs.media_set_id
    WHERE bs.database_name = DB_NAME()
      AND bs.type = 'D'
      AND mf.device_type = 2
    ORDER BY bs.backup_finish_date DESC;
END
GO

-- ---------------------------------------------------------------------------
-- Restauración (en master).
-- ---------------------------------------------------------------------------
USE master;
GO

IF OBJECT_ID('dbo.sp_StageUp_RestaurarBackup', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_StageUp_RestaurarBackup;
GO
CREATE PROCEDURE dbo.sp_StageUp_RestaurarBackup
    @nombreBase SYSNAME,
    @rutaBackup NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;

    IF DB_ID(@nombreBase) IS NULL
    BEGIN
        THROW 51022, 'La base de datos indicada no existe en esta instancia.', 1;
    END

    -- Anti-pisado: solo se aceptan archivos que SQL Server tenga registrados
    -- como backup completo de ESA base (no cualquier .bak del disco).
    IF NOT EXISTS
    (
        SELECT 1
        FROM msdb.dbo.backupset bs
        INNER JOIN msdb.dbo.backupmediafamily mf ON mf.media_set_id = bs.media_set_id
        WHERE bs.database_name = @nombreBase
          AND bs.type = 'D'
          AND mf.physical_device_name = @rutaBackup
    )
    BEGIN
        THROW 51023, 'El archivo no corresponde a un backup registrado de esta base.', 1;
    END

    -- Se verifica que el archivo exista y se pueda leer antes de tocar la base.
    RESTORE VERIFYONLY FROM DISK = @rutaBackup;

    DECLARE @sql NVARCHAR(MAX);
    BEGIN TRY
        SET @sql = N'ALTER DATABASE ' + QUOTENAME(@nombreBase) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
        EXEC (@sql);

        RESTORE DATABASE @nombreBase FROM DISK = @rutaBackup WITH REPLACE;

        SET @sql = N'ALTER DATABASE ' + QUOTENAME(@nombreBase) + N' SET MULTI_USER;';
        EXEC (@sql);
    END TRY
    BEGIN CATCH
        IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @nombreBase AND user_access_desc = N'SINGLE_USER')
        BEGIN
            SET @sql = N'ALTER DATABASE ' + QUOTENAME(@nombreBase) + N' SET MULTI_USER;';
            EXEC (@sql);
        END;

        THROW;
    END CATCH
END
GO

USE StageUp;
GO

-- ---------------------------------------------------------------------------
-- Permiso propio GESTIONAR_BACKUP (mismo patrón que GESTIONAR_SOPORTE en el
-- 38) + su opción en el menú dinámico.
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoBackup INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'GESTIONAR_BACKUP'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoBackup IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'GESTIONAR_BACKUP', N'Backup y restauración',
             N'Generar backups de la base de datos y restaurarla desde un backup.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/BackupRestore.aspx', 1);

        SET @idPermisoBackup = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Backup y restauración',
            descripcion = N'Generar backups de la base de datos y restaurarla desde un backup.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/BackupRestore.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoBackup;
    END

    DECLARE @idRolAdministrador INT =
    (
        SELECT TOP 1 idRolInterno
        FROM dbo.RolInterno
        WHERE nombreRol = N'Administrador'
          AND activo = 1
        ORDER BY idRolInterno
    );

    IF @idRolAdministrador IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.RolInternoPermiso
           WHERE idRolInterno = @idRolAdministrador
             AND idPermisoInterno = @idPermisoBackup
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoBackup, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoBackup;
    END

    IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
    BEGIN
        DECLARE @idGrupoAdministracion INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE tipoComponente = N'Grupo'
              AND idComponentePadre IS NULL
              AND nombre = N'Administración'
            ORDER BY idComponentePermiso
        );

        IF @idGrupoAdministracion IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (NULL, N'Grupo', NULL, N'Administración', NULL, NULL, 100, 1);

            SET @idGrupoAdministracion = CAST(SCOPE_IDENTITY() AS INT);
        END

        DECLARE @idComponenteBackup INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'GESTIONAR_BACKUP'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteBackup IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_BACKUP', N'Backup y restauración',
                 N'Generar backups de la base de datos y restaurarla desde un backup.',
                 N'~/Interno/BackupRestore.aspx', 140, 1);

            SET @idComponenteBackup = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Backup y restauración',
                descripcion = N'Generar backups de la base de datos y restaurarla desde un backup.',
                urlAsociada = N'~/Interno/BackupRestore.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteBackup;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteBackup
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteBackup, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteBackup;
            END
        END
    END

    -- Opción del menú dinámico (script 45), al final del menú.
    IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
       AND @idComponenteBackup IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponenteBackup)
    BEGIN
        INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
        SELECT N'Backup y restauración',
               N'Generar backups de la base de datos y restaurarla desde un backup.',
               N'~/Interno/BackupRestore.aspx', N'Administración',
               ISNULL(MAX(orden), 0) + 10,
               @idComponenteBackup, 1
        FROM dbo.OpcionMenu;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    DECLARE @mensajeError NVARCHAR(4000);
    SET @mensajeError = ERROR_MESSAGE();
    RAISERROR(N'%s', 16, 1, @mensajeError);
END CATCH;
GO
