IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/16_ComponentePermiso.sql.', 1;
END
GO

IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OpcionMenu
    (
        idOpcionMenu             INT IDENTITY(1,1) NOT NULL,
        texto                    NVARCHAR(100)  NOT NULL,
        descripcion              NVARCHAR(300)  NULL,
        url                      NVARCHAR(300)  NOT NULL,
        modulo                   NVARCHAR(100)  NOT NULL,
        orden                    INT            NOT NULL,
        idComponentePermiso      INT            NOT NULL,
        activo                   BIT            NOT NULL CONSTRAINT DF_OpcionMenu_activo DEFAULT (1),
        fechaAlta                DATETIME       NOT NULL CONSTRAINT DF_OpcionMenu_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion  DATETIME       NULL,
        CONSTRAINT PK_OpcionMenu PRIMARY KEY CLUSTERED (idOpcionMenu),
        CONSTRAINT FK_OpcionMenu_ComponentePermiso FOREIGN KEY (idComponentePermiso)
            REFERENCES dbo.ComponentePermiso (idComponentePermiso),
        CONSTRAINT CK_OpcionMenu_texto CHECK (LEN(LTRIM(RTRIM(texto))) > 0),
        CONSTRAINT CK_OpcionMenu_url CHECK (url LIKE N'~/%')
    );

    CREATE INDEX IX_OpcionMenu_Orden ON dbo.OpcionMenu (activo, orden);
END
GO

-- ---------------------------------------------------------------------------
-- Permiso propio GESTIONAR_MENU (mismo patrón que GESTIONAR_SOPORTE en el 38).
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoMenu INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'GESTIONAR_MENU'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoMenu IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'GESTIONAR_MENU', N'Gestión del menú',
             N'Crear, editar, ordenar y dar de baja las opciones del menú del panel interno.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/GestionMenu.aspx', 1);

        SET @idPermisoMenu = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Gestión del menú',
            descripcion = N'Crear, editar, ordenar y dar de baja las opciones del menú del panel interno.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/GestionMenu.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoMenu;
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
             AND idPermisoInterno = @idPermisoMenu
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoMenu, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoMenu;
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

        DECLARE @idComponenteMenu INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'GESTIONAR_MENU'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteMenu IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_MENU', N'Gestión del menú',
                 N'Crear, editar, ordenar y dar de baja las opciones del menú del panel interno.',
                 N'~/Interno/GestionMenu.aspx', 130, 1);

            SET @idComponenteMenu = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Gestión del menú',
                descripcion = N'Crear, editar, ordenar y dar de baja las opciones del menú del panel interno.',
                urlAsociada = N'~/Interno/GestionMenu.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteMenu;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteMenu
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteMenu, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteMenu;
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

-- ---------------------------------------------------------------------------
-- Carga inicial: una opción por cada permiso activo con URL, en el mismo orden
-- en que hoy aparece el menú (grupo y orden del árbol de permisos). Solo si la
-- tabla está vacía, para no pisar lo que ya se haya administrado.
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu)
BEGIN
    INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
    SELECT
        LEFT(h.nombre, 100),
        LEFT(h.descripcion, 300),
        h.urlAsociada,
        LEFT(ISNULL(g.nombre, N'General'), 100),
        CAST(ROW_NUMBER() OVER (ORDER BY ISNULL(g.orden, 0), ISNULL(g.nombre, N''), h.orden, h.nombre) * 10 AS INT),
        h.idComponentePermiso,
        1
    FROM dbo.ComponentePermiso h
    LEFT JOIN dbo.ComponentePermiso g ON g.idComponentePermiso = h.idComponentePadre
    WHERE h.tipoComponente = N'Permiso'
      AND h.activo = 1
      AND h.urlAsociada IS NOT NULL
      AND h.urlAsociada LIKE N'~/%';
END
GO

-- ---------------------------------------------------------------------------
-- Stored procedures del ABMC
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_OpcionMenu_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionMenu_Listar;
GO
CREATE PROCEDURE dbo.sp_OpcionMenu_Listar
    @soloActivas BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.idOpcionMenu, o.texto, o.descripcion, o.url, o.modulo, o.orden,
        o.idComponentePermiso, o.activo, o.fechaAlta, o.fechaUltimaModificacion,
        c.codigoPermiso, c.nombre AS nombrePermiso
    FROM dbo.OpcionMenu o
    INNER JOIN dbo.ComponentePermiso c ON c.idComponentePermiso = o.idComponentePermiso
    WHERE @soloActivas = 0 OR o.activo = 1
    ORDER BY o.orden, o.idOpcionMenu;
END
GO

IF OBJECT_ID('dbo.sp_OpcionMenu_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionMenu_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_OpcionMenu_ObtenerPorId
    @idOpcionMenu INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.idOpcionMenu, o.texto, o.descripcion, o.url, o.modulo, o.orden,
        o.idComponentePermiso, o.activo, o.fechaAlta, o.fechaUltimaModificacion,
        c.codigoPermiso, c.nombre AS nombrePermiso
    FROM dbo.OpcionMenu o
    INNER JOIN dbo.ComponentePermiso c ON c.idComponentePermiso = o.idComponentePermiso
    WHERE o.idOpcionMenu = @idOpcionMenu;
END
GO

IF OBJECT_ID('dbo.sp_OpcionMenu_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionMenu_Insertar;
GO
CREATE PROCEDURE dbo.sp_OpcionMenu_Insertar
    @texto                NVARCHAR(100),
    @descripcion          NVARCHAR(300) = NULL,
    @url                  NVARCHAR(300),
    @modulo               NVARCHAR(100),
    @idComponentePermiso  INT,
    @activo               BIT
AS
BEGIN
    SET NOCOUNT ON;

    -- Una opción nueva va al final del menú.
    DECLARE @orden INT = ISNULL((SELECT MAX(orden) FROM dbo.OpcionMenu), 0) + 10;

    INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
    VALUES (@texto, @descripcion, @url, @modulo, @orden, @idComponentePermiso, @activo);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idOpcionMenu;
END
GO

IF OBJECT_ID('dbo.sp_OpcionMenu_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionMenu_Modificar;
GO
CREATE PROCEDURE dbo.sp_OpcionMenu_Modificar
    @idOpcionMenu         INT,
    @texto                NVARCHAR(100),
    @descripcion          NVARCHAR(300) = NULL,
    @url                  NVARCHAR(300),
    @modulo               NVARCHAR(100),
    @idComponentePermiso  INT,
    @activo               BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OpcionMenu
    SET texto = @texto,
        descripcion = @descripcion,
        url = @url,
        modulo = @modulo,
        idComponentePermiso = @idComponentePermiso,
        activo = @activo,
        fechaUltimaModificacion = GETDATE()
    WHERE idOpcionMenu = @idOpcionMenu;
END
GO

IF OBJECT_ID('dbo.sp_OpcionMenu_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionMenu_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_OpcionMenu_CambiarEstado
    @idOpcionMenu INT,
    @activo       BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OpcionMenu
    SET activo = @activo,
        fechaUltimaModificacion = GETDATE()
    WHERE idOpcionMenu = @idOpcionMenu;
END
GO

IF OBJECT_ID('dbo.sp_OpcionMenu_ActualizarOrden', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OpcionMenu_ActualizarOrden;
GO
CREATE PROCEDURE dbo.sp_OpcionMenu_ActualizarOrden
    @idOpcionMenu INT,
    @orden        INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OpcionMenu
    SET orden = @orden,
        fechaUltimaModificacion = GETDATE()
    WHERE idOpcionMenu = @idOpcionMenu;
END
GO
