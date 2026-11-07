SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CodigoRecuperacion_Insertar
    @idUsuarioExterno INT, @codigo NVARCHAR(40), @fechaVencimiento DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @recurso NVARCHAR(255), @bloqueo INT;
    SELECT @recurso=N'StageUp:Recuperacion:'+LOWER(LTRIM(RTRIM(correoElectronico)))
    FROM dbo.UsuarioExterno WHERE idUsuarioExterno=@idUsuarioExterno;
    IF @recurso IS NULL THROW 51040, 'No se encontro la cuenta.', 1;
    EXEC @bloqueo=sys.sp_getapplock @Resource=@recurso,@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
    IF @bloqueo<0 THROW 51041, 'Otra operacion esta actualizando la recuperacion. Intenta nuevamente.', 1;
    UPDATE dbo.CodigoRecuperacion SET utilizado=1,fechaUtilizacion=GETDATE()
    WHERE idUsuarioExterno=@idUsuarioExterno AND utilizado=0;
    INSERT dbo.CodigoRecuperacion(idUsuarioExterno,codigo,fechaGeneracion,fechaVencimiento,utilizado)
    VALUES(@idUsuarioExterno,@codigo,GETDATE(),@fechaVencimiento,0);
    DECLARE @id INT=CONVERT(INT,SCOPE_IDENTITY());
    COMMIT TRANSACTION;
    SELECT @id;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_CodigoRecuperacion_ObtenerVigentePorUsuario @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP(1) * FROM dbo.CodigoRecuperacion
    WHERE idUsuarioExterno=@idUsuarioExterno AND utilizado=0
      AND idCodigoRecuperacion=(SELECT MAX(idCodigoRecuperacion) FROM dbo.CodigoRecuperacion WHERE idUsuarioExterno=@idUsuarioExterno)
    ORDER BY idCodigoRecuperacion DESC;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_CodigoRecuperacion_MarcarUtilizado @idCodigoRecuperacion INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CodigoRecuperacion SET utilizado=1,fechaUtilizacion=GETDATE()
    WHERE idCodigoRecuperacion=@idCodigoRecuperacion AND utilizado=0 AND fechaVencimiento>GETDATE();
    IF @@ROWCOUNT<>1 THROW 51042, 'El codigo ya no esta disponible. Solicita uno nuevo.', 1;
END
GO
UPDATE c SET utilizado=1,fechaUtilizacion=GETDATE()
FROM dbo.CodigoRecuperacion c
WHERE c.utilizado=0 AND EXISTS(SELECT 1 FROM dbo.CodigoRecuperacion n
 WHERE n.idUsuarioExterno=c.idUsuarioExterno AND n.idCodigoRecuperacion>c.idCodigoRecuperacion);
