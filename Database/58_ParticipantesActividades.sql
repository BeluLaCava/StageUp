IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Participante', N'U') IS NULL OR OBJECT_ID(N'dbo.ActividadParticipante', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/26_ActividadesYNotificaciones.sql.', 1;
END
GO

-- ---------------------------------------------------------------------------
-- CU-001-010: datos de contacto del participante y fechas de modificación y
-- baja. El DNI y el correo no se pueden repetir entre los participantes
-- ACTIVOS del gestor (un participante dado de baja no bloquea su DNI).
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Participante', N'correo') IS NULL
    ALTER TABLE dbo.Participante ADD correo NVARCHAR(254) NULL;
GO
IF COL_LENGTH(N'dbo.Participante', N'telefono') IS NULL
    ALTER TABLE dbo.Participante ADD telefono NVARCHAR(30) NULL;
GO
IF COL_LENGTH(N'dbo.Participante', N'fechaUltimaModificacion') IS NULL
    ALTER TABLE dbo.Participante ADD fechaUltimaModificacion DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.Participante', N'fechaBaja') IS NULL
    ALTER TABLE dbo.Participante ADD fechaBaja DATETIME NULL;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Participante_DniPorGestor')
    ALTER TABLE dbo.Participante DROP CONSTRAINT UQ_Participante_DniPorGestor;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Participante_DniActivo' AND object_id = OBJECT_ID(N'dbo.Participante'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Participante_DniActivo
        ON dbo.Participante (idUsuarioGestor, dni) WHERE activo = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Participante_CorreoActivo' AND object_id = OBJECT_ID(N'dbo.Participante'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Participante_CorreoActivo
        ON dbo.Participante (idUsuarioGestor, correo) WHERE activo = 1 AND correo IS NOT NULL;
GO

-- ---------------------------------------------------------------------------
-- La asociación con una actividad pasa a ser lógica: desvincular no borra la
-- fila, la marca como inactiva con su fecha (historial de asociaciones).
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.ActividadParticipante', N'activa') IS NULL
    ALTER TABLE dbo.ActividadParticipante ADD activa BIT NOT NULL
        CONSTRAINT DF_ActividadParticipante_activa DEFAULT (1);
GO
IF COL_LENGTH(N'dbo.ActividadParticipante', N'fechaDesvinculacion') IS NULL
    ALTER TABLE dbo.ActividadParticipante ADD fechaDesvinculacion DATETIME NULL;
GO

-- ---------------------------------------------------------------------------
-- Participante
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Participante_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_Insertar;
GO
CREATE PROCEDURE dbo.sp_Participante_Insertar
    @idUsuarioGestor INT,
    @nombre          NVARCHAR(120),
    @apellido        NVARCHAR(120),
    @dni             NVARCHAR(20),
    @correo          NVARCHAR(254) = NULL,
    @telefono        NVARCHAR(30)  = NULL,
    @notas           NVARCHAR(800) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.Participante (idUsuarioGestor, nombre, apellido, dni, correo, telefono, notas)
        VALUES (@idUsuarioGestor, @nombre, @apellido, @dni, @correo, @telefono, @notas);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS idParticipante;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Participante_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_Modificar;
GO
CREATE PROCEDURE dbo.sp_Participante_Modificar
    @idParticipante INT,
    @nombre         NVARCHAR(120),
    @apellido       NVARCHAR(120),
    @dni            NVARCHAR(20),
    @correo         NVARCHAR(254) = NULL,
    @telefono       NVARCHAR(30)  = NULL,
    @notas          NVARCHAR(800) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Participante
        SET nombre = @nombre,
            apellido = @apellido,
            dni = @dni,
            correo = @correo,
            telefono = @telefono,
            notas = @notas,
            fechaUltimaModificacion = GETDATE()
        WHERE idParticipante = @idParticipante;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Participante_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_DarDeBaja;
GO
CREATE PROCEDURE dbo.sp_Participante_DarDeBaja
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.Participante
        SET activo = 0,
            fechaBaja = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idParticipante = @idParticipante
          AND activo = 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Participante_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Participante_ObtenerPorId
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idParticipante, idUsuarioGestor, nombre, apellido, dni, correo, telefono, notas, activo,
           fechaCreacion, fechaUltimaModificacion, fechaBaja
    FROM dbo.Participante
    WHERE idParticipante = @idParticipante;
END
GO

