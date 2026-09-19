USE StageUp;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reserva') AND name = 'recordatorioEnviado')
BEGIN
    ALTER TABLE dbo.Reserva ADD recordatorioEnviado BIT NOT NULL CONSTRAINT DF_Reserva_recordatorioEnviado DEFAULT (0);
END
GO

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
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.estadoReserva = N'Aceptada'
      AND r.recordatorioEnviado = 0
      AND DATEADD(MINUTE, ISNULL(r.minutoDesde, 0), CAST(r.fechaSolicitada AS DATETIME)) BETWEEN @ahora AND @limite;
END
GO

IF OBJECT_ID('dbo.sp_Reserva_MarcarRecordatorioEnviado', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Reserva_MarcarRecordatorioEnviado;
GO

CREATE PROCEDURE dbo.sp_Reserva_MarcarRecordatorioEnviado
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Reserva SET recordatorioEnviado = 1 WHERE idReserva = @idReserva;
END
GO
