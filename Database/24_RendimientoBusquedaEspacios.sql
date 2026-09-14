IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.EspacioArtistico', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/02_EspacioArtistico.sql.', 1;
END
GO

IF OBJECT_ID('dbo.sp_FichaEspacioEquipamiento_ListarPublicadosPorIds', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPublicadosPorIds;
GO
CREATE PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPublicadosPorIds
    @idsEspacios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT fe.idEspacioArtistico, fe.codigoEquipamiento
    FROM dbo.FichaEspacioEquipamiento fe
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = fe.idEspacioArtistico
    INNER JOIN STRING_SPLIT(@idsEspacios, ',') s ON TRY_CONVERT(INT, s.value) = fe.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1;
END
GO

IF OBJECT_ID('dbo.sp_FranjaEspacio_ListarPublicadosPorIds', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_ListarPublicadosPorIds;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_ListarPublicadosPorIds
    @idsEspacios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idEspacioArtistico, f.diaSemana, f.fecha, f.minutoDesde, f.minutoHasta, f.bloqueado
    FROM dbo.FranjaEspacio f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    INNER JOIN STRING_SPLIT(@idsEspacios, ',') s ON TRY_CONVERT(INT, s.value) = f.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1;
END
GO

IF OBJECT_ID('dbo.sp_EspacioFoto_ListarPublicadosPorIds', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioFoto_ListarPublicadosPorIds;
GO
CREATE PROCEDURE dbo.sp_EspacioFoto_ListarPublicadosPorIds
    @idsEspacios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ef.idEspacioArtistico, ef.rutaFoto, ef.orden, ef.esPrincipal
    FROM dbo.EspacioFoto ef
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = ef.idEspacioArtistico
    INNER JOIN STRING_SPLIT(@idsEspacios, ',') s ON TRY_CONVERT(INT, s.value) = ef.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1
    ORDER BY ef.idEspacioArtistico, ef.orden ASC;
END
GO
