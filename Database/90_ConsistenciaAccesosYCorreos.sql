
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.IntentoAcceso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntentoAcceso (
        cuenta CHAR(64) NOT NULL CONSTRAINT PK_IntentoAcceso PRIMARY KEY,
        intentos INT NOT NULL CONSTRAINT DF_IntentoAcceso_intentos DEFAULT 0,
        bloqueadoHasta DATETIME2 NULL,
        ultimaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_IntentoAcceso_fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_IntentoAcceso_intentos CHECK (intentos BETWEEN 0 AND 5)
    );
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_IntentoAcceso_Consultar @cuenta CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.IntentoAcceso WITH (UPDLOCK, HOLDLOCK)
    SET intentos = 0, bloqueadoHasta = NULL, ultimaActualizacion = SYSUTCDATETIME()
    WHERE cuenta = @cuenta AND bloqueadoHasta <= SYSUTCDATETIME();
    SELECT ISNULL((SELECT CASE WHEN bloqueadoHasta > SYSUTCDATETIME()
        THEN DATEDIFF(SECOND, SYSUTCDATETIME(), bloqueadoHasta) + 1 ELSE 0 END
        FROM dbo.IntentoAcceso WHERE cuenta = @cuenta), 0) AS segundos;
    COMMIT;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_IntentoAcceso_Registrar @cuenta CHAR(64), @exitoso BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM dbo.IntentoAcceso WITH (UPDLOCK, HOLDLOCK) WHERE cuenta = @cuenta)
        INSERT dbo.IntentoAcceso(cuenta) VALUES(@cuenta);
    IF EXISTS (SELECT 1 FROM dbo.IntentoAcceso WHERE cuenta = @cuenta AND bloqueadoHasta > SYSUTCDATETIME())
    BEGIN
        SELECT DATEDIFF(SECOND, SYSUTCDATETIME(), bloqueadoHasta) + 1 AS segundos FROM dbo.IntentoAcceso WHERE cuenta = @cuenta;
        COMMIT;
        RETURN;
    END
    UPDATE dbo.IntentoAcceso SET intentos = 0, bloqueadoHasta = NULL
    WHERE cuenta = @cuenta AND bloqueadoHasta <= SYSUTCDATETIME();
    IF @exitoso = 1
        UPDATE dbo.IntentoAcceso SET intentos = 0, bloqueadoHasta = NULL, ultimaActualizacion = SYSUTCDATETIME() WHERE cuenta = @cuenta;
    ELSE
        UPDATE dbo.IntentoAcceso SET intentos = intentos + 1,
            bloqueadoHasta = CASE WHEN intentos + 1 >= 5 THEN DATEADD(MINUTE, 15, SYSUTCDATETIME()) END,
            ultimaActualizacion = SYSUTCDATETIME() WHERE cuenta = @cuenta;
    SELECT CASE WHEN bloqueadoHasta > SYSUTCDATETIME() THEN DATEDIFF(SECOND, SYSUTCDATETIME(), bloqueadoHasta) + 1 ELSE 0 END AS segundos
    FROM dbo.IntentoAcceso WHERE cuenta = @cuenta;
    COMMIT;
END
GO
IF OBJECT_ID(N'dbo.CorreoAdministrativo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorreoAdministrativo (
        idCorreo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CorreoAdministrativo PRIMARY KEY,
        destinatario NVARCHAR(300) NOT NULL,
        asunto NVARCHAR(500) NOT NULL,
        idReserva INT NULL REFERENCES dbo.Reserva(idReserva),
        idPago INT NULL REFERENCES dbo.Pago(idPago),
        idComprobante INT NULL REFERENCES dbo.Comprobante(idComprobante),
        fechaIntento DATETIME NOT NULL CONSTRAINT DF_CorreoAdministrativo_fecha DEFAULT GETDATE(),
        fechaResultado DATETIME NULL,
        estado NVARCHAR(20) NOT NULL CONSTRAINT DF_CorreoAdministrativo_estado DEFAULT N'EnProceso',
        detalle NVARCHAR(300) NULL,
        CONSTRAINT CK_CorreoAdministrativo_estado CHECK (estado IN (N'EnProceso',N'Enviado',N'Fallido'))
    );
    CREATE INDEX IX_CorreoAdministrativo_fecha ON dbo.CorreoAdministrativo(fechaIntento DESC);
    CREATE INDEX IX_CorreoAdministrativo_reserva ON dbo.CorreoAdministrativo(idReserva, fechaIntento DESC);
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_CorreoAdministrativo_Iniciar
    @destinatario NVARCHAR(300), @asunto NVARCHAR(500), @idReserva INT = NULL,
    @idPago INT = NULL, @idComprobante INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.CorreoAdministrativo(destinatario, asunto, idReserva, idPago, idComprobante, detalle)
    VALUES(@destinatario,@asunto,@idReserva,@idPago,@idComprobante,N'Intento registrado; todavía no hay un resultado de envío confirmado.');
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_CorreoAdministrativo_Finalizar @idCorreo INT, @enviado BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CorreoAdministrativo SET fechaResultado = GETDATE(), estado = CASE WHEN @enviado = 1 THEN N'Enviado' ELSE N'Fallido' END,
        detalle = CASE WHEN @enviado = 1 THEN N'Aceptado por el servidor de correo. No acredita entrega ni lectura por el destinatario.'
            ELSE N'El servicio no pudo confirmar el envío. La operación de negocio permanece registrada.' END
    WHERE idCorreo = @idCorreo AND estado = N'EnProceso';
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_CorreoAdministrativo_Listar
    @estado NVARCHAR(20) = NULL, @desde DATE = NULL, @hasta DATE = NULL, @idReserva INT = NULL, @destinatario NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (200) idCorreo,destinatario,asunto,idReserva,idPago,idComprobante,fechaIntento,fechaResultado,estado,detalle
    FROM dbo.CorreoAdministrativo
    WHERE (@estado IS NULL OR estado = @estado) AND (@desde IS NULL OR fechaIntento >= @desde)
      AND (@hasta IS NULL OR CONVERT(DATE,fechaIntento) <= @hasta)
      AND (@idReserva IS NULL OR idReserva = @idReserva)
      AND (@destinatario IS NULL OR CHARINDEX(@destinatario, destinatario) > 0)
    ORDER BY fechaIntento DESC,idCorreo DESC;
END
GO

BEGIN TRANSACTION;
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

UPDATE t SET estadoPago = N'Devuelto', fechaUltimaModificacion = GETDATE()
FROM dbo.TransaccionEconomica t JOIN dbo.Reserva r ON r.idReserva = t.idReserva
WHERE t.tipoOperacion = N'ReservaAbonada' AND r.estadoPago = N'Devuelto' AND t.estadoPago <> N'Devuelto';
COMMIT;
GO
