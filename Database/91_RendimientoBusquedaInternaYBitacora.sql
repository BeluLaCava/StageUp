

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RegistroActividad') AND name=N'IX_RegistroActividad_Fecha_Id_Consulta')
    CREATE INDEX IX_RegistroActividad_Fecha_Id_Consulta
    ON dbo.RegistroActividad(fechaOperacion DESC, idRegistroActividad DESC)
    INCLUDE(idUsuarioExternoResponsable,idUsuarioInternoResponsable,tipoOperacion,tipoEntidadAfectada,idEntidadAfectada,descripcionOperacion,origenOperacion);
GO

IF COL_LENGTH(N'dbo.RegistroActividad',N'tituloBusquedaInterna') IS NULL
    ALTER TABLE dbo.RegistroActividad ADD tituloBusquedaInterna AS (tipoOperacion + N' · ' + tipoEntidadAfectada) PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RegistroActividad') AND name=N'IX_RegistroActividad_Busqueda_FechaTitulo')
    CREATE INDEX IX_RegistroActividad_Busqueda_FechaTitulo ON dbo.RegistroActividad(fechaOperacion DESC,tituloBusquedaInterna ASC,idRegistroActividad ASC)
    INCLUDE(idUsuarioExternoResponsable,idUsuarioInternoResponsable,tipoOperacion,tipoEntidadAfectada,idEntidadAfectada,descripcionOperacion);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RegistroActividad') AND name=N'IX_RegistroActividad_Busqueda_TituloFecha')
    CREATE INDEX IX_RegistroActividad_Busqueda_TituloFecha ON dbo.RegistroActividad(tituloBusquedaInterna ASC,fechaOperacion DESC,idRegistroActividad ASC)
    INCLUDE(idUsuarioExternoResponsable,idUsuarioInternoResponsable,tipoOperacion,tipoEntidadAfectada,idEntidadAfectada,descripcionOperacion);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RegistroActividad') AND name=N'IX_RegistroActividad_Busqueda_Antiguos')
    CREATE INDEX IX_RegistroActividad_Busqueda_Antiguos ON dbo.RegistroActividad(fechaOperacion ASC,tituloBusquedaInterna ASC,idRegistroActividad ASC)
    INCLUDE(idUsuarioExternoResponsable,idUsuarioInternoResponsable,tipoOperacion,tipoEntidadAfectada,idEntidadAfectada,descripcionOperacion);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RegistroActividad') AND name=N'IX_RegistroActividad_TiposBusqueda')
    CREATE INDEX IX_RegistroActividad_TiposBusqueda ON dbo.RegistroActividad(tipoOperacion,tipoEntidadAfectada);
GO
IF COL_LENGTH(N'dbo.Reserva',N'prefijoTituloBusqueda') IS NULL
    ALTER TABLE dbo.Reserva ADD prefijoTituloBusqueda AS (N'Reserva #' + CONVERT(NVARCHAR(20),idReserva) + N' · ') PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Reserva') AND name=N'IX_Reserva_Busqueda_FechaTitulo')
    CREATE INDEX IX_Reserva_Busqueda_FechaTitulo ON dbo.Reserva(fechaSolicitada DESC,prefijoTituloBusqueda ASC,idReserva ASC)
    INCLUDE(idEspacioArtistico,idUsuarioExternoSolicitante,estadoReserva);
GO
IF COL_LENGTH(N'dbo.UsuarioExterno',N'nombreCompletoBusqueda') IS NULL
    ALTER TABLE dbo.UsuarioExterno ADD nombreCompletoBusqueda AS (nombre + N' ' + apellido) PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.UsuarioExterno') AND name=N'IX_UsuarioExterno_Busqueda_FechaNombre')
    CREATE INDEX IX_UsuarioExterno_Busqueda_FechaNombre ON dbo.UsuarioExterno(fechaAlta DESC,nombreCompletoBusqueda ASC,idUsuarioExterno ASC)
    INCLUDE(correoElectronico,estadoCuenta,perfilUsuario);
