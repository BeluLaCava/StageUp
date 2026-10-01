-- =============================================================================
-- Volumen_CargarDatos.sql
--
-- Carga datos de VOLUMEN para medir el rendimiento (ítem 36 de la revisión).
-- NO es un script numerado: EjecutarTodosLosScripts.ps1 no lo ejecuta nunca.
-- Se corre a mano, solo en una base de prueba, y se deshace con
-- Volumen_LimpiarDatos.sql.
--
-- Cómo correrlo (desde la carpeta Database):
--   sqlcmd -S .\SQLEXPRESS -d StageUp -E -f 65001 -b -i Volumen_CargarDatos.sql
-- o abrirlo en SSMS sobre la base StageUp y ejecutarlo (F5).
--
-- Todo lo que crea queda marcado para poder borrarlo sin tocar datos reales:
--   - usuarios con correo volumen.<algo>@stageup.test
--   - espacios con nombre que empieza con "[Volumen]"
--   - registros de bitácora con origenOperacion = 'CargaVolumen'
-- Los usuarios de volumen tienen la misma contraseña que los usuarios demo
-- del script 25 (por ejemplo marcos.gestor@stageup.test).
--
-- Cantidades por defecto (se pueden cambiar en las variables de abajo):
--   200 gestores, 3.000 clientes, 1.000 espacios, 60.000 reservas
--   (con sus pagos, movimientos de cuenta corriente y calificaciones),
--   1.000 actividades, 3.000 participantes, 3.000 tickets de soporte,
--   150.000 notificaciones y 300.000 registros de bitácora.
-- Tarda entre uno y tres minutos según la PC.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @mensaje NVARCHAR(400);

DECLARE @cantidadGestores        INT = 200;
DECLARE @cantidadClientes        INT = 3000;
DECLARE @espaciosPorGestor       INT = 5;
DECLARE @cantidadReservas        INT = 60000;
DECLARE @cantidadNotificaciones  INT = 150000;
DECLARE @cantidadRegistros       INT = 300000;
DECLARE @cantidadTickets         INT = 3000;
DECLARE @participantesPorGestor  INT = 15;

-- Misma contraseña que los usuarios demo del script 25.
DECLARE @passwordHash NVARCHAR(510) = N'100000.4UyTqCiktr0QpVcUEo1G+Q==.zUDI6gn9wRbCO3MDyZaXdOCahRMYvqNLic3+LCBtXY0=';
DECLARE @ahora DATETIME = GETDATE();
DECLARE @hoy DATE = CAST(GETDATE() AS DATE);
DECLARE @inicio DATETIME2 = SYSDATETIME();

IF EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico LIKE N'volumen.%@stageup.test')
BEGIN
    RAISERROR(N'Ya hay datos de volumen cargados. Correr primero Volumen_LimpiarDatos.sql.', 16, 1);
    RETURN;
END

IF @cantidadTickets > @cantidadReservas SET @cantidadTickets = @cantidadReservas;

-- -----------------------------------------------------------------------------
-- Tabla de números 1..N (sirve para generar filas sin cursores).
-- -----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Numeros') IS NOT NULL DROP TABLE #Numeros;
CREATE TABLE #Numeros (n INT NOT NULL PRIMARY KEY);

DECLARE @maximo INT = (SELECT MAX(v) FROM (VALUES
    (@cantidadGestores), (@cantidadClientes), (@cantidadGestores * @espaciosPorGestor), (@cantidadReservas),
    (@cantidadNotificaciones), (@cantidadRegistros), (@cantidadTickets), (@cantidadGestores * @participantesPorGestor)) AS t(v));

INSERT INTO #Numeros (n)
SELECT TOP (@maximo) ROW_NUMBER() OVER (ORDER BY (SELECT NULL))
FROM sys.all_objects a CROSS JOIN sys.all_objects b CROSS JOIN sys.all_objects c;

PRINT CONCAT(N'Números generados: ', @maximo);

