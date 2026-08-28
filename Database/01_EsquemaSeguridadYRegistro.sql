/*
    StageUp - Avance 1
    Esquema de base de datos: módulo de seguridad (registro, activación,
    autenticación, recuperación de contraseña, bitácora) y preparación de
    la arquitectura Usuario Interno -> Rol -> Permiso.

    Fuente de verdad: docs/referencia/StageUp_Tecnico.docx
    (10.7.4 Diccionario de Datos, CU-001-001, CU-001-002, CU-001-012, CU-001-013).
*/

SET NOCOUNT ON;
GO

-- =====================================================================
-- 1. TABLAS - USUARIO EXTERNO Y SEGURIDAD
-- =====================================================================

IF OBJECT_ID('dbo.UsuarioExterno', 'U') IS NOT NULL DROP TABLE dbo.UsuarioExterno;
GO
CREATE TABLE dbo.UsuarioExterno
(
    idUsuarioExterno            INT IDENTITY(1,1)   NOT NULL,
    nombre                      NVARCHAR(200)       NOT NULL,
    apellido                    NVARCHAR(200)       NOT NULL,
    correoElectronico           NVARCHAR(300)       NOT NULL,
    passwordHash                NVARCHAR(510)       NOT NULL,
    telefono                    NVARCHAR(60)        NULL,
    estadoCuenta                NVARCHAR(100)       NOT NULL, -- PendienteActivacion | Activa | Inactiva
    perfilUsuario               NVARCHAR(100)       NOT NULL, -- ExternoSolicitante | GestorEspacios
    aceptaTerminos              BIT                 NOT NULL DEFAULT (0),
    aceptaPoliticaPrivacidad    BIT                 NOT NULL DEFAULT (0),
    fechaAceptacionTerminos     DATETIME            NULL,
    fechaAlta                   DATETIME            NOT NULL DEFAULT (GETDATE()),
    fechaActivacion             DATETIME            NULL,
    fechaBaja                   DATETIME            NULL,
    fechaUltimaModificacion     DATETIME            NULL,
    activo                      BIT                 NOT NULL DEFAULT (1),
    CONSTRAINT PK_UsuarioExterno PRIMARY KEY CLUSTERED (idUsuarioExterno),
    CONSTRAINT UQ_UsuarioExterno_Correo UNIQUE (correoElectronico)
);
GO

IF OBJECT_ID('dbo.CodigoActivacion', 'U') IS NOT NULL DROP TABLE dbo.CodigoActivacion;
GO
CREATE TABLE dbo.CodigoActivacion
(
    idCodigoActivacion  INT IDENTITY(1,1)  NOT NULL,
    idUsuarioExterno    INT                NOT NULL,
    codigo              NVARCHAR(40)       NOT NULL,
    fechaGeneracion     DATETIME           NOT NULL DEFAULT (GETDATE()),
    fechaVencimiento    DATETIME           NOT NULL,
    utilizado           BIT                NOT NULL DEFAULT (0),
    fechaUtilizacion    DATETIME           NULL,
    CONSTRAINT PK_CodigoActivacion PRIMARY KEY CLUSTERED (idCodigoActivacion),
    CONSTRAINT FK_CodigoActivacion_UsuarioExterno FOREIGN KEY (idUsuarioExterno)
        REFERENCES dbo.UsuarioExterno (idUsuarioExterno)
);
GO

IF OBJECT_ID('dbo.CodigoRecuperacion', 'U') IS NOT NULL DROP TABLE dbo.CodigoRecuperacion;
GO
CREATE TABLE dbo.CodigoRecuperacion
(
    idCodigoRecuperacion    INT IDENTITY(1,1)  NOT NULL,
    idUsuarioExterno        INT                NOT NULL,
    codigo                  NVARCHAR(40)       NOT NULL,
    fechaGeneracion         DATETIME           NOT NULL DEFAULT (GETDATE()),
    fechaVencimiento        DATETIME           NOT NULL,
    utilizado               BIT                NOT NULL DEFAULT (0),
    fechaUtilizacion        DATETIME           NULL,
    CONSTRAINT PK_CodigoRecuperacion PRIMARY KEY CLUSTERED (idCodigoRecuperacion),
    CONSTRAINT FK_CodigoRecuperacion_UsuarioExterno FOREIGN KEY (idUsuarioExterno)
        REFERENCES dbo.UsuarioExterno (idUsuarioExterno)
);
GO

