

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @mensaje NVARCHAR(400);

DECLARE @inicio DATETIME2 = SYSDATETIME();
DECLARE @filas INT;

IF OBJECT_ID('tempdb..#U') IS NOT NULL DROP TABLE #U;
IF OBJECT_ID('tempdb..#E') IS NOT NULL DROP TABLE #E;
IF OBJECT_ID('tempdb..#R') IS NOT NULL DROP TABLE #R;

CREATE TABLE #U (id INT NOT NULL PRIMARY KEY);
CREATE TABLE #E (id INT NOT NULL PRIMARY KEY);
CREATE TABLE #R (id INT NOT NULL PRIMARY KEY);

INSERT INTO #U (id)
SELECT idUsuarioExterno FROM dbo.UsuarioExterno WHERE correoElectronico LIKE N'volumen.%@stageup.test';

INSERT INTO #E (id)
SELECT idEspacioArtistico FROM dbo.EspacioArtistico
WHERE idUsuarioGestor IN (SELECT id FROM #U)
   OR nombreEspacio LIKE N'\[Volumen\]%' ESCAPE N'\';

INSERT INTO #R (id)
SELECT idReserva FROM dbo.Reserva
WHERE idEspacioArtistico IN (SELECT id FROM #E)
   OR idUsuarioExternoSolicitante IN (SELECT id FROM #U);

SET @mensaje = CONCAT(N'A borrar: ', (SELECT COUNT(*) FROM #U), N' usuarios, ', (SELECT COUNT(*) FROM #E), N' espacios, ',
             (SELECT COUNT(*) FROM #R), N' reservas (y lo que depende de ellos).');
PRINT @mensaje;

BEGIN TRANSACTION;

DELETE m FROM dbo.MovimientoCuentaCorriente m
WHERE m.idUsuarioExterno IN (SELECT id FROM #U)
   OR m.idReserva IN (SELECT id FROM #R)
   OR m.idPago IN (SELECT idPago FROM dbo.Pago WHERE idUsuarioExterno IN (SELECT id FROM #U) OR idReserva IN (SELECT id FROM #R))
   OR m.idComprobante IN (SELECT idComprobante FROM dbo.Comprobante WHERE idUsuarioExterno IN (SELECT id FROM #U) OR idReserva IN (SELECT id FROM #R));
PRINT CONCAT(N'Movimientos de cuenta corriente: ', @@ROWCOUNT);

DELETE FROM dbo.Comprobante WHERE idUsuarioExterno IN (SELECT id FROM #U) OR idReserva IN (SELECT id FROM #R);
PRINT CONCAT(N'Comprobantes: ', @@ROWCOUNT);

DELETE FROM dbo.Pago WHERE idUsuarioExterno IN (SELECT id FROM #U) OR idReserva IN (SELECT id FROM #R);
PRINT CONCAT(N'Pagos: ', @@ROWCOUNT);

UPDATE dbo.Ticket SET idReservaAsociada = NULL
WHERE idReservaAsociada IN (SELECT id FROM #R) AND idUsuarioExterno NOT IN (SELECT id FROM #U);

IF OBJECT_ID(N'dbo.TicketActualizacion', N'U') IS NOT NULL
    DELETE FROM dbo.TicketActualizacion
    WHERE idTicket IN (SELECT idTicket FROM dbo.Ticket WHERE idUsuarioExterno IN (SELECT id FROM #U))
       OR idUsuarioExterno IN (SELECT id FROM #U);

DELETE FROM dbo.TicketMensaje
WHERE idTicket IN (SELECT idTicket FROM dbo.Ticket WHERE idUsuarioExterno IN (SELECT id FROM #U))
   OR idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Mensajes de tickets: ', @@ROWCOUNT);

DELETE FROM dbo.Ticket WHERE idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Tickets: ', @@ROWCOUNT);

DELETE FROM dbo.Calificacion WHERE idReserva IN (SELECT id FROM #R) OR idUsuarioAutor IN (SELECT id FROM #U);
PRINT CONCAT(N'Calificaciones: ', @@ROWCOUNT);

IF OBJECT_ID(N'dbo.MensajeReserva', N'U') IS NOT NULL
    DELETE FROM dbo.MensajeReserva
    WHERE idReserva IN (SELECT id FROM #R) OR idUsuarioExternoEmisor IN (SELECT id FROM #U);

DELETE FROM dbo.Reserva WHERE idReserva IN (SELECT id FROM #R);
PRINT CONCAT(N'Reservas: ', @@ROWCOUNT);

DELETE FROM dbo.RespuestaEncuesta WHERE idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Respuestas de encuestas: ', @@ROWCOUNT);

COMMIT TRANSACTION;

SET @filas = 1;
WHILE @filas > 0
BEGIN
    DELETE TOP (50000) FROM dbo.Notificacion WHERE idUsuarioExterno IN (SELECT id FROM #U);
    SET @filas = @@ROWCOUNT;
END
PRINT N'Notificaciones borradas.';

SET @filas = 1;
WHILE @filas > 0
BEGIN
    DELETE TOP (50000) FROM dbo.RegistroActividad
    WHERE idUsuarioExternoResponsable IN (SELECT id FROM #U)
       OR origenOperacion = N'CargaVolumen';
    SET @filas = @@ROWCOUNT;
END
PRINT N'Registros de bitácora borrados.';

BEGIN TRANSACTION;

DELETE FROM dbo.FranjaEspacio
WHERE idEspacioArtistico IN (SELECT id FROM #E)
   OR idActividad IN (SELECT idActividad FROM dbo.Actividad WHERE idEspacioArtistico IN (SELECT id FROM #E));

DELETE FROM dbo.Actividad WHERE idEspacioArtistico IN (SELECT id FROM #E);
PRINT CONCAT(N'Actividades: ', @@ROWCOUNT);

DELETE FROM dbo.ActividadParticipante
WHERE idParticipante IN (SELECT idParticipante FROM dbo.Participante WHERE idUsuarioGestor IN (SELECT id FROM #U));

DELETE FROM dbo.Participante WHERE idUsuarioGestor IN (SELECT id FROM #U);
PRINT CONCAT(N'Participantes: ', @@ROWCOUNT);

IF OBJECT_ID(N'dbo.PreguntaEspacio', N'U') IS NOT NULL
    DELETE FROM dbo.PreguntaEspacio
    WHERE idEspacioArtistico IN (SELECT id FROM #E) OR idUsuarioExterno IN (SELECT id FROM #U);

DELETE FROM dbo.FichaEspacio WHERE idEspacioArtistico IN (SELECT id FROM #E);
DELETE FROM dbo.EspacioArtistico WHERE idEspacioArtistico IN (SELECT id FROM #E);
PRINT CONCAT(N'Espacios: ', @@ROWCOUNT);

IF OBJECT_ID(N'dbo.ContactoComercial', N'U') IS NOT NULL
    UPDATE dbo.ContactoComercial SET idUsuarioExterno = NULL WHERE idUsuarioExterno IN (SELECT id FROM #U);

IF COL_LENGTH(N'dbo.SuscriptorNewsletter', N'idUsuarioExternoVinculoPendiente') IS NOT NULL
    EXEC sp_executesql N'UPDATE dbo.SuscriptorNewsletter SET idUsuarioExternoVinculoPendiente = NULL, hashTokenVinculo = NULL, vencimientoVinculo = NULL
                         WHERE idUsuarioExternoVinculoPendiente IN (SELECT id FROM #U);';

IF OBJECT_ID(N'dbo.EnvioNewsletterDestinatario', N'U') IS NOT NULL
    DELETE FROM dbo.EnvioNewsletterDestinatario
    WHERE idSuscriptorNewsletter IN (SELECT idSuscriptorNewsletter FROM dbo.SuscriptorNewsletter WHERE idUsuarioExterno IN (SELECT id FROM #U));

IF OBJECT_ID(N'dbo.SuscriptorNewsletter', N'U') IS NOT NULL
    DELETE FROM dbo.SuscriptorNewsletter WHERE idUsuarioExterno IN (SELECT id FROM #U);

DELETE FROM dbo.CodigoActivacion WHERE idUsuarioExterno IN (SELECT id FROM #U);
DELETE FROM dbo.CodigoRecuperacion WHERE idUsuarioExterno IN (SELECT id FROM #U);
DELETE FROM dbo.UsuarioExterno WHERE idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Usuarios: ', @@ROWCOUNT);

COMMIT TRANSACTION;

DROP TABLE #U, #E, #R;

PRINT CONCAT(N'Limpieza de volumen terminada en ', DATEDIFF(SECOND, @inicio, SYSDATETIME()), N' segundos.');
GO
