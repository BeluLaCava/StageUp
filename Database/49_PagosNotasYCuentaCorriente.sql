IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ---------------------------------------------------------------------------
-- Reserva: estado del pago
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Reserva', N'estadoPago') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva
        ADD estadoPago NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Reserva_estadoPago DEFAULT (N'NoRequerido') WITH VALUES;
END
GO

IF COL_LENGTH(N'dbo.Reserva', N'fechaLimitePago') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD fechaLimitePago DATETIME NULL;
END
GO

IF COL_LENGTH(N'dbo.Reserva', N'fechaPago') IS NULL
BEGIN
    ALTER TABLE dbo.Reserva ADD fechaPago DATETIME NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Reserva_estadoPago')
BEGIN
    ALTER TABLE dbo.Reserva ADD CONSTRAINT CK_Reserva_estadoPago
        CHECK (estadoPago IN (N'NoRequerido', N'Pendiente', N'Pagado', N'Devuelto', N'Vencido'));
END
GO

-- ---------------------------------------------------------------------------
-- Parámetros de la plataforma (los edita el administrador)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ParametroPlataforma', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParametroPlataforma
    (
        clave                    NVARCHAR(100)  NOT NULL CONSTRAINT PK_ParametroPlataforma PRIMARY KEY,
        valor                    NVARCHAR(200)  NOT NULL,
        descripcion              NVARCHAR(500)  NULL,
        fechaUltimaModificacion  DATETIME       NOT NULL CONSTRAINT DF_ParametroPlataforma_fecha DEFAULT (GETDATE())
    );
END
GO

MERGE dbo.ParametroPlataforma AS destino
USING (VALUES
    (N'ComisionPlataformaPorcentaje', N'10', N'Porcentaje que StageUp retiene de cada reserva pagada, antes de acreditarle el resto al gestor.'),
    (N'HorasLimitePago', N'48', N'Horas que tiene el cliente para pagar una reserva aceptada. Si la reserva empieza antes, el plazo es el inicio de la reserva.'),
    (N'PenalidadCancelacionPorcentaje', N'10', N'Porcentaje del importe que se cobra como penalidad si el cliente cancela con poca anticipación.'),
    (N'HorasCancelacionSinCargo', N'24', N'Con al menos estas horas de anticipación, el cliente cancela sin penalidad.')
) AS origen (clave, valor, descripcion)
ON destino.clave = origen.clave
WHEN NOT MATCHED THEN
    INSERT (clave, valor, descripcion) VALUES (origen.clave, origen.valor, origen.descripcion);
GO

-- ---------------------------------------------------------------------------
-- Pagos (intentos aprobados y rechazados). Nunca se guarda el número
-- completo de la tarjeta ni el código de seguridad: solo marca y últimos 4.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Pago', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pago
    (
        idPago                          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pago PRIMARY KEY,
        idUsuarioExterno                INT               NOT NULL,
        idReserva                       INT               NULL,
        concepto                        NVARCHAR(20)      NOT NULL,
        moneda                          NVARCHAR(3)       NOT NULL,
        importeTotal                    DECIMAL(18,2)     NOT NULL,
        importeTarjeta                  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Pago_importeTarjeta DEFAULT (0),
        importeSaldo                    DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Pago_importeSaldo DEFAULT (0),
        estado                          NVARCHAR(20)      NOT NULL,
        marcaTarjeta                    NVARCHAR(30)      NULL,
        ultimosDigitos                  NVARCHAR(4)       NULL,
        titularTarjeta                  NVARCHAR(150)     NULL,
        codigoAutorizacion              NVARCHAR(20)      NULL,
        motivoRechazo                   NVARCHAR(300)     NULL,
        porcentajeComisionPlataforma    DECIMAL(5,2)      NULL,
        importeComisionPlataforma       DECIMAL(18,2)     NULL,
        fechaPago                       DATETIME          NOT NULL CONSTRAINT DF_Pago_fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_Pago_UsuarioExterno FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_Pago_Reserva FOREIGN KEY (idReserva) REFERENCES dbo.Reserva (idReserva),
        CONSTRAINT CK_Pago_concepto CHECK (concepto IN (N'Reserva', N'Deuda')),
        CONSTRAINT CK_Pago_estado CHECK (estado IN (N'Aprobado', N'Rechazado')),
        CONSTRAINT CK_Pago_moneda CHECK (moneda IN (N'ARS', N'USD')),
        CONSTRAINT CK_Pago_importes CHECK (importeTotal > 0 AND importeTarjeta >= 0 AND importeSaldo >= 0)
    );

    CREATE INDEX IX_Pago_Usuario ON dbo.Pago (idUsuarioExterno, fechaPago DESC);
    CREATE INDEX IX_Pago_Reserva ON dbo.Pago (idReserva);
END
GO

-- ---------------------------------------------------------------------------
-- Comprobantes: notas de crédito (NC) y de débito (ND)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Comprobante', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Comprobante
    (
        idComprobante                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comprobante PRIMARY KEY,
        tipo                         NVARCHAR(2)       NOT NULL,
        numero                       AS (tipo + N'-' + RIGHT(N'00000000' + CAST(idComprobante AS NVARCHAR(10)), 8)),
        idUsuarioExterno             INT               NOT NULL,
        rolCuenta                    NVARCHAR(10)      NOT NULL,
        idReserva                    INT               NULL,
        importe                      DECIMAL(18,2)     NOT NULL,
        moneda                       NVARCHAR(3)       NOT NULL,
        origen                       NVARCHAR(30)      NOT NULL,
        motivo                       NVARCHAR(500)     NOT NULL,
        estado                       NVARCHAR(10)      NOT NULL CONSTRAINT DF_Comprobante_estado DEFAULT (N'Emitido'),
        idUsuarioInternoEmisor       INT               NULL,
        fechaEmision                 DATETIME          NOT NULL CONSTRAINT DF_Comprobante_fecha DEFAULT (GETDATE()),
        fechaAnulacion               DATETIME          NULL,
        motivoAnulacion              NVARCHAR(500)     NULL,
        CONSTRAINT FK_Comprobante_UsuarioExterno FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_Comprobante_Reserva FOREIGN KEY (idReserva) REFERENCES dbo.Reserva (idReserva),
        CONSTRAINT FK_Comprobante_UsuarioInterno FOREIGN KEY (idUsuarioInternoEmisor) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_Comprobante_tipo CHECK (tipo IN (N'NC', N'ND')),
        CONSTRAINT CK_Comprobante_rol CHECK (rolCuenta IN (N'Cliente', N'Gestor')),
        CONSTRAINT CK_Comprobante_origen CHECK (origen IN (N'Cancelacion', N'PenalidadCancelacion', N'AjusteManual')),
        CONSTRAINT CK_Comprobante_estado CHECK (estado IN (N'Emitido', N'Anulado')),
        CONSTRAINT CK_Comprobante_moneda CHECK (moneda IN (N'ARS', N'USD')),
        CONSTRAINT CK_Comprobante_importe CHECK (importe > 0)
    );

    CREATE INDEX IX_Comprobante_Usuario ON dbo.Comprobante (idUsuarioExterno, fechaEmision DESC);
