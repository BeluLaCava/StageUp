-- ============================================================================
-- 30_NovedadesAbmYPermiso.sql
--
-- Ítem 38 del checklist de correcciones: igual que pasaba con FAQ (ítem 37),
-- la pantalla de administración de novedades y newsletter
-- (Interno/GestionNovedades.aspx) tenía solo el formulario armado en UI, con
-- datos de ejemplo hardcodeados, sin ABM real ni conexión con
-- StageUp.Servicios.ServicioCorreo.EnviarNewsletter (que ya existía pero no
-- tenía ningún llamador), y el guard de acceso reutilizaba permisos que no
-- tienen nada que ver con Novedades (GESTIONAR_ROLES / GESTIONAR_IDIOMAS /
-- GESTIONAR_USUARIOS_INTERNOS). Este script agrega:
--   1) La tabla dbo.Novedad (no existía ninguna tabla para esto).
--   2) Los stored procedures de listado (admin y público), alta,
--      modificación, publicación, vuelta a borrador y marcado de envío de
--      newsletter.
--   3) Un stored procedure para listar destinatarios de newsletter
--      (usuarios externos activos) según la categoría elegida en el panel.
--   4) Un permiso interno propio, GESTIONAR_NOVEDADES, dado de alta tanto en
--      el esquema clásico (PermisoInterno/RolInternoPermiso) como en el
--      árbol de componentes que realmente usa el login para armar la sesión
--      y el menú (ComponentePermiso/RolInternoComponentePermiso), asignado
--      al rol Administrador. Mismo patrón que GESTIONAR_FAQ en
--      29_FaqAbmYPermiso.sql y GESTIONAR_IDIOMAS en 17_MultidiomaCompleto.sql.
-- ============================================================================

IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID('dbo.Novedad', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Novedad
    (
        idNovedad                  INT IDENTITY(1,1)  NOT NULL,
        titulo                     NVARCHAR(180)       NOT NULL,
        resumen                    NVARCHAR(300)       NOT NULL,
        contenido                  NVARCHAR(MAX)        NOT NULL,
        categoria                  NVARCHAR(100)        NOT NULL,
        urlImagen                  NVARCHAR(500)        NULL,
        publicado                  BIT                  NOT NULL CONSTRAINT DF_Novedad_publicado DEFAULT (0),
        fechaPublicacion           DATETIME             NULL,
        enviadaPorCorreo           BIT                  NOT NULL CONSTRAINT DF_Novedad_enviadaPorCorreo DEFAULT (0),
        destinatarioNewsletter     NVARCHAR(100)        NULL,
        fechaEnvioNewsletter       DATETIME             NULL,
        fechaAlta                  DATETIME             NOT NULL CONSTRAINT DF_Novedad_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion    DATETIME             NULL,
        activo                     BIT                  NOT NULL CONSTRAINT DF_Novedad_activo DEFAULT (1),
        CONSTRAINT PK_Novedad PRIMARY KEY CLUSTERED (idNovedad)
    );
END
GO

IF OBJECT_ID('dbo.sp_Novedad_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_Listar;
GO
CREATE PROCEDURE dbo.sp_Novedad_Listar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idNovedad, titulo, resumen, contenido, categoria, urlImagen, publicado, fechaPublicacion,
           enviadaPorCorreo, destinatarioNewsletter, fechaEnvioNewsletter, fechaAlta, fechaUltimaModificacion
    FROM dbo.Novedad
    WHERE activo = 1
    ORDER BY fechaAlta DESC;
END
GO

IF OBJECT_ID('dbo.sp_Novedad_ListarPublicadas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_ListarPublicadas;
GO
CREATE PROCEDURE dbo.sp_Novedad_ListarPublicadas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idNovedad, titulo, resumen, contenido, categoria, urlImagen, publicado, fechaPublicacion,
           enviadaPorCorreo, destinatarioNewsletter, fechaEnvioNewsletter, fechaAlta, fechaUltimaModificacion
    FROM dbo.Novedad
    WHERE activo = 1 AND publicado = 1
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

    SELECT idNovedad, titulo, resumen, contenido, categoria, urlImagen, publicado, fechaPublicacion,
           enviadaPorCorreo, destinatarioNewsletter, fechaEnvioNewsletter, fechaAlta, fechaUltimaModificacion
    FROM dbo.Novedad
    WHERE idNovedad = @idNovedad;
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
    @fechaPublicacion   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Novedad (titulo, resumen, contenido, categoria, urlImagen, publicado, fechaPublicacion, fechaAlta)
    VALUES (@titulo, @resumen, @contenido, @categoria, @urlImagen, @publicado, @fechaPublicacion, GETDATE());

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
    @fechaPublicacion   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Novedad WHERE idNovedad = @idNovedad)
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
        fechaUltimaModificacion = GETDATE()
    WHERE idNovedad = @idNovedad;
END
GO

IF OBJECT_ID('dbo.sp_Novedad_Publicar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_Publicar;
GO
CREATE PROCEDURE dbo.sp_Novedad_Publicar
    @idNovedad INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Novedad
    SET publicado = 1,
        fechaPublicacion = ISNULL(fechaPublicacion, GETDATE()),
        fechaUltimaModificacion = GETDATE()
    WHERE idNovedad = @idNovedad AND activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_Novedad_VolverABorrador', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_VolverABorrador;
GO
CREATE PROCEDURE dbo.sp_Novedad_VolverABorrador
    @idNovedad INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Novedad
    SET publicado = 0,
        fechaUltimaModificacion = GETDATE()
    WHERE idNovedad = @idNovedad AND activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_Novedad_MarcarEnviadaPorCorreo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Novedad_MarcarEnviadaPorCorreo;
GO
CREATE PROCEDURE dbo.sp_Novedad_MarcarEnviadaPorCorreo
    @idNovedad              INT,
    @destinatarioNewsletter NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Novedad
    SET enviadaPorCorreo = 1,
        destinatarioNewsletter = @destinatarioNewsletter,
        fechaEnvioNewsletter = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    WHERE idNovedad = @idNovedad;
END
GO

-- ---------------------------------------------------------------------------
-- Destinatarios de newsletter: usuarios externos activos, filtrados según la
-- categoría elegida en el desplegable "Destinatarios" del panel.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_UsuarioExterno_ListarParaNewsletter', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ListarParaNewsletter;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ListarParaNewsletter
    @criterio NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- SELECT * (y no una lista acotada de columnas) a propósito: así se
    -- puede reutilizar el mismo mapeo de fila que ya usa
    -- MPP_UsuarioExterno.ListarPorPerfil (MapearDesdeFila), que espera
    -- todas las columnas de dbo.UsuarioExterno.
    SELECT *
    FROM dbo.UsuarioExterno
    WHERE activo = 1
      AND
      (
          (@criterio = N'Gestores' AND perfilUsuario = N'GestorEspacios' AND estadoCuenta = N'Activa')
          OR (@criterio = N'Solicitantes' AND perfilUsuario = N'ExternoSolicitante' AND estadoCuenta = N'Activa')
          OR (@criterio = N'Todos' )
          OR (@criterio NOT IN (N'Gestores', N'Solicitantes', N'Todos') AND estadoCuenta = N'Activa')
      )
    ORDER BY apellido ASC, nombre ASC;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso propio GESTIONAR_NOVEDADES (mismo patrón que GESTIONAR_FAQ en
-- 29_FaqAbmYPermiso.sql): se registra en el esquema clásico y en el árbol de
-- componentes, y se asigna al rol Administrador.
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoNovedades INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'GESTIONAR_NOVEDADES'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoNovedades IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'GESTIONAR_NOVEDADES', N'Gestión de novedades',
             N'Crear, editar, publicar y enviar por correo las novedades públicas de StageUp.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/GestionNovedades.aspx', 1);

        SET @idPermisoNovedades = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Gestión de novedades',
            descripcion = N'Crear, editar, publicar y enviar por correo las novedades públicas de StageUp.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/GestionNovedades.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoNovedades;
    END

    DECLARE @idRolAdministrador INT =
    (
        SELECT TOP 1 idRolInterno
        FROM dbo.RolInterno
        WHERE nombreRol = N'Administrador'
          AND activo = 1
        ORDER BY idRolInterno
    );

    IF @idRolAdministrador IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.RolInternoPermiso
           WHERE idRolInterno = @idRolAdministrador
             AND idPermisoInterno = @idPermisoNovedades
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoNovedades, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoNovedades;
    END

    IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
    BEGIN
        DECLARE @idGrupoAdministracion INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE tipoComponente = N'Grupo'
              AND idComponentePadre IS NULL
              AND nombre = N'Administración'
            ORDER BY idComponentePermiso
        );

        IF @idGrupoAdministracion IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (NULL, N'Grupo', NULL, N'Administración', NULL, NULL, 100, 1);

            SET @idGrupoAdministracion = CAST(SCOPE_IDENTITY() AS INT);
        END

        DECLARE @idComponenteNovedades INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'GESTIONAR_NOVEDADES'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteNovedades IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_NOVEDADES', N'Gestión de novedades',
                 N'Crear, editar, publicar y enviar por correo las novedades públicas de StageUp.',
                 N'~/Interno/GestionNovedades.aspx', 110, 1);

            SET @idComponenteNovedades = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Gestión de novedades',
                descripcion = N'Crear, editar, publicar y enviar por correo las novedades públicas de StageUp.',
                urlAsociada = N'~/Interno/GestionNovedades.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteNovedades;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteNovedades
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteNovedades, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteNovedades;
            END
        END
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    DECLARE @mensajeError NVARCHAR(4000);
    SET @mensajeError = ERROR_MESSAGE();
    RAISERROR(N'%s', 16, 1, @mensajeError);
END CATCH;
GO
