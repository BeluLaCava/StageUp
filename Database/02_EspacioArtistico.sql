-- =====================================================================
-- StageUp - Avance 1 - ABMC de EspacioArtistico
-- =====================================================================
-- Entidad core del negocio, según el diccionario de datos (10.7.4) de
-- StageUp_Tecnico.docx y el caso de uso CU-001-007 "Gestionar espacios
-- artisticos". Se implementan en esta entrega las columnas propias de
-- EspacioArtistico (alta, modificacion, publicar/pausar, baja logica y
-- consulta). Las entidades relacionadas que menciona el CU (ubicacion,
-- caracteristicas fisicas, condiciones de uso, valores, equipamiento,
-- imagenes) tienen sus propias tablas en el diccionario de datos pero
-- se dejan para una proxima etapa (Avance 2 - core del negocio), para
-- no sobrecargar esta entrega con una funcionalidad mucho mas amplia
-- que un ABMC basico.
--
-- Supuesto (no definido explicitamente en la documentacion, a validar):
-- CU-001-007 tambien describe una "habilitacion como gestor de espacios"
-- con revision/aprobacion antes de poder publicar espacios. Esa revision
-- aparece ademas como requerimiento aparte en el catalogo de RQF
-- ("revision de habilitacion como gestor"), por lo que se interpreta
-- como una funcionalidad propia, todavia no implementada. Por ahora,
-- para poder probar el ABMC de punta a punta, CUALQUIER usuario externo
-- autenticado puede dar de alta y administrar sus propios espacios.
-- =====================================================================

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

-- =====================================================================
-- Alta de un espacio artistico nuevo. Se crea siempre en estado
-- "Borrador" y sin publicar; el gestor lo publica despues con una
-- accion aparte (sp_EspacioArtistico_Publicar).
-- =====================================================================
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

    INSERT INTO dbo.EspacioArtistico
        (idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio, estadoEspacio, publicado, activo, fechaAlta)
    VALUES
        (@idUsuarioGestor, @nombreEspacio, @descripcion, @tipoEspacio, N'Borrador', 0, 1, GETDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idEspacioArtistico;
END
GO

-- =====================================================================
-- Modificacion de los datos propios del espacio (no cambia estado ni
-- publicacion, eso se hace con las acciones dedicadas de abajo).
-- Solo puede modificarse un espacio que no fue dado de baja.
-- =====================================================================
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

    UPDATE dbo.EspacioArtistico
    SET nombreEspacio           = @nombreEspacio,
        descripcion             = @descripcion,
        tipoEspacio             = @tipoEspacio,
        fechaUltimaModificacion = GETDATE()
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND activo = 1;
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

-- =====================================================================
-- Listado de "Mis espacios" para un gestor (incluye borradores, pausados
-- y publicados, pero no los dados de baja).
-- =====================================================================
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

-- =====================================================================
-- Listado publico de espacios publicados (para el catalogo publico que
-- se va a implementar como proximo paso del Avance 1).
-- =====================================================================
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

    UPDATE dbo.EspacioArtistico
    SET publicado               = 1,
        estadoEspacio           = N'Publicado',
        fechaPublicacion        = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND activo = 1;
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

    UPDATE dbo.EspacioArtistico
    SET publicado               = 0,
        estadoEspacio           = N'Pausado',
        fechaUltimaModificacion = GETDATE()
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND activo = 1;
END
GO

-- =====================================================================
-- Baja logica: nunca se elimina fisicamente el registro (asi lo exige
-- el CU-001-007 explicitamente).
-- =====================================================================
IF OBJECT_ID('dbo.sp_EspacioArtistico_BajaLogica', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EspacioArtistico_BajaLogica;
GO

CREATE PROCEDURE dbo.sp_EspacioArtistico_BajaLogica
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.EspacioArtistico
    SET activo                  = 0,
        publicado               = 0,
        estadoEspacio           = N'Dado de baja',
        fechaBaja               = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND activo = 1;
END
GO

SELECT ue.correoElectronico, ca.codigo, ca.fechaGeneracion, ca.fechaVencimiento, ca.utilizado
FROM dbo.CodigoActivacion ca
JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = ca.idUsuarioExterno
ORDER BY ca.fechaGeneracion DESC;