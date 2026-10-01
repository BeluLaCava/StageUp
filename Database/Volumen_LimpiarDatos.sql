-- =============================================================================
-- Volumen_LimpiarDatos.sql
--
-- Borra todo lo que cargó Volumen_CargarDatos.sql (ítem 36 de la revisión).
-- NO es un script numerado: EjecutarTodosLosScripts.ps1 no lo ejecuta nunca.
--
-- Cómo correrlo (desde la carpeta Database):
--   sqlcmd -S .\SQLEXPRESS -d StageUp -E -f 65001 -b -i Volumen_LimpiarDatos.sql
-- o abrirlo en SSMS sobre la base StageUp y ejecutarlo (F5).
--
-- Qué borra:
--   - los usuarios con correo volumen.<algo>@stageup.test, sus espacios y
--     todo lo que cuelga de ellos (reservas, pagos, comprobantes, movimientos
--     de cuenta corriente, calificaciones, actividades, participantes,
--     tickets, notificaciones, respuestas de encuestas, códigos);
--   - las reservas de cualquier usuario hechas sobre un espacio de volumen
--     (por si se probó reservar uno de esos espacios con un usuario real);
--   - los registros de bitácora de usuarios de volumen y los que tienen
--     origenOperacion = 'CargaVolumen'.
-- Si un ticket de un usuario real quedó asociado a una reserva de volumen,
-- el ticket se conserva y solo se le quita la reserva asociada.
-- Se puede correr las veces que haga falta (si no hay nada, no hace nada).
-- =============================================================================
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

-- Cuenta corriente, comprobantes y pagos.
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

-- Soporte.
UPDATE dbo.Ticket SET idReservaAsociada = NULL
WHERE idReservaAsociada IN (SELECT id FROM #R) AND idUsuarioExterno NOT IN (SELECT id FROM #U);

DELETE FROM dbo.TicketMensaje
WHERE idTicket IN (SELECT idTicket FROM dbo.Ticket WHERE idUsuarioExterno IN (SELECT id FROM #U))
   OR idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Mensajes de tickets: ', @@ROWCOUNT);

DELETE FROM dbo.Ticket WHERE idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Tickets: ', @@ROWCOUNT);

-- Calificaciones y reservas.
DELETE FROM dbo.Calificacion WHERE idReserva IN (SELECT id FROM #R) OR idUsuarioAutor IN (SELECT id FROM #U);
PRINT CONCAT(N'Calificaciones: ', @@ROWCOUNT);

DELETE FROM dbo.Reserva WHERE idReserva IN (SELECT id FROM #R);
PRINT CONCAT(N'Reservas: ', @@ROWCOUNT);

-- Respuestas de encuestas (el detalle se borra en cascada).
DELETE FROM dbo.RespuestaEncuesta WHERE idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Respuestas de encuestas: ', @@ROWCOUNT);

COMMIT TRANSACTION;

-- Notificaciones y bitácora: en tandas, para no inflar el log de la base.
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

-- Actividades: primero sus bloqueos (FranjaEspacio apunta a Actividad sin
-- cascada); los días y los participantes asociados se borran en cascada.
DELETE FROM dbo.FranjaEspacio
WHERE idEspacioArtistico IN (SELECT id FROM #E)
   OR idActividad IN (SELECT idActividad FROM dbo.Actividad WHERE idEspacioArtistico IN (SELECT id FROM #E));

DELETE FROM dbo.Actividad WHERE idEspacioArtistico IN (SELECT id FROM #E);
PRINT CONCAT(N'Actividades: ', @@ROWCOUNT);

DELETE FROM dbo.ActividadParticipante
WHERE idParticipante IN (SELECT idParticipante FROM dbo.Participante WHERE idUsuarioGestor IN (SELECT id FROM #U));

DELETE FROM dbo.Participante WHERE idUsuarioGestor IN (SELECT id FROM #U);
PRINT CONCAT(N'Participantes: ', @@ROWCOUNT);

-- Espacios (equipamiento, fotos y franjas se borran en cascada con la ficha).
DELETE FROM dbo.FichaEspacio WHERE idEspacioArtistico IN (SELECT id FROM #E);
DELETE FROM dbo.EspacioArtistico WHERE idEspacioArtistico IN (SELECT id FROM #E);
PRINT CONCAT(N'Espacios: ', @@ROWCOUNT);

-- Usuarios.
DELETE FROM dbo.CodigoActivacion WHERE idUsuarioExterno IN (SELECT id FROM #U);
DELETE FROM dbo.CodigoRecuperacion WHERE idUsuarioExterno IN (SELECT id FROM #U);
DELETE FROM dbo.UsuarioExterno WHERE idUsuarioExterno IN (SELECT id FROM #U);
PRINT CONCAT(N'Usuarios: ', @@ROWCOUNT);

COMMIT TRANSACTION;

DROP TABLE #U, #E, #R;

PRINT CONCAT(N'Limpieza de volumen terminada en ', DATEDIFF(SECOND, @inicio, SYSDATETIME()), N' segundos.');
GO