END
GO

-- ---------------------------------------------------------------------------
-- Movimientos de cuenta corriente. importe con signo: + a favor del usuario,
-- - en contra.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.MovimientoCuentaCorriente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientoCuentaCorriente
    (
        idMovimiento                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientoCuentaCorriente PRIMARY KEY,
        idUsuarioExterno             INT               NOT NULL,
        rolCuenta                    NVARCHAR(10)      NOT NULL,
        tipoMovimiento               NVARCHAR(30)      NOT NULL,
        importe                      DECIMAL(18,2)     NOT NULL,
        moneda                       NVARCHAR(3)       NOT NULL,
        descripcion                  NVARCHAR(500)     NOT NULL,
        idReserva                    INT               NULL,
        idPago                       INT               NULL,
        idComprobante                INT               NULL,
        idUsuarioInternoResponsable  INT               NULL,
        fechaMovimiento              DATETIME          NOT NULL CONSTRAINT DF_MovimientoCC_fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_MovimientoCC_UsuarioExterno FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_MovimientoCC_Reserva FOREIGN KEY (idReserva) REFERENCES dbo.Reserva (idReserva),
        CONSTRAINT FK_MovimientoCC_Pago FOREIGN KEY (idPago) REFERENCES dbo.Pago (idPago),
        CONSTRAINT FK_MovimientoCC_Comprobante FOREIGN KEY (idComprobante) REFERENCES dbo.Comprobante (idComprobante),
        CONSTRAINT FK_MovimientoCC_UsuarioInterno FOREIGN KEY (idUsuarioInternoResponsable) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_MovimientoCC_rol CHECK (rolCuenta IN (N'Cliente', N'Gestor')),
        CONSTRAINT CK_MovimientoCC_moneda CHECK (moneda IN (N'ARS', N'USD')),
        CONSTRAINT CK_MovimientoCC_tipo CHECK (tipoMovimiento IN (
            N'CargoReserva', N'PagoTarjeta', N'NotaCredito', N'NotaDebito', N'AnulacionComprobante',
            N'IngresoReserva', N'ComisionPlataforma', N'AnulacionIngreso', N'AnulacionComision', N'Liquidacion')),
        CONSTRAINT CK_MovimientoCC_importe CHECK (importe <> 0)
    );

    CREATE INDEX IX_MovimientoCC_Cuenta ON dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, moneda, fechaMovimiento);
END
GO

-- =============================================================================
-- Parámetros
-- =============================================================================
IF OBJECT_ID('dbo.sp_ParametroPlataforma_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ParametroPlataforma_Listar;
GO
CREATE PROCEDURE dbo.sp_ParametroPlataforma_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT clave, valor, descripcion, fechaUltimaModificacion
    FROM dbo.ParametroPlataforma
    ORDER BY clave;
END
GO

IF OBJECT_ID('dbo.sp_ParametroPlataforma_Actualizar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ParametroPlataforma_Actualizar;
GO
CREATE PROCEDURE dbo.sp_ParametroPlataforma_Actualizar
    @clave  NVARCHAR(100),
    @valor  NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ParametroPlataforma
    SET valor = @valor, fechaUltimaModificacion = GETDATE()
    WHERE clave = @clave;

    SELECT CAST(@@ROWCOUNT AS INT) AS filasAfectadas;
END
GO

-- =============================================================================
-- Reservas: se agregan los datos del pago a los listados y se ajustan
-- aceptación, finalización, recordatorio y cancelación.
-- =============================================================================
IF OBJECT_ID(N'dbo.sp_Reserva_ListarPorSolicitante', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPorSolicitante;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPorSolicitante
    @idUsuarioExternoSolicitante INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        r.estadoPago, r.fechaLimitePago, r.fechaPago,
        e.nombreEspacio, e.idUsuarioGestor,
        g.nombre + N' ' + g.apellido AS nombreGestor,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'Espacio'
        ) THEN 1 ELSE 0 END) AS calificacionEspacioRealizada,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'UsuarioSolicitante'
        ) THEN 1 ELSE 0 END) AS calificacionSolicitanteRealizada
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE r.idUsuarioExternoSolicitante = @idUsuarioExternoSolicitante
    ORDER BY r.fechaCreacion DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_Reserva_ListarPorGestor', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPorGestor;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPorGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        r.estadoPago, r.fechaLimitePago, r.fechaPago,
        e.nombreEspacio, e.idUsuarioGestor,
        LTRIM(RTRIM(u.nombre + N' ' + u.apellido)) AS nombreSolicitante,
        u.correoElectronico AS correoSolicitante,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'Espacio'
        ) THEN 1 ELSE 0 END) AS calificacionEspacioRealizada,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'UsuarioSolicitante'
        ) THEN 1 ELSE 0 END) AS calificacionSolicitanteRealizada,
        reputacion.promedio AS promedioCalificacionSolicitante,
        COALESCE(reputacion.cantidadCalificaciones, 0) AS cantidadCalificacionesSolicitante
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = r.idUsuarioExternoSolicitante
    OUTER APPLY
    (
        SELECT
            CONVERT(DECIMAL(4,2), AVG(CONVERT(DECIMAL(10,2), c.puntaje))) AS promedio,
            COUNT(*) AS cantidadCalificaciones
        FROM dbo.Calificacion c
        INNER JOIN dbo.Reserva rr ON rr.idReserva = c.idReserva
        WHERE rr.idUsuarioExternoSolicitante = r.idUsuarioExternoSolicitante
          AND c.tipoCalificacion = N'UsuarioSolicitante'
          AND c.activo = 1
    ) reputacion
    WHERE e.idUsuarioGestor = @idUsuarioGestor
    ORDER BY
        CASE WHEN r.estadoReserva = N'Pendiente' THEN 0 WHEN r.estadoReserva = N'Aceptada' THEN 1 ELSE 2 END,
        r.fechaCreacion DESC;
