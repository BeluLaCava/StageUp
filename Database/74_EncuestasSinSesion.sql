IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- -----------------------------------------------------------------------------
-- Encuestas dirigidas a "Todos" sin iniciar sesión.
-- Un visitante sin cuenta puede responder una encuesta con público "Todos".
-- Para que no la responda dos veces, el navegador guarda una cookie con un
-- identificador aleatorio; en la base se guarda solo su hash SHA-256
-- (identificadorAnonimo), nunca la cookie.
-- Cada respuesta es de un usuario (idUsuarioExterno) o anónima
-- (identificadorAnonimo), nunca de los dos. Las encuestas dirigidas a un
-- perfil siguen pidiendo sesión.
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'dbo.RespuestaEncuesta', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/36_EncuestasYGraficoResultados.sql.', 1;
END
GO

IF COL_LENGTH(N'dbo.RespuestaEncuesta', N'identificadorAnonimo') IS NULL
    ALTER TABLE dbo.RespuestaEncuesta ADD identificadorAnonimo CHAR(64) NULL;
GO

-- La restricción única (idEncuesta, idUsuarioExterno) no admite más de una
-- respuesta anónima por encuesta (un solo NULL): se reemplaza por dos
-- índices únicos filtrados, uno para usuarios y otro para anónimos.
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_RespuestaEncuesta_EncuestaUsuario' AND parent_object_id = OBJECT_ID(N'dbo.RespuestaEncuesta'))
    ALTER TABLE dbo.RespuestaEncuesta DROP CONSTRAINT UQ_RespuestaEncuesta_EncuestaUsuario;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RespuestaEncuesta') AND name = N'idUsuarioExterno' AND is_nullable = 0)
    ALTER TABLE dbo.RespuestaEncuesta ALTER COLUMN idUsuarioExterno INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_RespuestaEncuesta_Usuario' AND object_id = OBJECT_ID(N'dbo.RespuestaEncuesta'))
    CREATE UNIQUE INDEX UX_RespuestaEncuesta_Usuario ON dbo.RespuestaEncuesta (idEncuesta, idUsuarioExterno) WHERE idUsuarioExterno IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_RespuestaEncuesta_Anonimo' AND object_id = OBJECT_ID(N'dbo.RespuestaEncuesta'))
    CREATE UNIQUE INDEX UX_RespuestaEncuesta_Anonimo ON dbo.RespuestaEncuesta (idEncuesta, identificadorAnonimo) WHERE identificadorAnonimo IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RespuestaEncuesta_Respondente')
    ALTER TABLE dbo.RespuestaEncuesta ADD CONSTRAINT CK_RespuestaEncuesta_Respondente CHECK
        ((idUsuarioExterno IS NOT NULL AND identificadorAnonimo IS NULL) OR (idUsuarioExterno IS NULL AND identificadorAnonimo IS NOT NULL));
GO

-- ---------------------------------------------------------------------------
-- Listados: @idUsuarioExterno NULL = visitante sin sesión (solo encuestas
-- para "Todos"). @identificadorAnonimo es el hash de la cookie del
-- navegador, si la hay: también cuenta como "ya respondida" para un usuario
-- con sesión que antes respondió desde ese navegador sin iniciar sesión.
-- ---------------------------------------------------------------------------
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
            AND (r.idUsuarioExterno = @idUsuarioExterno OR r.identificadorAnonimo = @identificadorAnonimo)
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
    WHERE e.estado IN (N'Activa', N'Cerrada') -- un Borrador nunca se publicó: no tiene resultados, aunque esté vencido
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
                AND (r.idUsuarioExterno = @idUsuarioExterno OR r.identificadorAnonimo = @identificadorAnonimo)
          )
      )
    ORDER BY e.fechaVencimiento DESC;
END
GO

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
        SELECT 1 FROM dbo.RespuestaEncuesta
        WHERE idEncuesta = @idEncuesta
          AND (idUsuarioExterno = @idUsuarioExterno OR identificadorAnonimo = @identificadorAnonimo)
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS existeRespuesta;
END
GO

-- Alta: con sesión se guarda el usuario; sin sesión, el hash de la cookie.
-- La encuesta tiene que estar activa y vigente, y una respuesta anónima solo
-- se acepta si está dirigida a "Todos" (segunda barrera; la primera es
-- BLL_Encuesta).
IF OBJECT_ID('dbo.sp_RespuestaEncuesta_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuesta_Insertar;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuesta_Insertar
    @idEncuesta             INT,
    @idUsuarioExterno       INT = NULL,
    @identificadorAnonimo   CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @idUsuarioExterno IS NOT NULL
        SET @identificadorAnonimo = NULL;

    IF @idUsuarioExterno IS NULL AND @identificadorAnonimo IS NULL
        THROW 51060, 'La respuesta no tiene usuario ni identificador anónimo.', 1;

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

    INSERT INTO dbo.RespuestaEncuesta (idEncuesta, idUsuarioExterno, identificadorAnonimo, fechaRespuesta)
    VALUES (@idEncuesta, @idUsuarioExterno, @identificadorAnonimo, GETDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idRespuestaEncuesta;
END
GO
