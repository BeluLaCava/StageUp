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

-- ---------------------------------------------------------------------------
-- CU-001-011: cada notificación tiene título además de la descripción.
-- Las que ya existían toman el título según su tipo.
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Notificacion', N'titulo') IS NULL
BEGIN
    ALTER TABLE dbo.Notificacion ADD titulo NVARCHAR(150) NULL;
END
GO

-- Títulos de las notificaciones que ya existían, según su tipo. Va como SQL
-- dinámico para que se compile recién cuando la columna ya existe.
IF COL_LENGTH(N'dbo.Notificacion', N'titulo') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
    UPDATE n
    SET n.titulo = m.titulo
    FROM dbo.Notificacion n
    INNER JOIN (VALUES
        (N''SolicitudReserva'', N''Nueva solicitud de reserva''),
        (N''ReservaAceptada'', N''Reserva aceptada''),
        (N''ReservaRechazada'', N''Reserva rechazada''),
        (N''ReservaCancelada'', N''Reserva cancelada''),
        (N''RecordatorioReserva'', N''Recordatorio de reserva''),
        (N''HabilitacionGestorAprobada'', N''Habilitación como gestor aprobada''),
        (N''HabilitacionGestorRechazada'', N''Habilitación como gestor rechazada''),
        (N''RespuestaTicket'', N''Respuesta de soporte''),
        (N''PagoAprobado'', N''Pago aprobado''),
        (N''PagoRecibido'', N''Pago recibido''),
        (N''PagoVencido'', N''Plazo de pago vencido''),
        (N''ComprobanteEmitido'', N''Comprobante emitido''),
        (N''LiquidacionRegistrada'', N''Liquidación registrada'')
    ) AS m (tipo, titulo) ON m.tipo = n.tipo
    WHERE n.titulo IS NULL;

    UPDATE dbo.Notificacion
    SET titulo = N''Aviso de StageUp''
    WHERE titulo IS NULL;';
END
GO

IF OBJECT_ID('dbo.sp_Notificacion_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_Insertar;
GO
CREATE PROCEDURE dbo.sp_Notificacion_Insertar
    @idUsuarioExterno INT,
    @tipo             NVARCHAR(50),
    @mensaje          NVARCHAR(300),
    @urlDestino       NVARCHAR(300) = NULL,
    @titulo           NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.Notificacion (idUsuarioExterno, tipo, titulo, mensaje, urlDestino)
        VALUES (@idUsuarioExterno, @tipo, @titulo, @mensaje, @urlDestino);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Notificacion_ListarPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_ListarPorUsuario;
GO
CREATE PROCEDURE dbo.sp_Notificacion_ListarPorUsuario
    @idUsuarioExterno INT,
    @cantidad         INT = 20,
    @estado           NVARCHAR(20) = N'Todas'   -- Todas | NoLeidas | Leidas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@cantidad) idNotificacion, idUsuarioExterno, tipo, titulo, mensaje, urlDestino, leida, fechaCreacion
    FROM dbo.Notificacion
    WHERE idUsuarioExterno = @idUsuarioExterno
      AND (@estado = N'Todas'
           OR (@estado = N'NoLeidas' AND leida = 0)
           OR (@estado = N'Leidas' AND leida = 1))
    ORDER BY fechaCreacion DESC, idNotificacion DESC;
END
GO

-- Solo devuelve la notificación si es del usuario indicado.
IF OBJECT_ID('dbo.sp_Notificacion_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Notificacion_ObtenerPorId
    @idNotificacion   INT,
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idNotificacion, idUsuarioExterno, tipo, titulo, mensaje, urlDestino, leida, fechaCreacion
    FROM dbo.Notificacion
    WHERE idNotificacion = @idNotificacion
      AND idUsuarioExterno = @idUsuarioExterno;
END
GO