IF OBJECT_ID('dbo.sp_Participante_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_Participante_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idParticipante, idUsuarioGestor, nombre, apellido, dni, correo, telefono, notas, activo,
           fechaCreacion, fechaUltimaModificacion, fechaBaja
    FROM dbo.Participante
    WHERE idUsuarioGestor = @idUsuarioGestor
      AND activo = 1
    ORDER BY apellido, nombre;
END
GO

-- Listado de la solapa Participantes con filtro de estado y búsqueda por
-- nombre, apellido, DNI o correo (el filtro se aplica en la consulta).
IF OBJECT_ID('dbo.sp_Participante_Buscar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_Buscar;
GO
CREATE PROCEDURE dbo.sp_Participante_Buscar
    @idUsuarioGestor INT,
    @estado          NVARCHAR(20)  = N'Activos',   -- Activos | Inactivos | Todos
    @texto           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idParticipante, idUsuarioGestor, nombre, apellido, dni, correo, telefono, notas, activo,
           fechaCreacion, fechaUltimaModificacion, fechaBaja
    FROM dbo.Participante
    WHERE idUsuarioGestor = @idUsuarioGestor
      AND (@estado = N'Todos'
           OR (@estado = N'Activos' AND activo = 1)
           OR (@estado = N'Inactivos' AND activo = 0))
      AND (@texto IS NULL
           OR nombre LIKE N'%' + @texto + N'%' ESCAPE N'\'
           OR apellido LIKE N'%' + @texto + N'%' ESCAPE N'\'
           OR (nombre + N' ' + apellido) LIKE N'%' + @texto + N'%' ESCAPE N'\'
           OR dni LIKE N'%' + @texto + N'%' ESCAPE N'\'
           OR correo LIKE N'%' + @texto + N'%' ESCAPE N'\')
    ORDER BY activo DESC, apellido, nombre;
END
GO

-- Otro participante ACTIVO del gestor con el mismo DNI o el mismo correo.
IF OBJECT_ID('dbo.sp_Participante_BuscarDuplicado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Participante_BuscarDuplicado;
GO
CREATE PROCEDURE dbo.sp_Participante_BuscarDuplicado
    @idUsuarioGestor        INT,
    @dni                    NVARCHAR(20),
    @correo                 NVARCHAR(254) = NULL,
    @idParticipanteExcluido INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 idParticipante, idUsuarioGestor, nombre, apellido, dni, correo, telefono, notas, activo,
           fechaCreacion, fechaUltimaModificacion, fechaBaja,
           CASE WHEN dni = @dni THEN N'Dni' ELSE N'Correo' END AS coincidencia
    FROM dbo.Participante
    WHERE idUsuarioGestor = @idUsuarioGestor
      AND activo = 1
      AND (@idParticipanteExcluido IS NULL OR idParticipante <> @idParticipanteExcluido)
      AND (dni = @dni OR (@correo IS NOT NULL AND correo = @correo))
    ORDER BY CASE WHEN dni = @dni THEN 0 ELSE 1 END;
END
GO

-- ---------------------------------------------------------------------------
-- ActividadParticipante (asociación lógica)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_ActividadParticipante_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_Insertar;
GO
CREATE PROCEDURE dbo.sp_ActividadParticipante_Insertar
    @idActividad    INT,
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM dbo.ActividadParticipante WHERE idActividad = @idActividad AND idParticipante = @idParticipante)
        BEGIN
            -- Se vuelve a asociar a alguien que había sido desvinculado.
            UPDATE dbo.ActividadParticipante
            SET activa = 1,
                fechaAsociacion = CASE WHEN activa = 1 THEN fechaAsociacion ELSE GETDATE() END,
                fechaDesvinculacion = NULL
            WHERE idActividad = @idActividad AND idParticipante = @idParticipante;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.ActividadParticipante (idActividad, idParticipante, activa)
            VALUES (@idActividad, @idParticipante, 1);
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_ActividadParticipante_Eliminar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_Eliminar;
GO
CREATE PROCEDURE dbo.sp_ActividadParticipante_Eliminar
    @idActividad    INT,
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.ActividadParticipante
        SET activa = 0,
            fechaDesvinculacion = GETDATE()
        WHERE idActividad = @idActividad
          AND idParticipante = @idParticipante
          AND activa = 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_ActividadParticipante_ListarPorActividad', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_ListarPorActividad;
GO
CREATE PROCEDURE dbo.sp_ActividadParticipante_ListarPorActividad
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.idParticipante, p.nombre, p.apellido, p.dni, p.correo, p.telefono, p.notas, p.activo, ap.fechaAsociacion
    FROM dbo.ActividadParticipante ap
    INNER JOIN dbo.Participante p ON p.idParticipante = ap.idParticipante
    WHERE ap.idActividad = @idActividad
      AND ap.activa = 1
      AND p.activo = 1
    ORDER BY p.apellido, p.nombre;
END
GO

IF OBJECT_ID('dbo.sp_ActividadParticipante_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_ActividadParticipante_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ap.idActividad, p.idParticipante, p.nombre, p.apellido, p.dni, p.correo, p.telefono, p.notas, p.activo, ap.fechaAsociacion
    FROM dbo.ActividadParticipante ap
    INNER JOIN dbo.Participante p ON p.idParticipante = ap.idParticipante
    INNER JOIN dbo.Actividad a ON a.idActividad = ap.idActividad
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = a.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND ap.activa = 1
      AND p.activo = 1;
END
GO

-- Actividades del participante: las vigentes y el historial (desvinculadas o
-- de actividades dadas de baja).
IF OBJECT_ID('dbo.sp_ActividadParticipante_ListarPorParticipante', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_ListarPorParticipante;
GO
CREATE PROCEDURE dbo.sp_ActividadParticipante_ListarPorParticipante
    @idParticipante INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.idActividad, a.nombre, a.idEspacioArtistico, e.nombreEspacio, a.activa AS actividadActiva,
           ap.activa AS asociacionActiva, ap.fechaAsociacion, ap.fechaDesvinculacion
    FROM dbo.ActividadParticipante ap
    INNER JOIN dbo.Actividad a ON a.idActividad = ap.idActividad
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = a.idEspacioArtistico
    WHERE ap.idParticipante = @idParticipante
    ORDER BY ap.activa DESC, a.activa DESC, ap.fechaAsociacion DESC;
END
GO
