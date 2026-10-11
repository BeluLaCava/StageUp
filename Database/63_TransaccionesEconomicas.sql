IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Pago', N'U') IS NULL OR COL_LENGTH(N'dbo.Pago', N'identificadorExterno') IS NULL
BEGIN
    THROW 51000, 'Primero deben ejecutarse Database/49_PagosNotasYCuentaCorriente.sql y Database/62_PagosReservasControl.sql.', 1;
END
GO

-- ---------------------------------------------------------------------------
-- CU-002-002: transacción económica generada por un hecho de negocio real:
-- una reserva aceptada y abonada (ReservaAbonada) o una cancelación de una
-- reserva aceptada con cargo (CargoCancelacion). Guarda el monto, la
-- comisión de StageUp, el importe del gestor y, para los cargos, el
-- porcentaje aplicado (50 % o 100 %). Si falta información queda
-- "PendienteControl" (A3) y si no hay referencia al medio de pago queda sin
-- informar (A4).
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.TransaccionEconomica', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransaccionEconomica
    (
        idTransaccion           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TransaccionEconomica PRIMARY KEY,
        codigo                  AS (N'TX-' + RIGHT(N'00000000' + CAST(idTransaccion AS NVARCHAR(10)), 8)),
        tipoOperacion           NVARCHAR(30)   NOT NULL,
        idReserva               INT            NULL,
        idUsuarioSolicitante    INT            NULL,
        idUsuarioGestor         INT            NULL,
        idEspacioArtistico      INT            NULL,
        idPago                  INT            NULL,
        idComprobante           INT            NULL,
        moneda                  NVARCHAR(3)    NOT NULL CONSTRAINT DF_TransaccionEconomica_moneda DEFAULT (N'ARS'),
        montoTotal              DECIMAL(18,2)  NOT NULL,
        porcentajeComision      DECIMAL(5,2)   NULL,
        importeComision         DECIMAL(18,2)  NULL,
        importeGestor           DECIMAL(18,2)  NULL,
        porcentajeCargo         DECIMAL(5,2)   NULL,
        estadoPago              NVARCHAR(20)   NOT NULL,
        referenciaPago          NVARCHAR(60)   NULL,
        estadoRegistro          NVARCHAR(20)   NOT NULL CONSTRAINT DF_TransaccionEconomica_estadoRegistro DEFAULT (N'Registrada'),
        observacion             NVARCHAR(500)  NULL,
        fechaGeneracion         DATETIME       NOT NULL CONSTRAINT DF_TransaccionEconomica_fecha DEFAULT (GETDATE()),
        fechaUltimaModificacion DATETIME       NULL,
        CONSTRAINT FK_TransaccionEconomica_Reserva FOREIGN KEY (idReserva) REFERENCES dbo.Reserva (idReserva),
        CONSTRAINT FK_TransaccionEconomica_Solicitante FOREIGN KEY (idUsuarioSolicitante) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_TransaccionEconomica_Gestor FOREIGN KEY (idUsuarioGestor) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_TransaccionEconomica_Espacio FOREIGN KEY (idEspacioArtistico) REFERENCES dbo.EspacioArtistico (idEspacioArtistico),
        CONSTRAINT FK_TransaccionEconomica_Pago FOREIGN KEY (idPago) REFERENCES dbo.Pago (idPago),
        CONSTRAINT FK_TransaccionEconomica_Comprobante FOREIGN KEY (idComprobante) REFERENCES dbo.Comprobante (idComprobante),
        CONSTRAINT CK_TransaccionEconomica_tipo CHECK (tipoOperacion IN (N'ReservaAbonada', N'CargoCancelacion')),
        CONSTRAINT CK_TransaccionEconomica_estadoPago CHECK (estadoPago IN (N'Pagado', N'Cobrado', N'Devuelto', N'Pendiente')),
        CONSTRAINT CK_TransaccionEconomica_estadoRegistro CHECK (estadoRegistro IN (N'Registrada', N'PendienteControl')),
        CONSTRAINT CK_TransaccionEconomica_moneda CHECK (moneda IN (N'ARS', N'USD')),
        CONSTRAINT CK_TransaccionEconomica_monto CHECK (montoTotal >= 0)
    );

    CREATE INDEX IX_TransaccionEconomica_Fecha ON dbo.TransaccionEconomica (fechaGeneracion DESC);
    CREATE INDEX IX_TransaccionEconomica_Reserva ON dbo.TransaccionEconomica (idReserva);
    CREATE UNIQUE INDEX UX_TransaccionEconomica_Pago ON dbo.TransaccionEconomica (idPago)
        WHERE tipoOperacion = N'ReservaAbonada' AND idPago IS NOT NULL;
    CREATE UNIQUE INDEX UX_TransaccionEconomica_Cargo ON dbo.TransaccionEconomica (idReserva)
        WHERE tipoOperacion = N'CargoCancelacion' AND idReserva IS NOT NULL;
