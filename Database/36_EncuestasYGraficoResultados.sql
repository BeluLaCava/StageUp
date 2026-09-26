IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ---------------------------------------------------------------------------
-- Tablas
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.Encuesta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Encuesta
    (
        idEncuesta                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Encuesta PRIMARY KEY,
        titulo                      NVARCHAR(200)      NOT NULL,
        descripcion                 NVARCHAR(1000)     NULL,
        fechaInicio                 DATETIME           NOT NULL,
        fechaVencimiento            DATETIME           NOT NULL,
        publicoObjetivo             NVARCHAR(30)       NOT NULL
            CONSTRAINT CK_Encuesta_publicoObjetivo CHECK (publicoObjetivo IN (N'Todos', N'GestorEspacios', N'ExternoSolicitante')),
        estado                      NVARCHAR(20)       NOT NULL
            CONSTRAINT DF_Encuesta_estado DEFAULT (N'Borrador')
            CONSTRAINT CK_Encuesta_estado CHECK (estado IN (N'Borrador', N'Activa', N'Cerrada')),
        fechaAlta                   DATETIME           NOT NULL CONSTRAINT DF_Encuesta_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion     DATETIME           NULL,
        CONSTRAINT CK_Encuesta_vencimiento CHECK (fechaVencimiento > fechaInicio)
    );
END
GO

