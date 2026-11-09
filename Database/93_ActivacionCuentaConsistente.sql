USE [StageUp];
GO
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CodigoActivacion_Insertar
    @idUsuarioExterno INT,
    @codigo NVARCHAR(40),
    @fechaVencimiento DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WITH (UPDLOCK, HOLDLOCK)
            WHERE idUsuarioExterno = @idUsuarioExterno AND activo = 1 AND estadoCuenta = N'PendienteActivacion')
            THROW 51000, 'La cuenta no está pendiente de activación.', 1;
        UPDATE dbo.CodigoActivacion
        SET utilizado = 1, fechaUtilizacion = GETDATE()
        WHERE idUsuarioExterno = @idUsuarioExterno AND utilizado = 0;
        INSERT INTO dbo.CodigoActivacion(idUsuarioExterno, codigo, fechaGeneracion, fechaVencimiento, utilizado)
        VALUES(@idUsuarioExterno, @codigo, GETDATE(), @fechaVencimiento, 0);
        DECLARE @id INT = CONVERT(INT, SCOPE_IDENTITY());
        COMMIT TRANSACTION;
        SELECT @id AS idCodigoActivacion;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CodigoActivacion_ObtenerVigentePorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.* FROM dbo.CodigoActivacion c
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = c.idUsuarioExterno
    WHERE c.idUsuarioExterno = @idUsuarioExterno AND c.utilizado = 0
      AND u.activo = 1 AND u.estadoCuenta = N'PendienteActivacion'
      AND c.idCodigoActivacion = (SELECT MAX(idCodigoActivacion) FROM dbo.CodigoActivacion WHERE idUsuarioExterno = @idUsuarioExterno);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CodigoActivacion_MarcarUtilizado
    @idCodigoActivacion INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @idUsuario INT = (SELECT idUsuarioExterno FROM dbo.CodigoActivacion WHERE idCodigoActivacion = @idCodigoActivacion);
        IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WITH (UPDLOCK, HOLDLOCK)
            WHERE idUsuarioExterno = @idUsuario AND activo = 1 AND estadoCuenta = N'PendienteActivacion')
            THROW 51000, 'La cuenta no está pendiente de activación.', 1;
        UPDATE dbo.CodigoActivacion
        SET utilizado = 1, fechaUtilizacion = GETDATE()
        WHERE idCodigoActivacion = @idCodigoActivacion AND utilizado = 0 AND fechaVencimiento > GETDATE()
          AND idCodigoActivacion = (SELECT MAX(idCodigoActivacion) FROM dbo.CodigoActivacion WHERE idUsuarioExterno = @idUsuario);
        IF @@ROWCOUNT <> 1 THROW 51000, 'El código ya no está vigente. Solicitá uno nuevo.', 1;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_UsuarioExterno_ActivarCuenta
    @idUsuarioExterno INT,
    @estadoCuenta NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF @estadoCuenta <> N'Activa' OR @estadoCuenta IS NULL
            THROW 51000, 'El estado de activación no es válido.', 1;
        UPDATE dbo.UsuarioExterno
        SET estadoCuenta = N'Activa', fechaActivacion = GETDATE(), fechaUltimaModificacion = GETDATE()
        WHERE idUsuarioExterno = @idUsuarioExterno AND activo = 1 AND estadoCuenta = N'PendienteActivacion';
        IF @@ROWCOUNT <> 1 THROW 51000, 'La cuenta no está pendiente de activación.', 1;
        UPDATE dbo.CodigoActivacion SET utilizado = 1, fechaUtilizacion = GETDATE()
        WHERE idUsuarioExterno = @idUsuarioExterno AND utilizado = 0;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
UPDATE c SET utilizado = 1, fechaUtilizacion = GETDATE()
FROM dbo.CodigoActivacion c
INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = c.idUsuarioExterno
WHERE c.utilizado = 0 AND (u.activo = 0 OR u.estadoCuenta <> N'PendienteActivacion'
    OR c.idCodigoActivacion <> (SELECT MAX(ultimo.idCodigoActivacion) FROM dbo.CodigoActivacion ultimo WHERE ultimo.idUsuarioExterno = c.idUsuarioExterno));
GO