-- =====================================================================
-- 2. ARQUITECTURA USUARIO INTERNO -> ROL -> PERMISO
--    (solo esquema, para "preparar la arquitectura desde el inicio",
--    según la pauta de la primera entrega. Todavía no se construyen
--    pantallas de administración sobre estas tablas en este avance.)
-- =====================================================================

IF OBJECT_ID('dbo.AreaInterna', 'U') IS NOT NULL DROP TABLE dbo.AreaInterna;
GO
CREATE TABLE dbo.AreaInterna
(
    idAreaInterna               INT IDENTITY(1,1)  NOT NULL,
    nombreArea                  NVARCHAR(200)      NOT NULL,
    descripcion                 NVARCHAR(510)      NULL,
    estadoArea                  NVARCHAR(100)      NOT NULL,
    fechaAlta                   DATETIME           NOT NULL DEFAULT (GETDATE()),
    fechaBaja                   DATETIME           NULL,
    fechaUltimaModificacion     DATETIME           NULL,
    activo                      BIT                NOT NULL DEFAULT (1),
    CONSTRAINT PK_AreaInterna PRIMARY KEY CLUSTERED (idAreaInterna)
);
GO

IF OBJECT_ID('dbo.RolInterno', 'U') IS NOT NULL DROP TABLE dbo.RolInterno;
GO
CREATE TABLE dbo.RolInterno
(
    idRolInterno                INT IDENTITY(1,1)  NOT NULL,
    idAreaInterna               INT                NULL,
    nombreRol                   NVARCHAR(200)      NOT NULL,
    descripcion                 NVARCHAR(510)      NULL,
    estadoRol                   NVARCHAR(100)      NOT NULL,
    fechaAlta                   DATETIME           NOT NULL DEFAULT (GETDATE()),
    fechaBaja                   DATETIME           NULL,
    fechaUltimaModificacion     DATETIME           NULL,
    activo                      BIT                NOT NULL DEFAULT (1),
    CONSTRAINT PK_RolInterno PRIMARY KEY CLUSTERED (idRolInterno),
    CONSTRAINT FK_RolInterno_AreaInterna FOREIGN KEY (idAreaInterna)
        REFERENCES dbo.AreaInterna (idAreaInterna)
);
GO

IF OBJECT_ID('dbo.PermisoInterno', 'U') IS NOT NULL DROP TABLE dbo.PermisoInterno;
GO
CREATE TABLE dbo.PermisoInterno
(
    idPermisoInterno            INT IDENTITY(1,1)  NOT NULL,
    codigoPermiso                NVARCHAR(200)      NOT NULL,
    nombrePermiso                NVARCHAR(200)      NOT NULL,
    descripcion                  NVARCHAR(510)      NULL,
    modulo                       NVARCHAR(200)      NOT NULL,
    accion                       NVARCHAR(200)      NOT NULL,
    estadoPermiso                NVARCHAR(100)      NOT NULL,
    fechaAlta                    DATETIME           NOT NULL DEFAULT (GETDATE()),
    fechaUltimaModificacion      DATETIME           NULL,
    activo                       BIT                NOT NULL DEFAULT (1),
    CONSTRAINT PK_PermisoInterno PRIMARY KEY CLUSTERED (idPermisoInterno),
    CONSTRAINT UQ_PermisoInterno_Codigo UNIQUE (codigoPermiso)
);
GO

IF OBJECT_ID('dbo.RolInternoPermiso', 'U') IS NOT NULL DROP TABLE dbo.RolInternoPermiso;
GO
CREATE TABLE dbo.RolInternoPermiso
(
    idRolInterno        INT         NOT NULL,
    idPermisoInterno    INT         NOT NULL,
    fechaAsignacion     DATETIME    NOT NULL DEFAULT (GETDATE()),
    activo              BIT         NOT NULL DEFAULT (1),
    CONSTRAINT PK_RolInternoPermiso PRIMARY KEY CLUSTERED (idRolInterno, idPermisoInterno),
    CONSTRAINT FK_RolInternoPermiso_Rol FOREIGN KEY (idRolInterno)
        REFERENCES dbo.RolInterno (idRolInterno),
    CONSTRAINT FK_RolInternoPermiso_Permiso FOREIGN KEY (idPermisoInterno)
        REFERENCES dbo.PermisoInterno (idPermisoInterno)
);
GO

