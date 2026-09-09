IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID('dbo.EspacioArtistico', 'U') IS NOT NULL
    DROP TABLE dbo.EspacioArtistico;
GO

CREATE TABLE dbo.EspacioArtistico
(
    idEspacioArtistico      INT IDENTITY(1,1) NOT NULL,
    idUsuarioGestor         INT NOT NULL,
    nombreEspacio           NVARCHAR(300) NOT NULL,
    descripcion             NVARCHAR(2000) NULL,
    tipoEspacio             NVARCHAR(200) NOT NULL,
    estadoEspacio           NVARCHAR(100) NOT NULL,
    publicado               BIT NOT NULL CONSTRAINT DF_EspacioArtistico_publicado DEFAULT (0),
    activo                  BIT NOT NULL CONSTRAINT DF_EspacioArtistico_activo DEFAULT (1),
    fechaAlta               DATETIME NOT NULL CONSTRAINT DF_EspacioArtistico_fechaAlta DEFAULT (GETDATE()),
    fechaPublicacion        DATETIME NULL,
    fechaBaja               DATETIME NULL,
    fechaUltimaModificacion DATETIME NULL,
    CONSTRAINT PK_EspacioArtistico PRIMARY KEY CLUSTERED (idEspacioArtistico ASC),
    CONSTRAINT FK_EspacioArtistico_UsuarioExterno FOREIGN KEY (idUsuarioGestor)
        REFERENCES dbo.UsuarioExterno (idUsuarioExterno)
);
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_Insertar', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_Insertar;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_Insertar
    @idUsuarioGestor INT,
    @nombreEspacio   NVARCHAR(300),
    @descripcion     NVARCHAR(2000) = NULL,
    @tipoEspacio     NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.EspacioArtistico
            (idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio, estadoEspacio, publicado, activo, fechaAlta)
        VALUES
            (@idUsuarioGestor, @nombreEspacio, @descripcion, @tipoEspacio, N'Borrador', 0, 1, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS idEspacioArtistico;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_Modificar', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_Modificar;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_Modificar
    @idEspacioArtistico INT,
    @nombreEspacio      NVARCHAR(300),
    @descripcion        NVARCHAR(2000) = NULL,
    @tipoEspacio        NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.EspacioArtistico
        SET nombreEspacio           = @nombreEspacio,
            descripcion             = @descripcion,
            tipoEspacio             = @tipoEspacio,
            fechaUltimaModificacion = GETDATE()
        WHERE idEspacioArtistico = @idEspacioArtistico
          AND activo = 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_ObtenerPorId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_ObtenerPorId;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_ObtenerPorId
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idEspacioArtistico, idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio,
           estadoEspacio, publicado, activo, fechaAlta, fechaPublicacion, fechaBaja, fechaUltimaModificacion
    FROM dbo.EspacioArtistico
    WHERE idEspacioArtistico = @idEspacioArtistico;
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_ListarPorUsuarioGestor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_ListarPorUsuarioGestor;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idEspacioArtistico, idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio,
           estadoEspacio, publicado, activo, fechaAlta, fechaPublicacion, fechaBaja, fechaUltimaModificacion
    FROM dbo.EspacioArtistico
    WHERE idUsuarioGestor = @idUsuarioGestor
      AND activo = 1
    ORDER BY fechaAlta DESC;
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_ListarPublicados', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_ListarPublicados;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_ListarPublicados
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idEspacioArtistico, idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio,
           estadoEspacio, publicado, activo, fechaAlta, fechaPublicacion, fechaBaja, fechaUltimaModificacion
    FROM dbo.EspacioArtistico
    WHERE activo = 1
      AND publicado = 1
    ORDER BY fechaPublicacion DESC;
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_Publicar', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_Publicar;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_Publicar
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.EspacioArtistico
        SET publicado               = 1,
            estadoEspacio           = N'Publicado',
            fechaPublicacion        = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idEspacioArtistico = @idEspacioArtistico
          AND activo = 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_Pausar', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_Pausar;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_Pausar
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.EspacioArtistico
        SET publicado               = 0,
            estadoEspacio           = N'Pausado',
            fechaUltimaModificacion = GETDATE()
        WHERE idEspacioArtistico = @idEspacioArtistico
          AND activo = 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_BajaLogica', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_BajaLogica;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_BajaLogica
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.EspacioArtistico
        SET activo                  = 0,
            publicado               = 0,
            estadoEspacio           = N'Dado de baja',
            fechaBaja               = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idEspacioArtistico = @idEspacioArtistico
          AND activo = 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
