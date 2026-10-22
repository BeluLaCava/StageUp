USE StageUp;
GO

IF OBJECT_ID('dbo.sp_MensajeReserva_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MensajeReserva_Insertar;
GO
CREATE PROCEDURE dbo.sp_MensajeReserva_Insertar
    @idReserva INT,
    @idUsuarioExternoEmisor INT,
    @texto NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @estado NVARCHAR(50);
    SELECT @estado = r.estadoReserva
    FROM dbo.Reserva r
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
    WHERE r.idReserva = @idReserva
      AND (r.idUsuarioExternoSolicitante = @idUsuarioExternoEmisor OR e.idUsuarioGestor = @idUsuarioExternoEmisor);
    IF @estado IS NULL THROW 51050, 'El usuario no participa de la reserva.', 1;
    IF @estado NOT IN (N'Aceptada', N'Finalizada') THROW 51051, 'La conversación se habilita después de aprobar la reserva.', 1;
    INSERT INTO dbo.MensajeReserva (idReserva, idUsuarioExternoEmisor, texto) VALUES (@idReserva, @idUsuarioExternoEmisor, @texto);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

IF OBJECT_ID('dbo.sp_Busqueda_Interna', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Interna;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Interna
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
    CREATE TABLE #resultados (tipo NVARCHAR(30), idReferencia INT, titulo NVARCHAR(400), detalle NVARCHAR(1000), fecha DATETIME NULL, prioridad INT);

    IF @incluirUsuariosExternos = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @rol IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT N'UsuarioExterno', u.idUsuarioExterno, u.nombre + N' ' + u.apellido,
               u.correoElectronico + N' · ' + ISNULL(u.perfilUsuario, N'-') + N' · Cuenta ' + ISNULL(u.estadoCuenta, N'-'), u.fechaAlta,
               CASE WHEN @texto IS NOT NULL AND (u.nombre + N' ' + u.apellido) LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.UsuarioExterno u
        WHERE (@texto IS NULL OR u.nombre LIKE @patron OR u.apellido LIKE @patron OR u.correoElectronico LIKE @patron)
          AND (@estado IS NULL OR u.estadoCuenta = @estado) AND (@perfil IS NULL OR u.perfilUsuario = @perfil)
          AND (@nombre IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR u.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @responsable + N'%' OR u.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR u.idUsuarioExterno = @idRegistro)
          AND (@fechaDesde IS NULL OR u.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR u.fechaAlta < @hasta);

    IF @incluirUsuariosInternos = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
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
          AND (@fechaDesde IS NULL OR i.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR i.fechaAlta < @hasta);

    IF @incluirEspacios = 1 AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
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
          AND (@fechaDesde IS NULL OR e.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR e.fechaAlta < @hasta);

    IF @incluirReservas = 1 AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT N'Reserva', r.idReserva, N'Reserva #' + CAST(r.idReserva AS NVARCHAR(20)) + N' · ' + e.nombreEspacio,
               N'Solicitante ' + s.nombre + N' ' + s.apellido + N' · Gestor ' + g.nombre + N' ' + g.apellido + N' · ' + r.estadoReserva,
               r.fechaSolicitada, CASE WHEN @texto IS NOT NULL AND r.idReserva = TRY_CAST(@texto AS INT) THEN 0 ELSE 1 END
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
          AND (@fechaDesde IS NULL OR r.fechaSolicitada >= @fechaDesde) AND (@hasta IS NULL OR r.fechaSolicitada < @hasta);

    IF @incluirTickets = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados
        SELECT N'Ticket', t.idTicket, t.asunto, t.categoria + N' · ' + t.estado + N' · ' + u.nombre + N' ' + u.apellido, t.fechaUltimaActividad,
               CASE WHEN @texto IS NOT NULL AND (t.idTicket = TRY_CAST(@texto AS INT) OR t.asunto LIKE @patron) THEN 0 ELSE 1 END
        FROM dbo.Ticket t INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = t.idUsuarioExterno LEFT JOIN dbo.UsuarioInterno ai ON ai.idUsuarioInterno = t.idUsuarioInternoAsignado
        WHERE (@texto IS NULL OR t.idTicket = TRY_CAST(@texto AS INT) OR t.asunto LIKE @patron OR t.categoria LIKE @patron OR (u.nombre + N' ' + u.apellido) LIKE @patron OR u.correoElectronico LIKE @patron)
          AND (@estado IS NULL OR t.estado = @estado)
          AND (@nombre IS NULL OR t.asunto LIKE N'%' + @nombre + N'%' OR (u.nombre + N' ' + u.apellido) LIKE N'%' + @nombre + N'%')
          AND (@correo IS NULL OR u.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@responsable IS NULL OR (ai.nombre + N' ' + ai.apellido) LIKE N'%' + @responsable + N'%' OR ai.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@idRegistro IS NULL OR t.idTicket = @idRegistro)
          AND (@fechaDesde IS NULL OR t.fechaUltimaActividad >= @fechaDesde) AND (@hasta IS NULL OR t.fechaUltimaActividad < @hasta);

    IF @incluirNovedades = 1 AND @ubicacion IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @responsable IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados SELECT N'Novedad', n.idNovedad, n.titulo, n.categoria + N' · ' + n.estado + N' · ' + n.resumen, n.fechaAlta, CASE WHEN @texto IS NOT NULL AND n.titulo LIKE @patron THEN 0 ELSE 1 END FROM dbo.vw_Novedad n WHERE (@texto IS NULL OR n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron) AND (@estado IS NULL OR n.estado = @estado) AND (@categoriaNovedad IS NULL OR n.categoria = @categoriaNovedad) AND (@nombre IS NULL OR n.titulo LIKE N'%' + @nombre + N'%') AND (@idRegistro IS NULL OR n.idNovedad = @idRegistro) AND (@fechaDesde IS NULL OR n.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR n.fechaAlta < @hasta);
    IF @incluirFaq = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @responsable IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados SELECT N'FAQ', q.idFaq, q.pregunta, q.estado + N' · ' + LEFT(q.respuesta, 250), NULL, CASE WHEN @texto IS NOT NULL AND q.pregunta LIKE @patron THEN 0 ELSE 1 END FROM dbo.Faq q WHERE (@texto IS NULL OR q.pregunta LIKE @patron OR q.respuesta LIKE @patron) AND (@estado IS NULL OR q.estado = @estado) AND (@nombre IS NULL OR q.pregunta LIKE N'%' + @nombre + N'%') AND (@idRegistro IS NULL OR q.idFaq = @idRegistro);
    IF @incluirEncuestas = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @correo IS NULL AND @responsable IS NULL AND @operacion IS NULL AND @entidadAfectada IS NULL
        INSERT INTO #resultados SELECT N'Encuesta', en.idEncuesta, en.titulo, en.estado + N' · Vence ' + CONVERT(NVARCHAR(10), en.fechaVencimiento, 103), en.fechaAlta, CASE WHEN @texto IS NOT NULL AND en.titulo LIKE @patron THEN 0 ELSE 1 END FROM dbo.Encuesta en WHERE (@texto IS NULL OR en.titulo LIKE @patron OR en.descripcion LIKE @patron) AND (@estado IS NULL OR en.estado = @estado) AND (@nombre IS NULL OR en.titulo LIKE N'%' + @nombre + N'%') AND (@idRegistro IS NULL OR en.idEncuesta = @idRegistro) AND (@fechaDesde IS NULL OR en.fechaAlta >= @fechaDesde) AND (@hasta IS NULL OR en.fechaAlta < @hasta);

    IF @incluirRegistrosActividad = 1 AND @ubicacion IS NULL AND @categoriaNovedad IS NULL AND @perfil IS NULL AND @rol IS NULL AND @estado IS NULL
        INSERT INTO #resultados
        SELECT N'RegistroActividad', r.idRegistroActividad, r.tipoOperacion + N' · ' + r.tipoEntidadAfectada,
               ISNULL(COALESCE(ui.nombre + N' ' + ui.apellido, ue.nombre + N' ' + ue.apellido), N'Sistema') + N' · ' + ISNULL(r.descripcionOperacion, N'Sin detalle'),
               r.fechaOperacion, CASE WHEN @texto IS NOT NULL AND (r.tipoOperacion LIKE @patron OR r.tipoEntidadAfectada LIKE @patron) THEN 0 ELSE 1 END
        FROM dbo.RegistroActividad r LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = r.idUsuarioInternoResponsable LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = r.idUsuarioExternoResponsable
        WHERE (@texto IS NULL OR r.tipoOperacion LIKE @patron OR r.tipoEntidadAfectada LIKE @patron OR r.descripcionOperacion LIKE @patron OR ui.nombre LIKE @patron OR ui.apellido LIKE @patron OR ui.correoElectronico LIKE @patron OR ue.nombre LIKE @patron OR ue.apellido LIKE @patron OR ue.correoElectronico LIKE @patron)
          AND (@operacion IS NULL OR r.tipoOperacion LIKE N'%' + @operacion + N'%') AND (@entidadAfectada IS NULL OR r.tipoEntidadAfectada LIKE N'%' + @entidadAfectada + N'%')
          AND (@responsable IS NULL OR (ui.nombre + N' ' + ui.apellido) LIKE N'%' + @responsable + N'%' OR ui.correoElectronico LIKE N'%' + @responsable + N'%' OR (ue.nombre + N' ' + ue.apellido) LIKE N'%' + @responsable + N'%' OR ue.correoElectronico LIKE N'%' + @responsable + N'%')
          AND (@nombre IS NULL OR r.descripcionOperacion LIKE N'%' + @nombre + N'%') AND (@correo IS NULL OR ui.correoElectronico LIKE N'%' + @correo + N'%' OR ue.correoElectronico LIKE N'%' + @correo + N'%')
          AND (@idRegistro IS NULL OR r.idRegistroActividad = @idRegistro OR r.idEntidadAfectada = @idRegistro)
          AND (@fechaDesde IS NULL OR r.fechaOperacion >= @fechaDesde) AND (@hasta IS NULL OR r.fechaOperacion < @hasta);

    SELECT tipo, idReferencia, titulo, detalle, fecha, posicion FROM (SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.tipo ORDER BY CASE WHEN @orden = N'Alfabetico' THEN r.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN r.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN r.fecha END ASC, r.fecha DESC, r.titulo ASC) AS posicion FROM #resultados r) x WHERE posicion <= @maximo ORDER BY tipo, posicion;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FichaEspacio') AND name = N'IX_FichaEspacio_UbicacionBusqueda')
    CREATE INDEX IX_FichaEspacio_UbicacionBusqueda ON dbo.FichaEspacio (provincia, ciudad) INCLUDE (direccion, capacidadMaxima, precioHora, tipoPiso);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.EspacioArtistico') AND name = N'IX_EspacioArtistico_PublicadoTipo')
    CREATE INDEX IX_EspacioArtistico_PublicadoTipo ON dbo.EspacioArtistico (activo, publicado, tipoEspacio) INCLUDE (nombreEspacio, fechaPublicacion, idUsuarioGestor);
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_BuscarPublicados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_BuscarPublicados;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_BuscarPublicados
    @textoBusqueda NVARCHAR(300) = NULL,
    @tipoEspacio NVARCHAR(200) = NULL,
    @ubicacion NVARCHAR(150) = NULL,
    @provincia NVARCHAR(100) = NULL,
    @ciudad NVARCHAR(100) = NULL,
    @zona NVARCHAR(100) = NULL,
    @precioMinimo DECIMAL(18,2) = NULL,
    @precioMaximo DECIMAL(18,2) = NULL,
    @capacidadMinima INT = NULL,
    @tipoPiso NVARCHAR(100) = NULL,
    @caracteristicas NVARCHAR(150) = NULL,
    @calificacionMinima DECIMAL(3,2) = NULL,
    @fechaDisponibilidad DATE = NULL,
    @minutoDesde SMALLINT = NULL,
    @minutoHasta SMALLINT = NULL,
    @reqEspejos BIT = 0,
    @reqSonido BIT = 0,
    @reqInstrumentos BIT = 0,
    @reqEquipamiento BIT = 0,
    @reqEscenario BIT = 0,
    @reqIluminacion BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @diaSemana TINYINT = NULL;
    IF @fechaDisponibilidad IS NOT NULL SET @diaSemana = ((DATEPART(WEEKDAY, @fechaDisponibilidad) + @@DATEFIRST - 2) % 7) + 1;
    SELECT e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
           e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
           f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento,
           f.superficieM2, f.alturaM, f.condicionesUso, f.reglasUso,
           g.nombre AS nombreGestor, g.apellido AS apellidoGestor, g.fechaActivacion AS gestorFechaActivacion, g.fechaAlta AS gestorFechaAlta,
           (SELECT COUNT(*) FROM dbo.EspacioArtistico e2 WHERE e2.idUsuarioGestor = e.idUsuarioGestor AND e2.activo = 1 AND e2.publicado = 1) AS cantidadEspaciosPublicadosGestor
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE e.activo = 1 AND e.publicado = 1
      AND (@textoBusqueda IS NULL OR e.nombreEspacio LIKE N'%' + @textoBusqueda + N'%' OR e.tipoEspacio LIKE N'%' + @textoBusqueda + N'%' OR e.descripcion LIKE N'%' + @textoBusqueda + N'%')
      AND (@tipoEspacio IS NULL OR e.tipoEspacio LIKE N'%' + @tipoEspacio + N'%')
      AND (@ubicacion IS NULL OR f.ciudad LIKE N'%' + @ubicacion + N'%' OR f.provincia LIKE N'%' + @ubicacion + N'%' OR f.direccion LIKE N'%' + @ubicacion + N'%')
      AND (@provincia IS NULL OR f.provincia LIKE N'%' + @provincia + N'%')
      AND (@ciudad IS NULL OR f.ciudad LIKE N'%' + @ciudad + N'%')
      AND (@zona IS NULL OR f.direccion LIKE N'%' + @zona + N'%' OR f.ciudad LIKE N'%' + @zona + N'%')
      AND (@precioMinimo IS NULL OR f.precioHora >= @precioMinimo)
      AND (@precioMaximo IS NULL OR f.precioHora <= @precioMaximo)
      AND (@capacidadMinima IS NULL OR f.capacidadMaxima >= @capacidadMinima)
      AND (@tipoPiso IS NULL OR f.tipoPiso LIKE N'%' + @tipoPiso + N'%')
      AND (@caracteristicas IS NULL OR e.descripcion LIKE N'%' + @caracteristicas + N'%' OR f.detalleEquipamiento LIKE N'%' + @caracteristicas + N'%' OR f.condicionesUso LIKE N'%' + @caracteristicas + N'%' OR f.reglasUso LIKE N'%' + @caracteristicas + N'%')
      AND (@calificacionMinima IS NULL OR ISNULL((SELECT AVG(CAST(c.puntaje AS DECIMAL(10,2))) FROM dbo.Calificacion c INNER JOIN dbo.Reserva rc ON rc.idReserva = c.idReserva WHERE rc.idEspacioArtistico = e.idEspacioArtistico AND c.tipoCalificacion = N'Espacio' AND c.activo = 1), 0) >= @calificacionMinima)
      AND (@reqEspejos = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'ESPEJOS'))
      AND (@reqSonido = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'SONIDO'))
      AND (@reqInstrumentos = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'INSTRUMENTOS'))
      AND (@reqEquipamiento = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'EQUIPAMIENTO'))
      AND (@reqEscenario = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'ESCENARIO'))
      AND (@reqIluminacion = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'ILUMINACION'))
      AND (@fechaDisponibilidad IS NULL OR (
          EXISTS (SELECT 1 FROM dbo.FranjaEspacio fr WHERE fr.idEspacioArtistico = e.idEspacioArtistico AND fr.bloqueado = 0 AND (fr.fecha = @fechaDisponibilidad OR (fr.fecha IS NULL AND fr.diaSemana = @diaSemana)) AND (@minutoDesde IS NULL OR (@minutoDesde >= fr.minutoDesde AND @minutoHasta <= fr.minutoHasta)))
          AND (@minutoDesde IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Reserva res WHERE res.idEspacioArtistico = e.idEspacioArtistico AND res.fechaSolicitada = @fechaDisponibilidad AND res.estadoReserva IN (N'Pendiente', N'Aceptada') AND @minutoDesde < res.minutoHasta AND res.minutoDesde < @minutoHasta))
          AND (@minutoDesde IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.FranjaEspacio fb WHERE fb.idEspacioArtistico = e.idEspacioArtistico AND fb.bloqueado = 1 AND (fb.fecha = @fechaDisponibilidad OR (fb.fecha IS NULL AND fb.diaSemana = @diaSemana)) AND @minutoDesde < fb.minutoHasta AND fb.minutoDesde < @minutoHasta))
      ))
    ORDER BY e.fechaPublicacion DESC;
