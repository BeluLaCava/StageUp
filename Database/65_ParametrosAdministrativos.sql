IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.ParametroPlataforma', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero deben ejecutarse Database/49_PagosNotasYCuentaCorriente.sql y Database/53_PoliticaCancelacionYDetalleReserva.sql.', 1;
END
GO

-- ---------------------------------------------------------------------------
-- CU-002-004: parámetros administrativos agrupados (comisión, política de
-- cancelación, estados administrativos de pagos y facturas, datos de
-- facturación de Artera y correos administrativos), con vigencia e
-- historial de cambios (valor anterior, valor nuevo, responsable, fecha y
-- vigencia).
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.ParametroPlataforma', N'valor') < 1000
BEGIN
    ALTER TABLE dbo.ParametroPlataforma ALTER COLUMN valor NVARCHAR(500) NOT NULL;
END
GO

MERGE dbo.ParametroPlataforma AS destino
USING (VALUES
    -- Estados administrativos: texto con el que se muestra cada estado.
    (N'EstadoPagoAprobado', N'Confirmado', N'Pago aprobado por el medio de pago.'),
    (N'EstadoPagoPendiente', N'Pendiente (informado por el medio de pago)', N'El medio de pago avisó el pago pero todavía no lo confirmó.'),
    (N'EstadoPagoEsperando', N'Pendiente (esperando el pago)', N'Reserva aceptada que el solicitante todavía no pagó.'),
    (N'EstadoPagoRechazado', N'Fallido', N'El medio de pago rechazó el pago.'),
    (N'EstadoPagoAnulado', N'Cancelado', N'Pago anulado por el medio de pago.'),
    (N'EstadoPagoVencido', N'Cancelado (venció el plazo de pago)', N'La reserva se canceló porque no se pagó a tiempo.'),
    (N'EstadoPagoObservado', N'En revisión (observado)', N'El aviso del medio de pago llegó con datos incompletos o distintos.'),
    (N'EstadoPagoNoAsociado', N'En revisión (sin reserva asociada)', N'El aviso del medio de pago no corresponde a ninguna reserva.'),
    (N'EstadoFacturaEmitida', N'Emitida', N'Factura emitida al confirmarse el pago.'),
    (N'EstadoFacturaPendiente', N'Factura pendiente', N'La operación todavía no tiene factura (pago sin confirmar).'),
    (N'EstadoFacturaAnulada', N'Anulada', N'Factura o comprobante anulado.'),
    (N'EstadoOperacionPagado', N'Pagado', N'Transacción de una reserva abonada.'),
    (N'EstadoOperacionCobrado', N'Cobrado', N'Cargo por cancelación cobrado.'),
    (N'EstadoOperacionDevuelto', N'Devuelto al solicitante', N'Reserva abonada que se canceló y volvió como saldo a favor.'),
    (N'EstadoOperacionPendiente', N'Pendiente', N'Transacción con el pago todavía pendiente.'),
    -- Datos generales de facturación de Artera (factura simulada).
    (N'FacturacionRazonSocial', N'Artera – StageUp', N'Razón social que figura como emisor de las facturas.'),
    (N'FacturacionCuit', N'30-00000000-0', N'CUIT del emisor (simulado), con el formato 00-00000000-0.'),
    (N'FacturacionCondicionIva', N'IVA Responsable Inscripto', N'Condición frente al IVA del emisor.'),
    (N'FacturacionDomicilio', N'Ciudad Autónoma de Buenos Aires', N'Domicilio comercial del emisor.'),
    (N'FacturacionPuntoVenta', N'1', N'Punto de venta de las facturas (1 a 99999).'),
    (N'FacturacionLeyenda', N'Comprobante simulado emitido por StageUp. No tiene validez fiscal.', N'Leyenda al pie de las facturas.'),
    -- Correos administrativos (pagos, vencimientos y comprobantes).
    (N'CorreosAdministrativosActivos', N'Si', N'Si se envían los correos administrativos de pagos, vencimientos y comprobantes.'),
    (N'CorreosAdministrativosRemitente', N'StageUp Administración', N'Nombre con el que llegan los correos administrativos.'),
    (N'CorreosAdministrativosResponderA', N'', N'Dirección a la que se responden los correos administrativos (opcional).'),
    (N'CorreosAdministrativosCopia', N'', N'Dirección que recibe una copia oculta de cada correo administrativo (opcional).')
) AS origen (clave, valor, descripcion)
ON destino.clave = origen.clave
WHEN NOT MATCHED THEN
    INSERT (clave, valor, descripcion) VALUES (origen.clave, origen.valor, origen.descripcion);
