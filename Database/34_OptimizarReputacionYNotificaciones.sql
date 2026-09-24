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

-- ============================================================================
-- 1) Notificacion: marcar leída validando pertenencia en la misma consulta
-- ============================================================================

IF OBJECT_ID('dbo.sp_Notificacion_MarcarLeida', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_MarcarLeida;
GO
CREATE PROCEDURE dbo.sp_Notificacion_MarcarLeida
    @idNotificacion   INT,
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Antes se validaba "es del usuario" trayendo todas sus
        -- notificaciones a memoria (ListarPorUsuario con cantidad =
        -- int.MaxValue) y buscando el id ahí. Si la notificación no es del
        -- usuario, este UPDATE simplemente no afecta filas (mismo
        -- comportamiento silencioso que tenía antes el código de la BLL).
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

-- ============================================================================
-- 2) Reputación de solicitantes: versiones "por lista de ids"
-- ============================================================================

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
    -- Mismo criterio que antes hacía BLL_Reserva en memoria: cuenta, sobre
    -- todo el historial del solicitante (no solo el de un espacio puntual),
    -- las reservas en estado Aceptada o Finalizada.
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
    -- Mismo filtro/orden que sp_Calificacion_ListarRecibidasPorUsuario
    -- (tipoCalificacion = 'UsuarioSolicitante', activo = 1, más recientes
    -- primero), pero resuelto para todos los solicitantes pedidos de una
    -- sola vez y recortado a las 3 más recientes por usuario con
    -- ROW_NUMBER() en vez de traer todo el historial de calificaciones de
    -- cada uno y recortarlo en memoria.
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
