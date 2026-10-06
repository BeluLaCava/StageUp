IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.SolicitudHabilitacionGestor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SolicitudHabilitacionGestor
    (
        idSolicitud                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SolicitudHabilitacionGestor PRIMARY KEY,
        idUsuarioExterno            INT               NOT NULL,
        estado                      NVARCHAR(20)      NOT NULL CONSTRAINT DF_SolicitudHabilitacion_estado DEFAULT (N'PendienteRevision'),
        -- Datos identificatorios del responsable
        nombreResponsable           NVARCHAR(150)     NOT NULL,
        documentoResponsable        NVARCHAR(20)      NOT NULL,
        -- Datos de contacto
        telefonoContacto            NVARCHAR(30)      NOT NULL,
        correoContacto              NVARCHAR(300)     NOT NULL,
        -- Datos administrativos
        condicionFiscal             NVARCHAR(40)      NOT NULL,
        razonSocial                 NVARCHAR(150)     NULL,
        cuit                        NVARCHAR(13)      NULL,
        -- Datos iniciales del espacio o entidad
        nombreEspacio               NVARCHAR(150)     NOT NULL,
        tipoEspacio                 NVARCHAR(100)     NOT NULL,
        provincia                   NVARCHAR(100)     NOT NULL,
        ciudad                      NVARCHAR(150)     NOT NULL,
        descripcionPropuesta        NVARCHAR(1000)    NOT NULL,
        -- Revisión
        fechaSolicitud              DATETIME          NOT NULL CONSTRAINT DF_SolicitudHabilitacion_fecha DEFAULT (GETDATE()),
        fechaRevision               DATETIME          NULL,
        idUsuarioInternoRevisor     INT               NULL,
        motivoRechazo               NVARCHAR(500)     NULL,
        CONSTRAINT FK_SolicitudHabilitacion_UsuarioExterno FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_SolicitudHabilitacion_UsuarioInterno FOREIGN KEY (idUsuarioInternoRevisor) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_SolicitudHabilitacion_estado CHECK (estado IN (N'PendienteRevision', N'Aprobada', N'Rechazada')),
        CONSTRAINT CK_SolicitudHabilitacion_condicionFiscal CHECK (condicionFiscal IN
            (N'ConsumidorFinal', N'Monotributista', N'ResponsableInscripto', N'Exento', N'NoInformado'))
    );

    CREATE INDEX IX_SolicitudHabilitacion_Usuario ON dbo.SolicitudHabilitacionGestor (idUsuarioExterno, fechaSolicitud DESC);
    CREATE INDEX IX_SolicitudHabilitacion_Estado ON dbo.SolicitudHabilitacionGestor (estado, fechaSolicitud);
END
GO

-- Cuentas pendientes de antes de este script: se les crea la solicitud con
-- los datos que ya tiene la cuenta (el resto queda "No informado").
INSERT INTO dbo.SolicitudHabilitacionGestor
    (idUsuarioExterno, estado, nombreResponsable, documentoResponsable, telefonoContacto, correoContacto,
     condicionFiscal, nombreEspacio, tipoEspacio, provincia, ciudad, descripcionPropuesta, fechaSolicitud)
SELECT u.idUsuarioExterno, N'PendienteRevision', LEFT(u.nombre + N' ' + u.apellido, 150), N'No informado', N'No informado',
       u.correoElectronico, N'NoInformado', N'No informado', N'No informado', N'No informado', N'No informado',
       N'Solicitud registrada antes del formulario de habilitación.', ISNULL(u.fechaUltimaModificacion, u.fechaAlta)
FROM dbo.UsuarioExterno u
WHERE u.perfilUsuario = N'PendienteHabilitacionGestor'
  AND NOT EXISTS (SELECT 1 FROM dbo.SolicitudHabilitacionGestor s
                  WHERE s.idUsuarioExterno = u.idUsuarioExterno AND s.estado = N'PendienteRevision');
GO

-- ---------------------------------------------------------------------------
-- Registrar (A1 pasos 7 a 9, A3)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_SolicitudHabilitacion_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SolicitudHabilitacion_Registrar;
GO
CREATE PROCEDURE dbo.sp_SolicitudHabilitacion_Registrar
    @idUsuarioExterno       INT,
    @nombreResponsable      NVARCHAR(150),
    @documentoResponsable   NVARCHAR(20),
    @telefonoContacto       NVARCHAR(30),
    @correoContacto         NVARCHAR(300),
    @condicionFiscal        NVARCHAR(40),
    @razonSocial            NVARCHAR(150) = NULL,
    @cuit                   NVARCHAR(13) = NULL,
    @nombreEspacio          NVARCHAR(150),
    @tipoEspacio            NVARCHAR(100),
    @provincia              NVARCHAR(100),
    @ciudad                 NVARCHAR(150),
    @descripcionPropuesta   NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @perfil NVARCHAR(100), @estadoCuenta NVARCHAR(100);
        SELECT @perfil = perfilUsuario, @estadoCuenta = estadoCuenta
        FROM dbo.UsuarioExterno WITH (UPDLOCK, HOLDLOCK)
        WHERE idUsuarioExterno = @idUsuarioExterno;

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @perfil IS NULL
            SET @resultado = N'NO_EXISTE';
        ELSE IF @estadoCuenta <> N'Activa'
            SET @resultado = N'CUENTA_NO_ACTIVA';
        ELSE IF @perfil = N'GestorEspacios'
            SET @resultado = N'YA_ES_GESTOR';
        ELSE IF EXISTS (SELECT 1 FROM dbo.SolicitudHabilitacionGestor WITH (UPDLOCK, HOLDLOCK)
                        WHERE idUsuarioExterno = @idUsuarioExterno AND estado = N'PendienteRevision')
            SET @resultado = N'YA_PENDIENTE';

        DECLARE @idSolicitud INT = NULL;

        IF @resultado = N'OK'
        BEGIN
            INSERT INTO dbo.SolicitudHabilitacionGestor
                (idUsuarioExterno, estado, nombreResponsable, documentoResponsable, telefonoContacto, correoContacto,
                 condicionFiscal, razonSocial, cuit, nombreEspacio, tipoEspacio, provincia, ciudad, descripcionPropuesta)
            VALUES
                (@idUsuarioExterno, N'PendienteRevision', @nombreResponsable, @documentoResponsable, @telefonoContacto, @correoContacto,
                 @condicionFiscal, @razonSocial, @cuit, @nombreEspacio, @tipoEspacio, @provincia, @ciudad, @descripcionPropuesta);

            SET @idSolicitud = CAST(SCOPE_IDENTITY() AS INT);

            UPDATE dbo.UsuarioExterno
            SET perfilUsuario = N'PendienteHabilitacionGestor',
                fechaUltimaModificacion = GETDATE()
            WHERE idUsuarioExterno = @idUsuarioExterno;
        END

        COMMIT TRANSACTION;

        SELECT @resultado AS resultado, @idSolicitud AS idSolicitud;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ---------------------------------------------------------------------------
