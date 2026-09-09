USE master;
GO

IF OBJECT_ID('dbo.RegistroActividad','U')  IS NOT NULL DROP TABLE dbo.RegistroActividad;
IF OBJECT_ID('dbo.RolInternoPermiso','U')  IS NOT NULL DROP TABLE dbo.RolInternoPermiso;
IF OBJECT_ID('dbo.UsuarioInterno','U')     IS NOT NULL DROP TABLE dbo.UsuarioInterno;
IF OBJECT_ID('dbo.CodigoRecuperacion','U') IS NOT NULL DROP TABLE dbo.CodigoRecuperacion;
IF OBJECT_ID('dbo.CodigoActivacion','U')   IS NOT NULL DROP TABLE dbo.CodigoActivacion;
IF OBJECT_ID('dbo.PermisoInterno','U')     IS NOT NULL DROP TABLE dbo.PermisoInterno;
IF OBJECT_ID('dbo.RolInterno','U')         IS NOT NULL DROP TABLE dbo.RolInterno;
IF OBJECT_ID('dbo.AreaInterna','U')        IS NOT NULL DROP TABLE dbo.AreaInterna;
IF OBJECT_ID('dbo.UsuarioExterno','U')     IS NOT NULL DROP TABLE dbo.UsuarioExterno;
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_Insertar','P')                     IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_Insertar;
IF OBJECT_ID('dbo.sp_UsuarioExterno_ObtenerPorCorreo','P')             IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ObtenerPorCorreo;
IF OBJECT_ID('dbo.sp_UsuarioExterno_ObtenerPorId','P')                 IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ObtenerPorId;
IF OBJECT_ID('dbo.sp_UsuarioExterno_ActivarCuenta','P')                IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ActivarCuenta;
IF OBJECT_ID('dbo.sp_UsuarioExterno_ActualizarPassword','P')           IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ActualizarPassword;
IF OBJECT_ID('dbo.sp_UsuarioExterno_ActualizarPerfil','P')             IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ActualizarPerfil;
IF OBJECT_ID('dbo.sp_CodigoActivacion_Insertar','P')                   IS NOT NULL DROP PROCEDURE dbo.sp_CodigoActivacion_Insertar;
IF OBJECT_ID('dbo.sp_CodigoActivacion_ObtenerVigentePorUsuario','P')   IS NOT NULL DROP PROCEDURE dbo.sp_CodigoActivacion_ObtenerVigentePorUsuario;
IF OBJECT_ID('dbo.sp_CodigoActivacion_MarcarUtilizado','P')            IS NOT NULL DROP PROCEDURE dbo.sp_CodigoActivacion_MarcarUtilizado;
IF OBJECT_ID('dbo.sp_CodigoRecuperacion_Insertar','P')                 IS NOT NULL DROP PROCEDURE dbo.sp_CodigoRecuperacion_Insertar;
IF OBJECT_ID('dbo.sp_CodigoRecuperacion_ObtenerVigentePorUsuario','P') IS NOT NULL DROP PROCEDURE dbo.sp_CodigoRecuperacion_ObtenerVigentePorUsuario;
IF OBJECT_ID('dbo.sp_CodigoRecuperacion_MarcarUtilizado','P')          IS NOT NULL DROP PROCEDURE dbo.sp_CodigoRecuperacion_MarcarUtilizado;
IF OBJECT_ID('dbo.sp_RegistroActividad_Insertar','P')                  IS NOT NULL DROP PROCEDURE dbo.sp_RegistroActividad_Insertar;
GO

USE master;
GO
IF OBJECT_ID('dbo.Reserva','U')          IS NOT NULL DROP TABLE dbo.Reserva;
IF OBJECT_ID('dbo.EspacioArtistico','U') IS NOT NULL DROP TABLE dbo.EspacioArtistico;
IF OBJECT_ID('dbo.UsuarioExterno','U')   IS NOT NULL DROP TABLE dbo.UsuarioExterno;
GO

USE master;
GO
SELECT
    fk.name AS NombreFK,
    OBJECT_NAME(fk.parent_object_id) AS TablaQueReferencia
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID('dbo.UsuarioExterno');

CREATE DATABASE StageUp
ON PRIMARY (NAME = N'StageUp', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA\StageUp_v2.mdf')
LOG ON (NAME = N'StageUp_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA\StageUp_v2_log.ldf');
GO
