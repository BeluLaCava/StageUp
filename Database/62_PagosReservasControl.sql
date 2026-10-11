IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Pago', N'U') IS NULL OR OBJECT_ID(N'dbo.Comprobante', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/49_PagosNotasYCuentaCorriente.sql.', 1;
END
GO

-- ---------------------------------------------------------------------------
-- CU-002-001: el pago guarda también lo que informa el medio de pago
-- (identificador de la operación, medio, estado informado y fecha), de dónde
-- vino (StageUp o aviso del medio externo) y una observación para el control
-- administrativo. Nuevos estados: Pendiente (el medio lo informó pendiente),
-- Anulado (cancelado), Observado (información incompleta, A1) y NoAsociado
-- (no se encontró la reserva, A2). Estos dos últimos pueden no tener usuario.
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Pago', N'origen') IS NULL
    ALTER TABLE dbo.Pago ADD origen NVARCHAR(20) NOT NULL CONSTRAINT DF_Pago_origen DEFAULT (N'StageUp');
GO
IF COL_LENGTH(N'dbo.Pago', N'identificadorExterno') IS NULL
    ALTER TABLE dbo.Pago ADD identificadorExterno NVARCHAR(60) NULL;
GO
IF COL_LENGTH(N'dbo.Pago', N'medioInformado') IS NULL
    ALTER TABLE dbo.Pago ADD medioInformado NVARCHAR(40) NULL;
GO
IF COL_LENGTH(N'dbo.Pago', N'estadoInformado') IS NULL
    ALTER TABLE dbo.Pago ADD estadoInformado NVARCHAR(40) NULL;
GO
IF COL_LENGTH(N'dbo.Pago', N'fechaOperacion') IS NULL
    ALTER TABLE dbo.Pago ADD fechaOperacion DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.Pago', N'observacion') IS NULL
    ALTER TABLE dbo.Pago ADD observacion NVARCHAR(500) NULL;
GO
IF COL_LENGTH(N'dbo.Pago', N'idUsuarioInternoRegistro') IS NULL
    ALTER TABLE dbo.Pago ADD idUsuarioInternoRegistro INT NULL
        CONSTRAINT FK_Pago_UsuarioInterno FOREIGN KEY REFERENCES dbo.UsuarioInterno (idUsuarioInterno);
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Pago') AND name = N'idUsuarioExterno' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_Usuario' AND object_id = OBJECT_ID(N'dbo.Pago'))
        DROP INDEX IX_Pago_Usuario ON dbo.Pago;
    ALTER TABLE dbo.Pago ALTER COLUMN idUsuarioExterno INT NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_Usuario' AND object_id = OBJECT_ID(N'dbo.Pago'))
    CREATE INDEX IX_Pago_Usuario ON dbo.Pago (idUsuarioExterno, fechaPago DESC);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Pago_estado')
    ALTER TABLE dbo.Pago DROP CONSTRAINT CK_Pago_estado;
GO
ALTER TABLE dbo.Pago WITH CHECK ADD CONSTRAINT CK_Pago_estado
    CHECK (estado IN (N'Aprobado', N'Rechazado', N'Pendiente', N'Anulado', N'Observado', N'NoAsociado'));
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Pago_importes')
    ALTER TABLE dbo.Pago DROP CONSTRAINT CK_Pago_importes;
