IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.sp_Reserva_ListarPorSolicitante', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPorSolicitante;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPorSolicitante
    @idUsuarioExternoSolicitante INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        e.nombreEspacio, e.idUsuarioGestor,
        g.nombre + N' ' + g.apellido AS nombreGestor,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'Espacio'
        ) THEN 1 ELSE 0 END) AS calificacionEspacioRealizada,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'UsuarioSolicitante'
        ) THEN 1 ELSE 0 END) AS calificacionSolicitanteRealizada
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE r.idUsuarioExternoSolicitante = @idUsuarioExternoSolicitante
    ORDER BY r.fechaCreacion DESC;
END
GO
