USE StageUp;
GO

SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.PermisoInterno', 'urlAsociada') IS NULL
BEGIN
    ALTER TABLE dbo.PermisoInterno ADD urlAsociada NVARCHAR(300) NULL;
END
GO

IF OBJECT_ID('dbo.sp_RolInterno_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_Insertar;
GO
CREATE PROCEDURE dbo.sp_RolInterno_Insertar
    @idAreaInterna  INT = NULL,
    @nombreRol      NVARCHAR(200),
    @descripcion    NVARCHAR(510) = NULL,
    @estadoRol      NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.RolInterno (idAreaInterna, nombreRol, descripcion, estadoRol, fechaAlta, activo)
        VALUES (@idAreaInterna, @nombreRol, @descripcion, @estadoRol, GETDATE(), 1);

        SELECT SCOPE_IDENTITY() AS idRolInterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_RolInterno_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_Modificar;
GO
CREATE PROCEDURE dbo.sp_RolInterno_Modificar
    @idRolInterno   INT,
    @idAreaInterna  INT = NULL,
    @nombreRol      NVARCHAR(200),
    @descripcion    NVARCHAR(510) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.RolInterno
        SET idAreaInterna = @idAreaInterna,
            nombreRol = @nombreRol,
            descripcion = @descripcion,
            fechaUltimaModificacion = GETDATE()
        WHERE idRolInterno = @idRolInterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_RolInterno_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_RolInterno_ObtenerPorId
    @idRolInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.*, a.nombreArea
    FROM dbo.RolInterno r
    LEFT JOIN dbo.AreaInterna a ON a.idAreaInterna = r.idAreaInterna
    WHERE r.idRolInterno = @idRolInterno;
END
GO

IF OBJECT_ID('dbo.sp_RolInterno_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_Listar;
GO
CREATE PROCEDURE dbo.sp_RolInterno_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.*, a.nombreArea
    FROM dbo.RolInterno r
    LEFT JOIN dbo.AreaInterna a ON a.idAreaInterna = r.idAreaInterna
    WHERE r.activo = 1
    ORDER BY r.nombreRol;
END
GO

IF OBJECT_ID('dbo.sp_RolInterno_Baja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_Baja;
GO
CREATE PROCEDURE dbo.sp_RolInterno_Baja
    @idRolInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.RolInterno
        SET activo = 0,
            estadoRol = 'Inactivo',
            fechaBaja = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idRolInterno = @idRolInterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_PermisoInterno_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PermisoInterno_Listar;
GO
CREATE PROCEDURE dbo.sp_PermisoInterno_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.PermisoInterno WHERE activo = 1 ORDER BY modulo, nombrePermiso;
END
GO

IF OBJECT_ID('dbo.sp_PermisoInterno_ListarPorRol', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PermisoInterno_ListarPorRol;
GO
CREATE PROCEDURE dbo.sp_PermisoInterno_ListarPorRol
    @idRolInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*
    FROM dbo.PermisoInterno p
    INNER JOIN dbo.RolInternoPermiso rp ON rp.idPermisoInterno = p.idPermisoInterno
    WHERE rp.idRolInterno = @idRolInterno
      AND rp.activo = 1
      AND p.activo = 1
    ORDER BY p.modulo, p.nombrePermiso;
END
GO

IF OBJECT_ID('dbo.sp_RolInternoPermiso_EliminarPorRol', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInternoPermiso_EliminarPorRol;
GO
CREATE PROCEDURE dbo.sp_RolInternoPermiso_EliminarPorRol
    @idRolInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.RolInternoPermiso WHERE idRolInterno = @idRolInterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_RolInternoPermiso_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInternoPermiso_Insertar;
GO
CREATE PROCEDURE dbo.sp_RolInternoPermiso_Insertar
    @idRolInterno       INT,
    @idPermisoInterno   INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.RolInternoPermiso (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES (@idRolInterno, @idPermisoInterno, GETDATE(), 1);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_UsuarioInterno_ContarActivosPorRol', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioInterno_ContarActivosPorRol;
GO
CREATE PROCEDURE dbo.sp_UsuarioInterno_ContarActivosPorRol
    @idRolInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad
    FROM dbo.UsuarioInterno
    WHERE idRolInterno = @idRolInterno
      AND activo = 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PermisoInterno WHERE codigoPermiso = 'APROBAR_GESTORES')
BEGIN
    INSERT INTO dbo.PermisoInterno (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
    VALUES ('APROBAR_GESTORES', 'Aprobación de gestores', 'Revisar y aprobar o rechazar solicitudes de habilitación como gestor de espacios.', 'Gestión de usuarios', 'Ver', 'Activo', '~/Interno/AprobacionGestores.aspx', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PermisoInterno WHERE codigoPermiso = 'VER_BITACORA')
BEGIN
    INSERT INTO dbo.PermisoInterno (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
    VALUES ('VER_BITACORA', 'Registros de actividad', 'Consultar la bitácora del sistema con filtros por usuario, fecha y tipo de operación.', 'Auditoría', 'Ver', 'Activo', '~/Interno/RegistrosActividad.aspx', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PermisoInterno WHERE codigoPermiso = 'GESTIONAR_ROLES')
BEGIN
    INSERT INTO dbo.PermisoInterno (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
    VALUES ('GESTIONAR_ROLES', 'Gestión de roles y permisos', 'Dar de alta o editar roles internos y decidir qué permisos tiene cada uno.', 'Administración', 'Ver', 'Activo', '~/Interno/GestionRoles.aspx', 1);
END
GO

IF EXISTS (SELECT 1 FROM dbo.RolInterno WHERE nombreRol = 'Administrador')
BEGIN
    DECLARE @idRolAdministrador INT = (SELECT TOP 1 idRolInterno FROM dbo.RolInterno WHERE nombreRol = 'Administrador');

    INSERT INTO dbo.RolInternoPermiso (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
    SELECT @idRolAdministrador, p.idPermisoInterno, GETDATE(), 1
    FROM dbo.PermisoInterno p
    WHERE p.codigoPermiso IN ('APROBAR_GESTORES', 'VER_BITACORA', 'GESTIONAR_ROLES')
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RolInternoPermiso rp
          WHERE rp.idRolInterno = @idRolAdministrador AND rp.idPermisoInterno = p.idPermisoInterno
      );
END
GO
