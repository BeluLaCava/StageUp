IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID('dbo.sp_UsuarioExterno_BajaLogica', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_BajaLogica;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_BajaLogica
    @idUsuarioExterno INT,
    @confirmar        BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @confirmar = 1
        BEGIN
            -- Mismo recurso que usan los pagos y las liquidaciones: mientras se
            -- da de baja la cuenta no puede entrar un pago ni un movimiento.
            DECLARE @recurso NVARCHAR(100) = N'StageUp_CuentaCorriente_' + CAST(@idUsuarioExterno AS NVARCHAR(10));
            DECLARE @bloqueo INT;
            EXEC @bloqueo = sp_getapplock @Resource = @recurso, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
            IF @bloqueo < 0
                THROW 50001, N'No se pudo procesar la baja en este momento. Probá nuevamente en unos segundos.', 1;
        END

        DECLARE @estadoCuenta NVARCHAR(100);
        SELECT @estadoCuenta = estadoCuenta
        FROM dbo.UsuarioExterno WITH (UPDLOCK, HOLDLOCK)
        WHERE idUsuarioExterno = @idUsuarioExterno;

        DECLARE @reservasPendientes INT, @reservasAceptadas INT, @pagosPendientes INT;
        SELECT @reservasPendientes = ISNULL(SUM(CASE WHEN estadoReserva = N'Pendiente' THEN 1 ELSE 0 END), 0),
               @reservasAceptadas  = ISNULL(SUM(CASE WHEN estadoReserva = N'Aceptada' THEN 1 ELSE 0 END), 0),
               @pagosPendientes    = ISNULL(SUM(CASE WHEN estadoReserva = N'Aceptada' AND estadoPago = N'Pendiente' THEN 1 ELSE 0 END), 0)
        FROM dbo.Reserva
        WHERE idUsuarioExternoSolicitante = @idUsuarioExterno
          AND estadoReserva IN (N'Pendiente', N'Aceptada');

        DECLARE @solicitudesRecibidas INT, @reservasRecibidasAceptadas INT;
        SELECT @solicitudesRecibidas       = ISNULL(SUM(CASE WHEN r.estadoReserva = N'Pendiente' THEN 1 ELSE 0 END), 0),
               @reservasRecibidasAceptadas = ISNULL(SUM(CASE WHEN r.estadoReserva = N'Aceptada' THEN 1 ELSE 0 END), 0)
        FROM dbo.Reserva r
        JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        WHERE e.idUsuarioGestor = @idUsuarioExterno
          AND r.estadoReserva IN (N'Pendiente', N'Aceptada');

        DECLARE @cuentasConSaldo INT =
        (
            SELECT COUNT(*)
            FROM (
                SELECT rolCuenta, moneda
                FROM dbo.MovimientoCuentaCorriente
                WHERE idUsuarioExterno = @idUsuarioExterno
                GROUP BY rolCuenta, moneda
                HAVING SUM(importe) <> 0
            ) saldos
        );

        DECLARE @espaciosPublicados INT =
        (
            SELECT COUNT(*) FROM dbo.EspacioArtistico
            WHERE idUsuarioGestor = @idUsuarioExterno AND publicado = 1 AND activo = 1
        );

        DECLARE @ticketsAbiertos INT =
        (
            SELECT COUNT(*) FROM dbo.Ticket
            WHERE idUsuarioExterno = @idUsuarioExterno AND estado <> N'Cerrado'
        );

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @estadoCuenta IS NULL
            SET @resultado = N'NO_EXISTE';
        ELSE IF @estadoCuenta <> N'Activa'
            SET @resultado = N'NO_ACTIVA';
        ELSE IF @reservasPendientes + @reservasAceptadas + @solicitudesRecibidas + @reservasRecibidasAceptadas + @cuentasConSaldo > 0
            SET @resultado = N'CONDICIONES_PENDIENTES';

        DECLARE @espaciosPausados INT = 0;

        IF @confirmar = 1 AND @resultado = N'OK'
        BEGIN
            UPDATE dbo.EspacioArtistico
            SET publicado = 0,
                estadoEspacio = N'Pausado',
                fechaUltimaModificacion = GETDATE()
            WHERE idUsuarioGestor = @idUsuarioExterno
              AND publicado = 1;

            SET @espaciosPausados = @@ROWCOUNT;

            UPDATE dbo.UsuarioExterno
            SET estadoCuenta = N'Inactiva',
                fechaBaja = GETDATE(),
                fechaUltimaModificacion = GETDATE()
            WHERE idUsuarioExterno = @idUsuarioExterno
              AND estadoCuenta = N'Activa';
        END

        COMMIT TRANSACTION;

        SELECT @resultado                  AS resultado,
               @reservasPendientes         AS reservasPendientes,
               @reservasAceptadas          AS reservasAceptadas,
               @pagosPendientes            AS pagosPendientes,
               @solicitudesRecibidas       AS solicitudesRecibidas,
               @reservasRecibidasAceptadas AS reservasRecibidasAceptadas,
               @cuentasConSaldo            AS cuentasConSaldo,
               @espaciosPublicados         AS espaciosPublicados,
               @ticketsAbiertos            AS ticketsAbiertos,
               @espaciosPausados           AS espaciosPausados;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