GO

-- Historial de cambios. estado: Aplicado (ya rige), Programado (rige desde
-- fechaVigencia) o Reemplazado (otro cambio posterior lo dejó sin efecto
-- antes de entrar en vigencia).
IF OBJECT_ID(N'dbo.ParametroPlataformaCambio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParametroPlataformaCambio
    (
        idCambio           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ParametroPlataformaCambio PRIMARY KEY,
        clave              NVARCHAR(100)  NOT NULL,
        valorAnterior      NVARCHAR(500)  NULL,
        valorNuevo         NVARCHAR(500)  NOT NULL,
        fechaVigencia      DATETIME       NOT NULL,
        estado             NVARCHAR(20)   NOT NULL,
        idUsuarioInterno   INT            NULL,
        fechaRegistro      DATETIME       NOT NULL CONSTRAINT DF_ParametroPlataformaCambio_fecha DEFAULT (GETDATE()),
        fechaAplicacion    DATETIME       NULL,
        CONSTRAINT FK_ParametroPlataformaCambio_Parametro FOREIGN KEY (clave) REFERENCES dbo.ParametroPlataforma (clave),
        CONSTRAINT FK_ParametroPlataformaCambio_Usuario FOREIGN KEY (idUsuarioInterno) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_ParametroPlataformaCambio_estado CHECK (estado IN (N'Aplicado', N'Programado', N'Reemplazado'))
    );

    CREATE INDEX IX_ParametroPlataformaCambio_Clave ON dbo.ParametroPlataformaCambio (clave, fechaAplicacion);
    CREATE INDEX IX_ParametroPlataformaCambio_Programado ON dbo.ParametroPlataformaCambio (fechaVigencia) WHERE estado = N'Programado';
END
GO

-- Aplica los cambios programados cuya vigencia ya empezó (en orden).
IF OBJECT_ID('dbo.sp_ParametroPlataforma_AplicarProgramados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ParametroPlataforma_AplicarProgramados;
GO
CREATE PROCEDURE dbo.sp_ParametroPlataforma_AplicarProgramados
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.ParametroPlataformaCambio WHERE estado = N'Programado' AND fechaVigencia <= GETDATE())
        RETURN;

    BEGIN TRANSACTION;

    DECLARE @idCambio INT, @clave NVARCHAR(100), @valor NVARCHAR(500);
    DECLARE pendientes CURSOR LOCAL FAST_FORWARD FOR
        SELECT idCambio, clave, valorNuevo
        FROM dbo.ParametroPlataformaCambio WITH (UPDLOCK, HOLDLOCK)
        WHERE estado = N'Programado' AND fechaVigencia <= GETDATE()
        ORDER BY fechaVigencia, idCambio;

    OPEN pendientes;
    FETCH NEXT FROM pendientes INTO @idCambio, @clave, @valor;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        UPDATE dbo.ParametroPlataformaCambio
        SET valorAnterior = (SELECT valor FROM dbo.ParametroPlataforma WHERE clave = @clave),
            estado = N'Aplicado',
            fechaAplicacion = GETDATE()
        WHERE idCambio = @idCambio;

        UPDATE dbo.ParametroPlataforma
        SET valor = @valor, fechaUltimaModificacion = GETDATE()
        WHERE clave = @clave;

        FETCH NEXT FROM pendientes INTO @idCambio, @clave, @valor;
    END
    CLOSE pendientes;
    DEALLOCATE pendientes;

    COMMIT TRANSACTION;
END
GO

-- Pasos 3 a 5: parámetros vigentes, con el cambio programado de cada uno
-- (si lo hay). Antes de listar aplica los programados que ya rigen.
IF OBJECT_ID('dbo.sp_ParametroPlataforma_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ParametroPlataforma_Listar;
GO
CREATE PROCEDURE dbo.sp_ParametroPlataforma_Listar
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.sp_ParametroPlataforma_AplicarProgramados;

    SELECT p.clave, p.valor, p.descripcion, p.fechaUltimaModificacion,
           prog.valorNuevo AS valorProgramado, prog.fechaVigencia AS fechaVigenciaProgramada
    FROM dbo.ParametroPlataforma p
    OUTER APPLY (SELECT TOP 1 c.valorNuevo, c.fechaVigencia
                 FROM dbo.ParametroPlataformaCambio c
                 WHERE c.clave = p.clave AND c.estado = N'Programado'
                 ORDER BY c.fechaVigencia DESC, c.idCambio DESC) prog
    ORDER BY p.clave;
END
GO

-- ---------------------------------------------------------------------------
-- Pasos 12 y 13 (y A15): guarda todos los cambios juntos en una
-- transacción. @cambios = <c><p clave="..." valor="..."/>...</c>. Sin
-- @fechaVigencia (o con una fecha que ya pasó) el cambio rige en el momento;
-- con una fecha futura queda programado. Un cambio nuevo deja sin efecto los
-- programados anteriores del mismo parámetro. Devuelve los cambios
-- registrados (los valores iguales al vigente se ignoran).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_ParametroPlataforma_GuardarCambios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ParametroPlataforma_GuardarCambios;
GO
CREATE PROCEDURE dbo.sp_ParametroPlataforma_GuardarCambios
    @cambios           XML,
    @fechaVigencia     DATETIME = NULL,
    @idUsuarioInterno  INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ahora DATETIME = GETDATE();
    DECLARE @inmediato BIT = CASE WHEN @fechaVigencia IS NULL OR @fechaVigencia <= @ahora THEN 1 ELSE 0 END;
    DECLARE @vigencia DATETIME = CASE WHEN @inmediato = 1 THEN @ahora ELSE @fechaVigencia END;

    DECLARE @entrada TABLE (clave NVARCHAR(100) PRIMARY KEY, valor NVARCHAR(500) NOT NULL);
    INSERT INTO @entrada (clave, valor)
    SELECT DISTINCT x.value('@clave', 'NVARCHAR(100)'), ISNULL(x.value('@valor', 'NVARCHAR(500)'), N'')
    FROM @cambios.nodes('/c/p') AS t(x);

    IF EXISTS (SELECT 1 FROM @entrada e WHERE NOT EXISTS (SELECT 1 FROM dbo.ParametroPlataforma p WHERE p.clave = e.clave))
    BEGIN
        SELECT N'PARAMETRO_INEXISTENTE' AS resultado, CAST(NULL AS INT) AS idCambio, CAST(NULL AS NVARCHAR(100)) AS clave,
               CAST(NULL AS NVARCHAR(500)) AS valorAnterior, CAST(NULL AS NVARCHAR(500)) AS valorNuevo, CAST(NULL AS DATETIME) AS fechaVigencia,
               CAST(NULL AS NVARCHAR(20)) AS estado;
        RETURN;
    END

    DECLARE @registrados TABLE (idCambio INT, clave NVARCHAR(100), valorAnterior NVARCHAR(500), valorNuevo NVARCHAR(500),
                                fechaVigencia DATETIME, estado NVARCHAR(20));

    BEGIN TRANSACTION;

    -- Solo lo que cambia respecto del valor vigente.
    DELETE e
    FROM @entrada e
    INNER JOIN dbo.ParametroPlataforma p WITH (UPDLOCK) ON p.clave = e.clave
    WHERE p.valor = e.valor COLLATE Latin1_General_CS_AS;

    UPDATE c
    SET estado = N'Reemplazado'
    FROM dbo.ParametroPlataformaCambio c
    INNER JOIN @entrada e ON e.clave = c.clave
    WHERE c.estado = N'Programado';

    INSERT INTO dbo.ParametroPlataformaCambio
        (clave, valorAnterior, valorNuevo, fechaVigencia, estado, idUsuarioInterno, fechaRegistro, fechaAplicacion)
    OUTPUT inserted.idCambio, inserted.clave, inserted.valorAnterior, inserted.valorNuevo, inserted.fechaVigencia, inserted.estado
        INTO @registrados
    SELECT e.clave, p.valor, e.valor, @vigencia,
           CASE WHEN @inmediato = 1 THEN N'Aplicado' ELSE N'Programado' END,
           @idUsuarioInterno, @ahora,
           CASE WHEN @inmediato = 1 THEN @ahora END
    FROM @entrada e
    INNER JOIN dbo.ParametroPlataforma p ON p.clave = e.clave;

    IF @inmediato = 1
    BEGIN
        UPDATE p
        SET valor = e.valor, fechaUltimaModificacion = @ahora
        FROM dbo.ParametroPlataforma p
        INNER JOIN @entrada e ON e.clave = p.clave;
    END

    COMMIT TRANSACTION;

    SELECT N'OK' AS resultado, idCambio, clave, valorAnterior, valorNuevo, fechaVigencia, estado
    FROM @registrados
    ORDER BY clave;
END
GO

-- Paso 13: historial de cambios (todos o de un parámetro).
IF OBJECT_ID('dbo.sp_ParametroPlataforma_ListarCambios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ParametroPlataforma_ListarCambios;
GO
CREATE PROCEDURE dbo.sp_ParametroPlataforma_ListarCambios
    @clave NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 300
        c.idCambio, c.clave, c.valorAnterior, c.valorNuevo, c.fechaVigencia, c.estado, c.idUsuarioInterno,
        c.fechaRegistro, c.fechaAplicacion,
        LTRIM(RTRIM(ISNULL(u.nombre, N'') + N' ' + ISNULL(u.apellido, N''))) AS nombreResponsable
    FROM dbo.ParametroPlataformaCambio c
    LEFT JOIN dbo.UsuarioInterno u ON u.idUsuarioInterno = c.idUsuarioInterno
    WHERE @clave IS NULL OR c.clave = @clave
    ORDER BY c.fechaRegistro DESC, c.idCambio DESC;
END
GO

-- Valores que regían en una fecha: si el parámetro cambió después de esa
-- fecha, vale el "valor anterior" del primer cambio posterior; si no, el
-- valor actual. Así una factura vieja se sigue mostrando con los datos de
-- facturación de su momento.
IF OBJECT_ID('dbo.sp_ParametroPlataforma_ObtenerValoresAl', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ParametroPlataforma_ObtenerValoresAl;
GO
CREATE PROCEDURE dbo.sp_ParametroPlataforma_ObtenerValoresAl
    @fecha DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.clave,
           ISNULL(posterior.valorAnterior, p.valor) AS valor
    FROM dbo.ParametroPlataforma p
    OUTER APPLY (SELECT TOP 1 c.valorAnterior
                 FROM dbo.ParametroPlataformaCambio c
                 WHERE c.clave = p.clave AND c.estado = N'Aplicado' AND c.fechaAplicacion > @fecha
                       AND c.valorAnterior IS NOT NULL
                 ORDER BY c.fechaAplicacion, c.idCambio) posterior
    ORDER BY p.clave;
END
GO

-- Nombre de la pantalla según el CU.
UPDATE dbo.ComponentePermiso
SET nombre = N'Parámetros administrativos',
    descripcion = N'Consultar y modificar los parámetros administrativos: comisión, política de cancelación, estados de pagos y facturas, datos de facturación y correos administrativos.',
    fechaUltimaModificacion = GETDATE()
WHERE codigoPermiso = N'CONFIGURAR_PARAMETROS';
GO

IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.OpcionMenu
    SET texto = N'Parámetros administrativos'
    WHERE url = N'~/Interno/ParametrosPlataforma.aspx';
END
GO
