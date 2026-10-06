IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF COL_LENGTH(N'dbo.FichaEspacio', N'superficieM2') IS NULL
    ALTER TABLE dbo.FichaEspacio ADD superficieM2 DECIMAL(8,2) NULL;
GO
IF COL_LENGTH(N'dbo.FichaEspacio', N'alturaM') IS NULL
    ALTER TABLE dbo.FichaEspacio ADD alturaM DECIMAL(5,2) NULL;
GO
IF COL_LENGTH(N'dbo.FichaEspacio', N'condicionesUso') IS NULL
    ALTER TABLE dbo.FichaEspacio ADD condicionesUso NVARCHAR(1000) NULL;
GO
IF COL_LENGTH(N'dbo.FichaEspacio', N'reglasUso') IS NULL
    ALTER TABLE dbo.FichaEspacio ADD reglasUso NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FichaEspacio_medidas')
    ALTER TABLE dbo.FichaEspacio ADD CONSTRAINT CK_FichaEspacio_medidas
        CHECK ((superficieM2 IS NULL OR superficieM2 > 0) AND (alturaM IS NULL OR alturaM > 0));
GO

-- ---------------------------------------------------------------------------
-- Guardado de la ficha (mismo comportamiento que el script 41, con los datos
-- nuevos)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_FichaEspacio_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_Guardar;
GO
CREATE PROCEDURE dbo.sp_FichaEspacio_Guardar
    @idEspacioArtistico     INT,
    @fotoRuta               NVARCHAR(300)   = NULL,
    @provincia              NVARCHAR(100),
    @ciudad                 NVARCHAR(150),
    @direccion              NVARCHAR(300),
    @capacidadMaxima        INT,
    @precioHora             DECIMAL(10,2),
    @moneda                 NVARCHAR(3),
    @tipoPiso               NVARCHAR(100)   = NULL,
    @detalleEquipamiento    NVARCHAR(1000)  = NULL,
    @superficieM2           DECIMAL(8,2)    = NULL,
    @alturaM                DECIMAL(5,2)    = NULL,
    @condicionesUso         NVARCHAR(1000)  = NULL,
    @reglasUso              NVARCHAR(1000)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM dbo.FichaEspacio WHERE idEspacioArtistico = @idEspacioArtistico)
        BEGIN
            UPDATE dbo.FichaEspacio
            SET fotoRuta = @fotoRuta,
                provincia = @provincia,
                ciudad = @ciudad,
                direccion = @direccion,
                capacidadMaxima = @capacidadMaxima,
                precioHora = @precioHora,
                moneda = @moneda,
                tipoPiso = @tipoPiso,
                detalleEquipamiento = @detalleEquipamiento,
                superficieM2 = @superficieM2,
                alturaM = @alturaM,
                condicionesUso = @condicionesUso,
                reglasUso = @reglasUso,
                fechaUltimaModificacion = GETDATE()
            WHERE idEspacioArtistico = @idEspacioArtistico;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.FichaEspacio
                (idEspacioArtistico, fotoRuta, provincia, ciudad, direccion, capacidadMaxima,
                 precioHora, moneda, tipoPiso, detalleEquipamiento, superficieM2, alturaM,
                 condicionesUso, reglasUso, fechaUltimaModificacion)
            VALUES
                (@idEspacioArtistico, @fotoRuta, @provincia, @ciudad, @direccion, @capacidadMaxima,
                 @precioHora, @moneda, @tipoPiso, @detalleEquipamiento, @superficieM2, @alturaM,
                 @condicionesUso, @reglasUso, GETDATE());
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ---------------------------------------------------------------------------
-- Lecturas con los datos nuevos (detalle y listado del gestor)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_EspacioArtistico_ObtenerPorIdV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_ObtenerPorIdV2;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_ObtenerPorIdV2
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
        e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
        f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento,
        f.superficieM2, f.alturaM, f.condicionesUso, f.reglasUso,
        g.nombre AS nombreGestor, g.apellido AS apellidoGestor,
        g.fechaActivacion AS gestorFechaActivacion, g.fechaAlta AS gestorFechaAlta,
        (SELECT COUNT(*) FROM dbo.EspacioArtistico e2
            WHERE e2.idUsuarioGestor = e.idUsuarioGestor AND e2.activo = 1 AND e2.publicado = 1) AS cantidadEspaciosPublicadosGestor
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE e.idEspacioArtistico = @idEspacioArtistico;
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_ListarPorUsuarioGestorV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_ListarPorUsuarioGestorV2;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_ListarPorUsuarioGestorV2
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
        e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
        f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento,
        f.superficieM2, f.alturaM, f.condicionesUso, f.reglasUso
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1
    ORDER BY e.fechaAlta DESC;
END
GO

-- ---------------------------------------------------------------------------
-- Precios de referencia para "Sugerir valores" (A13)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_EspacioArtistico_ListarPreciosReferencia', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_ListarPreciosReferencia;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_ListarPreciosReferencia
    @moneda                 NVARCHAR(3),
    @idEspacioExcluido      INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.idEspacioArtistico, e.tipoEspacio, f.provincia, f.ciudad, f.capacidadMaxima, f.superficieM2, f.precioHora
    FROM dbo.EspacioArtistico e
    INNER JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1
      AND f.moneda = @moneda
      AND f.precioHora > 0
      AND (@idEspacioExcluido IS NULL OR e.idEspacioArtistico <> @idEspacioExcluido);
END
GO
