IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ===========================================================================
-- Newsletter (ítems 4, 5 y 13).
--   * Categorías de novedades en una tabla administrable (CategoriaNovedad)
--     en lugar de valores fijos en el código y en un CHECK.
--   * Los destinatarios de un envío se listan sin crear suscripciones: solo
--     reciben los suscriptores confirmados.
--   * Cada envío guarda su estado y el de cada destinatario, para reintentar
--     solo los que fallaron.
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- Suscripciones creadas automáticamente por la versión anterior de
-- sp_Newsletter_ListarDestinatarios (origen Cuenta, sin ningún pedido de la
-- persona): no son suscripciones confirmadas y se quitan. Se hace una sola
-- vez, antes de crear las tablas de envíos.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.EnvioNewsletter', N'U') IS NULL AND OBJECT_ID(N'dbo.SuscriptorNewsletter', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.SuscriptorNewsletter
    WHERE origen = N'Cuenta'
      AND fechaUltimoEnvioConfirmacion IS NULL;

    PRINT CONCAT(N'Suscripciones automáticas quitadas: ', @@ROWCOUNT);
END
GO

-- ---------------------------------------------------------------------------
-- Categorías.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CategoriaNovedad', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CategoriaNovedad
    (
        codigo                   NVARCHAR(100)  NOT NULL,
        nombre                   NVARCHAR(60)   NOT NULL,
        orden                    INT            NOT NULL CONSTRAINT DF_CategoriaNovedad_orden DEFAULT (0),
        activa                   BIT            NOT NULL CONSTRAINT DF_CategoriaNovedad_activa DEFAULT (1),
        fechaAlta                DATETIME       NOT NULL CONSTRAINT DF_CategoriaNovedad_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion  DATETIME       NULL,
        CONSTRAINT PK_CategoriaNovedad PRIMARY KEY CLUSTERED (codigo),
        CONSTRAINT UQ_CategoriaNovedad_Nombre UNIQUE (nombre)
    );
END
GO

INSERT INTO dbo.CategoriaNovedad (codigo, nombre, orden)
SELECT v.codigo, v.nombre, v.orden
FROM (VALUES (N'Institucional', N'Institucional', 10),
             (N'Catalogo', N'Nuevos espacios', 20),
             (N'Consejos', N'Guías y consejos', 30),
             (N'Comunidad', N'Comunidad artística', 40)) v (codigo, nombre, orden)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad c WHERE c.codigo = v.codigo);
GO

-- Cualquier otra categoría que ya tengan las novedades (para poder crear la FK).
INSERT INTO dbo.CategoriaNovedad (codigo, nombre, orden, activa)
SELECT DISTINCT n.categoria, LEFT(n.categoria, 60), 100, 0
FROM dbo.Novedad n
WHERE NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad c WHERE c.codigo = n.categoria)
  AND NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad c WHERE c.nombre = LEFT(n.categoria, 60));
GO

IF OBJECT_ID(N'dbo.CK_SuscriptorNewsletterCategoria', N'C') IS NOT NULL
    ALTER TABLE dbo.SuscriptorNewsletterCategoria DROP CONSTRAINT CK_SuscriptorNewsletterCategoria;
GO

IF OBJECT_ID(N'dbo.FK_SuscriptorNewsletterCategoria_Categoria', N'F') IS NULL
    ALTER TABLE dbo.SuscriptorNewsletterCategoria WITH CHECK
        ADD CONSTRAINT FK_SuscriptorNewsletterCategoria_Categoria FOREIGN KEY (categoria) REFERENCES dbo.CategoriaNovedad (codigo);
GO

IF OBJECT_ID(N'dbo.FK_Novedad_Categoria', N'F') IS NULL
    ALTER TABLE dbo.Novedad WITH CHECK
        ADD CONSTRAINT FK_Novedad_Categoria FOREIGN KEY (categoria) REFERENCES dbo.CategoriaNovedad (codigo);
GO

