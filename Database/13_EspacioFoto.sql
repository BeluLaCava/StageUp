IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- =============================================================================
-- 13_EspacioFoto.sql
-- ADVERTENCIA: SCRIPT INICIAL DESTRUCTIVO (contiene DROP TABLE).
--
-- Sirve para armar la base desde cero. NO debe ejecutarse a mano sobre una base
-- con datos: borraría estas tablas y todo su contenido: EspacioFoto.
-- El flujo normal de instalación y actualización es Database/EjecutarTodosLosScripts.ps1,
-- que usa dbo._ScriptsEjecutados y nunca vuelve a correr un script ya aplicado.
-- Ver Database/LEEME_Scripts.md.
--
-- Protección: si alguna de esas tablas ya tiene filas, el script se detiene acá
-- con un error y no ejecuta nada más (SET NOEXEC ON). En una base nueva las tablas
-- no existen o están vacías, así que sigue normalmente.
-- =============================================================================
IF EXISTS
(
    SELECT 1
    FROM sys.partitions p
    WHERE p.index_id IN (0, 1)
      AND p.rows > 0
      AND p.object_id IN
      (
        OBJECT_ID(N'dbo.EspacioFoto', N'U')
      )
)
BEGIN
    RAISERROR(N'13_EspacioFoto.sql es un script inicial destructivo y la base ya tiene datos en sus tablas. No se ejecutó nada. Para actualizar una base existente usá EjecutarTodosLosScripts.ps1.', 16, 1);
    SET NOEXEC ON;
END
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

-- Deja la sesión como estaba si la protección de arriba frenó el script.
SET NOEXEC OFF;
GO