GO
ALTER TABLE dbo.Pago WITH CHECK ADD CONSTRAINT CK_Pago_importes
    CHECK (importeTotal >= 0 AND importeTarjeta >= 0 AND importeSaldo >= 0
           AND (importeTotal > 0 OR estado IN (N'Observado', N'NoAsociado')));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Pago_origen')
    ALTER TABLE dbo.Pago WITH CHECK ADD CONSTRAINT CK_Pago_origen CHECK (origen IN (N'StageUp', N'MedioExterno'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_Estado' AND object_id = OBJECT_ID(N'dbo.Pago'))
    CREATE INDEX IX_Pago_Estado ON dbo.Pago (estado, fechaPago DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_Identificador' AND object_id = OBJECT_ID(N'dbo.Pago'))
    CREATE INDEX IX_Pago_Identificador ON dbo.Pago (identificadorExterno) WHERE identificadorExterno IS NOT NULL;
GO

-- Los pagos aprobados anteriores toman como identificador el código de
-- autorización (o uno propio de StageUp si se pagó solo con saldo).
UPDATE dbo.Pago
SET identificadorExterno = ISNULL(codigoAutorizacion, N'STU-' + RIGHT(N'00000000' + CAST(idPago AS NVARCHAR(10)), 8)),
    medioInformado = CASE WHEN importeTarjeta > 0 AND importeSaldo > 0 THEN N'Tarjeta y saldo a favor'
                          WHEN importeTarjeta > 0 THEN N'Tarjeta'
                          ELSE N'Saldo a favor' END,
    estadoInformado = estado
WHERE identificadorExterno IS NULL AND estado IN (N'Aprobado', N'Rechazado');
GO

-- ---------------------------------------------------------------------------
-- Factura simulada del pago aprobado (FC): se emite con cada pago de reserva
-- aprobado. No genera movimientos de cuenta corriente (el cargo y el pago ya
-- los registra el pago) y no se puede anular.
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Comprobante', N'idPago') IS NULL
    ALTER TABLE dbo.Comprobante ADD idPago INT NULL
        CONSTRAINT FK_Comprobante_Pago FOREIGN KEY REFERENCES dbo.Pago (idPago);
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Comprobante_tipo')
    ALTER TABLE dbo.Comprobante DROP CONSTRAINT CK_Comprobante_tipo;
GO
ALTER TABLE dbo.Comprobante WITH CHECK ADD CONSTRAINT CK_Comprobante_tipo CHECK (tipo IN (N'NC', N'ND', N'FC'));
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Comprobante_origen')
    ALTER TABLE dbo.Comprobante DROP CONSTRAINT CK_Comprobante_origen;
GO
ALTER TABLE dbo.Comprobante WITH CHECK ADD CONSTRAINT CK_Comprobante_origen
    CHECK (origen IN (N'Cancelacion', N'PenalidadCancelacion', N'AjusteManual', N'PagoReserva'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Comprobante_FacturaPago' AND object_id = OBJECT_ID(N'dbo.Comprobante'))
    EXEC (N'CREATE UNIQUE INDEX UX_Comprobante_FacturaPago ON dbo.Comprobante (idPago) WHERE tipo = N''FC'' AND idPago IS NOT NULL');
GO

-- Facturas de los pagos de reserva aprobados antes de este script.
INSERT INTO dbo.Comprobante (tipo, idUsuarioExterno, rolCuenta, idReserva, importe, moneda, origen, motivo, fechaEmision, idPago)
SELECT N'FC', p.idUsuarioExterno, N'Cliente', p.idReserva, p.importeTotal, p.moneda, N'PagoReserva',
       N'Factura simulada del pago de la reserva N° ' + CAST(p.idReserva AS NVARCHAR(10)) + N' (' + e.nombreEspacio + N').',
       p.fechaPago, p.idPago
FROM dbo.Pago p
INNER JOIN dbo.Reserva r ON r.idReserva = p.idReserva
INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
WHERE p.estado = N'Aprobado' AND p.concepto = N'Reserva' AND p.idUsuarioExterno IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Comprobante c WHERE c.idPago = p.idPago AND c.tipo = N'FC');
GO

-- ---------------------------------------------------------------------------
-- Registro del pago aprobado de una reserva (redefinido): además de los
-- movimientos, guarda lo que informa el medio de pago y emite la factura
-- simulada (FC). Si viene @idPagoPendiente (un aviso anterior del medio
-- externo en estado Pendiente), se confirma ese mismo pago en lugar de crear
-- otro.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Pago_RegistrarReserva', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_RegistrarReserva;
GO
CREATE PROCEDURE dbo.sp_Pago_RegistrarReserva
    @idReserva                      INT,
    @idUsuarioExterno               INT,
    @importeTarjeta                 DECIMAL(18,2),
    @importeSaldo                   DECIMAL(18,2),
    @marcaTarjeta                   NVARCHAR(30)  = NULL,
    @ultimosDigitos                 NVARCHAR(4)   = NULL,
    @codigoAutorizacion             NVARCHAR(20)  = NULL,
    @porcentajeComisionPlataforma   DECIMAL(5,2),
    @origen                         NVARCHAR(20)  = N'StageUp',
    @identificadorExterno           NVARCHAR(60)  = NULL,
    @medioInformado                 NVARCHAR(40)  = NULL,
    @fechaOperacion                 DATETIME      = NULL,
    @idPagoPendiente                INT           = NULL,
    @idUsuarioInternoRegistro       INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @recurso NVARCHAR(100) = N'StageUp_CuentaCorriente_' + CAST(@idUsuarioExterno AS NVARCHAR(10));
        DECLARE @bloqueo INT;
        EXEC @bloqueo = sp_getapplock @Resource = @recurso, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
        IF @bloqueo < 0
            THROW 51010, N'La cuenta está ocupada con otra operación. Probá de nuevo en unos segundos.', 1;

        DECLARE @estadoReserva NVARCHAR(50), @estadoPago NVARCHAR(20), @idSolicitante INT, @total DECIMAL(18,2),
                @moneda NVARCHAR(3), @limite DATETIME, @idGestor INT, @nombreEspacio NVARCHAR(300);

        SELECT @estadoReserva = r.estadoReserva, @estadoPago = r.estadoPago, @idSolicitante = r.idUsuarioExternoSolicitante,
               @total = r.importeEstimado, @moneda = ISNULL(r.moneda, N'ARS'), @limite = r.fechaLimitePago,
               @idGestor = e.idUsuarioGestor, @nombreEspacio = e.nombreEspacio
        FROM dbo.Reserva r WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        WHERE r.idReserva = @idReserva;

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @idSolicitante IS NULL OR @idSolicitante <> @idUsuarioExterno SET @resultado = N'NO_ENCONTRADA';
        ELSE IF @estadoReserva <> N'Aceptada' OR @estadoPago <> N'Pendiente' SET @resultado = N'NO_PENDIENTE';
        ELSE IF @limite IS NOT NULL AND @limite <= GETDATE() SET @resultado = N'VENCIDA';
        ELSE IF @importeTarjeta IS NULL OR @importeSaldo IS NULL OR @total IS NULL
             OR @importeTarjeta < 0 OR @importeSaldo < 0 OR @importeTarjeta + @importeSaldo <> @total
             OR (@importeTarjeta > 0 AND @origen <> N'MedioExterno' AND (@marcaTarjeta IS NULL OR @ultimosDigitos IS NULL))
            SET @resultado = N'IMPORTE_INVALIDO';
        ELSE IF @importeSaldo > 0 AND @importeSaldo > ISNULL((
                SELECT SUM(importe) FROM dbo.MovimientoCuentaCorriente
                WHERE idUsuarioExterno = @idUsuarioExterno AND rolCuenta = N'Cliente' AND moneda = @moneda), 0)
            SET @resultado = N'SALDO_INSUFICIENTE';
        ELSE IF @identificadorExterno IS NOT NULL AND EXISTS (
                SELECT 1 FROM dbo.Pago WHERE identificadorExterno = @identificadorExterno AND estado = N'Aprobado')
            SET @resultado = N'YA_REGISTRADO';
        ELSE IF @idPagoPendiente IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM dbo.Pago WHERE idPago = @idPagoPendiente AND estado = N'Pendiente'
                  AND (idReserva IS NULL OR idReserva = @idReserva))
            SET @resultado = N'PAGO_NO_PENDIENTE';

        IF @resultado <> N'OK'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @resultado AS resultado, CAST(NULL AS INT) AS idPago, CAST(NULL AS INT) AS idComprobante;
            RETURN;
        END

        DECLARE @comision DECIMAL(18,2) = ROUND(@total * @porcentajeComisionPlataforma / 100, 2);
        DECLARE @textoReserva NVARCHAR(400) = N'reserva N° ' + CAST(@idReserva AS NVARCHAR(10)) + N' (' + @nombreEspacio + N')';
        DECLARE @medio NVARCHAR(40) = ISNULL(@medioInformado,
            CASE WHEN @importeTarjeta > 0 AND @importeSaldo > 0 THEN N'Tarjeta y saldo a favor'
                 WHEN @importeTarjeta > 0 THEN N'Tarjeta'
                 ELSE N'Saldo a favor' END);
        DECLARE @idPago INT;

        IF @idPagoPendiente IS NOT NULL
        BEGIN
            UPDATE dbo.Pago
            SET idUsuarioExterno = @idUsuarioExterno, idReserva = @idReserva, moneda = @moneda,
                importeTotal = @total, importeTarjeta = @importeTarjeta, importeSaldo = @importeSaldo,
                estado = N'Aprobado', estadoInformado = N'Aprobado',
                marcaTarjeta = CASE WHEN @importeTarjeta > 0 THEN @marcaTarjeta END,
                ultimosDigitos = CASE WHEN @importeTarjeta > 0 THEN @ultimosDigitos END,
                codigoAutorizacion = CASE WHEN @importeTarjeta > 0 THEN @codigoAutorizacion END,
                porcentajeComisionPlataforma = @porcentajeComisionPlataforma, importeComisionPlataforma = @comision,
                medioInformado = @medio, fechaOperacion = ISNULL(@fechaOperacion, fechaOperacion),
                identificadorExterno = ISNULL(@identificadorExterno, identificadorExterno),
                idUsuarioInternoRegistro = ISNULL(@idUsuarioInternoRegistro, idUsuarioInternoRegistro),
                fechaPago = GETDATE()
            WHERE idPago = @idPagoPendiente;
            SET @idPago = @idPagoPendiente;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.Pago
                (idUsuarioExterno, idReserva, concepto, moneda, importeTotal, importeTarjeta, importeSaldo, estado,
                 marcaTarjeta, ultimosDigitos, codigoAutorizacion,
                 porcentajeComisionPlataforma, importeComisionPlataforma,
                 origen, identificadorExterno, medioInformado, estadoInformado, fechaOperacion, idUsuarioInternoRegistro)
            VALUES
                (@idUsuarioExterno, @idReserva, N'Reserva', @moneda, @total, @importeTarjeta, @importeSaldo, N'Aprobado',
                 CASE WHEN @importeTarjeta > 0 THEN @marcaTarjeta END,
                 CASE WHEN @importeTarjeta > 0 THEN @ultimosDigitos END,
                 CASE WHEN @importeTarjeta > 0 THEN @codigoAutorizacion END,
                 @porcentajeComisionPlataforma, @comision,
                 CASE WHEN @origen = N'MedioExterno' THEN N'MedioExterno' ELSE N'StageUp' END,
                 ISNULL(@identificadorExterno, CASE WHEN @importeTarjeta > 0 THEN @codigoAutorizacion END),
                 @medio, N'Aprobado', ISNULL(@fechaOperacion, GETDATE()), @idUsuarioInternoRegistro);

            SET @idPago = CAST(SCOPE_IDENTITY() AS INT);

            UPDATE dbo.Pago
            SET identificadorExterno = N'STU-' + RIGHT(N'00000000' + CAST(@idPago AS NVARCHAR(10)), 8)
            WHERE idPago = @idPago AND identificadorExterno IS NULL;
        END

        -- Cliente: el cargo de la reserva y lo que pagó con tarjeta (lo
        -- cubierto con saldo a favor lo descuenta el propio cargo).
        INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
        VALUES (@idUsuarioExterno, N'Cliente', N'CargoReserva', -@total, @moneda,
                N'Cargo por la ' + @textoReserva +
                CASE WHEN @importeSaldo > 0 THEN N'. Se usaron ' + CAST(@importeSaldo AS NVARCHAR(30)) + N' ' + @moneda + N' de saldo a favor.' ELSE N'' END,
                @idReserva, @idPago);

        IF @importeTarjeta > 0
        BEGIN
            INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
            VALUES (@idUsuarioExterno, N'Cliente', N'PagoTarjeta', @importeTarjeta, @moneda,
                    CASE WHEN @origen = N'MedioExterno'
                         THEN N'Pago informado por el medio de pago (' + @medio + N', operación ' + ISNULL(@identificadorExterno, N'-') + N')'
                         ELSE N'Pago con tarjeta ' + @marcaTarjeta + N' terminada en ' + @ultimosDigitos + N' (autorización ' + ISNULL(@codigoAutorizacion, N'-') + N')' END,
                    @idReserva, @idPago);
        END

        -- Gestor: el importe de la reserva menos la comisión de StageUp.
        INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
        VALUES (@idGestor, N'Gestor', N'IngresoReserva', @total, @moneda, N'Ingreso por la ' + @textoReserva, @idReserva, @idPago);

        IF @comision > 0
        BEGIN
            INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
            VALUES (@idGestor, N'Gestor', N'ComisionPlataforma', -@comision, @moneda,
                    N'Comisión StageUp (' + CAST(@porcentajeComisionPlataforma AS NVARCHAR(10)) + N' %) de la ' + @textoReserva,
                    @idReserva, @idPago);
        END

        -- Factura simulada del pago.
        INSERT INTO dbo.Comprobante (tipo, idUsuarioExterno, rolCuenta, idReserva, importe, moneda, origen, motivo, idPago)
        VALUES (N'FC', @idUsuarioExterno, N'Cliente', @idReserva, @total, @moneda, N'PagoReserva',
                N'Factura simulada del pago de la ' + @textoReserva + N'.', @idPago);
        DECLARE @idComprobante INT = CAST(SCOPE_IDENTITY() AS INT);

        UPDATE dbo.Reserva
        SET estadoPago = N'Pagado', fechaPago = GETDATE(), fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva;

        COMMIT TRANSACTION;
        SELECT N'OK' AS resultado, @idPago AS idPago, @idComprobante AS idComprobante;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ---------------------------------------------------------------------------
-- CU-002-001 pasos 1 a 9 y A1 a A5: aviso del medio de pago externo que no
-- confirma un pago (pendiente, rechazado/fallido, cancelado) o que no se pudo
-- asociar (información incompleta u operación inexistente). Si ya hay un pago
-- pendiente con el mismo identificador, se actualiza ese mismo.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Pago_RegistrarAviso', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_RegistrarAviso;
GO
CREATE PROCEDURE dbo.sp_Pago_RegistrarAviso
    @idReserva                  INT            = NULL,
    @idUsuarioExterno           INT            = NULL,
    @moneda                     NVARCHAR(3)    = N'ARS',
    @importeTotal               DECIMAL(18,2)  = 0,
    @estado                     NVARCHAR(20),
    @identificadorExterno       NVARCHAR(60)   = NULL,
    @medioInformado             NVARCHAR(40)   = NULL,
    @estadoInformado            NVARCHAR(40)   = NULL,
    @fechaOperacion             DATETIME       = NULL,
    @motivoRechazo              NVARCHAR(300)  = NULL,
    @observacion                NVARCHAR(500)  = NULL,
    @idUsuarioInternoRegistro   INT            = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @estado NOT IN (N'Pendiente', N'Rechazado', N'Anulado', N'Observado', N'NoAsociado')
    BEGIN
        RAISERROR(N'Estado de aviso no válido.', 16, 1);
        RETURN;
    END

    DECLARE @idPago INT =
    (
        SELECT TOP 1 idPago FROM dbo.Pago
        WHERE @identificadorExterno IS NOT NULL
          AND identificadorExterno = @identificadorExterno
          AND estado = N'Pendiente'
        ORDER BY idPago DESC
    );

    IF @idPago IS NOT NULL AND @estado IN (N'Pendiente', N'Rechazado', N'Anulado')
    BEGIN
        UPDATE dbo.Pago
        SET estado = @estado,
            estadoInformado = ISNULL(@estadoInformado, @estado),
            motivoRechazo = ISNULL(@motivoRechazo, motivoRechazo),
            observacion = ISNULL(@observacion, observacion),
            fechaOperacion = ISNULL(@fechaOperacion, fechaOperacion),
            medioInformado = ISNULL(@medioInformado, medioInformado),
            idUsuarioInternoRegistro = ISNULL(@idUsuarioInternoRegistro, idUsuarioInternoRegistro)
        WHERE idPago = @idPago;

        SELECT @idPago AS idPago;
        RETURN;
    END

    INSERT INTO dbo.Pago
        (idUsuarioExterno, idReserva, concepto, moneda, importeTotal, importeTarjeta, importeSaldo, estado,
         motivoRechazo, origen, identificadorExterno, medioInformado, estadoInformado, fechaOperacion,
         observacion, idUsuarioInternoRegistro)
    VALUES
        (@idUsuarioExterno, @idReserva, N'Reserva', ISNULL(@moneda, N'ARS'), ISNULL(@importeTotal, 0), 0, 0, @estado,
         @motivoRechazo, N'MedioExterno', @identificadorExterno, @medioInformado, ISNULL(@estadoInformado, @estado),
         @fechaOperacion, @observacion, @idUsuarioInternoRegistro);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idPago;
END
GO

-- Pagos ya registrados con un identificador del medio de pago (para no
-- registrar dos veces el mismo aviso y para confirmar uno pendiente).
IF OBJECT_ID('dbo.sp_Pago_BuscarPorIdentificador', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_BuscarPorIdentificador;
GO
CREATE PROCEDURE dbo.sp_Pago_BuscarPorIdentificador
    @identificadorExterno NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPago, idReserva, estado
    FROM dbo.Pago
    WHERE identificadorExterno = @identificadorExterno
    ORDER BY CASE estado WHEN N'Aprobado' THEN 0 WHEN N'Pendiente' THEN 1 ELSE 2 END, idPago DESC;
END
GO

-- ---------------------------------------------------------------------------
-- CU-002-001 A6: listado de "Pagos de reservas". Además de los pagos
-- registrados, muestra como pendientes las reservas aceptadas que esperan el
-- pago y como canceladas las que vencieron sin pagarse. @grupo: Todos,
-- Pendiente, Confirmado, Fallido, Cancelado, EnRevision.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Pago_ListarAdministracion', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_ListarAdministracion;
GO
CREATE PROCEDURE dbo.sp_Pago_ListarAdministracion
    @grupo  NVARCHAR(20)  = N'Todos',
    @desde  DATE          = NULL,
    @hasta  DATE          = NULL,
    @texto  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hastaExclusivo DATETIME = CASE WHEN @hasta IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@hasta AS DATETIME)) END;
    DECLARE @like NVARCHAR(110) = CASE WHEN @texto IS NULL OR LTRIM(RTRIM(@texto)) = N'' THEN NULL
                                       ELSE N'%' + LTRIM(RTRIM(@texto)) + N'%' END;

    WITH filas AS
    (
        SELECT p.idPago, p.idReserva, p.concepto, p.origen, p.estado,
               CASE p.estado WHEN N'Aprobado' THEN N'Confirmado' WHEN N'Rechazado' THEN N'Fallido'
                             WHEN N'Anulado' THEN N'Cancelado' WHEN N'Pendiente' THEN N'Pendiente'
                             ELSE N'EnRevision' END AS grupo,
               p.moneda, p.importeTotal, p.importeTarjeta, p.importeSaldo,
               ISNULL(p.medioInformado, CASE WHEN p.marcaTarjeta IS NOT NULL THEN N'Tarjeta ' + p.marcaTarjeta END) AS medio,
               p.identificadorExterno, ISNULL(p.fechaOperacion, p.fechaPago) AS fecha,
               LTRIM(RTRIM(ISNULL(u.nombre, N'') + N' ' + ISNULL(u.apellido, N''))) AS nombreUsuario,
               u.correoElectronico AS correoUsuario, e.nombreEspacio,
               CAST(NULL AS DATETIME) AS fechaLimitePago
        FROM dbo.Pago p
        LEFT JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = p.idUsuarioExterno
        LEFT JOIN dbo.Reserva r ON r.idReserva = p.idReserva
        LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico

        UNION ALL

        SELECT NULL, r.idReserva, N'Reserva', N'StageUp',
               CASE WHEN r.estadoPago = N'Vencido' THEN N'Vencido' ELSE N'EsperandoPago' END,
               CASE WHEN r.estadoPago = N'Vencido' THEN N'Cancelado' ELSE N'Pendiente' END,
               ISNULL(r.moneda, N'ARS'), r.importeEstimado, 0, 0, NULL, NULL,
               CASE WHEN r.estadoPago = N'Vencido' THEN ISNULL(r.fechaCancelacion, r.fechaLimitePago) ELSE r.fechaResolucion END,
               LTRIM(RTRIM(u.nombre + N' ' + u.apellido)), u.correoElectronico, e.nombreEspacio, r.fechaLimitePago
        FROM dbo.Reserva r
        INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = r.idUsuarioExternoSolicitante
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        WHERE ISNULL(r.importeEstimado, 0) > 0
          AND ((r.estadoReserva = N'Aceptada' AND r.estadoPago = N'Pendiente') OR r.estadoPago = N'Vencido')
          AND NOT EXISTS (SELECT 1 FROM dbo.Pago pp WHERE pp.idReserva = r.idReserva AND pp.estado IN (N'Pendiente', N'Aprobado'))
    )
    SELECT TOP 500 *
    FROM filas
    WHERE (@grupo IS NULL OR @grupo = N'Todos' OR grupo = @grupo)
      AND (@desde IS NULL OR fecha >= @desde)
      AND (@hastaExclusivo IS NULL OR fecha < @hastaExclusivo)
      AND (@like IS NULL
           OR nombreUsuario LIKE @like OR correoUsuario LIKE @like OR nombreEspacio LIKE @like
           OR identificadorExterno LIKE @like
           OR CAST(idReserva AS NVARCHAR(10)) = LTRIM(RTRIM(@texto))
           OR CAST(idPago AS NVARCHAR(10)) = LTRIM(RTRIM(@texto)))
    ORDER BY fecha DESC;
END
GO

-- Cantidad de pagos sin filtros (para distinguir A8 de A9).
IF OBJECT_ID('dbo.sp_Pago_ContarAdministracion', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_ContarAdministracion;
GO
CREATE PROCEDURE dbo.sp_Pago_ContarAdministracion
AS
BEGIN
    SET NOCOUNT ON;
    SELECT (SELECT COUNT(*) FROM dbo.Pago)
         + (SELECT COUNT(*) FROM dbo.Reserva r
            WHERE ISNULL(r.importeEstimado, 0) > 0
              AND ((r.estadoReserva = N'Aceptada' AND r.estadoPago = N'Pendiente') OR r.estadoPago = N'Vencido')) AS total;
END
GO

-- ---------------------------------------------------------------------------
-- CU-002-001 A6 pasos 9 y 10: detalle del pago (o de la reserva que espera el
-- pago, si todavía no hay un pago registrado) con la reserva, el espacio, el
-- solicitante, el gestor, la comisión y la factura simulada.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Pago_ObtenerDetalle', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_ObtenerDetalle;
GO
CREATE PROCEDURE dbo.sp_Pago_ObtenerDetalle
    @idPago     INT = NULL,
    @idReserva  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @idPago IS NOT NULL
    BEGIN
        SELECT p.idPago, p.idUsuarioExterno, p.idReserva, p.concepto, p.moneda, p.importeTotal, p.importeTarjeta, p.importeSaldo,
               p.estado, p.marcaTarjeta, p.ultimosDigitos, p.codigoAutorizacion, p.motivoRechazo,
               p.porcentajeComisionPlataforma, p.importeComisionPlataforma, p.fechaPago,
               p.origen, p.identificadorExterno, p.medioInformado, p.estadoInformado, p.fechaOperacion, p.observacion,
               LTRIM(RTRIM(ISNULL(u.nombre, N'') + N' ' + ISNULL(u.apellido, N''))) AS nombreUsuario, u.correoElectronico AS correoUsuario,
               e.nombreEspacio, e.idUsuarioGestor,
               LTRIM(RTRIM(ISNULL(g.nombre, N'') + N' ' + ISNULL(g.apellido, N''))) AS nombreGestor, g.correoElectronico AS correoGestor,
               r.estadoReserva, r.estadoPago AS estadoPagoReserva, r.fechaSolicitada, r.minutoDesde, r.minutoHasta, r.fechaLimitePago,
               c.idComprobante, c.numero AS numeroFactura, c.fechaEmision AS fechaFactura, c.estado AS estadoFactura,
               LTRIM(RTRIM(ISNULL(ui.nombre, N'') + N' ' + ISNULL(ui.apellido, N''))) AS nombreInternoRegistro
        FROM dbo.Pago p
        LEFT JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = p.idUsuarioExterno
        LEFT JOIN dbo.Reserva r ON r.idReserva = p.idReserva
        LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
        LEFT JOIN dbo.Comprobante c ON c.idPago = p.idPago AND c.tipo = N'FC'
        LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = p.idUsuarioInternoRegistro
        WHERE p.idPago = @idPago;
        RETURN;
    END

    SELECT CAST(NULL AS INT) AS idPago, r.idUsuarioExternoSolicitante AS idUsuarioExterno, r.idReserva, N'Reserva' AS concepto,
           ISNULL(r.moneda, N'ARS') AS moneda, r.importeEstimado AS importeTotal, CAST(0 AS DECIMAL(18,2)) AS importeTarjeta,
           CAST(0 AS DECIMAL(18,2)) AS importeSaldo,
           CASE WHEN r.estadoPago = N'Vencido' THEN N'Vencido' ELSE N'EsperandoPago' END AS estado,
           CAST(NULL AS NVARCHAR(30)) AS marcaTarjeta, CAST(NULL AS NVARCHAR(4)) AS ultimosDigitos,
           CAST(NULL AS NVARCHAR(20)) AS codigoAutorizacion, CAST(NULL AS NVARCHAR(300)) AS motivoRechazo,
           CAST(NULL AS DECIMAL(5,2)) AS porcentajeComisionPlataforma, CAST(NULL AS DECIMAL(18,2)) AS importeComisionPlataforma,
           ISNULL(r.fechaResolucion, r.fechaCreacion) AS fechaPago, N'StageUp' AS origen, CAST(NULL AS NVARCHAR(60)) AS identificadorExterno,
           CAST(NULL AS NVARCHAR(40)) AS medioInformado, CAST(NULL AS NVARCHAR(40)) AS estadoInformado,
           CAST(NULL AS DATETIME) AS fechaOperacion, CAST(NULL AS NVARCHAR(500)) AS observacion,
           LTRIM(RTRIM(u.nombre + N' ' + u.apellido)) AS nombreUsuario, u.correoElectronico AS correoUsuario,
           e.nombreEspacio, e.idUsuarioGestor,
           LTRIM(RTRIM(g.nombre + N' ' + g.apellido)) AS nombreGestor, g.correoElectronico AS correoGestor,
           r.estadoReserva, r.estadoPago AS estadoPagoReserva, r.fechaSolicitada, r.minutoDesde, r.minutoHasta, r.fechaLimitePago,
           CAST(NULL AS INT) AS idComprobante, CAST(NULL AS NVARCHAR(20)) AS numeroFactura, CAST(NULL AS DATETIME) AS fechaFactura,
           CAST(NULL AS NVARCHAR(10)) AS estadoFactura, CAST(NULL AS NVARCHAR(200)) AS nombreInternoRegistro
    FROM dbo.Reserva r
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = r.idUsuarioExternoSolicitante
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE r.idReserva = @idReserva
      AND ISNULL(r.importeEstimado, 0) > 0
      AND ((r.estadoReserva = N'Aceptada' AND r.estadoPago = N'Pendiente') OR r.estadoPago = N'Vencido');
END
GO

-- Pago aprobado (con su factura) de una reserva, para el detalle que ven el
-- solicitante y el gestor.
IF OBJECT_ID('dbo.sp_Pago_ObtenerAprobadoPorReserva', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_ObtenerAprobadoPorReserva;
GO
CREATE PROCEDURE dbo.sp_Pago_ObtenerAprobadoPorReserva
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 idPago FROM dbo.Pago
    WHERE idReserva = @idReserva AND estado = N'Aprobado'
    ORDER BY idPago DESC;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso y opción de menú "Pagos de reservas" (CU-002-001 A6 y A7).
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
    @codigo = N'CONTROLAR_PAGOS_RESERVAS',
    @nombre = N'Pagos de reservas',
    @descripcion = N'Consultar y controlar los pagos de reservas: estado, medio, reserva o cargo asociado y factura; registrar avisos del medio de pago.',
    @url = N'~/Interno/PagosReservas.aspx',
    @accion = N'Controlar',
    @orden = 25;
GO

DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
