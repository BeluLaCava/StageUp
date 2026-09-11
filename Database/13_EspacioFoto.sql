IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO


IF OBJECT_ID('dbo.EspacioFoto', 'U') IS NOT NULL DROP TABLE dbo.EspacioFoto;
GO

CREATE TABLE dbo.EspacioFoto
(
    idEspacioFoto       INT IDENTITY(1,1) NOT NULL,
    idEspacioArtistico  INT             NOT NULL,
    rutaFoto            NVARCHAR(300)   NOT NULL,
    orden               INT             NOT NULL,
    esPrincipal         BIT             NOT NULL CONSTRAINT DF_EspacioFoto_esPrincipal DEFAULT (0),
    CONSTRAINT PK_EspacioFoto PRIMARY KEY CLUSTERED (idEspacioFoto ASC),
    CONSTRAINT FK_EspacioFoto_FichaEspacio FOREIGN KEY (idEspacioArtistico)
        REFERENCES dbo.FichaEspacio (idEspacioArtistico) ON DELETE CASCADE,
    CONSTRAINT CK_EspacioFoto_orden CHECK (orden >= 0)
);
GO

IF OBJECT_ID('dbo.sp_EspacioFoto_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioFoto_Insertar;
GO
CREATE PROCEDURE dbo.sp_EspacioFoto_Insertar
    @idEspacioArtistico INT,
    @rutaFoto           NVARCHAR(300),
    @orden               INT,
    @esPrincipal        BIT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.EspacioFoto (idEspacioArtistico, rutaFoto, orden, esPrincipal)
        VALUES (@idEspacioArtistico, @rutaFoto, @orden, @esPrincipal);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_EspacioFoto_ListarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioFoto_ListarPorEspacio;
GO
CREATE PROCEDURE dbo.sp_EspacioFoto_ListarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idEspacioArtistico, rutaFoto, orden, esPrincipal
    FROM dbo.EspacioFoto
    WHERE idEspacioArtistico = @idEspacioArtistico
    ORDER BY orden ASC;
END
GO

IF OBJECT_ID('dbo.sp_EspacioFoto_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioFoto_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_EspacioFoto_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ef.idEspacioArtistico, ef.rutaFoto, ef.orden, ef.esPrincipal
    FROM dbo.EspacioFoto ef
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = ef.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1
    ORDER BY ef.idEspacioArtistico, ef.orden ASC;
END
GO

IF OBJECT_ID('dbo.sp_EspacioFoto_ListarPublicados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioFoto_ListarPublicados;
GO
CREATE PROCEDURE dbo.sp_EspacioFoto_ListarPublicados
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ef.idEspacioArtistico, ef.rutaFoto, ef.orden, ef.esPrincipal
    FROM dbo.EspacioFoto ef
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = ef.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1
    ORDER BY ef.idEspacioArtistico, ef.orden ASC;
END
GO
