IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID('dbo.sp_Notificacion_ContarNoLeidas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_ContarNoLeidas;
GO
IF OBJECT_ID('dbo.sp_Notificacion_MarcarTodasLeidas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_MarcarTodasLeidas;
GO
IF OBJECT_ID('dbo.sp_Notificacion_MarcarLeida', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_MarcarLeida;
GO
IF OBJECT_ID('dbo.sp_Notificacion_ListarPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_ListarPorUsuario;
GO
IF OBJECT_ID('dbo.sp_Notificacion_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Notificacion_Insertar;
GO
IF OBJECT_ID('dbo.sp_FranjaEspacio_EliminarPorActividad', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_EliminarPorActividad;
GO
IF OBJECT_ID('dbo.sp_FranjaEspacio_InsertarDesdeActividad', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_InsertarDesdeActividad;
GO
IF OBJECT_ID('dbo.sp_ActividadParticipante_ListarPorParticipante', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_ListarPorParticipante;
GO
IF OBJECT_ID('dbo.sp_ActividadParticipante_ListarPorActividad', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_ListarPorActividad;
GO
IF OBJECT_ID('dbo.sp_ActividadParticipante_Eliminar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_Eliminar;
GO
IF OBJECT_ID('dbo.sp_ActividadParticipante_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_Insertar;
GO
IF OBJECT_ID('dbo.sp_Participante_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_DarDeBaja;
GO
IF OBJECT_ID('dbo.sp_Participante_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_Modificar;
GO
IF OBJECT_ID('dbo.sp_Participante_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_ObtenerPorId;
GO
IF OBJECT_ID('dbo.sp_Participante_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_ListarPorUsuarioGestor;
GO
IF OBJECT_ID('dbo.sp_Participante_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_Insertar;
GO
IF OBJECT_ID('dbo.sp_ActividadDiaSemana_ListarPorActividad', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadDiaSemana_ListarPorActividad;
GO
IF OBJECT_ID('dbo.sp_ActividadDiaSemana_EliminarPorActividad', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadDiaSemana_EliminarPorActividad;
GO
IF OBJECT_ID('dbo.sp_ActividadDiaSemana_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadDiaSemana_Insertar;
GO
IF OBJECT_ID('dbo.sp_Actividad_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_DarDeBaja;
GO
IF OBJECT_ID('dbo.sp_Actividad_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_Modificar;
GO
IF OBJECT_ID('dbo.sp_Actividad_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_ObtenerPorId;
GO
IF OBJECT_ID('dbo.sp_Actividad_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_ListarPorUsuarioGestor;
GO
IF OBJECT_ID('dbo.sp_Actividad_ListarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_ListarPorEspacio;
GO
IF OBJECT_ID('dbo.sp_Actividad_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_Insertar;
GO

IF OBJECT_ID('dbo.Notificacion', 'U') IS NOT NULL DROP TABLE dbo.Notificacion;
GO
IF OBJECT_ID('dbo.ActividadParticipante', 'U') IS NOT NULL DROP TABLE dbo.ActividadParticipante;
GO
IF OBJECT_ID('dbo.Participante', 'U') IS NOT NULL DROP TABLE dbo.Participante;
GO
IF OBJECT_ID('dbo.ActividadDiaSemana', 'U') IS NOT NULL DROP TABLE dbo.ActividadDiaSemana;
GO

-- Si esta segunda ejecución encuentra columnas ya agregadas en FranjaEspacio
-- (origen / idActividad) y la tabla Actividad, las saca antes de recrear todo.
-- Todo el cuerpo de este bloque va como SQL dinámico (EXEC) a propósito: si
-- es la primera vez que se corre el script, las columnas origen/idActividad
-- todavía no existen, y SQL Server valida los nombres de columna de TODO el
-- batch al compilarlo (no solo lo que efectivamente se ejecuta), aunque el
-- IF de más arriba nunca vaya a entrar acá. Con EXEC(), ese texto es apenas
-- una cadena hasta que se ejecuta, así que no se valida antes de tiempo.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FranjaEspacio') AND name = 'idActividad')
BEGIN
    EXEC(N'
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = ''FK_FranjaEspacio_Actividad'')
            ALTER TABLE dbo.FranjaEspacio DROP CONSTRAINT FK_FranjaEspacio_Actividad;
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = ''CK_FranjaEspacio_origenIdActividad'')
            ALTER TABLE dbo.FranjaEspacio DROP CONSTRAINT CK_FranjaEspacio_origenIdActividad;
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = ''CK_FranjaEspacio_origen'')
            ALTER TABLE dbo.FranjaEspacio DROP CONSTRAINT CK_FranjaEspacio_origen;
        DELETE FROM dbo.FranjaEspacio WHERE origen = N''Actividad'';
        ALTER TABLE dbo.FranjaEspacio DROP COLUMN idActividad;
        IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = ''DF_FranjaEspacio_origen'')
            ALTER TABLE dbo.FranjaEspacio DROP CONSTRAINT DF_FranjaEspacio_origen;
        ALTER TABLE dbo.FranjaEspacio DROP COLUMN origen;
    ');
END
GO

IF OBJECT_ID('dbo.Actividad', 'U') IS NOT NULL DROP TABLE dbo.Actividad;
GO

-- ============================================================================
-- Tablas
-- ============================================================================

CREATE TABLE dbo.Actividad
(
    idActividad             INT IDENTITY(1,1)   NOT NULL,
    idEspacioArtistico      INT                 NOT NULL,
    nombre                  NVARCHAR(200)       NOT NULL,
    tipo                    NVARCHAR(120)       NULL,
    modoRecurrencia         NVARCHAR(20)        NOT NULL,
    fecha                   DATE                NULL,
    semanaDelMes            TINYINT             NULL,
    diaSemanaMensual        TINYINT             NULL,
    minutoDesde             SMALLINT            NOT NULL,
    minutoHasta             SMALLINT            NOT NULL,
    cupoMaximo              INT                 NOT NULL,
    participantesEstimados  INT                 NULL,
    notas                   NVARCHAR(1000)      NULL,
    activa                  BIT                 NOT NULL CONSTRAINT DF_Actividad_activa DEFAULT (1),
    fechaCreacion           DATETIME            NOT NULL CONSTRAINT DF_Actividad_fechaCreacion DEFAULT (GETDATE()),
    fechaUltimaModificacion DATETIME            NULL,
    CONSTRAINT PK_Actividad PRIMARY KEY CLUSTERED (idActividad ASC),
    CONSTRAINT FK_Actividad_EspacioArtistico FOREIGN KEY (idEspacioArtistico)
        REFERENCES dbo.EspacioArtistico (idEspacioArtistico),
    CONSTRAINT CK_Actividad_modoRecurrencia CHECK (modoRecurrencia IN (N'Semanal', N'Mensual', N'Fecha')),
    CONSTRAINT CK_Actividad_horario CHECK (minutoDesde >= 0 AND minutoHasta <= 1440 AND minutoHasta > minutoDesde),
    CONSTRAINT CK_Actividad_cupoMaximo CHECK (cupoMaximo BETWEEN 1 AND 10000),
    CONSTRAINT CK_Actividad_semanaDelMes CHECK (semanaDelMes IS NULL OR semanaDelMes BETWEEN 1 AND 5),
    CONSTRAINT CK_Actividad_diaSemanaMensual CHECK (diaSemanaMensual IS NULL OR diaSemanaMensual BETWEEN 1 AND 7),
    CONSTRAINT CK_Actividad_recurrencia CHECK (
        (modoRecurrencia = N'Fecha'   AND fecha IS NOT NULL AND semanaDelMes IS NULL     AND diaSemanaMensual IS NULL) OR
        (modoRecurrencia = N'Mensual' AND fecha IS NULL     AND semanaDelMes IS NOT NULL AND diaSemanaMensual IS NOT NULL) OR
        (modoRecurrencia = N'Semanal' AND fecha IS NULL     AND semanaDelMes IS NULL     AND diaSemanaMensual IS NULL)
    )
);
GO

-- Una actividad "Semanal" puede repetirse en más de un día (ej: lunes y
-- miércoles), tal como ya permite el checklist de días de la pantalla.
CREATE TABLE dbo.ActividadDiaSemana
(
    idActividad     INT     NOT NULL,
    diaSemana       TINYINT NOT NULL,
    CONSTRAINT PK_ActividadDiaSemana PRIMARY KEY CLUSTERED (idActividad ASC, diaSemana ASC),
    CONSTRAINT FK_ActividadDiaSemana_Actividad FOREIGN KEY (idActividad)
        REFERENCES dbo.Actividad (idActividad) ON DELETE CASCADE,
    CONSTRAINT CK_ActividadDiaSemana_diaSemana CHECK (diaSemana BETWEEN 1 AND 7)
);
GO

-- Los participantes son un directorio propio de cada gestor (no son usuarios
-- de StageUp), tal como se ve en la pantalla: se cargan con nombre/apellido/
-- DNI y después se asocian a una o más actividades.
CREATE TABLE dbo.Participante
(
    idParticipante  INT IDENTITY(1,1)  NOT NULL,
    idUsuarioGestor INT                 NOT NULL,
    nombre          NVARCHAR(120)       NOT NULL,
    apellido        NVARCHAR(120)       NOT NULL,
    dni             NVARCHAR(20)        NOT NULL,
    notas           NVARCHAR(800)       NULL,
    activo          BIT                 NOT NULL CONSTRAINT DF_Participante_activo DEFAULT (1),
    fechaCreacion   DATETIME            NOT NULL CONSTRAINT DF_Participante_fechaCreacion DEFAULT (GETDATE()),
    CONSTRAINT PK_Participante PRIMARY KEY CLUSTERED (idParticipante ASC),
    CONSTRAINT FK_Participante_UsuarioExterno FOREIGN KEY (idUsuarioGestor)
        REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
    CONSTRAINT UQ_Participante_DniPorGestor UNIQUE (idUsuarioGestor, dni)
);
GO

CREATE TABLE dbo.ActividadParticipante
(
    idActividad     INT      NOT NULL,
    idParticipante  INT      NOT NULL,
    fechaAsociacion DATETIME NOT NULL CONSTRAINT DF_ActividadParticipante_fechaAsociacion DEFAULT (GETDATE()),
    CONSTRAINT PK_ActividadParticipante PRIMARY KEY CLUSTERED (idActividad ASC, idParticipante ASC),
    CONSTRAINT FK_ActividadParticipante_Actividad FOREIGN KEY (idActividad)
        REFERENCES dbo.Actividad (idActividad) ON DELETE CASCADE,
    CONSTRAINT FK_ActividadParticipante_Participante FOREIGN KEY (idParticipante)
        REFERENCES dbo.Participante (idParticipante)
);
GO

CREATE TABLE dbo.Notificacion
(
    idNotificacion  INT IDENTITY(1,1)  NOT NULL,
    idUsuarioExterno INT                NOT NULL,
    tipo            NVARCHAR(50)        NOT NULL,
    mensaje         NVARCHAR(300)       NOT NULL,
    urlDestino      NVARCHAR(300)       NULL,
    leida           BIT                 NOT NULL CONSTRAINT DF_Notificacion_leida DEFAULT (0),
    fechaCreacion   DATETIME            NOT NULL CONSTRAINT DF_Notificacion_fechaCreacion DEFAULT (GETDATE()),
    CONSTRAINT PK_Notificacion PRIMARY KEY CLUSTERED (idNotificacion ASC),
    CONSTRAINT FK_Notificacion_UsuarioExterno FOREIGN KEY (idUsuarioExterno)
        REFERENCES dbo.UsuarioExterno (idUsuarioExterno)
);
GO

-- FranjaEspacio: distinguir franjas cargadas a mano (Mis espacios) de las
-- generadas automáticamente por una actividad, para no perderlas cuando se
-- vuelve a guardar la ficha del espacio (ver comentario al inicio del script).
ALTER TABLE dbo.FranjaEspacio ADD
    origen      NVARCHAR(20) NOT NULL CONSTRAINT DF_FranjaEspacio_origen DEFAULT (N'Manual'),
    idActividad INT NULL;
GO
ALTER TABLE dbo.FranjaEspacio ADD CONSTRAINT CK_FranjaEspacio_origen CHECK (origen IN (N'Manual', N'Actividad'));
GO
ALTER TABLE dbo.FranjaEspacio ADD CONSTRAINT CK_FranjaEspacio_origenIdActividad CHECK (
    (origen = N'Manual'    AND idActividad IS NULL) OR
    (origen = N'Actividad' AND idActividad IS NOT NULL)
);
GO
ALTER TABLE dbo.FranjaEspacio ADD CONSTRAINT FK_FranjaEspacio_Actividad FOREIGN KEY (idActividad)
    REFERENCES dbo.Actividad (idActividad);
GO

-- ============================================================================
-- Actividad
-- ============================================================================

CREATE PROCEDURE dbo.sp_Actividad_Insertar
    @idEspacioArtistico     INT,
    @nombre                 NVARCHAR(200),
    @tipo                   NVARCHAR(120)   = NULL,
    @modoRecurrencia        NVARCHAR(20),
    @fecha                  DATE            = NULL,
    @semanaDelMes           TINYINT         = NULL,
    @diaSemanaMensual       TINYINT         = NULL,
    @minutoDesde            SMALLINT,
    @minutoHasta            SMALLINT,
    @cupoMaximo             INT,
    @participantesEstimados INT             = NULL,
    @notas                  NVARCHAR(1000)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.Actividad
            (idEspacioArtistico, nombre, tipo, modoRecurrencia, fecha, semanaDelMes, diaSemanaMensual,
             minutoDesde, minutoHasta, cupoMaximo, participantesEstimados, notas)
        VALUES
            (@idEspacioArtistico, @nombre, @tipo, @modoRecurrencia, @fecha, @semanaDelMes, @diaSemanaMensual,
             @minutoDesde, @minutoHasta, @cupoMaximo, @participantesEstimados, @notas);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS idActividad;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_Actividad_ListarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idActividad, idEspacioArtistico, nombre, tipo, modoRecurrencia, fecha, semanaDelMes,
           diaSemanaMensual, minutoDesde, minutoHasta, cupoMaximo, participantesEstimados, notas,
           activa, fechaCreacion, fechaUltimaModificacion
    FROM dbo.Actividad
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND activa = 1;
END
GO

CREATE PROCEDURE dbo.sp_Actividad_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.idActividad, a.idEspacioArtistico, a.nombre, a.tipo, a.modoRecurrencia, a.fecha, a.semanaDelMes,
           a.diaSemanaMensual, a.minutoDesde, a.minutoHasta, a.cupoMaximo, a.participantesEstimados, a.notas,
           a.activa, a.fechaCreacion, a.fechaUltimaModificacion, e.nombreEspacio
    FROM dbo.Actividad a
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = a.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND a.activa = 1
      AND e.activo = 1;
END
GO

CREATE PROCEDURE dbo.sp_Actividad_ObtenerPorId
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idActividad, idEspacioArtistico, nombre, tipo, modoRecurrencia, fecha, semanaDelMes,
           diaSemanaMensual, minutoDesde, minutoHasta, cupoMaximo, participantesEstimados, notas,
           activa, fechaCreacion, fechaUltimaModificacion
    FROM dbo.Actividad
    WHERE idActividad = @idActividad;
END
GO

CREATE PROCEDURE dbo.sp_Actividad_Modificar
    @idActividad            INT,
    @nombre                 NVARCHAR(200),
    @tipo                   NVARCHAR(120)   = NULL,
    @modoRecurrencia        NVARCHAR(20),
    @fecha                  DATE            = NULL,
    @semanaDelMes           TINYINT         = NULL,
    @diaSemanaMensual       TINYINT         = NULL,
    @minutoDesde            SMALLINT,
    @minutoHasta            SMALLINT,
    @cupoMaximo             INT,
    @participantesEstimados INT             = NULL,
    @notas                  NVARCHAR(1000)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Actividad
        SET nombre = @nombre,
            tipo = @tipo,
            modoRecurrencia = @modoRecurrencia,
            fecha = @fecha,
            semanaDelMes = @semanaDelMes,
            diaSemanaMensual = @diaSemanaMensual,
            minutoDesde = @minutoDesde,
            minutoHasta = @minutoHasta,
            cupoMaximo = @cupoMaximo,
            participantesEstimados = @participantesEstimados,
            notas = @notas,
            fechaUltimaModificacion = GETDATE()
        WHERE idActividad = @idActividad;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_Actividad_DarDeBaja
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Actividad
        SET activa = 0,
            fechaUltimaModificacion = GETDATE()
        WHERE idActividad = @idActividad;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- ActividadDiaSemana
-- ============================================================================

CREATE PROCEDURE dbo.sp_ActividadDiaSemana_Insertar
    @idActividad INT,
    @diaSemana   TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.ActividadDiaSemana (idActividad, diaSemana)
        VALUES (@idActividad, @diaSemana);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_ActividadDiaSemana_EliminarPorActividad
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.ActividadDiaSemana WHERE idActividad = @idActividad;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_ActividadDiaSemana_ListarPorActividad
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idActividad, diaSemana FROM dbo.ActividadDiaSemana WHERE idActividad = @idActividad;
END
GO

-- ============================================================================
-- Participante
-- ============================================================================

CREATE PROCEDURE dbo.sp_Participante_Insertar
    @idUsuarioGestor INT,
    @nombre          NVARCHAR(120),
    @apellido        NVARCHAR(120),
    @dni             NVARCHAR(20),
    @notas           NVARCHAR(800) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.Participante (idUsuarioGestor, nombre, apellido, dni, notas)
        VALUES (@idUsuarioGestor, @nombre, @apellido, @dni, @notas);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS idParticipante;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_Participante_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idParticipante, idUsuarioGestor, nombre, apellido, dni, notas, activo, fechaCreacion
    FROM dbo.Participante
    WHERE idUsuarioGestor = @idUsuarioGestor
      AND activo = 1;
END
GO

CREATE PROCEDURE dbo.sp_Participante_ObtenerPorId
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idParticipante, idUsuarioGestor, nombre, apellido, dni, notas, activo, fechaCreacion
    FROM dbo.Participante
    WHERE idParticipante = @idParticipante;
END
GO

CREATE PROCEDURE dbo.sp_Participante_Modificar
    @idParticipante INT,
    @nombre         NVARCHAR(120),
    @apellido       NVARCHAR(120),
    @dni            NVARCHAR(20),
    @notas          NVARCHAR(800) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Participante
        SET nombre = @nombre,
            apellido = @apellido,
            dni = @dni,
            notas = @notas
        WHERE idParticipante = @idParticipante;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_Participante_DarDeBaja
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Participante SET activo = 0 WHERE idParticipante = @idParticipante;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- ActividadParticipante
-- ============================================================================

CREATE PROCEDURE dbo.sp_ActividadParticipante_Insertar
    @idActividad    INT,
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.ActividadParticipante WHERE idActividad = @idActividad AND idParticipante = @idParticipante)
        BEGIN
            INSERT INTO dbo.ActividadParticipante (idActividad, idParticipante)
            VALUES (@idActividad, @idParticipante);
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_ActividadParticipante_Eliminar
    @idActividad    INT,
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.ActividadParticipante
        WHERE idActividad = @idActividad AND idParticipante = @idParticipante;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_ActividadParticipante_ListarPorActividad
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.idParticipante, p.nombre, p.apellido, p.dni, p.notas, p.activo, ap.fechaAsociacion
    FROM dbo.ActividadParticipante ap
    INNER JOIN dbo.Participante p ON p.idParticipante = ap.idParticipante
    WHERE ap.idActividad = @idActividad
      AND p.activo = 1;
END
GO

CREATE PROCEDURE dbo.sp_ActividadParticipante_ListarPorParticipante
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.idActividad, a.nombre, a.idEspacioArtistico, ap.fechaAsociacion
    FROM dbo.ActividadParticipante ap
    INNER JOIN dbo.Actividad a ON a.idActividad = ap.idActividad
    WHERE ap.idParticipante = @idParticipante
      AND a.activa = 1;
END
GO

-- ============================================================================
-- FranjaEspacio generadas por actividades
-- ============================================================================

CREATE PROCEDURE dbo.sp_FranjaEspacio_InsertarDesdeActividad
    @idEspacioArtistico INT,
    @idActividad        INT,
    @diaSemana          TINYINT = NULL,
    @fecha              DATE    = NULL,
    @minutoDesde        SMALLINT,
    @minutoHasta        SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.FranjaEspacio (idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado, origen, idActividad)
        VALUES (@idEspacioArtistico, @diaSemana, @fecha, @minutoDesde, @minutoHasta, 1, N'Actividad', @idActividad);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_FranjaEspacio_EliminarPorActividad
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.FranjaEspacio WHERE idActividad = @idActividad;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- Notificacion
-- ============================================================================

CREATE PROCEDURE dbo.sp_Notificacion_Insertar
    @idUsuarioExterno INT,
    @tipo             NVARCHAR(50),
    @mensaje          NVARCHAR(300),
    @urlDestino       NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.Notificacion (idUsuarioExterno, tipo, mensaje, urlDestino)
        VALUES (@idUsuarioExterno, @tipo, @mensaje, @urlDestino);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_Notificacion_ListarPorUsuario
    @idUsuarioExterno INT,
    @cantidad         INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@cantidad) idNotificacion, idUsuarioExterno, tipo, mensaje, urlDestino, leida, fechaCreacion
    FROM dbo.Notificacion
    WHERE idUsuarioExterno = @idUsuarioExterno
    ORDER BY fechaCreacion DESC;
END
GO

CREATE PROCEDURE dbo.sp_Notificacion_MarcarLeida
    @idNotificacion INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Notificacion SET leida = 1 WHERE idNotificacion = @idNotificacion;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_Notificacion_MarcarTodasLeidas
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Notificacion SET leida = 1 WHERE idUsuarioExterno = @idUsuarioExterno AND leida = 0;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.sp_Notificacion_ContarNoLeidas
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad FROM dbo.Notificacion WHERE idUsuarioExterno = @idUsuarioExterno AND leida = 0;
END
GO