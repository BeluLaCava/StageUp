IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID('dbo.sp_Busqueda_Publica', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Publica;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Publica
    @texto            NVARCHAR(300),
    @incluirEspacios  BIT = 1,
    @incluirNovedades BIT = 1,
    @incluirFaq       BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(310) = N'%' + @texto + N'%';

    SELECT tipo, idReferencia, titulo, detalle, fecha
    FROM
    (
        SELECT TOP 25 N'Espacio' AS tipo, e.idEspacioArtistico AS idReferencia, e.nombreEspacio AS titulo,
               e.tipoEspacio + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N'') + ISNULL(N' · ' + LEFT(e.descripcion, 200), N'') AS detalle,
               e.fechaPublicacion AS fecha,
               CASE WHEN e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END AS prioridad
        FROM dbo.EspacioArtistico e
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE @incluirEspacios = 1
          AND e.activo = 1 AND e.publicado = 1
          AND (e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron
               OR f.ciudad LIKE @patron OR f.provincia LIKE @patron)
        ORDER BY prioridad, e.fechaPublicacion DESC
    ) espacios
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha
    FROM
    (
        SELECT TOP 25 N'Novedad' AS tipo, n.idNovedad AS idReferencia, n.titulo,
               n.categoria + N' · ' + n.resumen AS detalle,
               ISNULL(n.fechaPublicacion, n.fechaAlta) AS fecha,
               CASE WHEN n.titulo LIKE @patron THEN 0 ELSE 1 END AS prioridad
        FROM dbo.Novedad n
        WHERE @incluirNovedades = 1
          AND n.activo = 1 AND n.publicado = 1
          AND (n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron)
        ORDER BY prioridad, ISNULL(n.fechaPublicacion, n.fechaAlta) DESC
    ) novedades
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha
    FROM
    (
        SELECT TOP 25 N'FAQ' AS tipo, q.idFaq AS idReferencia, q.pregunta AS titulo,
               LEFT(q.respuesta, 300) AS detalle,
               CAST(NULL AS DATETIME) AS fecha,
               CASE WHEN q.pregunta LIKE @patron THEN 0 ELSE 1 END AS prioridad
        FROM dbo.Faq q
        WHERE @incluirFaq = 1
          AND q.activo = 1
          AND (q.pregunta LIKE @patron OR q.respuesta LIKE @patron)
        ORDER BY prioridad, q.orden
    ) faqs;
END
GO

