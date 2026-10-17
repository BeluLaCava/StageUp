IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Novedad', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/30_NovedadesAbmYPermiso.sql.', 1;
END
GO

IF COL_LENGTH(N'dbo.Novedad', N'fechaVencimiento') IS NULL
    ALTER TABLE dbo.Novedad ADD fechaVencimiento DATETIME NULL;
GO

IF COL_LENGTH(N'dbo.Novedad', N'fechaBaja') IS NULL
    ALTER TABLE dbo.Novedad ADD fechaBaja DATETIME NULL;
GO

IF COL_LENGTH(N'dbo.Novedad', N'idUsuarioInternoBaja') IS NULL
    ALTER TABLE dbo.Novedad ADD idUsuarioInternoBaja INT NULL
        CONSTRAINT FK_Novedad_UsuarioInternoBaja FOREIGN KEY REFERENCES dbo.UsuarioInterno (idUsuarioInterno);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Novedad_Vigencia')
    ALTER TABLE dbo.Novedad ADD CONSTRAINT CK_Novedad_Vigencia
        CHECK (fechaVencimiento IS NULL OR fechaPublicacion IS NULL OR fechaVencimiento > fechaPublicacion);
GO

IF OBJECT_ID(N'dbo.vw_Novedad', N'V') IS NOT NULL DROP VIEW dbo.vw_Novedad;
GO
CREATE VIEW dbo.vw_Novedad
AS
SELECT n.idNovedad, n.titulo, n.resumen, n.contenido, n.categoria, n.urlImagen, n.publicado, n.fechaPublicacion,
       n.fechaVencimiento, n.enviadaPorCorreo, n.destinatarioNewsletter, n.fechaEnvioNewsletter, n.fechaAlta,
       n.fechaUltimaModificacion, n.activo, n.fechaBaja,
       CASE
           WHEN n.activo = 0 THEN N'DadaDeBaja'
           WHEN n.publicado = 0 THEN N'Borrador'
           WHEN n.fechaPublicacion > GETDATE() THEN N'Programada'
           WHEN n.fechaVencimiento IS NOT NULL AND n.fechaVencimiento <= GETDATE() THEN N'Vencida'
           ELSE N'Vigente'
       END AS estado
FROM dbo.Novedad n;
GO

