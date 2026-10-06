-- =============================================================================
-- 53_PoliticaCancelacionYDetalleReserva.sql  (incremental, no destructivo)
--
-- CU-001-005 Gestionar reservas de espacios artísticos:
--
--  * Política de cancelación del documento (A14, A15 y A16), con los plazos y
--    porcentajes editables en Interno > Parámetros de la plataforma:
--      - 14 días o más antes del inicio: sin cargo;
--      - menos de 14 y al menos 7 días antes: cargo del 50 %;
--      - menos de 7 días antes: cargo del 100 %.
--    Reemplaza a los parámetros HorasCancelacionSinCargo y
--    PenalidadCancelacionPorcentaje (24 h / 10 %) del script 49.
--  * sp_Reserva_Cancelar: el cargo por cancelación de una reserva pagada se
--    reparte entre el gestor y StageUp (A15/A16 paso 7). El cliente recibe
--    NC por lo pagado y ND por el cargo; al gestor se le anula el ingreso de
--    la reserva y se le acredita el cargo, menos la comisión de StageUp (el
--    mismo porcentaje que se aplicó al pago).
--  * sp_Reserva_ObtenerDetalle: datos completos para la pantalla
--    "Detalle de reserva" (A8 y A9).
--  * sp_Reserva_Rechazar: rechaza solo si sigue pendiente e informa si lo
--    hizo, para avisar "la solicitud ya fue procesada" (A11).
--  * Texto de Términos y condiciones sobre cancelaciones.
--
-- Seguro de volver a ejecutar.
-- =============================================================================
IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ---------------------------------------------------------------------------
-- Parámetros de la política de cancelación
-- ---------------------------------------------------------------------------
MERGE dbo.ParametroPlataforma AS destino
USING (VALUES
    (N'DiasCancelacionSinCargo', N'14', N'Con al menos estos días de anticipación, el cliente cancela una reserva aceptada sin cargo.'),
    (N'DiasCancelacionCargoParcial', N'7', N'Con menos días que el plazo sin cargo y al menos estos días de anticipación, se cobra el cargo parcial. Con menos, el cargo total.'),
    (N'PorcentajeCargoParcial', N'50', N'Porcentaje del valor de la reserva que se cobra como cargo parcial por cancelación.'),
    (N'PorcentajeCargoTotal', N'100', N'Porcentaje del valor de la reserva que se cobra cuando se cancela con menos anticipación que el cargo parcial.')
) AS origen (clave, valor, descripcion)
ON destino.clave = origen.clave
WHEN NOT MATCHED THEN
    INSERT (clave, valor, descripcion) VALUES (origen.clave, origen.valor, origen.descripcion);
GO

DELETE FROM dbo.ParametroPlataforma
WHERE clave IN (N'HorasCancelacionSinCargo', N'PenalidadCancelacionPorcentaje');
GO

