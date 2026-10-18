IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ===========================================================================
-- Mensajes privados por reserva: se marcan como leídos solo los mensajes
-- que se le mostraron al usuario (@idsMensajes, separados por coma). Uno que
-- llega mientras tanto queda sin leer.
-- ===========================================================================
IF OBJECT_ID('dbo.sp_MensajeReserva_MarcarLeidos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MensajeReserva_MarcarLeidos;
GO
CREATE PROCEDURE dbo.sp_MensajeReserva_MarcarLeidos
    @idReserva          INT,
    @idUsuarioExterno   INT,
    @idsMensajes        NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ids TABLE (id INT PRIMARY KEY);
    INSERT INTO @ids
    SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsMensajes, N''), N',')
    WHERE TRY_CAST(value AS INT) IS NOT NULL;

    UPDATE m
    SET m.fechaLectura = GETDATE()
    FROM dbo.MensajeReserva m
    INNER JOIN @ids l ON l.id = m.idMensajeReserva
    INNER JOIN dbo.Reserva r ON r.idReserva = m.idReserva
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE m.idReserva = @idReserva
      AND m.fechaLectura IS NULL
      AND m.idUsuarioExternoEmisor <> @idUsuarioExterno
      AND (r.idUsuarioExternoSolicitante = @idUsuarioExterno OR e.idUsuarioGestor = @idUsuarioExterno);

    SELECT @@ROWCOUNT;
END
GO

-- ===========================================================================
-- Encuestas: navegador de cada respuesta.
-- Una respuesta con sesión guarda también el hash del navegador desde el
-- que se respondió (identificadorNavegador). Así, si después se cierra la
-- sesión, ese navegador ya no puede responder la misma encuesta como
-- visitante. Las respuestas sin sesión siguen usando identificadorAnonimo
-- (único por encuesta).
-- ===========================================================================
IF COL_LENGTH(N'dbo.RespuestaEncuesta', N'identificadorNavegador') IS NULL
    ALTER TABLE dbo.RespuestaEncuesta ADD identificadorNavegador CHAR(64) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RespuestaEncuesta_Navegador' AND object_id = OBJECT_ID(N'dbo.RespuestaEncuesta'))
    CREATE INDEX IX_RespuestaEncuesta_Navegador ON dbo.RespuestaEncuesta (idEncuesta, identificadorNavegador) WHERE identificadorNavegador IS NOT NULL;
GO

-- "Ya respondida":
--   * con sesión: por la cuenta, o si desde ese navegador se respondió sin sesión;
--   * sin sesión: si desde ese navegador se respondió, con o sin sesión.
IF OBJECT_ID('dbo.sp_RespuestaEncuesta_ExisteDeUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuesta_ExisteDeUsuario;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuesta_ExisteDeUsuario
    @idEncuesta             INT,
    @idUsuarioExterno       INT = NULL,
    @identificadorAnonimo   CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.RespuestaEncuesta r
        WHERE r.idEncuesta = @idEncuesta
          AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
               OR r.identificadorAnonimo = @identificadorAnonimo
               OR (@idUsuarioExterno IS NULL AND r.identificadorNavegador = @identificadorAnonimo))
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS existeRespuesta;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_ListarPendientesParaUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ListarPendientesParaUsuario;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ListarPendientesParaUsuario
    @idUsuarioExterno       INT = NULL,
    @perfilUsuario          NVARCHAR(30) = NULL,
    @identificadorAnonimo   CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.idEncuesta, e.titulo, e.descripcion, e.fechaInicio, e.fechaVencimiento, e.publicoObjetivo, e.estado, e.fechaAlta, e.fechaUltimaModificacion
    FROM dbo.Encuesta e
    WHERE e.estado = N'Activa'
      AND e.fechaInicio <= GETDATE()
      AND e.fechaVencimiento >= GETDATE()
      AND (e.publicoObjetivo = N'Todos' OR (@idUsuarioExterno IS NOT NULL AND e.publicoObjetivo = @perfilUsuario))
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.RespuestaEncuesta r
          WHERE r.idEncuesta = e.idEncuesta
            AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
                 OR r.identificadorAnonimo = @identificadorAnonimo
                 OR (@idUsuarioExterno IS NULL AND r.identificadorNavegador = @identificadorAnonimo))
      )
    ORDER BY e.fechaVencimiento ASC;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_ListarConResultadosParaUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario
    @idUsuarioExterno       INT = NULL,
    @perfilUsuario          NVARCHAR(30) = NULL,
    @identificadorAnonimo   CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.idEncuesta, e.titulo, e.descripcion, e.fechaInicio, e.fechaVencimiento, e.publicoObjetivo, e.estado, e.fechaAlta, e.fechaUltimaModificacion
    FROM dbo.Encuesta e
    WHERE e.estado IN (N'Activa', N'Cerrada')
      AND e.fechaInicio <= GETDATE()
      AND (e.publicoObjetivo = N'Todos' OR (@idUsuarioExterno IS NOT NULL AND e.publicoObjetivo = @perfilUsuario))
      AND
      (
          e.estado = N'Cerrada'
          OR e.fechaVencimiento < GETDATE()
          OR EXISTS
          (
              SELECT 1 FROM dbo.RespuestaEncuesta r
              WHERE r.idEncuesta = e.idEncuesta
                AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
                     OR r.identificadorAnonimo = @identificadorAnonimo
                     OR (@idUsuarioExterno IS NULL AND r.identificadorNavegador = @identificadorAnonimo))
          )
      )
    ORDER BY e.fechaVencimiento DESC;
