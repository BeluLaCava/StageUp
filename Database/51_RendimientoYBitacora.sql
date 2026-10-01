IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ---------------------------------------------------------------------------
-- Bitácora
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RegistroActividad') AND name = N'IX_RegistroActividad_Fecha')
    CREATE INDEX IX_RegistroActividad_Fecha ON dbo.RegistroActividad (fechaOperacion DESC)
        INCLUDE (tipoOperacion, tipoEntidadAfectada, idUsuarioExternoResponsable, idUsuarioInternoResponsable);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RegistroActividad') AND name = N'IX_RegistroActividad_Externo')
    CREATE INDEX IX_RegistroActividad_Externo ON dbo.RegistroActividad (idUsuarioExternoResponsable, fechaOperacion DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RegistroActividad') AND name = N'IX_RegistroActividad_Interno')
    CREATE INDEX IX_RegistroActividad_Interno ON dbo.RegistroActividad (idUsuarioInternoResponsable, fechaOperacion DESC);
GO

IF OBJECT_ID('dbo.sp_RegistroActividad_Buscar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistroActividad_Buscar;
GO
CREATE PROCEDURE dbo.sp_RegistroActividad_Buscar
    @idUsuarioExternoResponsable    INT             = NULL,
    @fechaDesde                     DATETIME        = NULL,
    @fechaHasta                     DATETIME        = NULL,
    @tipoOperacion                  NVARCHAR(200)   = NULL,
    @tipoEntidadAfectada            NVARCHAR(200)   = NULL,
    @idUsuarioInternoResponsable    INT             = NULL,
    @textoResponsable               NVARCHAR(150)   = NULL,   -- nombre o correo (externo o interno)
    @tipoResponsable                NVARCHAR(10)    = NULL,   -- Externo | Interno | Sistema | NULL
    @maximo                         INT             = 500
AS
BEGIN
    SET NOCOUNT ON;

    IF @maximo IS NULL OR @maximo < 1 OR @maximo > 500 SET @maximo = 500;

    -- Los comodines de LIKE que escriba el usuario se buscan como texto.
    DECLARE @patron NVARCHAR(160) = CASE WHEN @textoResponsable IS NULL THEN NULL
        ELSE N'%' + REPLACE(REPLACE(REPLACE(@textoResponsable, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;

    SELECT TOP (@maximo)
        r.idRegistroActividad,
        r.idUsuarioExternoResponsable,
        r.idUsuarioInternoResponsable,
        r.tipoOperacion,
        r.tipoEntidadAfectada,
        r.idEntidadAfectada,
        r.descripcionOperacion,
        r.fechaOperacion,
        r.origenOperacion,
        ISNULL(ue.nombre + N' ' + ue.apellido, ui.nombre + N' ' + ui.apellido) AS nombreResponsable,
        ISNULL(ue.correoElectronico, ui.correoElectronico) AS correoResponsable
    FROM dbo.RegistroActividad r
    LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = r.idUsuarioExternoResponsable
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = r.idUsuarioInternoResponsable
    WHERE (@idUsuarioExternoResponsable IS NULL OR r.idUsuarioExternoResponsable = @idUsuarioExternoResponsable)
      AND (@idUsuarioInternoResponsable IS NULL OR r.idUsuarioInternoResponsable = @idUsuarioInternoResponsable)
      AND (@tipoResponsable IS NULL
           OR (@tipoResponsable = N'Externo' AND r.idUsuarioExternoResponsable IS NOT NULL)
           OR (@tipoResponsable = N'Interno' AND r.idUsuarioInternoResponsable IS NOT NULL)
           OR (@tipoResponsable = N'Sistema' AND r.idUsuarioExternoResponsable IS NULL AND r.idUsuarioInternoResponsable IS NULL))
      AND (@patron IS NULL
           OR ue.correoElectronico LIKE @patron OR (ue.nombre + N' ' + ue.apellido) LIKE @patron
           OR ui.correoElectronico LIKE @patron OR (ui.nombre + N' ' + ui.apellido) LIKE @patron)
      AND (@fechaDesde IS NULL OR r.fechaOperacion >= @fechaDesde)
      AND (@fechaHasta IS NULL OR r.fechaOperacion < DATEADD(DAY, 1, @fechaHasta))
      AND (@tipoOperacion IS NULL OR r.tipoOperacion = @tipoOperacion)
      AND (@tipoEntidadAfectada IS NULL OR r.tipoEntidadAfectada = @tipoEntidadAfectada)
    ORDER BY r.fechaOperacion DESC, r.idRegistroActividad DESC
    OPTION (RECOMPILE);
END
GO

IF OBJECT_ID('dbo.sp_RegistroActividad_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistroActividad_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_RegistroActividad_ObtenerPorId
    @idRegistroActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        r.idRegistroActividad, r.idUsuarioExternoResponsable, r.idUsuarioInternoResponsable,
        r.tipoOperacion, r.tipoEntidadAfectada, r.idEntidadAfectada, r.descripcionOperacion,
        r.fechaOperacion, r.origenOperacion,
        ISNULL(ue.nombre + N' ' + ue.apellido, ui.nombre + N' ' + ui.apellido) AS nombreResponsable,
        ISNULL(ue.correoElectronico, ui.correoElectronico) AS correoResponsable
    FROM dbo.RegistroActividad r
    LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = r.idUsuarioExternoResponsable
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = r.idUsuarioInternoResponsable
    WHERE r.idRegistroActividad = @idRegistroActividad;
END
GO

-- ---------------------------------------------------------------------------
-- Solicitudes recibidas
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_Reserva_ListarPorGestor', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ListarPorGestor;
GO
CREATE PROCEDURE dbo.sp_Reserva_ListarPorGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idReserva, r.idEspacioArtistico, r.idUsuarioExternoSolicitante, r.fechaSolicitada,
        r.comentarioSolicitante, r.estadoReserva, r.comentarioResolucion,
        r.fechaCreacion, r.fechaResolucion, r.fechaUltimaModificacion, r.fechaFinalizacion,
        r.minutoDesde, r.minutoHasta, r.precioHoraPactado, r.moneda, r.importeEstimado,
        r.comisionAplicada, r.importeComision, r.fechaCancelacion,
        r.estadoPago, r.fechaLimitePago, r.fechaPago,
        e.nombreEspacio, e.idUsuarioGestor,
        LTRIM(RTRIM(u.nombre + N' ' + u.apellido)) AS nombreSolicitante,
        u.correoElectronico AS correoSolicitante,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'Espacio'
        ) THEN 1 ELSE 0 END) AS calificacionEspacioRealizada,
        CONVERT(BIT, CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Calificacion c
            WHERE c.idReserva = r.idReserva AND c.tipoCalificacion = N'UsuarioSolicitante'
        ) THEN 1 ELSE 0 END) AS calificacionSolicitanteRealizada
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = r.idUsuarioExternoSolicitante
    WHERE e.idUsuarioGestor = @idUsuarioGestor
    ORDER BY
        CASE WHEN r.estadoReserva = N'Pendiente' THEN 0 WHEN r.estadoReserva = N'Aceptada' THEN 1 ELSE 2 END,
        r.fechaCreacion DESC;
END
GO

IF OBJECT_ID('dbo.sp_Reserva_ContarPendientesPorGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reserva_ContarPendientesPorGestor;
GO
CREATE PROCEDURE dbo.sp_Reserva_ContarPendientesPorGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND r.estadoReserva = N'Pendiente';
END
GO

IF OBJECT_ID('dbo.sp_Calificacion_ListarResumenesPorUsuarios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarResumenesPorUsuarios;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarResumenesPorUsuarios
    @idsUsuarios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    -- Mismo cálculo que sp_Calificacion_ListarResumenesUsuarios, pero solo
    -- para los usuarios pedidos y yendo por sus reservas (índice
    -- IX_Reserva_Solicitante) en lugar de recorrer toda la vista.
    SELECT
        r.idUsuarioExternoSolicitante AS idUsuarioExterno,
        CONVERT(DECIMAL(4,2), AVG(CONVERT(DECIMAL(10,2), c.puntaje))) AS promedio,
        COUNT(*) AS cantidadCalificaciones
    FROM (SELECT DISTINCT TRY_CONVERT(INT, value) AS idUsuario FROM STRING_SPLIT(@idsUsuarios, N',')) s
    INNER JOIN dbo.Reserva r ON r.idUsuarioExternoSolicitante = s.idUsuario
    INNER JOIN dbo.Calificacion c ON c.idReserva = r.idReserva
    WHERE c.tipoCalificacion = N'UsuarioSolicitante'
      AND c.activo = 1
    GROUP BY r.idUsuarioExternoSolicitante;
END
GO

IF OBJECT_ID('dbo.sp_Calificacion_ListarRecibidasTop3PorUsuarios', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Calificacion_ListarRecibidasTop3PorUsuarios;
GO
CREATE PROCEDURE dbo.sp_Calificacion_ListarRecibidasTop3PorUsuarios
    @idsUsuarios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    -- Mismo resultado que la versión del script 34 (las 3 calificaciones más
    -- recientes recibidas por cada solicitante), pero filtrando primero por
    -- las reservas de esos usuarios en vez de comparar contra la columna
    -- calculada idUsuarioEvaluado de la vista (que obligaba a recorrerla).
    ;WITH Numeradas AS (
        SELECT
            v.*,
            ROW_NUMBER() OVER (PARTITION BY v.idUsuarioEvaluado ORDER BY v.fechaAlta DESC) AS nroOrden
        FROM dbo.vw_CalificacionDetalle v
        WHERE v.tipoCalificacion = N'UsuarioSolicitante'
          AND v.activo = 1
          AND v.idReserva IN
          (
              SELECT r.idReserva
              FROM dbo.Reserva r
              INNER JOIN (SELECT DISTINCT TRY_CONVERT(INT, value) AS idUsuario FROM STRING_SPLIT(@idsUsuarios, N',')) s
                  ON s.idUsuario = r.idUsuarioExternoSolicitante
          )
    )
    SELECT *
    FROM Numeradas
    WHERE nroOrden <= 3
    ORDER BY idUsuarioEvaluado, fechaAlta DESC;
END
GO

-- ---------------------------------------------------------------------------
-- Índices para los procesos periódicos de reservas y las notificaciones
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Reserva') AND name = N'IX_Reserva_AceptadaPago')
    CREATE INDEX IX_Reserva_AceptadaPago ON dbo.Reserva (estadoReserva, estadoPago, fechaLimitePago)
        INCLUDE (fechaSolicitada, minutoDesde, minutoHasta, recordatorioEnviado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Reserva') AND name = N'IX_Reserva_FinalizadaSinAviso')
    CREATE INDEX IX_Reserva_FinalizadaSinAviso ON dbo.Reserva (estadoReserva, avisoFinalizacionEnviado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Notificacion') AND name = N'IX_Notificacion_UsuarioFecha')
    CREATE INDEX IX_Notificacion_UsuarioFecha ON dbo.Notificacion (idUsuarioExterno, fechaCreacion DESC)
        INCLUDE (tipo, mensaje, urlDestino, leida);
GO
