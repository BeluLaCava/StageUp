IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Notificacion', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/26_ActividadesYNotificaciones.sql.', 1;
END
GO

IF OBJECT_ID(N'dbo.vw_CalificacionDetalle', N'V') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/21_CalificacionesYReputacion.sql.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Notificacion_MarcarLeida', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_MarcarLeida;
GO
CREATE PROCEDURE dbo.sp_Notificacion_MarcarLeida
    @idNotificacion   INT,
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY

        UPDATE dbo.Notificacion
        SET leida = 1
        WHERE idNotificacion = @idNotificacion
          AND idUsuarioExterno = @idUsuarioExterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_ListarPorIds', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ListarPorIds;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ListarPorIds
    @idsUsuarios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*
    FROM dbo.UsuarioExterno u
    INNER JOIN STRING_SPLIT(@idsUsuarios, ',') s ON TRY_CONVERT(INT, s.value) = u.idUsuarioExterno;
END
GO

IF OBJECT_ID('dbo.sp_Reserva_ContarAceptadasPorSolicitantes', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ContarAceptadasPorSolicitantes;
GO
CREATE PROCEDURE dbo.sp_Reserva_ContarAceptadasPorSolicitantes
    @idsUsuarios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.idUsuarioExternoSolicitante, COUNT(*) AS cantidadAceptadas
    FROM dbo.Reserva r
    INNER JOIN STRING_SPLIT(@idsUsuarios, ',') s ON TRY_CONVERT(INT, s.value) = r.idUsuarioExternoSolicitante
    WHERE r.estadoReserva IN (N'Aceptada', N'Finalizada')
    GROUP BY r.idUsuarioExternoSolicitante;
END
GO

IF OBJECT_ID('dbo.sp_Calificacion_ListarRecibidasTop3PorUsuarios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarRecibidasTop3PorUsuarios;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarRecibidasTop3PorUsuarios
    @idsUsuarios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Numeradas AS (
        SELECT
            v.*,
            ROW_NUMBER() OVER (PARTITION BY v.idUsuarioEvaluado ORDER BY v.fechaAlta DESC) AS nroOrden
        FROM dbo.vw_CalificacionDetalle v
        INNER JOIN STRING_SPLIT(@idsUsuarios, ',') s ON TRY_CONVERT(INT, s.value) = v.idUsuarioEvaluado
        WHERE v.tipoCalificacion = N'UsuarioSolicitante'
          AND v.activo = 1
    )
    SELECT *
    FROM Numeradas
    WHERE nroOrden <= 3
    ORDER BY idUsuarioEvaluado, fechaAlta DESC;
END
GO