IF OBJECT_ID('dbo.sp_CategoriaNovedad_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CategoriaNovedad_Listar;
GO
CREATE PROCEDURE dbo.sp_CategoriaNovedad_Listar
    @soloActivas BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.codigo, c.nombre, c.orden, c.activa, c.fechaAlta, c.fechaUltimaModificacion,
           (SELECT COUNT(*) FROM dbo.Novedad n WHERE n.categoria = c.codigo AND n.activo = 1) AS cantidadNovedades,
           (SELECT COUNT(*) FROM dbo.SuscriptorNewsletterCategoria sc
            JOIN dbo.SuscriptorNewsletter s ON s.idSuscriptorNewsletter = sc.idSuscriptorNewsletter
            WHERE sc.categoria = c.codigo AND s.estado = N'Activo') AS cantidadSuscriptores
    FROM dbo.CategoriaNovedad c
    WHERE @soloActivas = 0 OR c.activa = 1
    ORDER BY c.orden, c.nombre;
END
GO

IF OBJECT_ID('dbo.sp_CategoriaNovedad_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CategoriaNovedad_Insertar;
GO
CREATE PROCEDURE dbo.sp_CategoriaNovedad_Insertar
    @codigo  NVARCHAR(100),
    @nombre  NVARCHAR(60),
    @orden   INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE nombre = @nombre)
        THROW 51070, N'Ya existe una categoría con ese nombre.', 1;

    IF EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE codigo = @codigo)
        THROW 51071, N'Ya existe una categoría con ese código.', 1;

    INSERT INTO dbo.CategoriaNovedad (codigo, nombre, orden, activa)
    VALUES (@codigo, @nombre, @orden, 1);
END
GO

IF OBJECT_ID('dbo.sp_CategoriaNovedad_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CategoriaNovedad_Modificar;
GO
CREATE PROCEDURE dbo.sp_CategoriaNovedad_Modificar
    @codigo  NVARCHAR(100),
    @nombre  NVARCHAR(60),
    @orden   INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE codigo = @codigo)
        THROW 51072, N'La categoría no existe.', 1;

    IF EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE nombre = @nombre AND codigo <> @codigo)
        THROW 51070, N'Ya existe una categoría con ese nombre.', 1;

    UPDATE dbo.CategoriaNovedad
    SET nombre = @nombre,
        orden = @orden,
        fechaUltimaModificacion = GETDATE()
    WHERE codigo = @codigo;
END
GO

-- Desactivar: deja de ofrecerse para nuevas novedades y en las preferencias
-- de los suscriptores; las novedades que ya la tienen la conservan.
IF OBJECT_ID('dbo.sp_CategoriaNovedad_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CategoriaNovedad_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_CategoriaNovedad_CambiarEstado
    @codigo  NVARCHAR(100),
    @activa  BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WITH (UPDLOCK, HOLDLOCK) WHERE codigo = @codigo)
        THROW 51072, N'La categoría no existe.', 1;

    IF @activa = 0 AND NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WITH (UPDLOCK, HOLDLOCK) WHERE activa = 1 AND codigo <> @codigo)
        THROW 51073, N'Tiene que quedar al menos una categoría activa.', 1;

    UPDATE dbo.CategoriaNovedad
    SET activa = @activa,
        fechaUltimaModificacion = GETDATE()
    WHERE codigo = @codigo;

    COMMIT TRANSACTION;
END
GO

-- ---------------------------------------------------------------------------
-- Suscriptores.
-- ---------------------------------------------------------------------------

-- Categorías elegidas, separadas por coma. Solo se guardan las activas.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_GuardarCategorias', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_GuardarCategorias;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_GuardarCategorias
    @idSuscriptorNewsletter  INT,
    @categorias              NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        DELETE FROM dbo.SuscriptorNewsletterCategoria WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter;

        INSERT INTO dbo.SuscriptorNewsletterCategoria (idSuscriptorNewsletter, categoria)
        SELECT DISTINCT @idSuscriptorNewsletter, c.codigo
        FROM STRING_SPLIT(ISNULL(@categorias, N''), N',') v
        JOIN dbo.CategoriaNovedad c ON c.codigo = LTRIM(RTRIM(v.value)) AND c.activa = 1;
    COMMIT TRANSACTION;
END
GO

