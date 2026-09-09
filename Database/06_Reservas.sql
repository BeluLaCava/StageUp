IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- Core del negocio (CU-001-005), versión simplificada para esta entrega: un usuario
-- autenticado solicita una fecha para un espacio publicado, el gestor del espacio la
-- acepta o la rechaza. Sin franjas horarias, sin disponibilidad configurable y sin pago
-- (queda para el Avance 2, junto con las calificaciones que dependen de la reserva).
IF OBJECT_ID('dbo.Reserva', 'U') IS NOT NULL DROP TABLE dbo.Reserva;
GO
CREATE TABLE dbo.Reserva
(
    idReserva                       INT IDENTITY(1,1)  NOT NULL,
    idEspacioArtistico               INT                NOT NULL,
    idUsuarioExternoSolicitante      INT                NOT NULL,
    fechaSolicitada                  DATE               NOT NULL,
    comentarioSolicitante            NVARCHAR(1000)     NULL,
    estadoReserva                    NVARCHAR(50)       NOT NULL CONSTRAINT DF_Reserva_estado DEFAULT (N'Pendiente'),
    comentarioResolucion             NVARCHAR(1000)     NULL,
    fechaCreacion                    DATETIME           NOT NULL CONSTRAINT DF_Reserva_fechaCreacion DEFAULT (GETDATE()),
    fechaResolucion                  DATETIME           NULL,
    fechaUltimaModificacion          DATETIME           NULL,
    CONSTRAINT PK_Reserva PRIMARY KEY CLUSTERED (idReserva),
    CONSTRAINT FK_Reserva_EspacioArtistico FOREIGN KEY (idEspacioArtistico)
        REFERENCES dbo.EspacioArtistico (idEspacioArtistico),
    CONSTRAINT FK_Reserva_UsuarioExterno FOREIGN KEY (idUsuarioExternoSolicitante)
        REFERENCES dbo.UsuarioExterno (idUsuarioExterno)
);
GO

IF OBJECT_ID('dbo.sp_Reserva_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Insertar;
GO
CREATE PROCEDURE dbo.sp_Reserva_Insertar
    @idEspacioArtistico             INT,
    @idUsuarioExternoSolicitante    INT,
    @fechaSolicitada                DATE,
    @comentarioSolicitante          NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.Reserva
            (idEspacioArtistico, idUsuarioExternoSolicitante, fechaSolicitada, comentarioSolicitante, estadoReserva, fechaCreacion)
        VALUES
            (@idEspacioArtistico, @idUsuarioExternoSolicitante, @fechaSolicitada, @comentarioSolicitante, N'Pendiente', GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS idReserva;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Reserva_ListarPorSolicitante', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPorSolicitante;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPorSolicitante
    @idUsuarioExternoSolicitante INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.idUsuarioExternoSolicitante = @idUsuarioExternoSolicitante
    ORDER BY r.fechaCreacion DESC;
END
GO

IF OBJECT_ID('dbo.sp_Reserva_ListarPorGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPorGestor;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPorGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion,
        e.nombreEspacio, e.idUsuarioGestor,
        u.nombre + ' ' + u.apellido AS nombreSolicitante, u.correoElectronico AS correoSolicitante
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = r.idUsuarioExternoSolicitante
    WHERE e.idUsuarioGestor = @idUsuarioGestor
    ORDER BY
        CASE WHEN r.estadoReserva = N'Pendiente' THEN 0 ELSE 1 END,
        r.fechaCreacion DESC;
END
GO

IF OBJECT_ID('dbo.sp_Reserva_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Reserva_ObtenerPorId
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.idReserva = @idReserva;
END
GO

IF OBJECT_ID('dbo.sp_Reserva_Resolver', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Resolver;
GO
CREATE PROCEDURE dbo.sp_Reserva_Resolver
    @idReserva              INT,
    @estadoReserva          NVARCHAR(50),
    @comentarioResolucion   NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Reserva
        SET estadoReserva           = @estadoReserva,
            comentarioResolucion    = @comentarioResolucion,
            fechaResolucion         = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva
          AND estadoReserva = N'Pendiente';
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Reserva_Cancelar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Cancelar;
GO
CREATE PROCEDURE dbo.sp_Reserva_Cancelar
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Reserva
        SET estadoReserva           = N'Cancelada',
            fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva
          AND estadoReserva = N'Pendiente';
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