END
GO

-- Transacciones de los pagos y cargos registrados antes de este script.
INSERT INTO dbo.TransaccionEconomica
    (tipoOperacion, idReserva, idUsuarioSolicitante, idUsuarioGestor, idEspacioArtistico, idPago, moneda, montoTotal,
     porcentajeComision, importeComision, importeGestor, estadoPago, referenciaPago, fechaGeneracion, observacion)
SELECT N'ReservaAbonada', r.idReserva, r.idUsuarioExternoSolicitante, e.idUsuarioGestor, e.idEspacioArtistico, p.idPago, p.moneda,
       p.importeTotal, ISNULL(p.porcentajeComisionPlataforma, 0), ISNULL(p.importeComisionPlataforma, 0),
       p.importeTotal - ISNULL(p.importeComisionPlataforma, 0),
       CASE WHEN r.estadoPago = N'Devuelto' THEN N'Devuelto' ELSE N'Pagado' END,
       p.identificadorExterno, p.fechaPago,
       CASE WHEN r.estadoPago = N'Devuelto' THEN N'La reserva se canceló: el importe volvió al solicitante como saldo a favor.' END
FROM dbo.Pago p
INNER JOIN dbo.Reserva r ON r.idReserva = p.idReserva
INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
WHERE p.estado = N'Aprobado' AND p.concepto = N'Reserva'
  AND NOT EXISTS (SELECT 1 FROM dbo.TransaccionEconomica t WHERE t.idPago = p.idPago AND t.tipoOperacion = N'ReservaAbonada');
GO

INSERT INTO dbo.TransaccionEconomica
    (tipoOperacion, idReserva, idUsuarioSolicitante, idUsuarioGestor, idEspacioArtistico, idPago, idComprobante, moneda, montoTotal,
     porcentajeComision, importeComision, importeGestor, porcentajeCargo, estadoPago, referenciaPago, fechaGeneracion)
SELECT N'CargoCancelacion', r.idReserva, r.idUsuarioExternoSolicitante, e.idUsuarioGestor, e.idEspacioArtistico, p.idPago, nd.idComprobante,
       ISNULL(r.moneda, N'ARS'), r.importeComision, ISNULL(p.porcentajeComisionPlataforma, 0),
       ROUND(r.importeComision * ISNULL(p.porcentajeComisionPlataforma, 0) / 100, 2),
       r.importeComision - ROUND(r.importeComision * ISNULL(p.porcentajeComisionPlataforma, 0) / 100, 2),
       CASE WHEN ISNULL(r.importeEstimado, 0) > 0 THEN ROUND(r.importeComision * 100 / r.importeEstimado, 0) END,
       N'Cobrado', p.identificadorExterno, ISNULL(r.fechaCancelacion, GETDATE())
FROM dbo.Reserva r
INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
OUTER APPLY (SELECT TOP 1 idPago, porcentajeComisionPlataforma, identificadorExterno FROM dbo.Pago
             WHERE idReserva = r.idReserva AND estado = N'Aprobado' ORDER BY idPago DESC) p
OUTER APPLY (SELECT TOP 1 idComprobante FROM dbo.Comprobante
             WHERE idReserva = r.idReserva AND tipo = N'ND' AND origen = N'PenalidadCancelacion' ORDER BY idComprobante DESC) nd
WHERE r.estadoReserva = N'Cancelada' AND r.comisionAplicada = 1 AND ISNULL(r.importeComision, 0) > 0
  AND r.estadoPago = N'Devuelto'
  AND NOT EXISTS (SELECT 1 FROM dbo.TransaccionEconomica t WHERE t.idReserva = r.idReserva AND t.tipoOperacion = N'CargoCancelacion');
GO

