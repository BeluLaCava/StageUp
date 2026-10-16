IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.MensajeReserva', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MensajeReserva
    (
        idMensajeReserva        INT IDENTITY(1,1) NOT NULL,
        idReserva               INT               NOT NULL,
        idUsuarioExternoEmisor  INT               NOT NULL,
        texto                   NVARCHAR(1000)    NOT NULL,
        fechaEnvio              DATETIME          NOT NULL CONSTRAINT DF_MensajeReserva_fechaEnvio DEFAULT (GETDATE()),
        fechaLectura            DATETIME          NULL,
        CONSTRAINT PK_MensajeReserva PRIMARY KEY CLUSTERED (idMensajeReserva),
        CONSTRAINT FK_MensajeReserva_Reserva FOREIGN KEY (idReserva) REFERENCES dbo.Reserva (idReserva),
        CONSTRAINT FK_MensajeReserva_Emisor FOREIGN KEY (idUsuarioExternoEmisor) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT CK_MensajeReserva_texto CHECK (LEN(LTRIM(RTRIM(texto))) > 0)
    );

    CREATE INDEX IX_MensajeReserva_Reserva ON dbo.MensajeReserva (idReserva, fechaEnvio);
END
GO

-- Alta. Segunda barrera (la primera es BLL_MensajeReserva): quien escribe
-- tiene que ser el solicitante o el gestor del espacio, y la reserva no
-- puede estar rechazada ni cancelada (la conversación queda como registro).
IF OBJECT_ID('dbo.sp_MensajeReserva_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MensajeReserva_Insertar;
GO
CREATE PROCEDURE dbo.sp_MensajeReserva_Insertar
    @idReserva               INT,
    @idUsuarioExternoEmisor  INT,
    @texto                   NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @estado NVARCHAR(50);
    SELECT @estado = r.estadoReserva
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.idReserva = @idReserva
      AND (r.idUsuarioExternoSolicitante = @idUsuarioExternoEmisor OR e.idUsuarioGestor = @idUsuarioExternoEmisor);

    IF @estado IS NULL
        THROW 51050, 'El usuario no participa de la reserva.', 1;

    IF @estado IN (N'Rechazada', N'Cancelada')
        THROW 51051, 'La reserva está cerrada para mensajes.', 1;

    INSERT INTO dbo.MensajeReserva (idReserva, idUsuarioExternoEmisor, texto)
    VALUES (@idReserva, @idUsuarioExternoEmisor, @texto);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

-- Conversación de una reserva (vacía si el usuario no participa).
IF OBJECT_ID('dbo.sp_MensajeReserva_ListarPorReserva', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MensajeReserva_ListarPorReserva;
GO
CREATE PROCEDURE dbo.sp_MensajeReserva_ListarPorReserva
    @idReserva          INT,
    @idUsuarioExterno   INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT m.idMensajeReserva, m.idReserva, m.idUsuarioExternoEmisor, m.texto, m.fechaEnvio, m.fechaLectura,
           u.nombre AS nombreEmisor, u.apellido AS apellidoEmisor
    FROM dbo.MensajeReserva m
    INNER JOIN dbo.Reserva r ON r.idReserva = m.idReserva
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = m.idUsuarioExternoEmisor
    WHERE m.idReserva = @idReserva
      AND (r.idUsuarioExternoSolicitante = @idUsuarioExterno OR e.idUsuarioGestor = @idUsuarioExterno)
    ORDER BY m.fechaEnvio, m.idMensajeReserva;
END
GO

-- Marca como leídos los mensajes que recibió el usuario en esa reserva.
IF OBJECT_ID('dbo.sp_MensajeReserva_MarcarLeidos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MensajeReserva_MarcarLeidos;
GO
CREATE PROCEDURE dbo.sp_MensajeReserva_MarcarLeidos
    @idReserva          INT,
    @idUsuarioExterno   INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE m
    SET m.fechaLectura = GETDATE()
    FROM dbo.MensajeReserva m
    INNER JOIN dbo.Reserva r ON r.idReserva = m.idReserva
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE m.idReserva = @idReserva
      AND m.fechaLectura IS NULL
      AND m.idUsuarioExternoEmisor <> @idUsuarioExterno
      AND (r.idUsuarioExternoSolicitante = @idUsuarioExterno OR e.idUsuarioGestor = @idUsuarioExterno);

    SELECT @@ROWCOUNT;
END
GO

-- Mensajes sin leer del usuario, por reserva y según su papel en ella
-- (Solicitante o Gestor), para los listados y el menú lateral.
IF OBJECT_ID('dbo.sp_MensajeReserva_NoLeidosPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MensajeReserva_NoLeidosPorUsuario;
GO
CREATE PROCEDURE dbo.sp_MensajeReserva_NoLeidosPorUsuario
    @idUsuarioExterno   INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT m.idReserva,
           CASE WHEN r.idUsuarioExternoSolicitante = @idUsuarioExterno THEN N'Solicitante' ELSE N'Gestor' END AS rol,
           COUNT(*) AS cantidad
    FROM dbo.MensajeReserva m
    INNER JOIN dbo.Reserva r ON r.idReserva = m.idReserva
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE m.fechaLectura IS NULL
      AND m.idUsuarioExternoEmisor <> @idUsuarioExterno
      AND (r.idUsuarioExternoSolicitante = @idUsuarioExterno OR e.idUsuarioGestor = @idUsuarioExterno)
    GROUP BY m.idReserva, CASE WHEN r.idUsuarioExternoSolicitante = @idUsuarioExterno THEN N'Solicitante' ELSE N'Gestor' END;
END
GO