IF OBJECT_ID('dbo.sp_Busqueda_Interna', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Interna;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Interna
    @texto                    NVARCHAR(300),
    @incluirUsuariosExternos  BIT = 0,
    @incluirUsuariosInternos  BIT = 0,
    @incluirEspacios          BIT = 0,
    @incluirReservas          BIT = 0,
    @incluirTickets           BIT = 0,
    @incluirNovedades         BIT = 0,
    @incluirFaq               BIT = 0,
    @incluirEncuestas         BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(310) = N'%' + @texto + N'%';
    DECLARE @idBuscado INT = TRY_CAST(@texto AS INT);

    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'UsuarioExterno' AS tipo, u.idUsuarioExterno AS idReferencia,
               u.nombre + N' ' + u.apellido AS titulo,
               u.correoElectronico + N' · ' + ISNULL(u.perfilUsuario, N'-') + N' · Cuenta ' + ISNULL(u.estadoCuenta, N'-') AS detalle,
               u.fechaAlta AS fecha
        FROM dbo.UsuarioExterno u
        WHERE @incluirUsuariosExternos = 1
          AND (u.nombre LIKE @patron OR u.apellido LIKE @patron OR u.correoElectronico LIKE @patron
               OR (u.nombre + N' ' + u.apellido) LIKE @patron)
        ORDER BY u.fechaAlta DESC
    ) ue
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'UsuarioInterno' AS tipo, i.idUsuarioInterno AS idReferencia,
               i.nombre + N' ' + i.apellido AS titulo,
               i.correoElectronico + N' · Rol ' + ISNULL(r.nombreRol, N'-') + N' · Cuenta ' + ISNULL(i.estadoCuenta, N'-') AS detalle,
               i.fechaAlta AS fecha
        FROM dbo.UsuarioInterno i
        LEFT JOIN dbo.RolInterno r ON r.idRolInterno = i.idRolInterno
        WHERE @incluirUsuariosInternos = 1
          AND (i.nombre LIKE @patron OR i.apellido LIKE @patron OR i.correoElectronico LIKE @patron
               OR (i.nombre + N' ' + i.apellido) LIKE @patron)
        ORDER BY i.fechaAlta DESC
    ) ui
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'Espacio' AS tipo, e.idEspacioArtistico AS idReferencia, e.nombreEspacio AS titulo,
               e.tipoEspacio + N' · Gestor ' + g.nombre + N' ' + g.apellido
                 + CASE WHEN e.publicado = 1 AND e.activo = 1 THEN N' · Publicado' ELSE N' · No publicado' END AS detalle,
               e.fechaAlta AS fecha
        FROM dbo.EspacioArtistico e
        INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
        WHERE @incluirEspacios = 1
          AND (e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron
               OR (g.nombre + N' ' + g.apellido) LIKE @patron)
        ORDER BY e.fechaAlta DESC
    ) esp
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'Reserva' AS tipo, r.idReserva AS idReferencia,
               N'Reserva #' + CAST(r.idReserva AS NVARCHAR(20)) + N' · ' + e.nombreEspacio AS titulo,
               N'Solicitante ' + s.nombre + N' ' + s.apellido + N' · ' + CONVERT(NVARCHAR(10), r.fechaSolicitada, 103)
                 + N' · ' + r.estadoReserva AS detalle,
               r.fechaCreacion AS fecha
        FROM dbo.Reserva r
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        INNER JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = r.idUsuarioExternoSolicitante
        WHERE @incluirReservas = 1
          AND (r.idReserva = @idBuscado OR e.nombreEspacio LIKE @patron OR (s.nombre + N' ' + s.apellido) LIKE @patron
               OR s.correoElectronico LIKE @patron OR r.estadoReserva LIKE @patron)
        ORDER BY r.fechaCreacion DESC
    ) res
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'Ticket' AS tipo, t.idTicket AS idReferencia, t.asunto AS titulo,
               t.categoria + N' · ' + t.estado + N' · ' + u.nombre + N' ' + u.apellido AS detalle,
               t.fechaUltimaActividad AS fecha
        FROM dbo.Ticket t
        INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = t.idUsuarioExterno
        WHERE @incluirTickets = 1
          AND (t.idTicket = @idBuscado OR t.asunto LIKE @patron OR t.categoria LIKE @patron
               OR (u.nombre + N' ' + u.apellido) LIKE @patron OR u.correoElectronico LIKE @patron
               OR EXISTS (SELECT 1 FROM dbo.TicketMensaje m WHERE m.idTicket = t.idTicket AND m.mensaje LIKE @patron))
        ORDER BY t.fechaUltimaActividad DESC
    ) tic
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'Novedad' AS tipo, n.idNovedad AS idReferencia, n.titulo,
               n.categoria + CASE WHEN n.publicado = 1 THEN N' · Publicada' ELSE N' · Borrador' END + N' · ' + n.resumen AS detalle,
               n.fechaAlta AS fecha
        FROM dbo.Novedad n
        WHERE @incluirNovedades = 1
          AND n.activo = 1
          AND (n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron)
        ORDER BY n.fechaAlta DESC
    ) nov
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'FAQ' AS tipo, q.idFaq AS idReferencia, q.pregunta AS titulo,
               CASE WHEN q.activo = 1 THEN N'Activa · ' ELSE N'Inactiva · ' END + LEFT(q.respuesta, 250) AS detalle,
               CAST(NULL AS DATETIME) AS fecha
        FROM dbo.Faq q
        WHERE @incluirFaq = 1
          AND (q.pregunta LIKE @patron OR q.respuesta LIKE @patron)
        ORDER BY q.orden
    ) faq
    UNION ALL
    SELECT tipo, idReferencia, titulo, detalle, fecha FROM
    (
        SELECT TOP 25 N'Encuesta' AS tipo, en.idEncuesta AS idReferencia, en.titulo,
               en.estado + N' · Vence ' + CONVERT(NVARCHAR(10), en.fechaVencimiento, 103) AS detalle,
               en.fechaAlta AS fecha
        FROM dbo.Encuesta en
        WHERE @incluirEncuestas = 1
          AND (en.titulo LIKE @patron OR en.descripcion LIKE @patron)
        ORDER BY en.fechaAlta DESC
    ) enc;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso BUSCAR_EN_PLATAFORMA (mismo patrón que GESTIONAR_BACKUP en el 46)
-- + su opción en el menú dinámico.
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoBusqueda INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'BUSCAR_EN_PLATAFORMA'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoBusqueda IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'BUSCAR_EN_PLATAFORMA', N'Búsqueda en la plataforma',
             N'Buscar usuarios, espacios, reservas, tickets y contenidos según los permisos del rol.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/BusquedaInterna.aspx', 1);

        SET @idPermisoBusqueda = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Búsqueda en la plataforma',
            descripcion = N'Buscar usuarios, espacios, reservas, tickets y contenidos según los permisos del rol.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/BusquedaInterna.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoBusqueda;
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
             AND idPermisoInterno = @idPermisoBusqueda
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoBusqueda, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoBusqueda;
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

        DECLARE @idComponenteBusqueda INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'BUSCAR_EN_PLATAFORMA'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteBusqueda IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'BUSCAR_EN_PLATAFORMA', N'Búsqueda en la plataforma',
                 N'Buscar usuarios, espacios, reservas, tickets y contenidos según los permisos del rol.',
                 N'~/Interno/BusquedaInterna.aspx', 5, 1);

            SET @idComponenteBusqueda = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Búsqueda en la plataforma',
                descripcion = N'Buscar usuarios, espacios, reservas, tickets y contenidos según los permisos del rol.',
                urlAsociada = N'~/Interno/BusquedaInterna.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteBusqueda;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteBusqueda
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteBusqueda, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteBusqueda;
            END
        END
    END

    -- Opción del menú dinámico (script 45), al final del menú.
    IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
       AND @idComponenteBusqueda IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponenteBusqueda)
    BEGIN
        INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
        SELECT N'Búsqueda en la plataforma',
               N'Buscar usuarios, espacios, reservas, tickets y contenidos según los permisos del rol.',
               N'~/Interno/BusquedaInterna.aspx', N'Administración',
               ISNULL(MAX(orden), 0) + 10,
               @idComponenteBusqueda, 1
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
END CATCH;
GO
