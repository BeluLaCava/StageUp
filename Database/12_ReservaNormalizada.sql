IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- (comisión de cancelación)

IF COL_LENGTH('dbo.Reserva', 'minutoDesde') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD minutoDesde SMALLINT NULL;
END
GO

IF COL_LENGTH('dbo.Reserva', 'minutoHasta') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD minutoHasta SMALLINT NULL;
END
GO

IF COL_LENGTH('dbo.Reserva', 'precioHoraPactado') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD precioHoraPactado DECIMAL(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.Reserva', 'moneda') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD moneda NVARCHAR(3) NULL;
END
GO

IF COL_LENGTH('dbo.Reserva', 'importeEstimado') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD importeEstimado DECIMAL(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.Reserva', 'comisionAplicada') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD comisionAplicada BIT NOT NULL CONSTRAINT DF_Reserva_comisionAplicada DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Reserva', 'importeComision') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD importeComision DECIMAL(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.Reserva', 'fechaCancelacion') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD fechaCancelacion DATETIME NULL;
END
GO

-- Restricciones de coherencia: si hay minutoDesde tiene que haber minutoHasta
-- (y viceversa), y minutoHasta tiene que ser posterior a minutoDesde. Igual
-- criterio que ya usan FranjaEspacio y BLL_Reserva.ValidarHorarioSolicitado.
IF OBJECT_ID('dbo.CK_Reserva_horario', 'C') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD CONSTRAINT CK_Reserva_horario CHECK (
        (minutoDesde IS NULL AND minutoHasta IS NULL) OR
        (minutoDesde IS NOT NULL AND minutoHasta IS NOT NULL AND minutoHasta > minutoDesde
            AND minutoDesde >= 0 AND minutoHasta <= 1440)
    );
END
GO

IF OBJECT_ID('dbo.sp_Reserva_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Insertar;
GO
CREATE PROCEDURE dbo.sp_Reserva_Insertar
    @idEspacioArtistico             INT,
    @idUsuarioExternoSolicitante    INT,
    @fechaSolicitada                DATE,
    @comentarioSolicitante          NVARCHAR(1000)  = NULL,
    @minutoDesde                    SMALLINT        = NULL,
    @minutoHasta                    SMALLINT        = NULL,
    @precioHoraPactado              DECIMAL(18,2)   = NULL,
    @moneda                         NVARCHAR(3)     = NULL,
    @importeEstimado                DECIMAL(18,2)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.Reserva
            (idEspacioArtistico, idUsuarioExternoSolicitante, fechaSolicitada, comentarioSolicitante,
             estadoReserva, fechaCreacion, minutoDesde, minutoHasta, precioHoraPactado, moneda, importeEstimado)
        VALUES
            (@idEspacioArtistico, @idUsuarioExternoSolicitante, @fechaSolicitada, @comentarioSolicitante,
             N'Pendiente', GETDATE(), @minutoDesde, @minutoHasta, @precioHoraPactado, @moneda, @importeEstimado);

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
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
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
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
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
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.idReserva = @idReserva;
END
GO

--(ValidarFicha, ValidarHorarioSolicitado).

IF OBJECT_ID('dbo.sp_Reserva_Cancelar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Cancelar;
GO
CREATE PROCEDURE dbo.sp_Reserva_Cancelar
    @idReserva          INT,
    @comisionAplicada   BIT,
    @importeComision    DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Reserva
        SET estadoReserva           = N'Cancelada',
            comisionAplicada        = @comisionAplicada,
            importeComision         = @importeComision,
            fechaCancelacion        = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva
          AND estadoReserva IN (N'Pendiente', N'Aceptada');
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- (evitar reservas superpuestas)

IF OBJECT_ID('dbo.sp_Reserva_ExisteSolapamiento', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ExisteSolapamiento;
GO
CREATE PROCEDURE dbo.sp_Reserva_ExisteSolapamiento
    @idEspacioArtistico     INT,
    @fechaSolicitada        DATE,
    @minutoDesde            SMALLINT,
    @minutoHasta            SMALLINT,
    @idReservaAExcluir      INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(
        CASE WHEN EXISTS (
            SELECT 1
            FROM dbo.Reserva r
            WHERE r.idEspacioArtistico = @idEspacioArtistico
              AND r.fechaSolicitada = @fechaSolicitada
              AND r.estadoReserva IN (N'Pendiente', N'Aceptada')
              AND r.minutoDesde IS NOT NULL
              AND r.minutoHasta IS NOT NULL
              AND @minutoDesde < r.minutoHasta
              AND r.minutoDesde < @minutoHasta
              AND (@idReservaAExcluir IS NULL OR r.idReserva <> @idReservaAExcluir)
        ) THEN 1 ELSE 0 END
    AS BIT) AS existeSolapamiento;
END
GO

-- Acepta una solicitud solo si sigue Pendiente y, si tiene horario cargado,
-- solo si ese horario sigue libre en este mismo momento 

IF OBJECT_ID('dbo.sp_Reserva_AceptarSiDisponible', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_AceptarSiDisponible;
GO
CREATE PROCEDURE dbo.sp_Reserva_AceptarSiDisponible
    @idReserva              INT,
    @comentarioResolucion   NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE r
        SET estadoReserva           = N'Aceptada',
            comentarioResolucion    = @comentarioResolucion,
            fechaResolucion         = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        FROM dbo.Reserva r
        WHERE r.idReserva = @idReserva
          AND r.estadoReserva = N'Pendiente'
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.Reserva otra
              WHERE otra.idEspacioArtistico = r.idEspacioArtistico
                AND otra.idReserva <> r.idReserva
                AND otra.fechaSolicitada = r.fechaSolicitada
                AND otra.estadoReserva = N'Aceptada'
                AND r.minutoDesde IS NOT NULL
                AND r.minutoHasta IS NOT NULL
                AND otra.minutoDesde IS NOT NULL
                AND otra.minutoHasta IS NOT NULL
                AND r.minutoDesde < otra.minutoHasta
                AND otra.minutoDesde < r.minutoHasta
          );

        SELECT CAST(@@ROWCOUNT AS BIT) AS seAcepto;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