-- -----------------------------------------------------------------------------
-- 1. Usuarios externos: gestores y clientes.
-- -----------------------------------------------------------------------------
INSERT INTO dbo.UsuarioExterno
    (nombre, apellido, correoElectronico, passwordHash, telefono, estadoCuenta, perfilUsuario,
     aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos, fechaAlta, fechaActivacion, activo)
SELECT N'Gestor', CONCAT(N'Volumen ', n), CONCAT(N'volumen.gestor', n, N'@stageup.test'), @passwordHash, NULL,
       N'Activa', N'GestorEspacios', 1, 1, DATEADD(DAY, -600, @ahora), DATEADD(DAY, -600, @ahora), DATEADD(DAY, -600, @ahora), 1
FROM #Numeros WHERE n <= @cantidadGestores;

INSERT INTO dbo.UsuarioExterno
    (nombre, apellido, correoElectronico, passwordHash, telefono, estadoCuenta, perfilUsuario,
     aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos, fechaAlta, fechaActivacion, activo)
SELECT N'Cliente', CONCAT(N'Volumen ', n), CONCAT(N'volumen.cliente', n, N'@stageup.test'), @passwordHash, NULL,
       N'Activa', N'ExternoSolicitante', 1, 1, DATEADD(DAY, -(n % 600), @ahora), DATEADD(DAY, -(n % 600), @ahora),
       DATEADD(DAY, -(n % 600), @ahora), 1
FROM #Numeros WHERE n <= @cantidadClientes;

IF OBJECT_ID('tempdb..#Gestores') IS NOT NULL DROP TABLE #Gestores;
IF OBJECT_ID('tempdb..#Clientes') IS NOT NULL DROP TABLE #Clientes;

SELECT ROW_NUMBER() OVER (ORDER BY idUsuarioExterno) AS rn, idUsuarioExterno AS id
INTO #Gestores
FROM dbo.UsuarioExterno WHERE correoElectronico LIKE N'volumen.gestor%@stageup.test';

SELECT ROW_NUMBER() OVER (ORDER BY idUsuarioExterno) AS rn, idUsuarioExterno AS id
INTO #Clientes
FROM dbo.UsuarioExterno WHERE correoElectronico LIKE N'volumen.cliente%@stageup.test';

CREATE UNIQUE CLUSTERED INDEX IX_G ON #Gestores (rn);
CREATE UNIQUE CLUSTERED INDEX IX_C ON #Clientes (rn);