-- ---------------------------------------------------------------------------
-- Cancelación con reparto del cargo entre gestor y StageUp
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Reserva_Cancelar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Cancelar;
GO
CREATE PROCEDURE dbo.sp_Reserva_Cancelar
    @idReserva          INT,
    @comisionAplicada   BIT,
    @importeComision    DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @idNotaCredito INT = NULL;
    DECLARE @idNotaDebito INT = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @estadoPago NVARCHAR(20), @idCliente INT, @idGestor INT, @moneda NVARCHAR(3),
                @total DECIMAL(18,2), @nombreEspacio NVARCHAR(300);

        SELECT @estadoPago = r.estadoPago, @idCliente = r.idUsuarioExternoSolicitante, @idGestor = e.idUsuarioGestor,
               @moneda = ISNULL(r.moneda, N'ARS'), @total = r.importeEstimado, @nombreEspacio = e.nombreEspacio
        FROM dbo.Reserva r WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        WHERE r.idReserva = @idReserva
          AND r.estadoReserva IN (N'Pendiente', N'Aceptada');

        IF @idCliente IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT CAST(0 AS BIT) AS seCancelo, CAST(NULL AS INT) AS idNotaCredito, CAST(NULL AS INT) AS idNotaDebito;
            RETURN;
        END

        -- Una reserva que espera el pago se cancela sin cargo, aunque llegue
        -- un cargo: nunca se cobró nada.
        IF @estadoPago NOT IN (N'Pagado', N'NoRequerido')
        BEGIN
            SET @comisionAplicada = 0;
            SET @importeComision = NULL;
        END

        -- El cargo nunca supera el valor de la reserva.
        IF @comisionAplicada = 1 AND @total IS NOT NULL AND @importeComision > @total
            SET @importeComision = @total;

        UPDATE dbo.Reserva
        SET estadoReserva           = N'Cancelada',
            comisionAplicada        = @comisionAplicada,
            importeComision         = @importeComision,
            estadoPago              = CASE WHEN estadoPago = N'Pagado' THEN N'Devuelto' ELSE estadoPago END,
            fechaCancelacion        = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva;

        IF @estadoPago = N'Pagado' AND @total > 0
        BEGIN
            DECLARE @textoReserva NVARCHAR(400) = N'reserva N° ' + CAST(@idReserva AS NVARCHAR(10)) + N' (' + @nombreEspacio + N')';

            DECLARE @idPago INT, @comisionPlataforma DECIMAL(18,2), @porcentajePlataforma DECIMAL(5,2);
            SELECT TOP 1 @idPago = idPago,
                         @comisionPlataforma = ISNULL(importeComisionPlataforma, 0),
                         @porcentajePlataforma = ISNULL(porcentajeComisionPlataforma, 0)
            FROM dbo.Pago
            WHERE idReserva = @idReserva AND estado = N'Aprobado'
            ORDER BY idPago DESC;

            -- Cliente: vuelve todo lo pagado como saldo a favor...
            INSERT INTO dbo.Comprobante (tipo, idUsuarioExterno, rolCuenta, idReserva, importe, moneda, origen, motivo)
            VALUES (N'NC', @idCliente, N'Cliente', @idReserva, @total, @moneda, N'Cancelacion',
                    N'Cancelación de la ' + @textoReserva + N'. El importe pagado queda como saldo a favor.');
            SET @idNotaCredito = CAST(SCOPE_IDENTITY() AS INT);

            INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idComprobante)
            VALUES (@idCliente, N'Cliente', N'NotaCredito', @total, @moneda,
                    N'Nota de crédito por cancelación de la ' + @textoReserva, @idReserva, @idNotaCredito);

            -- ...y se le debita el cargo por cancelación, si corresponde.
            IF @comisionAplicada = 1 AND ISNULL(@importeComision, 0) > 0
            BEGIN
                INSERT INTO dbo.Comprobante (tipo, idUsuarioExterno, rolCuenta, idReserva, importe, moneda, origen, motivo)
                VALUES (N'ND', @idCliente, N'Cliente', @idReserva, @importeComision, @moneda, N'PenalidadCancelacion',
                        N'Cargo por cancelar la ' + @textoReserva + N' fuera del plazo sin cargo.');
                SET @idNotaDebito = CAST(SCOPE_IDENTITY() AS INT);

                INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idComprobante)
                VALUES (@idCliente, N'Cliente', N'NotaDebito', -@importeComision, @moneda,
                        N'Nota de débito por el cargo de cancelación de la ' + @textoReserva, @idReserva, @idNotaDebito);
            END

            -- Gestor: se revierte lo que se le acreditó con el pago...
            INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
            VALUES (@idGestor, N'Gestor', N'AnulacionIngreso', -@total, @moneda,
                    N'El cliente canceló la ' + @textoReserva, @idReserva, @idPago);

            IF @comisionPlataforma > 0
            BEGIN
                INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
                VALUES (@idGestor, N'Gestor', N'AnulacionComision', @comisionPlataforma, @moneda,
                        N'Se devuelve la comisión de StageUp de la ' + @textoReserva, @idReserva, @idPago);
            END

            -- ...y se le acredita el cargo por cancelación, menos la comisión
            -- de StageUp (mismo porcentaje que se aplicó al pago).
            IF @comisionAplicada = 1 AND ISNULL(@importeComision, 0) > 0
            BEGIN
                DECLARE @comisionSobreCargo DECIMAL(18,2) = ROUND(@importeComision * ISNULL(@porcentajePlataforma, 0) / 100, 2);

                INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
                VALUES (@idGestor, N'Gestor', N'IngresoReserva', @importeComision, @moneda,
                        N'Cargo por cancelación de la ' + @textoReserva, @idReserva, @idPago);

                IF @comisionSobreCargo > 0
                BEGIN
                    INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
                    VALUES (@idGestor, N'Gestor', N'ComisionPlataforma', -@comisionSobreCargo, @moneda,
                            N'Comisión de StageUp sobre el cargo por cancelación de la ' + @textoReserva, @idReserva, @idPago);
                END
            END
        END

        COMMIT TRANSACTION;

        SELECT CAST(1 AS BIT) AS seCancelo, @idNotaCredito AS idNotaCredito, @idNotaDebito AS idNotaDebito;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ---------------------------------------------------------------------------