END
GO

-- ===========================================================================
-- Registro completo de una respuesta: cabecera y todas las opciones elegidas
-- en una sola transacción (todo o nada).
--   @opciones: pares "idPregunta:idOpcion" separados por coma, uno por cada
--   pregunta de la encuesta.
--   @identificadorNavegador: hash del navegador. Sin sesión es obligatorio
--   (se guarda como identificadorAnonimo); con sesión es opcional.
-- THROW 51060 sin usuario ni navegador; 51061 la encuesta no admite la
-- respuesta; 51062 ya respondida (por la cuenta o desde ese navegador);
-- 51063 las opciones no corresponden a las preguntas de la encuesta.
-- ===========================================================================
IF OBJECT_ID('dbo.sp_RespuestaEncuesta_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuesta_Registrar;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuesta_Registrar
    @idEncuesta               INT,
    @idUsuarioExterno         INT = NULL,
    @identificadorNavegador   CHAR(64) = NULL,
    @opciones                 NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @idUsuarioExterno IS NULL AND @identificadorNavegador IS NULL
        THROW 51060, 'La respuesta no tiene usuario ni identificador del navegador.', 1;

    DECLARE @elegidas TABLE (idPregunta INT NULL, idOpcion INT NULL);
    INSERT INTO @elegidas (idPregunta, idOpcion)
    SELECT TRY_CAST(LEFT(value, NULLIF(CHARINDEX(N':', value), 0) - 1) AS INT),
           TRY_CAST(SUBSTRING(value, NULLIF(CHARINDEX(N':', value), 0) + 1, 20) AS INT)
    FROM STRING_SPLIT(ISNULL(@opciones, N''), N',')
    WHERE LEN(value) > 0;

    -- Una opción por pregunta, todas las preguntas, y cada opción de su pregunta.
    IF (SELECT COUNT(*) FROM @elegidas) <> (SELECT COUNT(*) FROM dbo.PreguntaEncuesta WHERE idEncuesta = @idEncuesta)
       OR (SELECT COUNT(*) FROM @elegidas) = 0
       OR EXISTS (SELECT 1 FROM @elegidas WHERE idPregunta IS NULL OR idOpcion IS NULL)
       OR (SELECT COUNT(DISTINCT idPregunta) FROM @elegidas) <> (SELECT COUNT(*) FROM @elegidas)
       OR EXISTS (SELECT 1 FROM @elegidas l
                  WHERE NOT EXISTS (SELECT 1 FROM dbo.OpcionPregunta o
                                    INNER JOIN dbo.PreguntaEncuesta p ON p.idPreguntaEncuesta = o.idPreguntaEncuesta
                                    WHERE o.idOpcionPregunta = l.idOpcion AND p.idPreguntaEncuesta = l.idPregunta
                                      AND p.idEncuesta = @idEncuesta))
        THROW 51063, 'Las opciones no corresponden a las preguntas de la encuesta.', 1;

    DECLARE @idRespuestaEncuesta INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Encuesta
            WHERE idEncuesta = @idEncuesta
              AND estado = N'Activa'
              AND fechaInicio <= GETDATE()
              AND fechaVencimiento >= GETDATE()
              AND (@idUsuarioExterno IS NOT NULL OR publicoObjetivo = N'Todos')
        )
            THROW 51061, 'La encuesta no admite esta respuesta.', 1;

        -- Con UPDLOCK y HOLDLOCK, dos envíos simultáneos desde la misma cuenta
        -- o el mismo navegador no pasan los dos este control.
        IF EXISTS
        (
            SELECT 1 FROM dbo.RespuestaEncuesta r WITH (UPDLOCK, HOLDLOCK)
            WHERE r.idEncuesta = @idEncuesta
              AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
                   OR (@identificadorNavegador IS NOT NULL AND r.identificadorAnonimo = @identificadorNavegador)
                   OR (@idUsuarioExterno IS NULL AND r.identificadorNavegador = @identificadorNavegador))
        )
            THROW 51062, 'La encuesta ya fue respondida.', 1;

        INSERT INTO dbo.RespuestaEncuesta (idEncuesta, idUsuarioExterno, identificadorAnonimo, identificadorNavegador, fechaRespuesta)
        VALUES (@idEncuesta, @idUsuarioExterno,
                CASE WHEN @idUsuarioExterno IS NULL THEN @identificadorNavegador END,
                CASE WHEN @idUsuarioExterno IS NOT NULL THEN @identificadorNavegador END,
                GETDATE());

        SET @idRespuestaEncuesta = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO dbo.RespuestaEncuestaDetalle (idRespuestaEncuesta, idPreguntaEncuesta, idOpcionPregunta)
        SELECT @idRespuestaEncuesta, idPregunta, idOpcion FROM @elegidas;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @idRespuestaEncuesta AS idRespuestaEncuesta;
END
GO
