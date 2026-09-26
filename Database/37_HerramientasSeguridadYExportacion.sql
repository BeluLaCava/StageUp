IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoEncriptacion INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'GESTIONAR_ENCRIPTACION'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoEncriptacion IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'GESTIONAR_ENCRIPTACION', N'Herramientas de seguridad',
             N'Generar el par de claves asimétricas y descifrar archivos exportados desde la bitácora.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/HerramientasSeguridad.aspx', 1);

        SET @idPermisoEncriptacion = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Herramientas de seguridad',
            descripcion = N'Generar el par de claves asimétricas y descifrar archivos exportados desde la bitácora.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/HerramientasSeguridad.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoEncriptacion;
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
             AND idPermisoInterno = @idPermisoEncriptacion
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoEncriptacion, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoEncriptacion;
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

        DECLARE @idComponenteEncriptacion INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'GESTIONAR_ENCRIPTACION'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteEncriptacion IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_ENCRIPTACION', N'Herramientas de seguridad',
                 N'Generar el par de claves asimétricas y descifrar archivos exportados desde la bitácora.',
                 N'~/Interno/HerramientasSeguridad.aspx', 110, 1);

            SET @idComponenteEncriptacion = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Herramientas de seguridad',
                descripcion = N'Generar el par de claves asimétricas y descifrar archivos exportados desde la bitácora.',
                urlAsociada = N'~/Interno/HerramientasSeguridad.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteEncriptacion;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteEncriptacion
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteEncriptacion, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteEncriptacion;
            END
        END
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