-- ---------------------------------------------------------------------------
-- Pasos 5 a 20 y A1 paso 13: registra la transacción. Solicitante, gestor,
-- espacio y moneda salen de la reserva. Si ya existe la misma transacción
-- (mismo pago o mismo cargo), devuelve la existente sin duplicarla.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_TransaccionEconomica_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TransaccionEconomica_Registrar;
GO
CREATE PROCEDURE dbo.sp_TransaccionEconomica_Registrar
    @tipoOperacion       NVARCHAR(30),
    @idReserva           INT            = NULL,
    @idPago              INT            = NULL,
    @idComprobante       INT            = NULL,
    @montoTotal          DECIMAL(18,2),
    @porcentajeComision  DECIMAL(5,2)   = NULL,
    @importeComision     DECIMAL(18,2)  = NULL,
    @importeGestor       DECIMAL(18,2)  = NULL,
    @porcentajeCargo     DECIMAL(5,2)   = NULL,
    @estadoPago          NVARCHAR(20),
    @referenciaPago      NVARCHAR(60)   = NULL,
    @estadoRegistro      NVARCHAR(20)   = N'Registrada',
    @observacion         NVARCHAR(500)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @existente INT =
    (
        SELECT TOP 1 idTransaccion FROM dbo.TransaccionEconomica
        WHERE tipoOperacion = @tipoOperacion
          AND ((@tipoOperacion = N'ReservaAbonada' AND @idPago IS NOT NULL AND idPago = @idPago)
            OR (@tipoOperacion = N'CargoCancelacion' AND @idReserva IS NOT NULL AND idReserva = @idReserva))
    );

    IF @existente IS NOT NULL
    BEGIN
        SELECT @existente AS idTransaccion, CAST(0 AS BIT) AS esNueva;
        RETURN;
    END

    INSERT INTO dbo.TransaccionEconomica
        (tipoOperacion, idReserva, idUsuarioSolicitante, idUsuarioGestor, idEspacioArtistico, idPago, idComprobante, moneda,
         montoTotal, porcentajeComision, importeComision, importeGestor, porcentajeCargo, estadoPago, referenciaPago,
         estadoRegistro, observacion)
    SELECT @tipoOperacion, @idReserva, r.idUsuarioExternoSolicitante, e.idUsuarioGestor, e.idEspacioArtistico, @idPago, @idComprobante,
           ISNULL(r.moneda, N'ARS'), @montoTotal, @porcentajeComision, @importeComision, @importeGestor, @porcentajeCargo,
           @estadoPago, @referenciaPago, @estadoRegistro, @observacion
    FROM (SELECT 1 AS uno) x
    LEFT JOIN dbo.Reserva r ON r.idReserva = @idReserva
    LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico;

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idTransaccion, CAST(1 AS BIT) AS esNueva;
END
GO

