IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Calificacion', N'U') IS NULL
   OR COL_LENGTH(N'dbo.Reserva', N'avisoFinalizacionEnviado') IS NULL
BEGIN
    THROW 51000, 'Primero deben ejecutarse Database/21_CalificacionesYReputacion.sql y Database/42_AvisosPorCorreoReservas.sql.', 1;
END
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @idAutor INT =
(
    SELECT idUsuarioExterno FROM dbo.UsuarioExterno
    WHERE correoElectronico = N'ana.solicitante@stageup.test'
);

IF @idAutor IS NULL
BEGIN
    PRINT N'No existe ana.solicitante@stageup.test (script 05): no se cargan reseñas demo.';
    RETURN;
END

IF EXISTS (SELECT 1 FROM dbo.Reserva WHERE comentarioSolicitante LIKE N'Demo ranking:%')
BEGIN
    PRINT N'Las reseñas demo del ranking ya estaban cargadas.';
    RETURN;
END

DECLARE @resenas TABLE
(
    orden          INT IDENTITY(1,1),
    nombreEspacio  NVARCHAR(300),
    puntaje        TINYINT,
    comentario     NVARCHAR(1000),
    diasAtras      INT
);

INSERT INTO @resenas (nombreEspacio, puntaje, comentario, diasAtras) VALUES
    (N'Sala Principal StageUp', 5, N'Excelente sala, muy buena acústica y el gestor súper atento.', 40),
    (N'Sala Principal StageUp', 5, N'Todo impecable, volvería a reservar sin dudarlo.', 32),
    (N'Sala Principal StageUp', 4, N'Muy buena, solo le faltaba un poco más de ventilación.', 24),
    (N'Sala Principal StageUp', 5, N'Ideal para ensayos generales, espacio amplio y cómodo.', 16),
    (N'Estudio Creativo Palermo', 5, N'Luz natural hermosa para la sesión de fotos.', 38),
    (N'Estudio Creativo Palermo', 4, N'Buen estudio, bien equipado.', 27),
    (N'Estudio Creativo Palermo', 4, N'Cómodo y bien ubicado, el acceso es fácil.', 12),
    (N'Teatro San Telmo', 4, N'Lindo teatro, buena iluminación.', 45),
    (N'Teatro San Telmo', 4, N'Cumplió con todo lo que necesitábamos para la función.', 30),
    (N'Teatro San Telmo', 5, N'Hermosa sala y excelente atención.', 20),
    (N'Teatro San Telmo', 3, N'Correcto, aunque el sonido podría mejorar.', 9),
    (N'Sala Microescena Boedo', 5, N'Muy linda experiencia.', 14);

BEGIN TRANSACTION;

DECLARE @i INT = 1, @total INT = (SELECT COUNT(*) FROM @resenas);
DECLARE @nombreEspacio NVARCHAR(300), @puntaje TINYINT, @comentario NVARCHAR(1000), @diasAtras INT;
DECLARE @idEspacio INT, @fechaReserva DATE, @idReserva INT;

WHILE @i <= @total
BEGIN
    SELECT @nombreEspacio = nombreEspacio, @puntaje = puntaje, @comentario = comentario, @diasAtras = diasAtras
    FROM @resenas WHERE orden = @i;

    SET @idEspacio =
    (
        SELECT TOP 1 idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE nombreEspacio = @nombreEspacio AND idUsuarioGestor <> @idAutor
        ORDER BY idEspacioArtistico
    );

    IF @idEspacio IS NOT NULL
    BEGIN
        SET @fechaReserva = DATEADD(DAY, -@diasAtras, CAST(GETDATE() AS DATE));

        INSERT INTO dbo.Reserva
            (idEspacioArtistico, idUsuarioExternoSolicitante, fechaSolicitada, comentarioSolicitante,
             estadoReserva, comentarioResolucion, fechaCreacion, fechaResolucion, fechaFinalizacion,
             minutoDesde, minutoHasta, recordatorioEnviado, avisoFinalizacionEnviado)
        VALUES
            (@idEspacio, @idAutor, @fechaReserva, N'Demo ranking: reserva de ejemplo para reseñas.',
             N'Finalizada', N'Reserva confirmada.',
             DATEADD(DAY, -3, CAST(@fechaReserva AS DATETIME)), DATEADD(DAY, -2, CAST(@fechaReserva AS DATETIME)),
             DATEADD(HOUR, 20, CAST(@fechaReserva AS DATETIME)),
             1080, 1200, 1, 1);

        SET @idReserva = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO dbo.Calificacion (idReserva, idUsuarioAutor, tipoCalificacion, puntaje, comentario, fechaAlta)
        VALUES (@idReserva, @idAutor, N'Espacio', @puntaje, @comentario,
                DATEADD(DAY, 1, CAST(@fechaReserva AS DATETIME)));
    END

    SET @i = @i + 1;
END

COMMIT TRANSACTION;
GO