GO
IF COL_LENGTH(N'dbo.TicketMensaje',N'fechaLecturaDestinatario') IS NULL
BEGIN
    ALTER TABLE dbo.TicketMensaje ADD fechaLecturaDestinatario DATETIME NULL;
    EXEC(N'UPDATE m SET fechaLecturaDestinatario=CASE WHEN m.idUsuarioInterno IS NOT NULL THEN t.fechaUltimaLecturaUsuario ELSE t.fechaUltimaLecturaSoporte END
    FROM dbo.TicketMensaje m JOIN dbo.Ticket t ON t.idTicket=m.idTicket
    WHERE m.fechaEnvio<=CASE WHEN m.idUsuarioInterno IS NOT NULL THEN t.fechaUltimaLecturaUsuario ELSE t.fechaUltimaLecturaSoporte END;');
END
GO
IF COL_LENGTH(N'dbo.TicketActualizacion',N'fechaLecturaDestinatario') IS NULL
BEGIN
    ALTER TABLE dbo.TicketActualizacion ADD fechaLecturaDestinatario DATETIME NULL;
    EXEC(N'UPDATE a SET fechaLecturaDestinatario=CASE WHEN a.idUsuarioInterno IS NOT NULL THEN t.fechaUltimaLecturaUsuario ELSE t.fechaUltimaLecturaSoporte END
    FROM dbo.TicketActualizacion a JOIN dbo.Ticket t ON t.idTicket=a.idTicket
    WHERE a.fecha<=CASE WHEN a.idUsuarioInterno IS NOT NULL THEN t.fechaUltimaLecturaUsuario ELSE t.fechaUltimaLecturaSoporte END;');
END
GO
CREATE OR ALTER VIEW dbo.vw_TicketSoporte
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
       AND m.fechaLecturaDestinatario IS NULL)
    + (SELECT COUNT(*) FROM dbo.TicketActualizacion a
       WHERE a.idTicket = t.idTicket AND a.tipoActualizacion = N'CambioEstado' AND a.idUsuarioInterno IS NOT NULL
         AND a.fechaLecturaDestinatario IS NULL) AS novedadesUsuario,
    (SELECT COUNT(*) FROM dbo.TicketMensaje m
     WHERE m.idTicket = t.idTicket AND m.idUsuarioExterno IS NOT NULL AND m.esInicial = 0
       AND m.fechaLecturaDestinatario IS NULL)
    + CASE WHEN t.fechaUltimaLecturaSoporte IS NULL THEN 1 ELSE 0 END AS novedadesSoporte
FROM dbo.Ticket t
INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico;
GO
CREATE OR ALTER PROCEDURE dbo.sp_TicketMensaje_ListarPorTicket
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.idTicketMensaje, m.idTicket, m.idUsuarioExterno, m.idUsuarioInterno, m.mensaje, m.fechaEnvio, m.fechaLecturaDestinatario,
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
CREATE OR ALTER PROCEDURE dbo.sp_TicketActualizacion_ListarPorTicket
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.idTicketActualizacion, a.idTicket, a.tipoActualizacion, a.estadoAnterior, a.estadoNuevo,
        a.idUsuarioInterno, a.idUsuarioExterno, a.detalle, a.fecha, a.fechaLecturaDestinatario,
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
CREATE OR ALTER PROCEDURE dbo.sp_Ticket_MarcarLeido
    @idTicket INT, @lado NVARCHAR(10), @mensajes XML, @actualizaciones XML
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @lado NOT IN (N'Usuario',N'Soporte') THROW 51043, 'Lado de lectura no valido.', 1;
    BEGIN TRANSACTION;
    DECLARE @ahora DATETIME=GETDATE();
    UPDATE m SET fechaLecturaDestinatario=@ahora
    FROM dbo.TicketMensaje m
    WHERE m.idTicket=@idTicket AND m.fechaLecturaDestinatario IS NULL
      AND ((@lado=N'Usuario' AND m.idUsuarioInterno IS NOT NULL) OR (@lado=N'Soporte' AND m.idUsuarioExterno IS NOT NULL))
      AND EXISTS(SELECT 1 FROM @mensajes.nodes('/ids/id') x(n) WHERE x.n.value('.','int')=m.idTicketMensaje);
    UPDATE a SET fechaLecturaDestinatario=@ahora
    FROM dbo.TicketActualizacion a
    WHERE a.idTicket=@idTicket AND a.fechaLecturaDestinatario IS NULL
      AND ((@lado=N'Usuario' AND a.idUsuarioInterno IS NOT NULL) OR (@lado=N'Soporte' AND a.idUsuarioExterno IS NOT NULL))
      AND EXISTS(SELECT 1 FROM @actualizaciones.nodes('/ids/id') x(n) WHERE x.n.value('.','int')=a.idTicketActualizacion);
    UPDATE dbo.Ticket SET
      fechaUltimaLecturaUsuario=CASE WHEN @lado=N'Usuario' THEN @ahora ELSE fechaUltimaLecturaUsuario END,
      fechaUltimaLecturaSoporte=CASE WHEN @lado=N'Soporte' THEN @ahora ELSE fechaUltimaLecturaSoporte END
    WHERE idTicket=@idTicket;
    DECLARE @filas INT=@@ROWCOUNT;
    COMMIT TRANSACTION;
    SELECT @filas;
END
GO
