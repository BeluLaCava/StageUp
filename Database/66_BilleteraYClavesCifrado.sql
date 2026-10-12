IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ---------------------------------------------------------------------------
-- Ítems 9 y 10: pares de claves RSA de StageUp, versionados. La clave
-- privada se guarda cifrada con la clave simétrica de la configuración
-- privada (nunca en claro y nunca se muestra). Siempre hay como mucho una
-- versión Activa (la que cifra lo nuevo); las Retiradas se conservan para
-- poder leer lo que se cifró con ellas (por ejemplo, exportaciones viejas).
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ClaveCifrado', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClaveCifrado
    (
        idClaveCifrado          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClaveCifrado PRIMARY KEY,
        version                 INT            NOT NULL,
        clavePublica            NVARCHAR(MAX)  NOT NULL,
        clavePrivadaProtegida   NVARCHAR(MAX)  NOT NULL,
        huella                  NVARCHAR(60)   NOT NULL,
        estado                  NVARCHAR(10)   NOT NULL,
        motivo                  NVARCHAR(300)  NULL,
        idUsuarioInternoAlta    INT            NULL,
        fechaAlta               DATETIME       NOT NULL CONSTRAINT DF_ClaveCifrado_fechaAlta DEFAULT (GETDATE()),
        idUsuarioInternoRetiro  INT            NULL,
        fechaRetiro             DATETIME       NULL,
        CONSTRAINT UX_ClaveCifrado_version UNIQUE (version),
        CONSTRAINT FK_ClaveCifrado_UsuarioAlta FOREIGN KEY (idUsuarioInternoAlta) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT FK_ClaveCifrado_UsuarioRetiro FOREIGN KEY (idUsuarioInternoRetiro) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_ClaveCifrado_estado CHECK (estado IN (N'Activa', N'Retirada'))
    );

    CREATE UNIQUE INDEX UX_ClaveCifrado_Activa ON dbo.ClaveCifrado (estado) WHERE estado = N'Activa';
END
GO

-- ---------------------------------------------------------------------------
-- Ítem 8: billetera de tarjetas. En claro solo lo que se muestra (alias,
-- marca, últimos 4 y vencimiento). Número y titular van cifrados con el
-- esquema híbrido (ítem 9): AES-256 con una clave nueva por tarjeta (iv +
-- datosCifrados) y esa clave AES envuelta con la clave pública RSA de la
-- versión idClaveCifrado (claveCifrada). El código de seguridad nunca se
-- guarda. huella = HMAC del número, para no guardar dos veces la misma
-- tarjeta sin poder recuperar el número a partir de ella.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.TarjetaGuardada', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TarjetaGuardada
    (
        idTarjetaGuardada   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TarjetaGuardada PRIMARY KEY,
        idUsuarioExterno    INT            NOT NULL,
        alias               NVARCHAR(40)   NOT NULL,
        marca               NVARCHAR(30)   NOT NULL,
        ultimosDigitos      NVARCHAR(4)    NOT NULL,
        mesVencimiento      TINYINT        NOT NULL,
        anioVencimiento     SMALLINT       NOT NULL,
        preferida           BIT            NOT NULL CONSTRAINT DF_TarjetaGuardada_preferida DEFAULT (0),
        huella              NVARCHAR(64)   NOT NULL,
        iv                  NVARCHAR(50)   NULL,
        claveCifrada        NVARCHAR(1000) NULL,
        datosCifrados       NVARCHAR(500)  NULL,
        idClaveCifrado      INT            NULL,
        activa              BIT            NOT NULL CONSTRAINT DF_TarjetaGuardada_activa DEFAULT (1),
        fechaAlta           DATETIME       NOT NULL CONSTRAINT DF_TarjetaGuardada_fechaAlta DEFAULT (GETDATE()),
        fechaUltimoUso      DATETIME       NULL,
        fechaBaja           DATETIME       NULL,
        CONSTRAINT FK_TarjetaGuardada_Usuario FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_TarjetaGuardada_Clave FOREIGN KEY (idClaveCifrado) REFERENCES dbo.ClaveCifrado (idClaveCifrado),
        CONSTRAINT CK_TarjetaGuardada_mes CHECK (mesVencimiento BETWEEN 1 AND 12),
        CONSTRAINT CK_TarjetaGuardada_cifrado CHECK (activa = 0 OR (iv IS NOT NULL AND claveCifrada IS NOT NULL AND datosCifrados IS NOT NULL AND idClaveCifrado IS NOT NULL))
    );

    CREATE INDEX IX_TarjetaGuardada_Usuario ON dbo.TarjetaGuardada (idUsuarioExterno) WHERE activa = 1;
    CREATE INDEX IX_TarjetaGuardada_Clave ON dbo.TarjetaGuardada (idClaveCifrado) WHERE activa = 1;
    CREATE UNIQUE INDEX UX_TarjetaGuardada_Huella ON dbo.TarjetaGuardada (idUsuarioExterno, huella) WHERE activa = 1;
    CREATE UNIQUE INDEX UX_TarjetaGuardada_Preferida ON dbo.TarjetaGuardada (idUsuarioExterno) WHERE activa = 1 AND preferida = 1;
