IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.SuscriptorNewsletter', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SuscriptorNewsletter
    (
        idSuscriptorNewsletter        INT IDENTITY(1,1) NOT NULL,
        correoElectronico             NVARCHAR(254)     NOT NULL,
        nombre                        NVARCHAR(100)     NULL,
        idUsuarioExterno              INT               NULL,
        estado                        NVARCHAR(20)      NOT NULL,
        origen                        NVARCHAR(20)      NOT NULL,
        hashTokenConfirmacion         CHAR(64)          NULL,
        vencimientoConfirmacion       DATETIME          NULL,
        fechaUltimoEnvioConfirmacion  DATETIME          NULL,
        tokenPreferencias             VARCHAR(64)       NOT NULL,
        fechaAlta                     DATETIME          NOT NULL CONSTRAINT DF_SuscriptorNewsletter_fechaAlta DEFAULT (GETDATE()),
        fechaConfirmacion             DATETIME          NULL,
        fechaBaja                     DATETIME          NULL,
        CONSTRAINT PK_SuscriptorNewsletter PRIMARY KEY CLUSTERED (idSuscriptorNewsletter),
        CONSTRAINT UQ_SuscriptorNewsletter_Correo UNIQUE (correoElectronico),
        CONSTRAINT UQ_SuscriptorNewsletter_Token UNIQUE (tokenPreferencias),
        CONSTRAINT FK_SuscriptorNewsletter_Usuario FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT CK_SuscriptorNewsletter_Estado CHECK (estado IN (N'Pendiente', N'Activo', N'Baja')),
        CONSTRAINT CK_SuscriptorNewsletter_Origen CHECK (origen IN (N'Formulario', N'Cuenta'))
    );
END
GO

