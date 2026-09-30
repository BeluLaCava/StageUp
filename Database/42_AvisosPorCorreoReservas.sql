IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Reserva', N'U') IS NULL
   OR COL_LENGTH(N'dbo.Reserva', N'recordatorioEnviado') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/28_RecordatorioReservasYHabilitacionGestor.sql.', 1;
END
GO

IF COL_LENGTH(N'dbo.Reserva', N'avisoFinalizacionEnviado') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva
        ADD avisoFinalizacionEnviado BIT NOT NULL
            CONSTRAINT DF_Reserva_avisoFinalizacionEnviado DEFAULT (0);

    -- Dinámico porque la columna recién se agregó en este mismo lote.
    EXEC (N'UPDATE dbo.Reserva SET avisoFinalizacionEnviado = 1 WHERE estadoReserva = N''Finalizada'';');
END
GO

IF OBJECT_ID('dbo.sp_Reserva_ListarFinalizadasSinAviso', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Reserva_ListarFinalizadasSinAviso;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarFinalizadasSinAviso
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.estadoReserva = N'Finalizada'
      AND r.avisoFinalizacionEnviado = 0;
END
GO

IF OBJECT_ID('dbo.sp_Reserva_MarcarAvisoFinalizacionEnviado', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Reserva_MarcarAvisoFinalizacionEnviado;
GO
CREATE PROCEDURE dbo.sp_Reserva_MarcarAvisoFinalizacionEnviado
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Reserva SET avisoFinalizacionEnviado = 1 WHERE idReserva = @idReserva;
END
GO

-- El recordatorio de 24 hs ahora también sale por mail: se agregan al listado
-- los datos de importe que usa la plantilla (mismas columnas que antes + 3).
IF OBJECT_ID('dbo.sp_Reserva_ListarPendientesDeRecordatorio', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Reserva_ListarPendientesDeRecordatorio;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPendientesDeRecordatorio
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ahora DATETIME = GETDATE();
    DECLARE @limite DATETIME = DATEADD(HOUR, 24, @ahora);

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion,
        r.minutoDesde, r.minutoHasta, r.recordatorioEnviado,
        r.precioHoraPactado, r.moneda, r.importeEstimado,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.estadoReserva = N'Aceptada'
      AND r.recordatorioEnviado = 0
      AND DATEADD(MINUTE, ISNULL(r.minutoDesde, 0), CAST(r.fechaSolicitada AS DATETIME)) BETWEEN @ahora AND @limite;
END
GO
