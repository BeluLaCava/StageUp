USE [StageUp];
GO
CREATE OR ALTER PROCEDURE dbo.sp_SolicitudHabilitacion_Resolver
    @idSolicitud INT,
    @aprobar BIT,
    @motivoRechazo NVARCHAR(500) = NULL,
    @idUsuarioInternoRevisor INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @idUsuarioExterno INT, @estado NVARCHAR(20), @estadoCuenta NVARCHAR(100), @activo BIT;
        SELECT @idUsuarioExterno = idUsuarioExterno
        FROM dbo.SolicitudHabilitacionGestor WHERE idSolicitud = @idSolicitud;
        SELECT @estadoCuenta = estadoCuenta, @activo = activo
        FROM dbo.UsuarioExterno WITH (UPDLOCK, HOLDLOCK)
        WHERE idUsuarioExterno = @idUsuarioExterno;
        SELECT @estado = estado
        FROM dbo.SolicitudHabilitacionGestor WITH (UPDLOCK, HOLDLOCK)
        WHERE idSolicitud = @idSolicitud AND idUsuarioExterno = @idUsuarioExterno;

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @idUsuarioExterno IS NULL OR @estado IS NULL
            SET @resultado = N'NO_EXISTE';
        ELSE IF @estado <> N'PendienteRevision'
            SET @resultado = N'YA_RESUELTA';
        ELSE IF ISNULL(@activo, 0) = 0 OR ISNULL(@estadoCuenta, N'') <> N'Activa'
            SET @resultado = N'CUENTA_NO_ACTIVA';

        IF @resultado = N'OK'
        BEGIN
            UPDATE dbo.SolicitudHabilitacionGestor
            SET estado = CASE WHEN @aprobar = 1 THEN N'Aprobada' ELSE N'Rechazada' END,
                fechaRevision = GETDATE(), idUsuarioInternoRevisor = @idUsuarioInternoRevisor,
                motivoRechazo = CASE WHEN @aprobar = 1 THEN NULL ELSE @motivoRechazo END
            WHERE idSolicitud = @idSolicitud;
            UPDATE dbo.UsuarioExterno
            SET perfilUsuario = CASE WHEN @aprobar = 1 THEN N'GestorEspacios' ELSE N'ExternoSolicitante' END,
                fechaUltimaModificacion = GETDATE()
            WHERE idUsuarioExterno = @idUsuarioExterno AND perfilUsuario <> N'GestorEspacios';
        END;
        COMMIT TRANSACTION;
        SELECT @resultado AS resultado, @idUsuarioExterno AS idUsuarioExterno, @estado AS estadoAnterior;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