END
GO

IF OBJECT_ID('dbo.sp_Busqueda_Publica', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Publica;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Publica
    @texto NVARCHAR(300) = NULL, @incluirEspacios BIT = 1, @incluirNovedades BIT = 1, @incluirFaq BIT = 1,
    @fechaDesde DATETIME = NULL, @fechaHasta DATETIME = NULL, @ubicacion NVARCHAR(100) = NULL,
    @tipoEspacio NVARCHAR(100) = NULL, @provincia NVARCHAR(100) = NULL, @ciudad NVARCHAR(100) = NULL, @zona NVARCHAR(100) = NULL,
    @precioMinimo DECIMAL(18,2) = NULL, @capacidadMinima INT = NULL, @precioMaximo DECIMAL(18,2) = NULL,
    @fechaDisponibilidad DATE = NULL, @minutoDesde SMALLINT = NULL, @minutoHasta SMALLINT = NULL,
    @equipamiento NVARCHAR(400) = NULL, @caracteristicas NVARCHAR(150) = NULL, @calificacionMinima DECIMAL(3,2) = NULL,
    @categoriaNovedad NVARCHAR(100) = NULL, @orden NVARCHAR(20) = N'Relevancia', @maximo INT = 25
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @patron NVARCHAR(310) = N'%' + ISNULL(@texto, N'') + N'%';
    DECLARE @hasta DATETIME = CASE WHEN @fechaHasta IS NULL THEN NULL ELSE DATEADD(DAY, 1, @fechaHasta) END;
    DECLARE @diaSemana TINYINT = NULL;
    IF @fechaDisponibilidad IS NOT NULL SET @diaSemana = ((DATEPART(WEEKDAY, @fechaDisponibilidad) + @@DATEFIRST - 2) % 7) + 1;
    CREATE TABLE #resultados (tipo NVARCHAR(30), idReferencia INT, titulo NVARCHAR(400), detalle NVARCHAR(1000), fecha DATETIME NULL, prioridad INT);
    DECLARE @soloEspacios BIT = CASE WHEN @ubicacion IS NOT NULL OR @tipoEspacio IS NOT NULL OR @provincia IS NOT NULL OR @ciudad IS NOT NULL OR @zona IS NOT NULL OR @precioMinimo IS NOT NULL OR @capacidadMinima IS NOT NULL OR @precioMaximo IS NOT NULL OR @fechaDisponibilidad IS NOT NULL OR @equipamiento IS NOT NULL OR @caracteristicas IS NOT NULL OR @calificacionMinima IS NOT NULL THEN 1 ELSE 0 END;
    IF @incluirEspacios = 1
        INSERT INTO #resultados
        SELECT N'Espacio', e.idEspacioArtistico, e.nombreEspacio, e.tipoEspacio + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N'') + ISNULL(N' · Hasta ' + CAST(f.capacidadMaxima AS NVARCHAR(10)) + N' personas', N'') + ISNULL(N' · ' + ISNULL(f.moneda, N'ARS') + N' ' + CONVERT(NVARCHAR(30), f.precioHora) + N' por hora', N''), e.fechaPublicacion, CASE WHEN @texto IS NOT NULL AND e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.EspacioArtistico e LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE e.activo = 1 AND e.publicado = 1
          AND (@texto IS NULL OR e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron OR f.ciudad LIKE @patron OR f.provincia LIKE @patron)
          AND (@tipoEspacio IS NULL OR e.tipoEspacio LIKE N'%' + @tipoEspacio + N'%')
          AND (@ubicacion IS NULL OR f.ciudad LIKE N'%' + @ubicacion + N'%' OR f.provincia LIKE N'%' + @ubicacion + N'%' OR f.direccion LIKE N'%' + @ubicacion + N'%')
          AND (@provincia IS NULL OR f.provincia LIKE N'%' + @provincia + N'%') AND (@ciudad IS NULL OR f.ciudad LIKE N'%' + @ciudad + N'%') AND (@zona IS NULL OR f.direccion LIKE N'%' + @zona + N'%' OR f.ciudad LIKE N'%' + @zona + N'%')
          AND (@precioMinimo IS NULL OR f.precioHora >= @precioMinimo) AND (@precioMaximo IS NULL OR f.precioHora <= @precioMaximo) AND (@capacidadMinima IS NULL OR f.capacidadMaxima >= @capacidadMinima)
          AND (@caracteristicas IS NULL OR e.descripcion LIKE N'%' + @caracteristicas + N'%' OR f.detalleEquipamiento LIKE N'%' + @caracteristicas + N'%' OR f.condicionesUso LIKE N'%' + @caracteristicas + N'%' OR f.reglasUso LIKE N'%' + @caracteristicas + N'%')
          AND (@equipamiento IS NULL OR NOT EXISTS (SELECT 1 FROM STRING_SPLIT(@equipamiento, N',') req WHERE NOT EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = LTRIM(RTRIM(req.value)))))
          AND (@calificacionMinima IS NULL OR ISNULL((SELECT AVG(CAST(c.puntaje AS DECIMAL(10,2))) FROM dbo.Calificacion c INNER JOIN dbo.Reserva rc ON rc.idReserva = c.idReserva WHERE rc.idEspacioArtistico = e.idEspacioArtistico AND c.tipoCalificacion = N'Espacio' AND c.activo = 1), 0) >= @calificacionMinima)
          AND (@fechaDisponibilidad IS NULL OR (EXISTS (SELECT 1 FROM dbo.FranjaEspacio fr WHERE fr.idEspacioArtistico = e.idEspacioArtistico AND fr.bloqueado = 0 AND (fr.fecha = @fechaDisponibilidad OR (fr.fecha IS NULL AND fr.diaSemana = @diaSemana)) AND (@minutoDesde IS NULL OR (@minutoDesde >= fr.minutoDesde AND @minutoHasta <= fr.minutoHasta))) AND (@minutoDesde IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Reserva r WHERE r.idEspacioArtistico = e.idEspacioArtistico AND r.fechaSolicitada = @fechaDisponibilidad AND r.estadoReserva IN (N'Pendiente', N'Aceptada') AND @minutoDesde < r.minutoHasta AND r.minutoDesde < @minutoHasta))))
          AND (@fechaDesde IS NULL OR e.fechaPublicacion >= @fechaDesde) AND (@hasta IS NULL OR e.fechaPublicacion < @hasta);
    IF @incluirNovedades = 1 AND @soloEspacios = 0
        INSERT INTO #resultados SELECT N'Novedad', n.idNovedad, n.titulo, n.categoria + N' · ' + n.resumen, ISNULL(n.fechaPublicacion, n.fechaAlta), CASE WHEN @texto IS NOT NULL AND n.titulo LIKE @patron THEN 0 ELSE 1 END FROM dbo.vw_Novedad n WHERE n.estado = N'Vigente' AND (@texto IS NULL OR n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron) AND (@categoriaNovedad IS NULL OR n.categoria = @categoriaNovedad) AND (@fechaDesde IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) >= @fechaDesde) AND (@hasta IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) < @hasta);
    IF @incluirFaq = 1 AND @soloEspacios = 0 AND @categoriaNovedad IS NULL
        INSERT INTO #resultados SELECT N'FAQ', q.idFaq, q.pregunta, LEFT(q.respuesta, 300), NULL, CASE WHEN @texto IS NOT NULL AND q.pregunta LIKE @patron THEN 0 ELSE 1 END FROM dbo.Faq q INNER JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = q.idCategoriaAyuda AND c.activa = 1 WHERE q.estado = N'Activo' AND (@texto IS NULL OR q.pregunta LIKE @patron OR q.respuesta LIKE @patron);
    SELECT tipo, idReferencia, titulo, detalle, fecha, posicion FROM (SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.tipo ORDER BY CASE WHEN @orden = N'Alfabetico' THEN r.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN r.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN r.fecha END ASC, r.fecha DESC, r.titulo ASC) AS posicion FROM #resultados r) x WHERE posicion <= @maximo ORDER BY tipo, posicion;
END
GO