-- A2: la cancelación de una reserva abonada actualiza su transacción.
IF OBJECT_ID('dbo.sp_TransaccionEconomica_ActualizarEstadoPorReserva', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TransaccionEconomica_ActualizarEstadoPorReserva;
GO
CREATE PROCEDURE dbo.sp_TransaccionEconomica_ActualizarEstadoPorReserva
    @idReserva    INT,
    @estadoPago   NVARCHAR(20),
    @observacion  NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TransaccionEconomica
    SET estadoPago = @estadoPago,
        observacion = ISNULL(@observacion, observacion),
        fechaUltimaModificacion = GETDATE()
    WHERE idReserva = @idReserva AND tipoOperacion = N'ReservaAbonada';

    SELECT @@ROWCOUNT AS filas;
END
GO

-- ---------------------------------------------------------------------------
-- A5 pasos 3 a 6: listado de "Transacciones económicas" con filtros por tipo
-- de operación y estado de pago (y período y texto).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_TransaccionEconomica_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TransaccionEconomica_Listar;
GO
CREATE PROCEDURE dbo.sp_TransaccionEconomica_Listar
    @tipoOperacion  NVARCHAR(30)  = NULL,
    @estadoPago     NVARCHAR(20)  = NULL,
    @desde          DATE          = NULL,
    @hasta          DATE          = NULL,
    @texto          NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @like NVARCHAR(110) = CASE WHEN @texto IS NULL OR LTRIM(RTRIM(@texto)) = N'' THEN NULL
                                       ELSE N'%' + LTRIM(RTRIM(@texto)) + N'%' END;

    SELECT TOP 500
        t.idTransaccion, t.codigo, t.tipoOperacion, t.idReserva, t.idPago, t.idComprobante, t.moneda, t.montoTotal,
        t.porcentajeComision, t.importeComision, t.importeGestor, t.porcentajeCargo, t.estadoPago, t.referenciaPago,
        t.estadoRegistro, t.observacion, t.fechaGeneracion,
        LTRIM(RTRIM(ISNULL(s.nombre, N'') + N' ' + ISNULL(s.apellido, N''))) AS nombreSolicitante,
        LTRIM(RTRIM(ISNULL(g.nombre, N'') + N' ' + ISNULL(g.apellido, N''))) AS nombreGestor,
        e.nombreEspacio,
        fc.numero AS numeroFactura, c.numero AS numeroComprobante
    FROM dbo.TransaccionEconomica t
    LEFT JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = t.idUsuarioSolicitante
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = t.idUsuarioGestor
    LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = t.idEspacioArtistico
    LEFT JOIN dbo.Comprobante fc ON fc.idPago = t.idPago AND fc.tipo = N'FC'
    LEFT JOIN dbo.Comprobante c ON c.idComprobante = t.idComprobante
    WHERE (@tipoOperacion IS NULL OR t.tipoOperacion = @tipoOperacion)
      AND (@estadoPago IS NULL OR t.estadoPago = @estadoPago)
      AND (@desde IS NULL OR t.fechaGeneracion >= @desde)
      AND (@hasta IS NULL OR t.fechaGeneracion < DATEADD(DAY, 1, CAST(@hasta AS DATETIME)))
      AND (@like IS NULL
           OR s.nombre + N' ' + s.apellido LIKE @like OR s.correoElectronico LIKE @like
           OR g.nombre + N' ' + g.apellido LIKE @like OR g.correoElectronico LIKE @like
           OR e.nombreEspacio LIKE @like OR t.codigo LIKE @like
           OR CAST(t.idReserva AS NVARCHAR(10)) = LTRIM(RTRIM(@texto)))
    ORDER BY t.fechaGeneracion DESC, t.idTransaccion DESC;
END
GO

IF OBJECT_ID('dbo.sp_TransaccionEconomica_Contar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TransaccionEconomica_Contar;
GO
CREATE PROCEDURE dbo.sp_TransaccionEconomica_Contar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS total FROM dbo.TransaccionEconomica;
END
GO

-- A5 pasos 9 y 10: detalle de la transacción.
IF OBJECT_ID('dbo.sp_TransaccionEconomica_ObtenerDetalle', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TransaccionEconomica_ObtenerDetalle;
GO
CREATE PROCEDURE dbo.sp_TransaccionEconomica_ObtenerDetalle
    @idTransaccion INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTransaccion, t.codigo, t.tipoOperacion, t.idReserva, t.idPago, t.idComprobante, t.moneda, t.montoTotal,
        t.porcentajeComision, t.importeComision, t.importeGestor, t.porcentajeCargo, t.estadoPago, t.referenciaPago,
        t.estadoRegistro, t.observacion, t.fechaGeneracion,
        LTRIM(RTRIM(ISNULL(s.nombre, N'') + N' ' + ISNULL(s.apellido, N''))) AS nombreSolicitante, s.correoElectronico AS correoSolicitante,
        LTRIM(RTRIM(ISNULL(g.nombre, N'') + N' ' + ISNULL(g.apellido, N''))) AS nombreGestor, g.correoElectronico AS correoGestor,
        e.nombreEspacio,
        r.fechaSolicitada, r.minutoDesde, r.minutoHasta, r.importeEstimado, r.estadoReserva, r.fechaCancelacion,
        fc.numero AS numeroFactura, c.numero AS numeroComprobante,
        p.medioInformado, p.marcaTarjeta, p.ultimosDigitos, p.importeTarjeta, p.importeSaldo
    FROM dbo.TransaccionEconomica t
    LEFT JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = t.idUsuarioSolicitante
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = t.idUsuarioGestor
    LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = t.idEspacioArtistico
    LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReserva
    LEFT JOIN dbo.Pago p ON p.idPago = t.idPago
    LEFT JOIN dbo.Comprobante fc ON fc.idPago = t.idPago AND fc.tipo = N'FC'
    LEFT JOIN dbo.Comprobante c ON c.idComprobante = t.idComprobante
    WHERE t.idTransaccion = @idTransaccion;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso y opción de menú "Transacciones económicas" (A5 y A6).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#RegistrarPermisoAdministracion') IS NOT NULL DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
CREATE PROCEDURE #RegistrarPermisoAdministracion
    @codigo       NVARCHAR(100),
    @nombre       NVARCHAR(200),
    @descripcion  NVARCHAR(500),
    @url          NVARCHAR(300),
    @accion       NVARCHAR(50),
    @orden        INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @idPermiso INT =
        (
            SELECT TOP 1 idPermisoInterno
            FROM dbo.PermisoInterno
            WHERE codigoPermiso = @codigo
            ORDER BY idPermisoInterno
        );

        IF @idPermiso IS NULL
        BEGIN
            INSERT INTO dbo.PermisoInterno
                (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
            VALUES
                (@codigo, @nombre,
                 @descripcion,
                 N'Administración', @accion, N'Activo', @url, 1);

            SET @idPermiso = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.PermisoInterno
            SET nombrePermiso = @nombre,
                descripcion = @descripcion,
                modulo = N'Administración',
                accion = @accion,
                estadoPermiso = N'Activo',
                urlAsociada = @url,
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idPermisoInterno = @idPermiso;
        END

        DECLARE @idRolAdministrador INT =
        (
            SELECT TOP 1 idRolInterno
            FROM dbo.RolInterno
            WHERE nombreRol = N'Administrador'
              AND activo = 1
            ORDER BY idRolInterno
        );

        IF @idRolAdministrador IS NOT NULL
           AND NOT EXISTS
           (
               SELECT 1
               FROM dbo.RolInternoPermiso
               WHERE idRolInterno = @idRolAdministrador
                 AND idPermisoInterno = @idPermiso
           )
        BEGIN
            INSERT INTO dbo.RolInternoPermiso
                (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
            VALUES
                (@idRolAdministrador, @idPermiso, GETDATE(), 1);
        END
        ELSE IF @idRolAdministrador IS NOT NULL
        BEGIN
            UPDATE dbo.RolInternoPermiso
            SET activo = 1
            WHERE idRolInterno = @idRolAdministrador
              AND idPermisoInterno = @idPermiso;
        END

        IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
        BEGIN
            DECLARE @idGrupoAdministracion INT =
            (
                SELECT TOP 1 idComponentePermiso
                FROM dbo.ComponentePermiso
                WHERE tipoComponente = N'Grupo'
                  AND idComponentePadre IS NULL
                  AND nombre = N'Administración'
                ORDER BY idComponentePermiso
            );

            IF @idGrupoAdministracion IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (NULL, N'Grupo', NULL, N'Administración', NULL, NULL, 100, 1);

                SET @idGrupoAdministracion = CAST(SCOPE_IDENTITY() AS INT);
            END

            DECLARE @idComponente INT =
            (
                SELECT TOP 1 idComponentePermiso
                FROM dbo.ComponentePermiso
                WHERE codigoPermiso = @codigo
                ORDER BY idComponentePermiso
            );

            IF @idComponente IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (@idGrupoAdministracion, N'Permiso', @codigo, @nombre,
                     @descripcion,
                     @url, @orden, 1);

                SET @idComponente = CAST(SCOPE_IDENTITY() AS INT);
            END
            ELSE
            BEGIN
                UPDATE dbo.ComponentePermiso
                SET idComponentePadre = @idGrupoAdministracion,
                    tipoComponente = N'Permiso',
                    nombre = @nombre,
                    descripcion = @descripcion,
                    urlAsociada = @url,
                    activo = 1,
                    fechaUltimaModificacion = GETDATE()
                WHERE idComponentePermiso = @idComponente;
            END

            IF @idRolAdministrador IS NOT NULL
            BEGIN
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.RolInternoComponentePermiso
                    WHERE idRolInterno = @idRolAdministrador
                      AND idComponentePermiso = @idComponente
                )
                BEGIN
                    INSERT INTO dbo.RolInternoComponentePermiso
                        (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                    VALUES
                        (@idRolAdministrador, @idComponente, GETDATE(), 1);
                END
                ELSE
                BEGIN
                    UPDATE dbo.RolInternoComponentePermiso
                    SET activo = 1
                    WHERE idRolInterno = @idRolAdministrador
                      AND idComponentePermiso = @idComponente;
                END
            END
        END

        -- Opción del menú dinámico (script 45), al final del menú.
        IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
           AND @idComponente IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponente)
        BEGIN
            INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
            SELECT @nombre,
                   @descripcion,
                   @url, N'Administración',
                   ISNULL(MAX(orden), 0) + 10,
                   @idComponente, 1
            FROM dbo.OpcionMenu;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        DECLARE @mensajeError NVARCHAR(4000);
        SET @mensajeError = ERROR_MESSAGE();
        RAISERROR(N'%s', 16, 1, @mensajeError);
    END CATCH
END
GO

EXEC #RegistrarPermisoAdministracion
    @codigo = N'CONSULTAR_TRANSACCIONES',
    @nombre = N'Transacciones económicas',
    @descripcion = N'Consultar las transacciones económicas de StageUp (reservas abonadas y cargos por cancelación) con la comisión y el importe del gestor.',
    @url = N'~/Interno/TransaccionesEconomicas.aspx',
    @accion = N'Consultar',
    @orden = 26;
GO

DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
