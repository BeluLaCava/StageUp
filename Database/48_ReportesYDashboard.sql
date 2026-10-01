IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

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
              AND fechaSolicitada BETWEEN @desde AND @hasta) AS reservasConfirmadas,
        (SELECT ISNULL(SUM(importeEstimado), 0) FROM dbo.Reserva
            WHERE estadoReserva IN (N'Aceptada', N'Finalizada')
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
      AND ISNULL(r.moneda, N'ARS') = @moneda
      AND r.fechaSolicitada BETWEEN @desde AND @hasta
      AND (@tipoEspacio IS NULL OR e.tipoEspacio = @tipoEspacio)
    GROUP BY f.provincia, f.ciudad
    ORDER BY importeReservas DESC;
END
GO

IF OBJECT_ID('dbo.sp_Reporte_ReservasPorEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_ReservasPorEstado;
GO
CREATE PROCEDURE dbo.sp_Reporte_ReservasPorEstado
    @desde       DATE,
    @hasta       DATE,
    @provincia   NVARCHAR(100) = NULL,
    @tipoEspacio NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.estadoReserva AS estado, COUNT(*) AS cantidad
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE r.fechaCreacion >= @desde AND r.fechaCreacion < DATEADD(DAY, 1, @hasta)
      AND (@provincia IS NULL OR f.provincia = @provincia)
      AND (@tipoEspacio IS NULL OR e.tipoEspacio = @tipoEspacio)
    GROUP BY r.estadoReserva
    ORDER BY cantidad DESC;
END
GO

IF OBJECT_ID('dbo.sp_Reporte_ParticipacionEncuestas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_ParticipacionEncuestas;
GO
CREATE PROCEDURE dbo.sp_Reporte_ParticipacionEncuestas
AS
BEGIN
    SET NOCOUNT ON;

    -- destinatarios: usuarios externos activos del público objetivo (hoy),
    -- para comparar encuestas por tasa de participación y no solo por votos.
    SELECT e.idEncuesta, e.titulo, e.estado, e.publicoObjetivo, e.fechaVencimiento,
           (SELECT COUNT(*) FROM dbo.RespuestaEncuesta r WHERE r.idEncuesta = e.idEncuesta) AS cantidadRespuestas,
           (SELECT COUNT(*) FROM dbo.UsuarioExterno u
             WHERE u.activo = 1 AND u.estadoCuenta = N'Activa'
               AND (e.publicoObjetivo = N'Todos' OR u.perfilUsuario = e.publicoObjetivo)) AS cantidadDestinatarios
    FROM dbo.Encuesta e
    WHERE e.estado IN (N'Activa', N'Cerrada')
    ORDER BY e.fechaInicio DESC;
END
GO

-- Valores para los filtros (provincias y tipos de espacio con datos).
IF OBJECT_ID('dbo.sp_Reporte_ListarFiltros', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_ListarFiltros;
GO
CREATE PROCEDURE dbo.sp_Reporte_ListarFiltros
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT N'Provincia' AS filtro, f.provincia AS valor
    FROM dbo.FichaEspacio f
    WHERE f.provincia IS NOT NULL AND LEN(f.provincia) > 0
    UNION
    SELECT DISTINCT N'TipoEspacio', e.tipoEspacio
    FROM dbo.EspacioArtistico e
    WHERE e.activo = 1
    ORDER BY filtro, valor;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso VER_REPORTES (mismo patrón que GESTIONAR_BACKUP en el 46) + su
-- opción en el menú dinámico.
-- ---------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idPermisoReportes INT =
    (
        SELECT TOP 1 idPermisoInterno
        FROM dbo.PermisoInterno
        WHERE codigoPermiso = N'VER_REPORTES'
        ORDER BY idPermisoInterno
    );

    IF @idPermisoReportes IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
            (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
        VALUES
            (N'VER_REPORTES', N'Reportes y estadísticas',
             N'Ver el tablero de indicadores y los reportes con gráficos de reservas, ingresos y encuestas.',
             N'Administración', N'Ver', N'Activo', N'~/Interno/Reportes.aspx', 1);

        SET @idPermisoReportes = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Reportes y estadísticas',
            descripcion = N'Ver el tablero de indicadores y los reportes con gráficos de reservas, ingresos y encuestas.',
            modulo = N'Administración',
            accion = N'Ver',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/Reportes.aspx',
            activo = 1,
            fechaUltimaModificacion = GETDATE()
        WHERE idPermisoInterno = @idPermisoReportes;
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
             AND idPermisoInterno = @idPermisoReportes
       )
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoReportes, GETDATE(), 1);
    END
    ELSE IF @idRolAdministrador IS NOT NULL
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoReportes;
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

        DECLARE @idComponenteReportes INT =
        (
            SELECT TOP 1 idComponentePermiso
            FROM dbo.ComponentePermiso
            WHERE codigoPermiso = N'VER_REPORTES'
            ORDER BY idComponentePermiso
        );

        IF @idComponenteReportes IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'VER_REPORTES', N'Reportes y estadísticas',
                 N'Ver el tablero de indicadores y los reportes con gráficos de reservas, ingresos y encuestas.',
                 N'~/Interno/Reportes.aspx', 10, 1);

            SET @idComponenteReportes = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Reportes y estadísticas',
                descripcion = N'Ver el tablero de indicadores y los reportes con gráficos de reservas, ingresos y encuestas.',
                urlAsociada = N'~/Interno/Reportes.aspx',
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idComponentePermiso = @idComponenteReportes;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteReportes
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteReportes, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteReportes;
            END
        END
    END

    -- Opción del menú dinámico (script 45), al final del menú.
    IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
       AND @idComponenteReportes IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponenteReportes)
    BEGIN
        INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
        SELECT N'Reportes y estadísticas',
               N'Ver el tablero de indicadores y los reportes con gráficos de reservas, ingresos y encuestas.',
               N'~/Interno/Reportes.aspx', N'Administración',
               ISNULL(MAX(orden), 0) + 10,
               @idComponenteReportes, 1
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