-- Consultas (A3, A4, A6 y panel interno)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_SolicitudHabilitacion_ObtenerUltimaPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SolicitudHabilitacion_ObtenerUltimaPorUsuario;
GO
CREATE PROCEDURE dbo.sp_SolicitudHabilitacion_ObtenerUltimaPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        s.*, u.nombre + N' ' + u.apellido AS nombreUsuario, u.correoElectronico AS correoUsuario,
        ui.nombre + N' ' + ui.apellido AS nombreRevisor
    FROM dbo.SolicitudHabilitacionGestor s
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = s.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = s.idUsuarioInternoRevisor
    WHERE s.idUsuarioExterno = @idUsuarioExterno
    ORDER BY s.fechaSolicitud DESC, s.idSolicitud DESC;
END
GO

IF OBJECT_ID('dbo.sp_SolicitudHabilitacion_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SolicitudHabilitacion_Listar;
GO
CREATE PROCEDURE dbo.sp_SolicitudHabilitacion_Listar
    @estado  NVARCHAR(20) = NULL,
    @maximo  INT = 200
AS
BEGIN
    SET NOCOUNT ON;

    IF @maximo IS NULL OR @maximo < 1 OR @maximo > 500 SET @maximo = 200;

    SELECT TOP (@maximo)
        s.*, u.nombre + N' ' + u.apellido AS nombreUsuario, u.correoElectronico AS correoUsuario,
        ui.nombre + N' ' + ui.apellido AS nombreRevisor
    FROM dbo.SolicitudHabilitacionGestor s
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = s.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = s.idUsuarioInternoRevisor
    WHERE @estado IS NULL OR s.estado = @estado
    -- Las pendientes, de la más vieja a la más nueva; las resueltas, al revés.
    ORDER BY CASE WHEN s.estado = N'PendienteRevision' THEN 0 ELSE 1 END,
             CASE WHEN s.estado = N'PendienteRevision' THEN s.fechaSolicitud END ASC,
             ISNULL(s.fechaRevision, s.fechaSolicitud) DESC;
END
GO

-- ---------------------------------------------------------------------------
-- Aprobar / rechazar (panel interno)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_SolicitudHabilitacion_Resolver', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SolicitudHabilitacion_Resolver;
GO
CREATE PROCEDURE dbo.sp_SolicitudHabilitacion_Resolver
    @idSolicitud                INT,
    @aprobar                    BIT,
    @motivoRechazo              NVARCHAR(500) = NULL,
    @idUsuarioInternoRevisor    INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @idUsuarioExterno INT, @estado NVARCHAR(20);
        SELECT @idUsuarioExterno = idUsuarioExterno, @estado = estado
        FROM dbo.SolicitudHabilitacionGestor WITH (UPDLOCK, HOLDLOCK)
        WHERE idSolicitud = @idSolicitud;

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @idUsuarioExterno IS NULL
            SET @resultado = N'NO_EXISTE';
        ELSE IF @estado <> N'PendienteRevision'
            SET @resultado = N'YA_RESUELTA';

        IF @resultado = N'OK'
        BEGIN
            UPDATE dbo.SolicitudHabilitacionGestor
            SET estado = CASE WHEN @aprobar = 1 THEN N'Aprobada' ELSE N'Rechazada' END,
                fechaRevision = GETDATE(),
                idUsuarioInternoRevisor = @idUsuarioInternoRevisor,
                motivoRechazo = CASE WHEN @aprobar = 1 THEN NULL ELSE @motivoRechazo END
            WHERE idSolicitud = @idSolicitud;

            UPDATE dbo.UsuarioExterno
            SET perfilUsuario = CASE WHEN @aprobar = 1 THEN N'GestorEspacios' ELSE N'ExternoSolicitante' END,
                fechaUltimaModificacion = GETDATE()
            WHERE idUsuarioExterno = @idUsuarioExterno
              AND perfilUsuario <> N'GestorEspacios';
        END

        COMMIT TRANSACTION;

        SELECT @resultado AS resultado, @idUsuarioExterno AS idUsuarioExterno, @estado AS estadoAnterior;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
