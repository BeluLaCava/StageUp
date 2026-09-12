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


-- Milestone B del Composite de permisos: alta, movimiento y baja de grupos,
-- para la pantalla nueva de organización de permisos.

IF OBJECT_ID('dbo.sp_ComponentePermiso_InsertarGrupo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ComponentePermiso_InsertarGrupo;
GO
CREATE PROCEDURE dbo.sp_ComponentePermiso_InsertarGrupo
    @idComponentePadre  INT = NULL,
    @nombre             NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @idComponentePadre IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.ComponentePermiso
            WHERE idComponentePermiso = @idComponentePadre AND tipoComponente = N'Grupo' AND activo = 1)
        BEGIN
            THROW 51001, 'El grupo padre indicado no existe.', 1;
        END

        INSERT INTO dbo.ComponentePermiso (idComponentePadre, tipoComponente, codigoPermiso, nombre, orden, activo)
        VALUES (@idComponentePadre, N'Grupo', NULL, @nombre, 0, 1);

        SELECT SCOPE_IDENTITY() AS idComponentePermiso;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_ComponentePermiso_MoverComponente', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ComponentePermiso_MoverComponente;
GO
CREATE PROCEDURE dbo.sp_ComponentePermiso_MoverComponente
    @idComponentePermiso        INT,
    @idNuevoComponentePadre     INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @idNuevoComponentePadre IS NOT NULL
        BEGIN
            IF @idNuevoComponentePadre = @idComponentePermiso
            BEGIN
                THROW 51002, 'Un grupo no puede ser padre de sí mismo.', 1;
            END

            IF NOT EXISTS (
                SELECT 1 FROM dbo.ComponentePermiso
                WHERE idComponentePermiso = @idNuevoComponentePadre AND tipoComponente = N'Grupo' AND activo = 1)
            BEGIN
                THROW 51003, 'El grupo destino indicado no existe.', 1;
            END

            DECLARE @esDescendiente BIT = 0;

            ;WITH Descendientes AS (
                SELECT idComponentePermiso FROM dbo.ComponentePermiso WHERE idComponentePadre = @idComponentePermiso
                UNION ALL
                SELECT cp.idComponentePermiso
                FROM dbo.ComponentePermiso cp
                INNER JOIN Descendientes d ON cp.idComponentePadre = d.idComponentePermiso
            )
            SELECT @esDescendiente = 1
            FROM Descendientes
            WHERE idComponentePermiso = @idNuevoComponentePadre;

            IF @esDescendiente = 1
            BEGIN
                THROW 51004, 'No se puede mover un grupo dentro de uno de sus propios subgrupos.', 1;
            END
        END

        UPDATE dbo.ComponentePermiso
        SET idComponentePadre = @idNuevoComponentePadre,
            fechaUltimaModificacion = GETDATE()
        WHERE idComponentePermiso = @idComponentePermiso;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_ComponentePermiso_EliminarGrupo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ComponentePermiso_EliminarGrupo;
GO
CREATE PROCEDURE dbo.sp_ComponentePermiso_EliminarGrupo
    @idComponentePermiso INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (
            SELECT 1 FROM dbo.ComponentePermiso
            WHERE idComponentePermiso = @idComponentePermiso AND tipoComponente = N'Grupo' AND activo = 1)
        BEGIN
            THROW 51005, 'El grupo indicado no existe.', 1;
        END

        IF EXISTS (SELECT 1 FROM dbo.ComponentePermiso WHERE idComponentePadre = @idComponentePermiso AND activo = 1)
        BEGIN
            THROW 51006, 'El grupo tiene elementos adentro: movelos o eliminalos antes de borrar el grupo.', 1;
        END

        IF EXISTS (SELECT 1 FROM dbo.RolInternoComponentePermiso WHERE idComponentePermiso = @idComponentePermiso AND activo = 1)
        BEGIN
            THROW 51007, 'El grupo está asignado a uno o más roles: quitalo de esos roles antes de borrarlo.', 1;
        END

        UPDATE dbo.ComponentePermiso
        SET activo = 0, fechaUltimaModificacion = GETDATE()
        WHERE idComponentePermiso = @idComponentePermiso;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
