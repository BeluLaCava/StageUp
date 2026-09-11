IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO


IF COL_LENGTH('dbo.UsuarioExterno', 'fotoPerfilRuta') IS NULL
BEGIN
    ALTER TABLE dbo.UsuarioExterno ADD fotoPerfilRuta NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH('dbo.UsuarioExterno', 'descripcionPerfil') IS NULL
BEGIN
    ALTER TABLE dbo.UsuarioExterno ADD descripcionPerfil NVARCHAR(1200) NULL;
END
GO


IF OBJECT_ID('dbo.sp_UsuarioExterno_ObtenerPerfilPorIdV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ObtenerPerfilPorIdV2;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ObtenerPerfilPorIdV2
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        idUsuarioExterno, nombre, apellido, correoElectronico, passwordHash, telefono,
        estadoCuenta, perfilUsuario, fotoPerfilRuta, descripcionPerfil,
        aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos,
        fechaAlta, fechaActivacion, fechaBaja, fechaUltimaModificacion, activo
    FROM dbo.UsuarioExterno
    WHERE idUsuarioExterno = @idUsuarioExterno;
END
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_ActualizarDatosPerfilV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ActualizarDatosPerfilV2;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ActualizarDatosPerfilV2
    @idUsuarioExterno   INT,
    @nombre             NVARCHAR(200),
    @apellido           NVARCHAR(200),
    @correoElectronico  NVARCHAR(300),
    @fotoPerfilRuta     NVARCHAR(500)   = NULL,
    @descripcionPerfil  NVARCHAR(1200)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.UsuarioExterno
        SET nombre                  = @nombre,
            apellido                = @apellido,
            correoElectronico       = @correoElectronico,
            fotoPerfilRuta          = @fotoPerfilRuta,
            descripcionPerfil       = @descripcionPerfil,
            fechaUltimaModificacion = GETDATE()
        WHERE idUsuarioExterno = @idUsuarioExterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO