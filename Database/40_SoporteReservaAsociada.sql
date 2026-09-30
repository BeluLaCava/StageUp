IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Ticket', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/38_SoporteHelpdesk.sql.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_Crear', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_Crear;
GO
CREATE PROCEDURE dbo.sp_Ticket_Crear
    @idUsuarioExterno   INT,
    @idReservaAsociada  INT = NULL,
    @categoria          NVARCHAR(50),
    @asunto             NVARCHAR(200),
    @mensajeInicial     NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Segunda barrera (la primera es BLL_Ticket): un usuario solo puede
    -- asociar a su ticket una reserva que él mismo solicitó.
    IF @idReservaAsociada IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1 FROM dbo.Reserva r
           WHERE r.idReserva = @idReservaAsociada
             AND r.idUsuarioExternoSolicitante = @idUsuarioExterno
       )
    BEGIN
        THROW 51010, 'La reserva indicada no pertenece al usuario que crea el ticket.', 1;
    END

    BEGIN TRANSACTION;

    INSERT INTO dbo.Ticket (idUsuarioExterno, idReservaAsociada, categoria, asunto, estado, fechaCreacion, fechaUltimaActividad)
    VALUES (@idUsuarioExterno, @idReservaAsociada, @categoria, @asunto, N'Abierto', GETDATE(), GETDATE());

    DECLARE @idTicket INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.TicketMensaje (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio)
    VALUES (@idTicket, @idUsuarioExterno, NULL, @mensajeInicial, GETDATE());

    COMMIT TRANSACTION;

    SELECT @idTicket AS idTicket;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Ticket_ObtenerPorId
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTicket, t.idUsuarioExterno, t.idReservaAsociada, t.categoria, t.asunto, t.estado,
        t.idUsuarioInternoAsignado, t.fechaCreacion, t.fechaUltimaActividad, t.fechaCierre,
        ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno,
        ue.correoElectronico AS correoUsuarioExterno,
        ui.nombre + N' ' + ui.apellido AS nombreUsuarioInternoAsignado,
        -- Contexto de la reserva asociada (si la hay), para que soporte
        -- responda sabiendo de qué servicio contratado se trata.
        ea.nombreEspacio AS nombreEspacioReserva,
        r.fechaSolicitada AS fechaReserva,
        r.minutoDesde AS minutoDesdeReserva,
        r.minutoHasta AS minutoHastaReserva,
        r.estadoReserva AS estadoReserva
    FROM dbo.Ticket t
    INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
    LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
    LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico
    WHERE t.idTicket = @idTicket;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ListarPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ListarPorUsuario;
GO
CREATE PROCEDURE dbo.sp_Ticket_ListarPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTicket, t.idUsuarioExterno, t.idReservaAsociada, t.categoria, t.asunto, t.estado,
        t.idUsuarioInternoAsignado, t.fechaCreacion, t.fechaUltimaActividad, t.fechaCierre,
        ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno,
        ue.correoElectronico AS correoUsuarioExterno,
        ui.nombre + N' ' + ui.apellido AS nombreUsuarioInternoAsignado,
        ea.nombreEspacio AS nombreEspacioReserva,
        r.fechaSolicitada AS fechaReserva,
        r.minutoDesde AS minutoDesdeReserva,
        r.minutoHasta AS minutoHastaReserva,
        r.estadoReserva AS estadoReserva
    FROM dbo.Ticket t
    INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
    LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
    LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico
    WHERE t.idUsuarioExterno = @idUsuarioExterno
    ORDER BY t.fechaUltimaActividad DESC;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ListarParaInterno', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ListarParaInterno;
GO
CREATE PROCEDURE dbo.sp_Ticket_ListarParaInterno
    @estado     NVARCHAR(20) = NULL,
    @categoria  NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTicket, t.idUsuarioExterno, t.idReservaAsociada, t.categoria, t.asunto, t.estado,
        t.idUsuarioInternoAsignado, t.fechaCreacion, t.fechaUltimaActividad, t.fechaCierre,
        ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno,
        ue.correoElectronico AS correoUsuarioExterno,
        ui.nombre + N' ' + ui.apellido AS nombreUsuarioInternoAsignado,
        ea.nombreEspacio AS nombreEspacioReserva,
        r.fechaSolicitada AS fechaReserva,
        r.minutoDesde AS minutoDesdeReserva,
        r.minutoHasta AS minutoHastaReserva,
        r.estadoReserva AS estadoReserva
    FROM dbo.Ticket t
    INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
    LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
    LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico
    WHERE (@estado IS NULL OR t.estado = @estado)
      AND (@categoria IS NULL OR t.categoria = @categoria)
    ORDER BY t.fechaUltimaActividad DESC;
END
GO

-- Indicador para el equipo interno: tickets en estado Abierto (recién
-- creados, o en los que el usuario volvió a escribir después de una
-- respuesta de soporte). Se muestra como contador en el menú del panel.
IF OBJECT_ID('dbo.sp_Ticket_ContarAbiertos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ContarAbiertos;
GO
CREATE PROCEDURE dbo.sp_Ticket_ContarAbiertos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS cantidad
    FROM dbo.Ticket
    WHERE estado = N'Abierto';
END
GO
