IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF COL_LENGTH(N'dbo.Pago', N'titularTarjeta') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Pago DROP COLUMN titularTarjeta;
END
GO

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
    @porcentajeComisionPlataforma   DECIMAL(5,2)
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
             OR (@importeTarjeta > 0 AND (@marcaTarjeta IS NULL OR @ultimosDigitos IS NULL))
            SET @resultado = N'IMPORTE_INVALIDO';
        ELSE IF @importeSaldo > 0 AND @importeSaldo > ISNULL((
                SELECT SUM(importe) FROM dbo.MovimientoCuentaCorriente
                WHERE idUsuarioExterno = @idUsuarioExterno AND rolCuenta = N'Cliente' AND moneda = @moneda), 0)
            SET @resultado = N'SALDO_INSUFICIENTE';

        IF @resultado <> N'OK'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @resultado AS resultado, CAST(NULL AS INT) AS idPago;
            RETURN;
        END

        DECLARE @comision DECIMAL(18,2) = ROUND(@total * @porcentajeComisionPlataforma / 100, 2);
        DECLARE @textoReserva NVARCHAR(400) = N'reserva N° ' + CAST(@idReserva AS NVARCHAR(10)) + N' (' + @nombreEspacio + N')';

        INSERT INTO dbo.Pago
            (idUsuarioExterno, idReserva, concepto, moneda, importeTotal, importeTarjeta, importeSaldo, estado,
             marcaTarjeta, ultimosDigitos, codigoAutorizacion,
             porcentajeComisionPlataforma, importeComisionPlataforma)
        VALUES
            (@idUsuarioExterno, @idReserva, N'Reserva', @moneda, @total, @importeTarjeta, @importeSaldo, N'Aprobado',
             CASE WHEN @importeTarjeta > 0 THEN @marcaTarjeta END,
             CASE WHEN @importeTarjeta > 0 THEN @ultimosDigitos END,
             CASE WHEN @importeTarjeta > 0 THEN @codigoAutorizacion END,
             @porcentajeComisionPlataforma, @comision);

        DECLARE @idPago INT = CAST(SCOPE_IDENTITY() AS INT);

        -- Cliente: el cargo de la reserva y lo que pagó con tarjeta. Lo que se
        -- cubrió con saldo a favor no necesita otro movimiento: el cargo lo
        -- descuenta del saldo.
        INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
        VALUES (@idUsuarioExterno, N'Cliente', N'CargoReserva', -@total, @moneda,
                N'Cargo por la ' + @textoReserva +
                CASE WHEN @importeSaldo > 0 THEN N'. Se usaron ' + CAST(@importeSaldo AS NVARCHAR(30)) + N' ' + @moneda + N' de saldo a favor.' ELSE N'' END,
                @idReserva, @idPago);

        IF @importeTarjeta > 0
        BEGIN
            INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
            VALUES (@idUsuarioExterno, N'Cliente', N'PagoTarjeta', @importeTarjeta, @moneda,
                    N'Pago con tarjeta ' + @marcaTarjeta + N' terminada en ' + @ultimosDigitos + N' (autorización ' + ISNULL(@codigoAutorizacion, N'-') + N')',
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

        UPDATE dbo.Reserva
        SET estadoPago = N'Pagado', fechaPago = GETDATE(), fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva;

        COMMIT TRANSACTION;
        SELECT N'OK' AS resultado, @idPago AS idPago;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Pago_RegistrarRechazado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_RegistrarRechazado;
GO
CREATE PROCEDURE dbo.sp_Pago_RegistrarRechazado
    @idUsuarioExterno   INT,
    @idReserva          INT = NULL,
    @concepto           NVARCHAR(20),
    @moneda             NVARCHAR(3),
    @importeTotal       DECIMAL(18,2),
    @importeTarjeta     DECIMAL(18,2),
    @marcaTarjeta       NVARCHAR(30) = NULL,
    @ultimosDigitos     NVARCHAR(4) = NULL,
    @motivoRechazo      NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Pago
        (idUsuarioExterno, idReserva, concepto, moneda, importeTotal, importeTarjeta, importeSaldo, estado,
         marcaTarjeta, ultimosDigitos, motivoRechazo)
    VALUES
        (@idUsuarioExterno, @idReserva, @concepto, @moneda, @importeTotal, @importeTarjeta, 0, N'Rechazado',
         @marcaTarjeta, @ultimosDigitos, @motivoRechazo);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idPago;
END
GO

IF OBJECT_ID('dbo.sp_CuentaCorriente_PagarDeuda', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CuentaCorriente_PagarDeuda;
GO
CREATE PROCEDURE dbo.sp_CuentaCorriente_PagarDeuda
    @idUsuarioExterno   INT,
    @moneda             NVARCHAR(3),
    @importe            DECIMAL(18,2),
    @marcaTarjeta       NVARCHAR(30),
    @ultimosDigitos     NVARCHAR(4),
    @codigoAutorizacion NVARCHAR(20)
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

        DECLARE @saldo DECIMAL(18,2) = ISNULL((
            SELECT SUM(importe) FROM dbo.MovimientoCuentaCorriente
            WHERE idUsuarioExterno = @idUsuarioExterno AND rolCuenta = N'Cliente' AND moneda = @moneda), 0);

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @saldo >= 0 SET @resultado = N'SIN_DEUDA';
        ELSE IF @importe IS NULL OR @importe <= 0 OR @importe > -@saldo
             OR @marcaTarjeta IS NULL OR @ultimosDigitos IS NULL OR @codigoAutorizacion IS NULL
            SET @resultado = N'IMPORTE_INVALIDO';

        IF @resultado <> N'OK'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @resultado AS resultado, CAST(NULL AS INT) AS idPago;
            RETURN;
        END

        INSERT INTO dbo.Pago
            (idUsuarioExterno, idReserva, concepto, moneda, importeTotal, importeTarjeta, importeSaldo, estado,
             marcaTarjeta, ultimosDigitos, codigoAutorizacion)
        VALUES
            (@idUsuarioExterno, NULL, N'Deuda', @moneda, @importe, @importe, 0, N'Aprobado',
             @marcaTarjeta, @ultimosDigitos, @codigoAutorizacion);

        DECLARE @idPago INT = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idPago)
        VALUES (@idUsuarioExterno, N'Cliente', N'PagoTarjeta', @importe, @moneda,
                N'Pago de saldo deudor con tarjeta ' + @marcaTarjeta + N' terminada en ' + @ultimosDigitos +
                N' (autorización ' + @codigoAutorizacion + N')', @idPago);

        COMMIT TRANSACTION;
        SELECT N'OK' AS resultado, @idPago AS idPago;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Pago_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_Listar;
GO
CREATE PROCEDURE dbo.sp_Pago_Listar
    @desde   DATE,
    @hasta   DATE,
    @estado  NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 300
        p.idPago, p.idUsuarioExterno, p.idReserva, p.concepto, p.moneda, p.importeTotal, p.importeTarjeta, p.importeSaldo,
        p.estado, p.marcaTarjeta, p.ultimosDigitos, p.codigoAutorizacion, p.motivoRechazo,
        p.porcentajeComisionPlataforma, p.importeComisionPlataforma, p.fechaPago,
        LTRIM(RTRIM(u.nombre + N' ' + u.apellido)) AS nombreUsuario, u.correoElectronico AS correoUsuario,
        e.nombreEspacio
    FROM dbo.Pago p
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = p.idUsuarioExterno
    LEFT JOIN dbo.Reserva r ON r.idReserva = p.idReserva
    LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE p.fechaPago >= @desde AND p.fechaPago < DATEADD(DAY, 1, @hasta)
      AND (@estado IS NULL OR p.estado = @estado)
    ORDER BY p.fechaPago DESC;
END
GO

-- ---------------------------------------------------------------------------
-- Menú dinámico: solo páginas internas
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OpcionMenu_url')
        ALTER TABLE dbo.OpcionMenu DROP CONSTRAINT CK_OpcionMenu_url;

    DELETE FROM dbo.OpcionMenu
    WHERE NOT (
        url LIKE N'~/Interno/%.aspx'
        AND url NOT LIKE N'%..%'
        AND url NOT LIKE N'%:%'
        AND url NOT LIKE N'%//%'
        AND url NOT LIKE N'%\%'
        AND url NOT LIKE N'% %');

    IF @@ROWCOUNT > 0
        PRINT N'Se eliminaron opciones de menú que no apuntaban a una página interna.';

    ALTER TABLE dbo.OpcionMenu WITH CHECK ADD CONSTRAINT CK_OpcionMenu_url CHECK (
        url LIKE N'~/Interno/%.aspx'
        AND url NOT LIKE N'%..%'
        AND url NOT LIKE N'%:%'
        AND url NOT LIKE N'%//%'
        AND url NOT LIKE N'%\%'
        AND url NOT LIKE N'% %');
END
GO