-- Panel de administración: todas menos las dadas de baja (o solo un estado).
IF OBJECT_ID('dbo.sp_Novedad_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_Listar;
GO
CREATE PROCEDURE dbo.sp_Novedad_Listar
    @estado NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.vw_Novedad
    WHERE (@estado IS NULL AND estado <> N'DadaDeBaja') OR estado = @estado
    ORDER BY fechaAlta DESC;
END
GO

-- Página pública: solo las vigentes.
IF OBJECT_ID('dbo.sp_Novedad_ListarPublicadas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_ListarPublicadas;
GO
CREATE PROCEDURE dbo.sp_Novedad_ListarPublicadas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.vw_Novedad
    WHERE estado = N'Vigente'
    ORDER BY fechaPublicacion DESC;
END
GO

IF OBJECT_ID('dbo.sp_Novedad_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Novedad_ObtenerPorId
    @idNovedad INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.vw_Novedad WHERE idNovedad = @idNovedad;
END
GO

IF OBJECT_ID('dbo.sp_Novedad_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_Insertar;
GO
CREATE PROCEDURE dbo.sp_Novedad_Insertar
    @titulo             NVARCHAR(180),
    @resumen            NVARCHAR(300),
    @contenido          NVARCHAR(MAX),
    @categoria          NVARCHAR(100),
    @urlImagen          NVARCHAR(500),
    @publicado          BIT,
    @fechaPublicacion   DATETIME,
    @fechaVencimiento   DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Novedad (titulo, resumen, contenido, categoria, urlImagen, publicado, fechaPublicacion, fechaVencimiento, fechaAlta)
    VALUES (@titulo, @resumen, @contenido, @categoria, @urlImagen, @publicado, @fechaPublicacion, @fechaVencimiento, GETDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idNovedad;
END
GO

IF OBJECT_ID('dbo.sp_Novedad_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_Modificar;
GO
CREATE PROCEDURE dbo.sp_Novedad_Modificar
    @idNovedad          INT,
    @titulo             NVARCHAR(180),
    @resumen            NVARCHAR(300),
    @contenido          NVARCHAR(MAX),
    @categoria          NVARCHAR(100),
    @urlImagen          NVARCHAR(500),
    @publicado          BIT,
    @fechaPublicacion   DATETIME,
    @fechaVencimiento   DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Novedad WHERE idNovedad = @idNovedad AND activo = 1)
    BEGIN
        THROW 51201, 'No se encontró la novedad indicada.', 1;
    END

    UPDATE dbo.Novedad
    SET titulo = @titulo,
        resumen = @resumen,
        contenido = @contenido,
        categoria = @categoria,
        urlImagen = @urlImagen,
        publicado = @publicado,
        fechaPublicacion = @fechaPublicacion,
        fechaVencimiento = @fechaVencimiento,
        fechaUltimaModificacion = GETDATE()
    WHERE idNovedad = @idNovedad;
END
GO

-- Baja lógica: deja de verse en todos lados pero queda en el historial.
IF OBJECT_ID('dbo.sp_Novedad_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_DarDeBaja;
GO
CREATE PROCEDURE dbo.sp_Novedad_DarDeBaja
    @idNovedad              INT,
    @idUsuarioInternoBaja   INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Novedad
    SET activo = 0,
        fechaBaja = GETDATE(),
        idUsuarioInternoBaja = @idUsuarioInternoBaja,
        fechaUltimaModificacion = GETDATE()
    WHERE idNovedad = @idNovedad AND activo = 1;

    SELECT @@ROWCOUNT;
END
GO

-- Una novedad dada de baja vuelve como borrador (hay que revisarla y
-- publicarla de nuevo).
IF OBJECT_ID('dbo.sp_Novedad_Restaurar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_Restaurar;
GO
CREATE PROCEDURE dbo.sp_Novedad_Restaurar
    @idNovedad INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Novedad
    SET activo = 1,
        publicado = 0,
        fechaBaja = NULL,
        idUsuarioInternoBaja = NULL,
        fechaUltimaModificacion = GETDATE()
    WHERE idNovedad = @idNovedad AND activo = 0;

    SELECT @@ROWCOUNT;
END
GO

-- Búsqueda pública: las novedades solo aparecen mientras están vigentes.
IF OBJECT_ID('dbo.sp_Busqueda_Publica', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Publica;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Publica
    @texto            NVARCHAR(300),
    @incluirEspacios  BIT = 1,
    @incluirNovedades BIT = 1,
    @incluirFaq       BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(310) = N'%' + @texto + N'%';

    SELECT tipo, idReferencia, titulo, detalle, fecha
    FROM
    (
        SELECT TOP 25 N'Espacio' AS tipo, e.idEspacioArtistico AS idReferencia, e.nombreEspacio AS titulo,
               e.tipoEspacio + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N'') + ISNULL(N' · ' + LEFT(e.descripcion, 200), N'') AS detalle,
               e.fechaPublicacion AS fecha,
               CASE WHEN e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END AS prioridad
        FROM dbo.EspacioArtistico e
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE @incluirEspacios = 1
          AND e.activo = 1 AND e.publicado = 1
          AND (e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron
               OR f.ciudad LIKE @patron OR f.provincia LIKE @patron)
        ORDER BY prioridad, e.fechaPublicacion DESC
    ) espacios
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha
    FROM
    (
        SELECT TOP 25 N'Novedad' AS tipo, n.idNovedad AS idReferencia, n.titulo,
               n.categoria + N' · ' + n.resumen AS detalle,
               ISNULL(n.fechaPublicacion, n.fechaAlta) AS fecha,
               CASE WHEN n.titulo LIKE @patron THEN 0 ELSE 1 END AS prioridad
        FROM dbo.vw_Novedad n
        WHERE @incluirNovedades = 1
          AND n.estado = N'Vigente'
          AND (n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron)
        ORDER BY prioridad, ISNULL(n.fechaPublicacion, n.fechaAlta) DESC
    ) novedades
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha
    FROM
    (
        SELECT TOP 25 N'FAQ' AS tipo, q.idFaq AS idReferencia, q.pregunta AS titulo,
               LEFT(q.respuesta, 300) AS detalle,
               CAST(NULL AS DATETIME) AS fecha,
               CASE WHEN q.pregunta LIKE @patron THEN 0 ELSE 1 END AS prioridad
        FROM dbo.Faq q
        WHERE @incluirFaq = 1
          AND q.activo = 1
          AND (q.pregunta LIKE @patron OR q.respuesta LIKE @patron)
        ORDER BY prioridad, q.orden
    ) faqs;
END
GO