-- @idUsuarioExterno: cuenta desde la que se pidió (Mi perfil). Un pedido
-- hecho desde el formulario público no se asocia a ninguna cuenta.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Insertar;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Insertar
    @correoElectronico        NVARCHAR(254),
    @nombre                   NVARCHAR(100) = NULL,
    @hashTokenConfirmacion    CHAR(64),
    @vencimientoConfirmacion  DATETIME,
    @tokenPreferencias        VARCHAR(64),
    @idUsuarioExterno         INT = NULL,
    @origen                   NVARCHAR(20) = N'Formulario'
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SuscriptorNewsletter
        (correoElectronico, nombre, idUsuarioExterno, estado, origen, hashTokenConfirmacion, vencimientoConfirmacion,
         fechaUltimoEnvioConfirmacion, tokenPreferencias)
    VALUES
        (@correoElectronico, @nombre,
         @idUsuarioExterno,
         N'Pendiente', @origen, @hashTokenConfirmacion, @vencimientoConfirmacion, GETDATE(), @tokenPreferencias);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_RenovarConfirmacion', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_RenovarConfirmacion;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_RenovarConfirmacion
    @idSuscriptorNewsletter   INT,
    @nombre                   NVARCHAR(100) = NULL,
    @hashTokenConfirmacion    CHAR(64),
    @vencimientoConfirmacion  DATETIME,
    @idUsuarioExterno         INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SuscriptorNewsletter
    SET estado = N'Pendiente',
        nombre = ISNULL(@nombre, nombre),
        idUsuarioExterno = ISNULL(@idUsuarioExterno, idUsuarioExterno),
        hashTokenConfirmacion = @hashTokenConfirmacion,
        vencimientoConfirmacion = @vencimientoConfirmacion,
        fechaUltimoEnvioConfirmacion = GETDATE()
    WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter AND estado IN (N'Pendiente', N'Baja');

    SELECT @@ROWCOUNT;
END
GO

-- El correo de confirmación (o el aviso) no salió: no cuenta para la espera
-- entre envíos, así se puede volver a pedir enseguida.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_AnularEnvioConfirmacion', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_AnularEnvioConfirmacion;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_AnularEnvioConfirmacion
    @idSuscriptorNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SuscriptorNewsletter SET fechaUltimoEnvioConfirmacion = NULL WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter;
END
GO

-- "Con cuenta" = existe una cuenta externa activa con el mismo correo.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Resumen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Resumen;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Resumen
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(SUM(CASE WHEN s.estado = N'Activo' THEN 1 ELSE 0 END), 0) AS activos,
           ISNULL(SUM(CASE WHEN s.estado = N'Pendiente' THEN 1 ELSE 0 END), 0) AS pendientes,
           ISNULL(SUM(CASE WHEN s.estado = N'Baja' THEN 1 ELSE 0 END), 0) AS bajas,
           ISNULL(SUM(CASE WHEN s.estado = N'Activo' AND c.tieneCuenta = 0 THEN 1 ELSE 0 END), 0) AS activosSinCuenta
    FROM dbo.SuscriptorNewsletter s
    CROSS APPLY (SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.UsuarioExterno u
                                          WHERE u.correoElectronico = s.correoElectronico AND u.activo = 1 AND u.estadoCuenta = N'Activa')
                             THEN 1 ELSE 0 END AS tieneCuenta) c;
END
GO

