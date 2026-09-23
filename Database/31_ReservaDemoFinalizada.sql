IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

DECLARE @idEspacioDemo INT;
DECLARE @idSolicitanteDemo INT;

SELECT @idEspacioDemo = idEspacioArtistico
FROM dbo.EspacioArtistico
WHERE nombreEspacio = N'Sala Principal StageUp';

SELECT @idSolicitanteDemo = idUsuarioExterno
FROM dbo.UsuarioExterno
WHERE correoElectronico = N'ana.solicitante@stageup.test';

IF @idEspacioDemo IS NOT NULL AND @idSolicitanteDemo IS NOT NULL
BEGIN
    DECLARE @fechaSolicitada DATE = DATEADD(DAY, -3, CAST(GETDATE() AS DATE));
    DECLARE @fechaCreacion DATETIME = DATEADD(DAY, -4, GETDATE());
    DECLARE @fechaResolucion DATETIME = DATEADD(DAY, -2, GETDATE());
    DECLARE @idReservaDemo INT;

    SELECT TOP 1 @idReservaDemo = idReserva
    FROM dbo.Reserva
    WHERE idEspacioArtistico = @idEspacioDemo
      AND idUsuarioExternoSolicitante = @idSolicitanteDemo
      AND estadoReserva = N'Finalizada'
    ORDER BY idReserva ASC;

    IF @idReservaDemo IS NULL
    BEGIN
        INSERT INTO dbo.Reserva
            (idEspacioArtistico, idUsuarioExternoSolicitante, fechaSolicitada, comentarioSolicitante,
             estadoReserva, comentarioResolucion, fechaCreacion, fechaResolucion,
             minutoDesde, minutoHasta, precioHoraPactado, moneda, importeEstimado)
        VALUES
            (@idEspacioDemo, @idSolicitanteDemo, @fechaSolicitada,
             N'Necesitamos el espacio para el ensayo general de la muestra de fin de cuatrimestre.',
             N'Finalizada',
             N'Reserva confirmada y espacio entregado según lo solicitado. ¡Gracias por elegirnos!',
             @fechaCreacion, @fechaResolucion,
             1080, 1200, 95000.00, N'ARS', 190000.00);
    END
    ELSE
    BEGIN
        UPDATE dbo.Reserva
        SET fechaSolicitada = @fechaSolicitada,
            fechaCreacion = @fechaCreacion,
            fechaResolucion = @fechaResolucion,
            fechaUltimaModificacion = GETDATE(),
            estadoReserva = N'Finalizada'
        WHERE idReserva = @idReservaDemo;
    END
END
GO
