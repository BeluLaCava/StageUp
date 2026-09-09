IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.sp_UsuarioInterno_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioInterno_Insertar;
GO
CREATE PROCEDURE dbo.sp_UsuarioInterno_Insertar
    @idAreaInterna      INT,
    @idRolInterno       INT,
    @nombre             NVARCHAR(200),
    @apellido           NVARCHAR(200),
    @correoElectronico  NVARCHAR(300),
    @passwordHash       NVARCHAR(510),
    @estadoCuenta       NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.UsuarioInterno
            (idAreaInterna, idRolInterno, nombre, apellido, correoElectronico, passwordHash,
             estadoCuenta, fechaAlta, activo)
        VALUES
            (@idAreaInterna, @idRolInterno, @nombre, @apellido, @correoElectronico, @passwordHash,
             @estadoCuenta, GETDATE(), 1);

        SELECT SCOPE_IDENTITY() AS idUsuarioInterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_UsuarioInterno_ObtenerPorCorreo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioInterno_ObtenerPorCorreo;
GO
CREATE PROCEDURE dbo.sp_UsuarioInterno_ObtenerPorCorreo
    @correoElectronico NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.UsuarioInterno WHERE correoElectronico = @correoElectronico AND activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_UsuarioInterno_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioInterno_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_UsuarioInterno_ObtenerPorId
    @idUsuarioInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.UsuarioInterno WHERE idUsuarioInterno = @idUsuarioInterno AND activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_ListarPorPerfil', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ListarPorPerfil;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ListarPorPerfil
    @perfilUsuario NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.UsuarioExterno
    WHERE perfilUsuario = @perfilUsuario
      AND activo = 1
    ORDER BY fechaAlta ASC;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AreaInterna WHERE nombreArea = N'Administración')
BEGIN
    INSERT INTO dbo.AreaInterna (nombreArea, descripcion, estadoArea, fechaAlta, activo)
    VALUES (N'Administración', N'Área responsable de la operación interna de la plataforma StageUp.', N'Activa', GETDATE(), 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RolInterno WHERE nombreRol = N'Administrador')
BEGIN
    DECLARE @idAreaAdministracion INT;
    SELECT @idAreaAdministracion = idAreaInterna FROM dbo.AreaInterna WHERE nombreArea = N'Administración';

    INSERT INTO dbo.RolInterno (idAreaInterna, nombreRol, descripcion, estadoRol, fechaAlta, activo)
    VALUES (@idAreaAdministracion, N'Administrador', N'Rol con acceso completo al panel administrativo.', N'Activo', GETDATE(), 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioInterno WHERE correoElectronico = N'admin@stageup.test')
BEGIN
    DECLARE @idAreaAdmin INT, @idRolAdmin INT;
    SELECT @idAreaAdmin = idAreaInterna FROM dbo.AreaInterna WHERE nombreArea = N'Administración';
    SELECT @idRolAdmin = idRolInterno FROM dbo.RolInterno WHERE nombreRol = N'Administrador';

    INSERT INTO dbo.UsuarioInterno
        (idAreaInterna, idRolInterno, nombre, apellido, correoElectronico, passwordHash, estadoCuenta, fechaAlta, activo)
    VALUES
        (@idAreaAdmin, @idRolAdmin, N'Admin', N'StageUp', N'admin@stageup.test',
         N'100000.6r7B+SgPFLthDYDUE6pyPg==.Tu/TIFNwDUa6mYm8n2I/7ZTfetL2T/j95BBqXEuWxBI=',
         N'Activa', GETDATE(), 1);
END
GO
