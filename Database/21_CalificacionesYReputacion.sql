USE StageUp;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Reserva', N'U') IS NULL
BEGIN
    RAISERROR(N'Primero debe ejecutarse Database/12_ReservaNormalizada.sql.', 16, 1);
END
GO

IF COL_LENGTH(N'dbo.Reserva', N'fechaFinalizacion') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD fechaFinalizacion DATETIME NULL;
END
GO

IF OBJECT_ID(N'dbo.Calificacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Calificacion
    (
        idCalificacion       INT IDENTITY(1,1) NOT NULL,
        idReserva            INT NOT NULL,
        idUsuarioAutor       INT NOT NULL,
        tipoCalificacion     NVARCHAR(30) NOT NULL,
        puntaje              TINYINT NOT NULL,
        comentario           NVARCHAR(1000) NOT NULL,
        fechaAlta            DATETIME NOT NULL CONSTRAINT DF_Calificacion_fechaAlta DEFAULT (GETDATE()),
        activo               BIT NOT NULL CONSTRAINT DF_Calificacion_activo DEFAULT (1),
        CONSTRAINT PK_Calificacion PRIMARY KEY CLUSTERED (idCalificacion),
        CONSTRAINT FK_Calificacion_Reserva FOREIGN KEY (idReserva) REFERENCES dbo.Reserva (idReserva),
        CONSTRAINT FK_Calificacion_UsuarioAutor FOREIGN KEY (idUsuarioAutor) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT CK_Calificacion_tipo CHECK (tipoCalificacion IN (N'Espacio', N'UsuarioSolicitante')),
        CONSTRAINT CK_Calificacion_puntaje CHECK (puntaje BETWEEN 1 AND 5),
        CONSTRAINT CK_Calificacion_comentario CHECK (LEN(LTRIM(RTRIM(comentario))) > 0),
        CONSTRAINT UQ_Calificacion_ReservaTipo UNIQUE (idReserva, tipoCalificacion)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Calificacion') AND name = N'IX_Calificacion_UsuarioAutor')
BEGIN
    CREATE INDEX IX_Calificacion_UsuarioAutor ON dbo.Calificacion (idUsuarioAutor, fechaAlta DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Calificacion') AND name = N'IX_Calificacion_ReservaActivo')
BEGIN
    CREATE INDEX IX_Calificacion_ReservaActivo ON dbo.Calificacion (idReserva, activo) INCLUDE (tipoCalificacion, puntaje, comentario, fechaAlta);
END
GO

IF OBJECT_ID(N'dbo.vw_CalificacionDetalle', N'V') IS NOT NULL DROP VIEW dbo.vw_CalificacionDetalle;
GO
CREATE VIEW dbo.vw_CalificacionDetalle
AS
    SELECT
        c.idCalificacion,
        c.idReserva,
        c.idUsuarioAutor,
        c.tipoCalificacion,
        c.puntaje,
        c.comentario,
        c.fechaAlta,
        c.activo,
        r.idEspacioArtistico,
        CASE WHEN c.tipoCalificacion = N'UsuarioSolicitante' THEN r.idUsuarioExternoSolicitante ELSE NULL END AS idUsuarioEvaluado,
        e.nombreEspacio,
        LTRIM(RTRIM(autor.nombre + N' ' + autor.apellido)) AS nombreAutor,
        CASE
            WHEN c.tipoCalificacion = N'Espacio' THEN e.nombreEspacio
            ELSE LTRIM(RTRIM(evaluado.nombre + N' ' + evaluado.apellido))
        END AS nombreEvaluado,
        r.fechaSolicitada AS fechaReserva
    FROM dbo.Calificacion c
    INNER JOIN dbo.Reserva r ON r.idReserva = c.idReserva
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno autor ON autor.idUsuarioExterno = c.idUsuarioAutor
    INNER JOIN dbo.UsuarioExterno evaluado ON evaluado.idUsuarioExterno = r.idUsuarioExternoSolicitante;
GO

IF OBJECT_ID(N'dbo.sp_Reserva_FinalizarVencidas', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_FinalizarVencidas;
GO
CREATE PROCEDURE dbo.sp_Reserva_FinalizarVencidas
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Reserva
    SET estadoReserva = N'Finalizada',
        fechaFinalizacion = COALESCE(fechaFinalizacion, GETDATE()),
        fechaUltimaModificacion = GETDATE()
    WHERE estadoReserva = N'Aceptada'
      AND DATEADD(MINUTE, COALESCE(CONVERT(INT, minutoHasta), 1440), CONVERT(DATETIME, fechaSolicitada)) <= GETDATE();
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_Insertar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_Insertar;
GO
CREATE PROCEDURE dbo.sp_Calificacion_Insertar
    @idReserva          INT,
    @idUsuarioAutor     INT,
    @tipoCalificacion   NVARCHAR(30),
    @puntaje            TINYINT,
    @comentario         NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Reserva
        SET estadoReserva = N'Finalizada',
            fechaFinalizacion = COALESCE(fechaFinalizacion, GETDATE()),
            fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva
          AND estadoReserva = N'Aceptada'
          AND DATEADD(MINUTE, COALESCE(CONVERT(INT, minutoHasta), 1440), CONVERT(DATETIME, fechaSolicitada)) <= GETDATE();

        DECLARE @estadoReserva NVARCHAR(50);
        DECLARE @idSolicitante INT;
        DECLARE @idGestor INT;

        SELECT
            @estadoReserva = r.estadoReserva,
            @idSolicitante = r.idUsuarioExternoSolicitante,
            @idGestor = e.idUsuarioGestor
        FROM dbo.Reserva r WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        WHERE r.idReserva = @idReserva;

        IF @estadoReserva IS NULL
            RAISERROR(N'No se encontró la reserva indicada.', 16, 1);

        IF @estadoReserva <> N'Finalizada'
            RAISERROR(N'La reserva todavía no está finalizada.', 16, 1);

        IF @tipoCalificacion NOT IN (N'Espacio', N'UsuarioSolicitante')
            RAISERROR(N'El tipo de calificación no es válido.', 16, 1);

        IF @puntaje < 1 OR @puntaje > 5
            RAISERROR(N'El puntaje debe estar entre 1 y 5.', 16, 1);

        IF @comentario IS NULL OR LEN(LTRIM(RTRIM(@comentario))) = 0 OR LEN(@comentario) > 1000
            RAISERROR(N'El comentario es obligatorio y no puede superar los 1000 caracteres.', 16, 1);

        IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE idUsuarioExterno = @idUsuarioAutor AND activo = 1)
            RAISERROR(N'El autor de la calificación no tiene una cuenta activa.', 16, 1);

        IF @tipoCalificacion = N'Espacio' AND @idUsuarioAutor <> @idSolicitante
            RAISERROR(N'Solo el solicitante puede calificar el espacio reservado.', 16, 1);

        IF @tipoCalificacion = N'UsuarioSolicitante' AND @idUsuarioAutor <> @idGestor
            RAISERROR(N'Solo el gestor del espacio puede calificar al solicitante.', 16, 1);

        IF EXISTS (
            SELECT 1
            FROM dbo.Calificacion WITH (UPDLOCK, HOLDLOCK)
            WHERE idReserva = @idReserva
              AND tipoCalificacion = @tipoCalificacion
        )
            RAISERROR(N'Esta calificación ya fue registrada.', 16, 1);

        INSERT INTO dbo.Calificacion
            (idReserva, idUsuarioAutor, tipoCalificacion, puntaje, comentario, fechaAlta, activo)
        VALUES
            (@idReserva, @idUsuarioAutor, @tipoCalificacion, @puntaje, LTRIM(RTRIM(@comentario)), GETDATE(), 1);

        DECLARE @idCalificacion INT = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;
        SELECT @idCalificacion AS idCalificacion;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @mensaje NVARCHAR(2048) = ERROR_MESSAGE();
        RAISERROR(@mensaje, 16, 1);
    END CATCH
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_Existe', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_Existe;
GO
CREATE PROCEDURE dbo.sp_Calificacion_Existe
    @idReserva          INT,
    @tipoCalificacion   NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Calificacion
        WHERE idReserva = @idReserva AND tipoCalificacion = @tipoCalificacion
    ) THEN 1 ELSE 0 END) AS existe;
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_ListarPorEspacio', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarPorEspacio;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.vw_CalificacionDetalle
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND tipoCalificacion = N'Espacio'
      AND activo = 1
    ORDER BY fechaAlta DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_ListarRecibidasPorUsuario', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarRecibidasPorUsuario;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarRecibidasPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.vw_CalificacionDetalle
    WHERE idUsuarioEvaluado = @idUsuarioExterno
      AND tipoCalificacion = N'UsuarioSolicitante'
      AND activo = 1
    ORDER BY fechaAlta DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_ListarRealizadasPorUsuario', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarRealizadasPorUsuario;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarRealizadasPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.vw_CalificacionDetalle
    WHERE idUsuarioAutor = @idUsuarioExterno
      AND activo = 1
    ORDER BY fechaAlta DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_ResumenPorEspacio', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ResumenPorEspacio;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ResumenPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        @idEspacioArtistico AS idEspacioArtistico,
        CONVERT(DECIMAL(4,2), COALESCE(AVG(CONVERT(DECIMAL(10,2), puntaje)), 0)) AS promedio,
        COUNT(*) AS cantidadCalificaciones
    FROM dbo.vw_CalificacionDetalle
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND tipoCalificacion = N'Espacio'
      AND activo = 1;
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_ResumenPorUsuario', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ResumenPorUsuario;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ResumenPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        @idUsuarioExterno AS idUsuarioExterno,
        CONVERT(DECIMAL(4,2), COALESCE(AVG(CONVERT(DECIMAL(10,2), puntaje)), 0)) AS promedio,
        COUNT(*) AS cantidadCalificaciones
    FROM dbo.vw_CalificacionDetalle
    WHERE idUsuarioEvaluado = @idUsuarioExterno
      AND tipoCalificacion = N'UsuarioSolicitante'
      AND activo = 1;
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_ListarResumenesEspacios', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarResumenesEspacios;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarResumenesEspacios
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        idEspacioArtistico,
        CONVERT(DECIMAL(4,2), AVG(CONVERT(DECIMAL(10,2), puntaje))) AS promedio,
        COUNT(*) AS cantidadCalificaciones
    FROM dbo.vw_CalificacionDetalle
    WHERE tipoCalificacion = N'Espacio' AND activo = 1
    GROUP BY idEspacioArtistico;
END
GO

IF OBJECT_ID(N'dbo.sp_Calificacion_ListarResumenesUsuarios', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarResumenesUsuarios;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarResumenesUsuarios
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        idUsuarioEvaluado AS idUsuarioExterno,
        CONVERT(DECIMAL(4,2), AVG(CONVERT(DECIMAL(10,2), puntaje))) AS promedio,
        COUNT(*) AS cantidadCalificaciones
    FROM dbo.vw_CalificacionDetalle
    WHERE tipoCalificacion = N'UsuarioSolicitante' AND activo = 1
    GROUP BY idUsuarioEvaluado;
END
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
    WHERE r.idUsuarioExternoSolicitante = @idUsuarioExternoSolicitante
    ORDER BY r.fechaCreacion DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_Reserva_ListarPorGestor', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPorGestor;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPorGestor
    @idUsuarioGestor INT
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
        LTRIM(RTRIM(u.nombre + N' ' + u.apellido)) AS nombreSolicitante,
        u.correoElectronico AS correoSolicitante,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'Espacio'
        ) THEN 1 ELSE 0 END) AS calificacionEspacioRealizada,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'UsuarioSolicitante'
        ) THEN 1 ELSE 0 END) AS calificacionSolicitanteRealizada,
        reputacion.promedio AS promedioCalificacionSolicitante,
        COALESCE(reputacion.cantidadCalificaciones, 0) AS cantidadCalificacionesSolicitante
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = r.idUsuarioExternoSolicitante
    OUTER APPLY
    (
        SELECT
            CONVERT(DECIMAL(4,2), AVG(CONVERT(DECIMAL(10,2), c.puntaje))) AS promedio,
            COUNT(*) AS cantidadCalificaciones
        FROM dbo.Calificacion c
        INNER JOIN dbo.Reserva rr ON rr.idReserva = c.idReserva
        WHERE rr.idUsuarioExternoSolicitante = r.idUsuarioExternoSolicitante
          AND c.tipoCalificacion = N'UsuarioSolicitante'
          AND c.activo = 1
    ) reputacion
    WHERE e.idUsuarioGestor = @idUsuarioGestor
    ORDER BY
        CASE WHEN r.estadoReserva = N'Pendiente' THEN 0 WHEN r.estadoReserva = N'Aceptada' THEN 1 ELSE 2 END,
        r.fechaCreacion DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_Reserva_ObtenerPorId', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Reserva_ObtenerPorId
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.idReserva = @idReserva;
END
GO