GO
CREATE OR ALTER PROCEDURE dbo.sp_RegistroActividad_Buscar
    @idUsuarioExternoResponsable    INT             = NULL,
    @fechaDesde                     DATETIME        = NULL,
    @fechaHasta                     DATETIME        = NULL,
    @tipoOperacion                  NVARCHAR(200)   = NULL,
    @tipoEntidadAfectada            NVARCHAR(200)   = NULL,
    @idUsuarioInternoResponsable    INT             = NULL,
    @textoResponsable               NVARCHAR(150)   = NULL,
    @tipoResponsable                NVARCHAR(10)    = NULL,
    @maximo                         INT             = 500
AS
BEGIN
    SET NOCOUNT ON;

    IF @maximo IS NULL OR @maximo < 1 OR @maximo > 500 SET @maximo = 500;

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
CREATE OR ALTER PROCEDURE dbo.sp_Busqueda_Interna
    @texto NVARCHAR(300) = NULL,
    @incluirUsuariosExternos BIT = 0, @incluirUsuariosInternos BIT = 0, @incluirEspacios BIT = 0,
    @incluirReservas BIT = 0, @incluirTickets BIT = 0, @incluirNovedades BIT = 0,
    @incluirFaq BIT = 0, @incluirEncuestas BIT = 0, @incluirRegistrosActividad BIT = 0,
    @fechaDesde DATETIME = NULL, @fechaHasta DATETIME = NULL, @estado NVARCHAR(30) = NULL,
    @ubicacion NVARCHAR(100) = NULL, @categoriaNovedad NVARCHAR(100) = NULL,
    @perfil NVARCHAR(100) = NULL, @rol NVARCHAR(150) = NULL, @responsable NVARCHAR(150) = NULL,
    @nombre NVARCHAR(150) = NULL, @correo NVARCHAR(150) = NULL, @operacion NVARCHAR(150) = NULL,
    @entidadAfectada NVARCHAR(150) = NULL, @idRegistro INT = NULL,
    @orden NVARCHAR(20) = N'Relevancia', @maximo INT = 25
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @patron NVARCHAR(310) = N'%' + ISNULL(@texto, N'') + N'%';
    DECLARE @hasta DATETIME = CASE WHEN @fechaHasta IS NULL THEN NULL ELSE DATEADD(DAY, 1, @fechaHasta) END;
    DECLARE @limite INT = CASE WHEN @maximo > 0 THEN @maximo ELSE 0 END;
    CREATE TABLE #resultados (tipo NVARCHAR(30), idReferencia INT, titulo NVARCHAR(MAX), detalle NVARCHAR(MAX), fecha DATETIME NULL, prioridad INT);

    IF @incluirUsuariosExternos = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @rol IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        BEGIN
        IF @orden=N'Relevancia'
        BEGIN
            INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo,c.idReferencia,c.titulo,c.detalle,c.fecha,c.prioridad
        FROM (
SELECT N'UsuarioExterno', u.idUsuarioExterno, u.nombreCompletoBusqueda,
               u.correoElectronico + N' · ' + ISNULL(u.perfilUsuario, N'-') + N' · Cuenta ' + ISNULL(u.estadoCuenta, N'-'), u.fechaAlta,
               CAST(0 AS INT)
        FROM dbo.UsuarioExterno u
        WHERE (@texto IS NULL OR u.nombre LIKE @patron OR u.apellido LIKE @patron OR u.correoElectronico LIKE @patron)
          AND (@estado IS NULL OR u.estadoCuenta = @estado) AND (@perfil IS NULL OR u.perfilUsuario = @perfil)
          AND (@nombre IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR u.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @responsable + N'%' OR u.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR u.idUsuarioExterno = @idRegistro)
          AND (@fechaDesde IS NULL OR u.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR u.fechaAlta < @hasta) AND (CASE WHEN @texto IS NOT NULL AND (u.nombre + N' ' + u.apellido) LIKE @patron THEN 0 ELSE 1 END)=0
        ) AS c(tipo,idReferencia,titulo,detalle,fecha,prioridad)
        ORDER BY c.fecha DESC,c.titulo ASC,c.idReferencia ASC OPTION (RECOMPILE,MAXDOP 1);
            IF (SELECT COUNT(*) FROM #resultados WHERE tipo=N'UsuarioExterno') < @limite
            INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo,c.idReferencia,c.titulo,c.detalle,c.fecha,c.prioridad
        FROM (
SELECT N'UsuarioExterno', u.idUsuarioExterno, u.nombreCompletoBusqueda,
               u.correoElectronico + N' · ' + ISNULL(u.perfilUsuario, N'-') + N' · Cuenta ' + ISNULL(u.estadoCuenta, N'-'), u.fechaAlta,
               CAST(1 AS INT)
        FROM dbo.UsuarioExterno u
        WHERE (@texto IS NULL OR u.nombre LIKE @patron OR u.apellido LIKE @patron OR u.correoElectronico LIKE @patron)
          AND (@estado IS NULL OR u.estadoCuenta = @estado) AND (@perfil IS NULL OR u.perfilUsuario = @perfil)
          AND (@nombre IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR u.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @responsable + N'%' OR u.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR u.idUsuarioExterno = @idRegistro)
          AND (@fechaDesde IS NULL OR u.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR u.fechaAlta < @hasta) AND (CASE WHEN @texto IS NOT NULL AND (u.nombre + N' ' + u.apellido) LIKE @patron THEN 0 ELSE 1 END)=1
        ) AS c(tipo,idReferencia,titulo,detalle,fecha,prioridad)
        ORDER BY c.fecha DESC,c.titulo ASC,c.idReferencia ASC OPTION (RECOMPILE,MAXDOP 1);
        END
        ELSE
        BEGIN
            INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo,c.idReferencia,c.titulo,c.detalle,c.fecha,c.prioridad
        FROM (
SELECT N'UsuarioExterno', u.idUsuarioExterno, u.nombreCompletoBusqueda,
               u.correoElectronico + N' · ' + ISNULL(u.perfilUsuario, N'-') + N' · Cuenta ' + ISNULL(u.estadoCuenta, N'-'), u.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND (u.nombre + N' ' + u.apellido) LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.UsuarioExterno u
        WHERE (@texto IS NULL OR u.nombre LIKE @patron OR u.apellido LIKE @patron OR u.correoElectronico LIKE @patron)
          AND (@estado IS NULL OR u.estadoCuenta = @estado) AND (@perfil IS NULL OR u.perfilUsuario = @perfil)
          AND (@nombre IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR u.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @responsable + N'%' OR u.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR u.idUsuarioExterno = @idRegistro)
          AND (@fechaDesde IS NULL OR u.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR u.fechaAlta < @hasta)
        ) AS c(tipo,idReferencia,titulo,detalle,fecha,prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.titulo ASC, c.idReferencia ASC OPTION (RECOMPILE,MAXDOP 1);
        END
    END;

    IF @incluirUsuariosInternos = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'UsuarioInterno', i.idUsuarioInterno, i.nombre + N' ' + i.apellido,
               i.correoElectronico + N' · Rol ' + ISNULL(ro.nombreRol, N'-') + N' · Cuenta ' + ISNULL(i.estadoCuenta, N'-'), i.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND (i.nombre + N' ' + i.apellido) LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.UsuarioInterno i LEFT JOIN dbo.RolInterno ro ON ro.idRolInterno = i.idRolInterno
        WHERE (@texto IS NULL OR i.nombre LIKE @patron OR i.apellido LIKE @patron OR i.correoElectronico LIKE @patron OR ro.nombreRol LIKE @patron)
          AND (@estado IS NULL OR i.estadoCuenta = @estado) AND (@rol IS NULL OR ro.nombreRol LIKE N'%' + @rol + N'%')
          AND (@nombre IS NULL OR (i.nombre + N' ' + i.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR i.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (i.nombre + N' ' + i.apellido) LIKE N'%' + @responsable + N'%' OR i.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR i.idUsuarioInterno = @idRegistro)
          AND (@fechaDesde IS NULL OR i.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR i.fechaAlta < @hasta)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.titulo ASC, c.idReferencia ASC
        OPTION (RECOMPILE, MAXDOP 1);

    IF @incluirEspacios = 1 AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'Espacio', e.idEspacioArtistico, e.nombreEspacio,
               e.tipoEspacio + N' · Gestor ' + g.nombre + N' ' + g.apellido + N' · ' + e.estadoEspacio + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N''), e.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.EspacioArtistico e INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE (@texto IS NULL OR e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron OR (g.nombre + N' ' + g.apellido) LIKE @patron)
          AND (@estado IS NULL OR e.estadoEspacio = @estado)
          AND (@ubicacion IS NULL OR f.ciudad LIKE N'%' + @ubicacion + N'%' OR f.provincia LIKE N'%' + @ubicacion + N'%' OR f.direccion LIKE N'%' + @ubicacion + N'%')
          AND (@nombre IS NULL OR e.nombreEspacio LIKE N'%' + @nombre + N'%')
          AND (@responsable IS NULL OR (g.nombre + N' ' + g.apellido) LIKE N'%' + @responsable + N'%' OR g.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR e.idEspacioArtistico = @idRegistro)
          AND (@fechaDesde IS NULL OR e.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR e.fechaAlta < @hasta)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.titulo ASC, c.idReferencia ASC
        OPTION (RECOMPILE, MAXDOP 1);

    IF @incluirReservas = 1 AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'Reserva', r.idReserva, N'Reserva #' + CAST(r.idReserva AS NVARCHAR(20)) + N' · ' + e.nombreEspacio,
               N'Solicitante ' + s.nombre + N' ' + s.apellido + N' · Gestor ' + g.nombre + N' ' + g.apellido + N' · ' + r.estadoReserva,
               r.fechaSolicitada, CASE WHEN @texto IS NOT NULL AND r.idReserva = TRY_CAST(@texto AS INT) THEN 0 ELSE 1 END
, r.prefijoTituloBusqueda
        FROM dbo.Reserva r INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        INNER JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = r.idUsuarioExternoSolicitante INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE (@texto IS NULL OR r.idReserva = TRY_CAST(@texto AS INT) OR e.nombreEspacio LIKE @patron OR (s.nombre + N' ' + s.apellido) LIKE @patron OR s.correoElectronico LIKE @patron)
          AND (@estado IS NULL OR r.estadoReserva = @estado)
          AND (@ubicacion IS NULL OR f.ciudad LIKE N'%' + @ubicacion + N'%' OR f.provincia LIKE N'%' + @ubicacion + N'%' OR f.direccion LIKE N'%' + @ubicacion + N'%')
          AND (@nombre IS NULL OR e.nombreEspacio LIKE N'%' + @nombre + N'%' OR (s.nombre + N' ' + s.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR s.correoElectronico LIKE N'%' + @correo + N'%' OR g.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (s.nombre + N' ' + s.apellido) LIKE N'%' + @responsable + N'%' OR (g.nombre + N' ' + g.apellido) LIKE N'%' + @responsable + N'%' OR s.correoElectronico LIKE N'%' + @responsable + N'%' OR g.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR r.idReserva = @idRegistro)
          AND (@fechaDesde IS NULL OR r.fechaSolicitada >= @fechaDesde) AND (@hasta IS NULL OR r.fechaSolicitada < @hasta)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad, tituloOrden)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.tituloOrden END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.tituloOrden ASC, c.idReferencia ASC
        OPTION (RECOMPILE, MAXDOP 1);

    IF @incluirTickets = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'Ticket', t.idTicket, t.asunto, t.categoria + N' · ' + t.estado + N' · ' + u.nombre + N' ' + u.apellido, t.fechaUltimaActividad,
               CASE WHEN @texto IS NOT NULL AND (t.idTicket = TRY_CAST(@texto AS INT) OR t.asunto LIKE @patron) THEN 0 ELSE 1 END
        FROM dbo.Ticket t INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = t.idUsuarioExterno LEFT JOIN dbo.UsuarioInterno ai ON ai.idUsuarioInterno = t.idUsuarioInternoAsignado
        WHERE (@texto IS NULL OR t.idTicket = TRY_CAST(@texto AS INT) OR t.asunto LIKE @patron OR t.categoria LIKE @patron OR (u.nombre + N' ' + u.apellido) LIKE @patron OR u.correoElectronico LIKE @patron)
          AND (@estado IS NULL OR t.estado = @estado)
          AND (@nombre IS NULL OR t.asunto LIKE N'%' + @nombre + N'%' OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR u.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (ai.nombre + N' ' + ai.apellido) LIKE N'%' + @responsable + N'%' OR ai.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR t.idTicket = @idRegistro)
          AND (@fechaDesde IS NULL OR t.fechaUltimaActividad >= @fechaDesde) AND (@hasta IS NULL OR t.fechaUltimaActividad < @hasta)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.titulo ASC, c.idReferencia ASC
        OPTION (RECOMPILE, MAXDOP 1);

    IF @incluirNovedades = 1 AND @ubicacion IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @responsable IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'Novedad', n.idNovedad, n.titulo, n.categoria + N' · ' + n.estado + N' · ' + n.resumen, n.fechaAlta, CASE WHEN @texto IS NOT NULL AND n.titulo LIKE @patron THEN 0 ELSE 1 END FROM dbo.vw_Novedad n WHERE (@texto IS NULL OR n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron) AND (@estado IS NULL OR n.estado = @estado) AND (@categoriaNovedad IS NULL OR n.categoria = @categoriaNovedad) AND (@nombre IS NULL OR n.titulo LIKE N'%' + @nombre + N'%') AND (@idRegistro IS NULL OR n.idNovedad = @idRegistro) AND (@fechaDesde IS NULL OR n.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR n.fechaAlta < @hasta)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.titulo ASC, c.idReferencia ASC
        OPTION (RECOMPILE, MAXDOP 1);
    IF @incluirFaq = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @responsable IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'FAQ', q.idFaq, q.pregunta, q.estado + N' · ' + LEFT(q.respuesta, 250), NULL, CASE WHEN @texto IS NOT NULL AND q.pregunta LIKE @patron THEN 0 ELSE 1 END FROM dbo.Faq q WHERE (@texto IS NULL OR q.pregunta LIKE @patron OR q.respuesta LIKE @patron) AND (@estado IS NULL OR q.estado = @estado) AND (@nombre IS NULL OR q.pregunta LIKE N'%' + @nombre + N'%') AND (@idRegistro IS NULL OR q.idFaq = @idRegistro)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, c.titulo ASC, c.idReferencia ASC
        OPTION (RECOMPILE, MAXDOP 1);
    IF @incluirEncuestas = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @responsable IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'Encuesta', en.idEncuesta, en.titulo, en.estado + N' · Vence ' + CONVERT(NVARCHAR(10), en.fechaVencimiento, 103), en.fechaAlta, CASE WHEN @texto IS NOT NULL AND en.titulo LIKE @patron THEN 0 ELSE 1 END FROM dbo.Encuesta en WHERE (@texto IS NULL OR en.titulo LIKE @patron OR en.descripcion LIKE @patron) AND (@estado IS NULL OR en.estado = @estado) AND (@nombre IS NULL OR en.titulo LIKE N'%' + @nombre + N'%') AND (@idRegistro IS NULL OR en.idEncuesta = @idRegistro) AND (@fechaDesde IS NULL OR en.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR en.fechaAlta < @hasta)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN c.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.titulo ASC, c.idReferencia ASC
        OPTION (RECOMPILE, MAXDOP 1);

    IF @incluirRegistrosActividad = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @estado IS NULL
        BEGIN
        IF @orden = N'Relevancia'
        BEGIN

            SELECT TOP(0) tipoOperacion,tipoEntidadAfectada INTO #prioridadRegistro FROM dbo.RegistroActividad;

            DECLARE @opActual NVARCHAR(200), @entidadActual NVARCHAR(200);
            IF @texto IS NOT NULL
            BEGIN
                SELECT @opActual=MIN(tipoOperacion) FROM dbo.RegistroActividad;
                WHILE @opActual IS NOT NULL
                BEGIN
                    SELECT @entidadActual=MIN(tipoEntidadAfectada) FROM dbo.RegistroActividad WHERE tipoOperacion=@opActual;
                    WHILE @entidadActual IS NOT NULL
                    BEGIN
                        IF @opActual LIKE @patron OR @entidadActual LIKE @patron
                            INSERT INTO #prioridadRegistro VALUES(@opActual,@entidadActual);
                        SELECT @entidadActual=MIN(tipoEntidadAfectada) FROM dbo.RegistroActividad WHERE tipoOperacion=@opActual AND tipoEntidadAfectada>@entidadActual;
                    END
                    SELECT @opActual=MIN(tipoOperacion) FROM dbo.RegistroActividad WHERE tipoOperacion>@opActual;
                END
            END
            IF EXISTS (SELECT 1 FROM #prioridadRegistro)
            INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'RegistroActividad', r.idRegistroActividad, r.tituloBusquedaInterna,
               ISNULL(COALESCE(ui.nombre + N' ' + ui.apellido, ue.nombre + N' ' + ue.apellido), N'Sistema') + N' · ' + ISNULL(r.descripcionOperacion, N'Sin detalle'),
               r.fechaOperacion, CAST(0 AS INT)
        FROM dbo.RegistroActividad r LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = r.idUsuarioInternoResponsable LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = r.idUsuarioExternoResponsable
        WHERE (@texto IS NULL OR r.tipoOperacion LIKE @patron OR r.tipoEntidadAfectada LIKE @patron OR r.descripcionOperacion LIKE @patron OR ui.nombre LIKE @patron OR ui.apellido LIKE @patron OR ui.correoElectronico LIKE @patron OR ue.nombre LIKE @patron OR ue.apellido LIKE @patron OR ue.correoElectronico LIKE @patron)
          AND (@operacion IS NULL OR r.tipoOperacion LIKE N'%' + @operacion + N'%') AND (@entidadAfectada IS NULL OR r.tipoEntidadAfectada LIKE N'%' + @entidadAfectada + N'%')
          AND (@responsable IS NULL OR (ui.nombre + N' ' + ui.apellido) LIKE N'%' + @responsable + N'%' OR ui.correoElectronico LIKE N'%' + @responsable + N'%' OR (ue.nombre + N' ' + ue.apellido) LIKE N'%' + @responsable + N'%' OR ue.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@nombre IS NULL OR r.descripcionOperacion LIKE N'%' + @nombre + N'%') AND (@correo IS NULL OR ui.correoElectronico LIKE N'%' + @correo + N'%' OR ue.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@idRegistro IS NULL OR r.idRegistroActividad = @idRegistro OR r.idEntidadAfectada = @idRegistro)
          AND (@fechaDesde IS NULL OR r.fechaOperacion >= @fechaDesde) AND (@hasta IS NULL OR r.fechaOperacion < @hasta) AND EXISTS (SELECT 1 FROM #prioridadRegistro p WHERE p.tipoOperacion=r.tipoOperacion AND p.tipoEntidadAfectada=r.tipoEntidadAfectada)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY c.fecha DESC, c.titulo ASC, c.idReferencia ASC OPTION (RECOMPILE, MAXDOP 1);
            IF (SELECT COUNT(*) FROM #resultados WHERE tipo=N'RegistroActividad') < @limite
            INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'RegistroActividad', r.idRegistroActividad, r.tituloBusquedaInterna,
               ISNULL(COALESCE(ui.nombre + N' ' + ui.apellido, ue.nombre + N' ' + ue.apellido), N'Sistema') + N' · ' + ISNULL(r.descripcionOperacion, N'Sin detalle'),
               r.fechaOperacion, CAST(1 AS INT)
        FROM dbo.RegistroActividad r LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = r.idUsuarioInternoResponsable LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = r.idUsuarioExternoResponsable
        WHERE (@texto IS NULL OR r.tipoOperacion LIKE @patron OR r.tipoEntidadAfectada LIKE @patron OR r.descripcionOperacion LIKE @patron OR ui.nombre LIKE @patron OR ui.apellido LIKE @patron OR ui.correoElectronico LIKE @patron OR ue.nombre LIKE @patron OR ue.apellido LIKE @patron OR ue.correoElectronico LIKE @patron)
          AND (@operacion IS NULL OR r.tipoOperacion LIKE N'%' + @operacion + N'%') AND (@entidadAfectada IS NULL OR r.tipoEntidadAfectada LIKE N'%' + @entidadAfectada + N'%')
          AND (@responsable IS NULL OR (ui.nombre + N' ' + ui.apellido) LIKE N'%' + @responsable + N'%' OR ui.correoElectronico LIKE N'%' + @responsable + N'%' OR (ue.nombre + N' ' + ue.apellido) LIKE N'%' + @responsable + N'%' OR ue.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@nombre IS NULL OR r.descripcionOperacion LIKE N'%' + @nombre + N'%') AND (@correo IS NULL OR ui.correoElectronico LIKE N'%' + @correo + N'%' OR ue.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@idRegistro IS NULL OR r.idRegistroActividad = @idRegistro OR r.idEntidadAfectada = @idRegistro)
          AND (@fechaDesde IS NULL OR r.fechaOperacion >= @fechaDesde) AND (@hasta IS NULL OR r.fechaOperacion < @hasta) AND NOT EXISTS (SELECT 1 FROM #prioridadRegistro p WHERE p.tipoOperacion=r.tipoOperacion AND p.tipoEntidadAfectada=r.tipoEntidadAfectada)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY c.fecha DESC, c.titulo ASC, c.idReferencia ASC OPTION (RECOMPILE, MAXDOP 1);
        END
        ELSE
        BEGIN
            INSERT INTO #resultados
        SELECT TOP (@limite) c.tipo, c.idReferencia, c.titulo, c.detalle, c.fecha, c.prioridad
        FROM (
SELECT N'RegistroActividad', r.idRegistroActividad, r.tituloBusquedaInterna,
               ISNULL(COALESCE(ui.nombre + N' ' + ui.apellido, ue.nombre + N' ' + ue.apellido), N'Sistema') + N' · ' + ISNULL(r.descripcionOperacion, N'Sin detalle'),
               r.fechaOperacion, CAST(1 AS INT)
        FROM dbo.RegistroActividad r LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = r.idUsuarioInternoResponsable LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = r.idUsuarioExternoResponsable
        WHERE (@texto IS NULL OR r.tipoOperacion LIKE @patron OR r.tipoEntidadAfectada LIKE @patron OR r.descripcionOperacion LIKE @patron OR ui.nombre LIKE @patron OR ui.apellido LIKE @patron OR ui.correoElectronico LIKE @patron OR ue.nombre LIKE @patron OR ue.apellido LIKE @patron OR ue.correoElectronico LIKE @patron)
          AND (@operacion IS NULL OR r.tipoOperacion LIKE N'%' + @operacion + N'%') AND (@entidadAfectada IS NULL OR r.tipoEntidadAfectada LIKE N'%' + @entidadAfectada + N'%')
          AND (@responsable IS NULL OR (ui.nombre + N' ' + ui.apellido) LIKE N'%' + @responsable + N'%' OR ui.correoElectronico LIKE N'%' + @responsable + N'%' OR (ue.nombre + N' ' + ue.apellido) LIKE N'%' + @responsable + N'%' OR ue.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@nombre IS NULL OR r.descripcionOperacion LIKE N'%' + @nombre + N'%') AND (@correo IS NULL OR ui.correoElectronico LIKE N'%' + @correo + N'%' OR ue.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@idRegistro IS NULL OR r.idRegistroActividad = @idRegistro OR r.idEntidadAfectada = @idRegistro)
          AND (@fechaDesde IS NULL OR r.fechaOperacion >= @fechaDesde) AND (@hasta IS NULL OR r.fechaOperacion < @hasta)
        ) AS c(tipo, idReferencia, titulo, detalle, fecha, prioridad)
        ORDER BY CASE WHEN @orden = N'Alfabetico' THEN c.titulo END ASC, CASE WHEN @orden = N'Antiguos' THEN c.fecha END ASC, c.fecha DESC, c.titulo ASC, c.idReferencia ASC OPTION (RECOMPILE, MAXDOP 1);
        END
    END;

    SELECT tipo, idReferencia, titulo, detalle, fecha, posicion FROM (SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.tipo ORDER BY CASE WHEN @orden = N'Alfabetico' THEN r.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN r.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN r.fecha END ASC, r.fecha DESC, r.titulo ASC, r.idReferencia ASC) AS posicion FROM #resultados r) x WHERE posicion <= @maximo ORDER BY tipo, posicion;
END

GO
COMMIT TRANSACTION;
GO