-- Detalle de una reserva (A8 / A9)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Reserva_ObtenerDetalle', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ObtenerDetalle;
GO
CREATE PROCEDURE dbo.sp_Reserva_ObtenerDetalle
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion, r.recordatorioEnviado,
        r.estadoPago, r.fechaLimitePago, r.fechaPago,
        e.nombreEspacio, e.idUsuarioGestor, e.tipoEspacio,
        g.nombre + N' ' + g.apellido AS nombreGestor,
        s.nombre + N' ' + s.apellido AS nombreSolicitante,
        s.correoElectronico AS correoSolicitante,
        f.provincia AS provinciaEspacio, f.ciudad AS ciudadEspacio, f.direccion AS direccionEspacio,
        f.capacidadMaxima, f.tipoPiso, f.detalleEquipamiento,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Calificacion c
                               WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'Espacio' AND c.activo = 1)
                  THEN 1 ELSE 0 END AS BIT) AS calificacionEspacioRealizada,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Calificacion c
                               WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'UsuarioSolicitante' AND c.activo = 1)
                  THEN 1 ELSE 0 END AS BIT) AS calificacionSolicitanteRealizada
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    INNER JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = r.idUsuarioExternoSolicitante
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE r.idReserva = @idReserva;
END
GO

-- ---------------------------------------------------------------------------
-- Rechazo (A10 / A11)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Reserva_Rechazar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Rechazar;
GO
CREATE PROCEDURE dbo.sp_Reserva_Rechazar
    @idReserva              INT,
    @comentarioResolucion   NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Reserva
    SET estadoReserva           = N'Rechazada',
        comentarioResolucion    = @comentarioResolucion,
        fechaResolucion         = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    WHERE idReserva = @idReserva
      AND estadoReserva = N'Pendiente';

    SELECT @@ROWCOUNT AS filasAfectadas;
END
GO

-- ---------------------------------------------------------------------------
-- Términos y condiciones: texto de cancelaciones (si se cargó en el
-- diccionario de traducciones, se actualiza también ahí).
-- ---------------------------------------------------------------------------
DECLARE @textoEs NVARCHAR(2000) = N'El valor estimado se calcula a partir del precio por hora publicado y la duración solicitada. Una solicitud pendiente se puede cancelar sin cargo. Una reserva aceptada se cancela sin cargo con al menos 14 días de anticipación; con menos de 14 y al menos 7 días se cobra el 50 % de su valor, y con menos de 7 días, el 100 %. Lo pagado vuelve como saldo a favor, descontando el cargo que corresponda.';
DECLARE @textoEn NVARCHAR(2000) = N'The estimated price is calculated from the published hourly rate and the requested duration. A pending request can be cancelled free of charge. An accepted booking can be cancelled free of charge at least 14 days in advance; with less than 14 and at least 7 days, 50% of its value is charged, and with less than 7 days, 100%. Payments are returned as account credit, minus any applicable charge.';

UPDATE dbo.EtiquetaTraduccion
SET textoPredeterminado = @textoEs, fechaUltimaModificacion = GETDATE()
WHERE claveEtiqueta = N'Terms_PreciosTexto';

UPDATE t
SET t.textoTraducido = CASE WHEN i.codigoIdioma LIKE N'en%' THEN @textoEn ELSE @textoEs END,
    t.fechaUltimaModificacion = GETDATE()
FROM dbo.Traduccion t
INNER JOIN dbo.EtiquetaTraduccion et ON et.idEtiquetaTraduccion = t.idEtiquetaTraduccion
INNER JOIN dbo.Idioma i ON i.idIdioma = t.idIdioma
WHERE et.claveEtiqueta = N'Terms_PreciosTexto'
  AND (i.codigoIdioma LIKE N'es%' OR i.codigoIdioma LIKE N'en%');
GO