-- Administración de suscriptores.
--   @estado: Activo, Pendiente, Baja o NULL (todos).
--   @conCuenta: 1 con cuenta activa, 0 sin cuenta, NULL todos.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Listar;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Listar
    @texto      NVARCHAR(254) = NULL,
    @estado     NVARCHAR(20) = NULL,
    @conCuenta  BIT = NULL,
    @maximo     INT = 200
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(260) = CASE WHEN NULLIF(LTRIM(RTRIM(@texto)), N'') IS NULL THEN NULL
        ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@texto)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;

    ;WITH base AS
    (
        SELECT s.idSuscriptorNewsletter, s.correoElectronico, s.nombre, s.idUsuarioExterno, s.estado, s.origen,
               s.vencimientoConfirmacion, s.fechaUltimoEnvioConfirmacion, s.tokenPreferencias, s.fechaAlta,
               s.fechaConfirmacion, s.fechaBaja,
               STUFF((SELECT N',' + c.categoria FROM dbo.SuscriptorNewsletterCategoria c
                      WHERE c.idSuscriptorNewsletter = s.idSuscriptorNewsletter ORDER BY c.categoria
                      FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(400)'), 1, 1, N'') AS categorias,
               u.idUsuarioExterno AS idCuenta,
               u.perfilUsuario AS perfilCuenta
        FROM dbo.SuscriptorNewsletter s
        OUTER APPLY (SELECT TOP 1 x.idUsuarioExterno, x.perfilUsuario FROM dbo.UsuarioExterno x
                     WHERE x.correoElectronico = s.correoElectronico AND x.activo = 1 AND x.estadoCuenta = N'Activa'
                     ORDER BY x.idUsuarioExterno) u
        WHERE (@estado IS NULL OR s.estado = @estado)
          AND (@patron IS NULL OR s.correoElectronico LIKE @patron OR s.nombre LIKE @patron)
    )
    SELECT TOP (CASE WHEN @maximo BETWEEN 1 AND 1000 THEN @maximo ELSE 200 END) *,
           COUNT(*) OVER () AS totalFiltrado
    FROM base
    WHERE @conCuenta IS NULL OR (@conCuenta = 1 AND idCuenta IS NOT NULL) OR (@conCuenta = 0 AND idCuenta IS NULL)
    ORDER BY fechaAlta DESC, idSuscriptorNewsletter DESC;
END
GO

IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_ObtenerPorId
    @idSuscriptorNewsletter INT
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
    WHERE s.idSuscriptorNewsletter = @idSuscriptorNewsletter;
END
GO

-- ---------------------------------------------------------------------------
-- Destinatarios: solo suscriptores confirmados (estado Activo). No crea
-- suscripciones.
--   @criterio: Activos o Todos (todos los confirmados), Gestores y
--   Solicitantes (confirmados cuyo correo es el de una cuenta activa con ese
--   perfil) o Suscriptores (confirmados sin cuenta).
--   @categoria: la de la novedad; se excluye a quien eligió otras.
--   Una suscripción asociada a una cuenta dada de baja no recibe envíos.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.fn_Newsletter_Destinatarios', 'IF') IS NOT NULL DROP FUNCTION dbo.fn_Newsletter_Destinatarios;
GO
CREATE FUNCTION dbo.fn_Newsletter_Destinatarios
(
    @criterio   NVARCHAR(50),
    @categoria  NVARCHAR(100)
)
RETURNS TABLE
AS
RETURN
(
    SELECT s.idSuscriptorNewsletter, s.correoElectronico, ISNULL(u.nombre, s.nombre) AS nombre, s.tokenPreferencias
    FROM dbo.SuscriptorNewsletter s
    OUTER APPLY (SELECT TOP 1 x.idUsuarioExterno, x.nombre, x.perfilUsuario FROM dbo.UsuarioExterno x
                 WHERE x.correoElectronico = s.correoElectronico AND x.activo = 1 AND x.estadoCuenta = N'Activa'
                 ORDER BY x.idUsuarioExterno) u
    WHERE s.estado = N'Activo'
      AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno b
                      WHERE b.idUsuarioExterno = s.idUsuarioExterno AND b.fechaBaja IS NOT NULL AND b.activo = 0)
      AND (@criterio IN (N'Activos', N'Todos')
           OR (@criterio = N'Gestores' AND u.perfilUsuario = N'GestorEspacios')
           OR (@criterio = N'Solicitantes' AND u.perfilUsuario = N'ExternoSolicitante')
           OR (@criterio = N'Suscriptores' AND u.idUsuarioExterno IS NULL))
      AND (NOT EXISTS (SELECT 1 FROM dbo.SuscriptorNewsletterCategoria c WHERE c.idSuscriptorNewsletter = s.idSuscriptorNewsletter)
           OR EXISTS (SELECT 1 FROM dbo.SuscriptorNewsletterCategoria c WHERE c.idSuscriptorNewsletter = s.idSuscriptorNewsletter AND c.categoria = @categoria))
);
GO

IF OBJECT_ID('dbo.sp_Newsletter_ListarDestinatarios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Newsletter_ListarDestinatarios;
GO
CREATE PROCEDURE dbo.sp_Newsletter_ListarDestinatarios
    @criterio   NVARCHAR(50),
    @categoria  NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idSuscriptorNewsletter, correoElectronico, nombre, tokenPreferencias
    FROM dbo.fn_Newsletter_Destinatarios(@criterio, @categoria)
    ORDER BY correoElectronico;
END
GO

-- ---------------------------------------------------------------------------
-- Envíos y estado por destinatario.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.EnvioNewsletter', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnvioNewsletter
    (
        idEnvioNewsletter       INT IDENTITY(1,1) NOT NULL,
        idNovedad               INT               NOT NULL,
        criterio                NVARCHAR(50)      NOT NULL,
        categoria               NVARCHAR(100)     NOT NULL,
        estado                  NVARCHAR(20)      NOT NULL,
        cantidadDestinatarios   INT               NOT NULL CONSTRAINT DF_EnvioNewsletter_dest DEFAULT (0),
        cantidadEnviados        INT               NOT NULL CONSTRAINT DF_EnvioNewsletter_env DEFAULT (0),
        cantidadFallidos        INT               NOT NULL CONSTRAINT DF_EnvioNewsletter_fal DEFAULT (0),
        cantidadOmitidos        INT               NOT NULL CONSTRAINT DF_EnvioNewsletter_omi DEFAULT (0),
        idUsuarioInterno        INT               NULL,
        fechaInicio             DATETIME          NOT NULL CONSTRAINT DF_EnvioNewsletter_inicio DEFAULT (GETDATE()),
        fechaUltimaActividad    DATETIME          NOT NULL CONSTRAINT DF_EnvioNewsletter_actividad DEFAULT (GETDATE()),
        fechaFin                DATETIME          NULL,
        CONSTRAINT PK_EnvioNewsletter PRIMARY KEY CLUSTERED (idEnvioNewsletter),
        CONSTRAINT UQ_EnvioNewsletter_Novedad UNIQUE (idNovedad),
        CONSTRAINT FK_EnvioNewsletter_Novedad FOREIGN KEY (idNovedad) REFERENCES dbo.Novedad (idNovedad),
        CONSTRAINT FK_EnvioNewsletter_UsuarioInterno FOREIGN KEY (idUsuarioInterno) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_EnvioNewsletter_Estado CHECK (estado IN (N'EnCurso', N'Completo', N'Parcial', N'SinDestinatarios', N'Fallido'))
    );
END
GO

IF OBJECT_ID(N'dbo.EnvioNewsletterDestinatario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnvioNewsletterDestinatario
    (
        idEnvioNewsletter       INT            NOT NULL,
        idSuscriptorNewsletter  INT            NOT NULL,
        correoElectronico       NVARCHAR(254)  NOT NULL,
        nombre                  NVARCHAR(200)  NULL,
        estado                  NVARCHAR(20)   NOT NULL CONSTRAINT DF_EnvioNewsletterDestinatario_estado DEFAULT (N'Pendiente'),
        intentos                INT            NOT NULL CONSTRAINT DF_EnvioNewsletterDestinatario_intentos DEFAULT (0),
        fechaUltimoIntento      DATETIME       NULL,
        fechaEnvio              DATETIME       NULL,
        CONSTRAINT PK_EnvioNewsletterDestinatario PRIMARY KEY CLUSTERED (idEnvioNewsletter, idSuscriptorNewsletter),
        CONSTRAINT FK_EnvioNewsletterDestinatario_Envio FOREIGN KEY (idEnvioNewsletter) REFERENCES dbo.EnvioNewsletter (idEnvioNewsletter),
        CONSTRAINT FK_EnvioNewsletterDestinatario_Suscriptor FOREIGN KEY (idSuscriptorNewsletter) REFERENCES dbo.SuscriptorNewsletter (idSuscriptorNewsletter),
        CONSTRAINT CK_EnvioNewsletterDestinatario_Estado CHECK (estado IN (N'Pendiente', N'Enviado', N'Fallido', N'Omitido'))
    );
END
GO

-- Crea el envío de una novedad con la foto de sus destinatarios. Una novedad
-- tiene un único envío (los fallidos se reintentan sobre el mismo).
IF OBJECT_ID('dbo.sp_EnvioNewsletter_Iniciar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_Iniciar;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_Iniciar
    @idNovedad         INT,
    @criterio          NVARCHAR(50),
    @idUsuarioInterno  INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @categoria NVARCHAR(100), @enviada BIT;
    SELECT @categoria = categoria, @enviada = enviadaPorCorreo
    FROM dbo.Novedad WITH (UPDLOCK, HOLDLOCK)
    WHERE idNovedad = @idNovedad AND activo = 1;

    IF @categoria IS NULL
        THROW 51080, N'No se encontró la novedad seleccionada.', 1;

    IF @enviada = 1 OR EXISTS (SELECT 1 FROM dbo.EnvioNewsletter WITH (UPDLOCK, HOLDLOCK) WHERE idNovedad = @idNovedad)
        THROW 51081, N'Esta novedad ya se envió por correo. Si quedaron destinatarios sin recibirla, usá "Reintentar fallidos".', 1;

    INSERT INTO dbo.EnvioNewsletter (idNovedad, criterio, categoria, estado, idUsuarioInterno)
    VALUES (@idNovedad, @criterio, @categoria, N'EnCurso', @idUsuarioInterno);

    DECLARE @idEnvio INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.EnvioNewsletterDestinatario (idEnvioNewsletter, idSuscriptorNewsletter, correoElectronico, nombre)
    SELECT @idEnvio, d.idSuscriptorNewsletter, d.correoElectronico, d.nombre
    FROM dbo.fn_Newsletter_Destinatarios(@criterio, @categoria) d;

    DECLARE @cantidad INT = @@ROWCOUNT;
    UPDATE dbo.EnvioNewsletter SET cantidadDestinatarios = @cantidad WHERE idEnvioNewsletter = @idEnvio;

    COMMIT TRANSACTION;

    SELECT @idEnvio AS idEnvioNewsletter;
END
GO

-- Toma un envío Parcial o Fallido para reintentar (o uno que quedó EnCurso
-- sin actividad hace más de 15 minutos). Vacío si no hay nada para
-- reintentar o si otra persona ya lo está reintentando.
IF OBJECT_ID('dbo.sp_EnvioNewsletter_TomarParaReintento', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_TomarParaReintento;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_TomarParaReintento
    @idNovedad INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.EnvioNewsletter
    SET estado = N'EnCurso',
        fechaUltimaActividad = GETDATE(),
        fechaFin = NULL
    OUTPUT inserted.idEnvioNewsletter
    WHERE idNovedad = @idNovedad
      AND (estado IN (N'Parcial', N'Fallido')
           OR (estado = N'EnCurso' AND fechaUltimaActividad < DATEADD(MINUTE, -15, GETDATE())));
END
GO

-- Destinatarios que faltan (pendientes o fallidos). Antes se marcan como
-- omitidos los que se dieron de baja o ya no están confirmados.
IF OBJECT_ID('dbo.sp_EnvioNewsletter_ListarPorEnviar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_ListarPorEnviar;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_ListarPorEnviar
    @idEnvioNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE d
    SET estado = N'Omitido'
    FROM dbo.EnvioNewsletterDestinatario d
    JOIN dbo.SuscriptorNewsletter s ON s.idSuscriptorNewsletter = d.idSuscriptorNewsletter
    WHERE d.idEnvioNewsletter = @idEnvioNewsletter
      AND d.estado IN (N'Pendiente', N'Fallido')
      AND s.estado <> N'Activo';

    SELECT d.idSuscriptorNewsletter, s.correoElectronico, ISNULL(d.nombre, s.nombre) AS nombre, s.tokenPreferencias
    FROM dbo.EnvioNewsletterDestinatario d
    JOIN dbo.SuscriptorNewsletter s ON s.idSuscriptorNewsletter = d.idSuscriptorNewsletter
    WHERE d.idEnvioNewsletter = @idEnvioNewsletter
      AND d.estado IN (N'Pendiente', N'Fallido')
    ORDER BY s.correoElectronico;
END
GO

IF OBJECT_ID('dbo.sp_EnvioNewsletter_RegistrarResultado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_RegistrarResultado;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_RegistrarResultado
    @idEnvioNewsletter       INT,
    @idSuscriptorNewsletter  INT,
    @enviado                 BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.EnvioNewsletterDestinatario
    SET estado = CASE WHEN @enviado = 1 THEN N'Enviado' ELSE N'Fallido' END,
        intentos = intentos + 1,
        fechaUltimoIntento = GETDATE(),
        fechaEnvio = CASE WHEN @enviado = 1 THEN GETDATE() ELSE fechaEnvio END
    WHERE idEnvioNewsletter = @idEnvioNewsletter
      AND idSuscriptorNewsletter = @idSuscriptorNewsletter
      AND estado IN (N'Pendiente', N'Fallido');

    UPDATE dbo.EnvioNewsletter SET fechaUltimaActividad = GETDATE() WHERE idEnvioNewsletter = @idEnvioNewsletter;
END
GO

-- Cierra el envío: Completo (todos recibieron), Parcial (algunos fallaron),
-- Fallido (no llegó a nadie) o SinDestinatarios. Si llegó a alguien (o no
-- había nadie), la novedad queda marcada como enviada.
IF OBJECT_ID('dbo.sp_EnvioNewsletter_Finalizar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_Finalizar;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_Finalizar
    @idEnvioNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @total INT, @enviados INT, @fallidos INT, @omitidos INT;
    SELECT @total = COUNT(*),
           @enviados = ISNULL(SUM(CASE WHEN estado = N'Enviado' THEN 1 ELSE 0 END), 0),
           @fallidos = ISNULL(SUM(CASE WHEN estado IN (N'Fallido', N'Pendiente') THEN 1 ELSE 0 END), 0),
           @omitidos = ISNULL(SUM(CASE WHEN estado = N'Omitido' THEN 1 ELSE 0 END), 0)
    FROM dbo.EnvioNewsletterDestinatario
    WHERE idEnvioNewsletter = @idEnvioNewsletter;

    DECLARE @estado NVARCHAR(20) =
        CASE WHEN @total = 0 THEN N'SinDestinatarios'
             WHEN @fallidos = 0 AND @enviados = 0 THEN N'SinDestinatarios'
             WHEN @fallidos = 0 THEN N'Completo'
             WHEN @enviados = 0 THEN N'Fallido'
             ELSE N'Parcial' END;

    BEGIN TRANSACTION;

    UPDATE dbo.EnvioNewsletter
    SET estado = @estado,
        cantidadDestinatarios = @total,
        cantidadEnviados = @enviados,
        cantidadFallidos = @fallidos,
        cantidadOmitidos = @omitidos,
        fechaUltimaActividad = GETDATE(),
        fechaFin = GETDATE()
    WHERE idEnvioNewsletter = @idEnvioNewsletter;

    IF @estado <> N'Fallido'
        UPDATE n
        SET enviadaPorCorreo = 1,
            destinatarioNewsletter = e.criterio,
            fechaEnvioNewsletter = ISNULL(n.fechaEnvioNewsletter, GETDATE()),
            fechaUltimaModificacion = GETDATE()
        FROM dbo.Novedad n
        JOIN dbo.EnvioNewsletter e ON e.idNovedad = n.idNovedad
        WHERE e.idEnvioNewsletter = @idEnvioNewsletter;

    COMMIT TRANSACTION;

    SELECT @estado AS estado, @total AS cantidadDestinatarios, @enviados AS cantidadEnviados,
           @fallidos AS cantidadFallidos, @omitidos AS cantidadOmitidos;
END
GO

IF OBJECT_ID('dbo.sp_EnvioNewsletter_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_Listar;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_Listar
    @idNovedad  INT = NULL,
    @maximo     INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (CASE WHEN @maximo BETWEEN 1 AND 500 THEN @maximo ELSE 100 END)
           e.idEnvioNewsletter, e.idNovedad, n.titulo AS tituloNovedad, e.criterio, e.categoria, e.estado,
           e.cantidadDestinatarios, e.cantidadEnviados, e.cantidadFallidos, e.cantidadOmitidos,
           e.fechaInicio, e.fechaUltimaActividad, e.fechaFin,
           LTRIM(RTRIM(ISNULL(ui.nombre, N'') + N' ' + ISNULL(ui.apellido, N''))) AS responsable
    FROM dbo.EnvioNewsletter e
    JOIN dbo.Novedad n ON n.idNovedad = e.idNovedad
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = e.idUsuarioInterno
    WHERE @idNovedad IS NULL OR e.idNovedad = @idNovedad
    ORDER BY e.fechaInicio DESC, e.idEnvioNewsletter DESC;
END
GO

IF OBJECT_ID('dbo.sp_EnvioNewsletter_ListarDestinatarios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_ListarDestinatarios;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_ListarDestinatarios
    @idEnvioNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idSuscriptorNewsletter, correoElectronico, nombre, estado, intentos, fechaUltimoIntento, fechaEnvio
    FROM dbo.EnvioNewsletterDestinatario
    WHERE idEnvioNewsletter = @idEnvioNewsletter
    ORDER BY CASE estado WHEN N'Fallido' THEN 0 WHEN N'Pendiente' THEN 1 WHEN N'Omitido' THEN 2 ELSE 3 END, correoElectronico;
END
GO
