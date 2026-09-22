-- ============================================================================
-- 29_FaqAbmYPermiso.sql
--
-- Ítem 37 del checklist de correcciones: la pantalla de administración de
-- FAQ (Interno/GestionFaq.aspx) tenía solo el formulario armado en UI, sin
-- ABM real (04_FaqDinamica.sql solo tenía sp_Faq_ListarActivas, la que usa
-- el centro de ayuda público) y el guard de acceso reutilizaba permisos que
-- no tienen nada que ver con FAQ (GESTIONAR_ROLES / GESTIONAR_IDIOMAS /
-- GESTIONAR_USUARIOS_INTERNOS). Este script agrega:
--   1) Dos columnas de auditoría a dbo.Faq (fechaAlta, fechaUltimaModificacion).
--   2) Los stored procedures de alta, baja, reactivación, modificación y
--      listado completo (el admin necesita ver también las inactivas, no
--      solo las publicadas).
--   3) Un permiso interno propio, GESTIONAR_FAQ, dado de alta tanto en el
--      esquema clásico (PermisoInterno/RolInternoPermiso) como en el árbol
--      de componentes que realmente usa el login para armar la sesión y el
--      menú (ComponentePermiso/RolInternoComponentePermiso — ver
--      BLL_PermisoInterno.ObtenerHojasAsignadas), asignado al rol
--      Administrador. Se sigue el mismo patrón que ya usa GESTIONAR_IDIOMAS
--      en 17_MultidiomaCompleto.sql.
-- ============================================================================

IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF COL_LENGTH('dbo.Faq', 'fechaAlta') IS NULL
BEGIN
    ALTER TABLE dbo.Faq ADD fechaAlta DATETIME NOT NULL CONSTRAINT DF_Faq_fechaAlta DEFAULT (GETDATE());
END
GO

IF COL_LENGTH('dbo.Faq', 'fechaUltimaModificacion') IS NULL
BEGIN
    ALTER TABLE dbo.Faq ADD fechaUltimaModificacion DATETIME NULL;
END
GO

IF OBJECT_ID('dbo.sp_Faq_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Listar;
GO
CREATE PROCEDURE dbo.sp_Faq_Listar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idFaq, pregunta, respuesta, orden, activo, fechaAlta, fechaUltimaModificacion
    FROM dbo.Faq
    ORDER BY orden ASC, idFaq ASC;
END
GO

IF OBJECT_ID('dbo.sp_Faq_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Faq_ObtenerPorId
    @idFaq INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idFaq, pregunta, respuesta, orden, activo, fechaAlta, fechaUltimaModificacion
    FROM dbo.Faq
    WHERE idFaq = @idFaq;
END
GO

IF OBJECT_ID('dbo.sp_Faq_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Insertar;
GO
CREATE PROCEDURE dbo.sp_Faq_Insertar
    @pregunta   NVARCHAR(300),
    @respuesta  NVARCHAR(MAX),
    @orden      INT,
    @activo     BIT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Faq (pregunta, respuesta, orden, activo, fechaAlta)
    VALUES (@pregunta, @respuesta, @orden, @activo, GETDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idFaq;
END
GO

IF OBJECT_ID('dbo.sp_Faq_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Modificar;
GO
CREATE PROCEDURE dbo.sp_Faq_Modificar
    @idFaq      INT,
    @pregunta   NVARCHAR(300),
    @respuesta  NVARCHAR(MAX),
    @orden      INT,
    @activo     BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Faq WHERE idFaq = @idFaq)
    BEGIN
        THROW 51101, 'No se encontró la pregunta frecuente indicada.', 1;
    END

    UPDATE dbo.Faq
    SET pregunta = @pregunta,
        respuesta = @respuesta,
        orden = @orden,
        activo = @activo,
        fechaUltimaModificacion = GETDATE()
    WHERE idFaq = @idFaq;
END
GO

IF OBJECT_ID('dbo.sp_Faq_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_DarDeBaja;
GO
CREATE PROCEDURE dbo.sp_Faq_DarDeBaja
    @idFaq INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Faq
    SET activo = 0,
        fechaUltimaModificacion = GETDATE()
    WHERE idFaq = @idFaq AND activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_Faq_Reactivar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Reactivar;
GO
CREATE PROCEDURE dbo.sp_Faq_Reactivar
    @idFaq INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Faq
    SET activo = 1,
        fechaUltimaModificacion = GETDATE()
    WHERE idFaq = @idFaq AND activo = 0;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso propio GESTIONAR_FAQ (mismo patrón que GESTIONAR_IDIOMAS en
-- 17_MultidiomaCompleto.sql): se registra en el esquema clásico y en el
-- árbol de componentes, y se asigna al rol Administrador.
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoFaq INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'GESTIONAR_FAQ'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoFaq IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'GESTIONAR_FAQ', N'Gestión de FAQ',
             N'Dar de alta, modificar, dar de baja y reordenar las preguntas frecuentes del centro de ayuda.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/GestionFaq.aspx', 1);

        SET @idPermisoFaq = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Gestión de FAQ',
            descripcion = N'Dar de alta, modificar, dar de baja y reordenar las preguntas frecuentes del centro de ayuda.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/GestionFaq.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoFaq;
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
             AND idPermisoInterno = @idPermisoFaq
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoFaq, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoFaq;
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

        DECLARE @idComponenteFaq INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'GESTIONAR_FAQ'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteFaq IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_FAQ', N'Gestión de FAQ',
                 N'Dar de alta, modificar, dar de baja y reordenar las preguntas frecuentes del centro de ayuda.',
                 N'~/Interno/GestionFaq.aspx', 100, 1);

            SET @idComponenteFaq = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Gestión de FAQ',
                descripcion = N'Dar de alta, modificar, dar de baja y reordenar las preguntas frecuentes del centro de ayuda.',
                urlAsociada = N'~/Interno/GestionFaq.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteFaq;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteFaq
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteFaq, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteFaq;
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