END
GO

-- ===========================================================================
-- Claves de cifrado
-- ===========================================================================
IF OBJECT_ID('dbo.sp_ClaveCifrado_ObtenerActiva', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ClaveCifrado_ObtenerActiva;
GO
CREATE PROCEDURE dbo.sp_ClaveCifrado_ObtenerActiva
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idClaveCifrado, version, clavePublica, clavePrivadaProtegida, huella, estado, motivo,
           idUsuarioInternoAlta, fechaAlta, idUsuarioInternoRetiro, fechaRetiro
    FROM dbo.ClaveCifrado
    WHERE estado = N'Activa';
END
GO

IF OBJECT_ID('dbo.sp_ClaveCifrado_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ClaveCifrado_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_ClaveCifrado_ObtenerPorId
    @idClaveCifrado INT = NULL,
    @version        INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idClaveCifrado, version, clavePublica, clavePrivadaProtegida, huella, estado, motivo,
           idUsuarioInternoAlta, fechaAlta, idUsuarioInternoRetiro, fechaRetiro
    FROM dbo.ClaveCifrado
    WHERE (@idClaveCifrado IS NOT NULL AND idClaveCifrado = @idClaveCifrado)
       OR (@idClaveCifrado IS NULL AND version = @version);
END
GO

-- Listado para Herramientas de seguridad: sin las claves, con la huella y
-- cuántas tarjetas protege cada versión.
IF OBJECT_ID('dbo.sp_ClaveCifrado_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ClaveCifrado_Listar;
GO
CREATE PROCEDURE dbo.sp_ClaveCifrado_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idClaveCifrado, c.version, c.huella, c.estado, c.motivo, c.idUsuarioInternoAlta, c.fechaAlta,
           c.idUsuarioInternoRetiro, c.fechaRetiro,
           LTRIM(RTRIM(ISNULL(ua.nombre, N'') + N' ' + ISNULL(ua.apellido, N''))) AS nombreUsuarioAlta,
           LTRIM(RTRIM(ISNULL(ur.nombre, N'') + N' ' + ISNULL(ur.apellido, N''))) AS nombreUsuarioRetiro,
           (SELECT COUNT(*) FROM dbo.TarjetaGuardada t WHERE t.idClaveCifrado = c.idClaveCifrado AND t.activa = 1) AS tarjetasProtegidas
    FROM dbo.ClaveCifrado c
    LEFT JOIN dbo.UsuarioInterno ua ON ua.idUsuarioInterno = c.idUsuarioInternoAlta
    LEFT JOIN dbo.UsuarioInterno ur ON ur.idUsuarioInterno = c.idUsuarioInternoRetiro
    ORDER BY c.version DESC;
END
GO

-- Alta de una versión nueva: queda Activa y la anterior pasa a Retirada
-- (en la misma transacción). resultado: OK | YA_EXISTE_ACTIVA (cuando se
-- pidió crear la primera y otro proceso ya la creó).
IF OBJECT_ID('dbo.sp_ClaveCifrado_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ClaveCifrado_Registrar;
GO
CREATE PROCEDURE dbo.sp_ClaveCifrado_Registrar
    @clavePublica           NVARCHAR(MAX),
    @clavePrivadaProtegida  NVARCHAR(MAX),
    @huella                 NVARCHAR(60),
    @motivo                 NVARCHAR(300) = NULL,
    @idUsuarioInterno       INT = NULL,
    @soloSiNoHayActiva      BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @idAnterior INT = (SELECT idClaveCifrado FROM dbo.ClaveCifrado WITH (UPDLOCK, HOLDLOCK) WHERE estado = N'Activa');
    IF @soloSiNoHayActiva = 1 AND @idAnterior IS NOT NULL
    BEGIN
        COMMIT TRANSACTION;
        SELECT N'YA_EXISTE_ACTIVA' AS resultado, @idAnterior AS idClaveCifrado, CAST(NULL AS INT) AS idClaveAnterior,
               (SELECT version FROM dbo.ClaveCifrado WHERE idClaveCifrado = @idAnterior) AS version;
        RETURN;
    END

    UPDATE dbo.ClaveCifrado
    SET estado = N'Retirada', fechaRetiro = GETDATE(), idUsuarioInternoRetiro = @idUsuarioInterno
    WHERE idClaveCifrado = @idAnterior;

    DECLARE @version INT = ISNULL((SELECT MAX(version) FROM dbo.ClaveCifrado), 0) + 1;
    INSERT INTO dbo.ClaveCifrado (version, clavePublica, clavePrivadaProtegida, huella, estado, motivo, idUsuarioInternoAlta)
    VALUES (@version, @clavePublica, @clavePrivadaProtegida, @huella, N'Activa', @motivo, @idUsuarioInterno);

    DECLARE @idNueva INT = CAST(SCOPE_IDENTITY() AS INT);
    COMMIT TRANSACTION;

    SELECT N'OK' AS resultado, @idNueva AS idClaveCifrado, @idAnterior AS idClaveAnterior, @version AS version;
END
GO

-- ===========================================================================
-- Billetera
-- ===========================================================================
IF OBJECT_ID('dbo.sp_TarjetaGuardada_ListarPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_ListarPorUsuario;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_ListarPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idTarjetaGuardada, idUsuarioExterno, alias, marca, ultimosDigitos, mesVencimiento, anioVencimiento,
           preferida, fechaAlta, fechaUltimoUso
    FROM dbo.TarjetaGuardada
    WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1
    ORDER BY preferida DESC, fechaAlta DESC;
END
GO

-- Con los datos cifrados: solo para pagar (o rotar claves). Siempre filtra
-- por el dueño.
IF OBJECT_ID('dbo.sp_TarjetaGuardada_ObtenerParaUsar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_ObtenerParaUsar;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_ObtenerParaUsar
    @idTarjetaGuardada INT,
    @idUsuarioExterno  INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idTarjetaGuardada, idUsuarioExterno, alias, marca, ultimosDigitos, mesVencimiento, anioVencimiento,
           preferida, fechaAlta, fechaUltimoUso, iv, claveCifrada, datosCifrados, idClaveCifrado
    FROM dbo.TarjetaGuardada
    WHERE idTarjetaGuardada = @idTarjetaGuardada AND idUsuarioExterno = @idUsuarioExterno AND activa = 1;
END
GO

-- resultado: OK | REPETIDA | ALIAS_REPETIDO | LIMITE
IF OBJECT_ID('dbo.sp_TarjetaGuardada_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_Insertar;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_Insertar
    @idUsuarioExterno  INT,
    @alias             NVARCHAR(40),
    @marca             NVARCHAR(30),
    @ultimosDigitos    NVARCHAR(4),
    @mesVencimiento    TINYINT,
    @anioVencimiento   SMALLINT,
    @preferida         BIT,
    @huella            NVARCHAR(64),
    @iv                NVARCHAR(50),
    @claveCifrada      NVARCHAR(1000),
    @datosCifrados     NVARCHAR(500),
    @idClaveCifrado    INT,
    @maximo            INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @recurso NVARCHAR(100) = N'StageUp_Billetera_' + CAST(@idUsuarioExterno AS NVARCHAR(10));
    EXEC sp_getapplock @Resource = @recurso, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;

    DECLARE @resultado NVARCHAR(20) = N'OK';
    IF EXISTS (SELECT 1 FROM dbo.TarjetaGuardada WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1 AND huella = @huella)
        SET @resultado = N'REPETIDA';
    ELSE IF EXISTS (SELECT 1 FROM dbo.TarjetaGuardada WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1 AND alias = @alias)
        SET @resultado = N'ALIAS_REPETIDO';
    ELSE IF (SELECT COUNT(*) FROM dbo.TarjetaGuardada WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1) >= @maximo
        SET @resultado = N'LIMITE';

    IF @resultado <> N'OK'
    BEGIN
        COMMIT TRANSACTION;
        SELECT @resultado AS resultado, CAST(NULL AS INT) AS idTarjetaGuardada, CAST(0 AS BIT) AS preferida;
        RETURN;
    END

    -- La primera tarjeta queda como preferida.
    IF NOT EXISTS (SELECT 1 FROM dbo.TarjetaGuardada WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1)
        SET @preferida = 1;

    IF @preferida = 1
        UPDATE dbo.TarjetaGuardada SET preferida = 0 WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1 AND preferida = 1;

    INSERT INTO dbo.TarjetaGuardada
        (idUsuarioExterno, alias, marca, ultimosDigitos, mesVencimiento, anioVencimiento, preferida, huella,
         iv, claveCifrada, datosCifrados, idClaveCifrado)
    VALUES
        (@idUsuarioExterno, @alias, @marca, @ultimosDigitos, @mesVencimiento, @anioVencimiento, @preferida, @huella,
         @iv, @claveCifrada, @datosCifrados, @idClaveCifrado);

    DECLARE @id INT = CAST(SCOPE_IDENTITY() AS INT);
    COMMIT TRANSACTION;

    SELECT N'OK' AS resultado, @id AS idTarjetaGuardada, @preferida AS preferida;
END
GO

-- resultado: OK | NO_ENCONTRADA | ALIAS_REPETIDO
IF OBJECT_ID('dbo.sp_TarjetaGuardada_CambiarAlias', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_CambiarAlias;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_CambiarAlias
    @idTarjetaGuardada INT,
    @idUsuarioExterno  INT,
    @alias             NVARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.TarjetaGuardada WHERE idTarjetaGuardada = @idTarjetaGuardada AND idUsuarioExterno = @idUsuarioExterno AND activa = 1)
    BEGIN
        SELECT N'NO_ENCONTRADA' AS resultado;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.TarjetaGuardada
               WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1 AND alias = @alias AND idTarjetaGuardada <> @idTarjetaGuardada)
    BEGIN
        SELECT N'ALIAS_REPETIDO' AS resultado;
        RETURN;
    END

    UPDATE dbo.TarjetaGuardada SET alias = @alias WHERE idTarjetaGuardada = @idTarjetaGuardada;
    SELECT N'OK' AS resultado;
END
GO

-- resultado: OK | NO_ENCONTRADA
IF OBJECT_ID('dbo.sp_TarjetaGuardada_MarcarPreferida', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_MarcarPreferida;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_MarcarPreferida
    @idTarjetaGuardada INT,
    @idUsuarioExterno  INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.TarjetaGuardada WHERE idTarjetaGuardada = @idTarjetaGuardada AND idUsuarioExterno = @idUsuarioExterno AND activa = 1)
    BEGIN
        SELECT N'NO_ENCONTRADA' AS resultado;
        RETURN;
    END

    BEGIN TRANSACTION;
    UPDATE dbo.TarjetaGuardada SET preferida = 0
    WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1 AND preferida = 1 AND idTarjetaGuardada <> @idTarjetaGuardada;
    UPDATE dbo.TarjetaGuardada SET preferida = 1 WHERE idTarjetaGuardada = @idTarjetaGuardada;
    COMMIT TRANSACTION;

    SELECT N'OK' AS resultado;
END
GO

-- Baja: se borran los datos cifrados (no quedan en la base) y, si era la
-- preferida, pasa a serlo la más reciente que quede. resultado: OK | NO_ENCONTRADA
IF OBJECT_ID('dbo.sp_TarjetaGuardada_Eliminar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_Eliminar;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_Eliminar
    @idTarjetaGuardada INT,
    @idUsuarioExterno  INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @eraPreferida BIT =
        (SELECT preferida FROM dbo.TarjetaGuardada
         WHERE idTarjetaGuardada = @idTarjetaGuardada AND idUsuarioExterno = @idUsuarioExterno AND activa = 1);

    IF @eraPreferida IS NULL
    BEGIN
        SELECT N'NO_ENCONTRADA' AS resultado;
        RETURN;
    END

    BEGIN TRANSACTION;
    UPDATE dbo.TarjetaGuardada
    SET activa = 0, preferida = 0, fechaBaja = GETDATE(),
        iv = NULL, claveCifrada = NULL, datosCifrados = NULL, idClaveCifrado = NULL,
        huella = N'BAJA-' + CAST(idTarjetaGuardada AS NVARCHAR(10))
    WHERE idTarjetaGuardada = @idTarjetaGuardada;

    IF @eraPreferida = 1
        UPDATE dbo.TarjetaGuardada SET preferida = 1
        WHERE idTarjetaGuardada = (SELECT TOP 1 idTarjetaGuardada FROM dbo.TarjetaGuardada
                                   WHERE idUsuarioExterno = @idUsuarioExterno AND activa = 1
                                   ORDER BY fechaAlta DESC, idTarjetaGuardada DESC);
    COMMIT TRANSACTION;

    SELECT N'OK' AS resultado;
END
GO

IF OBJECT_ID('dbo.sp_TarjetaGuardada_RegistrarUso', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_RegistrarUso;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_RegistrarUso
    @idTarjetaGuardada INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TarjetaGuardada SET fechaUltimoUso = GETDATE() WHERE idTarjetaGuardada = @idTarjetaGuardada AND activa = 1;
    SELECT @@ROWCOUNT AS filas;
END
GO

-- Ítem 10 (rotación): tarjetas protegidas con versiones que no son la activa.
IF OBJECT_ID('dbo.sp_TarjetaGuardada_ListarParaReenvolver', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_ListarParaReenvolver;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_ListarParaReenvolver
    @idClaveActiva INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idTarjetaGuardada, claveCifrada, idClaveCifrado
    FROM dbo.TarjetaGuardada
    WHERE activa = 1 AND idClaveCifrado <> @idClaveActiva;
END
GO

-- Cambia la clave AES envuelta de una tarjeta, solo si sigue con la versión
-- anterior (si otro proceso ya la cambió, no hace nada).
IF OBJECT_ID('dbo.sp_TarjetaGuardada_Reenvolver', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TarjetaGuardada_Reenvolver;
GO
CREATE PROCEDURE dbo.sp_TarjetaGuardada_Reenvolver
    @idTarjetaGuardada  INT,
    @idClaveAnterior    INT,
    @idClaveNueva       INT,
    @claveCifrada       NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TarjetaGuardada
    SET claveCifrada = @claveCifrada, idClaveCifrado = @idClaveNueva
    WHERE idTarjetaGuardada = @idTarjetaGuardada AND idClaveCifrado = @idClaveAnterior AND activa = 1;
    SELECT @@ROWCOUNT AS filas;
END
GO

-- ---------------------------------------------------------------------------
-- Herramientas de seguridad pasa a decir que gestiona las claves de cifrado.
-- ---------------------------------------------------------------------------
UPDATE dbo.ComponentePermiso
SET descripcion = N'Rotar las claves de cifrado (RSA + AES) que protegen la billetera de tarjetas y las exportaciones, y descifrar exportaciones de la bitácora.',
    fechaUltimaModificacion = GETDATE()
WHERE codigoPermiso = N'GESTIONAR_ENCRIPTACION';
GO