END
GO

IF OBJECT_ID(N'dbo.sp_Reserva_ObtenerPorId', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Reserva_ObtenerPorId
    @idReserva INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        r.estadoPago, r.fechaLimitePago, r.fechaPago,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.idReserva = @idReserva;
END
GO

-- Al aceptar, si la reserva tiene importe, queda esperando el pago hasta
-- @fechaLimitePago (lo calcula BLL_Reserva con el parámetro HorasLimitePago:
-- ese plazo desde ahora, o el inicio de la reserva si es antes).
IF OBJECT_ID(N'dbo.sp_Reserva_AceptarSiDisponible', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_AceptarSiDisponible;
GO
CREATE PROCEDURE dbo.sp_Reserva_AceptarSiDisponible
    @idReserva              INT,
    @comentarioResolucion   NVARCHAR(1000) = NULL,
    @fechaLimitePago        DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @ahora DATETIME = GETDATE();

        UPDATE r
        SET estadoReserva           = N'Aceptada',
            comentarioResolucion    = @comentarioResolucion,
            fechaResolucion         = @ahora,
            fechaUltimaModificacion = @ahora,
            estadoPago              = CASE WHEN ISNULL(r.importeEstimado, 0) > 0 THEN N'Pendiente' ELSE N'NoRequerido' END,
            fechaLimitePago         = CASE WHEN ISNULL(r.importeEstimado, 0) > 0
                                           THEN ISNULL(@fechaLimitePago, DATEADD(HOUR, 48, @ahora)) END
        FROM dbo.Reserva r
        WHERE r.idReserva = @idReserva
          AND r.estadoReserva = N'Pendiente'
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.Reserva otra
              WHERE otra.idEspacioArtistico = r.idEspacioArtistico
                AND otra.idReserva <> r.idReserva
                AND otra.fechaSolicitada = r.fechaSolicitada
                AND otra.estadoReserva = N'Aceptada'
                AND r.minutoDesde IS NOT NULL
                AND r.minutoHasta IS NOT NULL
                AND otra.minutoDesde IS NOT NULL
                AND otra.minutoHasta IS NOT NULL
                AND r.minutoDesde < otra.minutoHasta
                AND otra.minutoDesde < r.minutoHasta
          );

        SELECT CAST(@@ROWCOUNT AS BIT) AS seAcepto;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- Solo finalizan las reservas pagadas (o anteriores al módulo de pagos). Las
-- que siguen esperando el pago las cancela sp_Reserva_VencerPagosPendientes.
IF OBJECT_ID(N'dbo.sp_Reserva_FinalizarVencidas', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_FinalizarVencidas;
GO
CREATE PROCEDURE dbo.sp_Reserva_FinalizarVencidas
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Reserva
    SET estadoReserva = N'Finalizada',
        fechaFinalizacion = COALESCE(fechaFinalizacion, GETDATE()),
        fechaUltimaModificacion = GETDATE()
    WHERE estadoReserva = N'Aceptada'
      AND estadoPago IN (N'Pagado', N'NoRequerido')
      AND DATEADD(MINUTE, COALESCE(CONVERT(INT, minutoHasta), 1440), CONVERT(DATETIME, fechaSolicitada)) <= GETDATE();
END
GO

-- Cancela (sin cargo) las reservas aceptadas cuyo plazo de pago venció y las
-- devuelve, para avisarle al cliente y al gestor.
IF OBJECT_ID(N'dbo.sp_Reserva_VencerPagosPendientes', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_VencerPagosPendientes;
GO
CREATE PROCEDURE dbo.sp_Reserva_VencerPagosPendientes
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @vencidas TABLE (idReserva INT PRIMARY KEY);

    UPDATE dbo.Reserva
    SET estadoReserva = N'Cancelada',
        estadoPago = N'Vencido',
        comisionAplicada = 0,
        importeComision = NULL,
        fechaCancelacion = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    OUTPUT inserted.idReserva INTO @vencidas (idReserva)
    WHERE estadoReserva = N'Aceptada'
      AND estadoPago = N'Pendiente'
      AND fechaLimitePago IS NOT NULL
      AND fechaLimitePago <= GETDATE();

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        r.estadoPago, r.fechaLimitePago, r.fechaPago,
        e.nombreEspacio, e.idUsuarioGestor
    FROM @vencidas v
    INNER JOIN dbo.Reserva r ON r.idReserva = v.idReserva
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico;
END
GO

-- El recordatorio de 24 hs es para reservas confirmadas: no se manda si la
-- reserva todavía está esperando el pago.
IF OBJECT_ID(N'dbo.sp_Reserva_ListarPendientesDeRecordatorio', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPendientesDeRecordatorio;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPendientesDeRecordatorio
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ahora DATETIME = GETDATE();
    DECLARE @limite DATETIME = DATEADD(HOUR, 24, @ahora);

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion,
        r.minutoDesde, r.minutoHasta, r.recordatorioEnviado,
        r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.estadoPago, r.fechaLimitePago, r.fechaPago,
        e.nombreEspacio, e.idUsuarioGestor
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.estadoReserva = N'Aceptada'
      AND r.estadoPago IN (N'Pagado', N'NoRequerido')
      AND r.recordatorioEnviado = 0
      AND DATEADD(MINUTE, ISNULL(r.minutoDesde, 0), CAST(r.fechaSolicitada AS DATETIME)) BETWEEN @ahora AND @limite;
END
GO

-- Calificar una reserva la finaliza si ya pasó su horario (script 21). Con el
-- módulo de pagos, solo si está pagada: una reserva impaga nunca se finaliza.
IF OBJECT_ID(N'dbo.sp_Calificacion_Insertar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_Insertar;
GO
CREATE PROCEDURE dbo.sp_Calificacion_Insertar
    @idReserva          INT,
    @idUsuarioAutor     INT,
    @tipoCalificacion   NVARCHAR(30),
    @puntaje            TINYINT,
    @comentario         NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Reserva
        SET estadoReserva = N'Finalizada',
            fechaFinalizacion = COALESCE(fechaFinalizacion, GETDATE()),
            fechaUltimaModificacion = GETDATE()
        WHERE idReserva = @idReserva
          AND estadoReserva = N'Aceptada'
          AND estadoPago IN (N'Pagado', N'NoRequerido')
          AND DATEADD(MINUTE, COALESCE(CONVERT(INT, minutoHasta), 1440), CONVERT(DATETIME, fechaSolicitada)) <= GETDATE();

        DECLARE @estadoReserva NVARCHAR(50);
        DECLARE @idSolicitante INT;
        DECLARE @idGestor INT;

        SELECT
            @estadoReserva = r.estadoReserva,
            @idSolicitante = r.idUsuarioExternoSolicitante,
            @idGestor = e.idUsuarioGestor
        FROM dbo.Reserva r WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        WHERE r.idReserva = @idReserva;

        IF @estadoReserva IS NULL
            RAISERROR(N'No se encontró la reserva indicada.', 16, 1);

        IF @estadoReserva <> N'Finalizada'
            RAISERROR(N'La reserva todavía no está finalizada.', 16, 1);

        IF @tipoCalificacion NOT IN (N'Espacio', N'UsuarioSolicitante')
            RAISERROR(N'El tipo de calificación no es válido.', 16, 1);

        IF @puntaje < 1 OR @puntaje > 5
            RAISERROR(N'El puntaje debe estar entre 1 y 5.', 16, 1);

        IF @comentario IS NULL OR LEN(LTRIM(RTRIM(@comentario))) = 0 OR LEN(@comentario) > 1000
            RAISERROR(N'El comentario es obligatorio y no puede superar los 1000 caracteres.', 16, 1);

        IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE idUsuarioExterno = @idUsuarioAutor AND activo = 1)
            RAISERROR(N'El autor de la calificación no tiene una cuenta activa.', 16, 1);

        IF @tipoCalificacion = N'Espacio' AND @idUsuarioAutor <> @idSolicitante
            RAISERROR(N'Solo el solicitante puede calificar el espacio reservado.', 16, 1);

        IF @tipoCalificacion = N'UsuarioSolicitante' AND @idUsuarioAutor <> @idGestor
            RAISERROR(N'Solo el gestor del espacio puede calificar al solicitante.', 16, 1);

        IF EXISTS (
            SELECT 1
            FROM dbo.Calificacion WITH (UPDLOCK, HOLDLOCK)
            WHERE idReserva = @idReserva
              AND tipoCalificacion = @tipoCalificacion
        )
            RAISERROR(N'Esta calificación ya fue registrada.', 16, 1);

        INSERT INTO dbo.Calificacion
            (idReserva, idUsuarioAutor, tipoCalificacion, puntaje, comentario, fechaAlta, activo)
        VALUES
            (@idReserva, @idUsuarioAutor, @tipoCalificacion, @puntaje, LTRIM(RTRIM(@comentario)), GETDATE(), 1);

        DECLARE @idCalificacion INT = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;
        SELECT @idCalificacion AS idCalificacion;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @mensaje NVARCHAR(2048) = ERROR_MESSAGE();
        RAISERROR(@mensaje, 16, 1);
    END CATCH
END
GO

-- Cancelación por el cliente. Si la reserva estaba pagada, en la misma
-- transacción:
--   * cliente: NC por el total (saldo a favor) y, si corresponde, ND por la
--     penalidad;
--   * gestor: se anula el ingreso y la comisión de StageUp de ese pago.
-- Devuelve si se canceló y los comprobantes emitidos.
IF OBJECT_ID(N'dbo.sp_Reserva_Cancelar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_Cancelar;
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
        -- una comisión: nunca se cobró nada.
        IF @estadoPago NOT IN (N'Pagado', N'NoRequerido')
        BEGIN
            SET @comisionAplicada = 0;
            SET @importeComision = NULL;
        END

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

            INSERT INTO dbo.Comprobante (tipo, idUsuarioExterno, rolCuenta, idReserva, importe, moneda, origen, motivo)
            VALUES (N'NC', @idCliente, N'Cliente', @idReserva, @total, @moneda, N'Cancelacion',
                    N'Cancelación de la ' + @textoReserva + N'. El importe pagado queda como saldo a favor.');
            SET @idNotaCredito = CAST(SCOPE_IDENTITY() AS INT);

            INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idComprobante)
            VALUES (@idCliente, N'Cliente', N'NotaCredito', @total, @moneda,
                    N'Nota de crédito por cancelación de la ' + @textoReserva, @idReserva, @idNotaCredito);

            IF @comisionAplicada = 1 AND ISNULL(@importeComision, 0) > 0
            BEGIN
                INSERT INTO dbo.Comprobante (tipo, idUsuarioExterno, rolCuenta, idReserva, importe, moneda, origen, motivo)
                VALUES (N'ND', @idCliente, N'Cliente', @idReserva, @importeComision, @moneda, N'PenalidadCancelacion',
                        N'Penalidad por cancelar la ' + @textoReserva + N' con poca anticipación.');
                SET @idNotaDebito = CAST(SCOPE_IDENTITY() AS INT);

                INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idComprobante)
                VALUES (@idCliente, N'Cliente', N'NotaDebito', -@importeComision, @moneda,
                        N'Nota de débito por cancelación tardía de la ' + @textoReserva, @idReserva, @idNotaDebito);
            END

            -- Gestor: se revierte lo que se le acreditó con el pago.
            DECLARE @idPago INT, @comisionPlataforma DECIMAL(18,2);
            SELECT TOP 1 @idPago = idPago, @comisionPlataforma = ISNULL(importeComisionPlataforma, 0)
            FROM dbo.Pago
            WHERE idReserva = @idReserva AND estado = N'Aprobado'
            ORDER BY idPago DESC;

            INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
            VALUES (@idGestor, N'Gestor', N'AnulacionIngreso', -@total, @moneda,
                    N'El cliente canceló la ' + @textoReserva, @idReserva, @idPago);

            IF @comisionPlataforma > 0
            BEGIN
                INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago)
                VALUES (@idGestor, N'Gestor', N'AnulacionComision', @comisionPlataforma, @moneda,
                        N'Se devuelve la comisión de StageUp de la ' + @textoReserva, @idReserva, @idPago);
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

-- =============================================================================
-- Pagos
-- =============================================================================

-- Pago aprobado de una reserva (tarjeta, saldo a favor o ambos). La
-- autorización de la tarjeta ya la hizo la pasarela (simulada) antes de
-- llamar a este SP. Todo en una transacción, con un bloqueo por cuenta para
-- que dos pagos simultáneos no usen el mismo saldo.
-- resultado: OK | NO_ENCONTRADA | NO_PENDIENTE | VENCIDA | IMPORTE_INVALIDO | SALDO_INSUFICIENTE
IF OBJECT_ID('dbo.sp_Pago_RegistrarReserva', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pago_RegistrarReserva;
GO
CREATE PROCEDURE dbo.sp_Pago_RegistrarReserva
    @idReserva                      INT,
    @idUsuarioExterno               INT,
    @importeTarjeta                 DECIMAL(18,2),
    @importeSaldo                   DECIMAL(18,2),
    @marcaTarjeta                   NVARCHAR(30)  = NULL,
    @ultimosDigitos                 NVARCHAR(4)   = NULL,
    @titularTarjeta                 NVARCHAR(150) = NULL,
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
             marcaTarjeta, ultimosDigitos, titularTarjeta, codigoAutorizacion,
             porcentajeComisionPlataforma, importeComisionPlataforma)
        VALUES
            (@idUsuarioExterno, @idReserva, N'Reserva', @moneda, @total, @importeTarjeta, @importeSaldo, N'Aprobado',
             CASE WHEN @importeTarjeta > 0 THEN @marcaTarjeta END,
             CASE WHEN @importeTarjeta > 0 THEN @ultimosDigitos END,
             CASE WHEN @importeTarjeta > 0 THEN @titularTarjeta END,
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

-- Intento rechazado por la pasarela: queda registrado, sin movimientos.
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
    @titularTarjeta     NVARCHAR(150) = NULL,
    @motivoRechazo      NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Pago
        (idUsuarioExterno, idReserva, concepto, moneda, importeTotal, importeTarjeta, importeSaldo, estado,
         marcaTarjeta, ultimosDigitos, titularTarjeta, motivoRechazo)
    VALUES
        (@idUsuarioExterno, @idReserva, @concepto, @moneda, @importeTotal, @importeTarjeta, 0, N'Rechazado',
         @marcaTarjeta, @ultimosDigitos, @titularTarjeta, @motivoRechazo);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idPago;
END
GO

-- Pago con tarjeta de un saldo deudor (por ejemplo, una nota de débito).
-- resultado: OK | SIN_DEUDA | IMPORTE_INVALIDO
IF OBJECT_ID('dbo.sp_CuentaCorriente_PagarDeuda', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CuentaCorriente_PagarDeuda;
GO
CREATE PROCEDURE dbo.sp_CuentaCorriente_PagarDeuda
    @idUsuarioExterno   INT,
    @moneda             NVARCHAR(3),
    @importe            DECIMAL(18,2),
    @marcaTarjeta       NVARCHAR(30),
    @ultimosDigitos     NVARCHAR(4),
    @titularTarjeta     NVARCHAR(150),
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
             marcaTarjeta, ultimosDigitos, titularTarjeta, codigoAutorizacion)
        VALUES
            (@idUsuarioExterno, NULL, N'Deuda', @moneda, @importe, @importe, 0, N'Aprobado',
             @marcaTarjeta, @ultimosDigitos, @titularTarjeta, @codigoAutorizacion);

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
        p.estado, p.marcaTarjeta, p.ultimosDigitos, p.titularTarjeta, p.codigoAutorizacion, p.motivoRechazo,
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

-- =============================================================================
-- Cuenta corriente
-- =============================================================================
IF OBJECT_ID('dbo.sp_CuentaCorriente_ObtenerSaldos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CuentaCorriente_ObtenerSaldos;
GO
CREATE PROCEDURE dbo.sp_CuentaCorriente_ObtenerSaldos
    @idUsuarioExterno INT,
    @rolCuenta        NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT moneda, SUM(importe) AS saldo, COUNT(*) AS cantidadMovimientos, MAX(fechaMovimiento) AS ultimoMovimiento
    FROM dbo.MovimientoCuentaCorriente
    WHERE idUsuarioExterno = @idUsuarioExterno AND rolCuenta = @rolCuenta
    GROUP BY moneda
    ORDER BY moneda;
END
GO

-- Movimientos con saldo acumulado (calculado sobre toda la historia de la
-- cuenta, antes de aplicar los filtros, para que el saldo de cada fila sea
-- el real).
IF OBJECT_ID('dbo.sp_CuentaCorriente_ListarMovimientos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CuentaCorriente_ListarMovimientos;
GO
CREATE PROCEDURE dbo.sp_CuentaCorriente_ListarMovimientos
    @idUsuarioExterno INT,
    @rolCuenta        NVARCHAR(10),
    @moneda           NVARCHAR(3) = NULL,
    @desde            DATE = NULL,
    @hasta            DATE = NULL,
    @tipoMovimiento   NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH movimientos AS
    (
        SELECT m.idMovimiento, m.idUsuarioExterno, m.rolCuenta, m.tipoMovimiento, m.importe, m.moneda, m.descripcion,
               m.idReserva, m.idPago, m.idComprobante, m.fechaMovimiento,
               SUM(m.importe) OVER (PARTITION BY m.moneda ORDER BY m.fechaMovimiento, m.idMovimiento
                                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS saldoAcumulado
        FROM dbo.MovimientoCuentaCorriente m
        WHERE m.idUsuarioExterno = @idUsuarioExterno AND m.rolCuenta = @rolCuenta
    )
    SELECT mv.idMovimiento, mv.idUsuarioExterno, mv.rolCuenta, mv.tipoMovimiento, mv.importe, mv.moneda, mv.descripcion,
           mv.idReserva, mv.idPago, mv.idComprobante, mv.fechaMovimiento, mv.saldoAcumulado,
           c.numero AS numeroComprobante, c.estado AS estadoComprobante, c.origen AS origenComprobante
    FROM movimientos mv
    LEFT JOIN dbo.Comprobante c ON c.idComprobante = mv.idComprobante
    WHERE (@moneda IS NULL OR mv.moneda = @moneda)
      AND (@desde IS NULL OR mv.fechaMovimiento >= @desde)
      AND (@hasta IS NULL OR mv.fechaMovimiento < DATEADD(DAY, 1, @hasta))
      AND (@tipoMovimiento IS NULL OR mv.tipoMovimiento = @tipoMovimiento)
    ORDER BY mv.fechaMovimiento DESC, mv.idMovimiento DESC;
END
GO

-- Administración: usuarios y sus cuentas. Sin texto, solo los que tienen
-- movimientos; con texto, busca por nombre o correo (aunque no tengan).
IF OBJECT_ID('dbo.sp_CuentaCorriente_ListarCuentas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CuentaCorriente_ListarCuentas;
GO
CREATE PROCEDURE dbo.sp_CuentaCorriente_ListarCuentas
    @texto NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(210) = CASE WHEN @texto IS NULL THEN NULL ELSE N'%' + @texto + N'%' END;

    ;WITH usuarios AS
    (
        SELECT TOP 50 u.idUsuarioExterno, LTRIM(RTRIM(u.nombre + N' ' + u.apellido)) AS nombreUsuario,
               u.correoElectronico, u.perfilUsuario
        FROM dbo.UsuarioExterno u
        WHERE (@patron IS NULL AND EXISTS (SELECT 1 FROM dbo.MovimientoCuentaCorriente m WHERE m.idUsuarioExterno = u.idUsuarioExterno))
           OR (@patron IS NOT NULL AND (u.nombre + N' ' + u.apellido LIKE @patron OR u.correoElectronico LIKE @patron))
        ORDER BY u.apellido, u.nombre
    )
    SELECT us.idUsuarioExterno, us.nombreUsuario, us.correoElectronico, us.perfilUsuario,
           m.rolCuenta, m.moneda, SUM(m.importe) AS saldo, MAX(m.fechaMovimiento) AS ultimoMovimiento
    FROM usuarios us
    LEFT JOIN dbo.MovimientoCuentaCorriente m ON m.idUsuarioExterno = us.idUsuarioExterno
    GROUP BY us.idUsuarioExterno, us.nombreUsuario, us.correoElectronico, us.perfilUsuario, m.rolCuenta, m.moneda
    ORDER BY us.nombreUsuario, m.rolCuenta, m.moneda;
END
GO

-- Liquidación al gestor: StageUp le transfiere (fuera del sistema) parte o
-- todo su saldo a favor y se registra acá.
-- resultado: OK | SALDO_INSUFICIENTE | IMPORTE_INVALIDO
IF OBJECT_ID('dbo.sp_CuentaCorriente_RegistrarLiquidacion', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CuentaCorriente_RegistrarLiquidacion;
GO
CREATE PROCEDURE dbo.sp_CuentaCorriente_RegistrarLiquidacion
    @idUsuarioGestor    INT,
    @moneda             NVARCHAR(3),
    @importe            DECIMAL(18,2),
    @detalle            NVARCHAR(300),
    @idUsuarioInterno   INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @recurso NVARCHAR(100) = N'StageUp_CuentaCorriente_' + CAST(@idUsuarioGestor AS NVARCHAR(10));
        DECLARE @bloqueo INT;
        EXEC @bloqueo = sp_getapplock @Resource = @recurso, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
        IF @bloqueo < 0
            THROW 51010, N'La cuenta está ocupada con otra operación. Probá de nuevo en unos segundos.', 1;

        DECLARE @saldo DECIMAL(18,2) = ISNULL((
            SELECT SUM(importe) FROM dbo.MovimientoCuentaCorriente
            WHERE idUsuarioExterno = @idUsuarioGestor AND rolCuenta = N'Gestor' AND moneda = @moneda), 0);

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @importe IS NULL OR @importe <= 0 SET @resultado = N'IMPORTE_INVALIDO';
        ELSE IF @importe > @saldo SET @resultado = N'SALDO_INSUFICIENTE';

        IF @resultado <> N'OK'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @resultado AS resultado, CAST(NULL AS INT) AS idMovimiento;
            RETURN;
        END

        INSERT INTO dbo.MovimientoCuentaCorriente
            (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idUsuarioInternoResponsable)
        VALUES
            (@idUsuarioGestor, N'Gestor', N'Liquidacion', -@importe, @moneda,
             N'Liquidación transferida al gestor' + CASE WHEN @detalle IS NULL THEN N'' ELSE N': ' + @detalle END,
             @idUsuarioInterno);

        DECLARE @idMovimiento INT = CAST(SCOPE_IDENTITY() AS INT);
        COMMIT TRANSACTION;
        SELECT N'OK' AS resultado, @idMovimiento AS idMovimiento;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =============================================================================
-- Comprobantes manuales (NC/ND emitidas por el administrador)
-- =============================================================================
-- resultado: OK | RESERVA_INVALIDA
IF OBJECT_ID('dbo.sp_Comprobante_EmitirManual', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Comprobante_EmitirManual;
GO
CREATE PROCEDURE dbo.sp_Comprobante_EmitirManual
    @tipo               NVARCHAR(2),
    @idUsuarioExterno   INT,
    @rolCuenta          NVARCHAR(10),
    @importe            DECIMAL(18,2),
    @moneda             NVARCHAR(3),
    @motivo             NVARCHAR(500),
    @idReserva          INT = NULL,
    @idUsuarioInterno   INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Si se indica una reserva, tiene que ser del usuario (como cliente o
    -- como gestor del espacio, según la cuenta).
    IF @idReserva IS NOT NULL AND NOT EXISTS
    (
        SELECT 1
        FROM dbo.Reserva r
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        WHERE r.idReserva = @idReserva
          AND ((@rolCuenta = N'Cliente' AND r.idUsuarioExternoSolicitante = @idUsuarioExterno)
            OR (@rolCuenta = N'Gestor' AND e.idUsuarioGestor = @idUsuarioExterno))
    )
    BEGIN
        SELECT N'RESERVA_INVALIDA' AS resultado, CAST(NULL AS INT) AS idComprobante;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.Comprobante (tipo, idUsuarioExterno, rolCuenta, idReserva, importe, moneda, origen, motivo, idUsuarioInternoEmisor)
        VALUES (@tipo, @idUsuarioExterno, @rolCuenta, @idReserva, @importe, @moneda, N'AjusteManual', @motivo, @idUsuarioInterno);

        DECLARE @idComprobante INT = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO dbo.MovimientoCuentaCorriente
            (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idComprobante, idUsuarioInternoResponsable)
        VALUES
            (@idUsuarioExterno, @rolCuenta,
             CASE WHEN @tipo = N'NC' THEN N'NotaCredito' ELSE N'NotaDebito' END,
             CASE WHEN @tipo = N'NC' THEN @importe ELSE -@importe END,
             @moneda,
             LEFT(CASE WHEN @tipo = N'NC' THEN N'Nota de crédito: ' ELSE N'Nota de débito: ' END + @motivo, 500),
             @idReserva, @idComprobante, @idUsuarioInterno);

        COMMIT TRANSACTION;
        SELECT N'OK' AS resultado, @idComprobante AS idComprobante;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Un comprobante no se borra: se anula con un movimiento inverso. Solo los
-- manuales (los de una cancelación forman parte de la reserva).
-- resultado: OK | NO_ENCONTRADO | NO_ANULABLE
IF OBJECT_ID('dbo.sp_Comprobante_Anular', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Comprobante_Anular;
GO
CREATE PROCEDURE dbo.sp_Comprobante_Anular
    @idComprobante      INT,
    @motivo             NVARCHAR(500),
    @idUsuarioInterno   INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @tipo NVARCHAR(2), @estado NVARCHAR(10), @origen NVARCHAR(30), @idUsuario INT, @rol NVARCHAR(10),
                @importe DECIMAL(18,2), @moneda NVARCHAR(3), @idReserva INT, @numero NVARCHAR(20);

        SELECT @tipo = tipo, @estado = estado, @origen = origen, @idUsuario = idUsuarioExterno, @rol = rolCuenta,
               @importe = importe, @moneda = moneda, @idReserva = idReserva, @numero = numero
        FROM dbo.Comprobante WITH (UPDLOCK, HOLDLOCK)
        WHERE idComprobante = @idComprobante;

        DECLARE @resultado NVARCHAR(30) = N'OK';
        IF @tipo IS NULL SET @resultado = N'NO_ENCONTRADO';
        ELSE IF @estado <> N'Emitido' OR @origen <> N'AjusteManual' SET @resultado = N'NO_ANULABLE';

        IF @resultado <> N'OK'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @resultado AS resultado;
            RETURN;
        END

        UPDATE dbo.Comprobante
        SET estado = N'Anulado', fechaAnulacion = GETDATE(), motivoAnulacion = @motivo
        WHERE idComprobante = @idComprobante;

        INSERT INTO dbo.MovimientoCuentaCorriente
            (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idComprobante, idUsuarioInternoResponsable)
        VALUES
            (@idUsuario, @rol, N'AnulacionComprobante',
             CASE WHEN @tipo = N'NC' THEN -@importe ELSE @importe END,
             @moneda, LEFT(N'Anulación de ' + @numero + N': ' + @motivo, 500), @idReserva, @idComprobante, @idUsuarioInterno);

        COMMIT TRANSACTION;
        SELECT N'OK' AS resultado;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Comprobante_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Comprobante_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Comprobante_ObtenerPorId
    @idComprobante INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idComprobante, c.tipo, c.numero, c.idUsuarioExterno, c.rolCuenta, c.idReserva, c.importe, c.moneda,
           c.origen, c.motivo, c.estado, c.idUsuarioInternoEmisor, c.fechaEmision, c.fechaAnulacion, c.motivoAnulacion,
           LTRIM(RTRIM(u.nombre + N' ' + u.apellido)) AS nombreUsuario, u.correoElectronico AS correoUsuario
    FROM dbo.Comprobante c
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = c.idUsuarioExterno
    WHERE c.idComprobante = @idComprobante;
END
GO

-- =============================================================================
-- Reportes (script 48): con el módulo de pagos, una reserva cuenta como
-- ingreso cuando está pagada (o es anterior al módulo, estadoPago =
-- 'NoRequerido'). Una aceptada que todavía espera el pago no es ingreso.
-- =============================================================================
IF OBJECT_ID('dbo.sp_Reporte_Indicadores', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_Indicadores;
GO
CREATE PROCEDURE dbo.sp_Reporte_Indicadores
    @desde  DATE,
    @hasta  DATE,
    @moneda NVARCHAR(3) = N'ARS'
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.UsuarioExterno WHERE activo = 1 AND estadoCuenta = N'Activa') AS usuariosActivos,
        (SELECT COUNT(*) FROM dbo.UsuarioExterno WHERE fechaAlta >= @desde AND fechaAlta < DATEADD(DAY, 1, @hasta)) AS usuariosNuevos,
        (SELECT COUNT(*) FROM dbo.EspacioArtistico WHERE activo = 1 AND publicado = 1) AS espaciosPublicados,
        (SELECT COUNT(*) FROM dbo.Reserva WHERE fechaCreacion >= @desde AND fechaCreacion < DATEADD(DAY, 1, @hasta)) AS reservasSolicitadas,
        (SELECT COUNT(*) FROM dbo.Reserva
            WHERE estadoReserva IN (N'Aceptada', N'Finalizada')
              AND estadoPago IN (N'Pagado', N'NoRequerido')
              AND fechaSolicitada BETWEEN @desde AND @hasta) AS reservasConfirmadas,
        (SELECT ISNULL(SUM(importeEstimado), 0) FROM dbo.Reserva
            WHERE estadoReserva IN (N'Aceptada', N'Finalizada')
              AND estadoPago IN (N'Pagado', N'NoRequerido')
              AND ISNULL(moneda, N'ARS') = @moneda
              AND fechaSolicitada BETWEEN @desde AND @hasta) AS importeReservas,
        (SELECT ISNULL(SUM(importeComision), 0) FROM dbo.Reserva
            WHERE estadoReserva = N'Cancelada' AND comisionAplicada = 1
              AND ISNULL(moneda, N'ARS') = @moneda
              AND fechaCancelacion >= @desde AND fechaCancelacion < DATEADD(DAY, 1, @hasta)) AS importeComisiones,
        (SELECT CONVERT(DECIMAL(4,2), AVG(CONVERT(DECIMAL(10,2), puntaje))) FROM dbo.Calificacion
            WHERE tipoCalificacion = N'Espacio' AND activo = 1) AS promedioCalificacion,
        (SELECT COUNT(*) FROM dbo.Ticket WHERE estado IN (N'Abierto', N'EnRevision')) AS ticketsPendientes;
END
GO

IF OBJECT_ID('dbo.sp_Reporte_Ingresos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_Ingresos;
GO
CREATE PROCEDURE dbo.sp_Reporte_Ingresos
    @desde        DATE,
    @hasta        DATE,
    @agrupacion   NVARCHAR(10) = N'Mes',   -- Dia | Semana | Mes | Anio
    @moneda       NVARCHAR(3) = N'ARS',
    @provincia    NVARCHAR(100) = NULL,
    @tipoEspacio  NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH movimientos AS
    (
        SELECT CAST(r.fechaSolicitada AS DATE) AS fecha, ISNULL(r.importeEstimado, 0) AS importeReserva,
               CAST(0 AS DECIMAL(18,2)) AS importeComision, 1 AS esReserva
        FROM dbo.Reserva r
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE r.estadoReserva IN (N'Aceptada', N'Finalizada')
          AND r.estadoPago IN (N'Pagado', N'NoRequerido')
          AND ISNULL(r.moneda, N'ARS') = @moneda
          AND r.fechaSolicitada BETWEEN @desde AND @hasta
          AND (@provincia IS NULL OR f.provincia = @provincia)
          AND (@tipoEspacio IS NULL OR e.tipoEspacio = @tipoEspacio)
        UNION ALL
        SELECT CAST(r.fechaCancelacion AS DATE), CAST(0 AS DECIMAL(18,2)), ISNULL(r.importeComision, 0), 0
        FROM dbo.Reserva r
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE r.estadoReserva = N'Cancelada' AND r.comisionAplicada = 1
          AND ISNULL(r.moneda, N'ARS') = @moneda
          AND r.fechaCancelacion >= @desde AND r.fechaCancelacion < DATEADD(DAY, 1, @hasta)
          AND (@provincia IS NULL OR f.provincia = @provincia)
          AND (@tipoEspacio IS NULL OR e.tipoEspacio = @tipoEspacio)
    ),
    agrupados AS
    (
        SELECT
            CASE @agrupacion
                WHEN N'Dia'    THEN fecha
                WHEN N'Semana' THEN DATEADD(DAY, -((DATEPART(WEEKDAY, fecha) + @@DATEFIRST - 2) % 7), fecha)
                WHEN N'Anio'   THEN DATEFROMPARTS(YEAR(fecha), 1, 1)
                ELSE DATEFROMPARTS(YEAR(fecha), MONTH(fecha), 1)
            END AS inicioPeriodo,
            importeReserva, importeComision, esReserva
        FROM movimientos
    )
    SELECT inicioPeriodo,
           SUM(esReserva) AS cantidadReservas,
           SUM(importeReserva) AS importeReservas,
           SUM(importeComision) AS importeComisiones
    FROM agrupados
    GROUP BY inicioPeriodo
    ORDER BY inicioPeriodo;
END
GO

IF OBJECT_ID('dbo.sp_Reporte_IngresosPorZona', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_IngresosPorZona;
GO
CREATE PROCEDURE dbo.sp_Reporte_IngresosPorZona
    @desde       DATE,
    @hasta       DATE,
    @moneda      NVARCHAR(3) = N'ARS',
    @tipoEspacio NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(f.provincia, N'Sin ubicación') AS provincia,
        ISNULL(f.ciudad, N'-') AS ciudad,
        COUNT(*) AS cantidadReservas,
        SUM(ISNULL(r.importeEstimado, 0)) AS importeReservas
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE r.estadoReserva IN (N'Aceptada', N'Finalizada')
      AND r.estadoPago IN (N'Pagado', N'NoRequerido')
      AND ISNULL(r.moneda, N'ARS') = @moneda
      AND r.fechaSolicitada BETWEEN @desde AND @hasta
      AND (@tipoEspacio IS NULL OR e.tipoEspacio = @tipoEspacio)
    GROUP BY f.provincia, f.ciudad
    ORDER BY importeReservas DESC;
END
GO


-- =============================================================================
-- Permisos y menú. Mismo patrón que los scripts 46 y 48, armado como
-- procedimiento temporal porque se registran dos permisos:
--   * GESTIONAR_PAGOS: Interno/GestionCuentasCorrientes.aspx
--   * CONFIGURAR_PARAMETROS: Interno/ParametrosPlataforma.aspx
-- =============================================================================
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
    @codigo = N'GESTIONAR_PAGOS',
    @nombre = N'Pagos y cuentas corrientes',
    @descripcion = N'Ver pagos y cuentas corrientes, emitir y anular notas de crédito y débito y registrar liquidaciones a gestores.',
    @url = N'~/Interno/GestionCuentasCorrientes.aspx',
    @accion = N'Gestionar',
    @orden = 20;
GO

EXEC #RegistrarPermisoAdministracion
    @codigo = N'CONFIGURAR_PARAMETROS',
    @nombre = N'Parámetros de la plataforma',
    @descripcion = N'Configurar la comisión de StageUp, el plazo de pago y la penalidad por cancelación.',
    @url = N'~/Interno/ParametrosPlataforma.aspx',
    @accion = N'Configurar',
    @orden = 30;
GO

DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