IF OBJECT_ID('dbo.PreguntaEncuesta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PreguntaEncuesta
    (
        idPreguntaEncuesta  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreguntaEncuesta PRIMARY KEY,
        idEncuesta          INT               NOT NULL,
        texto               NVARCHAR(300)     NOT NULL,
        orden               INT               NOT NULL CONSTRAINT DF_PreguntaEncuesta_orden DEFAULT (1),
        CONSTRAINT FK_PreguntaEncuesta_Encuesta FOREIGN KEY (idEncuesta)
            REFERENCES dbo.Encuesta (idEncuesta) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.OpcionPregunta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OpcionPregunta
    (
        idOpcionPregunta    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OpcionPregunta PRIMARY KEY,
        idPreguntaEncuesta  INT               NOT NULL,
        texto               NVARCHAR(200)     NOT NULL,
        orden               INT               NOT NULL CONSTRAINT DF_OpcionPregunta_orden DEFAULT (1),
        CONSTRAINT FK_OpcionPregunta_PreguntaEncuesta FOREIGN KEY (idPreguntaEncuesta)
            REFERENCES dbo.PreguntaEncuesta (idPreguntaEncuesta) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.RespuestaEncuesta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RespuestaEncuesta
    (
        idRespuestaEncuesta INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RespuestaEncuesta PRIMARY KEY,
        idEncuesta          INT               NOT NULL,
        idUsuarioExterno    INT               NOT NULL,
        fechaRespuesta      DATETIME          NOT NULL CONSTRAINT DF_RespuestaEncuesta_fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_RespuestaEncuesta_Encuesta FOREIGN KEY (idEncuesta)
            REFERENCES dbo.Encuesta (idEncuesta),
        CONSTRAINT FK_RespuestaEncuesta_UsuarioExterno FOREIGN KEY (idUsuarioExterno)
            REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT UQ_RespuestaEncuesta_EncuestaUsuario UNIQUE (idEncuesta, idUsuarioExterno)
    );
END
GO

IF OBJECT_ID('dbo.RespuestaEncuestaDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RespuestaEncuestaDetalle
    (
        idRespuestaEncuestaDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RespuestaEncuestaDetalle PRIMARY KEY,
        idRespuestaEncuesta        INT               NOT NULL,
        idPreguntaEncuesta         INT               NOT NULL,
        idOpcionPregunta           INT               NOT NULL,
        CONSTRAINT FK_RespuestaEncuestaDetalle_RespuestaEncuesta FOREIGN KEY (idRespuestaEncuesta)
            REFERENCES dbo.RespuestaEncuesta (idRespuestaEncuesta) ON DELETE CASCADE,
        CONSTRAINT FK_RespuestaEncuestaDetalle_PreguntaEncuesta FOREIGN KEY (idPreguntaEncuesta)
            REFERENCES dbo.PreguntaEncuesta (idPreguntaEncuesta),
        CONSTRAINT FK_RespuestaEncuestaDetalle_OpcionPregunta FOREIGN KEY (idOpcionPregunta)
            REFERENCES dbo.OpcionPregunta (idOpcionPregunta),
        CONSTRAINT UQ_RespuestaEncuestaDetalle_RespuestaPregunta UNIQUE (idRespuestaEncuesta, idPreguntaEncuesta)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RespuestaEncuestaDetalle_Opcion' AND object_id = OBJECT_ID('dbo.RespuestaEncuestaDetalle'))
BEGIN
    CREATE INDEX IX_RespuestaEncuestaDetalle_Opcion ON dbo.RespuestaEncuestaDetalle (idOpcionPregunta);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RespuestaEncuesta_Encuesta' AND object_id = OBJECT_ID('dbo.RespuestaEncuesta'))
BEGIN
    CREATE INDEX IX_RespuestaEncuesta_Encuesta ON dbo.RespuestaEncuesta (idEncuesta);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PreguntaEncuesta_Encuesta' AND object_id = OBJECT_ID('dbo.PreguntaEncuesta'))
BEGIN
    CREATE INDEX IX_PreguntaEncuesta_Encuesta ON dbo.PreguntaEncuesta (idEncuesta);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OpcionPregunta_Pregunta' AND object_id = OBJECT_ID('dbo.OpcionPregunta'))
BEGIN
    CREATE INDEX IX_OpcionPregunta_Pregunta ON dbo.OpcionPregunta (idPreguntaEncuesta);
END
GO

-- ---------------------------------------------------------------------------
-- ABM de Encuesta
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Encuesta_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_Insertar;
GO
CREATE PROCEDURE dbo.sp_Encuesta_Insertar
    @titulo             NVARCHAR(200),
    @descripcion        NVARCHAR(1000),
    @fechaInicio        DATETIME,
    @fechaVencimiento   DATETIME,
    @publicoObjetivo    NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Encuesta (titulo, descripcion, fechaInicio, fechaVencimiento, publicoObjetivo, estado, fechaAlta)
    VALUES (@titulo, @descripcion, @fechaInicio, @fechaVencimiento, @publicoObjetivo, N'Borrador', GETDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idEncuesta;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_Modificar;
GO
CREATE PROCEDURE dbo.sp_Encuesta_Modificar
    @idEncuesta         INT,
    @titulo             NVARCHAR(200),
    @descripcion        NVARCHAR(1000),
    @fechaInicio        DATETIME,
    @fechaVencimiento   DATETIME,
    @publicoObjetivo    NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Encuesta WHERE idEncuesta = @idEncuesta)
    BEGIN
        THROW 51201, 'No se encontró la encuesta indicada.', 1;
    END

    UPDATE dbo.Encuesta
    SET titulo = @titulo,
        descripcion = @descripcion,
        fechaInicio = @fechaInicio,
        fechaVencimiento = @fechaVencimiento,
        publicoObjetivo = @publicoObjetivo,
        fechaUltimaModificacion = GETDATE()
    WHERE idEncuesta = @idEncuesta;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_Encuesta_CambiarEstado
    @idEncuesta INT,
    @estado     NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Encuesta
    SET estado = @estado,
        fechaUltimaModificacion = GETDATE()
    WHERE idEncuesta = @idEncuesta;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_Listar;
GO
CREATE PROCEDURE dbo.sp_Encuesta_Listar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idEncuesta, titulo, descripcion, fechaInicio, fechaVencimiento, publicoObjetivo, estado, fechaAlta, fechaUltimaModificacion
    FROM dbo.Encuesta
    ORDER BY fechaAlta DESC;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ObtenerPorId
    @idEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idEncuesta, titulo, descripcion, fechaInicio, fechaVencimiento, publicoObjetivo, estado, fechaAlta, fechaUltimaModificacion
    FROM dbo.Encuesta
    WHERE idEncuesta = @idEncuesta;
END
GO

-- ---------------------------------------------------------------------------
-- Preguntas y opciones
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_PreguntaEncuesta_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEncuesta_Insertar;
GO
CREATE PROCEDURE dbo.sp_PreguntaEncuesta_Insertar
    @idEncuesta INT,
    @texto      NVARCHAR(300),
    @orden      INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PreguntaEncuesta (idEncuesta, texto, orden)
    VALUES (@idEncuesta, @texto, @orden);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idPreguntaEncuesta;
END
GO

IF OBJECT_ID('dbo.sp_PreguntaEncuesta_ListarPorEncuesta', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEncuesta_ListarPorEncuesta;
GO
CREATE PROCEDURE dbo.sp_PreguntaEncuesta_ListarPorEncuesta
    @idEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idPreguntaEncuesta, idEncuesta, texto, orden
    FROM dbo.PreguntaEncuesta
    WHERE idEncuesta = @idEncuesta
    ORDER BY orden ASC, idPreguntaEncuesta ASC;
END
GO

IF OBJECT_ID('dbo.sp_PreguntaEncuesta_Eliminar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEncuesta_Eliminar;
GO
CREATE PROCEDURE dbo.sp_PreguntaEncuesta_Eliminar
    @idPreguntaEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.PreguntaEncuesta
    WHERE idPreguntaEncuesta = @idPreguntaEncuesta;
END
GO

IF OBJECT_ID('dbo.sp_OpcionPregunta_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionPregunta_Insertar;
GO
CREATE PROCEDURE dbo.sp_OpcionPregunta_Insertar
    @idPreguntaEncuesta INT,
    @texto              NVARCHAR(200),
    @orden              INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OpcionPregunta (idPreguntaEncuesta, texto, orden)
    VALUES (@idPreguntaEncuesta, @texto, @orden);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idOpcionPregunta;
END
GO

IF OBJECT_ID('dbo.sp_OpcionPregunta_ListarPorPregunta', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionPregunta_ListarPorPregunta;
GO
CREATE PROCEDURE dbo.sp_OpcionPregunta_ListarPorPregunta
    @idPreguntaEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idOpcionPregunta, idPreguntaEncuesta, texto, orden
    FROM dbo.OpcionPregunta
    WHERE idPreguntaEncuesta = @idPreguntaEncuesta
    ORDER BY orden ASC, idOpcionPregunta ASC;
END
GO

-- ---------------------------------------------------------------------------
-- Listados para el usuario externo (público objetivo + vigencia)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Encuesta_ListarPendientesParaUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ListarPendientesParaUsuario;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ListarPendientesParaUsuario
    @idUsuarioExterno   INT,
    @perfilUsuario      NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    -- Activas, ya empezadas, todavía no vencidas ("vigentes" se calcula acá,
    -- no se persiste ningún estado "Vencida"), dirigidas a Todos o al perfil
    -- del usuario, y que el usuario todavía no haya respondido.
    SELECT e.idEncuesta, e.titulo, e.descripcion, e.fechaInicio, e.fechaVencimiento, e.publicoObjetivo, e.estado, e.fechaAlta, e.fechaUltimaModificacion
    FROM dbo.Encuesta e
    WHERE e.estado = N'Activa'
      AND e.fechaInicio <= GETDATE()
      AND e.fechaVencimiento >= GETDATE()
      AND (e.publicoObjetivo = N'Todos' OR e.publicoObjetivo = @perfilUsuario)
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.RespuestaEncuesta r
          WHERE r.idEncuesta = e.idEncuesta AND r.idUsuarioExterno = @idUsuarioExterno
      )
    ORDER BY e.fechaVencimiento ASC;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_ListarConResultadosParaUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario
    @idUsuarioExterno   INT,
    @perfilUsuario      NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    -- Encuestas ya empezadas y dirigidas al perfil del usuario, que además
    -- están vencidas o cerradas, o que el usuario ya respondió (para que vea
    -- el gráfico al instante apenas responde, aunque la encuesta siga
    -- vigente y abierta para otros usuarios).
    SELECT e.idEncuesta, e.titulo, e.descripcion, e.fechaInicio, e.fechaVencimiento, e.publicoObjetivo, e.estado, e.fechaAlta, e.fechaUltimaModificacion
    FROM dbo.Encuesta e
    WHERE e.fechaInicio <= GETDATE()
      AND (e.publicoObjetivo = N'Todos' OR e.publicoObjetivo = @perfilUsuario)
      AND
      (
          e.estado = N'Cerrada'
          OR e.fechaVencimiento < GETDATE()
          OR EXISTS
          (
              SELECT 1 FROM dbo.RespuestaEncuesta r
              WHERE r.idEncuesta = e.idEncuesta AND r.idUsuarioExterno = @idUsuarioExterno
          )
      )
    ORDER BY e.fechaVencimiento DESC;
END
GO

-- ---------------------------------------------------------------------------
-- Respuestas
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_RespuestaEncuesta_ExisteDeUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuesta_ExisteDeUsuario;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuesta_ExisteDeUsuario
    @idEncuesta         INT,
    @idUsuarioExterno   INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.RespuestaEncuesta
        WHERE idEncuesta = @idEncuesta AND idUsuarioExterno = @idUsuarioExterno
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS existeRespuesta;
END
GO

IF OBJECT_ID('dbo.sp_RespuestaEncuesta_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuesta_Insertar;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuesta_Insertar
    @idEncuesta         INT,
    @idUsuarioExterno   INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RespuestaEncuesta (idEncuesta, idUsuarioExterno, fechaRespuesta)
    VALUES (@idEncuesta, @idUsuarioExterno, GETDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idRespuestaEncuesta;
END
GO

IF OBJECT_ID('dbo.sp_RespuestaEncuestaDetalle_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuestaDetalle_Insertar;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuestaDetalle_Insertar
    @idRespuestaEncuesta    INT,
    @idPreguntaEncuesta     INT,
    @idOpcionPregunta       INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RespuestaEncuestaDetalle (idRespuestaEncuesta, idPreguntaEncuesta, idOpcionPregunta)
    VALUES (@idRespuestaEncuesta, @idPreguntaEncuesta, @idOpcionPregunta);
END
GO

-- ---------------------------------------------------------------------------
-- Resultados al instante: filas planas pregunta+opción con el
-- conteo de respuestas de cada opción y el total de respuestas de la
-- pregunta, para que la BLL arme el gráfico de barras con los porcentajes
-- calculados en el momento (nada queda cacheado).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Encuesta_ConsultarResultados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ConsultarResultados;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ConsultarResultados
    @idEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.idPreguntaEncuesta,
        p.texto AS textoPregunta,
        p.orden AS ordenPregunta,
        o.idOpcionPregunta,
        o.texto AS textoOpcion,
        o.orden AS ordenOpcion,
        (SELECT COUNT(*) FROM dbo.RespuestaEncuestaDetalle d WHERE d.idOpcionPregunta = o.idOpcionPregunta) AS cantidadRespuestas,
        (SELECT COUNT(*) FROM dbo.RespuestaEncuestaDetalle d2 WHERE d2.idPreguntaEncuesta = p.idPreguntaEncuesta) AS totalRespuestasPregunta
    FROM dbo.PreguntaEncuesta p
    INNER JOIN dbo.OpcionPregunta o ON o.idPreguntaEncuesta = p.idPreguntaEncuesta
    WHERE p.idEncuesta = @idEncuesta
    ORDER BY p.orden ASC, p.idPreguntaEncuesta ASC, o.orden ASC, o.idOpcionPregunta ASC;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso propio GESTIONAR_ENCUESTAS (mismo patrón que GESTIONAR_FAQ en
-- 29_FaqAbmYPermiso.sql): se registra en el esquema clásico y en el árbol de
-- componentes, y se asigna al rol Administrador.
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoEncuestas INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'GESTIONAR_ENCUESTAS'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoEncuestas IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'GESTIONAR_ENCUESTAS', N'Gestión de encuestas',
             N'Crear, publicar y cerrar encuestas dinámicas, y ver sus resultados.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/GestionEncuestas.aspx', 1);

        SET @idPermisoEncuestas = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Gestión de encuestas',
            descripcion = N'Crear, publicar y cerrar encuestas dinámicas, y ver sus resultados.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/GestionEncuestas.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoEncuestas;
    END

    DECLARE @idRolAdministrador INT =
    (
        SELECT TOP 1 idRolInterno
        FROM dbo.RolInterno
        WHERE nombreRol = N'Administrador'
          AND activo = 1
        ORDER BY idRolInterno
    );

    IF @idRolAdministrador IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.RolInternoPermiso
           WHERE idRolInterno = @idRolAdministrador
             AND idPermisoInterno = @idPermisoEncuestas
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoEncuestas, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoEncuestas;
    END

    IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
    BEGIN
        DECLARE @idGrupoAdministracion INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE tipoComponente = N'Grupo'
              AND idComponentePadre IS NULL
              AND nombre = N'Administración'
            ORDER BY idComponentePermiso
        );

        IF @idGrupoAdministracion IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (NULL, N'Grupo', NULL, N'Administración', NULL, NULL, 100, 1);

            SET @idGrupoAdministracion = CAST(SCOPE_IDENTITY() AS INT);
        END

        DECLARE @idComponenteEncuestas INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'GESTIONAR_ENCUESTAS'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteEncuestas IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_ENCUESTAS', N'Gestión de encuestas',
                 N'Crear, publicar y cerrar encuestas dinámicas, y ver sus resultados.',
                 N'~/Interno/GestionEncuestas.aspx', 110, 1);

            SET @idComponenteEncuestas = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Gestión de encuestas',
                descripcion = N'Crear, publicar y cerrar encuestas dinámicas, y ver sus resultados.',
                urlAsociada = N'~/Interno/GestionEncuestas.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteEncuestas;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteEncuestas
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteEncuestas, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteEncuestas;
            END
        END
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    DECLARE @mensajeError NVARCHAR(4000);
    SET @mensajeError = ERROR_MESSAGE();
    RAISERROR(N'%s', 16, 1, @mensajeError);
END CATCH;
GO
