IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.vw_Novedad', N'V') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/75_NovedadesVigenciaEImagen.sql.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Busqueda_Publica', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Publica;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Publica
    @texto              NVARCHAR(300) = NULL,
    @incluirEspacios    BIT = 1,
    @incluirNovedades   BIT = 1,
    @incluirFaq         BIT = 1,
    @fechaDesde         DATETIME = NULL,
    @fechaHasta         DATETIME = NULL,
    @ubicacion          NVARCHAR(100) = NULL,
    @capacidadMinima    INT = NULL,
    @precioMaximo       DECIMAL(18, 2) = NULL,
    @categoriaNovedad   NVARCHAR(100) = NULL,
    @orden              NVARCHAR(20) = N'Relevancia',
    @maximo             INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(310) = N'%' + ISNULL(@texto, N'') + N'%';
    DECLARE @patronUbicacion NVARCHAR(110) = N'%' + ISNULL(@ubicacion, N'') + N'%';

    CREATE TABLE #resultados
    (
        tipo NVARCHAR(30), idReferencia INT, titulo NVARCHAR(400), detalle NVARCHAR(1000),
        fecha DATETIME NULL, prioridad INT
    );

    -- Con capacidad, precio o ubicación se buscan solo espacios (son
    -- criterios que no tienen sentido en novedades ni en la ayuda).
    DECLARE @soloEspacios BIT = CASE WHEN @capacidadMinima IS NOT NULL OR @precioMaximo IS NOT NULL OR @ubicacion IS NOT NULL THEN 1 ELSE 0 END;

    IF @incluirEspacios = 1
        INSERT INTO #resultados
        SELECT N'Espacio', e.idEspacioArtistico, e.nombreEspacio,
               e.tipoEspacio + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N'')
                 + ISNULL(N' · Hasta ' + CAST(f.capacidadMaxima AS NVARCHAR(10)) + N' personas', N'')
                 + ISNULL(N' · ' + ISNULL(f.moneda, N'ARS') + N' ' + CONVERT(NVARCHAR(30), f.precioHora) + N' por hora', N'')
                 + ISNULL(N' · ' + LEFT(e.descripcion, 160), N''),
               e.fechaPublicacion,
               CASE WHEN @texto IS NOT NULL AND e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.EspacioArtistico e
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE e.activo = 1 AND e.publicado = 1
          AND (@texto IS NULL OR e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron
               OR f.ciudad LIKE @patron OR f.provincia LIKE @patron)
          AND (@ubicacion IS NULL OR f.ciudad LIKE @patronUbicacion OR f.provincia LIKE @patronUbicacion OR f.direccion LIKE @patronUbicacion)
          AND (@capacidadMinima IS NULL OR f.capacidadMaxima >= @capacidadMinima)
          AND (@precioMaximo IS NULL OR f.precioHora <= @precioMaximo)
          AND (@fechaDesde IS NULL OR e.fechaPublicacion >= @fechaDesde)
          AND (@fechaHasta IS NULL OR e.fechaPublicacion < DATEADD(DAY, 1, @fechaHasta));

    IF @incluirNovedades = 1 AND @soloEspacios = 0
        INSERT INTO #resultados
        SELECT N'Novedad', n.idNovedad, n.titulo, n.categoria + N' · ' + n.resumen,
               ISNULL(n.fechaPublicacion, n.fechaAlta),
               CASE WHEN @texto IS NOT NULL AND n.titulo LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.vw_Novedad n
        WHERE n.estado = N'Vigente'
          AND (@texto IS NULL OR n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron)
          AND (@categoriaNovedad IS NULL OR n.categoria = @categoriaNovedad)
          AND (@fechaDesde IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) >= @fechaDesde)
          AND (@fechaHasta IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) < DATEADD(DAY, 1, @fechaHasta));

    IF @incluirFaq = 1 AND @soloEspacios = 0 AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'FAQ', q.idFaq, q.pregunta, LEFT(q.respuesta, 300), NULL,
               CASE WHEN @texto IS NOT NULL AND q.pregunta LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.Faq q
        WHERE q.activo = 1
          AND (@texto IS NULL OR q.pregunta LIKE @patron OR q.respuesta LIKE @patron);

    SELECT tipo, idReferencia, titulo, detalle, fecha, posicion
    FROM
    (
        SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.tipo ORDER BY
                   CASE WHEN @orden = N'Alfabetico' THEN r.titulo END ASC,
                   CASE WHEN @orden = N'Relevancia' THEN r.prioridad END ASC,
                   CASE WHEN @orden = N'Antiguos' THEN r.fecha END ASC,
                   r.fecha DESC, r.titulo ASC) AS posicion
        FROM #resultados r
    ) ordenados
    WHERE posicion <= @maximo
    ORDER BY tipo, posicion;