IF OBJECT_ID('dbo.UsuarioInterno', 'U') IS NOT NULL DROP TABLE dbo.UsuarioInterno;
GO
CREATE TABLE dbo.UsuarioInterno
(
    idUsuarioInterno            INT IDENTITY(1,1)  NOT NULL,
    idAreaInterna               INT                NOT NULL,
    idRolInterno                INT                NOT NULL,
    nombre                      NVARCHAR(200)      NOT NULL,
    apellido                    NVARCHAR(200)      NOT NULL,
    correoElectronico           NVARCHAR(300)      NOT NULL,
    passwordHash                NVARCHAR(510)      NOT NULL,
    estadoCuenta                NVARCHAR(100)      NOT NULL,
    fechaAlta                   DATETIME           NOT NULL DEFAULT (GETDATE()),
    fechaBaja                   DATETIME           NULL,
    fechaUltimaModificacion     DATETIME           NULL,
    activo                      BIT                NOT NULL DEFAULT (1),
    CONSTRAINT PK_UsuarioInterno PRIMARY KEY CLUSTERED (idUsuarioInterno),
    CONSTRAINT UQ_UsuarioInterno_Correo UNIQUE (correoElectronico),
    CONSTRAINT FK_UsuarioInterno_AreaInterna FOREIGN KEY (idAreaInterna)
        REFERENCES dbo.AreaInterna (idAreaInterna),
    CONSTRAINT FK_UsuarioInterno_RolInterno FOREIGN KEY (idRolInterno)
        REFERENCES dbo.RolInterno (idRolInterno)
);
GO

-- =====================================================================
-- 3. BITÁCORA (registros de actividad) - CU-001-013
-- =====================================================================

IF OBJECT_ID('dbo.RegistroActividad', 'U') IS NOT NULL DROP TABLE dbo.RegistroActividad;
GO
CREATE TABLE dbo.RegistroActividad
(
    idRegistroActividad            INT IDENTITY(1,1)  NOT NULL,
    idUsuarioExternoResponsable    INT                NULL,
    idUsuarioInternoResponsable    INT                NULL,
    tipoOperacion                  NVARCHAR(200)      NOT NULL,
    tipoEntidadAfectada             NVARCHAR(200)      NOT NULL,
    idEntidadAfectada               INT                NULL,
    descripcionOperacion            NVARCHAR(2000)     NULL,
    fechaOperacion                  DATETIME           NOT NULL DEFAULT (GETDATE()),
    origenOperacion                 NVARCHAR(200)      NULL,
    CONSTRAINT PK_RegistroActividad PRIMARY KEY CLUSTERED (idRegistroActividad),
    CONSTRAINT FK_RegistroActividad_UsuarioExterno FOREIGN KEY (idUsuarioExternoResponsable)
        REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
    CONSTRAINT FK_RegistroActividad_UsuarioInterno FOREIGN KEY (idUsuarioInternoResponsable)
        REFERENCES dbo.UsuarioInterno (idUsuarioInterno)
);
GO

-- =====================================================================
-- 4. STORED PROCEDURES - UsuarioExterno
-- =====================================================================

IF OBJECT_ID('dbo.sp_UsuarioExterno_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_Insertar;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_Insertar
    @nombre                     NVARCHAR(200),
    @apellido                   NVARCHAR(200),
    @correoElectronico          NVARCHAR(300),
    @passwordHash               NVARCHAR(510),
    @telefono                   NVARCHAR(60) = NULL,
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

IF OBJECT_ID('dbo.sp_UsuarioExterno_ObtenerPorCorreo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ObtenerPorCorreo;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ObtenerPorCorreo
    @correoElectronico NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.UsuarioExterno WHERE correoElectronico = @correoElectronico;
END
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ObtenerPorId
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.UsuarioExterno WHERE idUsuarioExterno = @idUsuarioExterno;
END
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_ActivarCuenta', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ActivarCuenta;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ActivarCuenta
    @idUsuarioExterno   INT,
    @estadoCuenta       NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UsuarioExterno
    SET estadoCuenta = @estadoCuenta,
        fechaActivacion = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    WHERE idUsuarioExterno = @idUsuarioExterno;
END
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_ActualizarPassword', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_ActualizarPassword;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_ActualizarPassword
    @idUsuarioExterno   INT,
    @passwordHash       NVARCHAR(510)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UsuarioExterno
    SET passwordHash = @passwordHash,
        fechaUltimaModificacion = GETDATE()
    WHERE idUsuarioExterno = @idUsuarioExterno;
END
GO

-- =====================================================================
-- 5. STORED PROCEDURES - CodigoActivacion
-- =====================================================================

IF OBJECT_ID('dbo.sp_CodigoActivacion_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CodigoActivacion_Insertar;
GO
CREATE PROCEDURE dbo.sp_CodigoActivacion_Insertar
    @idUsuarioExterno   INT,
    @codigo             NVARCHAR(40),
    @fechaVencimiento   DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.CodigoActivacion (idUsuarioExterno, codigo, fechaGeneracion, fechaVencimiento, utilizado)
    VALUES (@idUsuarioExterno, @codigo, GETDATE(), @fechaVencimiento, 0);

    SELECT SCOPE_IDENTITY() AS idCodigoActivacion;
END
GO

IF OBJECT_ID('dbo.sp_CodigoActivacion_ObtenerVigentePorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CodigoActivacion_ObtenerVigentePorUsuario;
GO
CREATE PROCEDURE dbo.sp_CodigoActivacion_ObtenerVigentePorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 *
    FROM dbo.CodigoActivacion
    WHERE idUsuarioExterno = @idUsuarioExterno
      AND utilizado = 0
    ORDER BY fechaGeneracion DESC;
END
GO

IF OBJECT_ID('dbo.sp_CodigoActivacion_MarcarUtilizado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CodigoActivacion_MarcarUtilizado;
GO
CREATE PROCEDURE dbo.sp_CodigoActivacion_MarcarUtilizado
    @idCodigoActivacion INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CodigoActivacion
    SET utilizado = 1,
        fechaUtilizacion = GETDATE()
    WHERE idCodigoActivacion = @idCodigoActivacion;
END
GO

-- =====================================================================
-- 6. STORED PROCEDURES - CodigoRecuperacion
-- =====================================================================

IF OBJECT_ID('dbo.sp_CodigoRecuperacion_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CodigoRecuperacion_Insertar;
GO
CREATE PROCEDURE dbo.sp_CodigoRecuperacion_Insertar
    @idUsuarioExterno   INT,
    @codigo             NVARCHAR(40),
    @fechaVencimiento   DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.CodigoRecuperacion (idUsuarioExterno, codigo, fechaGeneracion, fechaVencimiento, utilizado)
    VALUES (@idUsuarioExterno, @codigo, GETDATE(), @fechaVencimiento, 0);

    SELECT SCOPE_IDENTITY() AS idCodigoRecuperacion;
END
GO

IF OBJECT_ID('dbo.sp_CodigoRecuperacion_ObtenerVigentePorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CodigoRecuperacion_ObtenerVigentePorUsuario;
GO
CREATE PROCEDURE dbo.sp_CodigoRecuperacion_ObtenerVigentePorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 *
    FROM dbo.CodigoRecuperacion
    WHERE idUsuarioExterno = @idUsuarioExterno
      AND utilizado = 0
    ORDER BY fechaGeneracion DESC;
END
GO

IF OBJECT_ID('dbo.sp_CodigoRecuperacion_MarcarUtilizado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CodigoRecuperacion_MarcarUtilizado;
GO
CREATE PROCEDURE dbo.sp_CodigoRecuperacion_MarcarUtilizado
    @idCodigoRecuperacion INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CodigoRecuperacion
    SET utilizado = 1,
        fechaUtilizacion = GETDATE()
    WHERE idCodigoRecuperacion = @idCodigoRecuperacion;
END
GO

-- =====================================================================
-- 7. STORED PROCEDURE - RegistroActividad (bitácora)
-- =====================================================================

IF OBJECT_ID('dbo.sp_RegistroActividad_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistroActividad_Insertar;
GO
CREATE PROCEDURE dbo.sp_RegistroActividad_Insertar
    @idUsuarioExternoResponsable    INT = NULL,
    @idUsuarioInternoResponsable    INT = NULL,
    @tipoOperacion                  NVARCHAR(200),
    @tipoEntidadAfectada            NVARCHAR(200),
    @idEntidadAfectada              INT = NULL,
    @descripcionOperacion           NVARCHAR(2000) = NULL,
    @origenOperacion                NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.RegistroActividad
        (idUsuarioExternoResponsable, idUsuarioInternoResponsable, tipoOperacion,
         tipoEntidadAfectada, idEntidadAfectada, descripcionOperacion, fechaOperacion, origenOperacion)
    VALUES
        (@idUsuarioExternoResponsable, @idUsuarioInternoResponsable, @tipoOperacion,
         @tipoEntidadAfectada, @idEntidadAfectada, @descripcionOperacion, GETDATE(), @origenOperacion);
END
GO