SET @mensaje = CONCAT(N'Usuarios: ', (SELECT COUNT(*) FROM #Gestores), N' gestores y ', (SELECT COUNT(*) FROM #Clientes), N' clientes.');
PRINT @mensaje;

-- -----------------------------------------------------------------------------
-- 2. Espacios con ficha, equipamiento, franjas semanales y foto.
--    Uno de cada diez queda Pausado (no publicado).
-- -----------------------------------------------------------------------------
DECLARE @cantidadEspacios INT = @cantidadGestores * @espaciosPorGestor;

INSERT INTO dbo.EspacioArtistico
    (idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio, estadoEspacio, publicado, activo, fechaAlta, fechaPublicacion)
SELECT g.id,
       CONCAT(N'[Volumen] Espacio ', x.n),
       N'Espacio generado por la carga de volumen para medir el rendimiento del catálogo y las búsquedas.',
       CASE x.n % 5 WHEN 0 THEN N'Teatro' WHEN 1 THEN N'Sala de ensayo' WHEN 2 THEN N'Estudio' WHEN 3 THEN N'Auditorio' ELSE N'Galería' END,
       CASE WHEN x.n % 10 = 0 THEN N'Pausado' ELSE N'Publicado' END,
       CASE WHEN x.n % 10 = 0 THEN 0 ELSE 1 END,
       1, DATEADD(DAY, -590, @ahora), DATEADD(DAY, -590, @ahora)
FROM #Numeros x
JOIN #Gestores g ON g.rn = ((x.n - 1) % @cantidadGestores) + 1
WHERE x.n <= @cantidadEspacios;

IF OBJECT_ID('tempdb..#Espacios') IS NOT NULL DROP TABLE #Espacios;
SELECT ROW_NUMBER() OVER (ORDER BY e.idEspacioArtistico) AS rn, e.idEspacioArtistico AS id, e.idUsuarioGestor AS idGestor,
       CAST(0 AS DECIMAL(10,2)) AS precioHora, CAST(N'ARS' AS NVARCHAR(3)) AS moneda
INTO #Espacios
FROM dbo.EspacioArtistico e
WHERE e.nombreEspacio LIKE N'\[Volumen\]%' ESCAPE N'\';

UPDATE #Espacios
SET moneda = CASE WHEN rn % 10 = 3 THEN N'USD' ELSE N'ARS' END,
    precioHora = CASE WHEN rn % 10 = 3 THEN 20 + (rn % 30) ELSE 5000 + (rn % 50) * 1000 END;

CREATE UNIQUE CLUSTERED INDEX IX_E ON #Espacios (rn);

INSERT INTO dbo.FichaEspacio
    (idEspacioArtistico, fotoRuta, provincia, ciudad, direccion, capacidadMaxima, precioHora, moneda,
     tipoPiso, detalleEquipamiento, fechaUltimaModificacion)
SELECT e.id, NULL,
       CASE e.rn % 4 WHEN 0 THEN N'Buenos Aires' WHEN 1 THEN N'Córdoba' WHEN 2 THEN N'Santa Fe' ELSE N'Mendoza' END,
       CASE e.rn % 8 WHEN 0 THEN N'CABA' WHEN 1 THEN N'Córdoba' WHEN 2 THEN N'Rosario' WHEN 3 THEN N'Mendoza'
                     WHEN 4 THEN N'La Plata' WHEN 5 THEN N'Villa María' WHEN 6 THEN N'Santa Fe' ELSE N'Godoy Cruz' END,
       CONCAT(N'Calle de prueba ', e.rn),
       10 + (e.rn % 40) * 10, e.precioHora, e.moneda, N'Madera',
       N'Equipamiento de prueba generado por la carga de volumen.', @ahora
FROM #Espacios e;

INSERT INTO dbo.FichaEspacioEquipamiento (idEspacioArtistico, codigoEquipamiento)
SELECT e.id, eq.codigo
FROM #Espacios e
JOIN (VALUES (0, N'SONIDO'), (1, N'ESPEJOS'), (2, N'ILUMINACION'), (3, N'ESCENARIO'), (4, N'INSTRUMENTOS')) AS eq(k, codigo)
    ON (e.rn + eq.k) % 3 <> 0;

INSERT INTO dbo.FranjaEspacio (idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado)
SELECT e.id, d.diaSemana, NULL, 540, 1320, 0
FROM #Espacios e
CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6)) AS d(diaSemana);

