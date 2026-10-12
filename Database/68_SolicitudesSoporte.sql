IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Ticket', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/38_SoporteHelpdesk.sql.', 1;
END
GO

IF COL_LENGTH(N'dbo.Ticket', N'descripcion') IS NULL
    ALTER TABLE dbo.Ticket ADD descripcion NVARCHAR(2000) NULL;
GO
IF COL_LENGTH(N'dbo.Ticket', N'informacionAdicional') IS NULL
    ALTER TABLE dbo.Ticket ADD informacionAdicional NVARCHAR(1000) NULL;
GO
IF COL_LENGTH(N'dbo.Ticket', N'origen') IS NULL
    ALTER TABLE dbo.Ticket ADD origen NVARCHAR(20) NOT NULL CONSTRAINT DF_Ticket_origen DEFAULT (N'Soporte');
GO
IF COL_LENGTH(N'dbo.Ticket', N'fechaResolucion') IS NULL
    ALTER TABLE dbo.Ticket ADD fechaResolucion DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.Ticket', N'fechaUltimaLecturaUsuario') IS NULL
    ALTER TABLE dbo.Ticket ADD fechaUltimaLecturaUsuario DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.Ticket', N'fechaUltimaLecturaSoporte') IS NULL
    ALTER TABLE dbo.Ticket ADD fechaUltimaLecturaSoporte DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.TicketMensaje', N'esInicial') IS NULL
    ALTER TABLE dbo.TicketMensaje ADD esInicial BIT NOT NULL CONSTRAINT DF_TicketMensaje_esInicial DEFAULT (0);
GO

IF OBJECT_ID(N'dbo.TicketActualizacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketActualizacion
    (
        idTicketActualizacion  INT IDENTITY(1,1) NOT NULL,
        idTicket               INT NOT NULL,
        tipoActualizacion      NVARCHAR(20) NOT NULL,   -- Alta, CambioEstado, Asignacion
        estadoAnterior         NVARCHAR(20) NULL,
        estadoNuevo            NVARCHAR(20) NULL,
        idUsuarioInterno       INT NULL,                -- responsable interno (si lo hubo)
        idUsuarioExterno       INT NULL,                -- o el propio usuario solicitante
        detalle                NVARCHAR(300) NULL,
        fecha                  DATETIME NOT NULL CONSTRAINT DF_TicketActualizacion_fecha DEFAULT (GETDATE()),
        CONSTRAINT PK_TicketActualizacion PRIMARY KEY (idTicketActualizacion),
        CONSTRAINT FK_TicketActualizacion_Ticket FOREIGN KEY (idTicket) REFERENCES dbo.Ticket (idTicket),
        CONSTRAINT FK_TicketActualizacion_UsuarioInterno FOREIGN KEY (idUsuarioInterno) REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT FK_TicketActualizacion_UsuarioExterno FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT CK_TicketActualizacion_tipo CHECK (tipoActualizacion IN (N'Alta', N'CambioEstado', N'Asignacion'))
    );

    CREATE INDEX IX_TicketActualizacion_idTicket ON dbo.TicketActualizacion (idTicket, fecha);
END
GO

-- -----------------------------------------------------------------------------
-- Conversión de los datos anteriores (idempotente).
-- -----------------------------------------------------------------------------
UPDATE dbo.Ticket
SET estado = CASE estado
                 WHEN N'Abierto' THEN N'Pendiente'
                 WHEN N'EnRevision' THEN N'EnProceso'
                 WHEN N'Respondido' THEN N'EnProceso'
                 WHEN N'Cerrado' THEN N'Cerrada'
                 ELSE N'Pendiente'
             END
WHERE estado NOT IN (N'Pendiente', N'EnProceso', N'Resuelta', N'Cerrada');

UPDATE dbo.Ticket
SET categoria = N'Otra'
WHERE categoria NOT IN (N'Reserva', N'Pago', N'Espacio', N'Cuenta', N'Gestor', N'Otra');

-- Antes la descripción se guardaba como primer mensaje del hilo: pasa a la
-- solicitud y ese mensaje queda marcado como inicial (no se borra).
UPDATE t
SET t.descripcion = m.mensaje
FROM dbo.Ticket t
CROSS APPLY
(
    SELECT TOP (1) idTicketMensaje, mensaje
    FROM dbo.TicketMensaje
    WHERE idTicket = t.idTicket
    ORDER BY fechaEnvio, idTicketMensaje
) m
WHERE t.descripcion IS NULL;

UPDATE m
SET m.esInicial = 1
FROM dbo.TicketMensaje m
WHERE m.esInicial = 0
  AND m.idUsuarioExterno IS NOT NULL
  AND m.idTicketMensaje =
  (
      SELECT TOP (1) x.idTicketMensaje
      FROM dbo.TicketMensaje x
      WHERE x.idTicket = m.idTicket
      ORDER BY x.fechaEnvio, x.idTicketMensaje
  )
  AND EXISTS (SELECT 1 FROM dbo.Ticket t WHERE t.idTicket = m.idTicket AND t.descripcion = m.mensaje)
  AND NOT EXISTS (SELECT 1 FROM dbo.TicketActualizacion a WHERE a.idTicket = m.idTicket);

UPDATE dbo.Ticket SET descripcion = asunto WHERE descripcion IS NULL;

-- Las solicitudes anteriores se toman como leídas por ambos lados, para que
-- no aparezcan todas como "nuevas" al aplicar el script.
UPDATE t
SET t.fechaUltimaLecturaUsuario = ISNULL(t.fechaUltimaLecturaUsuario, t.fechaUltimaActividad),
    t.fechaUltimaLecturaSoporte = CASE WHEN t.estado = N'Pendiente' THEN t.fechaUltimaLecturaSoporte
                                       ELSE ISNULL(t.fechaUltimaLecturaSoporte, t.fechaUltimaActividad) END,
    t.fechaResolucion = CASE WHEN t.estado = N'Cerrada' THEN ISNULL(t.fechaResolucion, t.fechaCierre) ELSE t.fechaResolucion END
FROM dbo.Ticket t
WHERE NOT EXISTS (SELECT 1 FROM dbo.TicketActualizacion a WHERE a.idTicket = t.idTicket);

-- Historial inicial de las solicitudes anteriores: el alta y, si ya no
-- estaban pendientes, el estado al que habían llegado.
INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo, idUsuarioInterno, idUsuarioExterno, detalle, fecha)
SELECT t.idTicket, N'Alta', NULL, N'Pendiente', NULL, t.idUsuarioExterno, N'Solicitud registrada.', t.fechaCreacion
FROM dbo.Ticket t
WHERE NOT EXISTS (SELECT 1 FROM dbo.TicketActualizacion a WHERE a.idTicket = t.idTicket);

INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo, idUsuarioInterno, idUsuarioExterno, detalle, fecha)
SELECT t.idTicket, N'CambioEstado', N'Pendiente', t.estado, t.idUsuarioInternoAsignado, NULL,
       N'Estado convertido al circuito de solicitudes de soporte.', t.fechaUltimaActividad
FROM dbo.Ticket t
WHERE t.estado <> N'Pendiente'
  AND NOT EXISTS (SELECT 1 FROM dbo.TicketActualizacion a WHERE a.idTicket = t.idTicket AND a.tipoActualizacion = N'CambioEstado');
GO

-- Estado inicial por defecto y valores válidos.
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Ticket_estado' AND definition <> N'(N''Pendiente'')')
    ALTER TABLE dbo.Ticket DROP CONSTRAINT DF_Ticket_estado;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Ticket_estado')
    ALTER TABLE dbo.Ticket ADD CONSTRAINT DF_Ticket_estado DEFAULT (N'Pendiente') FOR estado;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Ticket_estado')
    ALTER TABLE dbo.Ticket ADD CONSTRAINT CK_Ticket_estado CHECK (estado IN (N'Pendiente', N'EnProceso', N'Resuelta', N'Cerrada'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Ticket_funcionalidad')
    ALTER TABLE dbo.Ticket ADD CONSTRAINT CK_Ticket_funcionalidad CHECK (categoria IN (N'Reserva', N'Pago', N'Espacio', N'Cuenta', N'Gestor', N'Otra'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Ticket_fechaCreacion' AND object_id = OBJECT_ID(N'dbo.Ticket'))
    CREATE INDEX IX_Ticket_fechaCreacion ON dbo.Ticket (fechaCreacion);
GO

-- Los SP de alta y de mensajes del script 38 se reemplazan por los de abajo.
IF OBJECT_ID('dbo.sp_Ticket_Crear', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_Crear;
GO
IF OBJECT_ID('dbo.sp_TicketMensaje_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TicketMensaje_Insertar;
GO

-- -----------------------------------------------------------------------------
-- Vista con todo lo que muestran el listado y el detalle (de los dos lados).
--   novedadesUsuario: respuestas de soporte y cambios de estado hechos por
--     soporte después de la última vez que el usuario abrió la solicitud.
--   novedadesSoporte: mensajes del usuario después de la última vez que el
--     equipo abrió la solicitud (+1 si nunca la abrió nadie).
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.vw_TicketSoporte', N'V') IS NOT NULL DROP VIEW dbo.vw_TicketSoporte;
GO
CREATE VIEW dbo.vw_TicketSoporte
AS
SELECT
    t.idTicket, t.idUsuarioExterno, t.idReservaAsociada,
    t.categoria AS funcionalidadVinculada,
    t.asunto AS motivo,
    t.descripcion, t.informacionAdicional, t.origen, t.estado,
    t.idUsuarioInternoAsignado, t.fechaCreacion, t.fechaUltimaActividad, t.fechaResolucion, t.fechaCierre,
    t.fechaUltimaLecturaUsuario, t.fechaUltimaLecturaSoporte,
    ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno,
    ue.correoElectronico AS correoUsuarioExterno,
    ui.nombre + N' ' + ui.apellido AS nombreUsuarioInternoAsignado,
    ea.nombreEspacio AS nombreEspacioReserva,
    r.fechaSolicitada AS fechaReserva,
    r.minutoDesde AS minutoDesdeReserva,
    r.minutoHasta AS minutoHastaReserva,
    r.estadoReserva AS estadoReserva,
    (SELECT COUNT(*) FROM dbo.TicketMensaje m
     WHERE m.idTicket = t.idTicket AND m.idUsuarioInterno IS NOT NULL) AS cantidadRespuestas,
    (SELECT COUNT(*) FROM dbo.TicketMensaje m
     WHERE m.idTicket = t.idTicket AND m.idUsuarioInterno IS NOT NULL
       AND m.fechaEnvio > ISNULL(t.fechaUltimaLecturaUsuario, '19000101'))
    + (SELECT COUNT(*) FROM dbo.TicketActualizacion a
       WHERE a.idTicket = t.idTicket AND a.tipoActualizacion = N'CambioEstado' AND a.idUsuarioInterno IS NOT NULL
         AND a.fecha > ISNULL(t.fechaUltimaLecturaUsuario, '19000101')) AS novedadesUsuario,
    (SELECT COUNT(*) FROM dbo.TicketMensaje m
     WHERE m.idTicket = t.idTicket AND m.idUsuarioExterno IS NOT NULL AND m.esInicial = 0
       AND m.fechaEnvio > ISNULL(t.fechaUltimaLecturaSoporte, '19000101'))
    + CASE WHEN t.fechaUltimaLecturaSoporte IS NULL THEN 1 ELSE 0 END AS novedadesSoporte
FROM dbo.Ticket t
INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico;
GO

-- -----------------------------------------------------------------------------
-- CU-003-002 pasos 10 a 14: registro con identificador único, estado
-- inicial pendiente, asociación al usuario y primera entrada del historial.
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Ticket_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_Registrar;
GO
CREATE PROCEDURE dbo.sp_Ticket_Registrar
    @idUsuarioExterno      INT,
    @idReservaAsociada     INT = NULL,
    @funcionalidad         NVARCHAR(50),
    @motivo                NVARCHAR(200),
    @descripcion           NVARCHAR(2000),
    @informacionAdicional  NVARCHAR(1000) = NULL,
    @origen                NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Segunda barrera (la primera es BLL_Ticket): solo una reserva propia.
    IF @idReservaAsociada IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1 FROM dbo.Reserva r
           WHERE r.idReserva = @idReservaAsociada
             AND r.idUsuarioExternoSolicitante = @idUsuarioExterno
       )
    BEGIN
        THROW 51010, 'La reserva indicada no pertenece al usuario que registra la solicitud.', 1;
    END

    DECLARE @ahora DATETIME = GETDATE();

    BEGIN TRANSACTION;

    INSERT INTO dbo.Ticket
        (idUsuarioExterno, idReservaAsociada, categoria, asunto, descripcion, informacionAdicional, origen, estado,
         fechaCreacion, fechaUltimaActividad, fechaUltimaLecturaUsuario)
    VALUES
        (@idUsuarioExterno, @idReservaAsociada, @funcionalidad, @motivo, @descripcion, @informacionAdicional, @origen, N'Pendiente',
         @ahora, @ahora, @ahora);

    DECLARE @idTicket INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo, idUsuarioExterno, detalle, fecha)
    VALUES (@idTicket, N'Alta', NULL, N'Pendiente', @idUsuarioExterno, N'Solicitud registrada.', @ahora);

    COMMIT TRANSACTION;

    SELECT @idTicket AS idTicket;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Ticket_ObtenerPorId
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_TicketSoporte WHERE idTicket = @idTicket;
END
GO

-- CU-003-002 A5: "Mis solicitudes de soporte".
IF OBJECT_ID('dbo.sp_Ticket_ListarPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ListarPorUsuario;
GO
CREATE PROCEDURE dbo.sp_Ticket_ListarPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.vw_TicketSoporte
    WHERE idUsuarioExterno = @idUsuarioExterno
    ORDER BY fechaUltimaActividad DESC, idTicket DESC;
END
GO

-- -----------------------------------------------------------------------------
-- CU-003-003 pasos 4 a 6 y A3: listado interno con filtros por estado,
-- funcionalidad, fecha de registro, usuario solicitante, número de caso,
-- responsable asignado y "solo sin leer", paginado. Primero lo que tiene
-- mensajes sin leer y después por última actividad.
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Ticket_ListarParaInterno', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ListarParaInterno;
GO
CREATE PROCEDURE dbo.sp_Ticket_ListarParaInterno
    @estado                    NVARCHAR(20) = NULL,
    @funcionalidad             NVARCHAR(50) = NULL,
    @desde                     DATE = NULL,
    @hasta                     DATE = NULL,
    @usuario                   NVARCHAR(150) = NULL,
    @idTicket                  INT = NULL,
    @asignacion                NVARCHAR(20) = NULL,   -- NULL: todas; SinAsignar; Usuario (con @idUsuarioInternoAsignado)
    @idUsuarioInternoAsignado  INT = NULL,
    @soloNoLeidas              BIT = 0,
    @pagina                    INT = 1,
    @tamanioPagina             INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(160) = CASE WHEN @usuario IS NULL THEN NULL ELSE N'%' + @usuario + N'%' END;
    IF @pagina < 1 SET @pagina = 1;
    IF @tamanioPagina < 1 OR @tamanioPagina > 100 SET @tamanioPagina = 20;

    SELECT *
    FROM dbo.vw_TicketSoporte v
    WHERE (@estado IS NULL OR v.estado = @estado)
      AND (@funcionalidad IS NULL OR v.funcionalidadVinculada = @funcionalidad)
      AND (@desde IS NULL OR v.fechaCreacion >= @desde)
      AND (@hasta IS NULL OR v.fechaCreacion < DATEADD(DAY, 1, @hasta))
      AND (@patron IS NULL OR v.nombreUsuarioExterno LIKE @patron OR v.correoUsuarioExterno LIKE @patron)
      AND (@idTicket IS NULL OR v.idTicket = @idTicket)
      AND (@asignacion IS NULL
           OR (@asignacion = N'SinAsignar' AND v.idUsuarioInternoAsignado IS NULL)
           OR (@asignacion = N'Usuario' AND v.idUsuarioInternoAsignado = @idUsuarioInternoAsignado))
      AND (@soloNoLeidas = 0 OR (v.novedadesSoporte > 0 AND v.estado <> N'Cerrada'))
    ORDER BY CASE WHEN v.novedadesSoporte > 0 AND v.estado <> N'Cerrada' THEN 0 ELSE 1 END,
             v.fechaUltimaActividad DESC, v.idTicket DESC
    OFFSET (@pagina - 1) * @tamanioPagina ROWS FETCH NEXT @tamanioPagina ROWS ONLY;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ContarParaInterno', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ContarParaInterno;
GO
CREATE PROCEDURE dbo.sp_Ticket_ContarParaInterno
    @estado                    NVARCHAR(20) = NULL,
    @funcionalidad             NVARCHAR(50) = NULL,
    @desde                     DATE = NULL,
    @hasta                     DATE = NULL,
    @usuario                   NVARCHAR(150) = NULL,
    @idTicket                  INT = NULL,
    @asignacion                NVARCHAR(20) = NULL,
    @idUsuarioInternoAsignado  INT = NULL,
    @soloNoLeidas              BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(160) = CASE WHEN @usuario IS NULL THEN NULL ELSE N'%' + @usuario + N'%' END;

    SELECT COUNT(*) AS cantidad
    FROM dbo.vw_TicketSoporte v
    WHERE (@estado IS NULL OR v.estado = @estado)
      AND (@funcionalidad IS NULL OR v.funcionalidadVinculada = @funcionalidad)
      AND (@desde IS NULL OR v.fechaCreacion >= @desde)
      AND (@hasta IS NULL OR v.fechaCreacion < DATEADD(DAY, 1, @hasta))
      AND (@patron IS NULL OR v.nombreUsuarioExterno LIKE @patron OR v.correoUsuarioExterno LIKE @patron)
      AND (@idTicket IS NULL OR v.idTicket = @idTicket)
      AND (@asignacion IS NULL
           OR (@asignacion = N'SinAsignar' AND v.idUsuarioInternoAsignado IS NULL)
           OR (@asignacion = N'Usuario' AND v.idUsuarioInternoAsignado = @idUsuarioInternoAsignado))
      AND (@soloNoLeidas = 0 OR (v.novedadesSoporte > 0 AND v.estado <> N'Cerrada'));
END
GO

-- Resumen del panel (y A2: total = 0 significa que no hay ninguna solicitud).
IF OBJECT_ID('dbo.sp_Ticket_ResumenInterno', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ResumenInterno;
GO
CREATE PROCEDURE dbo.sp_Ticket_ResumenInterno
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS total,
        ISNULL(SUM(CASE WHEN estado = N'Pendiente' THEN 1 ELSE 0 END), 0) AS pendientes,
        ISNULL(SUM(CASE WHEN estado = N'EnProceso' THEN 1 ELSE 0 END), 0) AS enProceso,
        ISNULL(SUM(CASE WHEN estado = N'Resuelta' THEN 1 ELSE 0 END), 0) AS resueltas,
        ISNULL(SUM(CASE WHEN estado = N'Cerrada' THEN 1 ELSE 0 END), 0) AS cerradas,
        ISNULL(SUM(CASE WHEN estado <> N'Cerrada' AND novedadesSoporte > 0 THEN 1 ELSE 0 END), 0) AS sinLeer,
        ISNULL(SUM(CASE WHEN estado IN (N'Pendiente', N'EnProceso') AND idUsuarioInternoAsignado IS NULL THEN 1 ELSE 0 END), 0) AS sinAsignar
    FROM dbo.vw_TicketSoporte;
END
GO

-- Conversación (sin la descripción inicial, que se muestra aparte).
IF OBJECT_ID('dbo.sp_TicketMensaje_ListarPorTicket', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TicketMensaje_ListarPorTicket;
GO
CREATE PROCEDURE dbo.sp_TicketMensaje_ListarPorTicket
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.idTicketMensaje, m.idTicket, m.idUsuarioExterno, m.idUsuarioInterno, m.mensaje, m.fechaEnvio,
        CASE
            WHEN m.idUsuarioInterno IS NOT NULL THEN ui.nombre + N' ' + ui.apellido
            ELSE ue.nombre + N' ' + ue.apellido
        END AS nombreAutor,
        CASE WHEN m.idUsuarioInterno IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS esInterno
    FROM dbo.TicketMensaje m
    LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = m.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = m.idUsuarioInterno
    WHERE m.idTicket = @idTicket
      AND m.esInicial = 0
    ORDER BY m.fechaEnvio ASC, m.idTicketMensaje ASC;
END
GO

-- Historial del caso: alta, cambios de estado y asignaciones.
IF OBJECT_ID('dbo.sp_TicketActualizacion_ListarPorTicket', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TicketActualizacion_ListarPorTicket;
GO
CREATE PROCEDURE dbo.sp_TicketActualizacion_ListarPorTicket
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.idTicketActualizacion, a.idTicket, a.tipoActualizacion, a.estadoAnterior, a.estadoNuevo,
        a.idUsuarioInterno, a.idUsuarioExterno, a.detalle, a.fecha,
        CASE
            WHEN a.idUsuarioInterno IS NOT NULL THEN ui.nombre + N' ' + ui.apellido
            WHEN a.idUsuarioExterno IS NOT NULL THEN ue.nombre + N' ' + ue.apellido
            ELSE N'StageUp'
        END AS nombreResponsable,
        CASE WHEN a.idUsuarioInterno IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS esInterno
    FROM dbo.TicketActualizacion a
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = a.idUsuarioInterno
    LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = a.idUsuarioExterno
    WHERE a.idTicket = @idTicket
    ORDER BY a.fecha ASC, a.idTicketActualizacion ASC;
END
GO

-- -----------------------------------------------------------------------------
-- CU-003-003 pasos 12 a 17 y A7: respuesta del equipo. Si la solicitud
-- estaba pendiente pasa a "en proceso"; con @marcarResuelta pasa a
-- "resuelta". Si nadie la tenía asignada, queda asignada a quien responde.
-- @estadoAnterior es el estado que vio el usuario interno: si cambió en el
-- medio, no se guarda nada (51031).
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Ticket_RegistrarRespuesta', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_RegistrarRespuesta;
GO
CREATE PROCEDURE dbo.sp_Ticket_RegistrarRespuesta
    @idTicket          INT,
    @idUsuarioInterno  INT,
    @mensaje           NVARCHAR(2000),
    @estadoAnterior    NVARCHAR(20),
    @marcarResuelta    BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @estadoActual NVARCHAR(20), @asignado INT;
    SELECT @estadoActual = estado, @asignado = idUsuarioInternoAsignado
    FROM dbo.Ticket WITH (UPDLOCK, ROWLOCK)
    WHERE idTicket = @idTicket;

    IF @estadoActual IS NULL
        THROW 51030, 'No se encontró la solicitud de soporte.', 1;
    IF @estadoActual <> @estadoAnterior
        THROW 51031, 'La solicitud cambió de estado mientras la estabas viendo.', 1;
    IF @estadoActual = N'Cerrada'
        THROW 51032, 'La solicitud está cerrada.', 1;

    DECLARE @ahora DATETIME = GETDATE();
    DECLARE @estadoNuevo NVARCHAR(20) =
        CASE WHEN @marcarResuelta = 1 THEN N'Resuelta'
             WHEN @estadoActual = N'Pendiente' THEN N'EnProceso'
             ELSE @estadoActual END;

    INSERT INTO dbo.TicketMensaje (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio, esInicial)
    VALUES (@idTicket, NULL, @idUsuarioInterno, @mensaje, @ahora, 0);

    UPDATE dbo.Ticket
    SET estado = @estadoNuevo,
        idUsuarioInternoAsignado = ISNULL(idUsuarioInternoAsignado, @idUsuarioInterno),
        fechaUltimaActividad = @ahora,
        fechaUltimaLecturaSoporte = @ahora,
        fechaResolucion = CASE WHEN @estadoNuevo = N'Resuelta' AND @estadoActual <> N'Resuelta' THEN @ahora ELSE fechaResolucion END
    WHERE idTicket = @idTicket;

    IF @asignado IS NULL
    BEGIN
        INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, idUsuarioInterno, detalle, fecha)
        SELECT @idTicket, N'Asignacion', @idUsuarioInterno, N'Asignada a ' + nombre + N' ' + apellido + N' al responder.', @ahora
        FROM dbo.UsuarioInterno WHERE idUsuarioInterno = @idUsuarioInterno;
    END

    IF @estadoNuevo <> @estadoActual
    BEGIN
        INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo, idUsuarioInterno, detalle, fecha)
        VALUES (@idTicket, N'CambioEstado', @estadoActual, @estadoNuevo, @idUsuarioInterno,
                CASE WHEN @estadoNuevo = N'Resuelta' THEN N'Respuesta registrada y solicitud marcada como resuelta.'
                     ELSE N'Respuesta registrada: la solicitud está siendo atendida.' END, @ahora);
    END

    COMMIT TRANSACTION;

    SELECT @estadoNuevo AS estadoNuevo;
END
GO

-- CU-003-003 pasos 18 a 23, A8, A10 (reapertura) y A11 (cierre). La
-- transición la valida BLL_Ticket; acá se registra con responsable, fecha,
-- hora, estado anterior y nuevo.
IF OBJECT_ID('dbo.sp_Ticket_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_Ticket_CambiarEstado
    @idTicket          INT,
    @estadoAnterior    NVARCHAR(20),
    @estadoNuevo       NVARCHAR(20),
    @idUsuarioInterno  INT = NULL,
    @idUsuarioExterno  INT = NULL,
    @detalle           NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @estadoActual NVARCHAR(20);
    SELECT @estadoActual = estado
    FROM dbo.Ticket WITH (UPDLOCK, ROWLOCK)
    WHERE idTicket = @idTicket;

    IF @estadoActual IS NULL
        THROW 51030, 'No se encontró la solicitud de soporte.', 1;
    IF @estadoActual <> @estadoAnterior
        THROW 51031, 'La solicitud cambió de estado mientras la estabas viendo.', 1;

    DECLARE @ahora DATETIME = GETDATE();

    UPDATE dbo.Ticket
    SET estado = @estadoNuevo,
        fechaUltimaActividad = @ahora,
        fechaUltimaLecturaSoporte = CASE WHEN @idUsuarioInterno IS NOT NULL THEN @ahora ELSE fechaUltimaLecturaSoporte END,
        fechaResolucion = CASE WHEN @estadoNuevo = N'Resuelta' THEN @ahora ELSE fechaResolucion END,
        fechaCierre = CASE WHEN @estadoNuevo = N'Cerrada' THEN @ahora WHEN @estadoAnterior = N'Cerrada' THEN NULL ELSE fechaCierre END
    WHERE idTicket = @idTicket;

    INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo, idUsuarioInterno, idUsuarioExterno, detalle, fecha)
    VALUES (@idTicket, N'CambioEstado', @estadoAnterior, @estadoNuevo, @idUsuarioInterno, @idUsuarioExterno, @detalle, @ahora);

    COMMIT TRANSACTION;
END
GO

-- Ítem 12: asignación de un responsable (o quitarla). Con @tomar = 1 es
-- "Tomar solicitud": además una pendiente pasa a "en proceso".
IF OBJECT_ID('dbo.sp_Ticket_Asignar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_Asignar;
GO
CREATE PROCEDURE dbo.sp_Ticket_Asignar
    @idTicket                     INT,
    @idUsuarioInternoAsignado     INT = NULL,
    @idUsuarioInternoResponsable  INT,
    @tomar                        BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @estadoActual NVARCHAR(20);
    SELECT @estadoActual = estado
    FROM dbo.Ticket WITH (UPDLOCK, ROWLOCK)
    WHERE idTicket = @idTicket;

    IF @estadoActual IS NULL
        THROW 51030, 'No se encontró la solicitud de soporte.', 1;
    IF @estadoActual = N'Cerrada'
        THROW 51032, 'La solicitud está cerrada.', 1;

    DECLARE @ahora DATETIME = GETDATE();
    DECLARE @nombreAsignado NVARCHAR(210) =
        (SELECT nombre + N' ' + apellido FROM dbo.UsuarioInterno WHERE idUsuarioInterno = @idUsuarioInternoAsignado);

    UPDATE dbo.Ticket
    SET idUsuarioInternoAsignado = @idUsuarioInternoAsignado,
        fechaUltimaActividad = @ahora,
        fechaUltimaLecturaSoporte = @ahora
    WHERE idTicket = @idTicket;

    INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, idUsuarioInterno, detalle, fecha)
    VALUES (@idTicket, N'Asignacion', @idUsuarioInternoResponsable,
            CASE WHEN @idUsuarioInternoAsignado IS NULL THEN N'Se quitó el responsable asignado.'
                 WHEN @tomar = 1 THEN N'Tomada por ' + @nombreAsignado + N'.'
                 ELSE N'Asignada a ' + @nombreAsignado + N'.' END, @ahora);

    DECLARE @estadoFinal NVARCHAR(20) = @estadoActual;
    IF @tomar = 1 AND @estadoActual = N'Pendiente'
    BEGIN
        SET @estadoFinal = N'EnProceso';
        UPDATE dbo.Ticket SET estado = @estadoFinal WHERE idTicket = @idTicket;

        INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo, idUsuarioInterno, detalle, fecha)
        VALUES (@idTicket, N'CambioEstado', @estadoActual, @estadoFinal, @idUsuarioInternoResponsable,
                N'La solicitud está siendo atendida por el equipo de soporte.', @ahora);
    END

    COMMIT TRANSACTION;

    SELECT @estadoFinal AS estadoFinal;
END
GO

-- -----------------------------------------------------------------------------
-- CU-003-002 A8/A9: el usuario agrega información a una solicitud que sigue
-- abierta (no hace falta registrar otra). Si estaba resuelta, vuelve a
-- "en proceso" porque el usuario indica que todavía necesita ayuda.
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_TicketMensaje_InsertarUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TicketMensaje_InsertarUsuario;
GO
CREATE PROCEDURE dbo.sp_TicketMensaje_InsertarUsuario
    @idTicket          INT,
    @idUsuarioExterno  INT,
    @mensaje           NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @estadoActual NVARCHAR(20);
    SELECT @estadoActual = estado
    FROM dbo.Ticket WITH (UPDLOCK, ROWLOCK)
    WHERE idTicket = @idTicket AND idUsuarioExterno = @idUsuarioExterno;

    IF @estadoActual IS NULL
        THROW 51030, 'No se encontró la solicitud de soporte.', 1;
    IF @estadoActual = N'Cerrada'
        THROW 51032, 'La solicitud está cerrada.', 1;

    DECLARE @ahora DATETIME = GETDATE();

    INSERT INTO dbo.TicketMensaje (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio, esInicial)
    VALUES (@idTicket, @idUsuarioExterno, NULL, @mensaje, @ahora, 0);

    DECLARE @estadoFinal NVARCHAR(20) = CASE WHEN @estadoActual = N'Resuelta' THEN N'EnProceso' ELSE @estadoActual END;

    UPDATE dbo.Ticket
    SET estado = @estadoFinal,
        fechaUltimaActividad = @ahora,
        fechaUltimaLecturaUsuario = @ahora
    WHERE idTicket = @idTicket;

    IF @estadoFinal <> @estadoActual
    BEGIN
        INSERT INTO dbo.TicketActualizacion (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo, idUsuarioExterno, detalle, fecha)
        VALUES (@idTicket, N'CambioEstado', @estadoActual, @estadoFinal, @idUsuarioExterno,
                N'El usuario indicó que todavía necesita ayuda.', @ahora);
    END

    COMMIT TRANSACTION;

    SELECT @estadoFinal AS estadoFinal;
END
GO

-- Ítem 12: leído / no leído. Abrir el detalle marca como leído lo que había
-- hasta ese momento, del lado del usuario o del equipo de soporte.
IF OBJECT_ID('dbo.sp_Ticket_MarcarLeido', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_MarcarLeido;
GO
CREATE PROCEDURE dbo.sp_Ticket_MarcarLeido
    @idTicket  INT,
    @lado      NVARCHAR(10)   -- Usuario / Soporte
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Ticket
    SET fechaUltimaLecturaUsuario = CASE WHEN @lado = N'Usuario' THEN GETDATE() ELSE fechaUltimaLecturaUsuario END,
        fechaUltimaLecturaSoporte = CASE WHEN @lado = N'Soporte' THEN GETDATE() ELSE fechaUltimaLecturaSoporte END
    WHERE idTicket = @idTicket;

    SELECT @@ROWCOUNT AS filas;
END
GO

-- Ítem 12: actualización periódica. Las pantallas preguntan cada tanto por
-- esta "marca" y, si cambió, avisan que hay novedades.
IF OBJECT_ID('dbo.sp_Ticket_ObtenerMarca', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ObtenerMarca;
GO
CREATE PROCEDURE dbo.sp_Ticket_ObtenerMarca
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idUsuarioExterno, estado, fechaUltimaActividad
    FROM dbo.Ticket
    WHERE idTicket = @idTicket;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ObtenerMarcaListado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ObtenerMarcaListado;
GO
CREATE PROCEDURE dbo.sp_Ticket_ObtenerMarcaListado
    @idUsuarioExterno INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad, MAX(fechaUltimaActividad) AS ultimaActividad
    FROM dbo.Ticket
    WHERE @idUsuarioExterno IS NULL OR idUsuarioExterno = @idUsuarioExterno;
END
GO

-- Contador del menú interno: solicitudes que requieren atención (pendientes
-- o con mensajes del usuario sin leer).
IF OBJECT_ID('dbo.sp_Ticket_ContarAbiertos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ContarAbiertos;
GO
CREATE PROCEDURE dbo.sp_Ticket_ContarAbiertos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad
    FROM dbo.vw_TicketSoporte
    WHERE estado <> N'Cerrada'
      AND (estado = N'Pendiente' OR novedadesSoporte > 0);
END
GO

-- Contador del menú del usuario: solicitudes con novedades sin ver.
IF OBJECT_ID('dbo.sp_Ticket_ContarNovedadesUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ContarNovedadesUsuario;
GO
CREATE PROCEDURE dbo.sp_Ticket_ContarNovedadesUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad
    FROM dbo.vw_TicketSoporte
    WHERE idUsuarioExterno = @idUsuarioExterno
      AND novedadesUsuario > 0;
END
GO

-- -----------------------------------------------------------------------------
-- Nombre de la pantalla según el CU y permiso específico para reabrir
-- solicitudes cerradas (CU-003-003 A10).
-- -----------------------------------------------------------------------------
UPDATE dbo.PermisoInterno
SET nombrePermiso = N'Solicitudes de soporte',
    descripcion = N'Consultar, responder, asignar, cambiar de estado y cerrar las solicitudes de soporte de los usuarios.',
    fechaUltimaModificacion = GETDATE()
WHERE codigoPermiso = N'GESTIONAR_SOPORTE';

UPDATE dbo.ComponentePermiso
SET nombre = N'Solicitudes de soporte',
    descripcion = N'Consultar, responder, asignar, cambiar de estado y cerrar las solicitudes de soporte de los usuarios.',
    fechaUltimaModificacion = GETDATE()
WHERE codigoPermiso = N'GESTIONAR_SOPORTE';
GO

IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
    UPDATE dbo.OpcionMenu
    SET texto = N'Solicitudes de soporte',
        descripcion = N'Consultar, responder, asignar, cambiar de estado y cerrar las solicitudes de soporte de los usuarios.'
    WHERE url = N'~/Interno/GestionSoporte.aspx';
GO

IF OBJECT_ID('tempdb..#RegistrarPermisoAdministracion') IS NOT NULL DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
-- Igual que en los scripts 63 y 64, con @agregarAlMenu: este permiso no es
-- una pantalla, habilita una acción dentro de "Solicitudes de soporte".
CREATE PROCEDURE #RegistrarPermisoAdministracion
    @codigo         NVARCHAR(100),
    @nombre         NVARCHAR(200),
    @descripcion    NVARCHAR(500),
    @url            NVARCHAR(300),
    @accion         NVARCHAR(50),
    @orden          INT,
    @agregarAlMenu  BIT = 1
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
                (@codigo, @nombre, @descripcion, N'Administración', @accion, N'Activo', @url, 1);

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

        DECLARE @idComponente INT = NULL;

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

            SET @idComponente =
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
                    (@idGrupoAdministracion, N'Permiso', @codigo, @nombre, @descripcion, @url, @orden, 1);

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

        IF @agregarAlMenu = 1
           AND OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
           AND @idComponente IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponente)
        BEGIN
            INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
            SELECT @nombre, @descripcion, @url, N'Administración', ISNULL(MAX(orden), 0) + 10, @idComponente, 1
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
    @codigo = N'REABRIR_SOLICITUD_SOPORTE',
    @nombre = N'Reabrir solicitudes de soporte cerradas',
    @descripcion = N'Volver a poner "en proceso" una solicitud de soporte cerrada (las cerradas solo se consultan si no se tiene este permiso).',
    @url = N'~/Interno/GestionSoporte.aspx',
    @accion = N'Modificar',
    @orden = 121,
    @agregarAlMenu = 0;
GO

DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
