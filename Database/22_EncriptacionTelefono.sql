IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO


-- El teléfono ahora se guarda encriptado (AES-256 + Base64), así que necesita más lugar
-- que un NVARCHAR(60). Se ensancha la columna sin tocar los datos existentes.
IF COL_LENGTH('dbo.UsuarioExterno', 'telefono') IS NOT NULL AND COL_LENGTH('dbo.UsuarioExterno', 'telefono') < 200
BEGIN
    ALTER TABLE dbo.UsuarioExterno ALTER COLUMN telefono NVARCHAR(200) NULL;
END
GO


IF OBJECT_ID('dbo.sp_UsuarioExterno_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_Insertar;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_Insertar
    @nombre                     NVARCHAR(200),
    @apellido                   NVARCHAR(200),
    @correoElectronico          NVARCHAR(300),
    @passwordHash               NVARCHAR(510),
    @telefono                   NVARCHAR(200) = NULL,
    @estadoCuenta                NVARCHAR(100),
    @perfilUsuario               NVARCHAR(100),
    @aceptaTerminos              BIT,
    @aceptaPoliticaPrivacidad    BIT,
    @fechaAceptacionTerminos     DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, telefono,
         estadoCuenta, perfilUsuario, aceptaTerminos, aceptaPoliticaPrivacidad,
         fechaAceptacionTerminos, fechaAlta, activo)
    VALUES
        (@nombre, @apellido, @correoElectronico, @passwordHash, @telefono,
         @estadoCuenta, @perfilUsuario, @aceptaTerminos, @aceptaPoliticaPrivacidad,
         @fechaAceptacionTerminos, GETDATE(), 1);

    SELECT SCOPE_IDENTITY() AS idUsuarioExterno;
END
GO


-- Se agrega @telefono para que "Mi perfil" también pueda actualizarlo (encriptado desde la BLL/MPP).
IF OBJECT_ID('dbo.sp_UsuarioExterno_ActualizarDatosPerfilV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ActualizarDatosPerfilV2;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ActualizarDatosPerfilV2
    @idUsuarioExterno   INT,
    @nombre             NVARCHAR(200),
    @apellido           NVARCHAR(200),
    @correoElectronico  NVARCHAR(300),
    @telefono           NVARCHAR(200)   = NULL,
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
            telefono                = @telefono,
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