IF OBJECT_ID(N'dbo.SuscriptorNewsletterCategoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SuscriptorNewsletterCategoria
    (
        idSuscriptorNewsletter  INT            NOT NULL,
        categoria               NVARCHAR(100)  NOT NULL,
        CONSTRAINT PK_SuscriptorNewsletterCategoria PRIMARY KEY (idSuscriptorNewsletter, categoria),
        CONSTRAINT FK_SuscriptorNewsletterCategoria_Suscriptor FOREIGN KEY (idSuscriptorNewsletter)
            REFERENCES dbo.SuscriptorNewsletter (idSuscriptorNewsletter) ON DELETE CASCADE,
        CONSTRAINT CK_SuscriptorNewsletterCategoria CHECK (categoria IN (N'Institucional', N'Catalogo', N'Consejos', N'Comunidad'))
    );
END
GO

-- Consultas de un suscriptor (con sus categorías separadas por coma).
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Obtener;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Obtener
    @correoElectronico      NVARCHAR(254) = NULL,
    @tokenPreferencias      VARCHAR(64) = NULL,
    @hashTokenConfirmacion  CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.idSuscriptorNewsletter, s.correoElectronico, s.nombre, s.idUsuarioExterno, s.estado, s.origen,
           s.vencimientoConfirmacion, s.fechaUltimoEnvioConfirmacion, s.tokenPreferencias, s.fechaAlta,
           s.fechaConfirmacion, s.fechaBaja,
           STUFF((SELECT N',' + c.categoria FROM dbo.SuscriptorNewsletterCategoria c
                  WHERE c.idSuscriptorNewsletter = s.idSuscriptorNewsletter ORDER BY c.categoria
                  FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(400)'), 1, 1, N'') AS categorias
    FROM dbo.SuscriptorNewsletter s
    WHERE (@correoElectronico IS NOT NULL AND s.correoElectronico = @correoElectronico)
       OR (@tokenPreferencias IS NOT NULL AND s.tokenPreferencias = @tokenPreferencias)
       OR (@hashTokenConfirmacion IS NOT NULL AND s.hashTokenConfirmacion = @hashTokenConfirmacion);
END
GO

IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Insertar;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Insertar
    @correoElectronico        NVARCHAR(254),
    @nombre                   NVARCHAR(100) = NULL,
    @hashTokenConfirmacion    CHAR(64),
    @vencimientoConfirmacion  DATETIME,
    @tokenPreferencias        VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SuscriptorNewsletter
        (correoElectronico, nombre, idUsuarioExterno, estado, origen, hashTokenConfirmacion, vencimientoConfirmacion,
         fechaUltimoEnvioConfirmacion, tokenPreferencias)
    VALUES
        (@correoElectronico, @nombre,
         (SELECT TOP 1 idUsuarioExterno FROM dbo.UsuarioExterno WHERE correoElectronico = @correoElectronico AND activo = 1),
         N'Pendiente', N'Formulario', @hashTokenConfirmacion, @vencimientoConfirmacion, GETDATE(), @tokenPreferencias);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

-- Nuevo pedido de confirmación (suscriptor pendiente o dado de baja que se
-- vuelve a suscribir).
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_RenovarConfirmacion', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_RenovarConfirmacion;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_RenovarConfirmacion
    @idSuscriptorNewsletter   INT,
    @nombre                   NVARCHAR(100) = NULL,
    @hashTokenConfirmacion    CHAR(64),
    @vencimientoConfirmacion  DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SuscriptorNewsletter
    SET estado = N'Pendiente',
        nombre = ISNULL(@nombre, nombre),
        hashTokenConfirmacion = @hashTokenConfirmacion,
        vencimientoConfirmacion = @vencimientoConfirmacion,
        fechaUltimoEnvioConfirmacion = GETDATE()
    WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter AND estado IN (N'Pendiente', N'Baja');

    SELECT @@ROWCOUNT;
END
GO

-- Aviso "ya estás suscripto" enviado (para no mandarlo más de una vez cada
-- pocos minutos).
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_MarcarAvisoEnviado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_MarcarAvisoEnviado;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_MarcarAvisoEnviado
    @idSuscriptorNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SuscriptorNewsletter SET fechaUltimoEnvioConfirmacion = GETDATE() WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter;
END
GO

-- Doble confirmación: solo con el token vigente.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Confirmar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Confirmar;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Confirmar
    @idSuscriptorNewsletter  INT,
    @hashTokenConfirmacion   CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SuscriptorNewsletter
    SET estado = N'Activo',
        hashTokenConfirmacion = NULL,
        vencimientoConfirmacion = NULL,
        fechaConfirmacion = GETDATE(),
        fechaBaja = NULL
    WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter
      AND estado = N'Pendiente'
      AND hashTokenConfirmacion = @hashTokenConfirmacion
      AND vencimientoConfirmacion >= GETDATE();

    SELECT @@ROWCOUNT;
END
GO

-- Baja o reactivación desde el enlace de preferencias.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_CambiarEstado
    @idSuscriptorNewsletter  INT,
    @estadoAnterior          NVARCHAR(20),
    @estadoNuevo             NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SuscriptorNewsletter
    SET estado = @estadoNuevo,
        fechaBaja = CASE WHEN @estadoNuevo = N'Baja' THEN GETDATE() ELSE NULL END,
        hashTokenConfirmacion = NULL,
        vencimientoConfirmacion = NULL
    WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter AND estado = @estadoAnterior;

    SELECT @@ROWCOUNT;
END
GO

IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_GuardarCategorias', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_GuardarCategorias;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_GuardarCategorias
    @idSuscriptorNewsletter  INT,
    @institucional           BIT,
    @catalogo                BIT,
    @consejos                BIT,
    @comunidad               BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        DELETE FROM dbo.SuscriptorNewsletterCategoria WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter;

        INSERT INTO dbo.SuscriptorNewsletterCategoria (idSuscriptorNewsletter, categoria)
        SELECT @idSuscriptorNewsletter, v.categoria
        FROM (VALUES (N'Institucional', @institucional), (N'Catalogo', @catalogo),
                     (N'Consejos', @consejos), (N'Comunidad', @comunidad)) v (categoria, elegida)
        WHERE v.elegida = 1;
    COMMIT TRANSACTION;
END
GO

IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Resumen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Resumen;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Resumen
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(SUM(CASE WHEN estado = N'Activo' THEN 1 ELSE 0 END), 0) AS activos,
           ISNULL(SUM(CASE WHEN estado = N'Pendiente' THEN 1 ELSE 0 END), 0) AS pendientes,
           ISNULL(SUM(CASE WHEN estado = N'Baja' THEN 1 ELSE 0 END), 0) AS bajas,
           ISNULL(SUM(CASE WHEN estado = N'Activo' AND origen = N'Formulario' THEN 1 ELSE 0 END), 0) AS activosSinCuenta
    FROM dbo.SuscriptorNewsletter;
END
GO

-- Destinatarios de un newsletter.
--   @criterio: Activos, Gestores, Solicitantes, Todos (usuarios con cuenta
--   según su perfil) o Suscriptores (solo suscriptores sin cuenta).
--   Activos y Todos suman además a los suscriptores confirmados.
--   @categoria: la de la novedad; se excluye a quien eligió otras.
--   Nadie dado de baja recibe el newsletter.
IF OBJECT_ID('dbo.sp_Newsletter_ListarDestinatarios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Newsletter_ListarDestinatarios;
GO
CREATE PROCEDURE dbo.sp_Newsletter_ListarDestinatarios
    @criterio   NVARCHAR(50),
    @categoria  NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @usuarios TABLE (idUsuarioExterno INT PRIMARY KEY, correoElectronico NVARCHAR(254), nombre NVARCHAR(100));

    IF @criterio <> N'Suscriptores'
        INSERT INTO @usuarios (idUsuarioExterno, correoElectronico, nombre)
        SELECT u.idUsuarioExterno, u.correoElectronico, u.nombre
        FROM dbo.UsuarioExterno u
        WHERE u.activo = 1
          AND u.estadoCuenta = N'Activa'
          AND ((@criterio = N'Gestores' AND u.perfilUsuario = N'GestorEspacios')
               OR (@criterio = N'Solicitantes' AND u.perfilUsuario = N'ExternoSolicitante')
               OR @criterio NOT IN (N'Gestores', N'Solicitantes'));

    -- Registro de suscriptor para los usuarios que todavía no lo tienen
    -- (así también reciben el enlace de baja).
    INSERT INTO dbo.SuscriptorNewsletter (correoElectronico, nombre, idUsuarioExterno, estado, origen, tokenPreferencias, fechaConfirmacion)
    SELECT u.correoElectronico, u.nombre, u.idUsuarioExterno, N'Activo', N'Cuenta',
           CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(NEWID() AS VARCHAR(36)) + CONVERT(VARCHAR(64), CRYPT_GEN_RANDOM(32), 2)), 2),
           GETDATE()
    FROM @usuarios u
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SuscriptorNewsletter s WHERE s.correoElectronico = u.correoElectronico);

    SELECT s.correoElectronico, ISNULL(u.nombre, s.nombre) AS nombre, s.tokenPreferencias
    FROM dbo.SuscriptorNewsletter s
    LEFT JOIN @usuarios u ON u.correoElectronico = s.correoElectronico
    WHERE s.estado = N'Activo'
      AND (u.idUsuarioExterno IS NOT NULL
           OR (s.origen = N'Formulario' AND @criterio IN (N'Activos', N'Todos', N'Suscriptores')))
      AND (NOT EXISTS (SELECT 1 FROM dbo.SuscriptorNewsletterCategoria c WHERE c.idSuscriptorNewsletter = s.idSuscriptorNewsletter)
           OR EXISTS (SELECT 1 FROM dbo.SuscriptorNewsletterCategoria c WHERE c.idSuscriptorNewsletter = s.idSuscriptorNewsletter AND c.categoria = @categoria))
    ORDER BY s.correoElectronico;
END
GO