END
GO

IF OBJECT_ID('dbo.sp_Busqueda_Interna', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Interna;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Interna
    @texto                    NVARCHAR(300) = NULL,
    @incluirUsuariosExternos  BIT = 0,
    @incluirUsuariosInternos  BIT = 0,
    @incluirEspacios          BIT = 0,
    @incluirReservas          BIT = 0,
    @incluirTickets           BIT = 0,
    @incluirNovedades         BIT = 0,
    @incluirFaq               BIT = 0,
    @incluirEncuestas         BIT = 0,
    @fechaDesde               DATETIME = NULL,
    @fechaHasta               DATETIME = NULL,
    @estado                   NVARCHAR(30) = NULL,
    @ubicacion                NVARCHAR(100) = NULL,
    @categoriaNovedad         NVARCHAR(100) = NULL,
    @orden                    NVARCHAR(20) = N'Relevancia',
    @maximo                   INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(310) = N'%' + ISNULL(@texto, N'') + N'%';
    DECLARE @patronUbicacion NVARCHAR(110) = N'%' + ISNULL(@ubicacion, N'') + N'%';
    DECLARE @idBuscado INT = TRY_CAST(@texto AS INT);
    DECLARE @hasta DATETIME = CASE WHEN @fechaHasta IS NULL THEN NULL ELSE DATEADD(DAY, 1, @fechaHasta) END;

    CREATE TABLE #resultados
    (
        tipo NVARCHAR(30), idReferencia INT, titulo NVARCHAR(400), detalle NVARCHAR(1000),
        fecha DATETIME NULL, prioridad INT
    );

    IF @incluirUsuariosExternos = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'UsuarioExterno', u.idUsuarioExterno, u.nombre + N' ' + u.apellido,
               u.correoElectronico + N' · ' + ISNULL(u.perfilUsuario, N'-') + N' · Cuenta ' + ISNULL(u.estadoCuenta, N'-'),
               u.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND (u.nombre + N' ' + u.apellido) LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.UsuarioExterno u
        WHERE (@texto IS NULL OR u.nombre LIKE @patron OR u.apellido LIKE @patron OR u.correoElectronico LIKE @patron
               OR (u.nombre + N' ' + u.apellido) LIKE @patron)
          AND (@estado IS NULL OR u.estadoCuenta = @estado)
          AND (@fechaDesde IS NULL OR u.fechaAlta >= @fechaDesde)
          AND (@hasta IS NULL OR u.fechaAlta < @hasta);

    IF @incluirUsuariosInternos = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'UsuarioInterno', i.idUsuarioInterno, i.nombre + N' ' + i.apellido,
               i.correoElectronico + N' · Rol ' + ISNULL(ro.nombreRol, N'-') + N' · Cuenta ' + ISNULL(i.estadoCuenta, N'-'),
               i.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND (i.nombre + N' ' + i.apellido) LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.UsuarioInterno i
        LEFT JOIN dbo.RolInterno ro ON ro.idRolInterno = i.idRolInterno
        WHERE (@texto IS NULL OR i.nombre LIKE @patron OR i.apellido LIKE @patron OR i.correoElectronico LIKE @patron
               OR (i.nombre + N' ' + i.apellido) LIKE @patron)
          AND (@estado IS NULL OR i.estadoCuenta = @estado)
          AND (@fechaDesde IS NULL OR i.fechaAlta >= @fechaDesde)
          AND (@hasta IS NULL OR i.fechaAlta < @hasta);

    IF @incluirEspacios = 1 AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'Espacio', e.idEspacioArtistico, e.nombreEspacio,
               e.tipoEspacio + N' · Gestor ' + g.nombre + N' ' + g.apellido + N' · ' + e.estadoEspacio
                 + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N''),
               e.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.EspacioArtistico e
        INNER JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE (@texto IS NULL OR e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron
               OR (g.nombre + N' ' + g.apellido) LIKE @patron)
          AND (@estado IS NULL OR e.estadoEspacio = @estado)
          AND (@ubicacion IS NULL OR f.ciudad LIKE @patronUbicacion OR f.provincia LIKE @patronUbicacion OR f.direccion LIKE @patronUbicacion)
          AND (@fechaDesde IS NULL OR e.fechaAlta >= @fechaDesde)
          AND (@hasta IS NULL OR e.fechaAlta < @hasta);

    -- Reservas: la fecha es la del día reservado.
    IF @incluirReservas = 1 AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'Reserva', r.idReserva,
               N'Reserva #' + CAST(r.idReserva AS NVARCHAR(20)) + N' · ' + e.nombreEspacio,
               N'Solicitante ' + s.nombre + N' ' + s.apellido + N' · ' + CONVERT(NVARCHAR(10), r.fechaSolicitada, 103)
                 + N' · ' + r.estadoReserva + ISNULL(N' · ' + f.ciudad, N''),
               r.fechaSolicitada,
               CASE WHEN r.idReserva = @idBuscado THEN 0 ELSE 1 END
        FROM dbo.Reserva r
        INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
        INNER JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = r.idUsuarioExternoSolicitante
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE (@texto IS NULL OR r.idReserva = @idBuscado OR e.nombreEspacio LIKE @patron OR (s.nombre + N' ' + s.apellido) LIKE @patron
               OR s.correoElectronico LIKE @patron OR r.estadoReserva LIKE @patron)
          AND (@estado IS NULL OR r.estadoReserva = @estado)
          AND (@ubicacion IS NULL OR f.ciudad LIKE @patronUbicacion OR f.provincia LIKE @patronUbicacion OR f.direccion LIKE @patronUbicacion)
          AND (@fechaDesde IS NULL OR r.fechaSolicitada >= @fechaDesde)
          AND (@hasta IS NULL OR r.fechaSolicitada < @hasta);

    IF @incluirTickets = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'Ticket', t.idTicket, t.asunto,
               t.categoria + N' · ' + t.estado + N' · ' + u.nombre + N' ' + u.apellido,
               t.fechaUltimaActividad,
               CASE WHEN t.idTicket = @idBuscado OR (@texto IS NOT NULL AND t.asunto LIKE @patron) THEN 0 ELSE 1 END
        FROM dbo.Ticket t
        INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = t.idUsuarioExterno
        WHERE (@texto IS NULL OR t.idTicket = @idBuscado OR t.asunto LIKE @patron OR t.categoria LIKE @patron
               OR (u.nombre + N' ' + u.apellido) LIKE @patron OR u.correoElectronico LIKE @patron
               OR EXISTS (SELECT 1 FROM dbo.TicketMensaje m WHERE m.idTicket = t.idTicket AND m.mensaje LIKE @patron))
          AND (@estado IS NULL OR t.estado = @estado)
          AND (@fechaDesde IS NULL OR t.fechaUltimaActividad >= @fechaDesde)
          AND (@hasta IS NULL OR t.fechaUltimaActividad < @hasta);

    IF @incluirNovedades = 1 AND @ubicacion IS NULL
        INSERT INTO #resultados
        SELECT N'Novedad', n.idNovedad, n.titulo,
               n.categoria + N' · ' + n.estado + N' · ' + n.resumen,
               n.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND n.titulo LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.vw_Novedad n
        WHERE (@texto IS NULL OR n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron)
          AND (@estado IS NULL OR n.estado = @estado)
          AND (@categoriaNovedad IS NULL OR n.categoria = @categoriaNovedad)
          AND (@fechaDesde IS NULL OR n.fechaAlta >= @fechaDesde)
          AND (@hasta IS NULL OR n.fechaAlta < @hasta);

    IF @incluirFaq = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'FAQ', q.idFaq, q.pregunta,
               q.estado + N' · ' + LEFT(q.respuesta, 250),
               NULL,
               CASE WHEN @texto IS NOT NULL AND q.pregunta LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.Faq q
        WHERE (@texto IS NULL OR q.pregunta LIKE @patron OR q.respuesta LIKE @patron)
          AND (@estado IS NULL OR q.estado = @estado);

    IF @incluirEncuestas = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'Encuesta', en.idEncuesta, en.titulo,
               en.estado + N' · Vence ' + CONVERT(NVARCHAR(10), en.fechaVencimiento, 103),
               en.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND en.titulo LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.Encuesta en
        WHERE (@texto IS NULL OR en.titulo LIKE @patron OR en.descripcion LIKE @patron)
          AND (@estado IS NULL OR en.estado = @estado)
          AND (@fechaDesde IS NULL OR en.fechaAlta >= @fechaDesde)
          AND (@hasta IS NULL OR en.fechaAlta < @hasta);

    SELECT tipo, idReferencia, titulo, detalle, fecha, posicion
    FROM
    (
        SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.tipo ORDER BY
                   CASE WHEN @orden = N'Alfabetico' THEN r.titulo END ASC,
                   CASE WHEN @orden = N'Relevancia' THEN r.prioridad END ASC,
                   CASE WHEN @orden = N'Antiguos' THEN r.fecha END ASC,
                   r.fecha DESC, r.titulo ASC) AS posicion
        FROM #resultados r
    ) ordenados
    WHERE posicion <= @maximo
    ORDER BY tipo, posicion;
END
GO
