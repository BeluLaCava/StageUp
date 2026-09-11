IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO


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

IF OBJECT_ID('dbo.sp_EspacioArtistico_ListarPublicadosV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_ListarPublicadosV2;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_ListarPublicadosV2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
        e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
        f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento,
        g.nombre AS nombreGestor, g.apellido AS apellidoGestor,
        g.fechaActivacion AS gestorFechaActivacion, g.fechaAlta AS gestorFechaAlta,
        (SELECT COUNT(*) FROM dbo.EspacioArtistico e2
            WHERE e2.idUsuarioGestor = e.idUsuarioGestor AND e2.activo = 1 AND e2.publicado = 1) AS cantidadEspaciosPublicadosGestor
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE e.activo = 1
      AND e.publicado = 1
    ORDER BY e.fechaPublicacion DESC;
END
GO