SET @mensaje = CONCAT(N'Espacios: ', (SELECT COUNT(*) FROM #Espacios));
PRINT @mensaje;

-- -----------------------------------------------------------------------------
-- 3. Reservas: mezcla de estados con fechas pasadas y futuras.
--    Ninguna queda en un estado que el sistema tenga que procesar apenas se
--    abre la web (no hay aceptadas vencidas ni pagos ya vencidos).
--    resto n % 20:  0-3 Pendiente (futura)
--                   4-6 Aceptada (futura; mitad pagada, mitad con pago pendiente)
--                   7-8 Rechazada
--                   9-10 Cancelada
--                  11-19 Finalizada (pasada y pagada)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#ReservasNuevas') IS NOT NULL DROP TABLE #ReservasNuevas;
SELECT x.n,
       e.id AS idEspacio, e.idGestor, e.precioHora, e.moneda,
       c.id AS idCliente,
       CASE WHEN x.n % 20 <= 3 THEN N'Pendiente'
            WHEN x.n % 20 <= 6 THEN N'Aceptada'
            WHEN x.n % 20 <= 8 THEN N'Rechazada'
            WHEN x.n % 20 <= 10 THEN N'Cancelada'
            ELSE N'Finalizada' END AS estado,
       CASE WHEN x.n % 20 <= 6 THEN DATEADD(DAY, 2 + (x.n % 90), @hoy)
            ELSE DATEADD(DAY, -(3 + (x.n % 540)), @hoy) END AS fecha,
       540 + (x.n % 10) * 60 AS minutoDesde,
       60 * (1 + x.n % 3) AS duracion
INTO #ReservasNuevas
FROM #Numeros x
JOIN #Espacios e ON e.rn = ((x.n * 7 - 1) % @cantidadEspacios) + 1
JOIN #Clientes c ON c.rn = ((x.n - 1) % @cantidadClientes) + 1
WHERE x.n <= @cantidadReservas;

INSERT INTO dbo.Reserva
    (idEspacioArtistico, idUsuarioExternoSolicitante, fechaSolicitada, comentarioSolicitante, estadoReserva,
     comentarioResolucion, fechaCreacion, fechaResolucion, fechaUltimaModificacion,
     minutoDesde, minutoHasta, precioHoraPactado, moneda, importeEstimado, fechaCancelacion, fechaFinalizacion,
     recordatorioEnviado, avisoFinalizacionEnviado, estadoPago, fechaLimitePago, fechaPago)
SELECT r.idEspacio, r.idCliente, r.fecha,
       CONCAT(N'[Volumen] Reserva de prueba ', r.n),
       r.estado,
       CASE r.estado WHEN N'Rechazada' THEN N'Sin disponibilidad para esa fecha.' WHEN N'Aceptada' THEN N'Te esperamos.' ELSE NULL END,
       -- La solicitud se crea unos días antes de la fecha reservada (nunca en el futuro).
       CASE WHEN DATEADD(DAY, -10, CAST(r.fecha AS DATETIME)) > @ahora THEN DATEADD(MINUTE, -(r.n % 1440), @ahora)
            ELSE DATEADD(DAY, -10, CAST(r.fecha AS DATETIME)) END,
       CASE WHEN r.estado IN (N'Aceptada', N'Rechazada', N'Finalizada') THEN
            CASE WHEN DATEADD(DAY, -8, CAST(r.fecha AS DATETIME)) > @ahora THEN @ahora ELSE DATEADD(DAY, -8, CAST(r.fecha AS DATETIME)) END
       END,
       @ahora,
       r.minutoDesde, r.minutoDesde + r.duracion, r.precioHora, r.moneda,
       r.precioHora * r.duracion / 60,
       CASE WHEN r.estado = N'Cancelada' THEN
            CASE WHEN DATEADD(DAY, -5, CAST(r.fecha AS DATETIME)) > @ahora THEN @ahora ELSE DATEADD(DAY, -5, CAST(r.fecha AS DATETIME)) END
       END,
       CASE WHEN r.estado = N'Finalizada' THEN DATEADD(DAY, 1, CAST(r.fecha AS DATETIME)) END,
       CASE WHEN r.estado = N'Finalizada' THEN 1 ELSE 0 END,
       CASE WHEN r.estado = N'Finalizada' THEN 1 ELSE 0 END,
       CASE WHEN r.estado = N'Finalizada' THEN N'Pagado'
            WHEN r.estado = N'Aceptada' AND r.n % 2 = 0 THEN N'Pagado'
            WHEN r.estado = N'Aceptada' THEN N'Pendiente'
            ELSE N'NoRequerido' END,
       CASE WHEN r.estado = N'Aceptada' AND r.n % 2 = 1 THEN DATEADD(HOUR, 47, @ahora) END,
       CASE WHEN r.estado = N'Finalizada' THEN DATEADD(DAY, -7, CAST(r.fecha AS DATETIME))
            WHEN r.estado = N'Aceptada' AND r.n % 2 = 0 THEN DATEADD(MINUTE, -(r.n % 600), @ahora) END
FROM #ReservasNuevas r
ORDER BY r.n;

IF OBJECT_ID('tempdb..#Reservas') IS NOT NULL DROP TABLE #Reservas;
SELECT r.idReserva, rn.n, rn.idEspacio, rn.idGestor, rn.idCliente, r.estadoReserva, r.estadoPago,
       r.importeEstimado, r.moneda, r.fechaPago, r.fechaFinalizacion
INTO #Reservas
FROM dbo.Reserva r
JOIN #Espacios e ON e.id = r.idEspacioArtistico
JOIN #ReservasNuevas rn ON r.comentarioSolicitante = CONCAT(N'[Volumen] Reserva de prueba ', rn.n);

CREATE UNIQUE CLUSTERED INDEX IX_R ON #Reservas (n);

SET @mensaje = CONCAT(N'Reservas: ', (SELECT COUNT(*) FROM #Reservas));
PRINT @mensaje;

-- -----------------------------------------------------------------------------
-- 4. Pagos aprobados y movimientos de cuenta corriente (igual que
--    sp_Pago_RegistrarReserva: cargo y pago del cliente, ingreso y comisión
--    del 10% para el gestor).
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Pago
    (idUsuarioExterno, idReserva, concepto, moneda, importeTotal, importeTarjeta, importeSaldo, estado,
     marcaTarjeta, ultimosDigitos, codigoAutorizacion, porcentajeComisionPlataforma, importeComisionPlataforma, fechaPago)
SELECT r.idCliente, r.idReserva, N'Reserva', r.moneda, r.importeEstimado, r.importeEstimado, 0, N'Aprobado',
       N'Visa', N'4242', CONCAT(N'VOL', r.n), 10, ROUND(r.importeEstimado * 0.10, 2), r.fechaPago
FROM #Reservas r
WHERE r.estadoPago = N'Pagado';

IF OBJECT_ID('tempdb..#Pagos') IS NOT NULL DROP TABLE #Pagos;
SELECT p.idPago, r.idReserva, r.idCliente, r.idGestor, r.importeEstimado AS total, r.moneda, r.fechaPago,
       ROUND(r.importeEstimado * 0.10, 2) AS comision
INTO #Pagos
FROM dbo.Pago p
JOIN #Reservas r ON r.idReserva = p.idReserva;

INSERT INTO dbo.MovimientoCuentaCorriente (idUsuarioExterno, rolCuenta, tipoMovimiento, importe, moneda, descripcion, idReserva, idPago, fechaMovimiento)
SELECT idCliente, N'Cliente', N'CargoReserva', -total, moneda, CONCAT(N'[Volumen] Cargo de la reserva #', idReserva), idReserva, idPago, fechaPago FROM #Pagos
UNION ALL
SELECT idCliente, N'Cliente', N'PagoTarjeta', total, moneda, CONCAT(N'[Volumen] Pago con tarjeta de la reserva #', idReserva), idReserva, idPago, fechaPago FROM #Pagos
UNION ALL
SELECT idGestor, N'Gestor', N'IngresoReserva', total, moneda, CONCAT(N'[Volumen] Ingreso por la reserva #', idReserva), idReserva, idPago, fechaPago FROM #Pagos
UNION ALL
SELECT idGestor, N'Gestor', N'ComisionPlataforma', -comision, moneda, CONCAT(N'[Volumen] Comisión StageUp de la reserva #', idReserva), idReserva, idPago, fechaPago FROM #Pagos
WHERE comision > 0;

SET @mensaje = CONCAT(N'Pagos: ', (SELECT COUNT(*) FROM #Pagos));
PRINT @mensaje;

-- -----------------------------------------------------------------------------
-- 5. Calificaciones de reservas finalizadas (al espacio y al solicitante).
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Calificacion (idReserva, idUsuarioAutor, tipoCalificacion, puntaje, comentario, fechaAlta, activo)
SELECT r.idReserva, r.idCliente, N'Espacio', 3 + (r.n % 3),
       CASE r.n % 3 WHEN 0 THEN N'Correcto, cumplió con lo pactado.' WHEN 1 THEN N'Muy buen espacio, volvería.' ELSE N'Excelente atención y equipamiento.' END,
       DATEADD(DAY, 1, r.fechaFinalizacion), 1
FROM #Reservas r
WHERE r.estadoReserva = N'Finalizada' AND r.n % 3 <> 0;

INSERT INTO dbo.Calificacion (idReserva, idUsuarioAutor, tipoCalificacion, puntaje, comentario, fechaAlta, activo)
SELECT r.idReserva, r.idGestor, N'UsuarioSolicitante', 4 + (r.n % 2),
       N'Puntual y cuidadoso con el espacio.', DATEADD(DAY, 2, r.fechaFinalizacion), 1
FROM #Reservas r
WHERE r.estadoReserva = N'Finalizada' AND r.n % 2 = 0;

PRINT N'Calificaciones cargadas.';

-- -----------------------------------------------------------------------------
-- 6. Participantes y actividades (la mitad de los espacios publicados tiene
--    una actividad semanal y una de fecha única, con sus bloqueos).
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Participante (idUsuarioGestor, nombre, apellido, dni, notas, activo, fechaCreacion)
SELECT g.id, N'Participante', CONCAT(N'Volumen ', x.n), CAST(40000000 + x.n AS NVARCHAR(20)), NULL, 1, DATEADD(DAY, -300, @ahora)
FROM #Numeros x
JOIN #Gestores g ON g.rn = ((x.n - 1) % @cantidadGestores) + 1
WHERE x.n <= @cantidadGestores * @participantesPorGestor;

INSERT INTO dbo.Actividad
    (idEspacioArtistico, nombre, tipo, modoRecurrencia, fecha, semanaDelMes, diaSemanaMensual,
     minutoDesde, minutoHasta, cupoMaximo, participantesEstimados, notas, activa, fechaCreacion)
SELECT e.id, CONCAT(N'[Volumen] Taller semanal ', e.rn), N'Taller', N'Semanal', NULL, NULL, NULL,
       1200, 1320, 20, 12, NULL, 1, DATEADD(DAY, -200, @ahora)
FROM #Espacios e WHERE e.rn % 2 = 1 AND e.rn % 10 <> 0
UNION ALL
SELECT e.id, CONCAT(N'[Volumen] Muestra ', e.rn), N'Muestra', N'Fecha', DATEADD(DAY, 20 + (e.rn % 60), @hoy), NULL, NULL,
       1080, 1200, 50, 30, NULL, 1, DATEADD(DAY, -30, @ahora)
FROM #Espacios e WHERE e.rn % 2 = 1 AND e.rn % 10 <> 0;

IF OBJECT_ID('tempdb..#Actividades') IS NOT NULL DROP TABLE #Actividades;
SELECT a.idActividad, a.idEspacioArtistico, a.modoRecurrencia, a.fecha, a.minutoDesde, a.minutoHasta, e.idGestor, e.rn
INTO #Actividades
FROM dbo.Actividad a
JOIN #Espacios e ON e.id = a.idEspacioArtistico;

INSERT INTO dbo.ActividadDiaSemana (idActividad, diaSemana)
SELECT a.idActividad, d.diaSemana
FROM #Actividades a
CROSS JOIN (VALUES (2), (4)) AS d(diaSemana)
WHERE a.modoRecurrencia = N'Semanal';

INSERT INTO dbo.FranjaEspacio (idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado, origen, idActividad)
SELECT a.idEspacioArtistico, d.diaSemana, NULL, a.minutoDesde, a.minutoHasta, 1, N'Actividad', a.idActividad
FROM #Actividades a
CROSS JOIN (VALUES (2), (4)) AS d(diaSemana)
WHERE a.modoRecurrencia = N'Semanal'
UNION ALL
SELECT a.idEspacioArtistico, NULL, a.fecha, a.minutoDesde, a.minutoHasta, 1, N'Actividad', a.idActividad
FROM #Actividades a
WHERE a.modoRecurrencia = N'Fecha';

INSERT INTO dbo.ActividadParticipante (idActividad, idParticipante, fechaAsociacion)
SELECT a.idActividad, p.idParticipante, DATEADD(DAY, -20, @ahora)
FROM #Actividades a
CROSS APPLY (
    SELECT TOP (8) pa.idParticipante
    FROM dbo.Participante pa
    WHERE pa.idUsuarioGestor = a.idGestor
    ORDER BY pa.idParticipante
) p;

SET @mensaje = CONCAT(N'Actividades: ', (SELECT COUNT(*) FROM #Actividades));
PRINT @mensaje;

-- -----------------------------------------------------------------------------
-- 7. Tickets de soporte (cada uno asociado a una reserva del mismo cliente)
--    con su conversación.
-- -----------------------------------------------------------------------------
DECLARE @idInterno INT = (SELECT TOP (1) idUsuarioInterno FROM dbo.UsuarioInterno ORDER BY idUsuarioInterno);

INSERT INTO dbo.Ticket
    (idUsuarioExterno, idReservaAsociada, categoria, asunto, estado, idUsuarioInternoAsignado,
     fechaCreacion, fechaUltimaActividad, fechaCierre)
SELECT r.idCliente,
       CASE WHEN r.n % 2 = 0 THEN r.idReserva END,
       CASE r.n % 5 WHEN 0 THEN N'Reserva' WHEN 1 THEN N'Pago' WHEN 2 THEN N'Cuenta' WHEN 3 THEN N'Espacio' ELSE N'Otro' END,
       CONCAT(N'[Volumen] Consulta de prueba ', r.n),
       CASE r.n % 3 WHEN 0 THEN N'Abierto' WHEN 1 THEN N'Respondido' ELSE N'Cerrado' END,
       CASE WHEN r.n % 3 <> 0 THEN @idInterno END,
       DATEADD(DAY, -(2 + r.n % 300), @ahora),
       DATEADD(DAY, -(1 + r.n % 300), @ahora),
       CASE WHEN r.n % 3 = 2 THEN DATEADD(DAY, -(1 + r.n % 300), @ahora) END
FROM #Reservas r
WHERE r.n <= @cantidadTickets;

IF OBJECT_ID('tempdb..#Tickets') IS NOT NULL DROP TABLE #Tickets;
SELECT t.idTicket, t.idUsuarioExterno, t.estado, t.fechaCreacion
INTO #Tickets
FROM dbo.Ticket t
JOIN #Clientes c ON c.id = t.idUsuarioExterno
WHERE t.asunto LIKE N'\[Volumen\]%' ESCAPE N'\';

INSERT INTO dbo.TicketMensaje (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio)
SELECT idTicket, idUsuarioExterno, NULL, N'Hola, tengo una consulta sobre mi reserva.', fechaCreacion FROM #Tickets
UNION ALL
SELECT idTicket, NULL, @idInterno, N'Hola, gracias por escribirnos. Ya lo estamos revisando.', DATEADD(HOUR, 3, fechaCreacion)
FROM #Tickets WHERE estado <> N'Abierto' AND @idInterno IS NOT NULL
UNION ALL
SELECT idTicket, idUsuarioExterno, NULL, N'Perfecto, muchas gracias.', DATEADD(HOUR, 20, fechaCreacion)
FROM #Tickets WHERE estado = N'Cerrado';

SET @mensaje = CONCAT(N'Tickets: ', (SELECT COUNT(*) FROM #Tickets));
PRINT @mensaje;

-- -----------------------------------------------------------------------------
-- 8. Notificaciones (repartidas entre clientes y gestores; tres de cada
--    cuatro ya leídas).
-- -----------------------------------------------------------------------------
INSERT INTO dbo.Notificacion (idUsuarioExterno, tipo, mensaje, urlDestino, leida, fechaCreacion)
SELECT CASE WHEN x.n % 4 = 0 THEN g.id ELSE c.id END,
       CASE x.n % 6 WHEN 0 THEN N'SolicitudReserva' WHEN 1 THEN N'ReservaAceptada' WHEN 2 THEN N'ReservaRechazada'
                    WHEN 3 THEN N'RecordatorioReserva' WHEN 4 THEN N'PagoAprobado' ELSE N'RespuestaTicket' END,
       CONCAT(N'[Volumen] Notificación de prueba ', x.n),
       CASE WHEN x.n % 4 = 0 THEN N'~/SolicitudesRecibidas.aspx' ELSE N'~/MisReservas.aspx' END,
       CASE WHEN x.n % 4 = 3 THEN 0 ELSE 1 END,
       DATEADD(MINUTE, -(x.n % 1440), DATEADD(DAY, -(x.n % 365), @ahora))
FROM #Numeros x
JOIN #Clientes c ON c.rn = ((x.n - 1) % @cantidadClientes) + 1
JOIN #Gestores g ON g.rn = ((x.n - 1) % @cantidadGestores) + 1
WHERE x.n <= @cantidadNotificaciones;

PRINT CONCAT(N'Notificaciones: ', @cantidadNotificaciones);

-- -----------------------------------------------------------------------------
-- 9. Bitácora: 80% operaciones de usuarios externos, 15% de un usuario
--    interno (si existe) y 5% del sistema.
-- -----------------------------------------------------------------------------
INSERT INTO dbo.RegistroActividad
    (idUsuarioExternoResponsable, idUsuarioInternoResponsable, tipoOperacion, tipoEntidadAfectada,
     idEntidadAfectada, descripcionOperacion, fechaOperacion, origenOperacion)
SELECT CASE WHEN x.n % 20 < 16 THEN CASE WHEN x.n % 2 = 0 THEN c.id ELSE g.id END END,
       CASE WHEN x.n % 20 BETWEEN 16 AND 18 THEN @idInterno END,
       CASE x.n % 6 WHEN 0 THEN N'LOGIN' WHEN 1 THEN N'ALTA' WHEN 2 THEN N'MODIFICACION'
                    WHEN 3 THEN N'BAJA' WHEN 4 THEN N'ACTIVACION' ELSE N'PAGO' END,
       CASE x.n % 5 WHEN 0 THEN N'Reserva' WHEN 1 THEN N'EspacioArtistico' WHEN 2 THEN N'UsuarioExterno'
                    WHEN 3 THEN N'Ticket' ELSE N'Pago' END,
       x.n % 5000,
       CONCAT(N'[Volumen] Operación de prueba ', x.n),
       DATEADD(MINUTE, -(x.n % 1440), DATEADD(DAY, -(x.n % 540), @ahora)),
       N'CargaVolumen'
FROM #Numeros x
JOIN #Clientes c ON c.rn = ((x.n - 1) % @cantidadClientes) + 1
JOIN #Gestores g ON g.rn = ((x.n - 1) % @cantidadGestores) + 1
WHERE x.n <= @cantidadRegistros;

PRINT CONCAT(N'Registros de bitácora: ', @cantidadRegistros);

-- Estadísticas al día para que el optimizador vea los volúmenes nuevos.
UPDATE STATISTICS dbo.UsuarioExterno;
UPDATE STATISTICS dbo.EspacioArtistico;
UPDATE STATISTICS dbo.FichaEspacio;
UPDATE STATISTICS dbo.FranjaEspacio;
UPDATE STATISTICS dbo.Reserva;
UPDATE STATISTICS dbo.Calificacion;
UPDATE STATISTICS dbo.Pago;
UPDATE STATISTICS dbo.MovimientoCuentaCorriente;
UPDATE STATISTICS dbo.Notificacion;
UPDATE STATISTICS dbo.RegistroActividad;
UPDATE STATISTICS dbo.Ticket;

DROP TABLE #Numeros, #Gestores, #Clientes, #Espacios, #ReservasNuevas, #Reservas, #Pagos, #Actividades, #Tickets;

PRINT CONCAT(N'Carga de volumen terminada en ', DATEDIFF(SECOND, @inicio, SYSDATETIME()), N' segundos.');
GO
