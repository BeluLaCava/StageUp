IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- Tanda 4: mostrar nombre del gestor y una "reputación" liviana en el catálogo
-- público y en el detalle del espacio. Todavía no existe ningún sistema de
-- calificaciones/reseñas (queda para el Avance 2, CU-001-006), así que en vez de
-- inventar un promedio de estrellas se muestra información que ya existe: desde
-- cuándo es gestor en la plataforma (fechaActivacion de UsuarioExterno, o
-- fechaAlta si todavía no se activó) y cuántos espacios propios tiene publicados
-- ahora mismo. No se crea ninguna tabla nueva ni se migra nada.
--
-- Este script solo reemplaza dos procedimientos ya creados por
-- Database/09_FichaEspacio.sql (sp_EspacioArtistico_ObtenerPorIdV2 y
-- sp_EspacioArtistico_ListarPublicadosV2), agregando un JOIN a UsuarioExterno y
-- una subconsulta de conteo. Hay que haber corrido 09_FichaEspacio.sql antes.
-- sp_EspacioArtistico_ListarPorUsuarioGestorV2 (la de "Mis espacios") no se toca:
-- ahí el gestor ya sabe que es el dueño, no hace falta mostrárselo.

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
