USE StageUp;
GO

IF OBJECT_ID('dbo.sp_Reserva_ListarActivasPorEspacio', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Reserva_ListarActivasPorEspacio;
GO

CREATE PROCEDURE dbo.sp_Reserva_ListarActivasPorEspacio
    @idEspacioArtistico INT
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
    WHERE r.idEspacioArtistico = @idEspacioArtistico
      AND r.estadoReserva IN (N'Pendiente', N'Aceptada')
      AND r.fechaSolicitada >= CAST(GETDATE() AS DATE);
END
GO