IF OBJECT_ID(N'dbo.Idioma', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.EtiquetaTraduccion', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Traduccion', N'U') IS NOT NULL
BEGIN
    MERGE dbo.EtiquetaTraduccion AS destino
    USING
    (
        VALUES
            (N'Calificacion_CalificarEspacio', N'Calificar espacio', N'Calificaciones'),
            (N'Calificacion_ResenaEnviada', N'Reseña enviada', N'Calificaciones'),
            (N'Calificacion_ExperienciaVerificada', N'Experiencia verificada', N'Calificaciones'),
            (N'Calificacion_TituloEspacio', N'¿Cómo estuvo el espacio?', N'Calificaciones'),
            (N'Calificacion_AyudaEspacio', N'Tu opinión ayudará a otras personas a elegir y al gestor a seguir mejorando.', N'Calificaciones'),
            (N'Calificacion_Puntaje', N'Calificación *', N'Calificaciones'),
            (N'Calificacion_Comentario', N'Comentario *', N'Calificaciones'),
            (N'Calificacion_PlaceholderEspacio', N'Contá cómo fue tu experiencia con el espacio.', N'Calificaciones'),
            (N'Calificacion_Publicar', N'Publicar reseña', N'Calificaciones'),
            (N'Calificacion_FinalizadaGestor', N'La reserva finalizó. Ya podés contar cómo fue la experiencia con el solicitante.', N'Calificaciones'),
            (N'Calificacion_CalificarSolicitante', N'Calificar solicitante', N'Calificaciones'),
            (N'Calificacion_CalificacionEnviada', N'Calificación enviada', N'Calificaciones'),
            (N'Calificacion_TituloSolicitante', N'¿Cómo fue la experiencia con el solicitante?', N'Calificaciones'),
            (N'Calificacion_AyudaSolicitante', N'Tu calificación será visible para el usuario y ayudará a otros gestores a evaluar futuras solicitudes.', N'Calificaciones'),
            (N'Calificacion_PlaceholderSolicitante', N'Contá si respetó el horario, el espacio y los acuerdos.', N'Calificaciones'),
            (N'Calificacion_Enviar', N'Enviar calificación', N'Calificaciones'),
            (N'Calificacion_SoloReservasVerificadas', N'Todas las opiniones corresponden a reservas finalizadas.', N'Calificaciones'),
            (N'Calificacion_Historial', N'Experiencias verificadas', N'Calificaciones'),
            (N'Calificacion_MisCalificaciones', N'Mis calificaciones', N'Calificaciones'),
            (N'Calificacion_Recibidas', N'Recibidas como solicitante', N'Calificaciones'),
            (N'Calificacion_Realizadas', N'Realizadas', N'Calificaciones'),
            (N'Calificacion_VerResenas', N'Ver reseñas', N'Calificaciones'),
            (N'Calificacion_UltimasOpiniones', N'Últimas opiniones de gestores', N'Calificaciones'),
            (N'Calificacion_SeleccionarPuntaje', N'Seleccioná una puntuación', N'Calificaciones'),
            (N'Calificacion_Excelente', N'★★★★★ · Excelente', N'Calificaciones'),
            (N'Calificacion_MuyBueno', N'★★★★☆ · Muy bueno', N'Calificaciones'),
            (N'Calificacion_Bueno', N'★★★☆☆ · Bueno', N'Calificaciones'),
            (N'Calificacion_Regular', N'★★☆☆☆ · Regular', N'Calificaciones'),
            (N'Calificacion_Malo', N'★☆☆☆☆ · Malo', N'Calificaciones'),
            (N'Calificacion_SinCalificaciones', N'Sin calificaciones todavía', N'Calificaciones'),
            (N'Calificacion_NuevoSinResenas', N'Nuevo · Sin reseñas', N'Calificaciones'),
            (N'Calificacion_SinResenas', N'Sin reseñas todavía', N'Calificaciones'),
            (N'Calificacion_ResumenPlural', N'{0} de 5 · {1} calificaciones', N'Calificaciones'),
            (N'Calificacion_ResumenSingular', N'{0} de 5 · {1} calificación', N'Calificaciones'),
            (N'Calificacion_ResenasPlural', N'★ {0} · {1} reseñas', N'Calificaciones'),
            (N'Calificacion_ResenaSingular', N'★ {0} · {1} reseña', N'Calificaciones'),
            (N'Calificacion_ResenasVerificadasPlural', N'{0} reseñas verificadas', N'Calificaciones'),
            (N'Calificacion_ResenaVerificadaSingular', N'{0} reseña verificada', N'Calificaciones'),
            (N'Calificacion_EspacioDestino', N'Espacio: {0}', N'Calificaciones'),
            (N'Calificacion_SolicitanteDestino', N'Solicitante: {0}', N'Calificaciones'),
            (N'Calificacion_ReservaDel', N'Reserva del', N'Calificaciones'),
            (N'Calificacion_PublicadaEl', N'Publicada el', N'Calificaciones'),
            (N'Calificacion_Por', N'Por', N'Calificaciones'),
            (N'Calificacion_SinRecibidas', N'Todavía no recibiste calificaciones. Aparecerán después de tus reservas finalizadas.', N'Calificaciones'),
            (N'Calificacion_SinRealizadas', N'Todavía no realizaste calificaciones.', N'Calificaciones'),
            (N'Calificacion_CuandoPrimeraResena', N'Cuando finalice la primera reserva, la persona que utilizó el espacio podrá compartir su experiencia.', N'Calificaciones'),
            (N'Calificacion_ErrorPuntaje', N'Elegí una calificación entre 1 y 5 estrellas.', N'Calificaciones'),
            (N'Calificacion_ErrorComentario', N'Escribí un comentario sobre tu experiencia.', N'Calificaciones'),
            (N'Calificacion_PuntajeAria', N'{0} de 5 estrellas', N'Calificaciones')
    ) AS origen (claveEtiqueta, textoPredeterminado, modulo)
    ON destino.claveEtiqueta = origen.claveEtiqueta
    WHEN MATCHED THEN
        UPDATE SET
            destino.textoPredeterminado = origen.textoPredeterminado,
            destino.modulo = origen.modulo,
            destino.activo = 1,
            destino.fechaUltimaModificacion = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT (claveEtiqueta, textoPredeterminado, modulo, fechaAlta, activo)
        VALUES (origen.claveEtiqueta, origen.textoPredeterminado, origen.modulo, GETDATE(), 1);

    DECLARE @idEspanolCalificacion INT =
    (
        SELECT TOP 1 idIdioma
        FROM dbo.Idioma
        WHERE codigoIdioma = N'es-AR'
           OR codigoIdioma = N'es'
           OR nombreIdioma IN (N'Español', N'Espanol', N'Spanish')
        ORDER BY CASE WHEN codigoIdioma = N'es-AR' THEN 0 WHEN codigoIdioma = N'es' THEN 1 ELSE 2 END, idIdioma
    );

    IF @idEspanolCalificacion IS NOT NULL
    BEGIN
        MERGE dbo.Traduccion AS destino
        USING
        (
            SELECT @idEspanolCalificacion, idEtiquetaTraduccion, textoPredeterminado
            FROM dbo.EtiquetaTraduccion
            WHERE modulo = N'Calificaciones' AND activo = 1
        ) AS origen (idIdioma, idEtiquetaTraduccion, textoTraducido)
        ON destino.idIdioma = origen.idIdioma
           AND destino.idEtiquetaTraduccion = origen.idEtiquetaTraduccion
        WHEN MATCHED THEN
            UPDATE SET destino.textoTraducido = origen.textoTraducido, destino.fechaUltimaModificacion = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (idIdioma, idEtiquetaTraduccion, textoTraducido)
            VALUES (origen.idIdioma, origen.idEtiquetaTraduccion, origen.textoTraducido);
    END

    DECLARE @idInglesCalificacion INT =
    (
        SELECT TOP 1 idIdioma
        FROM dbo.Idioma
        WHERE codigoIdioma = N'en-US'
           OR codigoIdioma = N'en'
           OR codigoIdioma LIKE N'en-%'
           OR nombreIdioma IN (N'Inglés', N'Ingles', N'English')
        ORDER BY CASE WHEN codigoIdioma = N'en-US' THEN 0 WHEN codigoIdioma = N'en' THEN 1 WHEN codigoIdioma LIKE N'en-%' THEN 2 ELSE 3 END, idIdioma
    );

    IF @idInglesCalificacion IS NOT NULL
    BEGIN
        MERGE dbo.Traduccion AS destino
        USING
        (
            SELECT
                @idInglesCalificacion,
                etiqueta.idEtiquetaTraduccion,
                origen.textoTraducido
            FROM
            (
                VALUES
                    (N'Calificacion_CalificarEspacio', N'Rate space'),
                    (N'Calificacion_ResenaEnviada', N'Review submitted'),
                    (N'Calificacion_ExperienciaVerificada', N'Verified experience'),
                    (N'Calificacion_TituloEspacio', N'How was the space?'),
                    (N'Calificacion_AyudaEspacio', N'Your opinion will help others choose and help the manager keep improving.'),
                    (N'Calificacion_Puntaje', N'Rating *'),
                    (N'Calificacion_Comentario', N'Comment *'),
                    (N'Calificacion_PlaceholderEspacio', N'Tell us about your experience with the space.'),
                    (N'Calificacion_Publicar', N'Publish review'),
                    (N'Calificacion_FinalizadaGestor', N'The booking has ended. You can now share your experience with the applicant.'),
                    (N'Calificacion_CalificarSolicitante', N'Rate applicant'),
                    (N'Calificacion_CalificacionEnviada', N'Rating submitted'),
                    (N'Calificacion_TituloSolicitante', N'How was your experience with the applicant?'),
                    (N'Calificacion_AyudaSolicitante', N'Your rating will be visible to the user and will help other managers review future requests.'),
                    (N'Calificacion_PlaceholderSolicitante', N'Tell us whether they respected the schedule, the space, and the agreements.'),
                    (N'Calificacion_Enviar', N'Submit rating'),
                    (N'Calificacion_SoloReservasVerificadas', N'Every review comes from a completed booking.'),
                    (N'Calificacion_Historial', N'Verified experiences'),
                    (N'Calificacion_MisCalificaciones', N'My ratings'),
                    (N'Calificacion_Recibidas', N'Received as applicant'),
                    (N'Calificacion_Realizadas', N'Submitted'),
                    (N'Calificacion_VerResenas', N'View reviews'),
                    (N'Calificacion_UltimasOpiniones', N'Latest manager feedback'),
                    (N'Calificacion_SeleccionarPuntaje', N'Select a rating'),
                    (N'Calificacion_Excelente', N'★★★★★ · Excellent'),
                    (N'Calificacion_MuyBueno', N'★★★★☆ · Very good'),
                    (N'Calificacion_Bueno', N'★★★☆☆ · Good'),
                    (N'Calificacion_Regular', N'★★☆☆☆ · Fair'),
                    (N'Calificacion_Malo', N'★☆☆☆☆ · Poor'),
                    (N'Calificacion_SinCalificaciones', N'No ratings yet'),
                    (N'Calificacion_NuevoSinResenas', N'New · No reviews'),
                    (N'Calificacion_SinResenas', N'No reviews yet'),
                    (N'Calificacion_ResumenPlural', N'{0} out of 5 · {1} ratings'),
                    (N'Calificacion_ResumenSingular', N'{0} out of 5 · {1} rating'),
                    (N'Calificacion_ResenasPlural', N'★ {0} · {1} reviews'),
                    (N'Calificacion_ResenaSingular', N'★ {0} · {1} review'),
                    (N'Calificacion_ResenasVerificadasPlural', N'{0} verified reviews'),
                    (N'Calificacion_ResenaVerificadaSingular', N'{0} verified review'),
                    (N'Calificacion_EspacioDestino', N'Space: {0}'),
                    (N'Calificacion_SolicitanteDestino', N'Applicant: {0}'),
                    (N'Calificacion_ReservaDel', N'Booking date'),
                    (N'Calificacion_PublicadaEl', N'Published on'),
                    (N'Calificacion_Por', N'By'),
                    (N'Calificacion_SinRecibidas', N'You have not received any ratings yet. They will appear after your completed bookings.'),
                    (N'Calificacion_SinRealizadas', N'You have not submitted any ratings yet.'),
                    (N'Calificacion_CuandoPrimeraResena', N'After the first booking ends, the person who used the space will be able to share their experience.'),
                    (N'Calificacion_ErrorPuntaje', N'Choose a rating between 1 and 5 stars.'),
                    (N'Calificacion_ErrorComentario', N'Write a comment about your experience.'),
                    (N'Calificacion_PuntajeAria', N'{0} out of 5 stars')
            ) origen (claveEtiqueta, textoTraducido)
            INNER JOIN dbo.EtiquetaTraduccion etiqueta ON etiqueta.claveEtiqueta = origen.claveEtiqueta
        ) AS origen (idIdioma, idEtiquetaTraduccion, textoTraducido)
        ON destino.idIdioma = origen.idIdioma
           AND destino.idEtiquetaTraduccion = origen.idEtiquetaTraduccion
        WHEN MATCHED THEN
            UPDATE SET destino.textoTraducido = origen.textoTraducido, destino.fechaUltimaModificacion = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (idIdioma, idEtiquetaTraduccion, textoTraducido)
            VALUES (origen.idIdioma, origen.idEtiquetaTraduccion, origen.textoTraducido);
    END
END
GO
