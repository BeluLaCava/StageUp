-- ============================================================================
-- Eliminacion_creacion_bd.sql
--
-- Script de RECUPERACION MANUAL (se usó para resolver el incidente de la
-- base huérfana del 09/09). A propósito NO forma parte de la secuencia
-- automática de Database/: EjecutarTodosLosScripts.ps1 solo toma archivos
-- cuyo nombre matchea "^\d+_" (los "NN_..."), así que este nunca se ejecuta
-- solo ni por accidente — hay que correrlo a mano, y únicamente si sabés por
-- qué lo estás corriendo.
--
-- Antes de correrlo:
--   1) Los tres bloques de más abajo apuntan a "USE [StageUp]" (antes decía
--      "USE master", y por eso los DROP TABLE y la consulta de FKs corrían
--      silenciosamente contra la base master, sin tocar StageUp — no
--      rompían nada, pero tampoco limpiaban lo que debían: quedaba la falsa
--      sensación de que el script había hecho su trabajo). Si tu instancia
--      de SQL Server usa otro nombre de base, cambialo acá.
--   2) Si tu base StageUp ya no existe (se borró por completo, por ejemplo
--      desde SSMS), el primer bloque va a fallar con "no se pudo abrir la
--      base de datos StageUp" — es lo esperado, no hay nada que limpiar
--      todavía: saltealo y andá directo a la sección CREATE DATABASE del
--      final.
--   3) La sección CREATE DATABASE del final tiene rutas de archivo
--      (.mdf/.ldf) hardcodeadas a la instalación original de SQL Server
--      donde se armó este script. Si tu instancia usa otra carpeta de
--      datos, ajustá esas rutas antes de correrlo.
-- ============================================================================

USE [StageUp];
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

USE [StageUp];
GO
IF OBJECT_ID('dbo.Reserva','U')          IS NOT NULL DROP TABLE dbo.Reserva;
IF OBJECT_ID('dbo.EspacioArtistico','U') IS NOT NULL DROP TABLE dbo.EspacioArtistico;
IF OBJECT_ID('dbo.UsuarioExterno','U')   IS NOT NULL DROP TABLE dbo.UsuarioExterno;
GO

USE [StageUp];
GO
SELECT
    fk.name AS NombreFK,
    OBJECT_NAME(fk.parent_object_id) AS TablaQueReferencia
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID('dbo.UsuarioExterno');

-- Solo hace falta si la base StageUp fue borrada por completo (por ejemplo
-- desde SSMS) y hay que recrearla vacía antes de volver a correr
-- EjecutarTodosLosScripts.ps1. Si la base ya existe, este IF no hace nada
-- (no borra ni recrea lo que ya está) — mismo patrón de guarda que usa el
-- propio EjecutarTodosLosScripts.ps1 para crear la base la primera vez.
IF DB_ID(N'StageUp') IS NULL
    CREATE DATABASE StageUp
    ON PRIMARY (NAME = N'StageUp', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA\StageUp_v2.mdf')
    LOG ON (NAME = N'StageUp_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA\StageUp_v2_log.ldf');
GO
