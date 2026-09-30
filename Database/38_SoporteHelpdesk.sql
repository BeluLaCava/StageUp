IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID('dbo.Ticket', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ticket
    (
        idTicket                  INT IDENTITY(1,1) NOT NULL,
        idUsuarioExterno          INT NOT NULL,
        idReservaAsociada         INT NULL,
        categoria                 NVARCHAR(50) NOT NULL,
        asunto                    NVARCHAR(200) NOT NULL,
        estado                    NVARCHAR(20) NOT NULL CONSTRAINT DF_Ticket_estado DEFAULT (N'Abierto'),
        idUsuarioInternoAsignado  INT NULL,
        fechaCreacion             DATETIME NOT NULL CONSTRAINT DF_Ticket_fechaCreacion DEFAULT (GETDATE()),
        fechaUltimaActividad      DATETIME NOT NULL CONSTRAINT DF_Ticket_fechaUltimaActividad DEFAULT (GETDATE()),
        fechaCierre               DATETIME NULL,
        CONSTRAINT PK_Ticket PRIMARY KEY (idTicket),
        CONSTRAINT FK_Ticket_UsuarioExterno FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_Ticket_Reserva FOREIGN KEY (idReservaAsociada) REFERENCES dbo.Reserva (idReserva),
        CONSTRAINT FK_Ticket_UsuarioInterno FOREIGN KEY (idUsuarioInternoAsignado) REFERENCES dbo.UsuarioInterno (idUsuarioInterno)
    );

    CREATE INDEX IX_Ticket_idUsuarioExterno ON dbo.Ticket (idUsuarioExterno);
    CREATE INDEX IX_Ticket_estado ON dbo.Ticket (estado);
END
GO

IF OBJECT_ID('dbo.TicketMensaje', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketMensaje
    (
        idTicketMensaje    INT IDENTITY(1,1) NOT NULL,
        idTicket           INT NOT NULL,
        idUsuarioExterno   INT NULL,
        idUsuarioInterno   INT NULL,
        mensaje            NVARCHAR(2000) NOT NULL,
        fechaEnvio         DATETIME NOT NULL CONSTRAINT DF_TicketMensaje_fechaEnvio DEFAULT (GETDATE()),
        CONSTRAINT PK_TicketMensaje PRIMARY KEY (idTicketMensaje),
        CONSTRAINT FK_TicketMensaje_Ticket FOREIGN KEY (idTicket) REFERENCES dbo.Ticket (idTicket),
        CONSTRAINT FK_TicketMensaje_UsuarioExterno FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_TicketMensaje_UsuarioInterno FOREIGN KEY (idUsuarioInterno) REFERENCES dbo.UsuarioInterno (idUsuarioInterno)
    );

    CREATE INDEX IX_TicketMensaje_idTicket ON dbo.TicketMensaje (idTicket);
END
GO

-- ---------------------------------------------------------------------------
-- Alta de ticket: crea el ticket y su primer mensaje (el motivo de la
-- consulta) en una única transacción.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_Ticket_Crear', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_Crear;
GO
CREATE PROCEDURE dbo.sp_Ticket_Crear
    @idUsuarioExterno   INT,
    @idReservaAsociada  INT = NULL,
    @categoria          NVARCHAR(50),
    @asunto             NVARCHAR(200),
    @mensajeInicial     NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Segunda barrera (la primera es BLL_Ticket): un usuario solo puede
    -- asociar a su ticket una reserva que él mismo solicitó.
    IF @idReservaAsociada IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1 FROM dbo.Reserva r
           WHERE r.idReserva = @idReservaAsociada
             AND r.idUsuarioExternoSolicitante = @idUsuarioExterno
       )
    BEGIN
        THROW 51010, 'La reserva indicada no pertenece al usuario que crea el ticket.', 1;
    END

    BEGIN TRANSACTION;

    INSERT INTO dbo.Ticket (idUsuarioExterno, idReservaAsociada, categoria, asunto, estado, fechaCreacion, fechaUltimaActividad)
    VALUES (@idUsuarioExterno, @idReservaAsociada, @categoria, @asunto, N'Abierto', GETDATE(), GETDATE());

    DECLARE @idTicket INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.TicketMensaje (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio)
    VALUES (@idTicket, @idUsuarioExterno, NULL, @mensajeInicial, GETDATE());

    COMMIT TRANSACTION;

    SELECT @idTicket AS idTicket;
END
GO

IF OBJECT_ID('dbo.sp_TicketMensaje_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_TicketMensaje_Insertar;
GO
CREATE PROCEDURE dbo.sp_TicketMensaje_Insertar
    @idTicket          INT,
    @idUsuarioExterno  INT = NULL,
    @idUsuarioInterno  INT = NULL,
    @mensaje           NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TicketMensaje (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio)
    VALUES (@idTicket, @idUsuarioExterno, @idUsuarioInterno, @mensaje, GETDATE());

    UPDATE dbo.Ticket
    SET fechaUltimaActividad = GETDATE()
    WHERE idTicket = @idTicket;

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idTicketMensaje;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_Ticket_CambiarEstado
    @idTicket  INT,
    @estado    NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Ticket
    SET estado = @estado,
        fechaUltimaActividad = GETDATE(),
        fechaCierre = CASE WHEN @estado = N'Cerrado' THEN GETDATE() ELSE fechaCierre END
    WHERE idTicket = @idTicket;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_Asignar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_Asignar;
GO
CREATE PROCEDURE dbo.sp_Ticket_Asignar
    @idTicket                  INT,
    @idUsuarioInternoAsignado  INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Ticket
    SET idUsuarioInternoAsignado = @idUsuarioInternoAsignado
    WHERE idTicket = @idTicket;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Ticket_ObtenerPorId
    @idTicket INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTicket, t.idUsuarioExterno, t.idReservaAsociada, t.categoria, t.asunto, t.estado,
        t.idUsuarioInternoAsignado, t.fechaCreacion, t.fechaUltimaActividad, t.fechaCierre,
        ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno,
        ue.correoElectronico AS correoUsuarioExterno,
        ui.nombre + N' ' + ui.apellido AS nombreUsuarioInternoAsignado,
        -- Contexto de la reserva asociada (si la hay), para que soporte
        -- responda sabiendo de qué servicio contratado se trata.
        ea.nombreEspacio AS nombreEspacioReserva,
        r.fechaSolicitada AS fechaReserva,
        r.minutoDesde AS minutoDesdeReserva,
        r.minutoHasta AS minutoHastaReserva,
        r.estadoReserva AS estadoReserva
    FROM dbo.Ticket t
    INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
    LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
    LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico
    WHERE t.idTicket = @idTicket;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ListarPorUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ListarPorUsuario;
GO
CREATE PROCEDURE dbo.sp_Ticket_ListarPorUsuario
    @idUsuarioExterno INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTicket, t.idUsuarioExterno, t.idReservaAsociada, t.categoria, t.asunto, t.estado,
        t.idUsuarioInternoAsignado, t.fechaCreacion, t.fechaUltimaActividad, t.fechaCierre,
        ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno,
        ue.correoElectronico AS correoUsuarioExterno,
        ui.nombre + N' ' + ui.apellido AS nombreUsuarioInternoAsignado,
        ea.nombreEspacio AS nombreEspacioReserva,
        r.fechaSolicitada AS fechaReserva,
        r.minutoDesde AS minutoDesdeReserva,
        r.minutoHasta AS minutoHastaReserva,
        r.estadoReserva AS estadoReserva
    FROM dbo.Ticket t
    INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
    LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
    LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico
    WHERE t.idUsuarioExterno = @idUsuarioExterno
    ORDER BY t.fechaUltimaActividad DESC;
END
GO

IF OBJECT_ID('dbo.sp_Ticket_ListarParaInterno', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ListarParaInterno;
GO
CREATE PROCEDURE dbo.sp_Ticket_ListarParaInterno
    @estado     NVARCHAR(20) = NULL,
    @categoria  NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTicket, t.idUsuarioExterno, t.idReservaAsociada, t.categoria, t.asunto, t.estado,
        t.idUsuarioInternoAsignado, t.fechaCreacion, t.fechaUltimaActividad, t.fechaCierre,
        ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno,
        ue.correoElectronico AS correoUsuarioExterno,
        ui.nombre + N' ' + ui.apellido AS nombreUsuarioInternoAsignado,
        ea.nombreEspacio AS nombreEspacioReserva,
        r.fechaSolicitada AS fechaReserva,
        r.minutoDesde AS minutoDesdeReserva,
        r.minutoHasta AS minutoHastaReserva,
        r.estadoReserva AS estadoReserva
    FROM dbo.Ticket t
    INNER JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = t.idUsuarioExterno
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = t.idUsuarioInternoAsignado
    LEFT JOIN dbo.Reserva r ON r.idReserva = t.idReservaAsociada
    LEFT JOIN dbo.EspacioArtistico ea ON ea.idEspacioArtistico = r.idEspacioArtistico
    WHERE (@estado IS NULL OR t.estado = @estado)
      AND (@categoria IS NULL OR t.categoria = @categoria)
    ORDER BY t.fechaUltimaActividad DESC;
END
GO

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
    ORDER BY m.fechaEnvio ASC;
END
GO

-- Indicador para el equipo interno: tickets en estado Abierto (recién
-- creados, o en los que el usuario volvió a escribir después de una
-- respuesta de soporte). Se muestra como contador en el menú del panel.
IF OBJECT_ID('dbo.sp_Ticket_ContarAbiertos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Ticket_ContarAbiertos;
GO
CREATE PROCEDURE dbo.sp_Ticket_ContarAbiertos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS cantidad
    FROM dbo.Ticket
    WHERE estado = N'Abierto';
END
GO

-- ---------------------------------------------------------------------------
-- Permiso propio GESTIONAR_SOPORTE (mismo patrón que GESTIONAR_FAQ en
-- 29_FaqAbmYPermiso.sql y GESTIONAR_ENCRIPTACION en
-- 37_HerramientasSeguridadYExportacion.sql).
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoSoporte INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'GESTIONAR_SOPORTE'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoSoporte IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'GESTIONAR_SOPORTE', N'Gestión de soporte',
             N'Ver, responder, asignar y cerrar los tickets de soporte de los usuarios.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/GestionSoporte.aspx', 1);

        SET @idPermisoSoporte = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Gestión de soporte',
            descripcion = N'Ver, responder, asignar y cerrar los tickets de soporte de los usuarios.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/GestionSoporte.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoSoporte;
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
             AND idPermisoInterno = @idPermisoSoporte
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoSoporte, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoSoporte;
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

        DECLARE @idComponenteSoporte INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'GESTIONAR_SOPORTE'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteSoporte IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_SOPORTE', N'Gestión de soporte',
                 N'Ver, responder, asignar y cerrar los tickets de soporte de los usuarios.',
                 N'~/Interno/GestionSoporte.aspx', 120, 1);

            SET @idComponenteSoporte = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Gestión de soporte',
                descripcion = N'Ver, responder, asignar y cerrar los tickets de soporte de los usuarios.',
                urlAsociada = N'~/Interno/GestionSoporte.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteSoporte;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteSoporte
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteSoporte, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteSoporte;
            END
        END
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
END CATCH;
GO
